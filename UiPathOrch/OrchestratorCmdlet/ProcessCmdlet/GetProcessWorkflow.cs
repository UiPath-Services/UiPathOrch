using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

/// <summary>
/// Gets the workflows (`.xaml` files) inside the package a process runs, for the version that
/// folder actually deployed, flagged with which ones are entry points.
///
/// The same package download that answers `Get-OrchProcessDependency` answers this, and both
/// read it from one cache — asking a folder for its processes' dependencies and then for their
/// workflows costs one download per package version, not two.
/// </summary>
[Cmdlet(VerbsCommon.Get, "OrchProcessWorkflow")]
[OutputType(typeof(Entities.ProcessWorkflow))]
public class GetProcessWorkflowCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(ProcessNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    /// <summary>Filters by workflow path inside the project, e.g. '*Invoice*' or 'Sub/*.xaml'.</summary>
    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
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

    [Parameter]
    public uint Depth { get; set; }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(EffectivePath(Path, LiteralPath), Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();
        var wpWorkflow = Workflow.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();

        // Three passes: list the processes (cheap, parallel), download the packages they name
        // (expensive, sequential, with the real count as the denominator), then emit from the
        // warm cache in folder/process order. See PackageContentsPrefetch.
        var targets = ProcessPackageTargets.List(this, drivesFolders, wpName, cancelHandler,
            "Listing processes", "GetProcessWorkflowError");

        PackageContentsPrefetch.Warm(this, targets.Select(t => (t.Drive, t.Folder, t.PackageId, t.Version)),
            cancelHandler, "Downloading packages");

        foreach (var target in targets.WithCancellation(cancelHandler.Token))
        {
            IEnumerable<PackageWorkflow> workflows;
            try
            {
                workflows = target.Drive.GetPackageContents(target.Folder, target.PackageId, target.Version)?.Workflows ?? [];
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(target.Release.GetPSPath(), ex),
                    "GetProcessWorkflowError", ErrorCategory.InvalidOperation, target.Release));
                continue;
            }

            if (EntryPoint.IsPresent) workflows = workflows.Where(w => w.IsEntryPoint == true);

            WriteObject(workflows
                .FilterByWildcards(w => w?.Workflow, wpWorkflow)
                .OrderBy(w => w?.Workflow)
                .Select(w => new ProcessWorkflow(w, target.Release.Name, target.FolderPath)),
                true);
        }
    }
}
