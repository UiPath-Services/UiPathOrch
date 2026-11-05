using System.Management.Automation;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Update-, Enable-, Disable-, Remove- and Restore-PmTenant. See PmTenantWrite.cs: each waits for
// the tenant to settle unless -NoWait.

[Cmdlet(VerbsData.Update, "PmTenant", SupportsShouldProcess = true)]
[OutputType(typeof(PmTenant))]
public class UpdatePmTenantCmdlet : PmTenantWriteCmdletBase
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmTenantNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter]
    public string? NewName { get; set; }

    [Parameter]
    [ValidateSet("Production", "NonProduction", "Development")]
    public string? Environment { get; set; }

    [Parameter]
    public string? Color { get; set; }

    [Parameter]
    public SwitchParameter NoWait { get; set; }

    protected override void ProcessRecord()
    {
        if (NewName is null && Environment is null && Color is null)
        {
            WriteError(new ErrorRecord(new ArgumentException("Give -NewName, -Environment or -Color."), "UpdatePmTenantNothing", ErrorCategory.InvalidArgument, Name));
            return;
        }
        WriteTenants(Name, "Update Tenant", "UpdatePmTenantError", NoWait, true, (drive, org, tenant) =>
        {
            // Only what differs, so an unchanged request sends nothing.
            var request = new PmUpdateTenantRequest
            {
                name = NewName is not null && NewName != tenant.name ? NewName : null,
                environment = Environment is not null && Environment != tenant.environment ? Environment : null,
                color = Color is not null && Color != tenant.color ? Color : null,
            };
            if (request.name is null && request.environment is null && request.color is null) return null;
            return drive.OrchAPISession.PatchPmTenant(org, tenant.id!, request);
        });
    }
}

[Cmdlet(VerbsLifecycle.Enable, "PmTenant", SupportsShouldProcess = true)]
[OutputType(typeof(PmTenant))]
public class EnablePmTenantCmdlet : PmTenantWriteCmdletBase
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmTenantNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter]
    public SwitchParameter NoWait { get; set; }

    protected override void ProcessRecord()
        => WriteTenants(Name, "Enable Tenant", "EnablePmTenantError", NoWait, true, (drive, org, tenant) =>
        {
            // The organization answers an enable of an enabled tenant with an error ("Command
            // with operation id = ... not found!"), so one that is already enabled is left alone.
            if (string.Equals(tenant.status, "Enabled", StringComparison.OrdinalIgnoreCase))
            {
                WriteVerbose($"The tenant {drive.NameColonSeparator}{tenant.name} is already enabled.");
                return null;
            }
            return drive.OrchAPISession.SetPmTenantActive(org, tenant.id!, true, null);
        });
}

[Cmdlet(VerbsLifecycle.Disable, "PmTenant", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
[OutputType(typeof(PmTenant))]
public class DisablePmTenantCmdlet : PmTenantWriteCmdletBase
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmTenantNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    // Recorded with the action.
    [Parameter]
    public string? Reason { get; set; }

    [Parameter]
    public SwitchParameter NoWait { get; set; }

    protected override void ProcessRecord()
        => WriteTenants(Name, "Disable Tenant", "DisablePmTenantError", NoWait, true, (drive, org, tenant) =>
        {
            if (string.Equals(tenant.status, "Disabled", StringComparison.OrdinalIgnoreCase))
            {
                WriteVerbose($"The tenant {drive.NameColonSeparator}{tenant.name} is already disabled.");
                return null;
            }
            return drive.OrchAPISession.SetPmTenantActive(org, tenant.id!, false, Reason);
        });
}

[Cmdlet(VerbsCommon.Remove, "PmTenant", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
public class RemovePmTenantCmdlet : PmTenantWriteCmdletBase
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmTenantNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter]
    public SwitchParameter NoWait { get; set; }

    // A soft delete: the tenant leaves the list and cannot be read back, so its id, the only
    // handle Restore-PmTenant has, is said in the confirmation and in a verbose line.
    protected override void ProcessRecord()
        => WriteTenants(Name, "Remove Tenant", "RemovePmTenantError", NoWait, false, (drive, org, tenant) =>
        {
            var operationId = drive.OrchAPISession.DeletePmTenant(org, tenant.id!);
            WriteVerbose($"Removed the tenant {drive.NameColonSeparator}{tenant.name} (id {tenant.id}). On Automation Suite, Restore-PmTenant -Id {tenant.id} brings it back.");
            return operationId;
        });
}

// Undoes Remove-PmTenant, by id: a removed tenant is no longer listed, so its name cannot be
// resolved. Automation Suite allows it; Automation Cloud refuses it (403).
[Cmdlet(VerbsData.Restore, "PmTenant", SupportsShouldProcess = true)]
[OutputType(typeof(PmTenant))]
public class RestorePmTenantCmdlet : PmTenantWriteCmdletBase
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ValidatePattern("^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$")]
    public string[]? Id { get; set; }

    [Parameter]
    public SwitchParameter NoWait { get; set; }

    protected override void ProcessRecord()
    {
        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in SessionState.EnumPmDrives(EffectivePath(Path, LiteralPath)).WithCancellation(cancelHandler.Token))
        {
            foreach (var id in (Id ?? []).WithCancellation(cancelHandler.Token))
            {
                if (!ShouldProcess($"{drive.NameColonSeparator} tenant {id}", "Restore Tenant")) continue;
                try
                {
                    string org = OrgId(drive);
                    string? operationId;
                    try
                    {
                        operationId = RetryWhileBusy(() => drive.OrchAPISession.RestorePmTenant(org, id), $"{drive.NameColonSeparator}{id}", "Restore Tenant", cancelHandler.Token);
                    }
                    catch (HttpResponseException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        throw new OrchException(drive.NameColonSeparator,
                            $"The organization does not allow restoring the tenant {id} (403). Automation Cloud refuses it through the API; Automation Suite allows it.");
                    }
                    ClearTenantCaches(drive, id);
                    var after = NoWait ? null : WaitForTenant(drive, org, id, operationId, "Restore Tenant", cancelHandler.Token);
                    ClearTenantCaches(drive, id);
                    if (after is not null) WriteObject(Emit(after, drive));
                }
                catch (OrchException ex)
                {
                    WriteError(new ErrorRecord(ex, "RestorePmTenantError", ErrorCategory.InvalidOperation, id));
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "RestorePmTenantError", ErrorCategory.InvalidOperation, id));
                }
            }
        }
    }
}
