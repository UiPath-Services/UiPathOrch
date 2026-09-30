using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

/// <summary>
/// Gets the activity packages a process depends on, for the package version that folder
/// actually runs.
///
/// Orchestrator has no endpoint for this: the dependencies live in the `.nuspec` inside the
/// `.nupkg`, which is why the web UI downloads the whole package to show them. Going through
/// the process rather than the package is what makes this usable — the caller does not have to
/// look up which version a folder deployed, nor which feed that folder reads.
///
/// The download is cached per feed + package id + version, so a package shared by many
/// processes, in many folders, is fetched once.
/// </summary>
[Cmdlet(VerbsCommon.Get, "OrchProcessDependency")]
[OutputType(typeof(Entities.ProcessDependency))]
public class GetProcessDependencyCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(ProcessNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    /// <summary>Filters the rows to the dependencies whose id matches, e.g. 'UiPath.UIAutomation*'.</summary>
    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
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

    [Parameter]
    public uint Depth { get; set; }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(EffectivePath(Path, LiteralPath), Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();
        var wpDependency = Dependency.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();

        // Three passes: list the processes (cheap, parallel), download the packages they name
        // (expensive, sequential, with the real count as the denominator), then emit from the
        // warm cache in folder/process order. See PackageContentsPrefetch.
        var targets = ProcessPackageTargets.List(this, drivesFolders, wpName, cancelHandler,
            "Listing processes", "GetProcessDependencyError");

        PackageContentsPrefetch.Warm(this, targets.Select(t => (t.Drive, t.Folder, t.PackageId, t.Version)),
            cancelHandler, "Downloading packages");

        foreach (var target in targets.WithCancellation(cancelHandler.Token))
        {
            IEnumerable<PackageDependency> dependencies;
            try
            {
                dependencies = target.Drive.GetPackageContents(target.Folder, target.PackageId, target.Version)?.Dependencies ?? [];
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(target.Release.GetPSPath(), ex),
                    "GetProcessDependencyError", ErrorCategory.InvalidOperation, target.Release));
                continue;
            }

            WriteObject(dependencies
                .FilterByWildcards(d => d?.Dependency, wpDependency)
                .OrderBy(d => d?.Dependency)
                .Select(d => new ProcessDependency(d, target.Release.Name, target.FolderPath)),
                true);
        }
    }
}
