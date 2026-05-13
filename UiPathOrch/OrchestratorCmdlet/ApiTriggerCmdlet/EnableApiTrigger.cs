using System.Management.Automation;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsLifecycle.Enable, "OrchApiTrigger", SupportsShouldProcess = true)]
public class EnableApiTriggerCmdlet : EnableApiTriggerCmdletBase<True>
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(NameCompleter))]
    [SupportsWildcards]
    public override string[]? Name { get; set; }
}
