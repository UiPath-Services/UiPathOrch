using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

/// <summary>
/// Gets the activity packages a package version depends on, read from the `.nuspec` inside
/// the `.nupkg` — the same round trip the web UI's "Explore package" makes, because
/// Orchestrator exposes no endpoint for the dependency list.
///
/// This is the feed-side view. To ask the question the way it is usually meant — "what does
/// the process in this folder depend on" — use `Get-OrchProcessDependency`, which resolves the
/// deployed version and the folder's feed for you. Both share one cache, keyed by feed,
/// package id and version, so a package is downloaded once however it is reached.
/// </summary>
[Cmdlet(VerbsCommon.Get, "OrchPackageDependency")]
[OutputType(typeof(Entities.PackageDependency))]
public class GetPackageDependencyCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PackageIdCompleter))]
    [SupportsWildcards]
    public string[]? Id { get; set; }

    /// <summary>The package versions to read. Omitting it reads the latest version of each package.</summary>
    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PackageVersionCompleter))]
    [SupportsWildcards]
    public string[]? Version { get; set; }

    /// <summary>Filters the rows to the dependencies whose id matches, e.g. 'UiPath.UIAutomation*'.</summary>
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PackageDependencyCompleter))]
    [SupportsWildcards]
    public string[]? Dependency { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    protected override void ProcessRecord()
    {
        // Package feeds live on folders, so the same folder enumeration Get-OrchPackageVersion
        // uses decides which feeds are read.
        var drivesFolders = SessionState.EnumPackageFeedFolders(EffectivePath(Path, LiteralPath), Recurse.IsPresent);
        var wpId = Id.ConvertToWildcardPatternList();
        var wpVersion = Version.ConvertToWildcardPatternList();
        var wpDependency = Dependency.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();

        // Three passes: list the packages (cheap, parallel), download the versions to read
        // (expensive, sequential, with the real count as the denominator), then emit from the
        // warm cache in feed/package order. See PackageContentsPrefetch.
        var targets = FeedPackageTargets.List(this, drivesFolders, wpId, wpVersion, Version is not null,
            cancelHandler, "Listing packages", "GetPackageDependencyError");

        PackageContentsPrefetch.Warm(this, targets.Select(t => (t.Drive, t.Folder, t.Package.Id!, t.Version)),
            cancelHandler, "Downloading packages");

        foreach (var target in targets.WithCancellation(cancelHandler.Token))
        {
            try
            {
                var contents = target.Drive.GetPackageContents(target.Folder, target.Package.Id!, target.Version);
                WriteObject((contents?.Dependencies ?? [])
                    .FilterByWildcards(d => d?.Dependency, wpDependency)
                    .OrderBy(d => d?.Dependency)
                    .Select(d => { var c = d.ShallowClone(); c.Path = target.FolderPath; return c; }),
                    true);
            }
            catch (Exception ex)
            {
                string errorTarget = $"{target.FolderPath} {target.Package.Id}:{target.Version}";
                WriteError(new ErrorRecord(new OrchException(errorTarget, ex),
                    "GetPackageDependencyError", ErrorCategory.InvalidOperation, target.Package));
            }
        }
    }
}
