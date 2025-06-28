using System.Management.Automation;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchQueue")]
[OutputType(typeof(QueueDefinition))]
public class GetQueueCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(QueueNameCompleter<TPositional>))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    [Parameter]
    public string? ExportCsv { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(EncodingCompleter))]
    [EncodingArgumentTransformation]
    public Encoding? CsvEncoding { get; set; }

    private static readonly string DefaultCsvName = "ExportedQueues.csv";
    private static readonly string[] CsvHeaders = [
        "Path",
        "Name",
        "Description",
        "AcceptAutomaticallyRetry",
        "RetryAbandonedItems",
        "MaxNumberOfRetries",
        "EnforceUniqueReference",
        "Encrypted",
        "Release",
        "SlaInMinutes",
        "RiskSlaInMinutes",
        "SpecificDataJsonSchema",
        "OutputDataJsonSchema",
        "AnalyticsDataJsonSchema",
        "RetentionAction",
        "RetentionPeriod",
        "RetentionBucket",
        "StaleRetentionAction",
        "StaleRetentionPeriod",
        "StaleRetentionBucket",
        "Tags"
    ];

    // TODO: detailedQueue をキャッシュすべきだ。
    private void WriteCsvContent(StreamWriter writer, IEnumerable<Entities.QueueDefinition> output)
    {
        // 各キューに対してデータ行を書き込む
        foreach (var q in output)
        {
            var (drive, folder) = SessionState.EnumFolders(q.Path).First();

            QueueDefinition detailedQueue = null;
            try
            {
                detailedQueue = drive.OrchAPISession.GetQueue(folder.Id ?? 0, q.Id ?? 0);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(q.GetPSPath(), "Failed to get queue info", ex), "GetQueueError", ErrorCategory.InvalidOperation, q));
                continue;
            }
            detailedQueue ??= q;

            string release = null;
            if (q.ReleaseId is not null)
            {
                try
                {
                    var releases = drive.GetReleases(folder);
                    release = releases.FirstOrDefault(r => r.Id == q.ReleaseId)?.Name;
                }
                catch (Exception ex)
                {
                    WriteWarning($"{q.GetPSPath()}: Failed to retrieve Release: {ex.Message}");
                }
            }

            if (detailedQueue.RetentionBucketId is not null &&
                detailedQueue.RetentionBucketId != 0 &&
                string.IsNullOrEmpty(detailedQueue.RetentionBucketName))
            {
                try
                {
                    var buckets = drive.Buckets.Get(folder);
                    var bucket = buckets.FirstOrDefault(b => b.Id == detailedQueue.RetentionBucketId);
                    detailedQueue.RetentionBucketName = bucket?.Name;
                }
                catch (Exception ex)
                {
                    WriteWarning($"{q.GetPSPath()}: Failed to retrieve RetentionBucket: {ex.Message}");
                }
            }

            string[] line = [
                EscapeCsvValue(q.Path, true),
                EscapeCsvValue(detailedQueue.Name, true),
                EscapeCsvValue(detailedQueue.Description),
                EscapeCsvValue(detailedQueue.AcceptAutomaticallyRetry),
                EscapeCsvValue(detailedQueue.RetryAbandonedItems),
                EscapeCsvValue(detailedQueue.MaxNumberOfRetries),
                EscapeCsvValue(detailedQueue.EnforceUniqueReference),
                EscapeCsvValue(detailedQueue.Encrypted),
                EscapeCsvValue(release),
                EscapeCsvValue(detailedQueue.SlaInMinutes),
                EscapeCsvValue(detailedQueue.RiskSlaInMinutes),
                EscapeCsvValue(detailedQueue.SpecificDataJsonSchema),
                EscapeCsvValue(detailedQueue.OutputDataJsonSchema),
                EscapeCsvValue(detailedQueue.AnalyticsDataJsonSchema),
                EscapeCsvValue(detailedQueue.RetentionAction),
                EscapeCsvValue(detailedQueue.RetentionPeriod),
                EscapeCsvValue(detailedQueue.RetentionBucketName),
                EscapeCsvValue(detailedQueue.StaleRetentionAction),
                EscapeCsvValue(detailedQueue.StaleRetentionPeriod),
                EscapeCsvValue(detailedQueue.StaleRetentionBucketName),
                EscapeCsvValue(detailedQueue.Tags)
            ];
            writer.WriteCsvLine(line);
        }
    }

    internal void Output(StreamWriter? writer, IEnumerable<QueueDefinition> queues)
    {
        if (writer is not null)
        {
            WriteCsvContent(writer, queues);
        }
        else
        {
            WriteObject(queues, true);
        }
    }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();

        var (physicalCsvPath, providerCsvPath) = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
        using var writer = WriteCsvHeader(physicalCsvPath, CsvEncoding, CsvHeaders);

        using var results = OrchThreadPool.RunForEach(drivesFolders,
            df => df.folder.GetPSPath(),
            df => df.folder,
            df => df.drive.Queues.Get(df.folder));

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var queues = result.GetResult(cancelHandler.Token);
                if (queues is null) continue;

                Output(writer, queues
                    .FilterByWildcards(q => q?.Name, wpName)
                    .OrderBy(q => q.Name));
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetQueueError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }

        WriteCSVExportedMessage(this, providerCsvPath);
    }
}
