using System.Management.Automation;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "DuUser")]
[OutputType(typeof(Entities.DuUser))]
public class GetDuUserCmdlet : OrchestratorPSCmdlet
{
    //private const string UserNameSet = "UserNameSet";
    //private const string UserSet = "UserSet";

    // Feature request from Mishima-san (KDDI): allow specifying User Principal Name in Add-DuUser.
    // The code below would be needed for that, but I can't think of a good implementation.
    // Either sacrifice performance, or add complex parameters..
    // Personally, neither option is acceptable..
    //[Parameter(ParameterSetName = UserNameSet, Position = 0, ValueFromPipelineByPropertyName = true)]
    //[ArgumentCompleter(typeof(DuUserNameCompleter))]
    //[SupportsWildcards]
    //public string[]? UserName { get; set; }

    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DuNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public string? ExportCsv { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(EncodingCompleter))]
    [EncodingArgumentTransformation]
    public Encoding? CsvEncoding { get; set; }

    private static readonly string DefaultCsvName = "ExportedDuUsers.csv";
    private static readonly string[] CsvHeaders = [
        "Path",
        "Type",
        "Name",
        "Roles"
    ];

    private static void WriteCsvContent(StreamWriter writer, OrchDriveInfo drive, IEnumerable<DuUser?> users)
    {
        if (users is null) return;

        foreach (var user in users)
        {
            if (user is null) continue;

            string[] line = [
                EscapeCsvValue(user.Path, true),
                EscapeCsvValue(user.type, true),
                EscapeCsvValue(user.Name), // Add-DuUser's -DisplayName does not support wildcards
                EscapeCsvValue(user.roleAssignmentDtos?.Select(r => r.roleName), true)
            ];

            writer.WriteCsvLine(line);
        }
    }

    protected override void ProcessRecord()
    {
        var drivesProjects = SessionState.EnumDuFolders(Path, Recurse);
        //var wpUserName = UserName.ConvertToWildcardPatternList();
        var wpName = Name.ConvertToWildcardPatternList();

        var (physicalCsvPath, providerCsvPath) = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
        using var writer = WriteCsvHeader(physicalCsvPath, CsvEncoding, CsvHeaders);

        using var results = OrchThreadPool.RunForEach(drivesProjects,
            dp => dp.project.GetPSPath(),
            dp => dp.project,
            dp => dp.drive.GetDuUsers(dp.project));

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var entities = result.GetResult(cancelHandler.Token);
                if (entities is null) continue;

                var (drive, _) = result.Source;

                var targetEntities = entities
                        //.FilterByWildcards(u => u?.UserName, wpUserName)
                        .FilterByWildcards(u => u?.Name, wpName)
                        .OrderBy(e => e.Name);

                if (writer is not null)
                {
                    WriteCsvContent(writer, drive.ParentDrive, targetEntities);
                }
                else
                {
                    WriteObject(targetEntities, true);
                }
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetDuUserError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }

        WriteCSVExportedMessage(this, providerCsvPath);
    }
}
