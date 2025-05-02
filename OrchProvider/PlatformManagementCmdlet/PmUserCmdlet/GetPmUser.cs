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
    [ArgumentCompleter(typeof(PmUserEmailCompleter<TPositional>))]
    [SupportsWildcards]
    [Alias("UserName")]
    public string[]? Email { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
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
            #region groupId を groupName に変換
            List<string> groupNames = [];
            if (user.groupIDs is not null && user.groupIDs.Any())
            {
                var groups = drive.GetPmGroups();
                foreach (var groupId in user.groupIDs)
                {
                    if (groups.TryGetValue(groupId, out var group))
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

        var drives = OrchDriveInfo.EnumPmDrives(Path);
        var wpEmail = Email.ConvertToWildcardPatternList();

        using var results = OrchThreadPool.RunForEach(drives,
            drive => drive.NameColonSeparator,
            drive => drive,
            drive => drive.PmUsers.Get());

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            cancelHandler.Token.ThrowIfCancellationRequested();

            try
            {
                var entities = result.GetResult(cancelHandler.Token);
                if (entities is null) continue;

                var targetUsers = entities
                    .FilterByWildcards(u => u?.email, wpEmail)
                    .OrderBy(u => u.email);

                if (writer is not null)
                {
                    var drive = result.Source;
                    WriteCsvContent(writer, drive, targetUsers);
                }
                else
                {
                    WriteObject(targetUsers, true);
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
