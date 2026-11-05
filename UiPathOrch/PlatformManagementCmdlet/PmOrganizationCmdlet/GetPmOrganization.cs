using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

// The organization's own record, from the Organization Management Service: name, id, region,
// status, and its organization-level services. Automation Cloud and Automation Suite only.
[Cmdlet(VerbsCommon.Get, "PmOrganization")]
[OutputType(typeof(Entities.PmOrganization))]
public class GetPmOrganizationCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumPmDrives(EffectivePath(Path, LiteralPath));

        // Fetch in parallel; per-org caches serialize same-partition fetches internally.
        using var results = OrchThreadPool.RunForEach(drives,
            drive => drive.NameColonSeparator,
            drive => drive,
            drive => drive.PmOrganization.Get());

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results.WithCancellation(cancelHandler.Token))
        {
            try
            {
                var entity = result.GetResult(cancelHandler.Token);
                if (entity is null) continue;
                // A per-emit copy carries the drive-local Path without touching the shared
                // org singleton (see GetPmAuthenticationSetting).
                var emit = entity.ShallowClone();
                emit.Path = result.Source.NameColonSeparator;
                WriteObject(emit);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetPmOrganizationError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
