using System.Management.Automation;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsLifecycle.Enable, "OrchMaintenanceMode", SupportsShouldProcess = true)]
public class EnableMaintenanceModeCommand : EnableMaintenanceModeCommandBase<True>
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
