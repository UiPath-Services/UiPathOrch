using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using BoolCompleter = UiPath.PowerShell.Completer.StaticTextsCompleter<UiPath.PowerShell.Positional.True_False>;
using RetentionActionCompleter = UiPath.PowerShell.Completer.StaticTextsCompleter<UiPath.PowerShell.Positional.Delete_Archive>;

using Positional = UiPath.PowerShell.Positional.Name;
using System.Diagnostics;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsData.Update, "OrchQueue", SupportsShouldProcess = true)]
    [OutputType(typeof(QueueDefinition))]
    public class UpdateQueueCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(QueueNameCompleter<Positional.Name>))]
        [SupportsWildcards]
        public string[]? Name { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string? NewName { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string? Description { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(BoolCompleter))]
        public string? AcceptAutomaticallyRetry { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(BoolCompleter))]
        public string? RetryAbandonedItems { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        public int? MaxNumberOfRetries { get; set; }

        //ProcessScheduleId": null,

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(ProcessNameCompleter<Positional.Name>))]
        [SupportsWildcards]
        public string? Release { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        public int? SlaInMinutes { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        public int? RiskSlaInMinutes { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string? SpecificDataJsonSchema { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string? OutputDataJsonSchema { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string? AnalyticsDataJsonSchema { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(RetentionActionCompleter))]
        public string? RetentionAction { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        public int? RetentionPeriod { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(BucketNameCompleter<Positional.Name>))]
        [SupportsWildcards]
        public string? RetentionBucket { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string? Tags { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [SupportsWildcards]
        public string[]? Path { get; set; }

        [Parameter]
        public SwitchParameter Recurse { get; set; }

        [Parameter]
        public uint Depth { get; set; }

        protected override void ProcessRecord()
        {
            var drivesFolders = OrchDriveInfo.EnumFolders(Path, Recurse.IsPresent, Depth);
            var wpName = Name?.Select(id => new WildcardPattern(id, WildcardOptions.IgnoreCase)).ToList();

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var (drive, folder) in drivesFolders)
            {
                IEnumerable<QueueDefinition> queues = null;
                try
                {
                    queues = drive.GetQueues(folder);
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "GetQueueError", ErrorCategory.InvalidOperation, folder));
                }
                if (queues == null) continue;

                var targetQueues = queues.SelectByWildcards(p => p?.Name, wpName);

                foreach (var queue in targetQueues)
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();

                    string target = queue.GetPSPath();
                    QueueDefinitionPosting newQueue = OrchCollectionExtensions.DeepCopyAsSubClass<QueueDefinition, QueueDefinitionPosting>(queue);

                    #region 現在の Retention を取得
                    QueueRetentionSetting retention = null;
                    try
                    {
                        if (drive.OrchAPISession.ApiVersion >= 16)
                        {
                            retention = drive.OrchAPISession.GetQueueRetention(folder.Id ?? 0, queue.Id ?? 0);
                            if (retention != null)
                            {
                                newQueue.RetentionAction = retention.Action;
                                newQueue.RetentionPeriod = retention.Period;
                                newQueue.RetentionBucketId = retention.BucketId;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(target, $"\"{target}\": Failed to get queue retention settings", ex), "GetQueueError", ErrorCategory.InvalidOperation, target));
                    }
                    #endregion

                    newQueue.AssignString(RetentionAction,   (q, v) => q.RetentionAction = v);
                    newQueue.AssignNumber(RetentionPeriod,   (q, v) => q.RetentionPeriod = v);

                    #region RetentionBucket を RetentionBucketId に変換
                    newQueue.AssignIdFromName(
                        RetentionBucket,
                        () => drive.GetBuckets(folder),
                        e => e.Name!,
                        e => e.Id!,
                        (s, v) => s.RetentionBucketId = v,
                        this, target, "RetentionBucket");
                    #endregion

                    if (newQueue.RetentionAction != "Archive")
                    {
                        newQueue.RetentionBucketId = null;
                    }

                    newQueue.AssignString(NewName,                 (q, v) => q.Name = v);
                    newQueue.AssignEmptyString(Description,        (q, v) => q.Description = v);
                    newQueue.AssignBool(AcceptAutomaticallyRetry,  (q, v) => q.AcceptAutomaticallyRetry = v);
                    newQueue.AssignBool(RetryAbandonedItems,       (q, v) => q.RetryAbandonedItems = v);
                    newQueue.AssignNumber(MaxNumberOfRetries,      (q, v) => q.MaxNumberOfRetries = v);
                    //newQueue.assig  ProcessScheduleId": null,
                    newQueue.AssignString(SpecificDataJsonSchema,  (q, v) => q.SpecificDataJsonSchema = v);
                    newQueue.AssignString(OutputDataJsonSchema,    (q, v) => q.OutputDataJsonSchema = v);
                    newQueue.AssignString(AnalyticsDataJsonSchema, (q, v) => q.AnalyticsDataJsonSchema = v);
                    newQueue.AssignNumber(SlaInMinutes,            (q, v) => q.SlaInMinutes = v);
                    newQueue.AssignNumber(RiskSlaInMinutes,        (q, v) => q.RiskSlaInMinutes= v);

                    #region Release を ReleaseId に変換する
                    newQueue.AssignIdFromName(
                        Release,
                        () => drive.GetReleases(folder),
                        e => e.Name!,
                        e => e.Id!,
                        (s, v) => s.ReleaseId = v,
                        this, target, "Release");
                    #endregion

                    newQueue.AssignString(Tags,                    (r, v) => r.Tags = v.DeserializeTags());

                    if (ShouldProcess(target, "Update Queue"))
                    {
                        try
                        {
                            drive.OrchAPISession.EditQueue(folder.Id!.Value, newQueue);
                            drive._dicQueueDefinitions = null;
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(target, ex), "UpdateQueueError", ErrorCategory.InvalidOperation, folder));
                        }
                    }
                }
            }
        }
    }
}
