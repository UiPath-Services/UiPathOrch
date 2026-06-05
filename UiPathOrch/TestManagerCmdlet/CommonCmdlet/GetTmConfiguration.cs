using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "TmConfiguration")]
[OutputType(typeof(Entities.TmConfig))]
public class GetTmConfigurationCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TmDriveCompleter))]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumTmDrives(EffectivePath(Path, LiteralPath));

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
