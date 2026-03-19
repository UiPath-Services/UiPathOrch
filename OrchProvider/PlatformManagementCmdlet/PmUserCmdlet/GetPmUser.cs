using System.Management.Automation;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using TPositional = UiPath.PowerShell.Positional.Email;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "PmUser")]
[OutputType(typeof(Entities.PmUser))]
public class GetPmUserCommand : OrchestratorPSCmdlet
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
                    EscapeCsvValue(user.Path, true),
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
                    EscapeCsvValue(user.Path, true),
                    EscapeCsvValue(user.email),
                    EscapeCsvValue(string.Compare(user.email, user.userName, true) == 0 ? user.name : user.userName),
                    EscapeCsvValue(user.surname),
                    EscapeCsvValue(string.Compare(user.email, user.userName, true) != 0 && string.IsNullOrEmpty(user.displayName) ? user.name : user.displayName),
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

        foreach (var drive in drives)
        {
            try
            {
                var entities = drive.PmUsers.Get();
                if (entities is null) continue;

                var targetUsers = entities
                    .FilterByWildcards(u => u?.email, wpEmail)
                    .OrderBy(u => u.email);

                if (writer is not null)
                {
                    WriteCsvContent(writer, drive, targetUsers);
                }
                else
                {
                    WriteObject(targetUsers, true);
                }
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetPmUserError", ErrorCategory.InvalidOperation, drive));
            }
        }

        WriteCSVExportedMessage(this, providerCsvPath);
    }
}
