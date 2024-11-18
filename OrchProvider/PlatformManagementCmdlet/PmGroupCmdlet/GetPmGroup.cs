using System.Management.Automation;
using System.Text;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "OrchPmGroup")]
    [OutputType(typeof(Entities.PmGroup))]
    [OutputType(typeof(Entities.DirectoryUser))]
    [OutputType(typeof(Entities.DirectoryGroup))]
    [OutputType(typeof(Entities.DirectoryRobotUser))]
    [OutputType(typeof(Entities.DirectoryApplication))]
    public class GetPmGroupCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(PmGroupNameCompleter<GroupName>))]
        [SupportsWildcards]
        public string[]? GroupName { get; set; }

        [Parameter]
        public SwitchParameter ExpandMembers { get; set; }

        [Parameter]
        [ArgumentCompleter(typeof(DriveCompleter<GroupName>))]
        public string[]? Path { get; set; }

        [Parameter]
        public string? ExportCsv { get; set; }

        [Parameter]
        [ArgumentCompleter(typeof(EncodingCompleter))]
        [EncodingArgumentTransformation]
        public Encoding? CsvEncoding { get; set; }

        private static readonly string DefaultCsvName = "ExportedPmGroups.csv";
        private static readonly string[] CsvHeaders = ["Path", "GroupName", "Type", "UserName", "Email", "Source"];

        // TODO: -ExpandedMembers を指定しない場合は、メンバを展開しない方が良いのではないか？
        private static void WriteCsvContent(StreamWriter writer, PmGroup group)
        {
            // 各グループに対してデータ行を書き込む
            if (group?.members == null) return;

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
                WriteCsvLine(writer, line);
            }
        }

        protected override void ProcessRecord()
        {
            var drives = OrchDriveInfo.EnumOrchDrives(Path);
            var wpGroupName = GroupName.ConvertToWildcardPatternList();

            ExportCsv = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
            using var writer = WriteCsvHeader(ExportCsv, CsvEncoding, CsvHeaders);

            foreach (var drive in drives)
            {
                var groups = drive.GetPmGroups().Values
                    .Where(g => g != null)
                    .FilterByWildcards(g => g?.name!, wpGroupName)
                    .OrderBy(g => g.name);

                if (!ExpandMembers && writer == null)
                {
                    WriteObject(groups, true);
                }
                else
                {
                    using var results = OrchThreadPool.RunForEach(groups,
                        group => group.GetPSPath(),
                        group => group,
                        group => drive.GetPmGroup(group.id)
                    );

                    using var cancelHandler = new ConsoleCancelHandler();
                    foreach (var result in results)
                    {
                        try
                        {
                            var detailedGroup = result.GetResult(cancelHandler.Token);
                            if (detailedGroup == null) continue;

                            if (writer != null)
                            {
                                WriteCsvContent(writer, detailedGroup);
                            }
                            else
                            {
                                if (detailedGroup.members == null) continue;

                                WriteObject(detailedGroup.members.OrderBy(m => m.name), true);
                            }
                        }
                        catch (OrchException ex)
                        {
                            WriteError(new ErrorRecord(ex, "GetPmGroupError", ErrorCategory.InvalidOperation, ex.Target));
                        }
                    }

                    WriteCSVExportedMessage(this, ExportCsv);
                }
            }
        }
    }
}
