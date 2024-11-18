using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.Name_Destination;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Copy, "OrchTestSet", SupportsShouldProcess = true)]
    [OutputType(typeof(Entities.TestSet))]
    public class CopyTestSetCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(TestSetNameCompleter<Positional.Name_Destination>))]
        [SupportsWildcards]
        public string[]? Name { get; set; }

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

        protected override void ProcessRecord()
        {
            var (srcDrive, srcRootFolder) = OrchDriveInfo.ResolveToSingleFolder(Path);
            var srcDrivesFolders = OrchDriveInfo.EnumFolders(Path, Recurse.IsPresent, Depth);

            var (dstDrive, dstRootFolder) = OrchDriveInfo.ResolveToSingleFolder(Destination);

            // コピー元とコピー先が同じなら、何もしない
            if (srcDrive == dstDrive && srcRootFolder == dstRootFolder) return;

            var wpName = Name.ConvertToWildcardPatternList();

            string msg = "Copying test sets...";
            using var reporterTestSets = new ProgressReporter(this, 1100, Int32.MaxValue, msg, msg);
            using var cancelHandler = new ConsoleCancelHandler();

            foreach (var (_, srcFolder) in srcDrivesFolders)
            {
                cancelHandler.Token.ThrowIfCancellationRequested();

                try
                {
                    // コピー対象のエンティティがひとつもなければ、dstFolder を検索する必要はない
                    //srcDrive._dicTestSets?.TryRemove(srcFolder.Id ?? 0, out _);
                    var srcEntities = srcDrive.TestSets.Get(srcFolder).FilterByWildcards(b => b?.Name, wpName);
                    if (!srcEntities.Any()) continue;
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(srcFolder.GetPSPath(), ex), "GetTestSetError", ErrorCategory.InvalidOperation, srcFolder));
                    continue;
                }

                Folder? dstFolder = GetRelativeDstFolder(srcRootFolder, srcFolder, dstDrive, dstRootFolder);
                if (dstFolder == null || (srcDrive == dstDrive && srcFolder == dstFolder)) continue;

                try
                {
                    Core.OrchProvider.CopyTestSets(this,
                        srcDrive, srcFolder, wpName,
                        dstDrive, dstFolder, reporterTestSets, 
                        false, cancelHandler.Token);
                    dstDrive.TestSets.ClearCache(dstFolder);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    string target = dstFolder.GetPSPath();
                    WriteError(new ErrorRecord(new OrchException(target, ex), "CopyTestScheduleError", ErrorCategory.InvalidOperation, dstFolder));
                }
            }
        }
    }
}
