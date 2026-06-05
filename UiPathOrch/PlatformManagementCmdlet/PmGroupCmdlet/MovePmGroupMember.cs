using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// -Type is not yet implemented, but is this okay for now...
[Cmdlet(VerbsCommon.Move, "PmGroupMember", SupportsShouldProcess = true)]
public class MoveOrchPmGroupMemberCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmGroupNameCompleter))]
    [SupportsWildcards]
    [Alias("Name")]
    public string? GroupName { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(UserNameInPmGroupCompleter))]
    [SupportsWildcards]
    public string[]? UserName { get; set; }

    [Parameter(Position = 2, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmGroupNameCompleter))]
    [SupportsWildcards]
    public string[]? Destination { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BoolCompleter))]
    public string? KeepSource { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    // key: groupName
    private Dictionary<(OrchDriveInfo drive, PmGroup group), UpdateGroupCommand>? updates;

    protected override void ProcessRecord()
    {
        updates ??= [];

        //Type = Type.SplitValuesByUnescapedCommas()?.ToArray();
        var processedUserName = UserName;
        var processedDestination = Destination;
        var processedPath = EffectivePath(Path, LiteralPath);

        var drives = SessionState.EnumPmDrives(processedPath);
        var wpGroupName = new WildcardPattern(GroupName, WildcardOptions.IgnoreCase);

        //var wpType = Type.ConvertToWildcardPatternList();
        //var objectTypes = DirectoryTypes.Items.FilterByWildcards(t => t.Value, wpType).Select(t => t.Key);

        var wpUserName = processedUserName.ConvertToWildcardPatternList();
        var wpDestination = processedDestination.ConvertToWildcardPatternList();

        bool keepSource = KeepSource.ToNullableBool() ?? false;
        string action = keepSource ? "Copy PMGroupMember" : "Move PmGroupMember";

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives.WithCancellation(cancelHandler.Token))
        {
            IEnumerable<PmGroup> existingGroups = null;
            try
            {
                existingGroups = drive.PmGroups.Get();
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetPmGroupError", ErrorCategory.InvalidOperation, drive));
                continue;
            }

            var srcGroups = existingGroups.Where(g => wpGroupName.IsMatch(g.name));
            var dstGroups = existingGroups.FilterByWildcards(g => g?.name, wpDestination);

            // It's tedious to ensure the source group is exactly one...
            // Even if multiple are specified, we'll just process them all, which should be fine.

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
                        if (ShouldProcess(member.GetPSPath(drive.NameColonSeparator), action))
                        {
                            // First, record the addition to dstGroup
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

                            // Next, record the removal from srcGroup
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

            // Remove ids that appear in both the add and remove lists
            if (command.directoryUserIDsToAdd is not null && command.directoryUserIDsToRemove is not null)
            {
                var commonElements = command.directoryUserIDsToAdd.Intersect(command.directoryUserIDsToRemove).ToList();
                // Remove common elements from both lists
                foreach (var item in commonElements)
                {
                    command.directoryUserIDsToAdd.Remove(item);
                    command.directoryUserIDsToRemove.Remove(item);
                }
            }

            List<string> existingMemberIds = group.members?.Select(m => m.identifier!).ToList();

            // Remove ids that are already in the group from the add list
            if (command.directoryUserIDsToAdd is not null && existingMemberIds is not null)
            {
                command.directoryUserIDsToAdd.RemoveAll(m => existingMemberIds.Contains(m));
            }

            // Remove ids that are not already in the group from the remove list
            if (command.directoryUserIDsToRemove is not null && existingMemberIds is not null)
            {
                command.directoryUserIDsToRemove.RemoveAll(m => !existingMemberIds.Contains(m));
            }

            // Set empty lists to null
            if (command.directoryUserIDsToAdd?.Count == 0) command.directoryUserIDsToAdd = null;
            if (command.directoryUserIDsToRemove?.Count == 0) command.directoryUserIDsToRemove = null;

            // If both are null, no API call is needed
            if (command.directoryUserIDsToAdd is null &&
                command.directoryUserIDsToRemove is null)
            {
                continue;
            }

            try
            {
                drive.OrchAPISession.PutPmGroup(group.id, command);
                drive.PmGroups.ClearCache();
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(group.GetPSPath(drive.NameColonSeparator), ex), "UpdatePmGroupError", ErrorCategory.InvalidOperation, group));
            }
        }
    }
}
