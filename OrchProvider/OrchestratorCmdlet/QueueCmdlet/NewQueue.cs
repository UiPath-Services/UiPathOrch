using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.New, "OrchQueue", SupportsShouldProcess = true)]
    [OutputType(typeof(QueueDefinition))]
    public class AddQueueCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(QueueNameCompleter<TPositional>))]
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
        [ArgumentCompleter(typeof(BucketNameCompleter<TPositional>))]
        [SupportsWildcards]
        public string? RetentionBucket { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string[]? Tags { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [SupportsWildcards]
        public string[]? Path { get; set; }

        //[Parameter]
        //public SwitchParameter Recurse { get; set; }

        //[Parameter]
        //public uint Depth { get; set; }

        protected override void ProcessRecord()
        {
            var drivesFolders = OrchDriveInfo.EnumFolders(Path);

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var (drive, folder) in drivesFolders)
            {
                foreach (var name in Name!)
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();

                    string target = System.IO.Path.Combine(folder.GetPSPath(), name);

                    QueueDefinitionPosting newQueue = new()
                    {
                        Name = WildcardPattern.Unescape(name),
                    };

                    newQueue.AssignStringIfNotNull(Description,        (q, v) => q.Description = v);
                    newQueue.AssignBoolIfNotNull(AcceptAutomaticallyRetry,  (q, v) => q.AcceptAutomaticallyRetry = v);
                    newQueue.AssignBoolIfNotNull(RetryAbandonedItems,       (q, v) => q.RetryAbandonedItems = v);
                    newQueue.AssignNumberIfNotNullOrZero(MaxNumberOfRetries,      (q, v) => q.MaxNumberOfRetries = v);
                    newQueue.AssignBoolIfNotNull(EnforceUniqueReference,    (q, v) => q.EnforceUniqueReference = v);
                    newQueue.AssignBoolIfNotNull(Encrypted,                 (q, v) => q.Encrypted = v);
                    //newQueue.assig  ProcessScheduleId": null,
                    newQueue.AssignStringIfNotNullOrEmpty(SpecificDataJsonSchema,  (q, v) => q.SpecificDataJsonSchema = v);
                    newQueue.AssignStringIfNotNullOrEmpty(OutputDataJsonSchema,    (q, v) => q.OutputDataJsonSchema = v);
                    newQueue.AssignStringIfNotNullOrEmpty(AnalyticsDataJsonSchema, (q, v) => q.AnalyticsDataJsonSchema = v);

                    #region Release を ReleaseId に変換
                    newQueue.AssignIdFromName(
                        Release,
                        () => drive.GetReleases(folder),
                        e => e.Name!,
                        e => e.Id!,
                        (s, v) => s.ReleaseId = v,
                        this, target, "Release");
                    #endregion

                    newQueue.AssignNumberIfNotNullOrZero(SlaInMinutes,     (q, v) => q.SlaInMinutes = v);
                    newQueue.AssignNumberIfNotNullOrZero(RiskSlaInMinutes, (q, v) => q.RiskSlaInMinutes = v);
                    newQueue.AssignTags(Tags, (q, v) => q.Tags = v);

                    newQueue.AssignStringIfNotNullOrEmpty(RetentionAction, (q, v) => q.RetentionAction = v);
                    newQueue.AssignNumberIfNotNullOrZero(RetentionPeriod, (q, v) => q.RetentionPeriod = v);

                    if (drive.OrchAPISession.ApiVersion >= 16)
                    {
                        newQueue.RetentionAction ??= "Delete";
                        newQueue.RetentionPeriod ??= 30;

                        #region RetentionBucket を RetentionBucketId に変換
                        newQueue.AssignIdFromName(
                            RetentionBucket,
                            () => drive.Buckets.Get(folder),
                            e => e.Name!,
                            e => e.Id!,
                            (s, v) => s.RetentionBucketId = v,
                            this, target, "RetentionBucket");
                        #endregion
                    }

                    if (ShouldProcess(target, "Add Queue"))
                    {
                        try
                        {
                            var createdQueue = drive.OrchAPISession.CreateQueue(folder.Id!.Value, newQueue);
                            if (createdQueue != null)
                            {
                                drive.Queues.ClearCache(folder);
                                createdQueue.Path = folder.GetPSPath();
                                WriteObject(createdQueue);
                            }
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(target, ex), "AddQueueError", ErrorCategory.InvalidOperation, folder));
                        }
                    }
                }
            }
        }
    }
}
