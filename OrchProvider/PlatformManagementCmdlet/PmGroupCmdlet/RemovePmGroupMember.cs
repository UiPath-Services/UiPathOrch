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

    // Used to remove duplicate rows from CSV
    private Dictionary<(OrchDriveInfo drive, PmGroup group), HashSet<PmGroupMember>>? _visitedUsersHash = null;

    // Used to remove duplicate user name wildcard patterns from CSV
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
    [Alias("Name")]
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
        var drives = SessionState.EnumPmDrives(Path);

        // Split GroupName specified in CSV by commas
        var groupName = GroupName!
            .SelectMany(g => g.Split(',', StringSplitOptions.RemoveEmptyEntries))
            .Select(g => g.Trim())
            .ToArray();
        var wpGroupName = groupName.ConvertToWildcardPatternList();

        // Split Type specified in CSV by commas
        var type = Type!
             .SelectMany(t => t.Split(',', StringSplitOptions.RemoveEmptyEntries))
             .Select(t => t.Trim())
             .ToArray();
        var wpType = type.ConvertToWildcardPatternList();

        var wpUserName = UserName.ConvertToWildcardPatternList();

        // Preserve the specified parameters.
        // Expand drives, groups, and users.
        foreach (var drive in drives)
        {
            var targetGroups = drive.PmGroups.Get()
                .Where(g => g is not null)
                .FilterByWildcards(g => g!.name ?? "", wpGroupName);

            if (NoMatchWarning.IsPresent && !(targetGroups?.Any() ?? false))
            {
                WriteWarning($"No match found for GroupName '{drive.NameColonSeparator}{GroupName![0]}'.");
                continue;
            }

            foreach (var group in targetGroups)
            {
                // Get the members of this group
                var detailedGroup = drive.PmGroups.Get(group?.id);

                var targetMembers = detailedGroup?.members?
                    .FilterByWildcards(m => m?.objectType, wpType)
                    .FilterByWildcards(m => m?.name, wpUserName);

                if (NoMatchWarning.IsPresent && !(targetMembers?.Any() ?? false))
                {
                    _visitedUserPatterns ??= [];
                    // A bit rough, but when processing CSV, there's only one element in the array, so this is sufficient.
                    if (!_visitedUserPatterns.Add((drive, group!, Type![0], UserName![0])))
                        continue;

                    WriteWarning($"No match found for UserName '{UserName![0]}' ({Type![0]}) in GroupName '{group?.GetPSPath()}'.");

                    continue;
                }
                if (targetMembers is null) continue; // Handle the case when !WarnOnNoMatch.IsPresent

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
                        // Already processed, so skip
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
                var updatedGroup = drive.RemoveMemberFromPmGroup(group.id, group.name, toBeRemoved?.Select(m => m.identifier));
                if (updatedGroup is not null)
                {
                    WriteObject(updatedGroup);
                }
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(group!.GetPSPath(), ex), "PutPmGroupError", ErrorCategory.InvalidOperation, group));
            }
        }
    }
}
