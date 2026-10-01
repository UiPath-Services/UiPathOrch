using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

/// <summary>
/// Gets the activity packages a library depends on, read from the `.nuspec` inside the
/// library's `.nupkg` — Orchestrator has no endpoint for the dependency list, so the package
/// is downloaded and read, the same way `Get-OrchPackageDependency` works for processes.
///
/// Without -Version only each library's latest version is read. A library feed keeps every
/// version ever published and each one is a separate download, so reading them all is opt-in:
/// `-Version *` does it.
///
/// Tenant feed only, the same limit `Export-OrchLibrary` has: the Libraries download endpoint
/// takes no feedId, so a host-feed library cannot be fetched.
/// </summary>
[Cmdlet(VerbsCommon.Get, "OrchLibraryDependency")]
[OutputType(typeof(Entities.PackageDependency))]
public class GetLibraryDependencyCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(LibraryIdCompleter))]
    [SupportsWildcards]
    public string[]? Id { get; set; }

    /// <summary>The library versions to read. Omitting it reads the latest version of each library.</summary>
    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(LibraryVersionCompleter))]
    [SupportsWildcards]
    public string[]? Version { get; set; }

    /// <summary>Filters the rows to the dependencies whose id matches, e.g. 'UiPath.UIAutomation*'.</summary>
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Dependency { get; set; }

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
        var wpDependency = Dependency.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();

        // Three passes, as in the package cmdlets: list (cheap, parallel), download (expensive,
        // sequential, with the real count as the denominator), emit from the warm cache.
        using var libraryPool = OrchThreadPool.RunForEach(
            drives,
            drive => drive.NameColonSeparator,
            drive => drive,
            drive => drive.LibrariesInTenant.Get()
                .FilterByWildcards(l => l?.Id, wpId)
                .OrderBy(l => l.Id!.ToLower())
                .Select(lib => (drive, lib))
                .ToList());

        var targets = new List<(OrchDriveInfo drive, string libraryId, string version)>();
        using (var reporter = new ProgressReporter(this, 1, libraryPool.Count, "Listing libraries"))
        {
            foreach (var task in libraryPool)
            {
                try
                {
                    var found = libraryPool.GetResultWithProgress(task, reporter, cancelHandler.Token);
                    if (found is null) continue;

                    foreach (var (drive, lib) in found.WithCancellation(cancelHandler.Token))
                    {
                        if (string.IsNullOrEmpty(lib.Id)) continue;

                        var versions = Version is null
                            ? [lib.Version]
                            : drive.LibraryVersions.Get(lib.Id!)
                                .FilterByWildcards(v => v?.Version, wpVersion)
                                .Select(v => v.Version)
                                .ToArray();

                        foreach (var version in versions)
                        {
                            if (string.IsNullOrEmpty(version)) continue;
                            targets.Add((drive, lib.Id!, version!));
                        }
                    }
                }
                catch (OrchException ex)
                {
                    WriteError(new ErrorRecord(ex, "GetLibraryDependencyError", ErrorCategory.InvalidOperation, ex.Target));
                }
            }
        }

        // Downloads happen as each library is reached, so rows appear while the rest are still
        // being fetched; the bar below is sized from the ones not already cached.
        var pending = targets
            .Where(t => t.drive.LibraryContents.CachedEntries.All(e => e.Key != (t.libraryId, t.version)))
            .Select(t => (t.drive.NameColonSeparator, t.libraryId, t.version))
            .ToHashSet();

        using var downloadReporter = pending.Count > 0
            ? new ProgressReporter(this, 2, pending.Count, "Downloading libraries")
            : null;
        int downloaded = 0;

        // Emitted per drive, in one WriteObject: the table view sizes its columns from the
        // first batch it receives, so row-by-row emission clipped the later, longer ids.
        foreach (var driveGroup in targets.GroupBy(t => t.drive.NameColonSeparator).WithCancellation(cancelHandler.Token))
        {
            var rows = new List<PackageDependency>();

            foreach (var (drive, libraryId, version) in driveGroup)
            {
                try
                {
                    if (pending.Remove((drive.NameColonSeparator, libraryId, version)))
                    {
                        downloadReporter?.WriteProgress(++downloaded, $"{libraryId}:{version}");
                    }

                    var contents = drive.LibraryContents.Get((libraryId, version));
                    rows.AddRange((contents?.Dependencies ?? [])
                        .FilterByWildcards(d => d?.Dependency, wpDependency)
                        .OrderBy(d => d?.Dependency)
                        .Select(d => { var c = d.ShallowClone(); c.Path = drive.NameColonSeparator; return c; }));
                }
                catch (Exception ex)
                {
                    string errorTarget = $"{drive.NameColonSeparator}{libraryId}:{version}";
                    WriteError(new ErrorRecord(new OrchException(errorTarget, ex),
                        "GetLibraryDependencyError", ErrorCategory.InvalidOperation, libraryId));
                }
            }

            if (rows.Count > 0) WriteObject(rows, true);
        }
    }
}
