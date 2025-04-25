using System.Collections;
using System.Data;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text.Json;
using UiPath.OrchAPI;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Entities.JsonConverter;
using UiPath.PowerShell.Positional;
using TPositional = UiPath.PowerShell.Positional.Name_ReleaseName;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.New, "OrchTrigger", SupportsShouldProcess = true)]
[OutputType(typeof(Entities.ProcessSchedule))]
public class NewTriggerCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(NewTriggerNameCompleter))]
    public string[]? Name { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(ProcessNameCompleter<TPositional>))]
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
    [ArgumentCompleter(typeof(ExecutorRobotsCompleter<TPositional>))]
    public string[]? ExecutorRobots { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(MachineRobotsCompleter))]
    public string[]? MachineRobots { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    //[Parameter]
    //public SwitchParameter Recurse { get; set; }

    //[Parameter]
    //public uint Depth { get; set; }

    private class NewTriggerNameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);
            var results = ParallelResults.ForEach(drivesFolders, df => df.drive.GetTriggers(df.folder));

            // パラメータで選択済みの Name は、候補から除外する
            var names = GetParameterValues(commandAst, parameterName, TPositional.Parameters, wordToComplete);

            var entities = results.SelectMany(e => e.Result ?? []);
            yield return new CompletionResult(GenerateNewEntityName("NewTrigger", names, entities, e => e.Name!));
        }
    }

    // 利用可能なユーザー名一覧を候補に表示。New-OrchTrigger のための実装。
    // Update-OrchTrigger では、現在の内容を表示する方が使いやすいと思うので、この実装を共有はしない。。
    private class ExecutorRobotsCompleter<TPositional> : OrchArgumentCompleter where TPositional : IPositionalParameters
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

            // パラメータで選択済みの ExecutorRobots は、候補から除外する
            var wpExecutorRobots = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.ForEach(drivesFolders, df => df.drive.RobotsFromFolder.Get(df.folder));

            foreach (var result in results)
            {
                if (result.Result is null) continue;

                var (drive, folder) = result.Source;
                var users = drive.GetUsers();

                foreach (var user in result.Result
                    .Where(e => e.Type == "Unattended")
                    .Select(e => users.FirstOrDefault(u => u.Id == e.UserId))
                    .Where(u => !string.IsNullOrEmpty(u?.UnattendedRobot?.UserName))
                    .Where(u => wp.IsMatch(u?.UnattendedRobot?.UserName))
                    .FilterByWildcards(u => u?.UnattendedRobot?.UserName, wpExecutorRobots))
                {
                    string tooltip = user!.GetPSPath();
                    yield return new CompletionResult(PathTools.EscapePSText(user!.UnattendedRobot!.UserName), user.UnattendedRobot.UserName, CompletionResultType.Text, tooltip);
                }
            }
        }
    }

    private class MachineRobotsCompleter : OrchArgumentCompleter
    {
        private static string MakeCandidateText(Entities.User? user, MachineFolder? machine, MachineSessionRuntime? session)
        {
            string sessionName = session?.HostMachineName;
            if (!string.IsNullOrEmpty(session?.ServiceUserName))
            {
                sessionName += (" - " + session.ServiceUserName);
            }

            MachineRobotSessionForSerialize ret = new()
            {
                UserName = user?.UnattendedRobot?.UserName,
                MachineName = machine?.Name,
                SessionName = sessionName
            };

            return JsonSerializer.Serialize(ret, JsonTools.jsoOneLine);
        }

        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

            // パラメータで選択済みの MachineRobots は、候補から除外する
            var wpMachineRobots = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);
            var wp = CreateWPFromWordToComplete(wordToComplete);

            var usersPerFolders = ParallelResults.ForEach(drivesFolders, df => df.drive.FolderUsersWithInherited.Get(df.folder));
            var robotsPerFolders = ParallelResults.ForEach(drivesFolders, df => df.drive.FolderMachinesAssigned.Get(df.folder));
            var sessionsPerFolders = ParallelResults.ForEach(drivesFolders, df => df.drive.MachineSessionRuntimesByFolder.Get(df.folder));

            List<UserRoles> users = [null];
            foreach (var usersPerFolder in usersPerFolders)
            {
                if (usersPerFolder.Result is null) continue;
                users.AddRange(usersPerFolder.Result);
            }

            List<MachineFolder> machines = [null];
            foreach (var robotsPerFolder in robotsPerFolders)
            {
                if (robotsPerFolder.Result is null) continue;
                machines.AddRange(robotsPerFolder.Result);
            }

            List<MachineSessionRuntime> sessions = [null];
            foreach (var sessionsPerFolder in sessionsPerFolders)
            {
                if (sessionsPerFolder.Result is null) continue;
                sessions.AddRange(sessionsPerFolder.Result);
            }

            // すべての組み合わせを生成して処理
            var combinations = users
                .SelectMany(user => machines, (user, machine) => new { user, machine })
                .SelectMany(pair => sessions, (pair, session) => new { pair.user, pair.machine, session });

            foreach (var c in combinations)
            {
                // セッションの MachineId が不一致ならスキップ
                if (c.session is not null && c.machine?.Id != c.session.MachineId) continue;

                // Dynamic Allocation の場合には user を指定せずも可能だが、
                // machine mapping の場合には user は null ではいけない。
                // 候補が多すぎても不便だし、user が null のものはすべて候補から外す
                //if (c.user is null && c.machine is null) continue;
                if (c.user is null) continue;

                var drive = OrchDriveInfo.GetOrchDrive(c.user?.Path);
                var targetUser = drive.GetUsers().Where(u => u.Id == c.user?.Id).FirstOrDefault();
                if (targetUser?.UnattendedRobot?.UserName is null) continue;

                string text = MakeCandidateText(targetUser, c.machine, c.session);

                if (!wp.IsMatch(text)) continue;
                if (wpMachineRobots is not null && wpMachineRobots.Any(wpm => wpm.IsMatch(text))) continue;

                yield return new CompletionResult(PathTools.EscapePSText(text), text, CompletionResultType.Text, text);
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drivesFolders = OrchDriveInfo.EnumFolders(Path);
        int? specificPriorityValue = ConvertPriorityToSpecificPriorityValue(Priority);

        Path = Path.Split1stValueByUnescapedCommas()?.ToArray();
        Name = Name.Split1stValueByUnescapedCommas()?.ToArray();
        ExecutorRobots = ExecutorRobots.Split1stValueByUnescapedCommas()?.ToArray();
        // MachineRobots は JsonSerializer でデシリアライズするので、ここでカンマで区切る必要はない

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

                schedule.UseCalendar = (schedule.CalendarId is not null);
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

                // ExecutorRobots をデシリアライズ
                schedule.ExecutorRobots = DeserializeExecutorRobots(this, drive, folder, schedule.GetPSPath(), ExecutorRobots);

                // MachineRobots をデシリアライズ
                schedule.MachineRobots = DeserializeMachineRobotSessions(this, drive, folder, schedule.GetPSPath(), MachineRobots);

                if (ShouldProcess(target, "New Trigger"))
                {
                    try
                    {
                        var created = drive.OrchAPISession.PostProcessSchedule(folder.Id!.Value, schedule);
                        if (created is not null)
                        {
                            created.Path = folder.GetPSPath();
                            WriteObject(created);
                            drive._dicTriggers?.TryRemove(folder.Id.Value, out _);
                            drive._dicTriggers_Exceptions.ClearCache();
                            drive._dicTriggersDetailed?.TryRemove(folder.Id.Value, out _);
                            drive._dicTriggersDetailed_Exceptions.ClearCache();
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(target, ex), "NewTriggerError", ErrorCategory.InvalidOperation, folder));
                    }
                }
            }
        }
    }
}
