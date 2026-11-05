using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

// The catalog of services a tenant can have, in a region: which ones Add-PmTenantService can
// provision, and which ones every tenant always keeps (isAlwaysProvision). Automation Cloud and
// Automation Suite.
[Cmdlet(VerbsCommon.Get, "PmAvailableService")]
[OutputType(typeof(Entities.PmAvailableService))]
public class GetPmAvailableServiceCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? ServiceType { get; set; }

    // A region such as Europe or Japan; by default the organization's.
    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? Region { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumPmDrives(EffectivePath(Path, LiteralPath));
        var wpService = ServiceType.ConvertToWildcardPatternList();
        string region = Region ?? "";

        using var results = OrchThreadPool.RunForEach(drives,
            drive => drive.NameColonSeparator,
            drive => drive,
            drive => drive.PmAvailableServices.Get(region));

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results.WithCancellation(cancelHandler.Token))
        {
            try
            {
                var entities = result.GetResult(cancelHandler.Token);
                if (entities is null) continue;
                WriteObject(entities
                    .FilterByWildcards(s => s?.id, wpService)
                    .OrderBy(s => s.id)
                    .Select(s => { var c = s.ShallowClone(); c.Path = result.Source.NameColonSeparator; c.Region = Region; return c; }), true);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetPmAvailableServiceError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
