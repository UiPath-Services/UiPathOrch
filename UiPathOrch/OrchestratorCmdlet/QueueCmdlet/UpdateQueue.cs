using System.Collections;
using UiPath.PowerShell.Positional;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsData.Update, "OrchQueue", SupportsShouldProcess = true)]
[OutputType(typeof(QueueDefinition))]
public class UpdateQueueCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(QueueNameCompleter))]
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
    [ArgumentCompleter(typeof(ProcessNameCompleter))]
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
    [ArgumentCompleter(typeof(BucketNameCompleter<True>))]
    [SupportsWildcards]
    public string? RetentionBucket { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<Delete_Archive>))]
    public string? StaleRetentionAction { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public int? StaleRetentionPeriod { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BucketNameCompleter<True>))]
    [SupportsWildcards]
    public string? StaleRetentionBucket { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TagsCompleter))]
    [TagArgumentTransformation]
    public string[]? Tags { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    // Dedicated to queue Tags
    private class TagsCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

            // Exclude Names already selected by the parameter from the candidates
            var wpName = GetFakeBoundParameters(fakeBoundParameters, "Name").ConvertToWildcardPatternList();

            var results = ParallelResults.GroupBy(drivesFolders, df => df.drive.Queues.Get(df.folder));

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
        var drivesFolders = SessionState.EnumFolders(EffectivePath(Path, LiteralPath), Recurse.IsPresent, Depth);
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

            foreach (var queue in targetQueues
                .WithProgressBar(this, $"Updating queues in {folder.GetPSPath()}", q => q.Name)
                .WithCancellation(cancelHandler.Token))
            {
                string target = queue.GetPSPath();
                QueueDefinition newQueue = OrchCollectionExtensions.DeepCopy<QueueDefinition>(queue);

                // Resolve Release name -> id up front. This is the only API round-trip in the main
                // payload, and it also emits the not-found / multiple-match errors. A throwaway holder
                // captures the resolved id (and whether it resolved) without mutating the payload, so
                // the pure, API-free ComputeQueueUpdate core can diff it. AssignIdFromName's error
                // emission is unchanged (it depends on the name, not the target).
                #region Convert Release to ReleaseId
                long? resolvedReleaseId = null;
                bool releaseResolved = false;
                new QueueDefinition().AssignIdFromName(
                    Release,
                    () => drive.Releases.Get(folder),
                    e => e.Name!,
                    e => e.Id!,
                    (s, v) => { resolvedReleaseId = v; releaseResolved = true; },
                    this, target, "Release");
                #endregion

                bool queueDirty = ComputeQueueUpdate(newQueue, queue, new QueueUpdateInputs
                {
                    NewName = NewName,
                    Description = Description,
                    AcceptAutomaticallyRetry = AcceptAutomaticallyRetry,
                    RetryAbandonedItems = RetryAbandonedItems,
                    MaxNumberOfRetries = MaxNumberOfRetries,
                    SpecificDataJsonSchema = SpecificDataJsonSchema,
                    OutputDataJsonSchema = OutputDataJsonSchema,
                    AnalyticsDataJsonSchema = AnalyticsDataJsonSchema,
                    SlaInMinutes = SlaInMinutes,
                    RiskSlaInMinutes = RiskSlaInMinutes,
                    Tags = Tags,
                    ReleaseSpecified = !string.IsNullOrEmpty(Release),
                    ReleaseResolved = releaseResolved,
                    ResolvedReleaseId = resolvedReleaseId,
                });

                #region Retention (uses separate PutQueueRetention API)
                // Queue list API does not return retention fields, so fetch current values
                // from the dedicated QueueRetention API when any retention parameter is specified.
                bool hasRetentionParam = RetentionAction is not null || (RetentionPeriod is not null && RetentionPeriod != 0) || RetentionBucket is not null;
                bool hasStaleRetentionParam = StaleRetentionAction is not null || (StaleRetentionPeriod is not null && StaleRetentionPeriod != 0) || StaleRetentionBucket is not null;

                QueueRetentionSetting? currentRetention = null;
                if (hasRetentionParam || hasStaleRetentionParam)
                {
                    try
                    {
                        currentRetention = drive.OrchAPISession.GetQueueRetention(folder.Id ?? 0, queue.Id ?? 0);
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(target, "Failed to get queue retention settings.", ex), "GetQueueRetentionError", ErrorCategory.InvalidOperation, target));
                    }
                }

                // The bucket name -> id resolution stays here (needs the folder's bucket list); the
                // write/no-write decision + PUT fill-defaults are the pure, unit-tested
                // OrchStringExtensions.ComputeRetentionUpdate (shared with Update-OrchProcess).
                QueueRetentionSetting? retentionUpdate = null;
                if (hasRetentionParam && currentRetention is not null)
                {
                    long? resolvedBucketId = null;
                    _ = new QueueRetentionSetting().AssignIdFromName(
                        RetentionBucket, () => drive.Buckets.Get(folder), e => e.Name!, e => e.Id!,
                        (_, v) => resolvedBucketId = v, this, target, "RetentionBucket");

                    if (OrchStringExtensions.ComputeRetentionUpdate(
                            new OrchStringExtensions.RetentionUpdateInput
                            {
                                Action = RetentionAction,
                                Period = RetentionPeriod,
                                BucketCleared = RetentionBucket == "",
                                ResolvedBucketId = resolvedBucketId,
                            },
                            currentRetention.Action, currentRetention.Period, currentRetention.BucketId,
                            out string action, out int period, out long? bucketId))
                    {
                        retentionUpdate = new QueueRetentionSetting { QueueDefinitionId = queue.Id!.Value, Action = action, Period = period, BucketId = bucketId };
                    }
                }

                QueueRetentionSetting? staleRetentionUpdate = null;
                if (hasStaleRetentionParam)
                {
                    QueueRetentionSetting? currentStale = null;
                    try
                    {
                        currentStale = drive.OrchAPISession.GetQueueRetention(folder.Id ?? 0, queue.Id ?? 0, "Stale");
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(target, "Failed to get stale queue retention settings.", ex), "GetQueueRetentionError", ErrorCategory.InvalidOperation, target));
                    }

                    if (currentStale is not null)
                    {
                        long? resolvedBucketId = null;
                        _ = new QueueRetentionSetting().AssignIdFromName(
                            StaleRetentionBucket, () => drive.Buckets.Get(folder), e => e.Name!, e => e.Id!,
                            (_, v) => resolvedBucketId = v, this, target, "StaleRetentionBucket");

                        if (OrchStringExtensions.ComputeRetentionUpdate(
                                new OrchStringExtensions.RetentionUpdateInput
                                {
                                    Action = StaleRetentionAction,
                                    Period = StaleRetentionPeriod,
                                    BucketCleared = StaleRetentionBucket == "",
                                    ResolvedBucketId = resolvedBucketId,
                                },
                                currentStale.Action, currentStale.Period, currentStale.BucketId,
                                out string action, out int period, out long? bucketId))
                        {
                            staleRetentionUpdate = new QueueRetentionSetting { QueueDefinitionId = queue.Id!.Value, Type = "Stale", Action = action, Period = period, BucketId = bucketId };
                        }
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

    /// <summary>
    /// Pure inputs for <see cref="ComputeQueueUpdate"/>. The Release name -> id resolution (an API
    /// round-trip) is done by the cmdlet first and passed in here, so change detection for the main
    /// queue PATCH is fully testable without a live Orchestrator.
    /// </summary>
    internal sealed class QueueUpdateInputs
    {
        public string? NewName { get; init; }
        public string? Description { get; init; }
        public string? AcceptAutomaticallyRetry { get; init; }
        public string? RetryAbandonedItems { get; init; }
        public int? MaxNumberOfRetries { get; init; }
        public string? SpecificDataJsonSchema { get; init; }
        public string? OutputDataJsonSchema { get; init; }
        public string? AnalyticsDataJsonSchema { get; init; }
        public int? SlaInMinutes { get; init; }
        public int? RiskSlaInMinutes { get; init; }
        public string[]? Tags { get; init; }
        /// <summary>True when -Release was supplied (even if it did not resolve).</summary>
        public bool ReleaseSpecified { get; init; }
        /// <summary>True when the Release name resolved (exactly one match, or an explicit empty clear).</summary>
        public bool ReleaseResolved { get; init; }
        /// <summary>The resolved release id to diff against; only consulted when <see cref="ReleaseResolved"/> is true.</summary>
        public long? ResolvedReleaseId { get; init; }
    }

    /// <summary>
    /// Applies the requested changes onto <paramref name="payload"/> (a deep copy of
    /// <paramref name="source"/>) for the main queue PUT and returns whether anything actually
    /// changed, so the caller can skip the PutQueueDefinition when the request is a no-op. Retention
    /// is managed by a separate API and handled by the caller. No API access — unit-testable.
    /// </summary>
    internal static bool ComputeQueueUpdate(QueueDefinition payload, QueueDefinition source, QueueUpdateInputs input)
    {
        bool dirty = false;
        dirty |= payload.AssignStringIfNotNull(input.NewName, source, q => q.Name, (q, v) => q.Name = v);
        dirty |= payload.AssignStringIfNotNull(input.Description, source, q => q.Description, (q, v) => q.Description = v);
        dirty |= payload.AssignBoolIfNotNull(input.AcceptAutomaticallyRetry, source, q => q.AcceptAutomaticallyRetry, (q, v) => q.AcceptAutomaticallyRetry = v);
        dirty |= payload.AssignBoolIfNotNull(input.RetryAbandonedItems, source, q => q.RetryAbandonedItems, (q, v) => q.RetryAbandonedItems = v);
        dirty |= payload.AssignNumberIfNotNullOrZero(input.MaxNumberOfRetries, source, q => q.MaxNumberOfRetries, (q, v) => q.MaxNumberOfRetries = v);
        dirty |= payload.AssignStringIfNotNull(input.SpecificDataJsonSchema, source, q => q.SpecificDataJsonSchema, (q, v) => q.SpecificDataJsonSchema = v);
        dirty |= payload.AssignStringIfNotNull(input.OutputDataJsonSchema, source, q => q.OutputDataJsonSchema, (q, v) => q.OutputDataJsonSchema = v);
        dirty |= payload.AssignStringIfNotNull(input.AnalyticsDataJsonSchema, source, q => q.AnalyticsDataJsonSchema, (q, v) => q.AnalyticsDataJsonSchema = v);
        dirty |= payload.AssignNumberIfNotNullOrZero(input.SlaInMinutes, source, q => q.SlaInMinutes, (q, v) => q.SlaInMinutes = v);
        dirty |= payload.AssignNumberIfNotNullOrZero(input.RiskSlaInMinutes, source, q => q.RiskSlaInMinutes, (q, v) => q.RiskSlaInMinutes = v);

        // Release: only write when the resolved id differs from the current one.
        if (input.ReleaseResolved && source.ReleaseId != input.ResolvedReleaseId)
        {
            payload.ReleaseId = input.ResolvedReleaseId;
            dirty = true;
        }

        var effectiveTags = input.Tags?.Where(t => !string.IsNullOrEmpty(t)).ToArray();
        if (effectiveTags is not null && effectiveTags.Length != 0)
        {
            // Only write when the tag set actually differs from the current one.
            dirty |= payload.AssignTags(effectiveTags, source, q => q.Tags, (q, v) => q.Tags = v);
        }

        return dirty;
    }
}
