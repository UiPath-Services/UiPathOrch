using System.Collections;
using UiPath.PowerShell.Positional;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Commands.CsvHelper;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Add, "DuUser", SupportsShouldProcess = true)]
[OutputType(typeof(void))]
public class AddDuRoleToDuUserCmdlet : OrchestratorPSCmdlet
{
    Dictionary<((OrchDuDriveInfo drive, DuProject), string type, string name), CsvLine>? _csvLines = null;

    private class CsvLine(AddDuRoleToDuUserCmdlet cmdlet) : CsvLineBase
    {
        public HashSet<string> Roles { get; set; } = new(cmdlet.Roles ?? []);

        public void Update(AddDuRoleToDuUserCmdlet cmdl)
        {
            this.Roles.UnionWith(cmdl.Roles?.Where(r => !string.IsNullOrEmpty(r)) ?? []);
        }
    }

    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<DirectoryTypes>))]
    [ValidateStaticCandidate<DirectoryTypes>]
    public string[]? Type { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmDirectoryNameCompleter4Du))]
    public string[]? Name { get; set; }

    [Parameter(Position = 2, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(RoleCompleter))]
    [SupportsWildcards]
    public string[]? Roles { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    // The implementation is nearly identical to the class with the same name in AddFolderUser.cs.
    //private class UserNameCompleter4Du : OrchArgumentCompleter
    //{
    //    //  0: User, 1: Group, 2: Machine, 3: Robot, 4: ExternalApplication

    //    public override IEnumerable<CompletionResult> CompleteArgumentCore(
    //        string commandName,
    //        string parameterName,
    //        string wordToComplete,
    //        CommandAst commandAst,
    //        IDictionary fakeBoundParameters)
    //    {
    //        if (string.IsNullOrEmpty(wordToComplete))
    //        {
    //            yield return new CompletionResult(PathTools.EscapePSText("Please enter at least one character to search."));
    //            yield break;
    //        }

    //        var paramType = GetFakeBoundParameter(fakeBoundParameters, "Type");
    //        if (!DirectoryTypeItems.Items.TryGetValue(paramType ?? "", out var objectType))
    //        {
    //            //yield return new CompletionResult(PathTools.EscapePSText("Invalid Type."));
    //            yield break;
    //        }

    //        var drives = ResolveDuDrives(fakeBoundParameters);

    //        // Excluding users already assigned to the folder is not implemented for now
    //        //var existingMemberIds = GetExistingMemberIds(drives, wpName);
    //        // Retrieve assigned users in order to exclude them
    //        //ParallelResults3.GroupBy(drivesFolders, df => df.drive.GetUsersForFolder(df.folder, false));

    //        var paramUserName = GetSelfExclusionValues(commandAst, parameterName, wordToComplete);
    //        bool bFound = false;
    //        foreach (var drive in drives)
    //        {
    //            string partitionGlobalId = drive.ParentDrive.GetPartitionGlobalId();
    //            var ret = drive.ParentDrive.SearchDirectory(wordToComplete);
    //            if (ret is null) continue;

    //            foreach (var e in ret
    //                .Where(e => e.type == objectType)
    //                //.ExcludeByTexts(e => e.identityName!, assignedUsers?.Select(u => u.Id.ToString()!) ?? [])
    //                .ExcludeByClassValues(e => e?.identityName, paramUserName)
    //                .OrderBy(e => e.identityName))
    //            {
    //                bFound = true;
    //                string tiphelp = e.identityName;
    //                yield return new CompletionResult(PathTools.EscapePSText(e?.identityName), e?.identityName, CompletionResultType.Text, tiphelp);
    //            }
    //        }
    //        if (!bFound)
    //        {
    //            yield return new CompletionResult($"\"No results matching '{wordToComplete}'\".");
    //        }
    //    }
    //}

    // This RoleCompleter only enumerates roles that are NOT assigned to the user
    private class RoleCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            //var drives = ResolveDuDrives(fakeBoundParameters);
            var drivesProjects = ResolveDuPath(commandAst, fakeBoundParameters);

            // Exclude roles already assigned to users with this name
            var wpName = GetFakeBoundParameters(fakeBoundParameters, "Name").ConvertToWildcardPatternList();

            // Exclude already-selected Role values from completion candidates
            var wpRole = CreateSelfExclusionList(commandAst, "Role", wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drivesProjects, dp => dp.drive.GetDuRoles());

            foreach (var result in results)
            {
                var (drive, project) = result.Source;

                var users = drive.GetDuUsers(project)
                    .FilterByWildcards(u => u?.displayName, wpName).ToList();

                foreach (var role in result
                    .Where(e => wp.IsMatch(e?.name))
                    .ExcludeByWildcards(e => e?.name!, wpRole)
                    .OrderBy(e => e?.name))
                {
                    // If this role is already assigned to all target users, don't show it.
                    // This also hides inherited roles, but that should be fine.
                    if (users.Count != 0 && users.All(u => u.roleAssignmentDtos?.Select(r => r.roleId).Contains(role.id) ?? false)) continue;

                    string tiphelp = role.GetPSPath();
                    yield return new CompletionResult(PathTools.EscapePSText(role.name), role.name, CompletionResultType.Text, tiphelp);
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        _csvLines ??= new Dictionary<((OrchDuDriveInfo drive, DuProject project), string type, string name), CsvLine>(new SecondAndThirdItemIgnoreCaseComparer<OrchDuDriveInfo, DuProject>());

        // The first element may have been input from CSV, so split the first element by commas
        Path = Path.Split1stValueByUnescapedCommas()?.ToArray();
        Type = Type.Split1stValueByUnescapedCommas()?.ToArray();
        Name = Name.Split1stValueByUnescapedCommas()?.ToArray();
        Roles = Roles.Split1stValueByUnescapedCommas()?.ToArray();

        var projects = SessionState.EnumDuFolders(Path, Recurse);
        var wpEntityType = Type.ConvertToWildcardPatternList();
        var specifiedTypes = DirectoryTypes.Items.SelectByWildcards(t => t, wpEntityType);

        foreach (var project in projects)
        {
            foreach (var specifiedType in specifiedTypes)
            {
                foreach (var name in Name!)
                {
                    if (!_csvLines.TryGetValue((project, specifiedType, name), out var line))
                    {
                        _csvLines[(project, specifiedType, name)] = new CsvLine(this);
                    }
                    else
                    {
                        line.Update(this);
                    }
                }
            }
        }
    }

    private static void AddUserToPayload(UserRoleAssignmentsCmd payload, DuProject project, string tenantKey, string userId, int securityPrincipalType, IEnumerable<DuRole> roles)
    {
        foreach (var role in roles)
        {
            DuRoleAssignment assign = new()
            {
                roleId = role.id,
                scope = $"/tenant/{tenantKey}/DocumentUnderstanding/projects/{project.id}",
                securityPrincipalId = userId,
                securityPrincipalType = securityPrincipalType
            };
            payload.roleAssignmentsToAdd!.Add(assign);
        }
    }

    protected override void EndProcessing()
    {
        // The implementation here is very similar to the Add-OrchPmGroupMember cmdlet; refer to it as needed.

        if (_csvLines is null) return;

        // Bulk-query the directory for all names to add.
        // Need to group by type for querying.
        // Robots cannot be queried in bulk, so they are handled separately.
        // This is nearly identical to the Add-OrchPmGroupMember implementation.. It would be nice to share the code.

        using var cancelHandler = new ConsoleCancelHandler();

        foreach (var lines in _csvLines.GroupBy(line => (line.Key.Item1.drive, line.Key.type)).WithCancellation(cancelHandler.Token))
        {
            var (drive, type) = lines.Key;

            Dictionary<string, PmGroupMember?>? entries = null;
            switch (type)
            {
                case "DirectoryUser":
                    entries = drive.ParentDrive.PmBulkResolveByName("user", lines, line => line.Key.name);
                    break;
                case "DirectoryGroup":
                    entries = drive.ParentDrive.PmBulkResolveByName("group", lines, line => line.Key.name);
                    break;
                case "DirectoryApplication":
                    entries = drive.ParentDrive.PmBulkResolveByName("application", lines, line => line.Key.name);
                    break;
                case "DirectoryRobotUser":
                    foreach (var line in lines)
                    {
                        var addingMember = drive.ParentDrive.SearchPmDirectoryCache.Get(line.Key.name.ToLower())?
                            .Where(t => t.objectType == type)
                            .FirstOrDefault(t => string.Compare(t.identityName, line.Key.name, StringComparison.OrdinalIgnoreCase) == 0);

                        // Show a warning if no match is found
                        if (addingMember is null)
                        {
                            WriteWarning($"\"{drive.NameColonSeparator}\": \"{line.Key.name}\" ({type}) not found. Ignoring.");
                        }
                    }
                    break;
            }

            foreach (var entry in entries ?? [])
            {
                if (entry.Value is null)
                {
                    WriteWarning($"\"{drive.NameColonSeparator}\": \"{entry.Key}\" ({type}) not found. Ignoring.");
                }
            }
        }

        // Now the actual processing begins.
        // Add members to the project.
        // Group by drive and aggregate all users to add to this project into the payload
        UserRoleAssignmentsCmd payload = new()
        {
            roleAssignmentsToAdd = [],
            roleAssignmentsToDelete = []
        };

        HashSet<DuProject> updatedProjects = []; // Used at the end to clear the cache
        foreach (var lines in _csvLines.GroupBy(line => line.Key.Item1.drive))
        {
            var drive = lines.Key;
            var (_, tenantKey) = drive.ParentDrive.GetTenantId();

            foreach (var line in lines.WithCancellation(cancelHandler.Token))
            {
                var project = line.Key.Item1.Item2;
                var type = line.Key.type;
                var name = line.Key.name;
                var wpRoles = line.Value.Roles.ConvertToWildcardPatternList();

                var rolesAvailable = drive.GetDuRoles();
                if (rolesAvailable is null || rolesAvailable.Length == 0) continue;

                PmGroupMember? entry = null;
                switch (type)
                {
                    case "DirectoryRobotUser":
                        var robotEntry = drive.ParentDrive.SearchPmDirectoryCache.Get(name.ToLower())?
                            .Where(t => string.Compare(t.objectType, "DirectoryRobotUser", StringComparison.OrdinalIgnoreCase) == 0)
                            .FirstOrDefault(t => string.Compare(t.identityName, name, StringComparison.OrdinalIgnoreCase) == 0);
                        if (robotEntry is null) continue;
                        if (ShouldProcess($"Robot: {robotEntry.identityName} Project: {project.GetPSPath()}", "Add DuUser"))
                        {
                            AddUserToPayload(payload, project, tenantKey!, robotEntry.identifier!, 0,
                                rolesAvailable.FilterByWildcards(r => r?.name, wpRoles));
                            updatedProjects.Add(project);
                        }
                        break;
                    case "DirectoryUser":
                        entry = drive.ParentDrive.PmBulkResolveByName("user", [name], n => n).FirstOrDefault().Value;
                        if (entry is null) continue;
                        if (ShouldProcess($"User: {entry.name} Project: {project.GetPSPath()}", "Add DuUser"))
                        {
                            AddUserToPayload(payload, project, tenantKey!, entry.identifier!, 0,
                                rolesAvailable.FilterByWildcards(r => r?.name, wpRoles));
                            updatedProjects.Add(project);
                        }
                        break;
                    case "DirectoryGroup":
                        entry = drive.ParentDrive.PmBulkResolveByName("group", [name], n => n).FirstOrDefault().Value;
                        if (entry is null) continue;
                        if (ShouldProcess($"Group: {entry.name} Project: {project.GetPSPath()}", "Add DuUser"))
                        {
                            AddUserToPayload(payload, project, tenantKey!, entry.identifier!, 1,
                                rolesAvailable.FilterByWildcards(r => r?.name, wpRoles));
                            updatedProjects.Add(project);
                        }
                        break;
                    case "DirectoryApplication":
                        entry = drive.ParentDrive.PmBulkResolveByName("application", [name], n => n).FirstOrDefault().Value;
                        if (entry is null) continue;
                        if (ShouldProcess($"Application: {entry.name} Project: {project.GetPSPath()}", "Add DuUser"))
                        {
                            AddUserToPayload(payload, project, tenantKey!, entry.identifier!, 2,
                                rolesAvailable.FilterByWildcards(r => r?.name, wpRoles));
                            updatedProjects.Add(project);
                        }
                        break;
                }
            }

            if (payload.roleAssignmentsToAdd.Count == 0) continue;

            var projects = drive.GetDuProjects();

            try
            {
                var partitionGlobalId = drive.ParentDrive.GetPartitionGlobalId();

                drive.ParentDrive.OrchAPISession.SetDuRoleToDuUser(partitionGlobalId, payload);

                foreach (var project in updatedProjects)
                {
                    drive.DuUsers.ClearCache((tenantKey!, project.id!));
                }
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "AddDuRoleToDuUserError", ErrorCategory.InvalidOperation, drive));
            }
        }
    }
}
