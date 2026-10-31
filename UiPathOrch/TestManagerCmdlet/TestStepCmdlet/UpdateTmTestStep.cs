using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsData.Update, "TmTestStep", SupportsShouldProcess = true)]
[OutputType(typeof(void))]
public class UpdateTmTestStepCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TmTestCaseNameCompleter))]
    [SupportsWildcards]
    public string[]? TestCase { get; set; }

    // 0-based positions; negative values count from the end (-1 = the last step).
    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TmTestStepOrderNoCompleter))]
    public int[]? OrderNo { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? Description { get; set; }

    // "Expected behaviour" in the web form.
    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? ExpectedResult { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? ClipboardData { get; set; }

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
        var wpTestCase = TestCase.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, project, testCase) in TmTestSteps.SelectTestCases(this, drivesProjects, wpTestCase, all: false)
            .WithCancellation(cancelHandler.Token))
        {
            List<TmTestStep> steps;
            try
            {
                steps = TmTestSteps.Get(drive, project, testCase, refresh: true);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(testCase.GetPSPath(), ex), "GetTmTestStepError", ErrorCategory.InvalidOperation, testCase));
                continue;
            }

            var outOfRange = new List<int>();
            foreach (var step in TmTestSteps.SelectByPosition(steps, OrderNo!, outOfRange)
                .WithCancellation(cancelHandler.Token))
            {
                var operations = ComputeTestStepPatch(step, Description, ExpectedResult, ClipboardData);
                if (operations.Count == 0)
                {
                    continue;
                }

                var target = step.GetPSPath();
                if (ShouldProcess(target, "Update TmTestStep"))
                {
                    try
                    {
                        drive.OrchAPISession.PatchTmTestStep(project.id!, step.id!, operations);
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(target, ex), "UpdateTmTestStepError", ErrorCategory.InvalidOperation, step));
                    }
                    drive.TmTestSteps.ClearCache(project.id, testCase.id);
                }
            }
            foreach (var position in outOfRange)
            {
                WriteError(TmTestSteps.OutOfRangeError(testCase, position, steps.Count));
            }
        }
    }

    /// <summary>
    /// The JSON Patch operations that bring <paramref name="current"/> to the requested values: one
    /// "replace" per field that was given AND differs from the current value, so an unchanged
    /// request sends nothing. No API access -- unit-testable.
    /// </summary>
    internal static List<TmPatchOperation> ComputeTestStepPatch(TmTestStep current, string? description, string? expectedResult, string? clipboardData)
    {
        var operations = new List<TmPatchOperation>();
        TmPatch.ReplaceIfChanged(operations, "/description", description, current.description);
        TmPatch.ReplaceIfChanged(operations, "/expectedResult", expectedResult, current.expectedResult);
        TmPatch.ReplaceIfChanged(operations, "/clipboardData", clipboardData, current.clipboardData);
        return operations;
    }
}
