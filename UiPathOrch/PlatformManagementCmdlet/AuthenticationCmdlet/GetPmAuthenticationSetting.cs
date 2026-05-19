using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "PmAuthenticationSetting")]
[OutputType(typeof(Entities.PmAuthenticationRoot))]
public class GetPmAuthenticationSettingCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumPmDrives(Path);

        // Fetch in parallel; per-org caches serialize same-partition fetches
        // internally. WriteObject stays on the pipeline thread.
        using var results = OrchThreadPool.RunForEach(drives,
            drive => drive.NameColonSeparator,
            drive => drive,
            drive => drive.PmAuthenticationSetting.Get());

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var entity = result.GetResult(cancelHandler.Token);
                if (entity is not null)
                {
                    // Per-emit shallow copy carries the drive-local Path
                    // without mutating the shared org singleton (see
                    // PmAuthenticationRoot). No PSObject: a real Path
                    // property on a distinct instance stays isolated per
                    // drive even when collected into a variable.
                    var emit = entity.ShallowClone();
                    emit.Path = result.Source.NameColonSeparator;
                    WriteObject(emit);
                }
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetAuthenticationSettingError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
