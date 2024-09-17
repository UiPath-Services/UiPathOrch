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
    [Cmdlet(VerbsCommon.Copy, "OrchProcess", SupportsShouldProcess = true)]
    [OutputType(typeof(Entities.Release))]
    public class CopyProcessCommand : OrchestratorPSCmdlet
    {
        [Parameter (Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(ProcessNameCompleter<Positional.Name_Destination>))]
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
            var srcDrivesFolders = OrchDriveInfo.EnumFolders(Path);
            var dstDrivesFolders = OrchDriveInfo.EnumFolders(Destination);
            var wpName = Name.ConvertToWildcardPatternList();

            string msg = "Copying process(es)...";
            using var reporterProcesses = new ProgressReporter(this, 500, Int32.MaxValue, msg, msg);
            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var (dstDrive, dstFolder) in dstDrivesFolders)
            {
                foreach (var (srcDrive, srcFolder) in srcDrivesFolders)
                {
                    try
                    {
                        Core.OrchProvider.CopyProcesses(this,
                            srcDrive, srcFolder, wpName,
                            dstDrive, dstFolder, reporterProcesses,
                            cancelHandler.Token, false);
                        dstDrive._dicReleases?.TryRemove(dstFolder.Id ?? 0, out _);
                        dstDrive._dicReleaseList?.TryRemove(dstFolder.Id ?? 0, out _);
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
}
