using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// One step per call: New-TmTestStep <test case> <description> <expected result> <clipboard data>.
// Pipe objects with Description / ExpectedResult / ClipboardData properties (e.g. Import-Csv, or
// Get-TmTestStep of another test case) to add several in order.
[Cmdlet(VerbsCommon.New, "TmTestStep", SupportsShouldProcess = true)]
[OutputType(typeof(TmTestStep))]
public class NewTmTestStepCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TmTestCaseNameCompleter))]
    [SupportsWildcards]
    public string[]? TestCase { get; set; }

    // Required by the server, but may be empty.
    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [AllowEmptyString]
    public string? Description { get; set; }

    // "Expected behaviour" in the web form. The positions follow the form's order.
    [Parameter(Position = 2, ValueFromPipelineByPropertyName = true)]
    public string? ExpectedResult { get; set; }

    [Parameter(Position = 3, ValueFromPipelineByPropertyName = true)]
    public string? ClipboardData { get; set; }

    // Where the new step goes (0-based); the steps from there on move down by one. Negative values
    // count from the end: -1 (the default) appends, -2 inserts before the last step.
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TmTestStepOrderNoCompleter))]
    public int OrderNo { get; set; } = -1;

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    protected override void ProcessRecord()
    {
        var drivesProjects = SessionState.EnumTmFolders(EffectivePath(Path, LiteralPath));
        var wpTestCase = TestCase.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, project, testCase) in TmTestSteps.SelectTestCases(this, drivesProjects, wpTestCase, all: false)
            .WithCancellation(cancelHandler.Token))
        {
            string target = testCase.GetPSPath();
            if (!ShouldProcess(target, "New TmTestStep"))
            {
                continue;
            }

            try
            {
                // A position other than "append" needs the current step count to resolve.
                int orderNo = -1;
                if (OrderNo != -1)
                {
                    int count = TmTestSteps.Get(drive, project, testCase, refresh: true).Count;
                    if (TmTestSteps.ResolveInsertPosition(OrderNo, count) is not int index)
                    {
                        WriteError(TmTestSteps.OutOfRangeError(testCase, OrderNo, count));
                        continue;
                    }
                    orderNo = index;
                }

                var created = drive.OrchAPISession.PostTmTestStep(project.id!, new TmCreateTestStepRequest
                {
                    testCaseId = testCase.id,
                    orderNo = orderNo,
                    description = Description ?? "",
                    expectedResult = ExpectedResult,
                    clipboardData = ClipboardData,
                });
                if (created is null) continue;
                created.Path = project.GetPSPath();
                created.TestCase = testCase.name;
                WriteObject(created);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(target, ex), "NewTmTestStepError", ErrorCategory.InvalidOperation, testCase));
            }
            drive.TmTestSteps.ClearCache(project.id, testCase.id);
        }
    }
}
