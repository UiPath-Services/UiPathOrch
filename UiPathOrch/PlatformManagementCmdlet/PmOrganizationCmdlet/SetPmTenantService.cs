using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Add-, Remove-, Enable- and Disable-PmTenantService: provision or soft-remove a service on a
// tenant, or change its status. Synchronous (see PmTenantWrite.cs).
//
// A service the catalog marks isAlwaysProvision (orchestrator, dataservice, actions, ...) is
// answered with success and left as it is, so removing or disabling one is refused here rather
// than reported as done (from the catalog, Get-PmAvailableService). Where the catalog cannot be
// read, nothing is refused; either way the service is re-read after the write and a change that
// did not happen is reported.
public abstract class PmTenantServiceWriteCmdletBase : PmTenantWriteCmdletBase
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmTenantNameCompleter))]
    [SupportsWildcards]
    public string[]? Tenant { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmServiceTypeCompleter))]
    public string[]? ServiceType { get; set; }

    protected abstract string Action { get; }
    // Whether the write takes a service away (removing, disabling), which an always-provisioned
    // service does not allow.
    protected abstract bool Takes { get; }
    // The state the service should be in afterwards: "Enabled", "Disabled", or null for absent.
    protected abstract string? Expected { get; }
    protected abstract void Write(OrchDriveInfo drive, string orgId, string tenantId, string[] services);

    protected override void ProcessRecord()
    {
        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in SessionState.EnumPmDrives(EffectivePath(Path, LiteralPath)).WithCancellation(cancelHandler.Token))
        {
            string orgId;
            HashSet<string>? always;
            try
            {
                orgId = OrgId(drive);
                always = AlwaysProvisioned(drive);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "PmTenantServiceError", ErrorCategory.InvalidOperation, Tenant));
                continue;
            }

            foreach (var pattern in (Tenant ?? []).WithCancellation(cancelHandler.Token))
            {
                List<PmTenant> tenants;
                try { tenants = ResolveTenants(drive, pattern); }
                catch (OrchException ex) { WriteError(new ErrorRecord(ex, "PmTenantServiceError", ErrorCategory.InvalidOperation, pattern)); continue; }

                foreach (var tenant in tenants.WithCancellation(cancelHandler.Token))
                {
                    string target = $"{drive.NameColonSeparator}{tenant.name}";
                    var services = new List<string>();
                    foreach (var service in ServiceType ?? [])
                    {
                        if (Takes && always is not null && always.Contains(service))
                        {
                            WriteError(new ErrorRecord(new OrchException(target,
                                $"'{service}' is always provisioned on a tenant, so it cannot be removed or disabled."),
                                "PmTenantServiceAlwaysProvisioned", ErrorCategory.InvalidOperation, service));
                            continue;
                        }
                        services.Add(service);
                    }
                    if (services.Count == 0) continue;
                    if (!ShouldProcess(target, $"{Action}: {string.Join(", ", services)}")) continue;

                    try
                    {
                        RetryWhileBusy(() => { Write(drive, orgId, tenant.id!, [.. services]); return true; }, target, Action, cancelHandler.Token);
                        drive.PmTenantServices.ClearCache(tenant.id!);
                        drive.PmLicenseAllocations.ClearCache();
                        ReportUnchanged(drive, tenant, target, services);
                    }
                    catch (OrchException ex)
                    {
                        WriteError(new ErrorRecord(ex, "PmTenantServiceError", ErrorCategory.InvalidOperation, tenant));
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        WriteError(new ErrorRecord(new OrchException(target, ex), "PmTenantServiceError", ErrorCategory.InvalidOperation, tenant));
                    }
                }
            }
        }
    }

    // The always-provisioned service types, or null when there is no catalog to say so.
    private static HashSet<string>? AlwaysProvisioned(OrchDriveInfo drive)
    {
        try
        {
            return drive.PmAvailableServices.Get("")
                .Where(s => s.isAlwaysProvision == true && s.id is not null)
                .Select(s => s.id!).ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception)
        {
            return null;
        }
    }

    // The API answers success also when it changed nothing, so read the services back and say
    // which ones are not as asked.
    private void ReportUnchanged(OrchDriveInfo drive, PmTenant tenant, string target, List<string> services)
    {
        var now = drive.PmTenantServices.Get(tenant.id!).ToList();
        foreach (var service in services)
        {
            var row = now.FirstOrDefault(s => string.Equals(s.serviceType, service, StringComparison.OrdinalIgnoreCase));
            bool done = Expected is null ? row is null : string.Equals(row?.status, Expected, StringComparison.OrdinalIgnoreCase)
                || (Expected == "Enabled" && row is not null && PmTenantWriteCmdletBase.IsTransitional(row.status));
            if (!done)
            {
                WriteWarning($"\"{target}\": '{service}' is {(row is null ? "not on the tenant" : row.status)} after {Action}. The service may be one the tenant always keeps, or not available to the organization.");
            }
        }
    }
}

