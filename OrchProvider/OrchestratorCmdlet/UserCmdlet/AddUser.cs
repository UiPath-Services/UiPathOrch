using System.Collections;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using UiPath.PowerShell.Positional;
using Positional = UiPath.PowerShell.Positional.Type_UserName_Roles;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Add, "OrchUser", SupportsShouldProcess = true)]
    [OutputType(typeof(Entities.User))]
    public class AddUserCommand : OrchestratorPSCmdlet
    {
        Dictionary<(OrchDriveInfo drive, DirectoryObject user), HashSet<Role>>? _params = null;

        // Key: DirectoryObject.type;  Value: エンティティ内の Type
        private static readonly Dictionary<int, string> Types = new() {
            { 0, "DirectoryUser" },
            { 1, "DirectoryGroup" },
            { 3, "DirectoryRobot" },
            { 4, "DirectoryExternalApplication" }
        };

        [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
        [SupportsWildcards]
        [ArgumentCompleter(typeof(TypeCompleter))]
        public string[]? Type { get; set; }

        [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(UserNameCompleter))]
        public string[]? UserName { get; set; }

        [Parameter(Position = 2, ValueFromPipelineByPropertyName = true)]
        [Alias("TenantRoles")]
        [ArgumentCompleter(typeof(RolesCompleter))]
        public string[]? Roles { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(DriveCompleter<Type_UserName_Roles>))]
        public string[]? Path { get; set; }

        private class TypeCompleter : OrchArgumentCompleter
        {
            public override IEnumerable<CompletionResult> CompleteArgument(
                string commandName,
                string parameterName,
                string wordToComplete,
                CommandAst commandAst,
                IDictionary fakeBoundParameters)
            {
                var wpType = CreateWPListFromParameter(commandAst, "Type", Type_UserName_Roles.Parameters, wordToComplete);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                foreach (var t in Types.Values
                    .Where(t => wp.IsMatch(t))
                    .ExcludeByWildcards(t => t, wpType))
                {
                    yield return new CompletionResult(t);
                }
            }
        }

        private class UserNameCompleter : OrchArgumentCompleter
        {
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

                var drives = ResolveDrives(fakeBoundParameters);

                // パラメータで選択済みのユーザー名は、候補から除外する
                var wpUserName = CreateWPListFromParameter(commandAst, "UserName", Type_UserName_Roles.Parameters, wordToComplete);
                var wpType = CreateWPListFromOtherParameters(commandAst, "Type", Type_UserName_Roles.Parameters);

                var specifiedTypes = Types.SelectByWildcards(t => t.Value, wpType).Select(t => t.Key);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                bool bFound = false;
                foreach (var drive in drives)
                {
                    var existingTenantUser = drive.GetUsers();
                    var users = drive.SearchForUsersAndGroups(wordToComplete);
                    if (users == null) continue;

                    foreach (var user in users
                        .FilterByStructValues(u => u.type ?? -1, specifiedTypes)
                        .ExcludeByClassValues(u => u?.identityName?.ToLower(), existingTenantUser.Select(u => u.UserName?.ToLower()))
                        .ExcludeByWildcards(e => e?.identityName, wpUserName)
                        .OrderBy(e => e.identityName))
                    {
                        bFound = true;
                        string tiphelp = TipHelp(user);
                        yield return new CompletionResult(PathTools.EscapePSText(user?.identityName), user?.identityName, CompletionResultType.Text, tiphelp);
                    }
                }
                if (!bFound)
                {
                    yield return new CompletionResult($"\"No results matching '{wordToComplete}'\".");
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
                var drives = ResolveDrives(fakeBoundParameters);

                var wpUserName = CreateWPListFromOtherParameters(commandAst, "UserName", Type_UserName_Roles.Parameters);
                var wpRoles = CreateWPListFromParameter(commandAst, parameterName, Type_UserName_Roles.Parameters, wordToComplete);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drives, drive => drive.GetRoles());

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;

                    var drive = result.Source;

                    foreach (var role in entities!
                        .Where(r => r.Type != "Folder")
                        .ExcludeByWildcards(r => r?.Name, wpRoles)
                        .OrderBy(r => r.Name))
                    {
                        string tiphelp = TipHelp(role);
                        var ret = new CompletionResult(PathTools.EscapePSText(role.Name), role.Name, CompletionResultType.ParameterValue, tiphelp);
                        yield return ret;
                    }
                }
            }
        }

        protected override void ProcessRecord()
        {
            // 先頭の要素は CSV から入力されている可能性があるので、先頭の要素についてはカンマで区切る
            Roles = Roles.Split1stValueByUnescapedCommas()?.ToArray();

            _params ??= [];

            var wpType = Type.ConvertToWildcardPatternList();
            var specifiedTypes = Types.SelectByWildcards(t => t.Value, wpType).Select(t => t.Key);

            var wpRoles = Roles.ConvertToWildcardPatternList();

            var drives = OrchDriveInfo.EnumOrchDrives(Path);

            // 指定されたドライブに対して、既存のユーザーをキャッシュする
            // と思ったけど、どうせ初回にキャッシュされるし、毎回スレッドを起こす方が無駄かな。。
            //ParallelResults.ForEach(drives, drive => drive.GetUsers());

            // Roles が指定されていれば、あらかじめキャッシュする
            // と思ったけど、どうせ初回にキャッシュされるし、毎回スレッドを起こす方が無駄かな。。
            //if (Roles != null && !Roles.All(r => !string.IsNullOrEmpty(r)))
            //{
            //    ParallelResults.ForEach(drives, drive => drive.GetRoles());
            //}

            HashSet<(OrchDriveInfo, string)> warnedUsers = [];
            foreach (var drive in drives)
            {
                ICollection<User> existingUsers;
                try
                {
                    existingUsers = drive.GetUsers();
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetUserError", ErrorCategory.InvalidOperation, drive));
                    continue;
                }

                HashSet<Entities.Role> targetRoles = null;
                if (Roles != null && Roles.Any(r => !string.IsNullOrEmpty(r)))
                {
                    try
                    {
                        targetRoles = drive.GetRoles()
                            .Where(r => r.Type != "Folder")
                            .SelectByWildcards(r => r?.Name, wpRoles)
                            .ToHashSet();
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetRoleError", ErrorCategory.InvalidOperation, drive));
                        continue;
                    }
                }

                foreach (var identityName in UserName!)
                {
                    DirectoryObject[]? users;
                    try
                    {
                        users = drive.SearchForUsersAndGroups(identityName);
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "SearchForUsersAndGroupsError", ErrorCategory.InvalidOperation, drive));
                        continue;
                    }

                    var targetUsers = users?
                        .Where(u => string.Compare(u.identityName, identityName, StringComparison.OrdinalIgnoreCase) == 0)
                        .FilterByStructValues(u => u.type ?? -1, specifiedTypes);

                    if (targetUsers == null || !targetUsers.Any())
                    {
                        if (warnedUsers.Add((drive, identityName)))
                        {
                            WriteWarning($"No match found for user '{System.IO.Path.Combine(drive.NameColonSeparator, identityName)}' ({string.Join(", ", Type ?? [])}).");
                        }
                        continue;
                    }

                    foreach (var targetUser in targetUsers)
                    {
                        var existingUser = existingUsers.FirstOrDefault(u => u.DirectoryIdentifier == targetUser.identifier);
                        if (existingUser != null)
                        {
                            if (warnedUsers.Add((drive, identityName)))
                            {
                                WriteWarning($"The user '{System.IO.Path.Combine(drive.NameColonSeparator, identityName)}' ({string.Join(", ", Type ?? [])}) already exists.");
                            }
                            continue;
                        }

                        if (!_params.TryGetValue((drive, targetUser), out var rolesToAdd))
                        {
                            _params[(drive, targetUser)] = targetRoles ?? [];
                        }
                        else
                        {
                            foreach (var role in targetRoles ?? [])
                            {
                                rolesToAdd.Add(role);
                            }
                        }
                    }
                }
            }
        }

        protected override void EndProcessing()
        {
            var drives = OrchDriveInfo.EnumOrchDrives(Path);

            var wpType = Type.ConvertToWildcardPatternList();
            var specifiedTypes = Types.SelectByWildcards(t => t.Value, wpType).Select(t => t.Key);

            var wpRoles = Roles.ConvertToWildcardPatternList();

            using var cancelHandler = new ConsoleCancelHandler();

            foreach (var p in _params?
                .OrderBy(p => p.Key.drive.NameColon)
                .ThenBy(p => p.Key.user.identityName).ToList() ?? [])
            {
                var (drive, user) = p.Key;
                var roles = p.Value;

                string target = System.IO.Path.Combine(drive.NameColonSeparator, user.identityName ?? user.identifier ?? "");
                if (!string.IsNullOrEmpty(user.displayName))
                    target += $" ({user.displayName})";
                string target2 = roles?.Count != 0 ? $"{target} with {string.Join(", ", roles!.Select(r => $"'{r.Name}'"))}" : target;

                if (ShouldProcess(target2, $"Add {Types[user.type ?? 0]}"))
                {
                    User postingUser = new()
                    {
                        DirectoryIdentifier = user.identifier,
                        Domain = "autogen",
                        RolesList = roles?.Select(r => r.Name!).ToArray(),
                        Type = Types[user.type ?? 0],
                        NotificationSubscription = new()
                        {
                            Queues = true,
                            QueueItems = true,
                            Robots = true,
                            Jobs = true,
                            Tasks = true,
                            Schedules = true,
                            Insights = true,
                            CloudRobots = true,
                            Export = true,
                            RateLimitsDaily = true,
                            RateLimitsRealTime = false
                        },
                        MayHaveRobotSession = false,
                        MayHaveUnattendedSession = false,
                        MayHavePersonalWorkspace = false,
                        IsExternalLicensed = false,
                        RestrictToPersonalWorkspace = false,
                        UpdatePolicy = new()
                        {
                            Type = "None"
                        }
                    };

                    if (user.type == 3) // robot の場合
                    {
                        postingUser.MayHaveUserSession = false; // prohibit 'Standard Interface'
                        postingUser.MayHaveUnattendedSession = true;
                        postingUser.UnattendedRobot = new()
                        {
                            CredentialType = "NoCredential",
                            LimitConcurrentExecution = false,
                        };
                    }
                    else if (user.type == 4) // application の場合
                    {
                        postingUser.MayHaveUserSession = false; // prohibit 'Standard Interface'
                    }

                    try
                    {
                        drive.OrchAPISession.PostUser(postingUser);
                        drive._dicUsers = null;
                        drive._dicUsersDetailed = null;
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(target, ex), "AddUserError", ErrorCategory.InvalidOperation, drive));
                    }

                }



                //foreach (var drive in drives)
                //{
                //    List<Entities.Role> roles;
                //    try
                //    {
                //        roles = drive.GetRoles()
                //            .Where(r => r.Type != "Folder")
                //            .SelectByWildcards(r => r?.Name, wpRoles)
                //            .ToList();
                //    }
                //    catch (Exception ex)
                //    {
                //        WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetRoleError", ErrorCategory.InvalidOperation, drive));
                //        continue;
                //    }

                //    var strRolesToAdd = string.Join(", ", roles.Select(r => "'" + r.Name + "'"));

                //    foreach (var identityName in UserName!)
                //    {
                //        cancelHandler.Token.ThrowIfCancellationRequested();

                //        DirectoryObject[]? users;
                //        try
                //        {
                //            users = drive.SearchForUsersAndGroups(identityName);
                //        }
                //        catch (Exception ex)
                //        {
                //            WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "SearchForUsersAndGroupsError", ErrorCategory.InvalidOperation, drive));
                //            continue;
                //        }

                //        var targetUsers = users?
                //            .Where(u => string.Compare(u.identityName, identityName, StringComparison.OrdinalIgnoreCase) == 0)
                //            .FilterByStructValues(u => u.type ?? -1, specifiedTypes);

                //        if (WarnOnNoMatch.IsPresent && (targetUsers == null || !targetUsers.Any()))
                //        {
                //            WriteWarning($"No match found for UserName '{System.IO.Path.Combine(drive.NameColonSeparator, identityName)}' ({string.Join(", ", Type ?? [])}).");
                //            continue;
                //        }

                //        // この時点で、targetUsers は一人だけになっているはず
                //        foreach (var user in targetUsers ?? [])
                //        {
                //            cancelHandler.Token.ThrowIfCancellationRequested();

                //            string target = System.IO.Path.Combine(drive.NameColonSeparator, user.identityName ?? user.identifier ?? "");
                //            if (!string.IsNullOrEmpty(user.displayName))
                //                target += $" ({user.displayName})";
                //            string target2 = roles?.Count != 0 ? $"{target} with {strRolesToAdd}" : target;

                //            if (ShouldProcess(target2, $"Add {Types[user.type ?? 0]}"))
                //            {
                //                User postingUser = new()
                //                {
                //                    DirectoryIdentifier = user.identifier,
                //                    Domain = "autogen",
                //                    RolesList = roles?.Select(r => r.Name!).ToArray(),
                //                    Type = Types[user.type ?? 0],
                //                    NotificationSubscription = new()
                //                    {
                //                        Queues = true,
                //                        QueueItems = true,
                //                        Robots = true,
                //                        Jobs = true,
                //                        Tasks = true,
                //                        Schedules = true,
                //                        Insights = true,
                //                        CloudRobots = true,
                //                        Export = true,
                //                        RateLimitsDaily = true,
                //                        RateLimitsRealTime = false
                //                    },
                //                    MayHaveRobotSession = false,
                //                    MayHaveUnattendedSession = false,
                //                    MayHavePersonalWorkspace = false,
                //                    IsExternalLicensed = false,
                //                    RestrictToPersonalWorkspace = false,
                //                    UpdatePolicy = new()
                //                    {
                //                        Type = "None"
                //                    }
                //                };

                //                if (user.type == 3) // robot の場合
                //                {
                //                    postingUser.MayHaveUserSession = false; // prohibit 'Standard Interface'
                //                    postingUser.MayHaveUnattendedSession = true;
                //                    postingUser.UnattendedRobot = new()
                //                    {
                //                        CredentialType = "NoCredential",
                //                        LimitConcurrentExecution = false,
                //                    };
                //                }
                //                else if (user.type == 4) // application の場合
                //                {
                //                    postingUser.MayHaveUserSession = false; // prohibit 'Standard Interface'
                //                }

                //                try
                //                {
                //                    drive.OrchAPISession.PostUser(postingUser);
                //                    drive._dicUsers = null;
                //                }
                //                catch (Exception ex)
                //                {
                //                    WriteError(new ErrorRecord(new OrchException(target, ex), "AddUserError", ErrorCategory.InvalidOperation, drive));
                //                }
                //            }
                //        }
                //    }
            }
        }
    }
}
