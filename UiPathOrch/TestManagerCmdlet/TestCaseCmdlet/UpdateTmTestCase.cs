using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsData.Update, "TmTestCase", SupportsShouldProcess = true)]
[OutputType(typeof(void))]
public class UpdateTmTestCaseCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TmTestCaseNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? NewName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? Version { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? Description { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? PreCondition { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? PostCondition { get; set; }

    // Replaces the test case's labels with this list (removeOtherLabels), as -Tags does on
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
        var drivesProjects = SessionState.EnumTmFolders(EffectivePath(Path, LiteralPath), Recurse.IsPresent);
        var wpName = Name.ConvertToWildcardPatternList();
        var labels = TmLabelCsv.Parse(Labels);

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, project) in drivesProjects.WithCancellation(cancelHandler.Token))
        {
            IEnumerable<TmTestCase>? testCases = null;
            try
            {
                testCases = drive.TmTestCases.Get(project);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(project.GetPSPath(), ex), "GetTmTestCaseError", ErrorCategory.InvalidOperation, project));
            }
            if (testCases is null) continue;

            foreach (var testCase in testCases
                .SelectByWildcards(e => e?.name, wpName)
                .OrderBy(e => e.objKey!, ObjKeyComparer.Instance)
                .ToList()
                .WithProgressBar(this, $"Updating test cases in {project.GetPSPath()}", e => e.name)
                .WithCancellation(cancelHandler.Token))
            {
                var operations = ComputeTestCasePatch(testCase, NewName, Version, Description, PreCondition, PostCondition);
                bool hasLabels = labels is { Length: > 0 };
                if (operations.Count == 0 && !hasLabels)
                {
                    continue;
                }

                var target = testCase.GetPSPath();
                if (ShouldProcess(target, "Update TmTestCase"))
                {
                    try
                    {
                        if (operations.Count != 0)
                        {
                            drive.OrchAPISession.PatchTmTestCase(project.id!, testCase.id!, operations);
                        }
                        if (hasLabels)
                        {
                            drive.OrchAPISession.AddTmObjectLabels(project.id!, "testCase", testCase.id!, labels!, replace: true);
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(target, ex), "UpdateTmTestCaseError", ErrorCategory.InvalidOperation, testCase));
                    }
                    drive.TmTestCases.ClearCache(project.id);
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
    internal static List<TmPatchOperation> ComputeTestCasePatch(TmTestCase current, string? newName, string? version, string? description, string? preCondition, string? postCondition)
    {
        var operations = new List<TmPatchOperation>();
        TmPatch.ReplaceIfChanged(operations, "/name", newName, current.name);
        TmPatch.ReplaceIfChanged(operations, "/version", version, current.version);
        TmPatch.ReplaceIfChanged(operations, "/description", description, current.description);
        TmPatch.ReplaceIfChanged(operations, "/preCondition", preCondition, current.preCondition);
        TmPatch.ReplaceIfChanged(operations, "/postCondition", postCondition, current.postCondition);
        return operations;
    }
}
