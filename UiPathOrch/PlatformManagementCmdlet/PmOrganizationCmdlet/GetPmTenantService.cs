using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

// The services of each tenant (orchestrator, testmanager, dataservice, ...) with their status
// and URL, from the Organization Management Service. One row per tenant and service.
// Automation Cloud and Automation Suite only. The organization-level services are on
// Get-PmOrganization (organizationServiceInstances).
[Cmdlet(VerbsCommon.Get, "PmTenantService")]
[OutputType(typeof(Entities.PmService))]
public class GetPmTenantServiceCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmTenantNameCompleter))]
    [SupportsWildcards]
    public string[]? Tenant { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? ServiceType { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumPmDrives(EffectivePath(Path, LiteralPath));
        var wpTenant = Tenant.ConvertToWildcardPatternList();
        var wpService = ServiceType.ConvertToWildcardPatternList();

        // The tenant list, then each selected tenant's record (which carries its services),
        // all read in parallel.
        using var tenantResults = OrchThreadPool.RunForEach(drives,
            drive => drive.NameColonSeparator,
            drive => drive,
            drive => drive.PmTenants.Get());

        using var cancelHandler = new ConsoleCancelHandler();
        var targets = new List<(OrchDriveInfo drive, Entities.PmTenant tenant)>();
        foreach (var result in tenantResults.WithCancellation(cancelHandler.Token))
        {
            try
            {
                var tenants = result.GetResult(cancelHandler.Token);
                if (tenants is null) continue;
                targets.AddRange(tenants
                    .Where(t => !string.IsNullOrEmpty(t?.id))
                    .FilterByWildcards(t => t?.name, wpTenant)
                    .OrderBy(t => t.name)
                    .Select(t => (result.Source, t)));
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetPmTenantServiceError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }

        using var serviceResults = OrchThreadPool.RunForEach(targets,
            t => $"{t.drive.NameColonSeparator}{t.tenant.name}",
            t => t,
            t => t.drive.PmTenantServices.Get(t.tenant.id!));

        foreach (var result in serviceResults.WithCancellation(cancelHandler.Token))
        {
            try
            {
                var services = result.GetResult(cancelHandler.Token);
                if (services is null) continue;
                var (drive, tenant) = result.Source;

                WriteObject(services
                    .FilterByWildcards(s => s?.serviceType, wpService)
                    .OrderBy(s => s.serviceType)
                    .Select(s =>
                    {
                        var c = s.ShallowClone();
                        c.Path = drive.NameColonSeparator;
                        c.Tenant = tenant.name;
                        c.tenantId = tenant.id;
                        return c;
                    }), true);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetPmTenantServiceError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
