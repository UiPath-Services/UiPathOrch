using System.Collections;
using UiPath.PowerShell.Positional;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Add, "PmGroupMember", SupportsShouldProcess = true)]
public class AddPmGroupMemberCommand : OrchestratorPSCmdlet
{
    // Expand and hold parameters for Path and PmGroup only
    //private List<(OrchDriveInfo Drive, PmGroup Group, string Type, string UserName)>? _csvLines;
    //private HashSet<(OrchDriveInfo Drive, PmGroup Group, string Type, string UserName)>? _csvLines;

    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmGroupNameCompleter))]
    [SupportsWildcards]
    [Alias("Name")]
    public string[]? GroupName { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<DirectoryTypes>))]
    [SupportsWildcards]
    public string[]? Type { get; set; }

    [Parameter(Position = 2, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmUserNameCompleter))]
    public string[]? UserName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    private static void CacheExistingMemberIds(List<OrchDriveInfo> drives, List<WildcardPattern>? wpGroupName)
    {
        ParallelResults.GroupBy(drives, drive =>
        {
            var groups = drive.PmGroups.Get()
                .FilterByWildcards(g => g?.name!, wpGroupName)
                .OrderBy(g => g?.name);
            return ParallelResults.ForEach(groups, group => drive.PmGroups.Get(group?.id));
        }).ToList();
    }

    private class PmUserNameCompleter : OrchArgumentCompleter
    {
        private static bool IsMemberOf(PmGroup group, PmDirectoryEntityInfo user)
        {
            // Cannot verify correctly with identifier. Must check using identifierName.
            // Returns true if at least one matches.
            return group.members?.Any(m => string.Compare(m.name, user.identityName, true) == 0) ?? false;
        }

        private static bool IsMemberOfAll(IEnumerable<PmGroup> groups, PmDirectoryEntityInfo user)
        {
            return groups.All(g => IsMemberOf(g, user));
        }

        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            if (string.IsNullOrEmpty(wordToComplete))
            {
                yield return new CompletionResult(PathTools.EscapePSText("Please enter at least one character to search."));
                yield break;
            }

            var wpGroupName = GetFakeBoundParameters(fakeBoundParameters, "GroupName").ConvertToWildcardPatternList();
            var wpUserName = CreateSelfExclusionList(commandAst, "UserName", wordToComplete);
            var wpType = GetFakeBoundParameters(fakeBoundParameters, "Type").ConvertToWildcardPatternList();
            var wpType2 = DirectoryTypes2.Items.FilterByWildcards(d => d.Key, wpType).Select(d => d.Value).ConvertToWildcardPatternList();

            var drives = ResolvePmDrives(fakeBoundParameters);

            CacheExistingMemberIds(drives, wpGroupName);

            wordToComplete = RemoveEnclosingQuotes(wordToComplete);

            bool bFound = false;
            foreach (var drive in drives)
            {
                var existingGroups = drive.PmGroups.Get();
                var updatingGroups = existingGroups.FilterByWildcards(u => u!.name!, wpGroupName);

                var users = drive.SearchPmDirectory(wordToComplete);
                if (users is null) continue;

                foreach (var user in users
                    .ExcludeByWildcards(e => e?.identityName, wpUserName)
                    .FilterByWildcards(u => u?.objectType, wpType2)
                    .OrderBy(e => e.identityName))
                {
                    // Skip if all groups in updatingGroups already contain the user as a member
                    if (IsMemberOfAll(updatingGroups, user)) continue;

                    // Skip if it is a local group
                    if (user.objectType == "DirectoryGroup" && user.source == "local") continue;

                    // Note: SearchPmDirectory() does not return non-confidential apps, so this is fine

                    bFound = true;
                    string tiphelp = user.TipHelp();
                    yield return new CompletionResult(PathTools.EscapePSText(user?.identityName), user?.identityName, CompletionResultType.Text, tiphelp);
                }
            }
            if (!bFound)
            {
                yield return new CompletionResult($@"""(No users found for '{wordToComplete}')""");
            }
        }
    }

    private HashSet<(OrchDriveInfo drive, PmGroup group, string type, string userName)>? _csvLines;

    protected override void ProcessRecord()
    {
        GroupName = GroupName.Split1stValueByUnescapedCommas()?.ToArray();
        Type = Type.Split1stValueByUnescapedCommas()?.ToArray();
        UserName = UserName.Split1stValueByUnescapedCommas()?.ToArray();
        Path = Path.Split1stValueByUnescapedCommas()?.ToArray();

        // Check for duplicate user names ignoring case
        _csvLines ??= new(new ForthItemIgnoreCaseComparer<OrchDriveInfo, PmGroup, string>());

        var drives = SessionState.EnumPmDrives(Path);
        var wpGroupName = GroupName.ConvertToWildcardPatternList();
        var wpType = Type.ConvertToWildcardPatternList(); // Type does not support wildcards
        //var objectTypes = DirectoryTypes.Items.FilterByWildcards(t => t.Value, wpType).Select(t => t.Key);

        // To query user information in bulk, users would need to be grouped by Type.
        // Actually, just aggregating the CSV information here is sufficient.
        // TODO: Should probably warn when a wildcard doesn't match any group names, but that's a bit tedious.
        foreach (var drive in drives)
        {
            var groups = drive.PmGroups.Get();
            var filteredGroups = groups.FilterByWildcards(g => g?.name, wpGroupName).ToList();

            foreach (var group in filteredGroups)
            {
                foreach (var type in DirectoryTypes.Parameters.FilterByWildcards(v => v, wpType))
                {
                    foreach (var userName in UserName!)
                    {
                        _csvLines.Add((drive, group, type, userName));
                    }
                }
            }
        }
    }

    protected override void EndProcessing()
    {
        if (_csvLines is null) return;

        // Query the directory in bulk for the names to add.
        // Queries must be grouped by type.
        // Robots cannot be queried in bulk, so they are handled separately.
        foreach (var lines in _csvLines.GroupBy(g => (g.drive, g.type)))
        {
            var (drive, type) = lines.Key;

            Dictionary<string, PmGroupMember?>? entries = null;
            switch (type)
            {
                case "DirectoryUser":
                    entries = drive.PmBulkResolveByName("user", lines, line => line.userName);
                    break;
                case "DirectoryGroup":
                    entries = drive.PmBulkResolveByName("group", lines, line => line.userName);
                    break;
                case "DirectoryApplication":
                    entries = drive.PmBulkResolveByName("application", lines, line => line.userName);
                    break;
                case "DirectoryRobotUser":
                    foreach (var name in lines)
                    {
                        var addingMember = drive.SearchPmDirectory(name.userName)?
                            .Where(t => t.objectType == type)
                            .FirstOrDefault(t => string.Compare(t.identityName, name.userName, true) == 0);

                        // Display a warning if not found
                        if (addingMember is null)
                        {
                            WriteWarning($"\"{drive.NameColonSeparator}\": {type} \"{name.userName}\" not found. Ignoring.");
                        }
                    }
                    break;
            }

            foreach (var entry in entries ?? [])
            {
                if (entry.Value is null)
                {
                    WriteWarning($"\"{drive.NameColonSeparator}\": {type} \"{entry.Key}\" not found. Ignoring.");
                }
                else if (entry.Value.objectType == "DirectoryGroup" && entry.Value.source == "local")
                {
                    WriteWarning($"\"{drive.NameColonSeparator}\": {type} \"{entry.Key}\" cannot be added because it is a local group. Ignoring.");
                }
            }
        }

        // Now the actual processing begins.
        // Add members to their groups.
        foreach (var drivesGroups in _csvLines.GroupBy(g => (g.drive, g.group)))
        {
            var (drive, group) = drivesGroups.Key;

            try
            {
                // Get the member list for each group to be updated
                var detailedGroup = drive.PmGroups.Get(group.id);
                if (detailedGroup is null) continue;

                HashSet<string> identifiers = [];
                PmGroupMember? entry = null;
                PmDirectoryEntityInfo? robotEntry = null;
                foreach (var (_, _, type, name) in drivesGroups)
                {
                    switch (type)
                    {
                        case "DirectoryRobotUser":
                            robotEntry = drive.SearchPmDirectory(name)?
                                .Where(t => string.Compare(t.objectType, "DirectoryRobotUser", true) == 0)
                                .FirstOrDefault(t => string.Compare(t.identityName, name, true) == 0);
                            if (robotEntry is not null)
                            {
                                if (group.members?.Any(m => m.identifier == robotEntry.identifier) ?? false)
                                {
                                    WriteWarning($"\"{group.GetPSPath()}\" already includes {type} \"{robotEntry.identityName}\".");
                                    continue;
                                }
                                if (ShouldProcess($"Item: {name} Destination: {group.GetPSPath()}", $"Add {type} to PmGroup"))
                                {
                                    identifiers.Add(robotEntry.identifier!);
                                }
                            }
                            continue;
                        case "DirectoryUser":
                            entry = drive.PmBulkResolveByName("user", [name], n => n).FirstOrDefault().Value;
                            break;
                        case "DirectoryGroup":
                            entry = drive.PmBulkResolveByName("group", [name], n => n).FirstOrDefault().Value;
                            if (entry?.source == "local") continue;
                            break;
                        case "DirectoryApplication":
                            entry = drive.PmBulkResolveByName("application", [name], n => n).FirstOrDefault().Value;
                            break;
                    }

                    if (entry is not null)
                    {
                        if (group.members?.Any(m => m.identifier == entry.identifier) ?? false)
                        {
                            WriteWarning($"\"{group.GetPSPath()}\" already includes {type} \"{entry.name}\".");
                            continue;
                        }
                        if (ShouldProcess($"Item: {name} Destination: {group.GetPSPath()}", $"Add {type} to PmGroup"))
                        {
                            identifiers.Add(entry.identifier!);
                            continue;
                        }
                    }
                }

                if (identifiers.Count > 0)
                {
                    // Add members to the existing group
                    var updatedGroup = drive.AddMemberToPmGroup(group.id, group.name, identifiers);
                    if (updatedGroup is not null)
                    {
                        WriteObject(updatedGroup);
                    }
                }
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(group.GetPSPath(), ex), "UpdatePmGroupError", ErrorCategory.InvalidOperation, group));
            }
        }
    }
}
