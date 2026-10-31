using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsData.Update, "TmTestSet", SupportsShouldProcess = true)]
[OutputType(typeof(void))]
public class UpdateTmTestSetCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TmTestSetNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? NewName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? Description { get; set; }

    // "Enable RPA activity coverage calculation" (the test set's Configuration tab). The PUT
    // body cannot carry it, but the JSON Patch endpoint takes /enableCoverage.
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BoolCompleter))]
    public string? EnableCoverage { get; set; }

    // Replaces the test set's labels with this list (removeOtherLabels), as -Tags does on
    // Update-OrchQueue. An empty list leaves the labels alone.
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TmLabelNameCompleter))]
    public string[]? Labels { get; set; }

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
        bool? enableCoverage;
        try
        {
            enableCoverage = TmPatch.ParseBool(EnableCoverage, nameof(EnableCoverage));
        }
        catch (ArgumentException ex)
        {
            ThrowTerminatingError(new ErrorRecord(ex, "InvalidEnableCoverage", ErrorCategory.InvalidArgument, EnableCoverage));
            return;
        }

        var drivesProjects = SessionState.EnumTmFolders(EffectivePath(Path, LiteralPath), Recurse.IsPresent);
        var wpName = Name.ConvertToWildcardPatternList();
        var labels = TmLabelCsv.Parse(Labels);

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, project) in drivesProjects.WithCancellation(cancelHandler.Token))
        {
            IEnumerable<TmTestSet>? testSets = null;
            try
            {
                testSets = drive.TmTestSets.Get(project);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(project.GetPSPath(), ex), "GetTmTestSetError", ErrorCategory.InvalidOperation, project));
            }
            if (testSets is null) continue;

            foreach (var testSet in testSets
                .SelectByWildcards(e => e?.name, wpName)
                .OrderBy(e => e.objKey!, ObjKeyComparer.Instance)
                .ToList()
                .WithProgressBar(this, $"Updating test sets in {project.GetPSPath()}", e => e.name)
                .WithCancellation(cancelHandler.Token))
            {
                var operations = ComputeTestSetPatch(testSet, NewName, Description, enableCoverage);
                bool hasLabels = labels is { Length: > 0 };
                if (operations.Count == 0 && !hasLabels)
                {
                    continue;
                }

                var target = testSet.GetPSPath();
                if (ShouldProcess(target, "Update TmTestSet"))
                {
                    try
                    {
                        if (operations.Count != 0)
                        {
                            drive.OrchAPISession.PatchTmTestSet(project.id!, testSet.id!, operations);
                        }
                        if (hasLabels)
                        {
                            drive.OrchAPISession.AddTmObjectLabels(project.id!, "testSet", testSet.id!, labels!, replace: true);
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(target, ex), "UpdateTmTestSetError", ErrorCategory.InvalidOperation, testSet));
                    }
                    drive.TmTestSets.ClearCache(project.id);
                    if (hasLabels) drive.TmLabels.ClearCache(project.id);
                }
            }
        }
    }

    /// <summary>
    /// The JSON Patch operations that bring <paramref name="current"/> to the requested values: one
    /// "replace" per field that was given AND differs from the current value, so an unchanged
    /// request sends nothing. No API access -- unit-testable.
    /// </summary>
    internal static List<TmPatchOperation> ComputeTestSetPatch(TmTestSet current, string? newName, string? description, bool? enableCoverage)
    {
        var operations = new List<TmPatchOperation>();
        TmPatch.ReplaceIfChanged(operations, "/name", newName, current.name);
        TmPatch.ReplaceIfChanged(operations, "/description", description, current.description);
        TmPatch.ReplaceIfChanged(operations, "/enableCoverage", enableCoverage, current.enableCoverage);
        return operations;
    }
}
