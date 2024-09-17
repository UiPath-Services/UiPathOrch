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
    [Cmdlet(VerbsCommon.Copy, "OrchBucket", SupportsShouldProcess = true)]
    [OutputType(typeof(Bucket))]
    public class CopyBucketCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(BucketNameCompleter<Positional.Name_Destination>))]
        [SupportsWildcards]
        public string[]? Name { get; set; }

        [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [SupportsWildcards]
        public string[]? Destination { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [SupportsWildcards]
        public string? Path { get; set; }

        //[Parameter]
        //public SwitchParameter Recurse { get; set; }

        //[Parameter]
        //public uint Depth { get; set; }

        protected override void ProcessRecord()
        {
            var srcDrivesFolders = OrchDriveInfo.EnumFolders(Path);
            var dstDrivesFolders = OrchDriveInfo.EnumFolders(Destination);
            var wpName = Name.ConvertToWildcardPatternList();

            string msg = "Copying buckets...";
            using var reporterBuckets = new ProgressReporter(this, 1000, Int32.MaxValue, msg, msg);
            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var (dstDrive, dstFolder) in dstDrivesFolders)
            {
                foreach (var (srcDrive, srcFolder) in srcDrivesFolders)
                {
                    try
                    {
                        Core.OrchProvider.CopyBuckets(this,
                            srcDrive, srcFolder, wpName,
                            dstDrive, dstFolder, reporterBuckets,
                            cancelHandler.Token, false);
                        dstDrive._dicBuckets?.TryRemove(dstFolder.Id ?? 0, out _);
                        dstDrive._dicBucketLinks = null;
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        string target = dstFolder.GetPSPath();
                        WriteError(new ErrorRecord(new OrchException(target, ex), "CopyBucketError", ErrorCategory.InvalidOperation, dstFolder));
                    }
                }
            }
        }
    }
}
