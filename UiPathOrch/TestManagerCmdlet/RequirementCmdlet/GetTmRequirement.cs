using System.Management.Automation;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "TmRequirement")]
[OutputType(typeof(Entities.TmRequirement))]
public class GetTmRequirementCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TmRequirementNameCompleter))]
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

    // The columns are New-TmRequirement's parameters, so `Import-Csv | New-TmRequirement`
    // recreates the requirements. Path and Name are wildcard-escaped (New-TmRequirement unescapes
    // the name); Labels is read per requirement (see TmLabelCsv).
    private static readonly string DefaultCsvName = "ExportedTmRequirements.csv";
    private static readonly string[] CsvHeaders = [
        "Path",
        "Name",
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
            dp => dp.drive.TmRequirements.Get(dp.project));

        using var cancelHandler = new ConsoleCancelHandler();
        using var reporter = new ProgressReporter(this, results.Count, "Getting requirements");
        foreach (var result in results)
        {
            try
            {
                var entity = results.GetResultWithProgress(result, reporter, cancelHandler.Token);
                if (entity is null) continue;

                var requirements = entity
                    .FilterByWildcards(e => e?.name, wpName)
                    .OrderBy(e => e.objKey!, ObjKeyComparer.Instance);
                if (writer is null)
                {
                    WriteObject(requirements, true);
                    continue;
                }

                var (drive, project) = result.Source;
                TmLabelCsv.ForEachWithLabels(this, drive, project, "requirement", requirements.ToList(), e => e.id, e => e.GetPSPath(),
                    (e, labels) => writer.WriteCsvLine([
                        EscapeCsvValue(e.Path, true),
                        EscapeCsvValue(e.name, true),
                        EscapeCsvValue(e.description),
                        EscapeCsvValue(labels)
                    ]),
                    reporter, cancelHandler.Token);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetTmRequirementError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }

        WriteCSVExportedMessage(this, providerCsvPath);
    }
}
