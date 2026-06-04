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
    // Why the header is "UserName" even though the robot account's identifier
    // field is `name` (PmRobotAccount.name / CreateRobotAccountCommand.name):
    //
    // A robot account is a *principal*, and the identity family addresses
    // principals (users AND robot accounts) uniformly as "UserName"
    // (Add-/Remove-/Move-PmGroupMember, the license cmdlets). In those member
    // cmdlets the member parameter is -UserName and "Name" is already an alias
    // of -GroupName, so a robot account's member-identifier column MUST be
    // "UserName" — a "Name" column would bind the group, not the member.
    //
    // Surfacing the column as "UserName" therefore lets ONE exported CSV both
    // (a) import into Add-PmGroupMember as a member, and (b) round-trip into
    // New-/Set-PmRobotAccount, whose primary -Name parameter accepts the
    // "UserName" column via its -UserName alias. (-Name is primary so the
    // cmdlet still matches the entity/API field, Get-/Remove-PmRobotAccount,
    // and the Get | Set object pipe.) The value written is the account's `name`.
    private static readonly string[] CsvHeaders =
        ["Path", "UserName", "GroupName"];

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

            // All group memberships go in one comma-separated GroupName column
            // (EscapeCsvValue quotes it because of the commas); Import-Csv |
            // Set-PmRobotAccount splits it back on commas via -GroupName. This
            // replaces the legacy fixed GroupName0..GroupName9 columns, which
            // also capped a robot at 10 groups.
            var groupNames = (robotAccount.groupIds ?? [])
                .Where(id => groups.ContainsKey(id))
                .Select(id => groups[id].name)
                .Where(name => !string.IsNullOrEmpty(name));
            line.Append($",{EscapeCsvValue(string.Join(",", groupNames))}");

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
