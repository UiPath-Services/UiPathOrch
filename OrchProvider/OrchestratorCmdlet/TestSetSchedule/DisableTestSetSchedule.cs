using System.Management.Automation;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsLifecycle.Disable, "OrchTestSetSchedule", SupportsShouldProcess = true)]
public class DisableTestSetScheduleCommand : EnableTestSetScheduleCommandBase<False>
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(NameCompleter))]
    [SupportsWildcards]
    public override string[]? Name { get; set; }
}
