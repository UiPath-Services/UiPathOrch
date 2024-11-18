using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text.Json;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsData.Update, "OrchTrigger", SupportsShouldProcess = true)]
    [OutputType(typeof(Entities.ProcessSchedule))]
    public class UpdateTriggerCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(TriggerNameCompleter<Name>))]
        [SupportsWildcards]
        public string[]? Name { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string? NewName { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(BoolCompleter))]
        public string? Enabled { get; set; }

        // このパラメータはコマンドラインでの指定を受け付けない
        // CSV で "" が指定されたら 45 にしてしまえば良いので、この型は int にする
        [Parameter(DontShow = true, ValueFromPipelineByPropertyName = true)]
        public int? SpecificPriorityValue { get; set; }

        // このパラメータは CSV インポートを受け付けない
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

        // TODO: completer がほしい
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
        [ArgumentCompleter(typeof(CalendarNameCompleter<Name>))]
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
        [ArgumentCompleter(typeof(ProcessNameCompleter<Name>))]
        [SupportsWildcards]
        public string? ReleaseName { get; set; }

        //QueueDefinitionId
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(QueueNameCompleter<Name>))]
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
        [ArgumentCompleter(typeof(MachineRobotsCompleter))]
        public string? MachineRobots { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [SupportsWildcards]
        public string[]? Path { get; set; }

        [Parameter]
        public SwitchParameter Recurse { get; set; }

        [Parameter]
        public uint Depth { get; set; }

        internal class TimeZoneCompleter : OrchArgumentCompleter
        {
            public override IEnumerable<CompletionResult> CompleteArgument(
                string commandName,
                string parameterName,
                string wordToComplete,
                CommandAst commandAst,
                IDictionary fakeBoundParameters)
            {
                if (!wordToComplete.EndsWith('?')) wordToComplete += '*';
                var wp = CreateWPFromWordToComplete(wordToComplete);

                foreach (var timeZone in TimeZoneInfo.GetSystemTimeZones()
                    .Where(t => wp.IsMatch(t.DisplayName)))
                {
                    string tiphelp = $"{timeZone.DisplayName} (Id = '{timeZone.Id}')";
                    yield return new CompletionResult(PathTools.EscapePSText(timeZone.DisplayName), timeZone.DisplayName, CompletionResultType.ParameterValue, tiphelp);
                }
            }
        }

        internal class MachineRobotsCompleter : OrchArgumentCompleter
        {
            public override IEnumerable<CompletionResult> CompleteArgument(
                string commandName,
                string parameterName,
                string wordToComplete,
                CommandAst commandAst,
                IDictionary fakeBoundParameters)
            {
                var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

                // パラメータで選択済みの Name は、候補から除外する
                var wpName = CreateWPListFromOtherParameters(commandAst, "Name", Positional.Name.Parameters);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drivesFolders, df => df.drive.GetTriggers(df.folder));

                bool bExists = false;
                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;

                    var (drive, folder) = result.Source;

                    foreach (var trigger in entities!
                        .Where(t => t.MachineRobots != null)
                        .FilterByWildcards(e => e?.Name, wpName)
                        .Where(e => e?.MachineRobots != null)
                        .OrderBy(a => a.Name))
                    {
                        bExists = true;
                        string tiphelp = trigger.GetPSPath();
                        string machineRobots = SerializeMachineRobotSessionArray(drive, folder!, trigger.MachineRobots);
                        if (string.IsNullOrEmpty(machineRobots)) continue;

                        yield return new CompletionResult("'" + machineRobots + "'", machineRobots, CompletionResultType.Text, tiphelp);
                    }
                }
                if (!bExists)
                {
                    yield return new CompletionResult("'[{\"RobotName\":\"\",\"MachineName\":\"\",\"HostMachineName\":\"\"}]'");
                }
            }
        }

        protected override void ProcessRecord()
        {
            var drivesFolders = OrchDriveInfo.EnumFolders(Path, Recurse.IsPresent, Depth);
            var wpName = Name?.Select(id => new WildcardPattern(id, WildcardOptions.IgnoreCase)).ToList();

            int? specificPriorityValue = ConvertPriorityToSpecificPriorityValue(Priority);

            if (StopProcessDate != null)
            {
                StopProcessDate = StopProcessDate.Value.ToUniversalTime();
                //DateTime.SpecifyKind(StopProcessDate.Value, DateTimeKind.Utc);
            }

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
                if (triggers == null) continue;

                var targetTriggers = triggers.SelectByWildcards(t => t?.Name, wpName);

                foreach (var trigger in targetTriggers)
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();

                    string target = trigger.GetPSPath();

                    ProcessSchedule postTrigger = OrchCollectionExtensions.DeepCopy(trigger);

                    postTrigger.AssignStringIfNotNullOrEmpty(NewName, (s, v) => s.Name = v);

                    postTrigger.AssignBoolIfNotNull(Enabled, (s, v) => s.Enabled = v);
                    postTrigger.Enabled ??= true;

                    postTrigger.AssignNumberIfNotNullOrZero(StartStrategy, (s, v) => s.StartStrategy = v);
                    postTrigger.StartStrategy ??= 1;

                    postTrigger.AssignStringIfNotNullOrEmpty(StopStrategy,                    (s, v) => s.StopStrategy = v);
                    postTrigger.AssignStringIfNotNullOrEmpty(StopProcessExpression,           (s, v) => s.StopProcessExpression = v);
                    postTrigger.AssignStringIfNotNullOrEmpty(KillProcessExpression,           (s, v) => s.KillProcessExpression = v);
                    postTrigger.AssignStringIfNotNullOrEmpty(AlertPendingExpression,          (s, v) => s.AlertPendingExpression = v);
                    postTrigger.AssignStringIfNotNullOrEmpty(AlertRunningExpression,          (s, v) => s.AlertRunningExpression = v);
                    postTrigger.AssignNumberIfNotNullOrZero(ConsecutiveJobFailuresThreshold, (s, v) => s.ConsecutiveJobFailuresThreshold = v);
                    postTrigger.AssignNumberIfNotNullOrZero(JobFailuresGracePeriodInHours,   (s, v) => s.JobFailuresGracePeriodInHours = v);

                    postTrigger.AssignStringIfNotNullOrEmpty(RuntimeType,       (s, v) => s.RuntimeType = v);
                    postTrigger.RuntimeType ??= "Unattended";

                    postTrigger.AssignStringIfNotNullOrEmpty(InputArguments,    (s, v) => postTrigger.InputArguments = v);

                    postTrigger.AssignBoolIfNotNull(ResumeOnSameContext, (s, v) => s.ResumeOnSameContext = v);
                    postTrigger.ResumeOnSameContext ??= false;

                    postTrigger.AssignBoolIfNotNull(RunAsMe, (s, v) => s.RunAsMe = v);
                    postTrigger.RunAsMe ??= false;

                    postTrigger.AssignBoolIfNotNull(IsConnected, (s, v) => s.IsConnected = v);

                    #region // CalendarName を CalendarId に変換
                    postTrigger.AssignIdFromName(
                        CalendarName,
                        drive.GetCalendars!,
                        e => e.Name!,
                        e => e.Id!,
                        (s, v) => s.CalendarId = v,
                        this, target, "Calendar");

                    postTrigger.UseCalendar = (postTrigger.CalendarId != null);
                    #endregion

                    postTrigger.AssignNumberIfNotNullOrZero(ItemsActivationThreshold,    (s, v) => s.ItemsActivationThreshold = v);
                    postTrigger.AssignNumberIfNotNullOrZero(ItemsPerJobActivationTarget, (s, v) => s.ItemsPerJobActivationTarget = v);
                    postTrigger.AssignNumberIfNotNullOrZero(MaxJobsForActivation,        (s, v) => s.MaxJobsForActivation = v);
                    postTrigger.AssignBoolIfNotNull(ActivateOnJobComplete,         (s, v) => s.ActivateOnJobComplete = v);

                    postTrigger.AssignStringIfNotNullOrEmpty(StartProcessCron,            (s, v) => s.StartProcessCron = v);
                    postTrigger.StartProcessCron ??= "0 0/1 * 1/1 * ? *";

                    postTrigger.AssignStringIfNotNullOrEmpty(StartProcessCronDetails,     (s, v) => s.StartProcessCronDetails = v);
                    postTrigger.StartProcessCronDetails ??= $"\"{{advancedCron\":\"{postTrigger.StartProcessCron}\"}}";

                    // SpecificPriorityValue を先に適用、specificPriorityValue が非 null ならそれで上書き
                    postTrigger.AssignNumberIfNotNullOrZero(SpecificPriorityValue, (s, v) => s.SpecificPriorityValue = v);
                    postTrigger.AssignNumberIfNotNullOrZero(specificPriorityValue, (s, v) => s.SpecificPriorityValue = v);

                    if (postTrigger.SpecificPriorityValue != null)
                    {
                        postTrigger.JobPriority = null;
                    }

                    //schedule.AssignString(ExecutorRobots, (s, v) => s.ExecutorRobots = v);
                    // schedule.ExecutorRobots = []; // TODO

                    #region ReleaseName を ReleaseId に変換
                    postTrigger.AssignIdFromName(
                        ReleaseName,
                        () => drive.GetReleases(folder),
                        e => e.Name!,
                        e => e.Id!,
                        (s, v) => s.ReleaseId = v,
                        this, target, "Release");
                    #endregion

                    #region QueueDefinitionName を QueueDefinitionId に変換
                    postTrigger.AssignIdFromName(
                        QueueDefinitionName,
                        () => drive.Queues.Get(folder),
                        e => e.Name!,
                        e => e.Id!,
                        (s, v) => s.QueueDefinitionId = v,
                        this, target, "Queue");
                    #endregion

                    #region TimeZone を TimeZoneId に変換
                    postTrigger.AssignStringIfNotNullOrEmpty(TimeZoneId, (s, v) => s.TimeZoneId = v);

                    postTrigger.AssignIdFromName(
                        TimeZone,
                        TimeZoneInfo.GetSystemTimeZones,
                        e => e.DisplayName,
                        e => e.Id!,
                        (s, v) => s.TimeZoneId = v,
                        this, target, "TimeZone");

                    postTrigger.TimeZoneId ??= TimeZoneInfo.Local.Id;
                    #endregion

                    postTrigger.AssignDateTimeIfNotNull(StopProcessDate, (s, v) => s.StopProcessDate = v, false);

                    #region MachineRobots をデシリアライズ
                    if (!string.IsNullOrEmpty(MachineRobots))
                    {
                        // これを呼び出しておかないと、Orchestrator がロボットの検索に失敗してしまう
                        try
                        {
                            _ = drive.RobotsFromFolder.Get(folder);
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(target, $"Update robots for '{folder.GetPSPath()}' failed", ex), "UpdateRobotFromFolderError", ErrorCategory.InvalidOperation, folder));
                        }

                        var mrss = JsonSerializer.Deserialize<MachineRobotSessionForSerialize[]>(MachineRobots);

                        List<MachineRobotSession> targets = [];

                        var robots = drive.Robots.Get();
                        var machines = drive.Machines.Get();
                        var sessions = drive.MachineSessionRuntimesByFolder.Get(folder);

                        foreach (var mrs in mrss ?? [])
                        {
                            MachineRobotSession elem = new();

                            // RobotName を変換
                            if (!string.IsNullOrEmpty(mrs.RobotName))
                            {
                                elem.RobotId = robots.FirstOrDefault(r => r.Name == mrs.RobotName)?.Id;
                            }

                            // MachineName を変換
                            if (mrs.MachineName != null)
                            {
                                elem.MachineId = machines.FirstOrDefault(m => m.Name == mrs.MachineName)?.Id;
                            }

                            // HostMachineName を変換
                            if (mrs.MachineName != null)
                            {
                                elem.SessionId = sessions.FirstOrDefault(s => s.HostMachineName == mrs.HostMachineName)?.SessionId;
                            }
                            targets.Add(elem);
                        }
                        postTrigger.MachineRobots = targets.ToArray();

                        postTrigger.ExecutorRobots = postTrigger.MachineRobots?
                            .Select(m => new RobotExecutor() { Id = m.RobotId }).ToArray();
                    }
                    else if (MachineRobots == "")
                    {
                        postTrigger.MachineRobots = null;
                    }
                    #endregion

                    postTrigger.EnvironmentId = null;
                    postTrigger.CalendarName = null; // CalendarId があるので、CalendarName は不要
                    postTrigger.Key = null;
                    postTrigger.TimeZoneIana = null;
                    postTrigger.Tags = null;

                    if (ShouldProcess(target, "Update Trigger"))
                    {
                        try
                        {
                            drive.OrchAPISession.PutProcessSchedule(folder.Id!.Value, postTrigger);
                            drive._dicTriggers?.TryRemove(folder.Id ?? 0, out _);
                            drive._dicTriggers_Exceptions.ClearCache();
                            drive._dicTriggersDetailed?.TryRemove(folder.Id ?? 0, out _);
                            drive._dicTriggersDetailed_Exceptions.ClearCache();
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
}
