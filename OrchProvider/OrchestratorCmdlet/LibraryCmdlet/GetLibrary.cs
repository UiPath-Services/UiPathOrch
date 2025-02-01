using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Positional;
using TPositional = UiPath.PowerShell.Positional.Id;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "OrchLibrary")]
    [OutputType(typeof(Entities.Library))]
    public class GetLibraryCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(LibraryIdCompleter<TPositional>))]
        [SupportsWildcards]
        public string[]? Id { get; set; }

        [Parameter]
        public SwitchParameter HostFeed { get; set; }

        [Parameter]
        [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
        public string[]? Path { get; set; }

        protected override void ProcessRecord()
        {
            var drives = OrchDriveInfo.EnumOrchDrives(Path);
            var wpId = Id.ConvertToWildcardPatternList();

            using var results = OrchThreadPool.RunForEach(drives,
                drive => drive.NameColonSeparator,
                drive => drive,
                drive => HostFeed ? drive.LibrariesInHost.Get() : drive.LibrariesInTenant.Get());

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var result in results)
            {
                try
                {
                    var libraries = result.GetResult(cancelHandler.Token);
                    if (libraries == null) continue;

                    WriteObject(libraries
                        .FilterByWildcards(l => l?.Id, wpId)
                        .OrderBy(l => l.Id!.ToLower()),
                        true);
                }
                catch (OrchException ex)
                {
                    WriteError(new ErrorRecord(ex, "GetLibraryError", ErrorCategory.InvalidOperation, ex.Target));
                }
            }
        }
    }
}
