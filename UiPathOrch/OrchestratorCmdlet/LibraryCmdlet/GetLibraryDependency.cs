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

        WarmLibraries(targets, cancelHandler);

        foreach (var (drive, libraryId, version) in targets.WithCancellation(cancelHandler.Token))
        {
            try
            {
                var contents = drive.LibraryContents.Get((libraryId, version));
                WriteObject((contents?.Dependencies ?? [])
                    .FilterByWildcards(d => d?.Dependency, wpDependency)
                    .OrderBy(d => d?.Dependency)
                    .Select(d => { var c = d.ShallowClone(); c.Path = drive.NameColonSeparator; return c; }),
                    true);
            }
            catch (Exception ex)
            {
                string errorTarget = $"{drive.NameColonSeparator}{libraryId}:{version}";
                WriteError(new ErrorRecord(new OrchException(errorTarget, ex),
                    "GetLibraryDependencyError", ErrorCategory.InvalidOperation, libraryId));
            }
        }
    }

    /// <summary>
    /// Downloads each library version once, in sequence, so the bar counts the work that costs
    /// something and an unexpectedly large `-Version *` can be cancelled before it is done.
    /// Failures are left to the emit pass, which reports them against their own row.
    /// </summary>
    private void WarmLibraries(
        List<(OrchDriveInfo drive, string libraryId, string version)> targets, ConsoleCancelHandler cancelHandler)
    {
        var distinct = targets
            .GroupBy(t => (drive: t.drive.NameColonSeparator, t.libraryId, t.version))
            .Select(g => g.First())
            .ToList();

        if (distinct.Count == 0) return;

        using var reporter = new ProgressReporter(this, 2, distinct.Count, "Downloading libraries");
        int index = 0;
        foreach (var (drive, libraryId, version) in distinct)
        {
            cancelHandler.Token.ThrowIfCancellationRequested();
            reporter.WriteProgress(++index, $"{libraryId}:{version}");

            try { drive.LibraryContents.Get((libraryId, version)); }
            catch (Exception) { /* reported by the emit pass, against the row it concerns */ }
        }
    }
}
