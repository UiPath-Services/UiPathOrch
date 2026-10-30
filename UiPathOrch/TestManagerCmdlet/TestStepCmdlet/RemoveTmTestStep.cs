using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Deleting a step renumbers the ones after it, so positions coming down the pipeline would
// point at the wrong steps after the first delete. ProcessRecord therefore only resolves
// positions to steps; EndProcessing deletes them all.
[Cmdlet(VerbsCommon.Remove, "TmTestStep", SupportsShouldProcess = true)]
[OutputType(typeof(void))]
public class RemoveTmTestStepCmdlet : OrchestratorPSCmdlet
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
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    private readonly List<(OrchTmDriveInfo drive, TmProject project, TmTestStep step)> _toRemove = [];
    private readonly HashSet<string> _seen = [];

    // Steps read for this pipeline, by test case id: every record resolves against the steps as
    // they were before the first delete.
    private readonly Dictionary<string, List<TmTestStep>> _stepsByTestCase = [];

    protected override void ProcessRecord()
    {
        var drivesProjects = SessionState.EnumTmFolders(EffectivePath(Path, LiteralPath), Recurse.IsPresent);
        var wpTestCase = TestCase.ConvertToWildcardPatternList();

        foreach (var (drive, project, testCase) in TmTestSteps.SelectTestCases(this, drivesProjects, wpTestCase, all: false))
        {
            if (!_stepsByTestCase.TryGetValue(testCase.id!, out var steps))
            {
                try
                {
                    steps = TmTestSteps.Get(drive, project, testCase);
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(testCase.GetPSPath(), ex), "GetTmTestStepError", ErrorCategory.InvalidOperation, testCase));
                    continue;
                }
                _stepsByTestCase[testCase.id!] = steps;
            }

            var outOfRange = new List<int>();
            foreach (var step in TmTestSteps.SelectByPosition(steps, OrderNo!, outOfRange))
            {
                if (_seen.Add(step.id!))
                {
                    _toRemove.Add((drive, project, step));
                }
            }
            foreach (var position in outOfRange)
            {
                WriteError(TmTestSteps.OutOfRangeError(testCase, position, steps.Count));
            }
        }
    }

    protected override void EndProcessing()
    {
        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, project, step) in _toRemove
            .WithProgressBar(this, "Removing test steps", e => e.step.GetPSPath())
            .WithCancellation(cancelHandler.Token))
        {
            var target = step.GetPSPath();
            if (ShouldProcess(target, "Remove TmTestStep"))
            {
                try
                {
                    drive.OrchAPISession.RemoveTmTestStep(project.id!, step.id!);
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(target, ex), "RemoveTmTestStepError", ErrorCategory.InvalidOperation, step));
                }
            }
        }
    }
}
