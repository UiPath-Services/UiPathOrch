using System.Management.Automation;
using UiPath.PowerShell.Positional;
using TPositional = UiPath.PowerShell.Positional.Name_UserName;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsLifecycle.Disable, "OrchFolderMachineAccountMapping", SupportsShouldProcess = true)]
    public class DisableFolderMachineAccountMappingCommand : EnableFolderMachineAccountMappingCommandBase<False>
    {
        [Parameter(Position = 0, Mandatory = true)]
        [SupportsWildcards]
        [ArgumentCompleter(typeof(FolderMachineNameCompleter<TPositional>))]
        public override string[]? Name { get; set; }

        [Parameter(Position = 1)]
        [SupportsWildcards]
        [ArgumentCompleter(typeof(UserNameCompleter<TPositional>))]
        public override string[]? UserName { get; set; }
    }
}
