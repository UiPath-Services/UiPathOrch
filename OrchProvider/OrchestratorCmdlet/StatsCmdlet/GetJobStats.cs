using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using TPositional = UiPath.PowerShell.Positional.Path;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchJobStats")]
[OutputType(typeof(Entities.CountStats))]
public class GetJobStatsCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
    public string[]? Path { get; set; }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumOrchDrives(Path);

        // ToList() is needed to prevent deferred evaluation and allow queries within each thread
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
                if (entities is null) continue;

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
