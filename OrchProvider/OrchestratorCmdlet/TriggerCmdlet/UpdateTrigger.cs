using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsData.Update, "OrchTrigger", SupportsShouldProcess = true)]
[OutputType(typeof(Entities.ProcessSchedule))]
public class UpdateTriggerCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TriggerNameCompleter<TPositional>))]
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
    [ArgumentCompleter(typeof(CalendarNameCompleter<TPositional>))]
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
    [ArgumentCompleter(typeof(ProcessNameCompleter<TPositional>))]
    [SupportsWildcards]
    public string? ReleaseName { get; set; }

    //QueueDefinitionId
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(QueueNameCompleter<TPositional>))]
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

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    // 現在トリガに設定されているユーザー名を候補に表示。Update-OrchTrigger のための実装。
    // New-OrchTrigger では、利用可能なユーザーをすべて表示する方が使いやすいと思うので、この実装を共有はしない。。
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

            // パラメータで選択済みの Name は、候補から除外する
            var wpExecutorRobots = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

            // 指定の Name のトリガーの ExecutorRobots を取得して表示
            var wpName = CreateWPListFromOtherParameters(commandAst, "Name", TPositional.Parameters);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            foreach (var (drive, folder) in drivesFolders)
            {
                var triggers = drive.GetTriggers(folder)
                    .FilterByWildcards(t => t?.Name, wpName)
                    .OrderBy(t => t.Name);

                var results = ParallelResults.ForEach(triggers, trigger => drive.GetTrigger(folder, trigger));

                foreach (var result in results)
                {
                    if (result.Result is null) continue;
                    if (result.Result.ExecutorRobots is null || result.Result.ExecutorRobots.Length == 0) continue;

                    var executerRobots = SerializeExecutorRobotArray(drive, result.Result.ExecutorRobots);
                    if (wpExecutorRobots is not null && wpExecutorRobots.Any(wpe => wpe.IsMatch(executerRobots))) continue;
                    if (!string.IsNullOrEmpty(executerRobots))
                    {
                        string tooltip = result.Result.GetPSPath();
                        yield return new CompletionResult(PathTools.EscapePSText(executerRobots), executerRobots, CompletionResultType.Text, tooltip);
                    }
                }
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
            var wpName = CreateWPListFromOtherParameters(commandAst, "Name", TPositional.Parameters);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.ForEach(drivesFolders, df => df.drive.GetTriggers(df.folder));

            bool bExists = false;
            foreach (var result in results)
            {
                if (result.Result is null) continue;

                var (drive, folder) = result.Source;

                foreach (var trigger in result.Result
                    .Where(t => t.MachineRobots is not null)
                    .FilterByWildcards(e => e?.Name, wpName)
                    .Where(e => e?.MachineRobots is not null)
                    .OrderBy(a => a.Name))
                {
                    string tiphelp = trigger.GetPSPath();
                    string machineRobots = SerializeMachineRobotSessions(null, drive, folder!, null, trigger.MachineRobots);
                    if (string.IsNullOrEmpty(machineRobots)) continue;

                    bExists = true;
                    yield return new CompletionResult("'" + machineRobots.Replace("'", "''") + "'", machineRobots, CompletionResultType.Text, tiphelp);
                }
            }
            if (!bExists)
            {
                yield return new CompletionResult("'[{\"UserName\":\"\",\"MachineName\":\"\",\"SessionName\":\"\"}]'");
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drivesFolders = OrchDriveInfo.EnumFolders(Path, Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();
        int? specificPriorityValue = ConvertPriorityToSpecificPriorityValue(Priority);

        if (StopProcessDate is not null)
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
            if (triggers is null) continue;

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

                postTrigger.PackageName = null;
                postTrigger.ReleaseKey = null;
                postTrigger.ConsecutiveJobFailuresThreshold = null;
                postTrigger.JobFailuresGracePeriodInHours = null;
                postTrigger.AssignNumberIfNotNullOrZero(ConsecutiveJobFailuresThreshold, (s, v) => s.ConsecutiveJobFailuresThreshold = v);
                postTrigger.AssignNumberIfNotNullOrZero(JobFailuresGracePeriodInHours,   (s, v) => s.JobFailuresGracePeriodInHours = v);

                postTrigger.AssignStringIfNotNullOrEmpty(RuntimeType,       (s, v) => s.RuntimeType = v);
                postTrigger.RuntimeType ??= "Unattended";

                postTrigger.InputArguments ??= "{}";
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

                postTrigger.UseCalendar = (postTrigger.CalendarId is not null);
                #endregion

                postTrigger.ExternalJobKeyScheduler = null;

                postTrigger.ItemsActivationThreshold = null;
                postTrigger.ItemsPerJobActivationTarget = null;
                postTrigger.AssignNumberIfNotNullOrZero(ItemsActivationThreshold,    (s, v) => s.ItemsActivationThreshold = v);
                postTrigger.AssignNumberIfNotNullOrZero(ItemsPerJobActivationTarget, (s, v) => s.ItemsPerJobActivationTarget = v);
                postTrigger.MaxJobsForActivation = null;
                postTrigger.AssignNumberIfNotNullOrZero(MaxJobsForActivation,        (s, v) => s.MaxJobsForActivation = v);
                postTrigger.AssignBoolIfNotNull(ActivateOnJobComplete,         (s, v) => s.ActivateOnJobComplete = v);

                postTrigger.StartProcessCronSummary = null;
                postTrigger.StartProcessNextOccurrence = null;
                postTrigger.AssignStringIfNotNullOrEmpty(StartProcessCron,            (s, v) => s.StartProcessCron = v);
                postTrigger.StartProcessCron ??= "0 0/1 * 1/1 * ? *";

                postTrigger.AssignStringIfNotNullOrEmpty(StartProcessCronDetails,     (s, v) => s.StartProcessCronDetails = v);
                postTrigger.StartProcessCronDetails ??= $"\"{{advancedCron\":\"{postTrigger.StartProcessCron}\"}}";

                // SpecificPriorityValue を先に適用、specificPriorityValue が非 null ならそれで上書き
                postTrigger.AssignNumberIfNotNullOrZero(SpecificPriorityValue, (s, v) => s.SpecificPriorityValue = v);
                postTrigger.AssignNumberIfNotNullOrZero(specificPriorityValue, (s, v) => s.SpecificPriorityValue = v);

                if (postTrigger.SpecificPriorityValue is not null)
                {
                    postTrigger.JobPriority = null;
                }

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

                if (MachineRobots is not null)
                {
                    postTrigger.MachineRobots = DeserializeMachineRobotSessions(this, drive, folder, postTrigger.GetPSPath(), MachineRobots);
                }

                if (ExecutorRobots is not null)
                {
                    postTrigger.ExecutorRobots = DeserializeExecutorRobots(this, drive, folder, postTrigger.GetPSPath(), ExecutorRobots);
                }

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
