using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using TPositional = UiPath.PowerShell.Positional.UserName_Destination;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Move, "OrchFolderUser", SupportsShouldProcess = true)]
public class MoveFolderUserCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(UserNameCompleter))]
    [SupportsWildcards]
    public string[]? UserName { get; set; }

    //[Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    //[ArgumentCompleter(typeof(FullNameCompleter))]
    //[SupportsWildcards]
    //public string[]? FullName { get; set; }

    //[Parameter]
    //public SwitchParameter WarnOnNoMatch { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Destination { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BoolCompleter))]
    public string? KeepSource { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    //[Parameter]
    //public SwitchParameter Recurse { get; set; }

    //[Parameter]
    //public uint Depth { get; set; }

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

            var results = ParallelResults3.GroupBy(drivesFolders, df => df.drive.FolderUsersWithNoInherited.Get(df.folder));

            foreach (var result in results)
            {
                foreach (var userRoles in result
                    .Where(e => wp.IsMatch(e.UserEntity!.UserName))
                    .ExcludeByWildcards(e => e?.UserEntity?.UserName, wpUserName)
                    .FilterByWildcards(e => e?.UserEntity?.FullName, wpFullName)
                    .OrderBy(e => e.UserEntity!.UserName))
                {
                    string tiphelp = TipHelp(userRoles);
                    yield return new CompletionResult(PathTools.EscapePSText(userRoles.UserEntity!.UserName), userRoles.UserEntity.UserName, CompletionResultType.ParameterValue, tiphelp);
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        //if (UserName is not null && UserName.All(u => string.IsNullOrEmpty(u))) UserName = null;
        //if (FullName is not null && FullName.All(f => string.IsNullOrEmpty(f))) FullName = null;

        //if (UserName is null && FullName is null)
        //{
        //    WriteError(new ErrorRecord(new ArgumentException("Please make sure to specify either -UserName or -FullName."), "RemoveFolderUserError", ErrorCategory.InvalidOperation, this));
        //    return;
        //}

        var srcDrivesFolders = OrchDriveInfo.EnumFoldersWithoutPersonalWorkspace(Path);
        var dstDrivesFolders = OrchDriveInfo.EnumFoldersWithoutPersonalWorkspace(Destination);

        var wpUserName = UserName.ConvertToWildcardPatternList();
        //var wpFullName = FullName?.Select(fn => new WildcardPattern(fn, WildcardOptions.IgnoreCase)).ToList();

        bool keepSource = KeepSource.ToNullableBool() ?? false;

        string action = keepSource ? "Copy Folder User" : "Move Folder User";

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (srcDrive, srcFolder) in srcDrivesFolders)
        {
            cancelHandler.Token.ThrowIfCancellationRequested();

            IEnumerable<UserRoles> targetUsers;
            try
            {
                targetUsers = srcDrive.FolderUsersWithNoInherited.Get(srcFolder)
                    .Where(u => u is not null)
                    .Where(u => u.Id is not null)
                    .FilterByWildcards(fu => fu?.UserEntity?.UserName, wpUserName);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(srcFolder.GetPSPath(), ex), "MoveFolderUserError", ErrorCategory.InvalidOperation, srcFolder));
                continue;
            }

            foreach (var targetUser in targetUsers
                .OrderBy(u => u.UserEntity!.UserName))
            {
                foreach (var (dstDrive, dstFolder) in dstDrivesFolders)
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();

                    if (srcFolder == dstFolder) continue;

                    string targetUserPath = targetUser!.GetPSPath();
                    if (!string.IsNullOrEmpty(targetUser?.UserEntity?.FullName))
                    {
                        targetUserPath += $" ({targetUser.UserEntity.FullName})";
                    }

                    string target = $"Item: {targetUserPath} Destination: {dstFolder.GetPSPath()}";

                    if (srcDrive != dstDrive)
                    {
                        WriteWarning($"{target}: Moving folder users between different tenants is not supported.");
                        continue;
                    }


                    if (ShouldProcess(target, action))
                    {
                        try
                        {
                            // 宛先フォルダーに割り当てる
                            dstDrive.OrchAPISession.AssignUser(dstFolder.Id!.Value, targetUser!.Id!.Value,
                                targetUser.Roles?
                                .Where(r => r.Id is not null)
                                .Select(r => r.Id!.Value));

                            // 子フォルダーのキャッシュも削除しないといけない？ いやそうではないはずだ
                            // dstDrive.ClearFolderCache(srcFolder); これ必要だっけ？
                            dstDrive.FolderUsersWithInherited.ClearCache();
                            dstDrive.FolderUsersWithNoInherited.ClearCache();

                            if (!keepSource)
                            {
                                // 元フォルダーから削除する
                                srcDrive.OrchAPISession.UnassignUserFromFolder(srcFolder.Id!.Value, targetUser.Id.Value);
                                srcDrive.FolderUsersWithInherited.ClearCache();
                                srcDrive.FolderUsersWithNoInherited.ClearCache();
                                // srcDrive.ClearFolderCache(srcFolder); これ必要だっけ？
                            }

                            //Thread.Sleep(600); // API call rate limit を回避するため待機する
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(targetUserPath, ex), "MoveFolderUserError", ErrorCategory.InvalidOperation, targetUser));
                        }
                    }
                }
            }
        }
    }
}
