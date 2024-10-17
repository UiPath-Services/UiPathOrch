using System.Collections;
using System.Collections.Concurrent;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;
using Positional = UiPath.PowerShell.Positional.Name;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsLifecycle.Enable, "OrchFolderMachineInherit", SupportsShouldProcess = true)]
    public class EnableFolderMachineInheritCommand : EnableFolderMachineInheritCommandBase<True>
    {
        [Parameter(Position = 0)]
        [SupportsWildcards]
        [ArgumentCompleter(typeof(FolderMachineNameCompleter))]
        public override string[]? Name { get; set; }
    }
}
