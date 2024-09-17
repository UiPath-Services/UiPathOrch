using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "OrchCredentialStore")]
    [OutputType(typeof(Entities.CredentialStore))]
    public class GetCredentialStoreCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(CredentialStoreNameCompleter<Positional.Name>))]
        [SupportsWildcards]
        public string[]? Name { get; set; }

        [Parameter]
        [ArgumentCompleter(typeof(DriveCompleter<Positional.Name>))]
        public string[]? Path { get; set; }

        protected override void ProcessRecord()
        {
            var drives = OrchDriveInfo.EnumOrchDrives(Path);
            var wpName = Name?.Select(name => new WildcardPattern(PathTools.UnescapePSText(name), WildcardOptions.IgnoreCase)).ToList();

            using var results = OrchThreadPool.RunForEach(drives,
                drive => drive.NameColonSeparator,
                drive => drive,
                drive => drive.GetCredentialStores());

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var result in results)
            {
                try
                {
                    var machines = result.GetResult(cancelHandler.Token);
                    if (machines == null) continue;

                    WriteObject(machines
                        .FilterByWildcards(c => c?.Name, wpName)
                        .OrderBy(c => c.Name),
                        true);
                }
                catch (OrchException ex)
                {
                    WriteError(new ErrorRecord(ex, "GetCredentialStoreError", ErrorCategory.InvalidOperation, ex.Target));
                }
            }
        }
    }
}
