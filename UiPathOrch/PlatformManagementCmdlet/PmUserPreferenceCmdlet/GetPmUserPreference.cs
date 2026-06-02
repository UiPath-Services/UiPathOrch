using System.Management.Automation;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Reads a user's identity settings (theme, language, ...). Emits one
// PmUserPreference per (user, key); -ExportCsv writes Path,UserName,Key,Value so the
// output round-trips through Import-Csv | Set-PmUserPreference.
[Cmdlet(VerbsCommon.Get, "PmUserPreference")]
[OutputType(typeof(PmUserPreference))]
public class GetPmUserPreferenceCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmUserPreferenceUserNameCompleter))]
    [SupportsWildcards]
    public string[]? UserName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    [Parameter]
    public string? ExportCsv { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(EncodingCompleter))]
    [EncodingArgumentTransformation]
    public Encoding? CsvEncoding { get; set; }

    private static readonly string DefaultCsvName = "ExportedPmUserPreferences.csv";
    private static readonly string[] CsvHeaders = ["Path", "UserName", "Key", "Value"];

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumPmDrives(Path);
        var wpUserName = UserName.ConvertToWildcardPatternList();

        var (physicalCsvPath, providerCsvPath) = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
        using var writer = WriteCsvHeader(physicalCsvPath, CsvEncoding, CsvHeaders);

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives.WithCancellation(cancelHandler.Token))
        {
            string? partitionGlobalId;
            try
            {
                partitionGlobalId = drive.GetPartitionGlobalId();
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetGlobalPartitionIdError", ErrorCategory.InvalidOperation, drive));
                continue;
            }
            if (string.IsNullOrEmpty(partitionGlobalId)) continue;

            var users = drive.PmUsers.Get()
                .Where(u => u is not null && !string.IsNullOrEmpty(u.userName))
                .FilterByWildcards(u => u!.userName!, wpUserName)
                .OrderBy(u => u!.userName);

            foreach (var user in users.WithCancellation(cancelHandler.Token))
            {
                PmUserSettingDto[]? settings;
                try
                {
                    settings = drive.OrchAPISession.GetUserSettings(partitionGlobalId, user!.id!);
                }
                catch (Exception ex)
                {
                    string t = System.IO.Path.Combine(drive.NameColonSeparator, user!.userName!);
                    WriteError(new ErrorRecord(new OrchException(t, ex), "GetPmUserPreferenceError", ErrorCategory.InvalidOperation, t));
                    continue;
                }

                foreach (var s in (settings ?? []).OrderBy(s => s.key))
                {
                    if (writer is not null)
                    {
                        writer.WriteCsvLine([
                            EscapeCsvValue(drive.NameColonSeparator, true),
                            EscapeCsvValue(user!.userName, true),
                            EscapeCsvValue(s.key),
                            EscapeCsvValue(s.value),
                        ]);
                    }
                    else
                    {
                        WriteObject(new PmUserPreference
                        {
                            Path = drive.NameColonSeparator,
                            UserName = user!.userName,
                            Key = s.key,
                            Value = s.value,
                        });
                    }
                }
            }
        }

        if (writer is not null)
        {
            WriteCSVExportedMessage(this, providerCsvPath);
        }
    }
}
