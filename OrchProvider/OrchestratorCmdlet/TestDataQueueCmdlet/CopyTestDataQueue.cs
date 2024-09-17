using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.Name_Destination;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Copy, "OrchTestDataQueue", SupportsShouldProcess = true)]
    [OutputType(typeof(Entities.TestDataQueue))]
    public class CopyTestDataQueueCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(TestDataQueueNameCompleter<Positional.Name_Destination>))]
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

            string msg = "Copying test data queues...";
            using var reporterTestDataQueues = new ProgressReporter(this, 1300, Int32.MaxValue, msg, msg);
            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var (dstDrive, dstFolder) in dstDrivesFolders)
            {
                foreach (var (srcDrive, srcFolder) in srcDrivesFolders)
                {
                    if (srcDrive == dstDrive && srcFolder == dstFolder) continue;

                    try
                    {
                        Core.OrchProvider.CopyTestDataQueues(this,
                            srcDrive, srcFolder, wpName,
                            dstDrive, dstFolder, reporterTestDataQueues,
                            cancelHandler.Token, false);
                        dstDrive._dicTestDataQueues?.TryRemove(dstFolder.Id ?? 0, out _);
                    }
                    catch (Exception ex)
                    {
                        string target = dstFolder.GetPSPath();
                        WriteError(new ErrorRecord(new OrchException(target, ex), "CopyTestDataQueueError", ErrorCategory.InvalidOperation, dstFolder));
                    }
                }
            }

        }
    }
}
