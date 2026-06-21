using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchLibraryVersion")]
[OutputType(typeof(Entities.Library))]
public class GetLibraryVersionCmdlet : OrchestratorPSCmdlet
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

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumOrchDrives(EffectivePath(Path, LiteralPath));
        var wpId = Id.ConvertToWildcardPatternList();
        var wpVersion = Version.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();

        // Two sequential phases so each progress bar has a known denominator (a streaming
        // chain never learns the library total until every drive is listed). Phase 1 lists
        // libraries per drive (parallel, cap=4); Phase 2 fetches versions per library.
        using var libraryPool = OrchThreadPool.RunForEach(
            drives,
            drive => drive.NameColonSeparator,
            drive => (object)drive,
            drive => (HostFeed ? drive.LibrariesInHost.Get() : drive.LibrariesInTenant.Get())
                .FilterByWildcards(l => l?.Id, wpId)
                .Select(lib => (drive, lib))
                .ToList());

        var libraries = new List<(OrchDriveInfo drive, Library lib)>();
        using (var reporter = new ProgressReporter(this, 1, libraryPool.Count, "Listing libraries"))
        {
            foreach (var task in libraryPool)
            {
                try
                {
                    var found = libraryPool.GetResultWithProgress(task, reporter, cancelHandler.Token);
                    if (found is not null) libraries.AddRange(found);
                }
                catch (OrchException ex)
                {
                    // Distinct ErrorId ("couldn't list libraries") vs the version error below.
                    WriteError(new ErrorRecord(ex, "GetLibraryError", ErrorCategory.InvalidOperation, ex.Target));
                }
            }
        }

        using var versionPool = OrchThreadPool.RunForEach(
            libraries,
            t => t.lib.GetPSPath(),
            t => (object)t.lib,
            t => (HostFeed ? t.drive.LibraryVersionsInHostFeed.Get(t.lib.Id!) : t.drive.LibraryVersions.Get(t.lib.Id!))
                .FilterByWildcards(l => l?.Version, wpVersion));

        using var versionReporter = new ProgressReporter(this, 1, versionPool.Count, "Getting library versions");
        foreach (var task in versionPool)
        {
            try
            {
                var versions = versionPool.GetResultWithProgress(task, versionReporter, cancelHandler.Token);
                WriteObject(versions, true);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetLibraryVersionError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
