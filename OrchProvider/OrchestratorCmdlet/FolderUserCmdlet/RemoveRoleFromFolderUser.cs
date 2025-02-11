using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using TPositional = UiPath.PowerShell.Positional.UserName_Roles;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchRoleFromFolderUser", SupportsShouldProcess = true)]
public class RemoveRoleFromFolderUserCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(FolderUserUserNameCompleter))]
    public string[]? UserName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(FolderUserFullNameCompleter))]
    [SupportsWildcards]
    public string[]? FullName { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [Alias("FolderRoles")]
    [ArgumentCompleter(typeof(RolesCompleter))]
    public string[]? Roles { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(KeyOfDictionaryCompleter<DirectoryTypeItems, int>))]
    public string[]? Type { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    // TODO: これは共通化できる気がする
    private class FolderUserUserNameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var recurse = GetSwitchParameterValue(commandAst, "Recurse");
            var paramDepth = GetParameterValue(commandAst, "Depth");
            uint.TryParse(paramDepth, out uint depth);

            // パラメータからパスを抽出する。指定がなければ、カレントディレクトリを対象にする
            var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
            var drivesFolders = OrchDriveInfo.EnumFoldersWithoutPersonalWorkspace(paramPath, recurse, depth);

            // パラメータで選択済みの UserName は、候補から除外する
            var wpUserName = CreateWPListFromParameter(commandAst, "UserName", TPositional.Parameters, wordToComplete);

            // パラメータで選択された FullName のみ対象とする
            var wpFullName = CreateWPListFromOtherParameters(commandAst, "FullName", TPositional.Parameters);

            var wpType = CreateWPListFromOtherParameters(commandAst, "Type", TPositional.Parameters);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.ForEach(drivesFolders, df => df.drive.FolderUsersWithNoInherited.Get(df.folder));

            foreach (var result in results)
            {
                if (!result.TryGetValue(out var entities)) continue;

                foreach (var e in entities!
                    .Where(eu => wp.IsMatch(eu.UserEntity!.UserName!))
                    .ExcludeByWildcards(eu => eu?.UserEntity?.UserName, wpUserName)
                    .FilterByWildcards(eu => eu?.UserEntity?.FullName, wpFullName)
                    .FilterByWildcards(eu => eu?.UserEntity?.Type, wpType)
                    .OrderBy(u => u.UserEntity!.UserName))
                {
                    string tiphelp = TipHelp(e);
                    var ret = new CompletionResult(PathTools.EscapePSText(e.UserEntity!.UserName), e.UserEntity.UserName, CompletionResultType.ParameterValue, tiphelp);
                    yield return ret;
                }
            }
        }
    }

    private class FolderUserFullNameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var recurse = GetSwitchParameterValue(commandAst, "Recurse");
            var paramDepth = GetParameterValue(commandAst, "Depth");
            uint.TryParse(paramDepth, out uint depth);

            // パラメータからパスを抽出する。指定がなければ、カレントディレクトリを対象にする
            var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
            var drivesFolders = OrchDriveInfo.EnumFoldersWithoutPersonalWorkspace(paramPath, recurse, depth);

            // パラメータで選択された UserName のみ対象とする
            var wpUserName = CreateWPListFromOtherParameters(commandAst, "UserName", TPositional.Parameters);

            // パラメータで選択済みの FullName は、候補から除外する
            var wpFullName = CreateWPListFromParameter(commandAst, "FullName", TPositional.Parameters, wordToComplete);

            var wpType = CreateWPListFromOtherParameters(commandAst, "Type", TPositional.Parameters);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.ForEach(drivesFolders, df => df.drive.FolderUsersWithNoInherited.Get(df.folder));

            foreach (var result in results)
            {
                if (!result.TryGetValue(out var entities)) continue;

                foreach (var e in entities!
                    .Where(eu => wp.IsMatch(eu.UserEntity?.FullName))
                    .ExcludeByWildcards(eu => eu?.UserEntity?.FullName, wpFullName)
                    .FilterByWildcards(eu => eu?.UserEntity?.UserName, wpUserName)
                    .FilterByWildcards(eu => eu?.UserEntity?.Type, wpType)
                    .OrderBy(u => u.UserEntity!.FullName))
                {
                    string tiphelp = TipHelp(e);
                    var ret = new CompletionResult(PathTools.EscapePSText(e.UserEntity!.FullName), e.UserEntity.FullName, CompletionResultType.ParameterValue, tiphelp);
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
            var recurse = GetSwitchParameterValue(commandAst, "Recurse");
            var paramDepth = GetParameterValue(commandAst, "Depth");
            uint.TryParse(paramDepth, out uint depth);

            // パラメータからパスを抽出する。指定がなければ、カレントディレクトリを対象にする
            var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
            var drivesFolders = OrchDriveInfo.EnumFoldersWithoutPersonalWorkspace(paramPath, recurse, depth);

            var wpFullName = CreateWPListFromOtherParameters(commandAst, "FullName", TPositional.Parameters);
            var wpUserName = CreateWPListFromOtherParameters(commandAst, "UserName", TPositional.Parameters);
            var wpRoles = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

            var wpType = CreateWPListFromOtherParameters(commandAst, "Type", TPositional.Parameters);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.ForEach(drivesFolders, df => df.drive.FolderUsersWithNoInherited.Get(df.folder));

            foreach (var result in results)
            {
                if (!result.TryGetValue(out var entities)) continue;

                foreach (var e in entities!
                    .FilterByWildcards(u => u?.UserEntity?.FullName, wpFullName)
                    .FilterByWildcards(u => u?.UserEntity?.UserName, wpUserName)
                    .FilterByWildcards(u => u?.UserEntity?.Type, wpType))
                {
                    if (e.Roles is not null)
                    {
                        foreach (var role in e.Roles
                            .Where(r => wp.IsMatch(r.Name))
                            .ExcludeByWildcards(role => role?.Name, wpRoles)
                            .OrderBy(role => role.Name))
                        {
                            string tiphelp = TipHelp(e);
                            var ret = new CompletionResult(PathTools.EscapePSText(role.Name), role.Name, CompletionResultType.ParameterValue, tiphelp);
                            yield return ret;
                        }
                    }
                }
            }
        }
    }

    // もともとマルチスレッドにしていなかった
    protected override void ProcessRecord()
    {
        if (UserName?.Length == 0 || string.IsNullOrEmpty(UserName?[0])) UserName = null;
        if (FullName?.Length == 0 || string.IsNullOrEmpty(FullName?[0])) FullName = null;

        if (UserName is null && FullName is null)
        {
            WriteError(new ErrorRecord(new ArgumentException("Please make sure to specify either -UserName or -FullName."), "RemoveRoleFromFolderUserError", ErrorCategory.InvalidOperation, this));
            return;
        }

        var drivesFolders = OrchDriveInfo.EnumFoldersWithoutPersonalWorkspace(Path, Recurse.IsPresent, Depth);

        var wpFullName = FullName.ConvertToWildcardPatternList();
        var wpUserName = UserName.ConvertToWildcardPatternList();
        var wpType = Type.ConvertToWildcardPatternList();

        // 先頭の要素は CSV から入力されている可能性があるので、先頭の要素についてはカンマで区切る
        //if (Roles is not null && Roles.Length > 0) Roles = Roles[0].Split(',').Concat(Roles.Skip(1)).ToArray();
        var wpRoles = Roles.Split1stValueByUnescapedCommas().ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            try
            {
                var tenantRoles = drive.Roles.Get().Where(role => role.Type != "Tenant").FilterByWildcards(role => role?.DisplayName, wpRoles);
                try
                {
                    // 編集対象のユーザーを抽出する
                    var existingUsers = drive.FolderUsersWithNoInherited.Get(folder);
                    List<UserRoles> editingUsers = existingUsers
                        .FilterByWildcards(eu => eu?.UserEntity?.FullName, wpFullName)
                        .FilterByWildcards(eu => eu?.UserEntity?.UserName, wpUserName)
                        .FilterByWildcards(eu => eu?.UserEntity?.Type, wpType)
                        .ToList();

                    foreach (var user in editingUsers.OrderBy(user => user.UserEntity!.FullName))
                    {
                        cancelHandler.Token.ThrowIfCancellationRequested();

                        IEnumerable<SimpleRole> existingRoles = user.Roles;

                        // 削除するロールを抽出
                        IEnumerable<SimpleRole> targetRoles = existingRoles!.FilterByWildcards(role => role?.Name, wpRoles);
                        if (!targetRoles.Any()) continue;
                        string strTargetRoles = string.Join(", ", targetRoles.Select(role => role.Name));

                        // 削除するロールを除去
                        IEnumerable<SimpleRole> keepingRoles = existingRoles!.ExcludeByWildcards(role => role?.Name, wpRoles!);

                        string targetUser = user.GetPSPath();
                        try
                        {
                            if (ShouldProcess(targetUser, $"Remove Roles {strTargetRoles}"))
                            {
                                drive.OrchAPISession.AssignUser(folder.Id ?? 0, user.Id ?? 0, keepingRoles.Select(role => role.Id ?? 0));
                                drive.FolderUsersWithInherited.ClearCache(folder);
                                drive.FolderUsersWithNoInherited.ClearCache(folder);
                                drive.ClearFolderCache(folder);
                            }
                        }
                        catch (Exception ex)
                        {
                            var errorRecord = new ErrorRecord(new OrchException(targetUser, ex), "RemoveRoleFromFolderUserError", ErrorCategory.InvalidOperation, user);
                            WriteError(errorRecord);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    string targetFolder = folder.GetPSPath();
                    WriteError(new ErrorRecord(new OrchException(targetFolder, ex), "RemoveRoleFromFolderUserError", ErrorCategory.InvalidOperation, targetFolder));
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "RemoveRoleFromFolderUserError", ErrorCategory.InvalidOperation, drive));
            }
        }
    }
}
