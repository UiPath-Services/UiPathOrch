using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

/// <summary>
/// Gets a tenant's license as the services grant it: one row per product code with the
/// service that grants it, the quantity allocated, what the tenant has used, and what is
/// still free in the organization.
///
/// The service is the path segment <c>Set-PmLicenseAllocation</c> writes to, so this is
/// the view to check before and after a write. Product codes allocated 0 are omitted by
/// the API — use <c>Get-PmLicenseProductAllocation</c> for the full code list.
/// </summary>
[Cmdlet(VerbsCommon.Get, "PmLicenseServiceAllocation")]
[OutputType(typeof(Entities.ServiceLicenseProduct))]
public class GetPmLicenseServiceAllocationCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmTenantCompleter))]
    [SupportsWildcards]
    public string[]? Tenant { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Code { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
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
        var wpCode = Code.ConvertToWildcardPatternList();
        var wpService = ServiceType.ConvertToWildcardPatternList();

        // The tenant list carries both the GUID the licensing endpoints take and the
        // service catalog to ask about; the per-service license is then fetched per tenant.
        using var results = OrchThreadPool.RunForEach(drives,
            drive => drive.NameColonSeparator,
            drive => drive,
            drive => drive.PmLicenseAllocations.Get());

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var tenants = result.GetResult(cancelHandler.Token);
                if (tenants is null) continue;
                var drive = result.Source;

                foreach (var tenantRow in tenants
                    .FilterByWildcards(a => a?.tenant?.name, wpTenant)
                    .OrderBy(a => a?.tenant?.name)
                    .WithCancellation(cancelHandler.Token))
                {
                    var tenantEntity = tenantRow?.tenant;
                    var tenantId = tenantEntity?.id;
                    if (string.IsNullOrEmpty(tenantId)) continue;

                    foreach (var service in drive.PmServiceLicenses.Get(tenantId)
                        .FilterByWildcards(s => s?.serviceType, wpService)
                        .OrderBy(s => s?.serviceType))
                    {
                        var products = (service.products ?? [])
                            .FilterByWildcards(p => p?.code, wpCode)
                            .OrderBy(p => p?.code);

                        WriteObject(products.Select(p =>
                        {
                            var c = p.ShallowClone();
                            c.Path = drive.NameColonSeparator;
                            c.Tenant = tenantEntity?.name;
                            c.serviceType = service.serviceType;
                            return c;
                        }), true);
                    }
                }
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetPmLicenseServiceAllocationError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