[Cmdlet(VerbsCommon.Add, "PmTenantService", SupportsShouldProcess = true)]
public class AddPmTenantServiceCmdlet : PmTenantServiceWriteCmdletBase
{
    protected override string Action => "Add Service";
    protected override bool Takes => false;
    protected override string? Expected => "Enabled";
    protected override void Write(OrchDriveInfo drive, string orgId, string tenantId, string[] services)
        => drive.OrchAPISession.AddRemovePmTenantServices(orgId, tenantId, services.ToDictionary(s => s, _ => true));
}

[Cmdlet(VerbsCommon.Remove, "PmTenantService", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
public class RemovePmTenantServiceCmdlet : PmTenantServiceWriteCmdletBase
{
    protected override string Action => "Remove Service";
    protected override bool Takes => true;
    protected override string? Expected => null;
    protected override void Write(OrchDriveInfo drive, string orgId, string tenantId, string[] services)
        => drive.OrchAPISession.AddRemovePmTenantServices(orgId, tenantId, services.ToDictionary(s => s, _ => false));
}

[Cmdlet(VerbsLifecycle.Enable, "PmTenantService", SupportsShouldProcess = true)]
public class EnablePmTenantServiceCmdlet : PmTenantServiceWriteCmdletBase
{
    protected override string Action => "Enable Service";
    protected override bool Takes => false;
    protected override string? Expected => "Enabled";
    protected override void Write(OrchDriveInfo drive, string orgId, string tenantId, string[] services)
        => drive.OrchAPISession.SetPmTenantServiceStatus(orgId, tenantId, services.ToDictionary(s => s, _ => "Enabled"));
}

[Cmdlet(VerbsLifecycle.Disable, "PmTenantService", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
public class DisablePmTenantServiceCmdlet : PmTenantServiceWriteCmdletBase
{
    protected override string Action => "Disable Service";
    protected override bool Takes => true;
    protected override string? Expected => "Disabled";
    protected override void Write(OrchDriveInfo drive, string orgId, string tenantId, string[] services)
        => drive.OrchAPISession.SetPmTenantServiceStatus(orgId, tenantId, services.ToDictionary(s => s, _ => "Disabled"));
}

// Service types: those of the -Tenant tenants (Remove-, Enable-, Disable-) plus the catalog
// (Add-), for -ServiceType.
internal class PmServiceTypeCompleter : OrchArgumentCompleter
{
    public override IEnumerable<CompletionResult> CompleteArgumentCore(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var drives = ResolvePmDrives(fakeBoundParameters);
        var wpExclude = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);
        var names = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var drive in drives)
        {
            try
            {
                foreach (var s in drive.PmAvailableServices.Get("")) if (s.id is not null) names.Add(s.id);
            }
            catch (Exception) { }
            try
            {
                foreach (var t in drive.PmTenants.Get())
                {
                    if (t.id is null) continue;
                    foreach (var s in drive.PmTenantServices.Get(t.id)) if (s.serviceType is not null) names.Add(s.serviceType);
                }
            }
            catch (Exception) { }
        }
        foreach (var name in names.ExcludeByWildcards(n => n, wpExclude))
        {
            yield return new CompletionResult(name, name, CompletionResultType.ParameterValue, name);
        }
    }
}
