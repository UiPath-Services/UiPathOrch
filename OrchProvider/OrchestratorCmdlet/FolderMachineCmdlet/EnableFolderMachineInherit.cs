using System.Management.Automation;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsLifecycle.Enable, "OrchFolderMachineInherit", SupportsShouldProcess = true)]
public class EnableFolderMachineInheritCommand : EnableFolderMachineInheritCommandBase<True>
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(FolderMachineNameCompleter))]
    public override string[]? Name { get; set; }
}
