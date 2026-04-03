using System.Management.Automation;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "PmRobotAccount", DefaultParameterSetName = psDefault)]
[OutputType(typeof(Entities.PmRobotAccount))]
[OutputType(typeof(Entities.PmRobotAccountExpanded))]
public class GetPmRobotAccountCommand : OrchestratorPSCmdlet
{
    private const string psDefault = "Default";
    private const string psExportCsv = "ExportCsv";

    [Parameter(ParameterSetName = psDefault, Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmRobotAccountNameCompleter))]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
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

    //private static void WriteCsvContent(StreamWriter writer, IEnumerable<PmRobotAccount> robotAccounts, ConcurrentDictionary<string, PmGroup> groups)
    private static void WriteCsvContent(StreamWriter writer, IEnumerable<PmRobotAccount> robotAccounts, Dictionary<string, PmGroup> groups)
    {
        // Write data rows
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
                    line.Append($",{EscapeCsvValue(group.name)}");
                }
            }

            writer.WriteLine(line.ToString());
        }
    }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumPmDrives(Path);
        var wpName = Name.ConvertToWildcardPatternList();

        var (physicalCsvPath, providerCsvPath) = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
        using var writer = WriteCsvHeader(physicalCsvPath, CsvEncoding, CsvHeaders);

        foreach (var drive in drives)
        {
            try
            {
                var robotAccounts = drive.PmRobotAccounts.Get()?
                    .Where(r => r is not null)
                    .FilterByWildcards(r => r?.name!, wpName)
                    .OrderBy(r => r?.name);

                if (robotAccounts is null) continue;

                var dicGroups = drive.PmGroups.Get().ToDictionary(g => g.id!);

                if (writer is not null)
                {
                    WriteCsvContent(writer, robotAccounts, dicGroups);
                }
                else if (ExpandGroup.IsPresent)
                {
                    var groups = drive.PmGroups.Get();

                    foreach (var robot in robotAccounts)
                    {
                        if (robot!.groupIds is not null && robot.groupIds.Length != 0)
                        {
                            foreach (var groupId in robot.groupIds)
                            {
                                var groupName = groups
                                    .Where(g => g is not null)
                                    .FirstOrDefault(g => g!.id == groupId)?.name;
                                if (groupName is not null)
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
            catch (Exception ex)
            {
                var errorRecord = new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetPmRobotAccountError", ErrorCategory.InvalidOperation, drive);
                WriteError(errorRecord);
            }
        }

        WriteCSVExportedMessage(this, providerCsvPath);
    }
}
