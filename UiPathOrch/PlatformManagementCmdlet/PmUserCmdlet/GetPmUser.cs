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

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

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
        "UserName",
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

            writer.WriteCsvLine(BuildUserCsvRow(drive.NameColonSeparator, user, groupNames));
        }
    }

    // -ExportCsv row: one column per CsvHeaders entry, laid out to match the New-PmUser
    // import parameters so the export round-trips — including a userName that differs
    // from the email on Automation Suite / on-premises (previously there was no UserName
    // column, so a distinct userName was smuggled into the Name column and lost on
    // re-import). Type is always "user"; groupNames are resolved by the caller. Static so
    // the column layout is unit-testable.
    internal static string[] BuildUserCsvRow(string path, PmUser user, IEnumerable<string> groupNames) =>
    [
        EscapeCsvValue(path, true),
        EscapeCsvValue(user.email),
        EscapeCsvValue(user.userName),
        EscapeCsvValue(user.name),
        EscapeCsvValue(user.surname),
        EscapeCsvValue(user.displayName),
        EscapeCsvValue("user"),
        EscapeCsvValue(user.bypassBasicAuthRestriction),
        EscapeCsvValue(user.invitationAccepted),
        EscapeCsvValue(groupNames, true)
    ];

    protected override void ProcessRecord()
    {
        var (physicalCsvPath, providerCsvPath) = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
        using var writer = WriteCsvHeader(physicalCsvPath, CsvEncoding, CsvHeaders);

        var drives = SessionState.EnumPmDrives(EffectivePath(Path, LiteralPath));
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
                // -UserName is an alias of -Email; match a pattern against EITHER the
                // userName or the email so a userName that differs from the email (or a
                // userName-only account with no email) still resolves. Mirrors how
                // Get-OrchUser matches UserName/EmailAddress.
                var targetUsers = entities
                    .FilterByWildcardsAny([u => u?.userName, u => u?.email], wpEmail)
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
