using System.Management.Automation;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsLifecycle.Disable, "OrchFolderMachineInherit", SupportsShouldProcess = true)]
public class DisableFolderMachineInheritCmdlet : EnableFolderMachineInheritCmdletBase<False>
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(FolderMachineNameCompleter))]
    public override string[]? Name { get; set; }
}
