using System.Collections;
using UiPath.PowerShell.Positional;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Add, "OrchFolderUser", SupportsShouldProcess = true)]
public class AddFolderUserCommand : OrchestratorPSCmdlet
{
    List<(string type, string userName, string[] roles, OrchDriveInfo drive, Folder folder)>? parameters = null;

    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(KeyOfDictionaryCompleter<DirectoryTypeItems, int>))]
    [ValidateDictionaryKey<DirectoryTypeItems, int>]
    public string? Type { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(UserNameCompleter))]
    public string[]? UserName { get; set; }

    [Parameter(Position = 2, ValueFromPipelineByPropertyName = true)]
    [Alias("FolderRoles")]
    [ArgumentCompleter(typeof(RolesCompleter))]
    [SupportsWildcards]
    public string[]? Roles { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    private class UserNameCompleter : OrchArgumentCompleter
    {
        //  0: User, 1: Group, 2: Machine, 3: Robot, 4: ExternalApplication

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

            var paramType = GetFakeBoundParameter(fakeBoundParameters, "Type");
            if (!DirectoryTypeItems.Items.TryGetValue(paramType ?? "", out var objectType))
            {
                //yield return new CompletionResult(PathTools.EscapePSText("Invalid Type."));
                yield break;
            }

            var drives = ResolveOrchDrives(fakeBoundParameters);

            // Excluding users already assigned to the folder from candidates is not implemented for now.
            //var existingMemberIds = GetExistingMemberIds(drives, wpName);
            // Fetch assigned users to exclude already assigned users
            //ParallelResults3.GroupBy(drivesFolders, df => df.drive.GetUsersForFolder(df.folder, false));

            var paramUserName = GetSelfExclusionValues(commandAst, parameterName, wordToComplete);

            wordToComplete = RemoveEnclosingQuotes(wordToComplete);

            bool bFound = false;
            foreach (var drive in drives)
            {
                string partitionGlobalId = drive.GetPartitionGlobalId();
                var ret = drive.SearchDirectory(wordToComplete);
                if (ret is null) continue;

                foreach (var e in ret
                    .Where(e => e.type == objectType)
                    //.ExcludeByTexts(e => e.identityName!, assignedUsers?.Select(u => u.Id.ToString()!) ?? [])
                    .ExcludeByClassValues(e => e?.identityName, paramUserName)
                    .OrderBy(e => e.identityName))
                {
                    bFound = true;
                    string tiphelp = e.identityName;
                    yield return new CompletionResult(PathTools.EscapePSText(e?.identityName), e?.identityName, CompletionResultType.Text, tiphelp);
                }
            }
            if (!bFound)
            {
                yield return new CompletionResult($@"""(No users found for '{RemoveEnclosingQuotes(wordToComplete)}')""");
            }
        }
    }

    private class RolesCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drives = ResolveOrchDrives(fakeBoundParameters);

            // Exclude Roles already selected via parameter from the candidates
            var wpRoles = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drives, drive => drive.Roles.Get());

            foreach (var result in results)
            {
                foreach (var role in result
                    .Where(role => role.Type != "Tenant")
                    .Where(role => wp.IsMatch(role.Name))
                    .ExcludeByWildcards(role => role?.Name, wpRoles)
                    .OrderBy(role => role.Type)
                    .ThenBy(role => role.Name))
                {
                    string tiphelp = role.GetPSPath();
                    if (!string.IsNullOrEmpty(role.Type))
                    {
                        tiphelp += $" ({role.Type})";
                    }
                    yield return new CompletionResult(PathTools.EscapePSText(role.Name), role.Name, CompletionResultType.ParameterValue, tiphelp);
                }
            }
        }
    }

    // Read all CSV file entries first before processing, to query UserNames in bulk
    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFoldersWithoutPersonalWorkspace(Path, Recurse.IsPresent, Depth);

        parameters ??= [];
        foreach (var userName in UserName!)
        {
            foreach (var (drive, folder) in drivesFolders)
            {
                parameters.Add((
                    Type,
                    userName,
                    Roles?.Split1stValueByUnescapedCommas()?.ToArray(),
                    drive, folder)!
                );
            }
        }
    }

    private static string ConvertToKind(string type)
    {
        return type switch
        {
            "DirectoryUser" => "User",
            "DirectoryGroup" => "Group",
            "DirectoryExternalApplication" => "Application",
            _ => type
        };
    }

    protected override void EndProcessing()
    {
        if (parameters is null) return;

        // Group by drive and Type, and query user names in bulk.
        // Considering actual use cases, querying in bulk is often more efficient.
        // However, if -Confirm is used and processing is aborted midway, unnecessary queries will occur...
        foreach (var param in parameters
            .GroupBy(p => (p.drive, p.type))
            .OrderBy(g => g.Key.drive.Name))
        {
            var (drive, type) = param.Key;

            var kind = ConvertToKind(type);

            var groupsByType = param.GroupBy(g => g.type);
            foreach (var groupByType in groupsByType)
            {
                // Robots cannot be searched via PmBulkResolveByName!
                // Since Robots cannot be queried in bulk, it's better to search just before registration.
                if (type == "DirectoryRobot") continue;

                var userNames = param.Select(p => p.userName);
                try
                {
                    // Results are cached, so no need to receive them here.
                    // result is for debugging purposes.
                    var result = drive.PmBulkResolveByName(kind, userNames, u => u);
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, "Failed to search directory", ex), "SearchDirectoryError", ErrorCategory.InvalidOperation, drive));
                    continue;
                }
            }
        }

        // Group by folder and add users one by one
        foreach (var param in parameters
            .GroupBy(p => (p.drive, p.folder))
            .OrderBy(g => g.Key.drive.Name)
            .ThenBy(g => g.Key.folder.FullyQualifiedNameOrderable))
        {
            var (drive, folder) = param.Key;

            foreach (var groupByFolder in param)
            {
                var (type, userName, roles, _, _) = groupByFolder;

                string foundUserName = null;
                string foundUserDisplayName = null;
                string foundUserIdentifier = null;

                #region Search for user from cache
                if (type == "DirectoryRobot")
                {
                    // Search for Robot here
                    DirectoryObject? member = null;
                    try
                    {
                        // 3 refers to Robot. See DirectoryTypeItems
                        member = ResolveDirectoryName(this, drive, userName, 3);
                    }
                    catch (Exception ex)
                    {
                        string t = drive.NameColonSeparator + userName;
                        WriteError(new ErrorRecord(new OrchException(t, ex), "ResolveDirectoryNameError", ErrorCategory.InvalidOperation, folder));
                        continue;
                    }
                    if (member is null) continue;
                    foundUserName = member.identityName;
                    foundUserDisplayName = member.displayName;
                    foundUserIdentifier = member.identifier;
                }
                else
                {
                    // For non-Robot types, retrieve from cache.
                    // No API calls should occur here, so no need to output exceptions.
                    try
                    {
                        var kind = ConvertToKind(type);
                        var kv = drive.PmBulkResolveByName(kind!, [userName], u => u).First();
                        if (kv.Value is null)
                        {
                            WriteWarning($"'{folder.GetPSPath()}': {type} '{kv.Key}' was not found.");
                            continue;
                        }

                        foundUserIdentifier = kv.Value.identifier;
                        if (!string.IsNullOrEmpty(kv.Value.email))
                        {
                            foundUserName = kv.Value.email;
                            foundUserDisplayName = kv.Value.displayName;
                        }
                        else
                        {
                            foundUserName = kv.Value.displayName;
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
                #endregion

                string target = $"{foundUserName}";
                if (!string.IsNullOrEmpty(foundUserDisplayName))
                {
                    target += $" ({foundUserDisplayName})";
                }
                target += $" to '{folder.GetPSPath()}'";

                if (ShouldProcess(target, $"Add {type} to Folder"))
                {
                    #region Search for roles
                    IEnumerable<Role> existingRoles = null;
                    if (roles?.Length > 0)
                    {
                        try
                        {
                            existingRoles = drive.Roles.Get().Where(r => r.Type != "Tenant");
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, "Failed to get roles.", ex), "GetRolesError", ErrorCategory.InvalidOperation, drive));
                            continue;
                        }
                    }

                    // Warn if any specified Roles patterns don't match existing roles.
                    // There's some slightly redundant processing, but that's fine...
                    foreach (var role in roles ?? [])
                    {
                        var wpRole = new WildcardPattern(role, WildcardOptions.IgnoreCase);
                        if (existingRoles is null || !existingRoles.Any(r => wpRole.IsMatch(r.Name)))
                        {
                            WriteWarning($"'{role}': No matching role found in {drive.NameColonSeparator}.");
                        }
                    }

                    // Search for roles
                    var wpRoles = roles.ConvertToWildcardPatternList();
                    var addingRoles = existingRoles?.SelectByWildcards(role => role?.Name, wpRoles);

                    #endregion

                    DomainUserAssignment assignment = new()
                    {
                        Domain = "autogen",
                        DirectoryIdentifier = foundUserIdentifier,
                        UserType = type,
                        RolesPerFolder =
                        [
                            new FolderRoles()
                            {
                                FolderId = folder.Id,
                                RoleIds = addingRoles?.Select(r => r.Id ?? 0).ToList()
                            }
                        ]
                    };

                    // Which one is the correct one to call?
                    // Both seem to work, though.
                    try
                    {
                        drive.OrchAPISession.AssignDomainUser(assignment); // The one documented in swagger
                        //drive.OrchAPISession.AssignDirectoryUser(assignment); // The one actually called by the web interface

                        drive.FolderUsersWithNoInherited.ClearCache();
                        drive.FolderUsersWithInherited.ClearCache();
                        drive.ClearFolderCache(folder);

                        Thread.Sleep(600); // Wait to avoid API call rate limit
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(target, ex), "AddFolderUserError", ErrorCategory.InvalidOperation, folder));
                    }
                }
            }
        }
    }
}
