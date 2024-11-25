using System.Management.Automation;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;
using TPositional = UiPath.PowerShell.Positional.Name_Destination;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Copy, "OrchProcess", SupportsShouldProcess = true)]
    [OutputType(typeof(Entities.Release))]
    public class CopyProcessCommand : OrchestratorPSCmdlet
    {
        [Parameter (Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(ProcessNameCompleter<TPositional>))]
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

            string msg = "Copying processes...";
            using var reporterProcesses = new ProgressReporter(this, 500, Int32.MaxValue, msg, msg);
            using var cancelHandler = new ConsoleCancelHandler();

            foreach (var (_, srcFolder) in srcDrivesFolders)
            {
                cancelHandler.Token.ThrowIfCancellationRequested();

                try
                {
                    // コピー対象のエンティティがひとつもなければ、dstFolder を検索する必要はない
                    //srcDrive._dicReleases?.TryRemove(srcFolder.Id ?? 0, out _);
                    var srcEntities = srcDrive.GetReleases(srcFolder).FilterByWildcards(b => b?.Name, wpName);
                    if (!srcEntities.Any()) continue;
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(srcFolder.GetPSPath(), ex), "GetProcessError", ErrorCategory.InvalidOperation, srcFolder));
                    continue;
                }

                Folder? dstFolder = GetRelativeDstFolder(srcRootFolder, srcFolder, dstDrive, dstRootFolder);
                if (dstFolder == null || (srcDrive == dstDrive && srcFolder == dstFolder)) continue;

                try
                {
                    Core.OrchProvider.CopyProcesses(this,
                        srcDrive, srcFolder, wpName,
                        dstDrive, dstFolder, reporterProcesses,
                        false, cancelHandler.Token);
                    dstDrive._dicReleases?.TryRemove(dstFolder.Id ?? 0, out _);
                    //dstDrive._dicReleaseList?.TryRemove(dstFolder.Id ?? 0, out _);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    string target = dstFolder.GetPSPath();
                    WriteError(new ErrorRecord(new OrchException(target, ex), "CopyProcessError", ErrorCategory.InvalidOperation, target));
                }
            }
        }
    }
}
