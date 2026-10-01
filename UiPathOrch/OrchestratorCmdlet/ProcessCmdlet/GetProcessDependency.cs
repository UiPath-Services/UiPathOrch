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

        // List the processes first (cheap, parallel), then walk them folder by folder,
        // downloading each package as it is reached. The downloads are sequential and counted
        // up front (see PackageContentsLoader); a folder's rows are emitted as soon as that
        // folder is done -- in one WriteObject, because the table view sizes its columns from
        // the first batch it receives, and row-by-row emission clipped the later, longer ids.
        var targets = ProcessPackageTargets.List(this, drivesFolders, wpName, cancelHandler,
            "Listing processes", "GetProcessDependencyError");

        using var loader = new PackageContentsLoader(this,
            targets.Select(t => (t.Drive, t.Folder, t.PackageId, t.Version)), cancelHandler, "Downloading packages");

        foreach (var folderGroup in targets.GroupBy(t => t.FolderPath).WithCancellation(cancelHandler.Token))
        {
            var rows = new List<ProcessDependency>();

            foreach (var target in folderGroup)
            {
                IEnumerable<PackageDependency> dependencies;
                try
                {
                    dependencies = loader.Load(target.Drive, target.Folder, target.PackageId, target.Version)?.Dependencies ?? [];
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(target.Release.GetPSPath(), ex),
                        "GetProcessDependencyError", ErrorCategory.InvalidOperation, target.Release));
                    continue;
                }

                rows.AddRange(dependencies
                    .FilterByWildcards(d => d?.Dependency, wpDependency)
                    .OrderBy(d => d?.Dependency)
                    .Select(d => new ProcessDependency(d, target.Release.Name, target.FolderPath)));
            }

            if (rows.Count > 0) WriteObject(rows, true);
        }
    }
}
