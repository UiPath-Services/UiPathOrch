using System.Management.Automation;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using TPositional = UiPath.PowerShell.Positional.GroupName;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "PmGroup")]
[OutputType(typeof(Entities.PmGroup))]
public class GetPmGroupCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmGroupNameCompleter<TPositional>))]
    [SupportsWildcards]
    public string[]? GroupName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
    public string[]? Path { get; set; }

    [Parameter]
    public string? ExportCsv { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(EncodingCompleter))]
    [EncodingArgumentTransformation]
    public Encoding? CsvEncoding { get; set; }

    private static readonly string DefaultCsvName = "ExportedPmGroups.csv";
    private static readonly string[] CsvHeaders = ["Path", "GroupName"];

    private static void WriteCsvContent(StreamWriter writer, IEnumerable<PmGroup> groups)
    {
        foreach (var group in groups.OrderBy(g => g.name))
        {
            string[] line = [
                EscapeCsvValue(group.Path, true),
                EscapeCsvValue(group.name, true)
            ];
            WriteCsvLine(writer, line);
        }
    }

    protected override void ProcessRecord()
    {
        var drives = OrchDriveInfo.EnumPmDrives(Path);
        var wpGroupName = GroupName.ConvertToWildcardPatternList();

        var (physicalCsvPath, providerCsvPath) = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
        using var writer = WriteCsvHeader(physicalCsvPath, CsvEncoding, CsvHeaders);

        foreach (var drive in drives)
        {
            var groups = drive.GetPmGroups().Values
                .Where(g => g is not null)
                .FilterByWildcards(g => g?.name!, wpGroupName)
                .OrderBy(g => g.name);

            if (writer is null)
            {
                WriteObject(groups, true);
            }
            else
            {
                WriteCsvContent(writer, groups);
                WriteCSVExportedMessage(this, providerCsvPath);
            }
        }
    }
}
