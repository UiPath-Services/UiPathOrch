using System.Collections;
using System.Data;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;
using TPositional = UiPath.PowerShell.Positional.Type_UserName_Roles;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Add, "OrchFolderUser", SupportsShouldProcess = true)]
    public class AddFolderUserCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(KeyOfDictionaryCompleter<DirectoryTypeItems, int>))]
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

                var paramType = GetParameterValue(commandAst, "Type", TPositional.Parameters);
                if (!DirectoryTypeItems.Items.TryGetValue(paramType ?? "", out var objectType))
                {
                    //yield return new CompletionResult(PathTools.EscapePSText("Invalid Type."));
                    yield break;
                }

                var drives = ResolveDrives(fakeBoundParameters);

                // フォルダに割り当て済みのユーザーを候補から除外する処理は、いったん実装せずとした
                //var existingMemberIds = GetExistingMemberIds(drives, wpName);
                // アサイン済みのユーザーを除外するため、アサイン済みのユーザーを取得
                //ParallelResults.ForEach(drivesFolders, df => df.drive.GetUsersForFolder(df.folder, false));

                var paramUserName = GetParameterValues(commandAst, parameterName, TPositional.Parameters, wordToComplete);
                bool bFound = false;
                foreach (var drive in drives)
                {
                    string partitionGlobalId = drive.GetPartitionGlobalId();
                    var ret = drive.SearchForUsersAndGroups(wordToComplete);
                    if (ret == null) continue;

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

                // パラメータで選択済みの Roles は、候補から除外する
                var wpRoles = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drives, drive => drive.Roles.Get());

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;
                    foreach (var role in entities!
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

        // このメソッドはほかの cmdlet でも使うか？
        // そうであれば、OrchestratorPSCmdlet class に移動すべきだが、
        protected DirectoryObject? SearchDirectory(OrchDriveInfo drive, string name, int type)
        {
            string strType = type switch
            {
                0 => "users",
                1 => "groups",
                2 => "machines",
                3 => "robots",
                4 => "applications",
                _ => throw new InvalidOperationException()
            };

            var resolved = drive.SearchForUsersAndGroups(name)?.Where(g => g.type == type).ToList();

            if (resolved == null || resolved.Count == 0)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, $"No {strType} found for '{name}'."), "SearchForUsersAndGroupsError", ErrorCategory.InvalidOperation, drive));
                return null;
            }
            if (resolved.Count > 1)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, $"Duplicated {strType} found for '{name}'."), "SearchForUsersAndGroupsError", ErrorCategory.InvalidOperation, drive));
                return null;
            }
            DirectoryObject found = resolved.First();
            if (string.Compare(found.identityName, name, true) != 0)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, $"No {strType} found for '{name}'."), "SearchForUsersAndGroupsError", ErrorCategory.InvalidOperation, drive));
                return null;
            }
            return found;
        }

        protected override void ProcessRecord()
        {
            if (!DirectoryTypeItems.Items.TryGetValue(Type ?? "", out var objectType))
            {
                throw new Exception("Invalid Type.");
            }

            var drives = OrchDriveInfo.EnumOrchDrives(Path);
            var drivesFolders = OrchDriveInfo.EnumFoldersWithoutPersonalWorkspace(Path, Recurse.IsPresent, Depth);
            var wpUserName = UserName.ConvertToWildcardPatternList();

            // 先頭の要素は CSV から入力されている可能性があるので、先頭の要素についてはカンマで区切る
            // Roles はあとでまた使うので、区切り直した値を Roles にも入れておく
            Roles = Roles?.Split1stValueByUnescapedCommas()?.ToArray();
            var wpRoles = Roles.ConvertToWildcardPatternList();

            // これはしなくて良いか。。
            //ParallelResults.ForEach(drives, drive => drive.Roles.Get());

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var (drive, folder) in drivesFolders)
            {
                // 個人用ワークスペースには、ユーザーを割り当てできないが
                // EnumFoldersWithoutPersonalWorkspace() により除外されているので
                // ここで考慮する必要はない
                //if (folder.FolderType == "Personal") continue;

                IEnumerable<Role> existingRoles = null;
                if (Roles?.Length > 0)
                {
                    try
                    {
                        existingRoles = drive.Roles.Get().Where(r => r.Type != "Tenant");
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, "Failed to get roles", ex), "GetRolesError", ErrorCategory.InvalidOperation, drive));
                        continue;
                    }
                }

                // 指定された Roles に、既存のロールに合致しないパターンがあれば警告
                // 微妙に無駄な処理があるが、まあいいか。。
                foreach (var role in Roles ?? [])
                {
                    var wpRole = new WildcardPattern(role, WildcardOptions.IgnoreCase);
                    if (existingRoles == null || !existingRoles.Any(r => wpRole.IsMatch(r.Name)))
                    {
                        WriteWarning($"'{role}': No matching role found in {drive.NameColonSeparator}.");
                    }
                }

                // ロールを検索
                var addingRoles = existingRoles?.SelectByWildcards(role => role?.Name, wpRoles);

                // ディレクトリから、追加すべき名前を検索
                // bulk で検索すると、ユーザーに対して親切なメッセージを表示できないため
                // ひとりずつ検索する
                foreach (var userName in UserName!)
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();

                    DirectoryObject? member = null;
                    try
                    {
                        member = SearchDirectory(drive, userName, objectType);
                    }
                    catch (Exception ex)
                    {
                        string t = drive.NameColonSeparator + userName;
                        WriteError(new ErrorRecord(new OrchException(t, "Failed to search directory", ex), "SearchDirectoryError", ErrorCategory.InvalidOperation, drive));
                        continue;
                    }
                    if (member == null) continue;

                    // このフォルダに追加済みのユーザーは除外する処理は未実装

                    string target = $"{member.identityName}";
                    if (!string.IsNullOrEmpty(member.displayName))
                    {
                        target += $" ({member.displayName})";
                    }

                    if (addingRoles?.Any() ?? false)
                    {
                        var targetRoles = string.Join(", ", addingRoles.Select(role => "'" + role.DisplayName + "'"));
                        target += $" with {targetRoles}";
                    }
                    target += $" to {folder.GetPSPath()}";

                    if (ShouldProcess(target, $"Add {Type} to Folder"))
                    {
                        try
                        {
                            DomainUserAssignment assignment = new()
                            {
                                Domain = "autogen",
                                DirectoryIdentifier = member.identifier,
                                UserType = Type,
                                RolesPerFolder =
                                [
                                    new FolderRoles()
                                    {
                                        FolderId = folder.Id,
                                        RoleIds = addingRoles?.Select(r => r.Id ?? 0).ToList()
                                    }
                                ]
                            };

                            // 次は、どっちを呼び出すのが正しいのでしょうか。
                            // どちらを呼び出しても動作するようだけど。
                            drive.OrchAPISession.AssignDomainUser(assignment); // swagger doc に記載があるほう
                            //drive.OrchAPISession.AssignDirectoryUser(assignment); // web interface が実際に呼び出すほう

                            drive.FolderUsersWithNoInherited.ClearCache();
                            drive.FolderUsersWithInherited.ClearCache();
                            drive.ClearFolderCache(folder);

                            Thread.Sleep(600); // API call rate limit を回避するため待機する
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
}
