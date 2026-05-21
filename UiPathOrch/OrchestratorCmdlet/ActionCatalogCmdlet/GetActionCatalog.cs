using System.Management.Automation;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchActionCatalog")]
[OutputType(typeof(TaskCatalog))]
public class GetActionCatalogCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(ActionCatalogNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    [Parameter]
    public string? ExportCsv { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(EncodingCompleter))]
    [EncodingArgumentTransformation]
    public Encoding? CsvEncoding { get; set; }

    private static readonly string DefaultCsvName = "ExportedActionCatalogs.csv";

    // Column names align with New-OrchActionCatalog parameter names so
    // Get | Export-Csv | Import-Csv | New-OrchActionCatalog round-trips.
    private static readonly string[] CsvHeaders = [
        "Path",
        "Name",
        "Description",
        "Encrypted",
    ];

    private void WriteCsvContent(StreamWriter writer, IEnumerable<TaskCatalog> catalogs)
    {
        foreach (var c in catalogs)
        {
            string[] line = [
                EscapeCsvValue(c.Path, true),
                EscapeCsvValue(c.Name, true),
                EscapeCsvValue(c.Description),
                EscapeCsvValue(c.Encrypted),
            ];
            writer.WriteCsvLine(line);
        }
    }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();

        var (physicalCsvPath, providerCsvPath) = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
        using var writer = WriteCsvHeader(physicalCsvPath, CsvEncoding, CsvHeaders);

        using var results = OrchThreadPool.RunForEach(drivesFolders,
            df => df.folder.GetPSPath(),
            df => df.folder,
            df => df.drive.ActionCatalogs.Get(df.folder));

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var entities = result.GetResult(cancelHandler.Token);
                if (entities is null) continue;

                var filtered = entities
                    .FilterByWildcards(s => s?.Name, wpName)
                    .OrderBy(s => s.Name);

                if (writer is not null) { WriteCsvContent(writer, filtered); }
                else { WriteObject(filtered, true); }
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetActionCatalogError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }

        if (writer is not null)
        {
            WriteCSVExportedMessage(this, providerCsvPath);
        }
    }
}
