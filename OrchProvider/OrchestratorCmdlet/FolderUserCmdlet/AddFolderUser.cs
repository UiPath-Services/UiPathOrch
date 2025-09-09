using System.Collections;
using System.Data;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Reflection.Metadata;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;
using TPositional = UiPath.PowerShell.Positional.Type_UserName_Roles;

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

            var paramType = GetParameterValue(commandAst, "Type", TPositional.Parameters);
            if (!DirectoryTypeItems.Items.TryGetValue(paramType ?? "", out var objectType))
            {
                //yield return new CompletionResult(PathTools.EscapePSText("Invalid Type."));
                yield break;
            }

            var drives = ResolveOrchDrives(fakeBoundParameters);

            // フォルダに割り当て済みのユーザーを候補から除外する処理は、いったん実装せずとした
            //var existingMemberIds = GetExistingMemberIds(drives, wpName);
            // アサイン済みのユーザーを除外するため、アサイン済みのユーザーを取得
            //ParallelResults.ForEach(drivesFolders, df => df.drive.GetUsersForFolder(df.folder, false));

            var paramUserName = GetParameterValues(commandAst, parameterName, TPositional.Parameters, wordToComplete);

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

            // パラメータで選択済みの Roles は、候補から除外する
            var wpRoles = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults3.GroupBy(drives, drive => drive.Roles.Get());

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

    // UserName をバルクで問い合わせるため、CSV ファイルをすべて読み込んでから処理する
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

        // ドライブと Type でグループ化し、ユーザー名をバルクで問い合わせておく
        // 実際の use case を考慮すると、まとめて問い合わせてしまう方が効率的なことが多いだろう。
        // -Confirm を指定して、途中で処理をやめる場合などは、無駄な問い合わせが発生することになるが。。
        foreach (var param in parameters
            .GroupBy(p => (p.drive, p.type))
            .OrderBy(g => g.Key.drive.Name))
        {
            var (drive, type) = param.Key;

            var kind = ConvertToKind(type);

            var groupsByType = param.GroupBy(g => g.type);
            foreach (var groupByType in groupsByType)
            {
                // Robot は PmBulkResolveByName では検索できない！
                // Robot はバルクで問い合わせできないのだから、登録の直前に検索した方が良い。
                if (type == "DirectoryRobot") continue;

                var userNames = param.Select(p => p.userName);
                try
                {
                    // 結果はキャッシュされるので、ここで受け取る必要はない
                    // result はデバッグ用だね。
                    var result = drive.PmBulkResolveByName(kind, userNames, u => u);
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, "Failed to search directory", ex), "SearchDirectoryError", ErrorCategory.InvalidOperation, drive));
                    continue;
                }
            }
        }

        // フォルダでグループ化し、ユーザーをひとりずつ追加していく
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

                #region ユーザーをキャッシュから検索
                if (type == "DirectoryRobot")
                {
                    // Robot はここで検索
                    DirectoryObject? member = null;
                    try
                    {
                        // 3 はロボットを指す。DirectoryTypeItems を参照
                        member = ResolveDirectoryName(this, drive, userName, 3);
                    }
                    catch(Exception ex)
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
                    // Robot 以外はキャッシュから取得
                    // ここで API call は発生しないはずだから、例外は出力しなくて良いか。
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
                    #region ロールを検索
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

                    // 指定された Roles に、既存のロールに合致しないパターンがあれば警告
                    // 微妙に無駄な処理があるが、まあいいか。。
                    foreach (var role in roles ?? [])
                    {
                        var wpRole = new WildcardPattern(role, WildcardOptions.IgnoreCase);
                        if (existingRoles is null || !existingRoles.Any(r => wpRole.IsMatch(r.Name)))
                        {
                            WriteWarning($"'{role}': No matching role found in {drive.NameColonSeparator}.");
                        }
                    }

                    // ロールを検索
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

                    // 次は、どっちを呼び出すのが正しいのでしょうか。
                    // どちらを呼び出しても動作するようだけど。
                    try
                    {
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

#if false

        DirectoryTypeItems.Items.TryGetValue(Type ?? "", out var objectType);
        if (!DirectoryTypeItems.Items.TryGetValue(Type ?? "", out var objectType))
        {
            throw new Exception($"Invalid Type: {Type}. Allowed values are: {string.Join(", ", DirectoryTypeItems.Items.Select(item => item.Key)}");
        }

        var drives = SessionState.EnumOrchDrives(Path);
        var drivesFolders = SessionState.EnumFoldersWithoutPersonalWorkspace(Path, Recurse.IsPresent, Depth);

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
                if (existingRoles is null || !existingRoles.Any(r => wpRole.IsMatch(r.Name)))
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
                    member = ResolveDirectoryName(this, drive, userName, objectType);
                }
                catch (Exception ex)
                {
                    string t = drive.NameColonSeparator + userName;
                    WriteError(new ErrorRecord(new OrchException(t, "Failed to search directory", ex), "SearchDirectoryError", ErrorCategory.InvalidOperation, drive));
                    continue;
                }
                if (member is null) continue;

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
#endif
    }
}
