using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using TPositional = UiPath.PowerShell.Positional.GroupName_UserName_Destination;

namespace UiPath.PowerShell.Commands;

// -Type が未実装だけど、これで良いのかな。。
[Cmdlet(VerbsCommon.Move, "PmGroupMember", SupportsShouldProcess = true)]
public class MoveOrchPmGroupMemberCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmGroupNameCompleter<TPositional>))]
    [SupportsWildcards]
    public string? GroupName { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(UserNameInPmGroupCompleter<TPositional>))]
    [SupportsWildcards]
    public string[]? UserName { get; set; }

    [Parameter(Position = 2, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmGroupNameCompleter<TPositional>))]
    [SupportsWildcards]
    public string[]? Destination { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BoolCompleter))]
    public string? KeepSource { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter<Positional.GroupName_Type_UserName>))]
    public string[]? Path { get; set; }

    // key: groupName
    private Dictionary<(OrchDriveInfo drive, PmGroup group), UpdateGroupCommand>? updates;

    protected override void ProcessRecord()
    {
        updates ??= [];

        //Type = Type.Split1stValueByUnescapedCommas()?.ToArray();
        UserName = UserName.Split1stValueByUnescapedCommas()?.ToArray();
        Destination = Destination.Split1stValueByUnescapedCommas()?.ToArray();
        Path = Path.Split1stValueByUnescapedCommas()?.ToArray();

        var drives = OrchDriveInfo.EnumPmDrives(Path);
        var wpGroupName = new WildcardPattern(GroupName, WildcardOptions.IgnoreCase);

        //var wpType = Type.ConvertToWildcardPatternList();
        //var objectTypes = DirectoryTypes.Items.FilterByWildcards(t => t.Value, wpType).Select(t => t.Key);

        var wpUserName = UserName.ConvertToWildcardPatternList();
        var wpDestination = Destination.ConvertToWildcardPatternList();

        bool keepSource = KeepSource.ToNullableBool() ?? false;
        string action = keepSource ? "Copy PMGroupMember" : "Move PmGroupMember";

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives)
        {
            cancelHandler.Token.ThrowIfCancellationRequested();

            ICollection<PmGroup> existingGroups = null;
            try
            {
                existingGroups = drive.GetPmGroups().Values;
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetPmGroupError", ErrorCategory.InvalidOperation, drive));
                continue;
            }

            var srcGroups = existingGroups.Where(g => wpGroupName.IsMatch(g.name));
            var dstGroups = existingGroups.FilterByWildcards(g => g?.name, wpDestination);

            // source group が一つであることを sure にするのが面倒くさい。。
            // 複数指定した場合でも、そのまま動かしちゃうのでいいか。。

            foreach (var srcGroup in srcGroups.OrderBy(g => g.name))
            {
                var targetMembers = srcGroup.members?
                    //.Where(t => objectTypes.Contains(t.objectType))
                    .FilterByWildcards(m => m?.name, wpUserName) ?? [];

                foreach (var dstGroup in dstGroups.OrderBy(g => g.name))
                {
                    if (srcGroup == dstGroup) continue;

                    foreach (var member in targetMembers.OrderBy(m => m.name))
                    {
                        if (ShouldProcess(member.GetPSPath(), action))
                        {
                            // まず dstGroup への追加を記録
                            if (updates.TryGetValue((drive, dstGroup), out var groupToAddMember))
                            {
                                groupToAddMember.directoryUserIDsToAdd ??= [];
                                groupToAddMember.directoryUserIDsToAdd.Add(member.identifier!);
                            }
                            else
                            {
                                updates[(drive, dstGroup)] = new UpdateGroupCommand()
                                {
                                    partitionGlobalId = drive.GetPartitionGlobalId(),
                                    name = dstGroup.name,
                                    directoryUserIDsToAdd = [member.identifier!]
                                };
                            }

                            // 続いて、srcGroup からの削除を記録
                            if (!keepSource)
                            {
                                if (updates.TryGetValue((drive, srcGroup), out var groupToRemoveMember))
                                {
                                    groupToRemoveMember.directoryUserIDsToRemove ??= [];
                                    groupToRemoveMember.directoryUserIDsToRemove.Add(member.identifier!);
                                }
                                else
                                {
                                    updates[(drive, srcGroup)] = new UpdateGroupCommand()
                                    {
                                        partitionGlobalId = drive.GetPartitionGlobalId(),
                                        name = srcGroup.name,
                                        directoryUserIDsToRemove = [member.identifier!]
                                    };
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    protected override void EndProcessing()
    {
        if (updates is null) return;

        foreach (var update in updates.OrderBy(u => u.Key.drive.Name).ThenBy(u => u.Key.group.name))
        {
            var (drive, group) = update.Key;
            var command = update.Value;

            // add と remove の両方にある id は除去しておく
            if (command.directoryUserIDsToAdd is not null && command.directoryUserIDsToRemove is not null)
            {
                var commonElements = command.directoryUserIDsToAdd.Intersect(command.directoryUserIDsToRemove).ToList();
                // 共通の要素を両方のリストから削除
                foreach (var item in commonElements)
                {
                    command.directoryUserIDsToAdd.Remove(item);
                    command.directoryUserIDsToRemove.Remove(item);
                }
            }

            List<string> existingMemberIds = group.members?.Select(m => m.identifier!).ToList();

            // すでにグループにある id は、add から除去しておく
            if (command.directoryUserIDsToAdd is not null && existingMemberIds  is not null)
            {
                command.directoryUserIDsToAdd.RemoveAll(m => existingMemberIds.Contains(m));
            }

            // すでにグループにない id は、remove から削除しておく
            if (command.directoryUserIDsToRemove is not null && existingMemberIds is not null)
            {
                command.directoryUserIDsToRemove.RemoveAll(m => !existingMemberIds.Contains(m));
            }

            // 空っぽになったリストには null を入れておく
            if (command.directoryUserIDsToAdd   ?.Count == 0) command.directoryUserIDsToAdd    = null;
            if (command.directoryUserIDsToRemove?.Count == 0) command.directoryUserIDsToRemove = null;

            // 両方 null なら、API call は不要
            if (command.directoryUserIDsToAdd    is null &&
                command.directoryUserIDsToRemove is null)
            {
                continue;
            }

            try
            {
                drive.OrchAPISession.PutPmGroup(group.id, command);
                drive._dicPmGroups = null;
                drive._dicPmGroups_Exception.ClearCache();
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(group.GetPSPath(), ex), "UpdatePmGroupError", ErrorCategory.InvalidOperation, group));
            }
        }
    }
}
