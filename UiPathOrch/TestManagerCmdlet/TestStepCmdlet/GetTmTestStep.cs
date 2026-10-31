using System.Management.Automation;
using System.Text;
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
    [EmptyStringToNullArgumentTransformation] // an empty CSV cell means every step, not step 0
    public int[]? OrderNo { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public string? ExportCsv { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(EncodingCompleter))]
    [EncodingArgumentTransformation]
    public Encoding? CsvEncoding { get; set; }

    // The columns are New-TmTestStep's parameters, so `Import-Csv | New-TmTestStep` recreates the
    // steps. Path and TestCase are wildcard-escaped because those parameters take wildcards.
    private static readonly string DefaultCsvName = "ExportedTmTestSteps.csv";
    private static readonly string[] CsvHeaders = [
        "Path",
        "TestCase",
        "OrderNo",
        "Description",
        "ExpectedResult",
        "ClipboardData"
    ];

    private void Output(StreamWriter? writer, List<TmTestStep> steps)
    {
        if (writer is null)
        {
            WriteObject(steps, true);
            return;
        }
        foreach (var step in steps)
        {
            writer.WriteCsvLine([
                EscapeCsvValue(step.Path, true),
                EscapeCsvValue(step.TestCase, true),
                EscapeCsvValue(step.orderNo),
                EscapeCsvValue(step.description),
                EscapeCsvValue(step.expectedResult),
                EscapeCsvValue(step.clipboardData)
            ]);
        }
    }

    protected override void ProcessRecord()
    {
        var drivesProjects = SessionState.EnumTmFolders(EffectivePath(Path, LiteralPath), Recurse.IsPresent);
        var wpTestCase = TestCase.ConvertToWildcardPatternList();
        var testCases = TmTestSteps.SelectTestCases(this, drivesProjects, wpTestCase, all: true).ToList();

        var (physicalCsvPath, providerCsvPath) = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
        using var writer = WriteCsvHeader(physicalCsvPath, CsvEncoding, CsvHeaders);

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
                    Output(writer, steps);
                    continue;
                }

                var outOfRange = new List<int>();
                Output(writer, TmTestSteps.SelectByPosition(steps, OrderNo, outOfRange));
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

        WriteCSVExportedMessage(this, providerCsvPath);
    }
}
