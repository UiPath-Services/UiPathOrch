using System.Management.Automation;
using System.Text;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "OrchPmGroup")]
    [OutputType(typeof(Entities.PmGroup))]
    [OutputType(typeof(Entities.DirectoryUser))]
    [OutputType(typeof(Entities.DirectoryGroup))]
    [OutputType(typeof(Entities.DirectoryRobotUser))]
    [OutputType(typeof(Entities.DirectoryApplication))]
    public class GetIdGroupCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(PmGroupNameCompleter<Positional.Name>))]
        [SupportsWildcards]
        public string[]? GroupName { get; set; }

        [Parameter]
        public SwitchParameter ExpandMembers { get; set; }

        [Parameter]
        [ArgumentCompleter(typeof(DriveCompleter<Positional.Name>))]
        public string[]? Path { get; set; }

        [Parameter]
        public string? ExportCsv { get; set; }

        [Parameter]
        [ArgumentCompleter(typeof(EncodingCompleter))]
        [EncodingArgumentTransformation]
        public Encoding? CsvEncoding { get; set; }

        private static readonly string DefaultCsvName = "ExportedPmGroups.csv";
        private static readonly string[] CsvHeaders = ["Path", "GroupName", "Type", "UserName"];

        // TODO: -ExpandedMembers を指定しない場合は、メンバを展開しない方が良いのではないか？
        private static void WriteCsvContent(StreamWriter writer, IEnumerable<PmGroup> output)
        {
            // 各グループに対してデータ行を書き込む
            foreach (var member in output
                .Where(g => g.members != null)
                .SelectMany(g => g.members!)
                .OrderBy(m => m.groupName)
                .ThenBy(m => m.objectType)
                .ThenBy(m => m.name))
            {
                var line = new StringBuilder();
                line.Append($"{EscapeCsvValue(member.Path, true)},");
                line.Append($"{EscapeCsvValue(member.groupName, true)},");
                line.Append($"{member.objectType},"); ////////// TODO: これ変換しないと。
                line.Append($"{EscapeCsvValue(member.name)}");
                writer.WriteLine(line.ToString());
            }
        }

        protected override void ProcessRecord()
        {
            var drives = OrchDriveInfo.EnumOrchDrives(Path);
            var wpGroupName = GroupName.ConvertToWildcardPatternList();

            ExportCsv = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
            using var writer = WriteCsvHeader(ExportCsv, CsvEncoding, CsvHeaders);

            using var results = OrchThreadPool.RunForEach(drives,
                drive => drive.NameColonSeparator,
                drive => drive,
                drive =>
                {
                    var ret = drive.GetPmGroups().Values;
                    if (ExpandMembers.IsPresent || !string.IsNullOrEmpty(ExportCsv))
                    {
                        var groups = ret
                            .Where(g => g != null)
                            .FilterByWildcards(g => g?.name!, wpGroupName);
                        foreach (var group in groups)
                        {
                            drive.GetPmGroup(group!.id!);
                        }
                        ret = drive.GetPmGroups().Values;
                    }
                    return ret;
                }
            );

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var result in results)
            {
                try
                {
                    var groups = result.GetResult(cancelHandler.Token);
                    if (groups == null) continue;

                    var targetGroups = groups
                        .Where(g => g != null)
                        .FilterByWildcards(g => g?.displayName!, wpGroupName)
                        .OrderBy(g => g!.name);

                    if (writer != null)
                    {
                        WriteCsvContent(writer, targetGroups!);
                    }
                    else if (ExpandMembers.IsPresent)
                    {
                        foreach (var group in targetGroups)
                        {
                            WriteObject(group!.members?
                                .OrderBy(m => m.objectType)
                                .ThenBy(m => m.displayName), true);
                        }
                    }
                    else
                    {
                        WriteObject(groups
                            .FilterByWildcards(g => g?.displayName!, wpGroupName)
                            .OrderBy(g => g!.displayName), true);
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
