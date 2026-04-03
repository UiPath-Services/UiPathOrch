using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchLibraryVersion")]
[OutputType(typeof(Entities.Library))]
public class GetLibraryVersionCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(LibraryIdCompleter))]
    [SupportsWildcards]
    public string[]? Id { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(LibraryVersionCompleter))]
    [SupportsWildcards]
    public string[]? Version { get; set; }

    [Parameter]
    public SwitchParameter HostFeed { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumOrchDrives(Path);
        var wpId = Id.ConvertToWildcardPatternList();
        var wpVersion = Version.ConvertToWildcardPatternList();

        using var results = OrchThreadPool.RunForEach(drives,
            drive => drive.NameColonSeparator,
            drive => drive,
            drive => {
                if (HostFeed)
                {
                    var librariesInHost = drive.LibrariesInHost.Get();
                    return OrchThreadPool.RunForEach(librariesInHost.FilterByWildcards(l => l?.Id, wpId),
                        lib => lib.GetPSPath(),
                        lib => lib,
                        lib => drive.GetLibraryVersionsInHostFeed(lib.Id!).FilterByWildcards(l => l?.Version, wpVersion));
                }
                else
                {
                    var librariesInTenant = drive.LibrariesInTenant.Get();
                    return OrchThreadPool.RunForEach(librariesInTenant.FilterByWildcards(l => l?.Id, wpId),
                        lib => lib.GetPSPath(),
                        lib => lib,
                        lib => drive.GetLibraryVersions(lib.Id!).FilterByWildcards(l => l?.Version, wpVersion));
                }
            });

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                using var threads = result.GetResult(cancelHandler.Token);

                foreach (var thread in threads!)
                {
                    try
                    {
                        var versions = thread.GetResult(cancelHandler.Token);
                        WriteObject(versions, true);
                    }
                    catch (OrchException ex)
                    {
                        WriteError(new ErrorRecord(ex, "GetLibraryVersionError", ErrorCategory.InvalidOperation, ex.Target));
                    }
                }
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetLibraryError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }


#if false
        using var results = OrchThreadPool.RunForEach(drives,
            drive => drive.NameColonSeparator,
            drive => drive,
            drive => drive.GetLibraries());

        foreach (var result in results)
        {
            try
            {
                var libraries = result.GetResult();
                if (libraries is null) continue;

                var drive = result.Source;

                var matchedLibraries = libraries
                    .FilterByWildcards(p => p.Id!, wpId)
                    .OrderBy(l => l.Id!.ToLower()).ToList();

                using var results2 = OrchThreadPool.RunForEach(matchedLibraries,
                    ml => drive.NameColonSeparator,
                    ml => drive,
                    ml => drive.GetLibraryVersions(ml.Id!));

                foreach (var result2 in results2)
                {
                    try
                    {
                        var versions = result2.GetResult();
                        if (versions is null) continue;

                        WriteObject(versions
                            .FilterByWildcards(p => p.Version!, wpVersion)
                            .OrderBy(p => p.Version!, new VersionComparer()),
                            true);
                    }
                    catch (OrchException ex)
                    {
                        WriteError(new ErrorRecord(ex, "GetLibraryVersionError", ErrorCategory.InvalidOperation, ex.Target));
                    }
                }
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetLibraryError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
#endif
    }
}
