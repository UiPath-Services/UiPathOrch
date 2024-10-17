using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.MachineName_HostMachineName_ServiceUserName_SessionId;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsLifecycle.Disable, "OrchMaintenanceMode", SupportsShouldProcess = true)]
    public class DisableMaintenanceModeCommand : EnableMaintenanceModeCommandBase<False>
    {
        [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(MachineNameCompleter))]
        public override string[]? MachineName { get; set; }

        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(HostMachineNameCompleter))]
        public override string[]? HostMachineName { get; set; }

        [Parameter(Position = 2, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(ServiceUserNameCompleter))]
        public override string[]? ServiceUserName { get; set; }

        [Parameter(Position = 3, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(SessionIdCompleter))]
        public override Int64[]? SessionId { get; set; }
    }
}
