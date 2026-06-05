using System.Management.Automation;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchWebhook")]
[OutputType(typeof(Webhook))]
public class GetWebhookCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(WebhookNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

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

    private static readonly string DefaultCsvName = "ExportedWebhooks.csv";

    // Webhook columns match New-OrchWebhook / Update-OrchWebhook
    // parameter names exactly so the CSV round-trips into either cmdlet.
    // Secret is included because it's a New-/Update- parameter — the
    // round-trip contract is "every write-cmdlet param has a column",
    // and partial coverage breaks bulk re-import. Users who don't want
    // Secret in plaintext should clear the column before sharing the
    // file or restrict file permissions.
    private static readonly string[] CsvHeaders = [
        "Path",
        "Name",
        "Url",
        "Description",
        "Enabled",
        "AllowInsecureSsl",
        "SubscribeToAllEvents",
        "Events",
        "Secret",
    ];

    private void WriteCsvContent(StreamWriter writer, OrchDriveInfo drive, IEnumerable<Webhook> webhooks)
    {
        foreach (var w in webhooks)
        {
            // Events round-trips as a ';'-joined EventType list; New-/Update-
            // OrchWebhook -Events splits it back into one pattern per value.
            string events = string.Join(";",
                (w.Events ?? System.Array.Empty<WebhookEvent>())
                    .Select(e => e.EventType)
                    .Where(t => !string.IsNullOrEmpty(t)));

            string[] line = [
                EscapeCsvValue(drive.NameColonSeparator, true),
                EscapeCsvValue(w.Name, true),
                EscapeCsvValue(w.Url),
                EscapeCsvValue(w.Description),
                EscapeCsvValue(w.Enabled),
                EscapeCsvValue(w.AllowInsecureSsl),
                EscapeCsvValue(w.SubscribeToAllEvents),
                EscapeCsvValue(events),
                EscapeCsvValue(w.Secret),
            ];
            writer.WriteCsvLine(line);
        }
    }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumOrchDrives(EffectivePath(Path, LiteralPath));
        var wpName = Name?.Select(name => new WildcardPattern(PathTools.UnescapePSText(name), WildcardOptions.IgnoreCase)).ToList();

        var (physicalCsvPath, providerCsvPath) = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
        using var writer = WriteCsvHeader(physicalCsvPath, CsvEncoding, CsvHeaders);

        using var results = OrchThreadPool.RunForEach(drives,
            drive => drive.NameColonSeparator,
            drive => drive,
            drive => drive.Webhooks.Get());

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var webhooks = result.GetResult(cancelHandler.Token);
                if (webhooks is null) continue;

                var filtered = webhooks
                    .FilterByWildcards(c => c?.Name, wpName)
                    .OrderBy(c => c.Name);

                if (writer is not null) { WriteCsvContent(writer, result.Source, filtered); }
                else { WriteObject(filtered, true); }
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetWebhookError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }

        if (writer is not null)
        {
            WriteCSVExportedMessage(this, providerCsvPath);
        }
    }
}
