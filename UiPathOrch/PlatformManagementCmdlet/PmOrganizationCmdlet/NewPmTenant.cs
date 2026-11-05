using System.Management.Automation;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Creates a tenant in the drive's organization and, unless -NoWait, waits until it is
// provisioned (a few minutes). See PmTenantWrite.cs.
[Cmdlet(VerbsCommon.New, "PmTenant", SupportsShouldProcess = true)]
[OutputType(typeof(PmTenant))]
public class NewPmTenantCmdlet : PmTenantWriteCmdletBase
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    public string? Name { get; set; }

    // The services to provision; the organization's always-provisioned ones come anyway.
    // Defaults to orchestrator, which Automation Suite requires the list to contain something.
    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string[]? Service { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? Region { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ValidateSet("Production", "NonProduction", "Development")]
    public string? Environment { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? Color { get; set; }

    [Parameter]
    public SwitchParameter NoWait { get; set; }

    protected override void ProcessRecord()
    {
        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in SessionState.EnumPmDrives(EffectivePath(Path, LiteralPath)).WithCancellation(cancelHandler.Token))
        {
            if (!ShouldProcess($"{drive.NameColonSeparator}{Name}", "New Tenant")) continue;
            try
            {
                string orgId = OrgId(drive);
                var created = drive.OrchAPISession.PostPmTenant(orgId, new PmCreateTenantRequest
                {
                    name = Name,
                    region = Region,
                    environment = Environment,
                    color = Color,
                    services = Service is { Length: > 0 } ? Service : ["orchestrator"],
                });
                ClearTenantCaches(drive, created?.id);
                if (created?.id is null) continue;

                var tenant = NoWait ? created : WaitForTenant(drive, orgId, created.id, created.operationId, "New Tenant", cancelHandler.Token) ?? created;
                ClearTenantCaches(drive, created.id);
                WriteObject(Emit(tenant, drive));
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "NewPmTenantError", ErrorCategory.InvalidOperation, Name));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "NewPmTenantError", ErrorCategory.InvalidOperation, Name));
            }
        }
    }
}
