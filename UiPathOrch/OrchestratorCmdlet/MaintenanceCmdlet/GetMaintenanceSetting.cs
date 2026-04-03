using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// host only
// What does "host" mean here?

//[Cmdlet(VerbsCommon.Get, "OrchMaintenanceSetting")]
[OutputType(typeof(Entities.MaintenanceSetting))]
class GetMaintenanceSettingCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumOrchDrives(Path);

        foreach (var drive in drives)
        {
            var (tenantId, _) = drive.GetTenantId();
            try
            {
                MaintenanceSetting? ms = drive.OrchAPISession.GetMaintenance(tenantId);
                WriteObject(ms);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetMaintenanceSettingError", ErrorCategory.InvalidOperation, drive));
            }
        }
    }
}
