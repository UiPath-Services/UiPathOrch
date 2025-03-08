using System.Management.Automation;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "DuUser")]
[OutputType(typeof(Entities.DuUser))]
public class GetDuUserCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DuUserNameCompleter<TPositional>))]
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

            //string? key = !string.IsNullOrEmpty(user.displayName) ? user.displayName : user.email;
            //if (string.IsNullOrEmpty(key)) continue;

            //var pmUsers = drive.SearchPmDirectory(key);
            //if (pmUsers is null) continue;
            //PmDirectoryEntityInfo pmUser = pmUsers.Where(u => u.identifier == user.securityPrincipalId).FirstOrDefault();

            string name = !string.IsNullOrEmpty(user.email) ? user.email : user.displayName;
            if (string.IsNullOrEmpty(name)) name = user.securityPrincipalId;

            string[] line = [
                EscapeCsvValue(user.Path, true),
                EscapeCsvValue(user.type, true),
                EscapeCsvValue(name), // Add-DuUser の -Name はワイルドカードをサポートしない
                EscapeCsvValue(user.roleAssignmentDtos?.Select(r => r.roleName), true)
            ];

            WriteCsvLine(writer, line);
        }
    }

    protected override void ProcessRecord()
    {
        var drivesProjects = OrchDuDriveInfo.EnumFolders(Path, Recurse.IsPresent);
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
                        .FilterByWildcards(u => u?.displayName, wpName)
                        .OrderBy(e => e.displayName);

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
