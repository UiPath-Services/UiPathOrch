using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// The web's Move First / Up / Down / Last are -TargetPosition 0 / n-1 / n+1 / -1.
[Cmdlet(VerbsCommon.Move, "TmTestStep", SupportsShouldProcess = true)]
[OutputType(typeof(void))]
public class MoveTmTestStepCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TmTestCaseNameCompleter))]
    [SupportsWildcards]
    public string[]? TestCase { get; set; }

    // The step to move: 0-based; negative values count from the end (-1 = the last step).
    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TmTestStepOrderNoCompleter))]
    public int OrderNo { get; set; }

    // Where it ends up, counted the same way; the steps in between shift by one.
    [Parameter(Position = 2, Mandatory = true)]
    [ArgumentCompleter(typeof(TmTestStepOrderNoCompleter))]
    public int TargetPosition { get; set; }

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
            try
            {
                var steps = TmTestSteps.Get(drive, project, testCase);
                if (TmTestSteps.ResolvePosition(OrderNo, steps.Count) is not int from)
                {
                    WriteError(TmTestSteps.OutOfRangeError(testCase, OrderNo, steps.Count));
                    continue;
                }
                if (TmTestSteps.ResolvePosition(TargetPosition, steps.Count) is not int to)
                {
                    WriteError(TmTestSteps.OutOfRangeError(testCase, TargetPosition, steps.Count));
                    continue;
                }
                if (from == to) continue;

                var step = steps[from];
                if (ShouldProcess(step.GetPSPath(), $"Move TmTestStep to position {to}"))
                {
                    drive.OrchAPISession.MoveTmTestStep(project.id!, step.id!, to);
                }
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(testCase.GetPSPath(), ex), "MoveTmTestStepError", ErrorCategory.InvalidOperation, testCase));
            }
        }
    }
}
