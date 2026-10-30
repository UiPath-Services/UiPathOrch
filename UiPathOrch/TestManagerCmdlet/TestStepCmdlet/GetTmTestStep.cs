using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "TmTestStep")]
[OutputType(typeof(TmTestStep))]
public class GetTmTestStepCmdlet : OrchestratorPSCmdlet
{
    // Names of the test cases whose steps to get. Every test case when omitted.
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TmTestCaseNameCompleter))]
    [SupportsWildcards]
    public string[]? TestCase { get; set; }

    // 0-based positions; negative values count from the end (-1 = the last step). Every step
    // when omitted.
    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
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

    protected override void ProcessRecord()
    {
        var drivesProjects = SessionState.EnumTmFolders(EffectivePath(Path, LiteralPath), Recurse.IsPresent);
        var wpTestCase = TestCase.ConvertToWildcardPatternList();
        var testCases = TmTestSteps.SelectTestCases(this, drivesProjects, wpTestCase, all: true).ToList();

        using var results = OrchThreadPool.RunForEach(testCases,
            t => t.testCase.GetPSPath(),
            t => t.testCase,
            t => (t.testCase, steps: TmTestSteps.Get(t.drive, t.project, t.testCase)));

        using var cancelHandler = new ConsoleCancelHandler();
        using var reporter = new ProgressReporter(this, results.Count, "Getting test steps");
        foreach (var result in results)
        {
            try
            {
                var (testCase, steps) = results.GetResultWithProgress(result, reporter, cancelHandler.Token);
                if (OrderNo is null)
                {
                    WriteObject(steps, true);
                    continue;
                }

                var outOfRange = new List<int>();
                WriteObject(TmTestSteps.SelectByPosition(steps, OrderNo, outOfRange), true);
                foreach (var position in outOfRange)
                {
                    WriteError(TmTestSteps.OutOfRangeError(testCase, position, steps.Count));
                }
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetTmTestStepError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
