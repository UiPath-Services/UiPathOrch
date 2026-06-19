using System.Collections;
using UiPath.PowerShell.Positional;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.New, "OrchQueue", SupportsShouldProcess = true)]
[OutputType(typeof(QueueDefinition))]
public class NewQueueCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(NewQueueNameCompleter))]
    public string[]? Name { get; set; }

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

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BoolCompleter))]
    public string? EnforceUniqueReference { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BoolCompleter))]
    public string? Encrypted { get; set; }

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
    [ArgumentCompleter(typeof(StaticTextsCompleter<Item180>))]
    public int? StaleRetentionPeriod { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BucketNameCompleter<True>))]
    [SupportsWildcards]
    public string? StaleRetentionBucket { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [TagArgumentTransformation]
    public string[]? Tags { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    //[Parameter]
    //public SwitchParameter Recurse { get; set; }

    //[Parameter]
    //public uint Depth { get; set; }

    private class NewQueueNameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);
            var results = ParallelResults.GroupBy(drivesFolders, df => df.drive.Queues.Get(df.folder));

            // Exclude Names already selected by the parameter from the candidates
            var names = GetSelfExclusionValues(commandAst, parameterName, wordToComplete);

            var entities = results.SelectMany(e => e);
            yield return new CompletionResult(GenerateNewEntityName("NewQueue", names, entities, e => e.Name!));
        }
    }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(EffectivePath(Path, LiteralPath));

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            foreach (var name in Name!.WithCancellation(cancelHandler.Token))
            {
                string target = System.IO.Path.Combine(folder.GetPSPath(), name);

                QueueDefinition newQueue = new()
                {
                    Name = WildcardPattern.Unescape(name),
                };

                newQueue.AssignStringIfNotNull(Description, (q, v) => q.Description = v);
                newQueue.AssignBoolIfNotNull(AcceptAutomaticallyRetry, (q, v) => q.AcceptAutomaticallyRetry = v);
                newQueue.AssignBoolIfNotNull(RetryAbandonedItems, (q, v) => q.RetryAbandonedItems = v);
                newQueue.AssignNumberIfNotNullOrZero(MaxNumberOfRetries, (q, v) => q.MaxNumberOfRetries = v);
                newQueue.AssignBoolIfNotNull(EnforceUniqueReference, (q, v) => q.EnforceUniqueReference = v);
                newQueue.AssignBoolIfNotNull(Encrypted, (q, v) => q.Encrypted = v);
                //newQueue.assig  ProcessScheduleId": null,
                newQueue.AssignStringIfNotNullOrEmpty(SpecificDataJsonSchema, (q, v) => q.SpecificDataJsonSchema = v);
                newQueue.AssignStringIfNotNullOrEmpty(OutputDataJsonSchema, (q, v) => q.OutputDataJsonSchema = v);
                newQueue.AssignStringIfNotNullOrEmpty(AnalyticsDataJsonSchema, (q, v) => q.AnalyticsDataJsonSchema = v);

                #region Convert Release to ReleaseId
                newQueue.AssignIdFromName(
                    Release,
                    () => drive.Releases.Get(folder),
                    e => e.Name!,
                    e => e.Id!,
                    (s, v) => s.ReleaseId = v,
                    this, target, "Release");
                #endregion

                newQueue.AssignNumberIfNotNullOrZero(SlaInMinutes, (q, v) => q.SlaInMinutes = v);
                newQueue.AssignNumberIfNotNullOrZero(RiskSlaInMinutes, (q, v) => q.RiskSlaInMinutes = v);
                newQueue.AssignTags(Tags, (q, v) => q.Tags = v);

                newQueue.AssignStringIfNotNullOrEmpty(RetentionAction, (q, v) => q.RetentionAction = v);
                newQueue.AssignNumberIfNotNullOrZero(RetentionPeriod, (q, v) => q.RetentionPeriod = v);
                newQueue.AssignStringIfNotNullOrEmpty(StaleRetentionAction, (q, v) => q.StaleRetentionAction = v);
                newQueue.AssignNumberIfNotNullOrZero(StaleRetentionPeriod, (q, v) => q.StaleRetentionPeriod = v);

                // Retention defaults (Delete/30 final, Delete/180 stale, None->Delete at
                // >= v19) are applied centrally in OrchAPISession.CreateQueue.

                if (drive.OrchAPISession.ApiVersion >= 16)
                {
                    #region Convert RetentionBucket to RetentionBucketId
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
                }

                if (ShouldProcess(target, "New Queue"))
                {
                    try
                    {
                        var createdQueue = drive.OrchAPISession.CreateQueue(folder.Id!.Value, newQueue);
                        if (createdQueue is not null)
                        {
                            drive.Queues.ClearCache(folder);
                            createdQueue.Path = folder.GetPSPath();
                            WriteObject(createdQueue);
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(target, ex), "NewQueueError", ErrorCategory.InvalidOperation, folder));
                    }
                }
            }
        }
    }
}
