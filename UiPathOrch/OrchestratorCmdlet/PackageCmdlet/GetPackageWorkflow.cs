using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

/// <summary>
/// Gets the workflows (`.xaml` files) inside a package version, flagged with which ones the
/// project publishes as entry points — the file list the web UI's "Explore package" shows.
///
/// This is the feed-side view. To ask it of a process in a folder, which resolves the deployed
/// version and that folder's feed, use `Get-OrchProcessWorkflow`. Both share one cache with
/// the dependency cmdlets, so a package is downloaded once however it is reached.
/// </summary>
[Cmdlet(VerbsCommon.Get, "OrchPackageWorkflow")]
[OutputType(typeof(Entities.PackageWorkflow))]
public class GetPackageWorkflowCmdlet : OrchestratorPSCmdlet
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

    /// <summary>Filters by workflow path inside the project, e.g. '*Invoice*' or 'Sub/*.xaml'.</summary>
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PackageWorkflowCompleter))]
    [SupportsWildcards]
    public string[]? Workflow { get; set; }

    /// <summary>Returns only the workflows the project publishes as entry points.</summary>
    [Parameter]
    public SwitchParameter EntryPoint { get; set; }

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
        var wpWorkflow = Workflow.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();

        // Three passes: list the packages (cheap, parallel), download the versions to read
        // (expensive, sequential, with the real count as the denominator), then emit from the
        // warm cache in feed/package order. See PackageContentsPrefetch.
        var targets = FeedPackageTargets.List(this, drivesFolders, wpId, wpVersion, Version is not null,
            cancelHandler, "Listing packages", "GetPackageWorkflowError");

        PackageContentsPrefetch.Warm(this, targets.Select(t => (t.Drive, t.Folder, t.Package.Id!, t.Version)),
            cancelHandler, "Downloading packages");

        foreach (var target in targets.WithCancellation(cancelHandler.Token))
        {
            try
            {
                IEnumerable<PackageWorkflow> workflows =
                    target.Drive.GetPackageContents(target.Folder, target.Package.Id!, target.Version)?.Workflows ?? [];

                if (EntryPoint.IsPresent) workflows = workflows.Where(w => w.IsEntryPoint == true);

                WriteObject(workflows
                    .FilterByWildcards(w => w?.Workflow, wpWorkflow)
                    .OrderBy(w => w?.Workflow)
                    .Select(w => { var c = w.ShallowClone(); c.Path = target.FolderPath; return c; }),
                    true);
            }
            catch (Exception ex)
            {
                string errorTarget = $"{target.FolderPath} {target.Package.Id}:{target.Version}";
                WriteError(new ErrorRecord(new OrchException(errorTarget, ex),
                    "GetPackageWorkflowError", ErrorCategory.InvalidOperation, target.Package));
            }
        }
    }
}
