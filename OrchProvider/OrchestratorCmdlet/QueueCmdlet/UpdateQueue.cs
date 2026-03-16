using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsData.Update, "OrchQueue", SupportsShouldProcess = true)]
[OutputType(typeof(QueueDefinition))]
public class UpdateQueueCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(QueueNameCompleter<TPositional>))]
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
    [ArgumentCompleter(typeof(ProcessNameCompleter<TPositional>))]
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
    [ArgumentCompleter(typeof(StaticTextsCompleter<Delete_Archive>))]
    public string? RetentionAction { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public int? RetentionPeriod { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BucketNameCompleter<TPositional, True>))]
    [SupportsWildcards]
    public string? RetentionBucket { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<Delete_Archive>))]
    public string? StaleRetentionAction { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public int? StaleRetentionPeriod { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BucketNameCompleter<TPositional, True>))]
    [SupportsWildcards]
    public string? StaleRetentionBucket { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TagsCompleter))]
    public string[]? Tags { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    // Dedicated to queue Tags
    private class TagsCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

            // Exclude Names already selected by the parameter from the candidates
            var wpName = CreateWPListFromOtherParameters(commandAst, "Name", TPositional.Parameters);

            var results = ParallelResults3.GroupBy(drivesFolders, df => df.drive.Queues.Get(df.folder));

            foreach (var result in results)
            {
                foreach (var release in result
                    .FilterByWildcards(p => p?.Name, wpName)
                    .OrderBy(p => p.Name))
                {
                    if (release?.Tags is null) continue;

                    var values = release.Tags.ConvertToString();
                    if (string.IsNullOrEmpty(values)) continue;

                    string tiphelp = TipHelp(release);
                    yield return new CompletionResult(PathTools.EscapePSText(values), values, CompletionResultType.Text, tiphelp);
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            IEnumerable<QueueDefinition> queues = null;
            try
            {
                queues = drive.Queues.Get(folder);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "GetQueueError", ErrorCategory.InvalidOperation, folder));
            }
            if (queues is null) continue;

            var targetQueues = queues.SelectByWildcards(p => p?.Name, wpName).OrderBy(q => q.Name);

            foreach (var queue in targetQueues)
            {
                cancelHandler.Token.ThrowIfCancellationRequested();

                string target = queue.GetPSPath();
                QueueDefinition newQueue = OrchCollectionExtensions.DeepCopy<QueueDefinition>(queue);

                bool queueDirty = false;
                queueDirty |= newQueue.AssignStringIfNotNull(NewName,                      queue, q => q.Name,                    (q, v) => q.Name = v);
                queueDirty |= newQueue.AssignStringIfNotNull(Description,                  queue, q => q.Description,             (q, v) => q.Description = v);
                queueDirty |= newQueue.AssignBoolIfNotNull(AcceptAutomaticallyRetry,        queue, q => q.AcceptAutomaticallyRetry, (q, v) => q.AcceptAutomaticallyRetry = v);
                queueDirty |= newQueue.AssignBoolIfNotNull(RetryAbandonedItems,             queue, q => q.RetryAbandonedItems,      (q, v) => q.RetryAbandonedItems = v);
                queueDirty |= newQueue.AssignNumberIfNotNullOrZero(MaxNumberOfRetries,      queue, q => q.MaxNumberOfRetries,       (q, v) => q.MaxNumberOfRetries = v);
                queueDirty |= newQueue.AssignStringIfNotNull(SpecificDataJsonSchema,        queue, q => q.SpecificDataJsonSchema,   (q, v) => q.SpecificDataJsonSchema = v);
                queueDirty |= newQueue.AssignStringIfNotNull(OutputDataJsonSchema,          queue, q => q.OutputDataJsonSchema,     (q, v) => q.OutputDataJsonSchema = v);
                queueDirty |= newQueue.AssignStringIfNotNull(AnalyticsDataJsonSchema,       queue, q => q.AnalyticsDataJsonSchema,  (q, v) => q.AnalyticsDataJsonSchema = v);
                queueDirty |= newQueue.AssignNumberIfNotNullOrZero(SlaInMinutes,            queue, q => q.SlaInMinutes,             (q, v) => q.SlaInMinutes = v);
                queueDirty |= newQueue.AssignNumberIfNotNullOrZero(RiskSlaInMinutes,        queue, q => q.RiskSlaInMinutes,         (q, v) => q.RiskSlaInMinutes = v);

                #region Convert Release to ReleaseId
                newQueue.AssignIdFromName(
                    Release,
                    () => drive.GetReleases(folder),
                    e => e.Name!,
                    e => e.Id!,
                    (s, v) => { if (queue.ReleaseId != v) { s.ReleaseId = v; queueDirty = true; } },
                    this, target, "Release");
                #endregion

                var effectiveTags = Tags?.Where(t => !string.IsNullOrEmpty(t)).ToArray();
                if (effectiveTags is not null && effectiveTags.Length != 0)
                {
                    newQueue.AssignTags(effectiveTags, (r, v) => r.Tags = v);
                    queueDirty = true;
                }

                #region Retention (uses separate PutQueueRetention API)
                QueueRetentionSetting? retentionUpdate = null;
                {
                    var ret = new QueueRetentionSetting { QueueDefinitionId = queue.Id!.Value };
                    bool retDirty = false;
                    if (RetentionAction is not null && RetentionAction != (queue.RetentionAction ?? "")) { ret.Action = RetentionAction; retDirty = true; }
                    if (RetentionPeriod is not null && RetentionPeriod != 0 && RetentionPeriod != queue.RetentionPeriod) { ret.Period = RetentionPeriod; retDirty = true; }
                    ret.AssignIdFromName(
                        RetentionBucket,
                        () => drive.Buckets.Get(folder),
                        e => e.Name!,
                        e => e.Id!,
                        (s, v) => { if (queue.RetentionBucketId != v) { s.BucketId = v; retDirty = true; } },
                        this, target, "RetentionBucket");
                    if (retDirty)
                    {
                        ret.Action ??= queue.RetentionAction ?? "Delete";
                        ret.Period ??= queue.RetentionPeriod ?? 30;
                        ret.BucketId ??= queue.RetentionBucketId;
                        retentionUpdate = ret;
                    }
                }

                QueueRetentionSetting? staleRetentionUpdate = null;
                {
                    var ret = new QueueRetentionSetting { QueueDefinitionId = queue.Id!.Value, Type = "Stale" };
                    bool retDirty = false;
                    if (StaleRetentionAction is not null && StaleRetentionAction != (queue.StaleRetentionAction ?? "")) { ret.Action = StaleRetentionAction; retDirty = true; }
                    if (StaleRetentionPeriod is not null && StaleRetentionPeriod != 0 && StaleRetentionPeriod != queue.StaleRetentionPeriod) { ret.Period = StaleRetentionPeriod; retDirty = true; }
                    ret.AssignIdFromName(
                        StaleRetentionBucket,
                        () => drive.Buckets.Get(folder),
                        e => e.Name!,
                        e => e.Id!,
                        (s, v) => { if (queue.StaleRetentionBucketId != v) { s.BucketId = v; retDirty = true; } },
                        this, target, "StaleRetentionBucket");
                    if (retDirty)
                    {
                        ret.Action ??= queue.StaleRetentionAction ?? "Delete";
                        ret.Period ??= queue.StaleRetentionPeriod ?? 30;
                        ret.BucketId ??= queue.StaleRetentionBucketId;
                        staleRetentionUpdate = ret;
                    }
                }
                #endregion

                // Strip retention fields from queue PUT payload (managed via separate API)
                newQueue.RetentionAction = null;
                newQueue.RetentionPeriod = null;
                newQueue.RetentionBucketId = null;
                newQueue.RetentionBucketName = null;
                newQueue.StaleRetentionAction = null;
                newQueue.StaleRetentionPeriod = null;
                newQueue.StaleRetentionBucketId = null;
                newQueue.StaleRetentionBucketName = null;

                if (!queueDirty && retentionUpdate is null && staleRetentionUpdate is null)
                {
                    continue;
                }

                if (ShouldProcess(target, "Update Queue"))
                {
                    try
                    {
                        if (queueDirty)
                        {
                            drive.OrchAPISession.PutQueueDefinition(folder.Id!.Value, newQueue);
                        }

                        if (retentionUpdate is not null)
                        {
                            drive.OrchAPISession.PutQueueRetention(folder.Id!.Value, queue.Id!.Value, retentionUpdate);
                        }
                        if (staleRetentionUpdate is not null)
                        {
                            drive.OrchAPISession.PutQueueRetention(folder.Id!.Value, queue.Id!.Value, staleRetentionUpdate);
                        }

                        drive.Queues.ClearCache(folder);
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
