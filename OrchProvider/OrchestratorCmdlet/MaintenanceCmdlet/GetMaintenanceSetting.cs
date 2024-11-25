using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using TPositional = UiPath.PowerShell.Positional.Path;

namespace UiPath.PowerShell.Commands
{
    // host only
    // host って何や？

    //[Cmdlet(VerbsCommon.Get, "OrchMaintenanceSetting")]
    [OutputType(typeof(Entities.MaintenanceSetting))]
    class GetMaintenanceSettingCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
        public string[]? Path { get; set; }

        protected override void ProcessRecord()
        {
            var drives = OrchDriveInfo.EnumOrchDrives(Path);

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
}
