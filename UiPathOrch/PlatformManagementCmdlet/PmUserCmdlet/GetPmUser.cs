using System.Management.Automation;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "PmUser")]
[OutputType(typeof(Entities.PmUser))]
public class GetPmUserCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmUserEmailCompleter))]
    [SupportsWildcards]
    [Alias("UserName")]
    public string[]? Email { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    [Parameter]
    public string? ExportCsv { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(EncodingCompleter))]
    [EncodingArgumentTransformation]
    public Encoding? CsvEncoding { get; set; }

    private static readonly string DefaultCsvName = "ExportedPmUsers.csv";
    private static readonly string[] CsvHeaders = [
        "Path",
        "Email",
        "Name",
        "SurName",
        "DisplayName",
        "Type",
        "BypassBasicAuthRestriction",
        "InvitationAccepted",
        "GroupName"
    ];

    private void WriteCsvContent(StreamWriter writer, OrchDriveInfo drive, IEnumerable<PmUser> output)
    {
        foreach (var user in output)
        {
            #region Convert groupId to groupName
            List<string> groupNames = [];
            if (user.groupIDs is not null && user.groupIDs.Any())
            {
                var groups = drive.PmGroups.Get();
                foreach (var groupId in user.groupIDs)
                {
                    var group = groups.FirstOrDefault(g => g.id == groupId); // Should we convert to a dictionary before searching?
                    if (group is not null)
                    {
                        if (!string.IsNullOrEmpty(group.name))
                        {
                            groupNames.Add(group.name);
                        }
                    }
                }
            }
            #endregion

            string[] line;
            //if (string.IsNullOrEmpty(user.email) && (user.userName?.Contains('@') ?? false))
            if (string.IsNullOrEmpty(user.email))
            {
                line = [
                    EscapeCsvValue(drive.NameColonSeparator, true),
                    "",
                    EscapeCsvValue(user.userName),
                    EscapeCsvValue(user.surname),
                    EscapeCsvValue(user.name),
                    EscapeCsvValue("user"),
                    EscapeCsvValue(user.bypassBasicAuthRestriction),
                    EscapeCsvValue(user.invitationAccepted),
                    EscapeCsvValue(groupNames)
                ];
            }
            else
            {
                line = [
                    EscapeCsvValue(drive.NameColonSeparator, true),
                    EscapeCsvValue(user.email),
                    EscapeCsvValue(string.Compare(user.email, user.userName, StringComparison.OrdinalIgnoreCase) == 0 ? user.name : user.userName),
                    EscapeCsvValue(user.surname),
                    EscapeCsvValue(string.Compare(user.email, user.userName, StringComparison.OrdinalIgnoreCase) != 0 && string.IsNullOrEmpty(user.displayName) ? user.name : user.displayName),
                    EscapeCsvValue("user"),
                    EscapeCsvValue(user.bypassBasicAuthRestriction),
                    EscapeCsvValue(user.invitationAccepted),
                    EscapeCsvValue(groupNames)
                ];
            }
            writer.WriteCsvLine(line);
        }
    }

    protected override void ProcessRecord()
    {
        var (physicalCsvPath, providerCsvPath) = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
        using var writer = WriteCsvHeader(physicalCsvPath, CsvEncoding, CsvHeaders);

        var drives = SessionState.EnumPmDrives(Path);
        var wpEmail = Email.ConvertToWildcardPatternList();

        // Fetch the user list in parallel; per-org caches serialize
        // same-partition fetches internally. Filtering, the secondary
        // PmGroups lookup inside WriteCsvContent, and WriteObject stay on
        // the pipeline thread (same split as Get-OrchBucket's
        // CredentialStores lookup).
        using var results = OrchThreadPool.RunForEach(drives,
            drive => drive.NameColonSeparator,
            drive => drive,
            drive => drive.PmUsers.Get());

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var entities = result.GetResult(cancelHandler.Token);
                if (entities is null) continue;

                var drive = result.Source;
                var targetUsers = entities
                    .FilterByWildcards(u => u?.email, wpEmail)
                    .OrderBy(u => u.email);

                if (writer is not null)
                {
                    WriteCsvContent(writer, drive, targetUsers);
                }
                else
                {
                    WriteObject(targetUsers.Select(u => { var c = u.ShallowClone(); c.Path = drive.NameColonSeparator; return c; }), true);
                }
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetPmUserError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }

        WriteCSVExportedMessage(this, providerCsvPath);
    }
}
