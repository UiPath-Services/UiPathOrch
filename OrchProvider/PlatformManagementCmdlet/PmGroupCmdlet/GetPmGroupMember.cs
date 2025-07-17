using System.Management.Automation;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using TPositional = UiPath.PowerShell.Positional.GroupName;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "PmGroupMember")]
[OutputType(typeof(Entities.DirectoryUser))]
[OutputType(typeof(Entities.DirectoryGroup))]
[OutputType(typeof(Entities.DirectoryRobotUser))]
[OutputType(typeof(Entities.DirectoryApplication))]
public class GetPmGroupMemberCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmGroupNameCompleter<TPositional>))]
    [SupportsWildcards]
    [Alias("Name")]
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
    private static readonly string[] CsvHeaders = ["Path", "GroupName", "Type", "UserName", "Email", "Source"];

    private static void WriteCsvContent(StreamWriter writer, PmGroup group)
    {
        // 各グループに対してデータ行を書き込む
        if (group?.members is null) return;

        foreach (var member in group.members
            .OrderBy(m => m.groupName)
            .ThenBy(m => m.objectType)
            .ThenBy(m => m.name))
        {
            string[] line = [
                EscapeCsvValue(member.Path, true),
                EscapeCsvValue(member.groupName, true),
                EscapeCsvValue(member.objectType), ////////// TODO: これ変換必要だっけ？
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

            using var results = OrchThreadPool.RunForEach(groups,
                group => group.GetPSPath(),
                group => group,
                group => drive.PmGroups.Get(group.id)
            );

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var result in results)
            {
                try
                {
                    var detailedGroup = result.GetResult(cancelHandler.Token);
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
                catch (OrchException ex)
                {
                    WriteError(new ErrorRecord(ex, "GetPmGroupMemberError", ErrorCategory.InvalidOperation, ex.Target));
                }
            }

            WriteCSVExportedMessage(this, providerCsvPath);
        }
    }
}
