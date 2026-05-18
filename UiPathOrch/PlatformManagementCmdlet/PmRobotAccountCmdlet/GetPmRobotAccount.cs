using System.Management.Automation;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "PmRobotAccount", DefaultParameterSetName = psDefault)]
[OutputType(typeof(Entities.PmRobotAccount))]
[OutputType(typeof(Entities.PmRobotAccountExpanded))]
public class GetPmRobotAccountCmdlet : OrchestratorPSCmdlet
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
    private static void WriteCsvContent(StreamWriter writer, string drivePath, IEnumerable<PmRobotAccount> robotAccounts, Dictionary<string, PmGroup> groups)
    {
        // Write data rows
        foreach (var robotAccount in robotAccounts
            .OrderBy(ra => ra.name))
        {
            var line = new StringBuilder();
            line.Append($"{EscapeCsvValue(drivePath, true)},");
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

        // Fetch the robot-account list in parallel; per-org caches serialize
        // same-partition fetches internally. Filtering, the secondary
        // PmGroups lookup, expansion and WriteObject stay on the pipeline
        // thread (same split as Get-OrchBucket's CredentialStores lookup).
        using var poolResults = OrchThreadPool.RunForEach(drives,
            drive => drive.NameColonSeparator,
            drive => drive,
            drive => drive.PmRobotAccounts.Get());

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var poolResult in poolResults)
        {
            try
            {
                var fetched = poolResult.GetResult(cancelHandler.Token);
                var drive = poolResult.Source;

                var robotAccounts = fetched?
                    .Where(r => r is not null)
                    .FilterByWildcards(r => r?.name!, wpName)
                    .OrderBy(r => r?.name);

                if (robotAccounts is null) continue;

                var dicGroups = drive.PmGroups.Get().ToDictionary(g => g.id!);

                if (writer is not null)
                {
                    WriteCsvContent(writer, drive.NameColonSeparator, robotAccounts, dicGroups);
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
                                        Path = drive.NameColonSeparator,
                                        RobotAccount = robot!.name,
                                        PathRobotAccount = robot.GetPSPath(drive.NameColonSeparator),
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
                    WriteObject(robotAccounts.Select(r => { var c = r!.ShallowClone(); c.Path = drive.NameColonSeparator; return c; }), true);
                }
            }
            catch (OrchException ex)
            {
                // Parallel fetch failure — already wrapped with Path + Target.
                WriteError(new ErrorRecord(ex, "GetPmRobotAccountError", ErrorCategory.InvalidOperation, ex.Target));
            }
            catch (Exception ex)
            {
                // Failure in the main-thread post-processing (secondary
                // PmGroups lookup / expansion). Wrap per-drive as before.
                var drive = poolResult.Source;
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetPmRobotAccountError", ErrorCategory.InvalidOperation, drive));
            }
        }

        WriteCSVExportedMessage(this, providerCsvPath);
    }
}
