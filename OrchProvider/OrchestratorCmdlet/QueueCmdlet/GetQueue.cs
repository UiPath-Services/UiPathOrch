using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text;
using System.Text.Json;
using UiPath.OrchAPI;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "OrchQueue")]
    [OutputType(typeof(QueueDefinition))]
    public class GetQueueCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(QueueNameCompleter<Positional.Name>))]
        [SupportsWildcards]
        public string[]? Name { get; set; }

        [Parameter]
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
            "Tags"
        ];

        private void WriteCsvContent(StreamWriter writer, IEnumerable<Entities.QueueDefinition> output)
        {
            // 各キューに対してデータ行を書き込む
            foreach (var q in output)
            {
                var (drive, folder) = OrchDriveInfo.EnumFolders(q.Path).First();

                string release = null;
                if (q.ReleaseId != null)
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

                QueueRetentionSetting retention = null;
                if (drive.OrchAPISession.ApiVersion >= 16)
                {
                    try
                    {
                        retention = drive.OrchAPISession.GetQueueRetention(folder.Id ?? 0, q.Id ?? 0);
                    }
                    catch (Exception ex)
                    {
                        WriteWarning($"{q.GetPSPath()}: Failed to retrieve QueueRetention: {ex.Message}");
                    }
                }

                string retentionBucket = null;
                if (retention?.BucketId != null)
                {
                    try
                    {
                        var buckets = drive.GetBuckets(folder);
                        var bucket = buckets.FirstOrDefault(b => b.Id == retention.BucketId);
                        retentionBucket = bucket?.Name;
                    }
                    catch (Exception ex)
                    {
                        WriteWarning($"{q.GetPSPath()}: Failed to retrieve RetentionBucket: {ex.Message}");
                    }
                }

                string[] line = [
                    EscapeCsvValue(q.Path, true),
                    EscapeCsvValue(q.Name, true),
                    EscapeCsvValue(q.Description),
                    EscapeCsvValue(q.AcceptAutomaticallyRetry),
                    EscapeCsvValue(q.RetryAbandonedItems),
                    EscapeCsvValue(q.MaxNumberOfRetries),
                    EscapeCsvValue(q.EnforceUniqueReference),
                    EscapeCsvValue(q.Encrypted),
                    EscapeCsvValue(release),
                    EscapeCsvValue(q.SlaInMinutes),
                    EscapeCsvValue(q.RiskSlaInMinutes),
                    EscapeCsvValue(q.SpecificDataJsonSchema),
                    EscapeCsvValue(q.OutputDataJsonSchema),
                    EscapeCsvValue(q.AnalyticsDataJsonSchema),
                    EscapeCsvValue(retention?.Action),
                    EscapeCsvValue(retention?.Period),
                    EscapeCsvValue(retentionBucket),
                    EscapeCsvValue(q.Tags)
                ];
                WriteCsvLine(writer, line);
            }
        }

        internal void Output(StreamWriter? writer, IEnumerable<QueueDefinition> queues)
        {
            if (writer != null)
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
            var drivesFolders = OrchDriveInfo.EnumFolders(Path, Recurse.IsPresent, Depth);
            var wpName = Name.ConvertToWildcardPatternList();

            ExportCsv = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
            using var writer = WriteCsvHeader(ExportCsv, CsvEncoding, CsvHeaders);

            using var results = OrchThreadPool.RunForEach(drivesFolders,
                df => df.folder.GetPSPath(),
                df => df.folder,
                df => df.drive.GetQueues(df.folder));

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var result in results)
            {
                try
                {
                    var queues = result.GetResult(cancelHandler.Token);
                    if (queues == null) continue;

                    Output(writer, queues
                        .FilterByWildcards(q => q?.Name, wpName)
                        .OrderBy(q => q.Name));
                }
                catch (OrchException ex)
                {
                    WriteError(new ErrorRecord(ex, "GetQueueError", ErrorCategory.InvalidOperation, ex.Target));
                }
            }

            WriteCSVExportedMessage(this, ExportCsv);
        }
    }
}
