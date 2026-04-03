using System.Management.Automation;
using UiPath.PowerShell.Positional;
using UiPath.PowerShell.Completer;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsLifecycle.Enable, "OrchLicenseRuntime", SupportsShouldProcess = true)]
public class EnableLicenseRuntimeCommand : EnableLicenseRuntimeCommandBase<True>
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<LicenseRobotTypeItems>))]
    [SupportsWildcards]
    public override string[]? RobotType { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(KeyCompleter))]
    [SupportsWildcards]
    public override string[]? Key { get; set; }
}
