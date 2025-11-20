using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using TPositional = UiPath.PowerShell.Positional.Path;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "TmConfiguration")]
[OutputType(typeof(Entities.TmConfig))]
public class GetTmConfigurationCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TmDriveCompleter<TPositional>))]
    public string[]? Path { get; set; }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumTmDrives(Path);

        //foreach (var drive in drives)
        //{
        //    WriteObject(drive.GetTmConfiguration());
        //}

        using var results = OrchThreadPool.RunForEach(drives,
            drive => drive.NameColonSeparator,
            drive => drive,
            drive => drive.TmConfiguration.Get());

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var entity = result.GetResult(cancelHandler.Token);
                if (entity is null) continue;

                WriteObject(entity);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetTmConfigurationError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
