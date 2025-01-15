using System.Collections.Concurrent;
using System.Management.Automation;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "OrchPmRobotAccount", DefaultParameterSetName = psDefault)]
    [OutputType(typeof(Entities.PmRobotAccount))]
    [OutputType(typeof(Entities.PmRobotAccountExpanded))]
    public class GetPmRobotAccountCommand : OrchestratorPSCmdlet
    {
        private const string psDefault = "Default";
        private const string psExportCsv = "ExportCsv";

        [Parameter(ParameterSetName = psDefault, Position = 0)]
        [ArgumentCompleter(typeof(PmRobotAccountNameCompleter<TPositional>))]
        public string[]? Name { get; set; }

        [Parameter]
        [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
        public string[]? Path { get; set; }

        [Parameter(ParameterSetName = psDefault)]
        public SwitchParameter ExpandGroup { get; set; }

        [Parameter(ParameterSetName = psExportCsv)]
        public string? ExportCsv { get; set; }

        [Parameter(ParameterSetName = psExportCsv, Position = 0)]
        [ArgumentCompleter(typeof(EncodingCompleter))]
        [EncodingArgumentTransformation]
        public Encoding? CsvEncoding { get; set; }

        private static readonly string DefaultCsvName = "ExportedPmRobotAccounts.csv";
        private static readonly string[] CsvHeaders = 
            ["Path", "UserName", "GroupName0", "GroupName1", "GroupName2", "GroupName3", "GroupName4", "GroupName5", "GroupName6", "GroupName7", "GroupName8", "GroupName9"];

        private static void WriteCsvContent(StreamWriter writer, IEnumerable<PmRobotAccount> robotAccounts, ConcurrentDictionary<string, PmGroup> groups)
        {
            // データ行を書き込む
            foreach (var robotAccount in robotAccounts
                .OrderBy(ra => ra.name))
            {
                var line = new StringBuilder();
                line.Append($"{EscapeCsvValue(robotAccount.Path, true)},");
                line.Append($"{EscapeCsvValue(robotAccount.name, true)}");

                foreach (var groupId in robotAccount.groupIds ?? [])
                {
                    if (groups.TryGetValue(groupId, out var group))
                    {
                        line.Append($",{EscapeCsvValue(group?.name!)}");
                    }
                }

                writer.WriteLine(line.ToString());
            }
        }

        protected override void ProcessRecord()
        {
            var drives = OrchDriveInfo.EnumOrchDrives(Path);
            var wpName = Name.ConvertToWildcardPatternList();

            var (physicalCsvPath, providerCsvPath) = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
            using var writer = WriteCsvHeader(physicalCsvPath, CsvEncoding, CsvHeaders);

            using var results = OrchThreadPool.RunForEach(drives,
                drive => drive.NameColonSeparator,
                drive => drive,
                drive => drive.PmRobotAccounts.Get());

            if (ExpandGroup.IsPresent)
            {
                ParallelResults.ForEach(drives, drive => drive.GetPmGroups());
            }

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var result in results)
            {
                try
                {
                    var robotAccounts = result.GetResult(cancelHandler.Token)?
                        .Where(r => r != null)
                        .FilterByWildcards(r => r?.name!, wpName)
                        .OrderBy(r => r?.name);

                    if (robotAccounts == null) continue;
                    var drive = result.Source;

                    if (writer != null)
                    {
                        WriteCsvContent(writer, robotAccounts, drive!.GetPmGroups());
                    }
                    else if (ExpandGroup.IsPresent)
                    {
                        var groups = drive!.GetPmGroups().Values;

                        foreach (var robot in robotAccounts)
                        {
                            if (robot!.groupIds != null && robot.groupIds.Length != 0)
                            {
                                foreach (var groupId in robot.groupIds)
                                {
                                    var groupName = groups
                                        .Where(g => g != null)
                                        .FirstOrDefault(g => g!.id == groupId)?.name;
                                    if (groupName != null)
                                    {
                                        var r = new PmRobotAccountExpanded()
                                        {
                                            Path = robot.Path,
                                            RobotAccount = robot!.name,
                                            PathRobotAccount = robot.GetPSPath(),
                                            groupId = groupId,
                                            groupName = groupName
                                        };
                                        WriteObject(r);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        WriteObject(robotAccounts, true);
                    }
                }
                catch (OrchException ex)
                {
                    var errorRecord = new ErrorRecord(ex, "GetPmRobotAccountError", ErrorCategory.InvalidOperation, ex.Target);
                    WriteError(errorRecord);
                }
            }

            WriteCSVExportedMessage(this, providerCsvPath);
        }
    }
}
