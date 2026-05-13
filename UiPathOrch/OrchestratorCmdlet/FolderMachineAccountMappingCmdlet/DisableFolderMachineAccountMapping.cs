using System.Management.Automation;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsLifecycle.Disable, "OrchFolderMachineAccountMapping", SupportsShouldProcess = true)]
public class DisableFolderMachineAccountMappingCmdlet : EnableFolderMachineAccountMappingCmdletBase<False>
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(FolderMachineNameCompleter))]
    public override string[]? Name { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(UserNameCompleter))]
    public override string[]? UserName { get; set; }
}
