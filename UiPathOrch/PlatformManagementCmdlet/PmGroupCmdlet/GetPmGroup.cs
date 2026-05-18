using System.Management.Automation;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "PmGroup")]
[OutputType(typeof(Entities.PmGroup))]
public class GetPmGroupCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmGroupNameCompleter))]
    [SupportsWildcards]
    [Alias("Name")]
    public string[]? GroupName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    [Parameter]
    public string? ExportCsv { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(EncodingCompleter))]
    [EncodingArgumentTransformation]
    public Encoding? CsvEncoding { get; set; }

    private static readonly string DefaultCsvName = "ExportedPmGroups.csv";
    private static readonly string[] CsvHeaders = ["Path", "GroupName"];

    private static void WriteCsvContent(StreamWriter writer, string drivePath, IEnumerable<PmGroup> groups)
    {
        foreach (var group in groups.OrderBy(g => g.name))
        {
            string[] line = [
                EscapeCsvValue(drivePath, true),
                EscapeCsvValue(group.name, true)
            ];
            writer.WriteCsvLine(line);
        }
    }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumPmDrives(Path);
        var wpGroupName = GroupName.ConvertToWildcardPatternList();

        var (physicalCsvPath, providerCsvPath) = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
        using var writer = WriteCsvHeader(physicalCsvPath, CsvEncoding, CsvHeaders);

        // Fetch in parallel; per-org caches serialize same-partition fetches
        // internally. Filtering / WriteObject / CSV stay on the pipeline
        // thread. No try/catch here — as in the original, a fetch failure
        // propagates and terminates the cmdlet (surfaced by GetResult).
        using var results = OrchThreadPool.RunForEach(drives,
            drive => drive.NameColonSeparator,
            drive => drive,
            drive => drive.PmGroups.Get());

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            var entities = result.GetResult(cancelHandler.Token);
            if (entities is null) continue;
            var drive = result.Source;
            var groups = entities
                .Where(g => g is not null)
                .FilterByWildcards(g => g?.name!, wpGroupName)
                .OrderBy(g => g.name);

            if (writer is null)
            {
                WriteObject(groups.Select(g => { var c = g.ShallowClone(); c.Path = drive.NameColonSeparator; return c; }), true);
            }
            else
            {
                WriteCsvContent(writer, drive.NameColonSeparator, groups);
                WriteCSVExportedMessage(this, providerCsvPath);
            }
        }
    }
}
