using System.Collections;
using UiPath.PowerShell.Positional;
using System.Data;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text.Json;
using UiPath.OrchAPI;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Entities.JsonConverter;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.New, "OrchTrigger", SupportsShouldProcess = true)]
[OutputType(typeof(Entities.ProcessSchedule))]
public class NewTriggerCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(NewTriggerNameCompleter))]
    public string[]? Name { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(ProcessNameCompleter))]
    [SupportsWildcards]
    public string? ReleaseName { get; set; }

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
            var results = ParallelResults.GroupBy(drivesFolders, df => df.drive.GetTriggers(df.folder));

            // Exclude already-selected Names from candidates
            var names = GetSelfExclusionValues(commandAst, parameterName, wordToComplete);

            var entities = results.SelectMany(e => e);
            yield return new CompletionResult(GenerateNewEntityName("NewTrigger", names, entities, e => e.Name!));
        }
    }

    // Display a list of available user names as candidates. Implementation for New-OrchTrigger.
    // For Update-OrchTrigger, displaying the current values would be more user-friendly, so this implementation is not shared.
    private class ExecutorRobotsCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

            // Exclude already-selected ExecutorRobots from candidates
            var wpExecutorRobots = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drivesFolders, df => df.drive.RobotsFromFolder.Get(df.folder));

            foreach (var result in results)
            {
                var (drive, folder) = result.Source;
                var users = drive.GetUsers();

                foreach (var user in result
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

            // Exclude already-selected MachineRobots from candidates
            var wpMachineRobots = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);
            var wp = CreateWPFromWordToComplete(wordToComplete);

            var usersPerFolders = ParallelResults.GroupBy(drivesFolders, df => df.drive.FolderUsersWithInherited.Get(df.folder));
            var robotsPerFolders = ParallelResults.GroupBy(drivesFolders, df => df.drive.FolderMachinesAssigned.Get(df.folder));
            var sessionsPerFolders = ParallelResults.GroupBy(drivesFolders, df => df.drive.MachineSessionRuntimesByFolder.Get(df.folder));

            List<UserRoles?> users = [null];
            users.AddRange(usersPerFolders.SelectMany(g => g));

            List<MachineFolder?> machines = [null];
            machines.AddRange(robotsPerFolders.SelectMany(g => g));

            List<MachineSessionRuntime?> sessions = [null];
            sessions.AddRange(sessionsPerFolders.SelectMany(g => g));

            // Generate and process all combinations
            var combinations = users
                .SelectMany(user => machines, (user, machine) => new { user, machine })
                .SelectMany(pair => sessions, (pair, session) => new { pair.user, pair.machine, session });

            foreach (var c in combinations)
            {
                // Skip if the session's MachineId does not match
                if (c.session is not null && c.machine?.Id != c.session.MachineId) continue;

                // For Dynamic Allocation, user can be omitted,
                // but for machine mapping, user must not be null.
                // Too many candidates would be inconvenient, so exclude all entries where user is null
                //if (c.user is null && c.machine is null) continue;
                if (c.user is null) continue;

                var drive = SessionState.GetOrchDrive(c.user?.Path);
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
        var drivesFolders = SessionState.EnumFolders(Path);
        int? specificPriorityValue = ConvertPriorityToSpecificPriorityValue(Priority);

        Path = Path.Split1stValueByUnescapedCommas()?.ToArray();
        Name = Name.Split1stValueByUnescapedCommas()?.ToArray();
        ExecutorRobots = ExecutorRobots.Split1stValueByUnescapedCommas()?.ToArray();
        // MachineRobots is deserialized by JsonSerializer, so there is no need to split by commas here

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            foreach (var name in Name!.WithCancellation(cancelHandler.Token))
            {
                string target = System.IO.Path.Combine(folder.GetPSPath(), name);

                ProcessSchedule schedule = new()
                {
                    Name = WildcardPattern.Unescape(name)
                };

                schedule.AssignBoolIfNotNull(Enabled, (s, v) => s.Enabled = v);
                schedule.Enabled ??= true;

                schedule.AssignNumberIfNotNullOrZero(StartStrategy, (s, v) => s.StartStrategy = v);
                schedule.StartStrategy ??= 1;

                schedule.AssignStringIfNotNullOrEmpty(StopStrategy, (s, v) => s.StopStrategy = v);
                schedule.AssignStringIfNotNullOrEmpty(StopProcessExpression, (s, v) => s.StopProcessExpression = v);
                schedule.AssignStringIfNotNullOrEmpty(KillProcessExpression, (s, v) => s.KillProcessExpression = v);
                schedule.AssignStringIfNotNullOrEmpty(AlertPendingExpression, (s, v) => s.AlertPendingExpression = v);
                schedule.AssignStringIfNotNullOrEmpty(AlertRunningExpression, (s, v) => s.AlertRunningExpression = v);
                schedule.AssignNumberIfNotNullOrZero(ConsecutiveJobFailuresThreshold, (s, v) => s.ConsecutiveJobFailuresThreshold = v);
                schedule.AssignNumberIfNotNullOrZero(JobFailuresGracePeriodInHours, (s, v) => s.JobFailuresGracePeriodInHours = v);

                #region SpecificPriorityValue
                // Apply SpecificPriorityValue first, then override with specificPriorityValue if non-null
                schedule.AssignNumberIfNotNullOrZero(SpecificPriorityValue, (s, v) => s.SpecificPriorityValue = v);
                schedule.AssignNumberIfNotNullOrZero(specificPriorityValue, (s, v) => s.SpecificPriorityValue = v);
                #endregion

                schedule.AssignStringIfNotNullOrEmpty(RuntimeType, (s, v) => s.RuntimeType = v);
                schedule.RuntimeType ??= "Unattended";

                schedule.AssignStringIfNotNullOrEmpty(InputArguments, (s, v) => schedule.InputArguments = v);
                schedule.InputArguments ??= "{}";

                schedule.AssignBoolIfNotNull(ResumeOnSameContext, (s, v) => s.ResumeOnSameContext = v);
                schedule.ResumeOnSameContext ??= false;

                schedule.AssignBoolIfNotNull(RunAsMe, (s, v) => s.RunAsMe = v);
                schedule.RunAsMe ??= false;

                schedule.AssignBoolIfNotNull(IsConnected, (s, v) => s.IsConnected = v);

                #region // Convert CalendarName to CalendarId
                schedule.AssignIdFromName(
                    CalendarName,
                    drive.GetCalendars!,
                    e => e.Name!,
                    e => e.Id!,
                    (s, v) => s.CalendarId = v,
                    this, target, "Calendar");

                schedule.UseCalendar = (schedule.CalendarId is not null);
                #endregion

                schedule.AssignNumberIfNotNullOrZero(ItemsActivationThreshold, (s, v) => s.ItemsActivationThreshold = v);
                schedule.AssignNumberIfNotNullOrZero(ItemsPerJobActivationTarget, (s, v) => s.ItemsPerJobActivationTarget = v);
                schedule.AssignNumberIfNotNullOrZero(MaxJobsForActivation, (s, v) => s.MaxJobsForActivation = v);
                schedule.AssignBoolIfNotNull(ActivateOnJobComplete, (s, v) => s.ActivateOnJobComplete = v);

                schedule.AssignStringIfNotNullOrEmpty(StartProcessCron, (s, v) => s.StartProcessCron = v);
                schedule.StartProcessCron ??= "0 0/1 * 1/1 * ? *";

                schedule.AssignStringIfNotNullOrEmpty(StartProcessCronDetails, (s, v) => s.StartProcessCronDetails = v);
                schedule.StartProcessCronDetails ??= $"{{\"advancedCron\":{System.Text.Json.JsonSerializer.Serialize(schedule.StartProcessCron ?? "")}}}";

                #region Convert ReleaseName to ReleaseId
                schedule.AssignIdFromName(
                    ReleaseName,
                    () => drive.GetReleases(folder),
                    e => e.Name!,
                    e => e.Id!,
                    (s, v) => s.ReleaseId = v,
                    this, target, "Release");
                #endregion

                #region Convert QueueDefinitionName to QueueDefinitionId
                schedule.AssignIdFromName(
                    QueueDefinitionName,
                    () => drive.Queues.Get(folder),
                    e => e.Name!,
                    e => e.Id!,
                    (s, v) => s.QueueDefinitionId = v,
                    this, target, "Queue");
                #endregion

                #region Convert TimeZone to TimeZoneId
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

                // Deserialize ExecutorRobots
                schedule.ExecutorRobots = DeserializeExecutorRobots(this, drive, folder, schedule.GetPSPath(), ExecutorRobots);

                // Deserialize MachineRobots
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
