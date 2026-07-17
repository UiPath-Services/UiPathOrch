using System.Collections;
using UiPath.PowerShell.Positional;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsData.Update, "OrchTrigger", SupportsShouldProcess = true)]
[OutputType(typeof(Entities.ProcessSchedule))]
public class UpdateTriggerCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TriggerNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? NewName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BoolCompleter))]
    public string? Enabled { get; set; }

    // This parameter does not accept command-line input
    // Since we can just treat "" in CSV as 45, the type is int
    [Parameter(DontShow = true, ValueFromPipelineByPropertyName = true)]
    public int? SpecificPriorityValue { get; set; }

    // This parameter does not accept CSV import
    [Parameter]
    [ArgumentCompleter(typeof(StaticTextsCompleter<JobPriorityItems>))]
    public string? Priority { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public int? StartStrategy { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<SoftStop_Kill>))]
    public string? StopStrategy { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? StopProcessExpression { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? KillProcessExpression { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? AlertPendingExpression { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? AlertRunningExpression { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public int? ConsecutiveJobFailuresThreshold { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public int? JobFailuresGracePeriodInHours { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<RuntimeTypes>))]
    public string? RuntimeType { get; set; }

    // TODO: Need a completer
    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? InputArguments { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BoolCompleter))]
    public string? ResumeOnSameContext { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BoolCompleter))]
    public string? RunAsMe { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BoolCompleter))]
    public string? IsConnected { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(CalendarNameCompleter))]
    [SupportsWildcards]
    public string? CalendarName { get; set; }

    // After completing jobs, reassess conditions and start new jobs if possible
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BoolCompleter))]
    public string? ActivateOnJobComplete { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public int? ItemsActivationThreshold { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public int? ItemsPerJobActivationTarget { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public int? MaxJobsForActivation { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? StartProcessCronDetails { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? StartProcessCron { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(ProcessNameCompleter))]
    [SupportsWildcards]
    public string? ReleaseName { get; set; }

    //QueueDefinitionId
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(QueueNameCompleter))]
    [SupportsWildcards]
    public string? QueueDefinitionName { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(TimeZoneCompleter))]
    [SupportsWildcards]
    public string? TimeZone { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true, DontShow = true)]
    [SupportsWildcards]
    public string? TimeZoneId { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(OneWeekAfterCompleter))]
    public DateTime? StopProcessDate { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(ExecutorRobotsCompleter))]
    [RobotExecutorArgumentTransformation]
    public string[]? ExecutorRobots { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TimeTriggerMachineRobotsCompleter))]
    public string[]? MachineRobots { get; set; }

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

    // Display user names currently configured on the trigger as candidates. Implementation for Update-OrchTrigger.
    // For New-OrchTrigger, showing all available users would be more user-friendly, so this implementation is not shared.
    private class ExecutorRobotsCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

            // Exclude already-selected Names from candidates
            var wpExecutorRobots = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);

            // Retrieve and display ExecutorRobots for triggers matching the specified Name
            var wpName = GetFakeBoundParameters(fakeBoundParameters, "Name").ConvertToWildcardPatternList();

            var wp = CreateWPFromWordToComplete(wordToComplete);

            foreach (var (drive, folder) in drivesFolders)
            {
                var triggers = drive.GetTriggers(folder)
                    .FilterByWildcards(t => t?.Name, wpName)
                    .OrderBy(t => t.Name);

                var results = ParallelResults.ForEach(triggers, trigger => drive.TriggersDetailed.Get(folder, trigger.Id!.Value));

                foreach (var result in results)
                {
                    if (result.Item.ExecutorRobots is null || result.Item.ExecutorRobots.Length == 0) continue;

                    var executerRobots = SerializeExecutorRobotArray(drive, result.Item.ExecutorRobots);
                    if (wpExecutorRobots is not null && wpExecutorRobots.Any(wpe => wpe.IsMatch(executerRobots))) continue;
                    if (!string.IsNullOrEmpty(executerRobots))
                    {
                        string tooltip = result.Item.GetPSPath();
                        yield return new CompletionResult(PathTools.EscapePSText(executerRobots), executerRobots, CompletionResultType.Text, tooltip);
                    }
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(EffectivePath(Path, LiteralPath), Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();
        int? specificPriorityValue = ConvertPriorityToSpecificPriorityValue(Priority);

        // CSV export joins ExecutorRobots into one comma-separated cell; split it (honoring escaped
        // commas) to match New-OrchTrigger, otherwise a multi-robot cell binds as a single wildcard
        // and the executor-robot assignment is lost on re-import via Update-OrchTrigger.
        ExecutorRobots = ExecutorRobots.SplitValuesByUnescapedCommasPreservingEscapes()?.ToArray();

        var utcStopProcessDate = StopProcessDate?.ToUniversalTime();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            IEnumerable<ProcessSchedule> triggers = null;
            try
            {
                triggers = drive.GetTriggers(folder);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "GetTriggerError", ErrorCategory.InvalidOperation, folder));
            }
            if (triggers is null) continue;

            var targetTriggers = triggers.SelectByWildcards(t => t?.Name, wpName).OrderBy(t => t.Name);

            foreach (var trigger in targetTriggers
                .WithProgressBar(this, $"Updating triggers in {folder.GetPSPath()}", t => t.Name)
                .WithCancellation(cancelHandler.Token))
            {
                string target = trigger.GetPSPath();

                ProcessSchedule postTrigger = OrchCollectionExtensions.DeepCopy(trigger);

                // Resolve every API/host-dependent input (name -> id, robot deserialization) up
                // front, then hand it all to the pure, API-free ComputeTriggerUpdate core so the
                // change decision is unit-testable without a live Orchestrator. Each AssignIdFromName
                // below preserves the exact not-found / ambiguous error emission; a throwaway target
                // captures only the resolved id + a resolved flag (a null name leaves the setter
                // uncalled -> skipped; an empty name resolves to the default id -> clears, matching
                // the previous inline behavior).
                long? resolvedCalendarId = null; bool calendarResolved = false;
                new ProcessSchedule().AssignIdFromName(
                    CalendarName, () => drive.Calendars.Get(), e => e.Name!, e => e.Id!,
                    (s, v) => { resolvedCalendarId = v; calendarResolved = true; }, this, target, "Calendar");

                long? resolvedReleaseId = null; bool releaseResolved = false;
                new ProcessSchedule().AssignIdFromName(
                    ReleaseName, () => drive.Releases.Get(folder), e => e.Name!, e => e.Id!,
                    (s, v) => { resolvedReleaseId = v; releaseResolved = true; }, this, target, "Release");

                long? resolvedQueueDefinitionId = null; bool queueResolved = false;
                new ProcessSchedule().AssignIdFromName(
                    QueueDefinitionName, () => drive.Queues.Get(folder), e => e.Name!, e => e.Id!,
                    (s, v) => { resolvedQueueDefinitionId = v; queueResolved = true; }, this, target, "Queue");

                string? resolvedTimeZoneId = null; bool timeZoneResolved = false;
                new ProcessSchedule().AssignIdFromName(
                    TimeZone, TimeZoneInfo.GetSystemTimeZones, e => e.DisplayName, e => e.Id!,
                    (s, v) => { resolvedTimeZoneId = v; timeZoneResolved = true; }, this, target, "TimeZone");

                bool machineRobotsSpecified = MachineRobots is not null;
                MachineRobotSession[]? resolvedMachineRobots = null;
                if (machineRobotsSpecified)
                {
                    resolvedMachineRobots = DeserializeMachineRobotSessions(this, drive, folder, postTrigger.GetPSPath(), MachineRobots, out bool mrParseFailed);
                    if (mrParseFailed) continue; // malformed -MachineRobots: error already written, skip this trigger
                }

                bool executorRobotsSpecified = ExecutorRobots is not null;
                RobotExecutor[]? resolvedExecutorRobots = null;
                if (executorRobotsSpecified)
                {
                    resolvedExecutorRobots = DeserializeExecutorRobots(this, drive, folder, postTrigger.GetPSPath(), ExecutorRobots);

                    // Modern Orchestrator (API v12+) assigns trigger execution as
                    // account+machine pairs; -ExecutorRobots alone writes the robot
                    // relation but no machine mapping — a shape the web dialog never
                    // produces. Warn on the likely mix-up. Not an error: it is the only
                    // assignment form API v11 has, and a CSV round-trip can legitimately
                    // carry an ExecutorRobots-only value.
                    if (MachineRobots is null && drive.OrchAPISession.ApiVersion >= 12)
                    {
                        WriteWarning($"'{postTrigger.GetPSPath()}': -ExecutorRobots alone does not set the account+machine pairs modern Orchestrator uses. The web trigger dialog assigns robot+machine pairs — use -MachineRobots (ExecutorRobots is then sent automatically) unless you intend a robot-only assignment.");
                    }
                }

                bool dirty = ComputeTriggerUpdate(postTrigger, trigger, new TriggerUpdateInputs
                {
                    NewName = NewName,
                    Enabled = Enabled,
                    StartStrategy = StartStrategy,
                    StopStrategy = StopStrategy,
                    StopProcessExpression = StopProcessExpression,
                    KillProcessExpression = KillProcessExpression,
                    AlertPendingExpression = AlertPendingExpression,
                    AlertRunningExpression = AlertRunningExpression,
                    ConsecutiveJobFailuresThreshold = ConsecutiveJobFailuresThreshold,
                    JobFailuresGracePeriodInHours = JobFailuresGracePeriodInHours,
                    RuntimeType = RuntimeType,
                    InputArguments = InputArguments,
                    ResumeOnSameContext = ResumeOnSameContext,
                    RunAsMe = RunAsMe,
                    IsConnected = IsConnected,
                    ActivateOnJobComplete = ActivateOnJobComplete,
                    ItemsActivationThreshold = ItemsActivationThreshold,
                    ItemsPerJobActivationTarget = ItemsPerJobActivationTarget,
                    MaxJobsForActivation = MaxJobsForActivation,
                    StartProcessCron = StartProcessCron,
                    StartProcessCronDetails = StartProcessCronDetails,
                    SpecificPriorityValue = SpecificPriorityValue,
                    SpecificPriorityValueFromPriority = specificPriorityValue,
                    TimeZoneId = TimeZoneId,
                    ResolvedCalendarId = resolvedCalendarId,
                    CalendarResolved = calendarResolved,
                    ResolvedReleaseId = resolvedReleaseId,
                    ReleaseResolved = releaseResolved,
                    ResolvedQueueDefinitionId = resolvedQueueDefinitionId,
                    QueueResolved = queueResolved,
                    ResolvedTimeZoneId = resolvedTimeZoneId,
                    TimeZoneResolved = timeZoneResolved,
                    UtcStopProcessDate = utcStopProcessDate,
                    MachineRobotsSpecified = machineRobotsSpecified,
                    ResolvedMachineRobots = resolvedMachineRobots,
                    ExecutorRobotsSpecified = executorRobotsSpecified,
                    ResolvedExecutorRobots = resolvedExecutorRobots,
                });

                if (!dirty)
                {
                    continue;
                }

                #region Apply defaults required by PUT
                postTrigger.Enabled ??= true;
                postTrigger.StartStrategy ??= 1;
                postTrigger.RuntimeType ??= "Unattended";
                postTrigger.InputArguments ??= "{}";
                postTrigger.ResumeOnSameContext ??= false;
                postTrigger.RunAsMe ??= false;
                postTrigger.StartProcessCron ??= "0 0/1 * 1/1 * ? *";
                postTrigger.StartProcessCronDetails ??= $"{{\"advancedCron\":{System.Text.Json.JsonSerializer.Serialize(postTrigger.StartProcessCron ?? "")}}}";
                postTrigger.TimeZoneId ??= TimeZoneInfo.Local.Id;
                postTrigger.UseCalendar = (postTrigger.CalendarId is not null);
                if (postTrigger.SpecificPriorityValue is not null)
                {
                    postTrigger.JobPriority = null;
                }
                #endregion

                #region Nullify fields not needed for PUT
                postTrigger.PackageName = null;
                postTrigger.ReleaseKey = null;
                postTrigger.ExternalJobKeyScheduler = null;
                postTrigger.StartProcessCronSummary = null;
                postTrigger.StartProcessNextOccurrence = null;
                postTrigger.EnvironmentId = null;
                postTrigger.CalendarName = null;
                postTrigger.Key = null;
                postTrigger.TimeZoneIana = null;
                postTrigger.Tags = null;

                // ProcessScheduleDto below API v12 has no MachineRobots member (swagger
                // v11): classic robots are already user+machine-bound, so the derived
                // ExecutorRobots above carries the whole assignment there. Strip the
                // member the old surface doesn't know. Unknown version sends both.
                if (drive.OrchAPISession.ApiVersion < 12) postTrigger.MachineRobots = null;
                #endregion

                if (ShouldProcess(target, "Update Trigger"))
                {
                    try
                    {
                        drive.OrchAPISession.PutProcessSchedule(folder.Id!.Value, postTrigger);
                        drive.Triggers.ClearCache(folder);
                        drive.TriggersDetailed.ClearCache(folder);
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(target, ex), "UpdateTriggerError", ErrorCategory.InvalidOperation, folder));
                    }
                }
            }
        }
    }

    /// <summary>
    /// Pure inputs for <see cref="ComputeTriggerUpdate"/>. Everything that needs an API/host
    /// round-trip (Calendar/Release/Queue/TimeZone name -> id, robot deserialization) is resolved
    /// by the cmdlet first and passed in here, so change detection is fully testable without a
    /// live Orchestrator.
    /// </summary>
    internal sealed class TriggerUpdateInputs
    {
        public string? NewName { get; init; }
        public string? Enabled { get; init; }
        public int? StartStrategy { get; init; }
        public string? StopStrategy { get; init; }
        public string? StopProcessExpression { get; init; }
        public string? KillProcessExpression { get; init; }
        public string? AlertPendingExpression { get; init; }
        public string? AlertRunningExpression { get; init; }
        public int? ConsecutiveJobFailuresThreshold { get; init; }
        public int? JobFailuresGracePeriodInHours { get; init; }
        public string? RuntimeType { get; init; }
        public string? InputArguments { get; init; }
        public string? ResumeOnSameContext { get; init; }
        public string? RunAsMe { get; init; }
        public string? IsConnected { get; init; }
        public string? ActivateOnJobComplete { get; init; }
        public int? ItemsActivationThreshold { get; init; }
        public int? ItemsPerJobActivationTarget { get; init; }
        public int? MaxJobsForActivation { get; init; }
        public string? StartProcessCron { get; init; }
        public string? StartProcessCronDetails { get; init; }
        public int? SpecificPriorityValue { get; init; }
        /// <summary>The int? derived from -Priority (applied to SpecificPriorityValue after the direct param, matching the original order).</summary>
        public int? SpecificPriorityValueFromPriority { get; init; }
        public string? TimeZoneId { get; init; }

        public long? ResolvedCalendarId { get; init; }
        public bool CalendarResolved { get; init; }
        public long? ResolvedReleaseId { get; init; }
        public bool ReleaseResolved { get; init; }
        public long? ResolvedQueueDefinitionId { get; init; }
        public bool QueueResolved { get; init; }
        public string? ResolvedTimeZoneId { get; init; }
        public bool TimeZoneResolved { get; init; }

        public DateTime? UtcStopProcessDate { get; init; }

        public bool MachineRobotsSpecified { get; init; }
        public MachineRobotSession[]? ResolvedMachineRobots { get; init; }
        public bool ExecutorRobotsSpecified { get; init; }
        public RobotExecutor[]? ResolvedExecutorRobots { get; init; }
    }

    /// <summary>
    /// Applies the requested changes onto <paramref name="payload"/> (a deep copy of
    /// <paramref name="source"/>) and returns whether anything actually changed, so the caller can
    /// skip the PUT — and the full-record audit entry it produces — when the request is a no-op.
    /// Reproduces the cmdlet's field order exactly. No API access — unit-testable in isolation.
    /// </summary>
    internal static bool ComputeTriggerUpdate(ProcessSchedule payload, ProcessSchedule source, TriggerUpdateInputs input)
    {
        bool dirty = false;

        dirty |= payload.AssignStringIfNotNull(input.NewName, source, s => s.Name, (s, v) => s.Name = v);
        dirty |= payload.AssignBoolIfNotNull(input.Enabled, source, s => s.Enabled, (s, v) => s.Enabled = v);
        dirty |= payload.AssignNumberIfNotNullOrZero(input.StartStrategy, source, s => s.StartStrategy, (s, v) => s.StartStrategy = v);
        dirty |= payload.AssignStringIfNotNull(input.StopStrategy, source, s => s.StopStrategy, (s, v) => s.StopStrategy = v);
        dirty |= payload.AssignStringIfNotNull(input.StopProcessExpression, source, s => s.StopProcessExpression, (s, v) => s.StopProcessExpression = v);
        dirty |= payload.AssignStringIfNotNull(input.KillProcessExpression, source, s => s.KillProcessExpression, (s, v) => s.KillProcessExpression = v);
        dirty |= payload.AssignStringIfNotNull(input.AlertPendingExpression, source, s => s.AlertPendingExpression, (s, v) => s.AlertPendingExpression = v);
        dirty |= payload.AssignStringIfNotNull(input.AlertRunningExpression, source, s => s.AlertRunningExpression, (s, v) => s.AlertRunningExpression = v);
        dirty |= payload.AssignNumberIfNotNullOrZero(input.ConsecutiveJobFailuresThreshold, source, s => s.ConsecutiveJobFailuresThreshold, (s, v) => s.ConsecutiveJobFailuresThreshold = v);
        dirty |= payload.AssignNumberIfNotNullOrZero(input.JobFailuresGracePeriodInHours, source, s => s.JobFailuresGracePeriodInHours, (s, v) => s.JobFailuresGracePeriodInHours = v);
        dirty |= payload.AssignStringIfNotNull(input.RuntimeType, source, s => s.RuntimeType, (s, v) => s.RuntimeType = v);
        dirty |= payload.AssignStringIfNotNull(input.InputArguments, source, s => s.InputArguments, (s, v) => s.InputArguments = v);
        dirty |= payload.AssignBoolIfNotNull(input.ResumeOnSameContext, source, s => s.ResumeOnSameContext, (s, v) => s.ResumeOnSameContext = v);
        dirty |= payload.AssignBoolIfNotNull(input.RunAsMe, source, s => s.RunAsMe, (s, v) => s.RunAsMe = v);
        dirty |= payload.AssignBoolIfNotNull(input.IsConnected, source, s => s.IsConnected, (s, v) => s.IsConnected = v);
        dirty |= payload.AssignBoolIfNotNull(input.ActivateOnJobComplete, source, s => s.ActivateOnJobComplete, (s, v) => s.ActivateOnJobComplete = v);
        dirty |= payload.AssignNumberIfNotNullOrZero(input.ItemsActivationThreshold, source, s => s.ItemsActivationThreshold, (s, v) => s.ItemsActivationThreshold = v);
        dirty |= payload.AssignNumberIfNotNullOrZero(input.ItemsPerJobActivationTarget, source, s => s.ItemsPerJobActivationTarget, (s, v) => s.ItemsPerJobActivationTarget = v);
        dirty |= payload.AssignNumberIfNotNullOrZero(input.MaxJobsForActivation, source, s => s.MaxJobsForActivation, (s, v) => s.MaxJobsForActivation = v);
        dirty |= payload.AssignStringIfNotNull(input.StartProcessCron, source, s => s.StartProcessCron, (s, v) => s.StartProcessCron = v);
        dirty |= payload.AssignStringIfNotNull(input.StartProcessCronDetails, source, s => s.StartProcessCronDetails, (s, v) => s.StartProcessCronDetails = v);
        dirty |= payload.AssignNumberIfNotNullOrZero(input.SpecificPriorityValue, source, s => s.SpecificPriorityValue, (s, v) => s.SpecificPriorityValue = v);
        dirty |= payload.AssignNumberIfNotNullOrZero(input.SpecificPriorityValueFromPriority, source, s => s.SpecificPriorityValue, (s, v) => s.SpecificPriorityValue = v);
        dirty |= payload.AssignStringIfNotNull(input.TimeZoneId, source, s => s.TimeZoneId, (s, v) => s.TimeZoneId = v);

        // Name -> id resolutions (resolved by the cmdlet); diff against the source, matching the
        // original inline `if (source.XId != v)` guards.
        if (input.CalendarResolved && source.CalendarId != input.ResolvedCalendarId) { payload.CalendarId = input.ResolvedCalendarId; dirty = true; }
        if (input.ReleaseResolved && source.ReleaseId != input.ResolvedReleaseId) { payload.ReleaseId = input.ResolvedReleaseId; dirty = true; }
        if (input.QueueResolved && source.QueueDefinitionId != input.ResolvedQueueDefinitionId) { payload.QueueDefinitionId = input.ResolvedQueueDefinitionId; dirty = true; }
        if (input.TimeZoneResolved && source.TimeZoneId != input.ResolvedTimeZoneId) { payload.TimeZoneId = input.ResolvedTimeZoneId; dirty = true; }

        payload.AssignDateTimeIfNotNull(input.UtcStopProcessDate, (s, v) => s.StopProcessDate = v, false);
        if (input.UtcStopProcessDate is not null && !Nullable.Equals(source.StopProcessDate, payload.StopProcessDate)) dirty = true;

        if (input.MachineRobotsSpecified)
        {
            // Compare on the fields the deserialized form carries (MachineId, RobotId, SessionId)
            // so re-sending the same assignment is a no-op instead of a full re-write.
            if (!OrchStringExtensions.UnorderedEquals(source.MachineRobots, input.ResolvedMachineRobots, m => $"{m.MachineId}|{m.RobotId}|{m.SessionId}"))
            {
                payload.MachineRobots = input.ResolvedMachineRobots;
                dirty = true;
            }
        }

        if (input.ExecutorRobotsSpecified)
        {
            // The deserialized form carries only Id, so diff on it.
            if (!OrchStringExtensions.UnorderedEquals(source.ExecutorRobots, input.ResolvedExecutorRobots, e => e.Id?.ToString() ?? ""))
            {
                payload.ExecutorRobots = input.ResolvedExecutorRobots;
                dirty = true;
            }
        }
        else if (input.MachineRobotsSpecified)
        {
            // Mirror the web PUT: -MachineRobots alone must still write the robot relation, or the
            // trigger screen shows an empty Account and RobotUserName reads back null. No dirty flag
            // needed — it rides on the -MachineRobots change above.
            payload.ExecutorRobots = DeriveExecutorRobotsFromMachineRobots(payload.MachineRobots);
        }

        return dirty;
    }
}
