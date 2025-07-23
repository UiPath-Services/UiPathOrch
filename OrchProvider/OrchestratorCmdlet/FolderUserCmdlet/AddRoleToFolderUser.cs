using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;
using TPositional = UiPath.PowerShell.Positional.UserName_Roles;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Add, "OrchRoleToFolderUser", SupportsShouldProcess = true)]
public class AddRoleToFolderUserCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(UserNameCompleter))]
    public string[]? UserName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(FullNameCompleter))]
    [SupportsWildcards]
    public string[]? FullName { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [Alias("FolderRoles")]
    [ArgumentCompleter(typeof(RolesCompleter))]
    public string[]? Roles { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(KeyOfDictionaryCompleter<DirectoryTypeItems, int>))]
    [ValidateDictionaryKey(typeof(DirectoryTypeItems), AllowWildcard = true)]
    public string[]? Type { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    // TODO: FolderUserUserNameCompleter class を作成するのが吉か。
    private class UserNameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePathWithoutPersonalWorkspace(commandAst, fakeBoundParameters);

            // パラメータで選択された FullName のみ対象とする
            var wpFullName = CreateWPListFromOtherParameters(commandAst, "FullName", TPositional.Parameters);

            // パラメータで選択済みの UserName は、候補から除外する
            var wpUserName = CreateWPListFromParameter(commandAst, "UserName", TPositional.Parameters, wordToComplete);

            var wpType = CreateWPListFromOtherParameters(commandAst, "Type", TPositional.Parameters);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            // このフォルダに追加済みのユーザーのみ表示する
            var results = ParallelResults3.GroupBy(drivesFolders, df => df.drive.FolderUsersWithNoInherited.Get(df.folder));

            foreach (var result in results)
            {
                foreach (var userRoles in result
                    .Where(u => wp.IsMatch(u.UserEntity!.UserName!))
                    .FilterByWildcards(eu => eu?.UserEntity?.FullName, wpFullName)
                    .ExcludeByWildcards(eu => eu?.UserEntity?.UserName, wpUserName)
                    .FilterByWildcards(eu => eu?.UserEntity?.Type, wpType)
                    .OrderBy(u => u.UserEntity!.UserName))
                {
                    string tiphelp = TipHelp(userRoles);
                    var ret = new CompletionResult(PathTools.EscapePSText(userRoles.UserEntity!.UserName), userRoles.UserEntity.UserName, CompletionResultType.ParameterValue, tiphelp);
                    yield return ret;
                }
            }
        }
    }

    private class FullNameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePathWithoutPersonalWorkspace(commandAst, fakeBoundParameters);

            // パラメータで選択済みの FullName は、候補から除外する
            var wpFullName = CreateWPListFromParameter(commandAst, "FullName", TPositional.Parameters, wordToComplete);

            // パラメータで選択された UserName のみ対象とする
            var wpUserName = CreateWPListFromOtherParameters(commandAst, "UserName", TPositional.Parameters);

            var wpType = CreateWPListFromOtherParameters(commandAst, "Type", TPositional.Parameters);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            // このフォルダに追加済みのユーザーのみ表示する
            var results = ParallelResults3.GroupBy(drivesFolders, df => df.drive.FolderUsersWithNoInherited.Get(df.folder));

            foreach (var result in results)
            {
                foreach (var userRoles in result
                    .Where(u => wp.IsMatch(u.UserEntity!.FullName!))
                    .ExcludeByWildcards(eu => eu?.UserEntity?.FullName, wpFullName)
                    .FilterByWildcards(eu => eu?.UserEntity?.UserName, wpUserName)
                    .FilterByWildcards(eu => eu?.UserEntity?.Type, wpType)
                    .OrderBy(u => u.UserEntity!.FullName))
                {
                    string tiphelp = TipHelp(userRoles);
                    var ret = new CompletionResult(PathTools.EscapePSText(userRoles.UserEntity!.FullName), userRoles.UserEntity.FullName, CompletionResultType.ParameterValue, tiphelp);
                    yield return ret;
                }
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
            var drivesFolders = ResolvePathWithoutPersonalWorkspace(commandAst, fakeBoundParameters);
            var drives = ResolveOrchDrives(fakeBoundParameters);

            // 操作対象の User を抽出する

            var wpFullName = CreateWPListFromOtherParameters(commandAst, "FullName", TPositional.Parameters);
            var wpUserName = CreateWPListFromOtherParameters(commandAst, "UserName", TPositional.Parameters);
            var wpRoles = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

            var wpType = CreateWPListFromOtherParameters(commandAst, "Type", TPositional.Parameters);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            ParallelResults3.GroupBy(drives, drive => drive.Roles.Get());

            ParallelResults3.GroupBy(drivesFolders, df => df.drive.FolderUsersWithNoInherited.Get(df.folder));

            foreach (var (drive, folder) in drivesFolders)
            {
                // このフォルダーで利用可能なロールの一覧
                var tenantRoles = drive.Roles.Get().Where(tr => tr.Type != "Tenant").ToList();

                // このフォルダーに割り当て済みのユーザー一覧
                var folderUsers = drive.FolderUsersWithNoInherited.Get(folder);

                if (folderUsers.Count != 0)
                {
                    // パラメータで指定された、割り当て済みのユーザーを取り出す
                    var folderUsersFiltered = folderUsers
                        .FilterByWildcards(u => u?.UserEntity?.FullName, wpFullName)
                        .FilterByWildcards(u => u?.UserEntity?.UserName, wpUserName)
                        .FilterByWildcards(u => u?.UserEntity?.Type, wpType);

                    List<Role> notAssignedRoles = new();
                    foreach (var folderUser in folderUsersFiltered)
                    {
                        var assignedRoles = folderUser.Roles!.Select(r => r.Name);
                        notAssignedRoles.AddRange(tenantRoles?.ExcludeByClassValues(tr => tr?.DisplayName, assignedRoles) ?? []);
                    }

                    foreach (var role in notAssignedRoles
                        .Where(tr => wp.IsMatch(tr.DisplayName))
                        .ExcludeByWildcards(tr => tr?.DisplayName, wpRoles)
                        .OrderBy(r => r.DisplayName))
                    {
                        string tiphelp = $"{role.GetPSPath()} ({role.Type})";
                        yield return new CompletionResult(PathTools.EscapePSText(role.DisplayName), role.DisplayName, CompletionResultType.ParameterValue, tiphelp);
                    }
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        if (UserName?.Length == 0 || string.IsNullOrEmpty(UserName?[0])) UserName = null;
        if (FullName?.Length == 0 || string.IsNullOrEmpty(FullName?[0])) FullName = null;

        if (UserName is null && FullName is null)
        {
            WriteError(new ErrorRecord(new ArgumentException("Please make sure to specify either -UserName or -FullName."), "AddRoleToFolderUserError", ErrorCategory.InvalidOperation, this));
            return;
        }

        var drives = SessionState.EnumOrchDrives(Path);
        var drivesFolders = SessionState.EnumFoldersWithoutPersonalWorkspace(Path, Recurse.IsPresent, Depth);

        var wpFullName = FullName.ConvertToWildcardPatternList();
        var wpUserName = UserName.ConvertToWildcardPatternList();
        var wpType = Type.ConvertToWildcardPatternList();

        // 先頭の要素は CSV から入力されている可能性があるので、先頭の要素についてはカンマで区切る
        //if (Roles is not null && Roles.Length > 0) Roles = Roles[0].Split(',').Concat(Roles.Skip(1)).ToArray();
        var wpRoles = Roles.Split1stValueByUnescapedCommas().ConvertToWildcardPatternList();

        // 対象のドライブの roles を、非同期にまとめて取得する
        // ParallelResults.ForEach(drives, drive => drive.Roles.Get());

        using var results = OrchThreadPool.RunForEach(drivesFolders,
            df => df.folder.GetPSPath(),
            df => df.folder,
            df => df.drive.FolderUsersWithNoInherited.Get(df.folder));

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var existingUsers = result.GetResult(cancelHandler.Token);
                var (drive, folder) = result.Source;

                var tenantRoles = drive.Roles.Get()
                    .Where(role => role.Type != "Tenant")
                    .FilterByWildcards(role => role?.Name, wpRoles);

                // 編集対象のユーザーを抽出する
                List<UserRoles> editingUsers = existingUsers!
                    .FilterByWildcards(eu => eu?.UserEntity?.FullName, wpFullName)
                    .FilterByWildcards(eu => eu?.UserEntity?.UserName, wpUserName)
                    .FilterByWildcards(eu => eu?.UserEntity?.Type, wpType)
                    .ToList();

                foreach (var user in editingUsers.OrderBy(user => user.UserEntity!.FullName))
                {
                    IEnumerable<SimpleRole> existingRoles = user.Roles;

                    // 追加するロールを抽出(追加済みのロールは除去)
                    IEnumerable<Role> addingRoles = tenantRoles
                        .ExcludeByStructValues(role => role.Id ?? 0, existingRoles!.Select(role => role.Id ?? 0));
                    if (!addingRoles.Any()) continue;
                    var targetRoles = string.Join(", ", addingRoles.Select(r => r.Name).Order());

                    // 追加するロールを抽出(追加済みのロールを含む)
                    List<Int64> allRoles = existingRoles!
                        .Select(r => r.Id ?? 0)
                        .Concat(addingRoles.Select(r => r.Id ?? 0)).Distinct().ToList();

                    string targetUser = user.GetPSPath();
                    try
                    {
                        if (ShouldProcess(targetUser, $"Add Roles {targetRoles}"))
                        {
                            drive.OrchAPISession.AssignUser(folder.Id ?? 0, user.Id ?? 0, allRoles);
                            drive.FolderUsersWithInherited.ClearCache();
                            drive.FolderUsersWithNoInherited.ClearCache();
                            drive.ClearFolderCache(folder);
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(targetUser, ex), "AddRoleToFolderUserError", ErrorCategory.InvalidOperation, user));
                    }
                }
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "AddRoleToFolderUserError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
