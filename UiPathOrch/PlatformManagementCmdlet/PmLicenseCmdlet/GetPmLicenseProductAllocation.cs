using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "PmLicenseProductAllocation")]
[OutputType(typeof(Entities.TenantProductAllocation))]
public class GetPmLicenseProductAllocationCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmTenantCompleter))]
    [SupportsWildcards]
    public string[]? Tenant { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Code { get; set; }

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

        // The tenant list comes from the org-wide allocations (the only endpoint pairing
        // a tenant name with the GUID the per-tenant endpoint takes); the product rows
        // are then fetched per tenant.
        using var results = OrchThreadPool.RunForEach(drives,
            drive => drive.NameColonSeparator,
            drive => drive,
            drive => drive.PmLicenseAllocations.Get());

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                // GetResult() first: Source is only readable once the result was collected.
                var tenants = result.GetResult(cancelHandler.Token);
                if (tenants is null) continue;
                var drive = result.Source;

                foreach (var tenant in tenants
                    .FilterByWildcards(a => a?.tenant?.name, wpTenant)
                    .OrderBy(a => a?.tenant?.name)
                    .WithCancellation(cancelHandler.Token))
                {
                    var tenantEntity = tenant?.tenant;
                    var tenantId = tenantEntity?.id;
                    if (string.IsNullOrEmpty(tenantId)) continue;

                    var products = drive.PmLicenseProductAllocations.Get(tenantId)
                        .FilterByWildcards(p => p?.code, wpCode)
                        .OrderBy(p => p?.code);

                    WriteObject(products.Select(p =>
                    {
                        var c = p.ShallowClone();
                        c.Path = drive.NameColonSeparator;
                        c.Tenant = tenantEntity?.name;
                        return c;
                    }), true);
                }
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetPmLicenseProductAllocationError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
