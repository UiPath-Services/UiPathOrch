using System.Collections;
using System.Collections.Concurrent;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Xml.Linq;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.UserName;

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

                var results = ParallelResults.ForEach(drivesFolders, df => df.drive.GetUsersForFolder(df.folder, IncludeInherited));

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
            //var srcDrivesFolders = OrchDriveInfo.EnumFolders(Path);
            //var dstDrivesFolders = OrchDriveInfo.EnumFolders(Destination);

            var (srcDrive, srcRootFolder) = OrchDriveInfo.ResolveToSingleFolder(Path);
            var srcDrivesFolders = OrchDriveInfo.EnumFolders(Path, Recurse.IsPresent, Depth);

            var (dstDrive, dstRootFolder) = OrchDriveInfo.ResolveToSingleFolder(Destination);

            // コピー元とコピー先が同じなら、何もしない
            if (srcDrive == dstDrive && srcRootFolder.FullyQualifiedName == dstRootFolder.FullyQualifiedName) return;

            var wpUserName = UserName.ConvertToWildcardPatternList();

            string msg = "Copying assigned users...";
            using var reporter = new ProgressReporter(this, 200, Int32.MaxValue, msg, msg);
            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var (_, srcFolder) in srcDrivesFolders)
            {
                // コピー対象のエンティティがひとつもなければ、dstFolder を検索する必要はない
                srcDrive!._dicUserRoles?.TryRemove((srcFolder.Id ?? 0, true), out var _);
                srcDrive!._dicUserRoles?.TryRemove((srcFolder.Id ?? 0, false), out var _);
                var srcEntities = srcDrive.GetUsersForFolder(srcFolder, false)
                    .FilterByWildcards(u => u?.UserEntity?.UserName, wpUserName);
                if (!srcEntities.Any()) continue;

                Folder? dstFolder = GetRelativeDstFolder(srcRootFolder, srcFolder, dstDrive, dstRootFolder);
                if (dstFolder == null) continue;

                try
                {
                    Core.OrchProvider.CopyFolderUsers(this,
                        srcDrive, srcFolder, wpUserName,
                        dstDrive, dstFolder, reporter,
                        cancelHandler.Token, false);
                    dstDrive._dicUserRoles?.TryRemove((dstFolder.Id ?? 0, false), out _);
                    dstDrive._dicUserRoles?.TryRemove((dstFolder.Id ?? 0, true), out _);
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
