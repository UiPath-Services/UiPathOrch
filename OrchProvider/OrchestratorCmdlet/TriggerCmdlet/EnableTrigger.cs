using System.Management.Automation;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsLifecycle.Enable, "OrchTrigger", SupportsShouldProcess = true)]
    public class EnableTriggerCommand : EnableTriggerCommandBase<True>
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(NameCompleter))]
        [SupportsWildcards]
        public override string[]? Name { get; set; }
    }
}
