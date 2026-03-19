using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using TPositional = UiPath.PowerShell.Positional.Id;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchLibrary")]
[OutputType(typeof(Entities.Library))]
public class GetLibraryCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(LibraryIdCompleter))]
    [SupportsWildcards]
    public string[]? Id { get; set; }

    [Parameter]
    public SwitchParameter HostFeed { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumOrchDrives(Path);
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
                if (libraries is null) continue;

                // When outputting with WriteObject(coll, true), Ctrl+C doesn't stop it when processing is slow..
                // It might be better to output one at a time, as shown below.
                foreach (var library in libraries.FilterByWildcards(l => l?.Id, wpId).OrderBy(l => l.Id!.ToLower()))
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();
                    WriteObject(library);
                }
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetLibraryError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
