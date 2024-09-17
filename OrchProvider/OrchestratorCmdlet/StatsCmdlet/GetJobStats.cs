using System.Management.Automation;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.Path;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "OrchJobStats")]
    [OutputType(typeof(Entities.CountStats))]
    public class GetJobStatsCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(DriveCompleter<Positional.Path>))]
        public string[]? Path { get; set; }

        protected override void ProcessRecord()
        {
            var drives = OrchDriveInfo.EnumOrchDrives(Path);

            // ToList() は遅延評価を抑止し、各スレッド内で問い合わせを行えるようにするために必要
            using var results = OrchThreadPool.RunForEach(drives,
                drive => drive.NameColonSeparator,
                drive => drive,
                drive => drive.OrchAPISession.GetJobStats().ToList());

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var result in results)
            {
                try
                {
                    var entities = result.GetResult(cancelHandler.Token);
                    if (entities == null) continue;

                    var drive = result.Source;

                    foreach (var stat in entities)
                    {
                        stat.Path = drive!.NameColonSeparator;
                        WriteObject(stat);
                    }
                }
                catch (OrchException ex)
                {
                    WriteError(new ErrorRecord(ex, "GetJobStatsError", ErrorCategory.InvalidOperation, ex.Target));
                }
            }
        }
    }
}
