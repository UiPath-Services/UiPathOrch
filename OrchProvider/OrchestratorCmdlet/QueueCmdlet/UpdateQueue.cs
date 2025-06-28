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

    // キューの Tags 専用
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

            // パラメータで選択済みの Id は、候補から除外する
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

            var targetQueues = queues.SelectByWildcards(p => p?.Name, wpName);

            foreach (var queue in targetQueues)
            {
                cancelHandler.Token.ThrowIfCancellationRequested();

                string target = queue.GetPSPath();
                //QueueDefinition newQueue = OrchCollectionExtensions.DeepCopyAsSubClass<QueueDefinition, QueueDefinitionPosting>(queue);
                QueueDefinition newQueue = OrchCollectionExtensions.DeepCopy<QueueDefinition>(queue);

                #region 現在の Retention を取得
                QueueRetentionSetting retention = null;
                try
                {
                    // TODO: 15 で成功するか？ たぶん失敗するはず
                    // 16 で成功することは確認済み
                    if (drive.OrchAPISession.ApiVersion >= 16)
                    {
                        retention = drive.OrchAPISession.GetQueueRetention(folder.Id ?? 0, queue.Id ?? 0);
                        if (retention is not null)
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

                newQueue.AssignStringIfNotNullOrEmpty(RetentionAction, (q, v) => q.RetentionAction = v);
                newQueue.AssignNumberIfNotNullOrZero(RetentionPeriod, (q, v) => q.RetentionPeriod = v);
                newQueue.AssignStringIfNotNullOrEmpty(StaleRetentionAction, (q, v) => q.StaleRetentionAction = v);
                newQueue.AssignNumberIfNotNullOrZero(StaleRetentionPeriod, (q, v) => q.StaleRetentionPeriod = v);

                #region RetentionBucket を RetentionBucketId に変換
                newQueue.AssignIdFromName(
                    RetentionBucket,
                    () => drive.Buckets.Get(folder),
                    e => e.Name!,
                    e => e.Id!,
                    (s, v) => s.RetentionBucketId = v,
                    this, target, "RetentionBucket");

                newQueue.AssignIdFromName(
                    StaleRetentionBucket,
                    () => drive.Buckets.Get(folder),
                    e => e.Name!,
                    e => e.Id!,
                    (s, v) => s.StaleRetentionBucketId = v,
                    this, target, "StaleRetentionBucket");
                #endregion

                if (drive.OrchAPISession.ApiVersion >= 19)
                {
                    if (string.IsNullOrEmpty(newQueue.RetentionAction) || newQueue.RetentionAction == "None")
                    {
                        newQueue.RetentionAction = "Delete";
                        newQueue.RetentionPeriod ??= 30;
                    }
                }

                if (newQueue.RetentionAction != "Archive")
                {
                    newQueue.RetentionBucketId = null;
                }
                if (newQueue.StaleRetentionAction != "Archive")
                {
                    newQueue.StaleRetentionBucketId = null;
                }

                newQueue.AssignStringIfNotNullOrEmpty(NewName,                 (q, v) => q.Name = v);
                newQueue.AssignStringIfNotNull(Description,                    (q, v) => q.Description = v);
                newQueue.AssignBoolIfNotNull(AcceptAutomaticallyRetry,         (q, v) => q.AcceptAutomaticallyRetry = v);
                newQueue.AssignBoolIfNotNull(RetryAbandonedItems,              (q, v) => q.RetryAbandonedItems = v);
                newQueue.AssignNumberIfNotNullOrZero(MaxNumberOfRetries,       (q, v) => q.MaxNumberOfRetries = v);
                //newQueue.assig  ProcessScheduleId": null,
                newQueue.AssignStringIfNotNullOrEmpty(SpecificDataJsonSchema,  (q, v) => q.SpecificDataJsonSchema = v);
                newQueue.AssignStringIfNotNullOrEmpty(OutputDataJsonSchema,    (q, v) => q.OutputDataJsonSchema = v);
                newQueue.AssignStringIfNotNullOrEmpty(AnalyticsDataJsonSchema, (q, v) => q.AnalyticsDataJsonSchema = v);
                newQueue.AssignNumberIfNotNullOrZero(SlaInMinutes,             (q, v) => q.SlaInMinutes = v);
                newQueue.AssignNumberIfNotNullOrZero(RiskSlaInMinutes,         (q, v) => q.RiskSlaInMinutes= v);

                #region Release を ReleaseId に変換する
                newQueue.AssignIdFromName(
                    Release,
                    () => drive.GetReleases(folder),
                    e => e.Name!,
                    e => e.Id!,
                    (s, v) => s.ReleaseId = v,
                    this, target, "Release");
                #endregion

                newQueue.AssignTags(Tags, (r, v) => r.Tags = v);

                if (ShouldProcess(target, "Update Queue"))
                {
                    try
                    {
                        drive.OrchAPISession.EditQueue(folder.Id!.Value, newQueue);
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
