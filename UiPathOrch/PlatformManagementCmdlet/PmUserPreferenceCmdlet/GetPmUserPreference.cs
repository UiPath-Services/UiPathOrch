using System.Management.Automation;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Reads the connected user's own portal preferences (theme, language, ...). Emits
// one PmUserPreference per key; -ExportCsv writes Path,Key,Value so the output
// round-trips through Import-Csv | Set-PmUserPreference. The cmdlet always acts on
// the user behind the drive's token, so it requires a non-confidential app or PAT.
[Cmdlet(VerbsCommon.Get, "PmUserPreference")]
[OutputType(typeof(PmUserPreference))]
public class GetPmUserPreferenceCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmPreferenceKeyCompleter))]
    public string[]? Key { get; set; }

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

    private static readonly string DefaultCsvName = "ExportedPmUserPreferences.csv";
    private static readonly string[] CsvHeaders = ["Path", "Key", "Value"];

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumPmDrives(EffectivePath(Path, LiteralPath));
        var keys = (Key is { Length: > 0 }) ? Key : PmUserPreferenceKeys.ReadDefaults;

        var (physicalCsvPath, providerCsvPath) = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
        using var writer = WriteCsvHeader(physicalCsvPath, CsvEncoding, CsvHeaders);

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives.WithCancellation(cancelHandler.Token))
        {
            // Surfaces a clear error on confidential-app drives (no user) and skips
            // them; also primes auth/partition for the cached lookups below.
            if (PmUserPreferenceCurrentUser.Resolve(this, drive) is null) continue;

            // "<Path><connected user>" for the grouped view header (display only).
            var pathUserName = drive.NameColonSeparator + drive.CurrentUser.Get()?.UserName;

            List<PmUserSettingDto> settings = [];
            try
            {
                foreach (var key in keys)
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();
                    var s = drive.PmUserPreferences.Get(key);
                    if (s is not null && !string.IsNullOrEmpty(s.key)) settings.Add(s);
                }
            }
            catch (OperationCanceledException)
            {
                throw; // Ctrl+C: propagate the stop instead of one canceled-error per drive
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetPmUserPreferenceError", ErrorCategory.InvalidOperation, drive));
                continue;
            }

            foreach (var s in settings.OrderBy(s => s.key))
            {
                if (writer is not null)
                {
                    writer.WriteCsvLine([
                        EscapeCsvValue(drive.NameColonSeparator, true),
                        EscapeCsvValue(s.key),
                        EscapeCsvValue(s.value),
                    ]);
                }
                else
                {
                    WriteObject(new PmUserPreference
                    {
                        Path = drive.NameColonSeparator,
                        PathUserName = pathUserName,
                        Key = s.key,
                        Value = s.value,
                    });
                }
            }
        }

        if (writer is not null)
        {
            WriteCSVExportedMessage(this, providerCsvPath);
        }
    }
}
