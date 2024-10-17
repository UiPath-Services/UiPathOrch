using System.Management.Automation;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsLifecycle.Disable, "OrchLicenseRuntime", SupportsShouldProcess = true)]
    public class DisableLicenseRuntimeCommand : EnableLicenseRuntimeCommandBase<False>
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(RobotTypeCompleter))]
        [SupportsWildcards]
        public override string[]? RobotType { get; set; }

        [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(KeyCompleter))]
        [SupportsWildcards]
        public override string[]? Key { get; set; }
    }
}
