using System.Collections;
using System.Collections.Concurrent;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.Name_Destination;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Copy, "OrchQueue", SupportsShouldProcess = true)]
    [OutputType(typeof(Entities.QueueDefinition))]
    public class CopyQueueCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(QueueNameCompleter<Positional.Name_Destination>))]
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
            var srcDrives = OrchDriveInfo.EnumOrchDrives(Path);
            var srcDrivesFolders = OrchDriveInfo.EnumFolders(Path);
            var dstDrivesFolders = OrchDriveInfo.EnumFolders(Destination);
            var wpName = Name.ConvertToWildcardPatternList();

            foreach (var srcDrive in srcDrives)
            {
                srcDrive._dicQueueLinks = null;
            }

            // コピーの直前でキャッシュを削除するので、ここで取得しておくのは意味がない

            string msg = "Copying queues...";
            using var reporterQueues = new ProgressReporter(this, 700, Int32.MaxValue, msg, msg);
            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var (dstDrive, dstFolder) in dstDrivesFolders)
            {
                foreach (var (srcDrive, srcFolder) in srcDrivesFolders)
                {
                    try
                    {
                        Core.OrchProvider.CopyQueues(this,
                            srcDrive, srcFolder, wpName,
                            dstDrive, dstFolder, reporterQueues,
                            cancelHandler.Token, false);
                        dstDrive._dicQueueDefinitions?.TryRemove(dstFolder.Id ?? 0, out _);
                    }
                    catch (Exception ex)
                    {
                        string target = dstFolder.GetPSPath();
                        WriteError(new ErrorRecord(new OrchException(target, ex), "CopyQueueError", ErrorCategory.InvalidOperation, target));
                    }
                }
            }
        }
    }
}
