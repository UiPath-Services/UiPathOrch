using System.Management.Automation;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "TmTestSet")]
[OutputType(typeof(Entities.TmTestSet))]
public class GetTmTestSetCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TmTestSetNameCompleter))]
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

    // The columns are New-TmTestSet's parameters, so `Import-Csv | New-TmTestSet` recreates the
    // test sets (without their test cases). Path and Name are wildcard-escaped (New-TmTestSet
    // unescapes the name); Labels is read per test set (see TmLabelCsv).
    private static readonly string DefaultCsvName = "ExportedTmTestSets.csv";
    private static readonly string[] CsvHeaders = [
        "Path",
        "Name",
        "Description",
        "EnableCoverage",
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
            dp => dp.drive.TmTestSets.Get(dp.project));

        using var cancelHandler = new ConsoleCancelHandler();
        using var reporter = new ProgressReporter(this, results.Count, "Getting test sets");
        foreach (var result in results)
        {
            try
            {
                var entity = results.GetResultWithProgress(result, reporter, cancelHandler.Token);
                if (entity is null) continue;

                var testSets = entity
                    .FilterByWildcards(e => e?.name, wpName)
                    .OrderBy(e => e.objKey!, ObjKeyComparer.Instance);
                if (writer is null)
                {
                    WriteObject(testSets, true);
                    continue;
                }

                var (drive, project) = result.Source;
                TmLabelCsv.ForEachWithLabels(this, drive, project, "testSet", testSets.ToList(), e => e.id, e => e.GetPSPath(),
                    (e, labels) => writer.WriteCsvLine([
                        EscapeCsvValue(e.Path, true),
                        EscapeCsvValue(e.name, true),
                        EscapeCsvValue(e.description),
                        EscapeCsvValue(e.enableCoverage),
                        EscapeCsvValue(labels)
                    ]),
                    reporter, cancelHandler.Token);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetTmTestSetError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }

        WriteCSVExportedMessage(this, providerCsvPath);
    }
}
