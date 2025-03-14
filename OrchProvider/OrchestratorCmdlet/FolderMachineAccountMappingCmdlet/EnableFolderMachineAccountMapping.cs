using System.Management.Automation;
using UiPath.PowerShell.Positional;
using TPositional = UiPath.PowerShell.Positional.Name_UserName;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsLifecycle.Enable, "OrchFolderMachineAccountMapping", SupportsShouldProcess = true)]
public class EnableFolderMachineAccountMappingCommand : EnableFolderMachineAccountMappingCommandBase<True>
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(FolderMachineNameCompleter<TPositional>))]
    public override string[]? Name { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(UserNameCompleter<TPositional>))]
    public override string[]? UserName { get; set; }
}
