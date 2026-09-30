using System.Collections;
using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands;

/// <summary>
/// Sets the per-tenant license allocation (the Robots &amp; Services / Consumables
/// panels of the Admin / Licenses page).
///
/// The underlying PUT replaces one service's whole allocation: any product code
/// missing from a non-empty products array is set to 0. This cmdlet therefore reads
/// the tenant's current service licenses first and re-sends them with the requested
/// codes overlaid, so unrelated allocations survive.
/// </summary>
[Cmdlet(VerbsCommon.Set, "PmLicenseAllocation", SupportsShouldProcess = true)]
[OutputType(typeof(Entities.TenantProductAllocation))]
public class SetPmLicenseAllocationCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmTenantCompleter))]
    public string? Tenant { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public int? UnattendedRobot { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public int? UnattendedHostingRobot { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public int? NonProductionRobot { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public int? TestingRobot { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public int? AppTestRobot { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public int? PerformanceTesting { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public int? DataServiceUnit { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public double? PlatformUnits { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public double? ScreenPlayRuns { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public double? TestHeals { get; set; }

    /// <summary>Product codes the named parameters don't cover, as @{ CODE = quantity }.</summary>
    [Parameter(ValueFromPipelineByPropertyName = true)]
    public Hashtable? Products { get; set; }

    /// <summary>
    /// Forces every requested code through this service (orchestrator / tenant /
    /// dataservice / ...). Only needed for a code the cmdlet cannot place itself.
    /// </summary>
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<Orchestrator_Tenant_DataService>))]
    public string? ServiceType { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumPmDrives(EffectivePath(Path, LiteralPath));

        var requested = CollectRequestedQuantities();
        if (requested.Count == 0)
        {
            WriteWarning("No product quantity was specified; nothing to set.");
            return;
        }

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives.WithCancellation(cancelHandler.Token))
        {
            var tenantRow = drive.PmLicenseAllocations.Get()
                .FirstOrDefault(a => string.Equals(a?.tenant?.name, Tenant, StringComparison.OrdinalIgnoreCase));

            string target = $"{drive.NameColonSeparator}{Tenant}";
            var tenantEntity = tenantRow?.tenant;
            if (tenantEntity is null || string.IsNullOrEmpty(tenantEntity.id))
            {
                WriteError(new ErrorRecord(
                    new ItemNotFoundException($"{target}: tenant not found in this organization."),
                    "SetPmLicenseAllocationTenantNotFound", ErrorCategory.ObjectNotFound, Tenant));
                continue;
            }

            // TenantAllocationTenant.id is the tenant's portal GUID, already a string.
            var tenantId = tenantEntity.id!;
            var partitionGlobalId = drive.GetPartitionGlobalId();
            if (string.IsNullOrEmpty(partitionGlobalId)) continue;

            try
            {
                // The merge base: what each service grants the tenant right now.
                var current = drive.PmServiceLicenses.Get(tenantId).ToList();

                var payloads = ComputeServicePayloads(current, requested, ServiceType, out var unplaced);

                foreach (var code in unplaced)
                {
                    WriteError(new ErrorRecord(
                        new ArgumentException($"{target}: cannot tell which service grants the product code '{code}'. Re-run with -ServiceType."),
                        "SetPmLicenseAllocationUnknownService", ErrorCategory.InvalidArgument, code));
                }

                bool wrote = false;
                foreach (var (serviceType, products) in payloads.OrderBy(p => p.Key))
                {
                    string serviceTarget = $"{target} [{serviceType}]";
                    if (!ShouldProcess(serviceTarget, "Set PmLicenseAllocation")) continue;

                    drive.OrchAPISession.PutPmLicenseAllocation(partitionGlobalId, tenantId, serviceType,
                        new ServiceLicenseUpdate { products = products });
                    wrote = true;
                }

                if (!wrote) continue;

                drive.PmLicenseAllocations.ClearCache();
                drive.PmLicenseProductAllocations.ClearCache(tenantId);
                drive.PmServiceLicenses.ClearCache(tenantId);
                drive.PmLicenseInventory.ClearCache();

                var refreshed = drive.PmLicenseProductAllocations.Get(tenantId)
                    .Where(p => p?.code is not null && requested.ContainsKey(p.code!))
                    .OrderBy(p => p?.code);

                WriteObject(refreshed.Select(p =>
                {
                    var c = p.ShallowClone();
                    c.Path = drive.NameColonSeparator;
                    c.Tenant = tenantEntity.name;
                    return c;
                }), true);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(target, ex), "SetPmLicenseAllocationError", ErrorCategory.InvalidOperation, target));
            }
        }
    }

    /// <summary>
    /// Merges the named quantity parameters with -Products into one code -> quantity map.
    /// -Products wins on a collision, so a hashtable can override a named parameter.
    /// </summary>
    private Dictionary<string, double> CollectRequestedQuantities()
    {
        var requested = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        void Add(string code, double? value) { if (value is not null) requested[code] = value.Value; }

        Add("UNATT", UnattendedRobot);
        Add("UNATT-HOSTING", UnattendedHostingRobot);
        Add("NONPR", NonProductionRobot);
        Add("TAUNATT", TestingRobot);
        Add("APPTESTR", AppTestRobot);
        Add("PERFTEST", PerformanceTesting);
        Add("DSU", DataServiceUnit);
        Add("PLTU", PlatformUnits);
        Add("SPR", ScreenPlayRuns);
        Add("HEALTEST", TestHeals);

        if (Products is not null)
        {
            foreach (DictionaryEntry entry in Products)
            {
                var code = entry.Key?.ToString();
                if (string.IsNullOrWhiteSpace(code)) continue;
                if (entry.Value is null) continue;
                requested[code!] = Convert.ToDouble(entry.Value);
            }
        }

        return requested;
    }

    /// <summary>
    /// Product codes whose service cannot be read off the tenant's current license
    /// (the service-licenses endpoint omits codes allocated 0, so a code being raised
    /// from 0 is not listed there). Taken from the PUT bodies the Admin / Licenses page
    /// itself sends, so it covers every code that page can edit; a code outside it needs
    /// -ServiceType.
    /// </summary>
    internal static string? ServiceTypeOfProductCode(string code) => code.ToUpperInvariant() switch
    {
        "UNATT" or "UNATT-HOSTING" or "NONPR" or "TAUNATT" or "APPTESTR" or "ACR"
            or "PERFTEST" or "PERFTEST-RUNTIME" => "orchestrator",
        "DSU" => "dataservice",
        "RU" or "AGU" or "PLTU" or "TEU" or "HEAL" or "HEALTEST" or "SPR" or "MRSU"
            or "LU" or "APPU" or "S2PU" or "RCMU" or "GIC" => "tenant",
        _ => null,
    };

    /// <summary>
    /// Builds one full products array per affected service: the service's current codes
    /// and quantities with <paramref name="requested"/> overlaid. Codes that cannot be
    /// placed in a service are reported through <paramref name="unplaced"/> instead of
    /// being sent somewhere arbitrary. No API access, so this is unit-testable.
    /// </summary>
    internal static Dictionary<string, ProductQuantity[]> ComputeServicePayloads(
        IEnumerable<ServiceLicense> current,
        IReadOnlyDictionary<string, double> requested,
        string? forcedServiceType,
        out List<string> unplaced)
    {
        unplaced = [];

        // Current state: service -> (code -> quantity). Codes the API omits are at 0.
        var currentByService = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
        var serviceOfCode = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var service in current)
        {
            if (string.IsNullOrEmpty(service?.serviceType)) continue;
            var codes = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            foreach (var product in service.products ?? [])
            {
                if (string.IsNullOrEmpty(product?.code)) continue;
                codes[product.code!] = product.quantity ?? product.allocated ?? 0;
                serviceOfCode[product.code!] = service.serviceType!;
            }
            currentByService[service.serviceType!] = codes;
        }

        var touched = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);

        foreach (var (code, quantity) in requested)
        {
            // -ServiceType wins, then where the code is allocated today, then the
            // static grouping. Unknown codes are surfaced, never guessed.
            var serviceType = forcedServiceType
                ?? serviceOfCode.GetValueOrDefault(code)
                ?? ServiceTypeOfProductCode(code);

            if (string.IsNullOrEmpty(serviceType)) { unplaced.Add(code); continue; }

            if (!touched.TryGetValue(serviceType, out var merged))
            {
                // Start from the service's current allocation so untouched codes keep
                // their quantity through the replace-all PUT.
                merged = currentByService.TryGetValue(serviceType, out var existing)
                    ? new Dictionary<string, double>(existing, StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                touched[serviceType] = merged;
            }

            merged[code] = quantity;
        }

        return touched.ToDictionary(
            kv => kv.Key,
            kv => kv.Value
                .OrderBy(c => c.Key, StringComparer.OrdinalIgnoreCase)
                .Select(c => new ProductQuantity { code = c.Key, quantity = c.Value })
                .ToArray(),
            StringComparer.OrdinalIgnoreCase);
    }
}
