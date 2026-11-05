using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// =============================================================================
// Writes to the organization's tenants and their services (the Organization Management Service,
// the API of `uip admin tenants`). Shared by New-, Update-, Enable-, Disable-, Remove- and
// Restore-PmTenant and by Add-, Remove-, Enable- and Disable-PmTenantService.
//
// A tenant write is asynchronous and one at a time per tenant: the call answers at once, the
// tenant reads "Updating" until it is done, and another write meanwhile is refused ("Another
// operation is in-progress"). So the cmdlets wait for the tenant to settle, unless -NoWait,
// which a chain of writes cannot use. A service write is synchronous.
// =============================================================================

public abstract class PmTenantWriteCmdletBase : OrchestratorPSCmdlet
{
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    // States a tenant passes through while a write is in progress.
    private static readonly HashSet<string> Transitional = new(StringComparer.OrdinalIgnoreCase)
    {
        "Updating", "Provisioning", "Enabling", "Disabling", "Deleting", "Restoring",
    };

    internal static bool IsTransitional(string? status) => status is not null && Transitional.Contains(status);

    // How long a tenant may stay in a transitional state. A new tenant took about four minutes
    // on Automation Cloud (2026-11-05); the other writes, seconds.
    private static readonly TimeSpan WaitLimit = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    internal static string OrgId(OrchDriveInfo drive)
        => drive.GetPartitionGlobalId() is { Length: > 0 } id ? id
            : throw new OrchException(drive.NameColonSeparator, "The organization of this drive is not known (no partition id in its sign-in).");

