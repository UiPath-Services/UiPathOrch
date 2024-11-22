using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Copy, "OrchFolderUser", SupportsShouldProcess = true)]
    [OutputType(typeof(Entities.UserRoles))]
    public class CopyFolderUserCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(UserNameCompleter))]
        [SupportsWildcards]
        public string[]? UserName { get; set; }

        // TODO: これは string[] にしても良い気がする？
        [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [SupportsWildcards]
        public string? Destination { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [SupportsWildcards]
        public string? Path { get; set; }

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
                var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

                var IncludeInherited = GetFakeBoundParameterAsBool(fakeBoundParameters, "IncludeInherited");

                // パラメータで選択済みの UserName は、候補から除外する
                var paramUserName = GetParameterValues(commandAst, "UserName", null, wordToComplete);
                var wpUserName = paramUserName.Select(un => new WildcardPattern(un, WildcardOptions.IgnoreCase)).ToList();

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drivesFolders, df => {
                    return IncludeInherited
                        ? df.drive.FolderUsersWithInherited.Get(df.folder)
                        : df.drive.FolderUsersWithNoInherited.Get(df.folder);
                });

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;

                    foreach (var e in entities!
                        .Where(fu => wp.IsMatch(fu.UserEntity!.UserName))
                        .ExcludeByWildcards(u => u?.UserEntity?.UserName!, wpUserName)
                        .OrderBy(u => u.UserEntity!.UserName!))
                    {
                        string tiphelp = TipHelp(e);
                        yield return new CompletionResult(PathTools.EscapePSText(e.UserEntity!.UserName), e.UserEntity.UserName, CompletionResultType.ParameterValue, tiphelp);
                    }
                }
            }
        }

        protected override void ProcessRecord()
        {
            var (srcDrive, srcRootFolder) = OrchDriveInfo.ResolveToSingleFolder(Path);
            var srcDrivesFolders = OrchDriveInfo.EnumFolders(Path, Recurse.IsPresent, Depth);

            var (dstDrive, dstRootFolder) = OrchDriveInfo.ResolveToSingleFolder(Destination);

            // コピー元とコピー先が同じなら、何もしない
            if (srcDrive == dstDrive && srcRootFolder == dstRootFolder) return;

            var wpUserName = UserName.ConvertToWildcardPatternList();

            string msg = "Copying folder users...";
            using var reporter = new ProgressReporter(this, 200, Int32.MaxValue, msg, msg);
            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var (_, srcFolder) in srcDrivesFolders)
            {
                cancelHandler.Token.ThrowIfCancellationRequested();

                try
                {
                    // コピー対象のエンティティがひとつもなければ、dstFolder を検索する必要はない
                    //srcDrive.FolderUsersWithInherited.ClearCache();
                    //srcDrive.FolderUsersWithNoInherited.ClearCache();
                    var srcEntities = srcDrive.FolderUsersWithNoInherited.Get(srcFolder)
                        .FilterByWildcards(u => u?.UserEntity?.UserName, wpUserName);
                    if (!srcEntities.Any()) continue;
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(srcFolder.GetPSPath(), ex), "GetFolderUserError", ErrorCategory.InvalidOperation, srcFolder));
                    continue;
                }

                Folder? dstFolder = GetRelativeDstFolder(srcRootFolder, srcFolder, dstDrive, dstRootFolder);
                if (dstFolder == null || (srcDrive == dstDrive && srcFolder == dstFolder)) continue;

                try
                {
                    Core.OrchProvider.CopyFolderUsers(this,
                        srcDrive, srcFolder, wpUserName,
                        dstDrive, dstFolder, reporter,
                        false, cancelHandler.Token);
                    dstDrive.FolderUsersWithInherited.ClearCache();
                    dstDrive.FolderUsersWithNoInherited.ClearCache();
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    string target = dstFolder.GetPSPath();
                    WriteError(new ErrorRecord(new OrchException(target, ex), "CopyFolderUserError", ErrorCategory.InvalidOperation, dstFolder));
                }
            }
        }
    }
}
