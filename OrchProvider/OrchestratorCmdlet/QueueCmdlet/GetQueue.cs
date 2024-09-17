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

        private static void WriteCsvContent(StreamWriter writer, IEnumerable<Entities.QueueDefinition> output)
        {
            // 各キューに対してデータ行を書き込む
            foreach (var q in output)
            {
                var (drive, folder) = OrchDriveInfo.EnumFolders(q.Path).First();

                var line = new StringBuilder();

                line.Append($"{EscapeCsvValue(q.Path, true)},");
                line.Append($"{EscapeCsvValue(q.Name, true)},");
                line.Append($"{EscapeCsvValue(q.Description)},");
                line.Append($"{q.AcceptAutomaticallyRetry},");
                line.Append($"{q.RetryAbandonedItems},");
                line.Append($"{q.MaxNumberOfRetries},");
                line.Append($"{q.EnforceUniqueReference},");
                line.Append($"{q.Encrypted},");

                if (q.ReleaseId != null)
                {
                    var releases = drive.GetReleases(folder);
                    var release = releases.FirstOrDefault(r => r.Id == q.ReleaseId);
                    line.Append($"{release?.Name},");
                }
                else
                {
                    line.Append($",");
                }

                line.Append($"{q.SlaInMinutes},");
                line.Append($"{q.RiskSlaInMinutes},");
                line.Append($"{EscapeCsvValue(q.SpecificDataJsonSchema)},");
                line.Append($"{EscapeCsvValue(q.OutputDataJsonSchema)},");
                line.Append($"{EscapeCsvValue(q.AnalyticsDataJsonSchema)},");

                QueueRetentionSetting retention = null;
                try
                {
                    if (drive.OrchAPISession.ApiVersion >= 16)
                    {
                        retention = drive.OrchAPISession.GetQueueRetention(folder.Id ?? 0, q.Id ?? 0);
                    }
                }
                catch { }

                line.Append($"{EscapeCsvValue(retention?.Action)},");
                line.Append($"{retention?.Period},");

                string retentionBucket = null;
                if (retention?.BucketId != null)
                {
                    var buckets = drive.GetBuckets(folder);
                    var bucket = buckets.FirstOrDefault(b => b.Id == retention.BucketId);
                    retentionBucket = bucket?.Name;
                }
                line.Append($"{EscapeCsvValue(retentionBucket)},");

                line.Append($"{EscapeCsvValue(JsonSerializer.Serialize(q?.Tags, OrchAPISession.jsoWhenWritingNull), false)}");

                writer.WriteLine(line.ToString());
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
