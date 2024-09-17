using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.Name_Destination;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Copy, "OrchTestSetSchedule", SupportsShouldProcess = true)]
    [OutputType(typeof(Entities.TestSetSchedule))]
    public class CopyTestSetScheduleCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(TestScheduleNameCompleter<Positional.Name_Destination>))]
        [SupportsWildcards]
        public string[]? Name { get; set; }

        [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [SupportsWildcards]
        public string[]? Destination { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [SupportsWildcards]
        public string[]? Path { get; set; }

        //[Parameter]
        //public SwitchParameter Recurse { get; set; }

        //[Parameter]
        //public uint Depth { get; set; }

        protected override void ProcessRecord()
        {
            var srcDrivesFolders = OrchDriveInfo.EnumFoldersWithoutPersonalWorkspace(Path);
            var dstDrivesFolders = OrchDriveInfo.EnumFoldersWithoutPersonalWorkspace(Destination);
            var wpName = Name.ConvertToWildcardPatternList();

            // コピーの直前でキャッシュを削除するので、ここで取得しておくのは意味がない

            string msg = "Copying test schedules...";
            using var reporterTestSchedules = new ProgressReporter(this, 1200, Int32.MaxValue, msg, msg);
            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var (dstDrive, dstFolder) in dstDrivesFolders)
            {
                foreach (var (srcDrive, srcFolder) in srcDrivesFolders)
                {
                    try
                    {
                        Core.OrchProvider.CopyTestSetSchedules(this,
                            srcDrive, srcFolder, wpName,
                            dstDrive, dstFolder, reporterTestSchedules,
                            cancelHandler.Token, false);
                        dstDrive._dicTestSetSchedules?.TryRemove(dstFolder.Id ?? 0, out _);
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
}
