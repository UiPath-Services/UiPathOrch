using System.Management.Automation;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "PmGroupMember")]
[OutputType(typeof(Entities.DirectoryUser))]
[OutputType(typeof(Entities.DirectoryGroup))]
[OutputType(typeof(Entities.DirectoryRobotUser))]
[OutputType(typeof(Entities.DirectoryApplication))]
public class GetPmGroupMemberCommand : OrchestratorPSCmdlet
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
    private static readonly string[] CsvHeaders = ["Path", "GroupName", "Type", "UserName", "Email", "Source"];

    private static void WriteCsvContent(StreamWriter writer, PmGroup group)
    {
        // Write data rows for each group
        if (group?.members is null) return;

        foreach (var member in group.members
            .OrderBy(m => m.groupName)
            .ThenBy(m => m.objectType)
            .ThenBy(m => m.name))
        {
            string[] line = [
                EscapeCsvValue(member.Path, true),
                EscapeCsvValue(member.groupName, true),
                EscapeCsvValue(member.objectType), ////////// TODO: Does this need conversion?
                EscapeCsvValue(member.name),
                member.email ?? "",
                member.source ?? ""
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

        foreach (var drive in drives)
        {
            var groups = drive.PmGroups.Get()
                .Where(g => g is not null)
                .FilterByWildcards(g => g?.name!, wpGroupName)
                .OrderBy(g => g.name);

            foreach (var group in groups)
            {
                try
                {
                    var detailedGroup = drive.PmGroups.Get(group.id);
                    if (detailedGroup is null) continue;

                    if (writer is not null)
                    {
                        WriteCsvContent(writer, detailedGroup);
                    }
                    else
                    {
                        if (detailedGroup.members is null) continue;

                        WriteObject(detailedGroup.members.OrderBy(m => m.name), true);
                    }
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(group.GetPSPath(), ex), "GetPmGroupMemberError", ErrorCategory.InvalidOperation, group));
                }
            }

            WriteCSVExportedMessage(this, providerCsvPath);
        }
    }
}
