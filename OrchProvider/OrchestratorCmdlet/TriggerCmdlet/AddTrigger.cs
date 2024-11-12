using System.Data;
using System.Management.Automation;
using System.Text.Json;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Add, "OrchTrigger", SupportsShouldProcess = true)]
    [OutputType(typeof(Entities.ProcessSchedule))]
    public class AddTriggerCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(TriggerNameCompleter<Name>))]
        public string[]? Name { get; set; }

        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(ProcessNameCompleter<Name>))]
        [SupportsWildcards]
        public string? ReleaseName { get; set; }

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
        public string? MachineRobots { get; set; }

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

            int? specificPriorityValue = ConvertPriorityToSpecificPriorityValue(Priority);

            // ExecutorRobots は、MachineRobots から生成すれば良いので、パラメータからは外す。
            // 先頭の要素は CSV から入力されている可能性があるので、先頭の要素についてはカンマで区切る
            //if (ExecutorRobots != null && ExecutorRobots.Length > 0) ExecutorRobots = ExecutorRobots[0].Split(',').Concat(ExecutorRobots.Skip(1)).ToArray();

            // MachineRobots は JSON でシリアライズしているため、配列ではないのでそのまま。。

            //if (StopProcessDate != null)
            //{
            //    StopProcessDate = DateTime.SpecifyKind(StopProcessDate.Value, DateTimeKind.Utc);
            //}

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var (drive, folder) in drivesFolders)
            {
                foreach (var name in Name!)
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();

                    string target = System.IO.Path.Combine(folder.GetPSPath(), name);

                    ProcessSchedule schedule = new()
                    {
                        Name = WildcardPattern.Unescape(name)
                    };

                    schedule.AssignBoolIfNotNull(Enabled, (s, v) => s.Enabled = v);
                    schedule.Enabled ??= true;

                    schedule.AssignNumberIfNotNullOrZero(StartStrategy, (s, v) => s.StartStrategy = v);
                    schedule.StartStrategy ??= 1;

                    schedule.AssignStringIfNotNullOrEmpty(StopStrategy,                    (s, v) => s.StopStrategy = v);
                    schedule.AssignStringIfNotNullOrEmpty(StopProcessExpression,           (s, v) => s.StopProcessExpression = v);
                    schedule.AssignStringIfNotNullOrEmpty(KillProcessExpression,           (s, v) => s.KillProcessExpression = v);
                    schedule.AssignStringIfNotNullOrEmpty(AlertPendingExpression,          (s, v) => s.AlertPendingExpression = v);
                    schedule.AssignStringIfNotNullOrEmpty(AlertRunningExpression,          (s, v) => s.AlertRunningExpression = v);
                    schedule.AssignNumberIfNotNullOrZero(ConsecutiveJobFailuresThreshold, (s, v) => s.ConsecutiveJobFailuresThreshold = v);
                    schedule.AssignNumberIfNotNullOrZero(JobFailuresGracePeriodInHours,   (s, v) => s.JobFailuresGracePeriodInHours = v);

                    #region SpecificPriorityValue
                    // SpecificPriorityValue を先に適用、specificPriorityValue が非 null ならそれで上書き
                    schedule.AssignNumberIfNotNullOrZero(SpecificPriorityValue,           (s, v) => s.SpecificPriorityValue = v);
                    schedule.AssignNumberIfNotNullOrZero(specificPriorityValue,           (s, v) => s.SpecificPriorityValue = v);
                    #endregion

                    schedule.AssignStringIfNotNullOrEmpty(RuntimeType,       (s, v) => s.RuntimeType = v);
                    schedule.RuntimeType ??= "Unattended";

                    schedule.AssignStringIfNotNullOrEmpty(InputArguments,    (s, v) => schedule.InputArguments = v);

                    schedule.AssignBoolIfNotNull(ResumeOnSameContext, (s, v) => s.ResumeOnSameContext = v);
                    schedule.ResumeOnSameContext ??= false;

                    schedule.AssignBoolIfNotNull(RunAsMe,             (s, v) => s.RunAsMe = v);
                    schedule.RunAsMe ??= false;

                    schedule.AssignBoolIfNotNull(IsConnected,         (s, v) => s.IsConnected = v);

                    #region // CalendarName を CalendarId に変換
                    schedule.AssignIdFromName(
                        CalendarName,
                        drive.GetCalendars!,
                        e => e.Name!,
                        e => e.Id!,
                        (s, v) => s.CalendarId = v,
                        this, target, "Calendar");

                    schedule.UseCalendar = (schedule.CalendarId != null);
                    #endregion

                    schedule.AssignNumberIfNotNullOrZero(ItemsActivationThreshold,    (s, v) => s.ItemsActivationThreshold = v);
                    schedule.AssignNumberIfNotNullOrZero(ItemsPerJobActivationTarget, (s, v) => s.ItemsPerJobActivationTarget = v);
                    schedule.AssignNumberIfNotNullOrZero(MaxJobsForActivation,        (s, v) => s.MaxJobsForActivation = v);
                    schedule.AssignBoolIfNotNull(ActivateOnJobComplete,         (s, v) => s.ActivateOnJobComplete = v);

                    schedule.AssignStringIfNotNullOrEmpty(StartProcessCron,            (s, v) => s.StartProcessCron = v);
                    schedule.StartProcessCron ??= "0 0/1 * 1/1 * ? *";

                    schedule.AssignStringIfNotNullOrEmpty(StartProcessCronDetails,     (s, v) => s.StartProcessCronDetails = v);
                    schedule.StartProcessCronDetails ??= $"{{\"advancedCron\":\"{schedule.StartProcessCron}\"}}";

                    #region ReleaseName を ReleaseId に変換
                    schedule.AssignIdFromName(
                        ReleaseName,
                        () => drive.GetReleases(folder),
                        e => e.Name!,
                        e => e.Id!,
                        (s, v) => s.ReleaseId = v,
                        this, target, "Release");
                    #endregion

                    #region QueueDefinitionName を QueueDefinitionId に変換
                    schedule.AssignIdFromName(
                        QueueDefinitionName,
                        () => drive.Queues.Get(folder),
                        e => e.Name!,
                        e => e.Id!,
                        (s, v) => s.QueueDefinitionId = v,
                        this, target, "Queue");
                    #endregion

                    #region TimeZone を TimeZoneId に変換
                    schedule.AssignStringIfNotNullOrEmpty(TimeZoneId, (s, v) => s.TimeZoneId = v);

                    schedule.AssignIdFromName(
                        TimeZone,
                        TimeZoneInfo.GetSystemTimeZones,
                        e => e.DisplayName,
                        e => e.Id!,
                        (s, v) => s.TimeZoneId = v,
                        this, target, "TimeZone");

                    schedule.TimeZoneId ??= TimeZoneInfo.Local.Id;
                    #endregion

                    schedule.AssignDateTimeIfNotNull(StopProcessDate, (s, v) => s.StopProcessDate = v);

                    #region MachineRobots をデシリアライズ
                    if (!string.IsNullOrEmpty(MachineRobots))
                    {
                        var mrss = JsonSerializer.Deserialize<MachineRobotSessionForSerialize[]>(MachineRobots);

                        List<MachineRobotSession> targets = [];

                        var robots = drive.Robots.Get();
                        var machines = drive.Machines.Get();
                        var sessions = drive.GetMachineSessionRuntimesByFolderId(folder);

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
                        schedule.MachineRobots = targets.ToArray();

                        schedule.ExecutorRobots = schedule.MachineRobots?
                            .Select(m => new RobotExecutor() { Id = m.RobotId }).ToArray();
                    }
                    #endregion


                    if (ShouldProcess(target, "Add Trigger"))
                    {
                        try
                        {
                            var created = drive.OrchAPISession.PostProcessSchedule(folder.Id!.Value, schedule);
                            if (created != null)
                            {
                                created.Path = folder.Path;
                                WriteObject(created);
                                drive._dicTriggers?.TryRemove(folder.Id.Value, out _);
                                drive._dicTriggers_Exceptions.ClearCache();
                                drive._dicTriggersDetailed?.TryRemove(folder.Id.Value, out _);
                                drive._dicTriggersDetailed_Exceptions.ClearCache();
                            }
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(target, ex), "AddTriggerError", ErrorCategory.InvalidOperation, folder));
                        }
                    }
                }
            }
        }
    }
}
