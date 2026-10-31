using System.Management.Automation;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "TmTestCase")]
[OutputType(typeof(Entities.TmTestCase))]
public class GetTmTestCaseCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TmTestCaseNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

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

    // The columns are New-TmTestCase's parameters, so `Import-Csv | New-TmTestCase` recreates the
    // test cases. Path and Name are wildcard-escaped (New-TmTestCase unescapes the name); Labels
    // is read per test case (see TmLabelCsv).
    private static readonly string DefaultCsvName = "ExportedTmTestCases.csv";
    private static readonly string[] CsvHeaders = [
        "Path",
        "Name",
        "Version",
        "Description",
        "Labels"
    ];

    protected override void ProcessRecord()
    {
        var drivesProjects = SessionState.EnumTmFolders(EffectivePath(Path, LiteralPath), Recurse.IsPresent);
        var wpName = Name.ConvertToWildcardPatternList();

        var (physicalCsvPath, providerCsvPath) = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
        using var writer = WriteCsvHeader(physicalCsvPath, CsvEncoding, CsvHeaders);

        using var results = OrchThreadPool.RunForEach(drivesProjects,
            dp => dp.project.GetPSPath(),
            dp => dp.project,
            dp => dp.drive.TmTestCases.Get(dp.project));

        using var cancelHandler = new ConsoleCancelHandler();
        using var reporter = new ProgressReporter(this, results.Count, "Getting test cases");
        foreach (var result in results)
        {
            try
            {
                var entity = results.GetResultWithProgress(result, reporter, cancelHandler.Token);
                if (entity is null) continue;

                var testCases = entity
                    .FilterByWildcards(e => e?.name, wpName)
                    .OrderBy(e => e.objKey!, ObjKeyComparer.Instance);
                if (writer is null)
                {
                    WriteObject(testCases, true);
                    continue;
                }

                var (drive, project) = result.Source;
                TmLabelCsv.ForEachWithLabels(this, drive, project, "testCase", testCases.ToList(), e => e.id, e => e.GetPSPath(),
                    (e, labels) => writer.WriteCsvLine([
                        EscapeCsvValue(e.Path, true),
                        EscapeCsvValue(e.name, true),
                        EscapeCsvValue(e.version),
                        EscapeCsvValue(e.description),
                        EscapeCsvValue(labels)
                    ]),
                    reporter, cancelHandler.Token);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetTmTestCaseError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }

        WriteCSVExportedMessage(this, providerCsvPath);
    }
}
