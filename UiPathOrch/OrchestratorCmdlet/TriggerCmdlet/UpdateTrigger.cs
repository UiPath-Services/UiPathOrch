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

                var results = ParallelResults.ForEach(triggers, trigger => drive.GetTrigger(folder, trigger));

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

            foreach (var trigger in targetTriggers.WithCancellation(cancelHandler.Token))
            {
                string target = trigger.GetPSPath();

                ProcessSchedule postTrigger = OrchCollectionExtensions.DeepCopy(trigger);

                #region Apply user-specified parameters with dirty detection
                bool dirty = false;
                dirty |= postTrigger.AssignStringIfNotNull(NewName, trigger, s => s.Name, (s, v) => s.Name = v);
                dirty |= postTrigger.AssignBoolIfNotNull(Enabled, trigger, s => s.Enabled, (s, v) => s.Enabled = v);
                dirty |= postTrigger.AssignNumberIfNotNullOrZero(StartStrategy, trigger, s => s.StartStrategy, (s, v) => s.StartStrategy = v);
                dirty |= postTrigger.AssignStringIfNotNull(StopStrategy, trigger, s => s.StopStrategy, (s, v) => s.StopStrategy = v);
                dirty |= postTrigger.AssignStringIfNotNull(StopProcessExpression, trigger, s => s.StopProcessExpression, (s, v) => s.StopProcessExpression = v);
                dirty |= postTrigger.AssignStringIfNotNull(KillProcessExpression, trigger, s => s.KillProcessExpression, (s, v) => s.KillProcessExpression = v);
                dirty |= postTrigger.AssignStringIfNotNull(AlertPendingExpression, trigger, s => s.AlertPendingExpression, (s, v) => s.AlertPendingExpression = v);
                dirty |= postTrigger.AssignStringIfNotNull(AlertRunningExpression, trigger, s => s.AlertRunningExpression, (s, v) => s.AlertRunningExpression = v);
                dirty |= postTrigger.AssignNumberIfNotNullOrZero(ConsecutiveJobFailuresThreshold, trigger, s => s.ConsecutiveJobFailuresThreshold, (s, v) => s.ConsecutiveJobFailuresThreshold = v);
                dirty |= postTrigger.AssignNumberIfNotNullOrZero(JobFailuresGracePeriodInHours, trigger, s => s.JobFailuresGracePeriodInHours, (s, v) => s.JobFailuresGracePeriodInHours = v);
                dirty |= postTrigger.AssignStringIfNotNull(RuntimeType, trigger, s => s.RuntimeType, (s, v) => s.RuntimeType = v);
                dirty |= postTrigger.AssignStringIfNotNull(InputArguments, trigger, s => s.InputArguments, (s, v) => s.InputArguments = v);
                dirty |= postTrigger.AssignBoolIfNotNull(ResumeOnSameContext, trigger, s => s.ResumeOnSameContext, (s, v) => s.ResumeOnSameContext = v);
                dirty |= postTrigger.AssignBoolIfNotNull(RunAsMe, trigger, s => s.RunAsMe, (s, v) => s.RunAsMe = v);
                dirty |= postTrigger.AssignBoolIfNotNull(IsConnected, trigger, s => s.IsConnected, (s, v) => s.IsConnected = v);
                dirty |= postTrigger.AssignBoolIfNotNull(ActivateOnJobComplete, trigger, s => s.ActivateOnJobComplete, (s, v) => s.ActivateOnJobComplete = v);
                dirty |= postTrigger.AssignNumberIfNotNullOrZero(ItemsActivationThreshold, trigger, s => s.ItemsActivationThreshold, (s, v) => s.ItemsActivationThreshold = v);
                dirty |= postTrigger.AssignNumberIfNotNullOrZero(ItemsPerJobActivationTarget, trigger, s => s.ItemsPerJobActivationTarget, (s, v) => s.ItemsPerJobActivationTarget = v);
                dirty |= postTrigger.AssignNumberIfNotNullOrZero(MaxJobsForActivation, trigger, s => s.MaxJobsForActivation, (s, v) => s.MaxJobsForActivation = v);
                dirty |= postTrigger.AssignStringIfNotNull(StartProcessCron, trigger, s => s.StartProcessCron, (s, v) => s.StartProcessCron = v);
                dirty |= postTrigger.AssignStringIfNotNull(StartProcessCronDetails, trigger, s => s.StartProcessCronDetails, (s, v) => s.StartProcessCronDetails = v);
                dirty |= postTrigger.AssignNumberIfNotNullOrZero(SpecificPriorityValue, trigger, s => s.SpecificPriorityValue, (s, v) => s.SpecificPriorityValue = v);
                dirty |= postTrigger.AssignNumberIfNotNullOrZero(specificPriorityValue, trigger, s => s.SpecificPriorityValue, (s, v) => s.SpecificPriorityValue = v);
                dirty |= postTrigger.AssignStringIfNotNull(TimeZoneId, trigger, s => s.TimeZoneId, (s, v) => s.TimeZoneId = v);

                #region // Convert CalendarName to CalendarId
                postTrigger.AssignIdFromName(
                    CalendarName,
                    () => drive.Calendars.Get(),
                    e => e.Name!,
                    e => e.Id!,
                    (s, v) => { if (trigger.CalendarId != v) { s.CalendarId = v; dirty = true; } },
                    this, target, "Calendar");
                #endregion

                #region Convert ReleaseName to ReleaseId
                postTrigger.AssignIdFromName(
                    ReleaseName,
                    () => drive.Releases.Get(folder),
                    e => e.Name!,
                    e => e.Id!,
                    (s, v) => { if (trigger.ReleaseId != v) { s.ReleaseId = v; dirty = true; } },
                    this, target, "Release");
                #endregion

                #region Convert QueueDefinitionName to QueueDefinitionId
                postTrigger.AssignIdFromName(
                    QueueDefinitionName,
                    () => drive.Queues.Get(folder),
                    e => e.Name!,
                    e => e.Id!,
                    (s, v) => { if (trigger.QueueDefinitionId != v) { s.QueueDefinitionId = v; dirty = true; } },
                    this, target, "Queue");
                #endregion

                #region Convert TimeZone to TimeZoneId
                postTrigger.AssignIdFromName(
                    TimeZone,
                    TimeZoneInfo.GetSystemTimeZones,
                    e => e.DisplayName,
                    e => e.Id!,
                    (s, v) => { if (trigger.TimeZoneId != v) { s.TimeZoneId = v; dirty = true; } },
                    this, target, "TimeZone");
                #endregion

                postTrigger.AssignDateTimeIfNotNull(utcStopProcessDate, (s, v) => s.StopProcessDate = v, false);
                if (utcStopProcessDate is not null) dirty = true;

                if (MachineRobots is not null)
                {
                    postTrigger.MachineRobots = DeserializeMachineRobotSessions(this, drive, folder, postTrigger.GetPSPath(), MachineRobots);
                    dirty = true;
                }

                if (ExecutorRobots is not null)
                {
                    postTrigger.ExecutorRobots = DeserializeExecutorRobots(this, drive, folder, postTrigger.GetPSPath(), ExecutorRobots);
                    dirty = true;
                }
                #endregion

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
}