    // The tenants a pattern names (see TmTestCaseAssignment.Resolve: a name must match exactly
    // one, a wildcard selects every match). Errors are written, and the result is then empty.
    internal List<PmTenant> ResolveTenants(OrchDriveInfo drive, string pattern)
    {
        var tenants = drive.PmTenants.Get().ToList();
        var matches = TmTestCaseAssignment.Resolve(tenants, t => t.name, pattern, "tenant", out var error, $" in {drive.NameColonSeparator}");
        if (error is not null)
        {
            WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, error), "PmTenantNotFound", ErrorCategory.ObjectNotFound, pattern));
        }
        return matches;
    }

    // Drop what a write to this tenant changes: the tenant list, its services, and the license
    // allocation rows (which list the tenants too).
    internal static void ClearTenantCaches(OrchDriveInfo drive, string? tenantId)
    {
        drive.PmTenants.ClearCache();
        drive.PmLicenseAllocations.ClearCache();
        if (!string.IsNullOrEmpty(tenantId)) drive.PmTenantServices.ClearCache(tenantId);
    }

    // operationStatus values of an operation still running. Done, it names what it did ("Created",
    // "Edited", ...), which is why this lists the pending states rather than the final ones.
    private static readonly HashSet<string> PendingOperation = new(StringComparer.OrdinalIgnoreCase)
    {
        "Queued", "Pending", "NotStarted", "Started", "InProgress", "Running", "Processing", "Scheduled",
    };

    // Waits until the write is done: its operation (when the call returned one) has left the
    // pending states and the tenant has left the transitional ones. Both, because the tenant
    // reads "Enabled" again before the operation of a new tenant ends, and another write is
    // refused until it does. Returns the tenant as it then is, or null when it can no longer be
    // read (a delete). Throws when the operation reports an error, or on timeout.
    internal PmTenant? WaitForTenant(OrchDriveInfo drive, string orgId, string tenantId, string? operationId, string label, CancellationToken token)
    {
        using var reporter = new ProgressReporter(this, null, label);
        var started = DateTime.UtcNow;
        int polls = 0;
        while (true)
        {
            token.Sleep((int)PollInterval.TotalMilliseconds);
            token.ThrowIfCancellationRequested();

            PmOperation? operation = null;
            if (operationId is not null)
            {
                try { operation = drive.OrchAPISession.GetPmOperation(orgId, operationId); }
                catch (HttpResponseException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound) { }
                if (!string.IsNullOrEmpty(operation?.errorMessage))
                {
                    throw new OrchException(drive.NameColonSeparator, $"{label} failed: {operation.errorMessage}");
                }
            }
            bool operationPending = operation?.operationStatus is { } s && PendingOperation.Contains(s);

            PmTenant? tenant;
            try
            {
                tenant = drive.OrchAPISession.GetPmTenant(orgId, tenantId);
            }
            catch (HttpResponseException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                if (!operationPending) return null;
                tenant = null;
            }
            reporter.WriteProgress(++polls, $"{tenant?.name ?? tenantId}: {tenant?.status ?? "gone"}{(operationPending ? $" ({operation!.operationStatus})" : "")}");
            if (!operationPending && !IsTransitional(tenant?.status)) return tenant;
            if (DateTime.UtcNow - started > WaitLimit)
            {
                throw new OrchException(drive.NameColonSeparator, $"The tenant '{tenant?.name ?? tenantId}' is still {(operationPending ? operation!.operationStatus : tenant?.status)} after {WaitLimit.TotalMinutes:0} minutes. Check it with Get-PmTenant.");
            }
        }
    }

    // Whether the organization refused a write because another one is still running on the
    // tenant. Nothing says reliably when one ends: on Automation Cloud a new tenant reads
    // "Enabled", and its operation record "Created", while services are still being provisioned
    // and further writes are refused for minutes more. So a refused write is retried instead.
    internal static bool IsBusy(Exception ex)
        => ex is HttpResponseException && ex.Message.Contains("Another operation is in-progress", StringComparison.OrdinalIgnoreCase);

    private static readonly TimeSpan BusyRetryInterval = TimeSpan.FromSeconds(10);

    // Calls `call`, and while the tenant is busy with another operation, waits and calls again,
    // up to WaitLimit, with a progress bar saying what it is waiting for.
    internal T RetryWhileBusy<T>(Func<T> call, string target, string label, CancellationToken token)
    {
        var started = DateTime.UtcNow;
        ProgressReporter? reporter = null;
        try
        {
            int tries = 0;
            while (true)
            {
                try
                {
                    return call();
                }
                catch (Exception ex) when (IsBusy(ex) && DateTime.UtcNow - started < WaitLimit)
                {
                    reporter ??= new ProgressReporter(this, null, label);
                    reporter.WriteProgress(++tries, $"{target}: waiting for another operation on the tenant to finish");
                    token.Sleep((int)BusyRetryInterval.TotalMilliseconds);
                    token.ThrowIfCancellationRequested();
                }
            }
        }
        finally
        {
            reporter?.Dispose();
        }
    }

    // The common shape of a write to existing tenants: for each drive and name pattern, resolve
    // the tenants, confirm each ("<action>" on "Drive:\name"), call `write`, then wait for the
    // tenant to settle unless noWait, and emit it when `emit`. The target names the tenant's id
    // too, so a -WhatIf / -Confirm line says what Restore-PmTenant would need.
    internal void WriteTenants(string[]? names, string action, string errorId, bool noWait, bool emit,
        Func<OrchDriveInfo, string, PmTenant, string?> write)
    {
        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in SessionState.EnumPmDrives(EffectivePath(Path, LiteralPath)).WithCancellation(cancelHandler.Token))
        {
            foreach (var pattern in (names ?? []).WithCancellation(cancelHandler.Token))
            {
                List<PmTenant> tenants;
                string orgId;
                try
                {
                    orgId = OrgId(drive);
                    tenants = ResolveTenants(drive, pattern);
                }
                catch (OrchException ex)
                {
                    WriteError(new ErrorRecord(ex, errorId, ErrorCategory.InvalidOperation, pattern));
                    continue;
                }

                foreach (var tenant in tenants.WithCancellation(cancelHandler.Token))
                {
                    if (!ShouldProcess($"{drive.NameColonSeparator}{tenant.name} ({tenant.id})", action)) continue;
                    try
                    {
                        string target = $"{drive.NameColonSeparator}{tenant.name}";
                        string? operationId = RetryWhileBusy(() => write(drive, orgId, tenant), target, action, cancelHandler.Token);
                        ClearTenantCaches(drive, tenant.id);
                        var after = noWait ? null : WaitForTenant(drive, orgId, tenant.id!, operationId, action, cancelHandler.Token);
                        ClearTenantCaches(drive, tenant.id);
                        if (emit) WriteObject(Emit(after ?? tenant, drive));
                    }
                    catch (OrchException ex)
                    {
                        WriteError(new ErrorRecord(ex, errorId, ErrorCategory.InvalidOperation, tenant));
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        WriteError(new ErrorRecord(new OrchException($"{drive.NameColonSeparator}{tenant.name}", ex), errorId, ErrorCategory.InvalidOperation, tenant));
                    }
                }
            }
        }
    }

    // A tenant as emitted: a copy with the drive-local Path.
    internal static PmTenant Emit(PmTenant tenant, OrchDriveInfo drive)
    {
        var c = tenant.ShallowClone();
        c.Path = drive.NameColonSeparator;
        c.tenantServiceInstances = null;
        return c;
    }
}
