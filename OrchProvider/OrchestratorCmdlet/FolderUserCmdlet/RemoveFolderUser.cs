using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using TPositional = UiPath.PowerShell.Positional.UserName_FullName;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchFolderUser", SupportsShouldProcess = true)]
public class RemoveFolderUserCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(UserNameCompleter))]
    [SupportsWildcards]
    public string[]? UserName { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(FullNameCompleter))]
    [SupportsWildcards]
    public string[]? FullName { get; set; }

    [Parameter]
    public SwitchParameter NoMatchWarning { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    private class UserNameCompleter : OrchArgumentCompleter
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
            _= uint.TryParse(paramDepth, out uint depth);

            // パラメータからパスを抽出する。指定がなければ、カレントディレクトリを対象にする
            var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
            var drivesFolders = OrchDriveInfo.EnumFoldersWithoutPersonalWorkspace(paramPath, recurse, depth);

            // パラメータで選択済みの UserName は、候補から除外する
            var wpUserName = CreateWPListFromParameter(commandAst, "UserName", TPositional.Parameters, wordToComplete);

            // パラメータで選択された FullName のみ対象とする
            var wpFullName = CreateWPListFromOtherParameters(commandAst, "FullName", TPositional.Parameters);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.ForEach(drivesFolders, df => df.drive.FolderUsersWithNoInherited.Get(df.folder));

            foreach (var result in results)
            {
                if (!result.TryGetValue(out var entities)) continue;

                foreach (var e in entities!
                    .Where(e => wp.IsMatch(e.UserEntity!.UserName))
                    .ExcludeByWildcards(e => e?.UserEntity?.UserName, wpUserName)
                    .FilterByWildcards(e => e?.UserEntity?.FullName, wpFullName)
                    .OrderBy(e => e.UserEntity!.UserName))
                {
                    string tiphelp = TipHelp(e);
                    yield return new CompletionResult(PathTools.EscapePSText(e.UserEntity!.UserName), e.UserEntity.UserName, CompletionResultType.ParameterValue, tiphelp);
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

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.ForEach(drivesFolders, df => df.drive.FolderUsersWithNoInherited.Get(df.folder));

            foreach (var result in results)
            {
                if (!result.TryGetValue(out var entities)) continue;

                foreach (var e in entities!
                    .Where(e => wp.IsMatch(e.UserEntity!.FullName))
                    .FilterByWildcards(e => e?.UserEntity?.UserName, wpUserName)
                    .ExcludeByWildcards(e => e?.UserEntity?.FullName, wpFullName)
                    .OrderBy(e => e.UserEntity!.FullName))
                {
                    string tiphelp = TipHelp(e);
                    yield return new CompletionResult(PathTools.EscapePSText(e.UserEntity!.FullName), e.UserEntity.FullName, CompletionResultType.ParameterValue, tiphelp);
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        if (UserName is not null && UserName.All(u => string.IsNullOrEmpty(u))) UserName = null;
        if (FullName is not null && FullName.All(f => string.IsNullOrEmpty(f))) FullName = null;

        if (UserName is null && FullName is null)
        {
            WriteError(new ErrorRecord(new ArgumentException("Please make sure to specify either -UserName or -FullName."), "RemoveFolderUserError", ErrorCategory.InvalidOperation, this));
            return;
        }

        var drivesFolders = OrchDriveInfo.EnumFoldersWithoutPersonalWorkspace(Path, Recurse.IsPresent, Depth);

        var wpUserName = UserName.ConvertToWildcardPatternList();
        var wpFullName = FullName.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            try
            {
                //drive.FolderUsersWithInherited.ClearCache(); // 一貫したキャッシュを保持できるように、こっちもクリアしておく
                //drive.FolderUsersWithNoInherited.ClearCache();
                var folderUsers = drive.FolderUsersWithNoInherited.Get(folder);

                IEnumerable<UserRoles> filteredUsers = folderUsers
                    .FilterByWildcards(fu => fu?.UserEntity?.UserName, wpUserName)
                    .FilterByWildcards(fu => fu?.UserEntity?.FullName, wpFullName);

                if (NoMatchWarning.IsPresent && !filteredUsers.Any())
                {
                    // ちょっと適当な実装だけど、これでも CSV インポート時にちゃんと動くから十分か。。
                    // ちゃんと実装するには、UserName の配列を先頭から順にひとつずつ処理しないといけない。
                    WriteWarning($"No match found for UserName '{UserName?[0]}' and FullName '{FullName?[0]}'.");
                    continue;
                }

                foreach (var folderUser in filteredUsers.OrderBy(u => u.UserEntity!.UserName))
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();

                    var targetUser = folderUser.GetPSPath();
                    if (!string.IsNullOrEmpty(folderUser?.UserEntity?.FullName))
                    {
                        targetUser += $" ({folderUser.UserEntity.FullName})";
                    }

                    if (ShouldProcess(targetUser, $"Remove Folder User"))
                    {
                        try
                        {
                            drive.OrchAPISession.UnassignUserFromFolder(folder.Id ?? 0, folderUser?.Id ?? 0);
                            drive.FolderUsersWithInherited.ClearCache();
                            drive.FolderUsersWithNoInherited.ClearCache();
                            drive.ClearFolderCache(folder);
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(targetUser, ex), "RemoveFolderUserError", ErrorCategory.InvalidOperation, folderUser));
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "GetFolderUserError", ErrorCategory.InvalidOperation, folder));
            }
        }
    }

    // マルチスレッド化したバージョン
    // HTTP call を cap した状態では逆に遅くなる場合があるため、シングルスレッドで書き直した
    //protected override void ProcessRecord()
    //{
    //    if ((FullName is null || FullName.Length == 0) && (UserName is null || UserName.Length == 0))
    //    {
    //        throw new Exception("Please make sure to specify either -FullName or -UserName.");
    //    }

    //    var drivesFolders = OrchDriveInfo.EnumFoldersWithoutPersonalWorkspace(Path, Recurse.IsPresent, Depth);
    //    var wpUserName = UserName?.Select(un => new WildcardPattern(un, WildcardOptions.IgnoreCase)).ToList();
    //    var wpFullName = FullName?.Select(fn => new WildcardPattern(fn, WildcardOptions.IgnoreCase)).ToList();

    //    // あらかじめ、非同期で対象のフォルダーユーザーを取得しておく
    //    Parallel.ForEach(drivesFolders, driveFolders =>
    //    {
    //        var (drive, folder) = driveFolders;
    //        try
    //        {
    //            drive._dicUserRoles?.TryRemove((folder.Id ?? 0, true), out _);
    //            drive._dicUserRoles?.TryRemove((folder.Id ?? 0, false), out _);
    //            drive.GetUsersForFolder(folder, false);
    //        }
    //        catch { }
    //    });

    //    using var cancelHandler = new ConsoleCancelHandler();
    //    foreach (var (drive, folder) in drivesFolders)
    //    {
    //        try
    //        {
    //            var folderUsers = drive.GetUsersForFolder(folder, false);
    //            string targetUser;

    //            IEnumerable<UserRoles> filteredUsers = folderUsers
    //                .FilterByWildcards(fu => fu.UserEntity!.UserName!, wpUserName)
    //                .FilterByWildcards(fu => fu.UserEntity!.FullName!, wpFullName);

    //            foreach (var folderUser in filteredUsers)
    //            {
    //                cancelHandler.Token.ThrowIfCancellationRequested();

    //                targetUser = folderUser.GetPSPath();
    //                if (ShouldProcess(targetUser, $"Remove Folder User"))
    //                {
    //                    try
    //                    {
    //                        drive.OrchAPISession.UnassignUserFromFolder(folder.Id ?? 0, folderUser.Id ?? 0);
    //                        drive._dicUserRoles?.TryRemove((folder.Id ?? 0, false), out _);
    //                        drive._dicUserRoles?.TryRemove((folder.Id ?? 0, true), out _);
    //                        drive.ClearFolderCache(folder);
    //                    }
    //                    catch (Exception ex)
    //                    {
    //                        WriteError(new ErrorRecord(new OrchException(targetUser, ex), "RemoveFolderUserError", ErrorCategory.InvalidOperation, folderUser));
    //                    }
    //                }
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "GetFolderUserError", ErrorCategory.InvalidOperation, folder));
    //        }
    //    }
    //}
}
