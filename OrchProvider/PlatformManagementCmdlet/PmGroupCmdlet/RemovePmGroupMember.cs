using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using TPositional = UiPath.PowerShell.Positional.GroupName_Type_UserName;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "PmGroupMember", SupportsShouldProcess = true)]
//[OutputType(typeof(Entities.IdGroup))]
public class RemovePmGroupMemberCommand : OrchestratorPSCmdlet
{
    // Key: (drive, group), Value: Members
    private Dictionary<(OrchDriveInfo drive, PmGroup group), HashSet<PmGroupMember>>? _parameterSets = null;

    // CSV で重複した行を除去するために使う
    private Dictionary<(OrchDriveInfo drive, PmGroup group), HashSet<PmGroupMember>>? _visitedUsersHash = null;

    // CSV で指定したユーザー名ワイルドカードの重複を除去するために使う
    private HashSet<(OrchDriveInfo drive, PmGroup group, string type, string userName)>? _visitedUserPatterns = null;

    private class DirectoryType(int type, string objectKind)
    {
        public static readonly Dictionary<string, DirectoryType> All = new()
        {
            { "DirectoryUser",        new DirectoryType(0, "user") },
            { "DirectoryRobotUser",   new DirectoryType(3, "robot") },
            { "DirectoryApplication", new DirectoryType(4, "application") },
        };

        public int Type { get; } = type;
        public string ObjectKind { get; } = objectKind;
    }

    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmGroupNameCompleter<TPositional>))]
    [SupportsWildcards]
    public string[]? GroupName { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TypeInPmGroupCompleter<TPositional>))]
    [SupportsWildcards]
    public string[]? Type { get; set; }

    [Parameter(Position = 2, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(UserNameInPmGroupCompleter<TPositional>))]
    [SupportsWildcards]
    public string[]? UserName { get; set; }

    [Parameter]
    public SwitchParameter NoMatchWarning { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
    public string[]? Path { get; set; }

    protected override void ProcessRecord()
    {
        var drives = OrchDriveInfo.EnumPmDrives(Path);

        // CSV に指定された GroupName はカンマで区切る
        var groupName = GroupName!
            .SelectMany(g => g.Split(',', StringSplitOptions.RemoveEmptyEntries))
            .Select(g => g.Trim())
            .ToArray();
        var wpGroupName = groupName.ConvertToWildcardPatternList();

        // CSV に指定された Type はカンマで区切る
        var type = Type!
             .SelectMany(t => t.Split(',', StringSplitOptions.RemoveEmptyEntries))
             .Select(t => t.Trim())
             .ToArray();
        var wpType = type.ConvertToWildcardPatternList();

        var wpUserName = UserName.ConvertToWildcardPatternList();

        // 指定されたパラメータを保持する
        // ドライブ、グループ、ユーザーは展開する
        foreach (var drive in drives)
        {
            var existingGroups = drive.GetPmGroups();

            var targetGroups = existingGroups.Values
                .Where(g => g is not null)
                .FilterByWildcards(g => g!.name ?? "", wpGroupName);

            if (NoMatchWarning.IsPresent && !(targetGroups?.Any() ?? false))
            {
                WriteWarning($"No match found for GroupName '{drive.NameColonSeparator}{GroupName![0]}'.");
                continue;
            }

            foreach (var group in targetGroups)
            {
                // このグループのメンバを取得
                var detailedGroup = drive.GetPmGroup(group?.id);

                var targetMembers = detailedGroup?.members?
                    .FilterByWildcards(m => m?.objectType, wpType)
                    .FilterByWildcards(m => m?.name, wpUserName);

                if (NoMatchWarning.IsPresent && !(targetMembers?.Any() ?? false))
                {
                    _visitedUserPatterns ??= [];
                    // ちょっと雑だけど、CSV を処理する場合は配列にひとつの要素しかないのでこれで十分か。
                    if (!_visitedUserPatterns.Add((drive, group!, Type![0], UserName![0])))
                        continue;

                    WriteWarning($"No match found for UserName '{UserName![0]}' ({Type![0]}) in GroupName '{group?.GetPSPath()}'.");

                    continue;
                }
                if (targetMembers is null) continue; // !WarnOnNoMatch.IsPresent の場合を考慮

                foreach (var member in targetMembers)
                {
                    _visitedUsersHash ??= [];
                    if (!_visitedUsersHash.TryGetValue((drive, detailedGroup!), out var visitedUsers))
                    {
                        visitedUsers = [];
                        _visitedUsersHash[(drive, detailedGroup!)] = visitedUsers;
                    }

                    if (!visitedUsers!.Add(member))
                    {
                        // 処理済みなのでスキップする
                        continue;
                    }

                    string target = $"{member.TipHelp()} from {group?.GetPSPath()}";
                    if (ShouldProcess(target, $"Remove Member From Group"))
                    {
                        _parameterSets ??= [];
                        if (!_parameterSets.TryGetValue((drive, detailedGroup!), out var membersToRemove))
                        {
                            membersToRemove = [];
                            _parameterSets[(drive, detailedGroup!)] = membersToRemove;
                        }
                        membersToRemove!.Add(member);
                    }
                }
            }
        }
    }

    protected override void EndProcessing()
    {
        if (_parameterSets is null) return;

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var param in _parameterSets
            .OrderBy(p => p.Key.drive.Name)
            .ThenBy(p => p.Key.group.name))
        {
            cancelHandler.Token.ThrowIfCancellationRequested();

            var (drive, group) = param.Key;
            var toBeRemoved = param.Value;

            if (toBeRemoved.Count == 0) continue;

            string partitionGlobalId = drive.GetPartitionGlobalId();

            try
            {
                UpdateGroupCommand updateGroupCommand = new()
                {
                    partitionGlobalId = partitionGlobalId,
                    name = group!.name,
                    directoryUserIDsToAdd = [],
                    directoryUserIDsToRemove = toBeRemoved.Select(m => m.identifier).ToList()!
                };

                drive.OrchAPISession.PutPmGroup(group.id, updateGroupCommand);
                drive._dicPmGroups = null;
                drive._dicPmGroups_Exception.ClearCache();
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(group!.GetPSPath(), ex), "PutPmGroupError", ErrorCategory.InvalidOperation, group));
            }
        }
    }
}
