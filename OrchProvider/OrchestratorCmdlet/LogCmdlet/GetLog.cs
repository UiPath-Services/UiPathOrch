using System.Collections;
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Reflection.PortableExecutable;
using System.Security.AccessControl;
using System.Security.Principal;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using UiPath.PowerShell.Positional;
using Job = UiPath.PowerShell.Entities.Job;
using JobState = UiPath.PowerShell.Entities.JobState;

using LastItems = UiPath.PowerShell.Positional.Hour_Day_Week_Month_3Month_6Month_Year_3Year;
using Positional = UiPath.PowerShell.Positional.Id_Level;
using System.Diagnostics.Eventing.Reader;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "OrchLog")]
    [OutputType(typeof(Log))]
    public class GetLogCommand : OrchestratorPSCmdlet
    {
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(IdCompleter))]
        public Int64[]? JobId { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(StaticTextsCompleter<LastItems>))]
        public string? Last { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(TimeAfterCompleter))]
        public DateTime? TimeStampAfter { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(TimeBeforeCompleter))]
        public DateTime? TimeStampBefore { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(LevelCompleter))]
        public string? Level { get; set; }

        // OC API の制限？不具合？で、複数のマシンを指定したフィルターが機能しない。
        // 本当は、配列にしてワイルドカードもサポートしたいが、動かない。
        // そのため、配列での指定はできないようにしておく。
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(MachineCompleter))]
        public string? Machine { get; set; }

        // OC API の制限？不具合？で、複数のプロセスを指定したフィルターが機能しない。
        // "((ProcessName eq 'OpenAvidemux') or (ProcessName eq 'OrchestratorManager'))" とか。
        // 本当は、配列にしてワイルドカードもサポートしたいが、動かない。
        // そのため、配列での指定はできないようにしておく。
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(ListReleasesCompleter<Id_Level>))]
        public string? ProcessName { get; set; }

        // なぜか、これは複数の Identity を or で連結したフィルターが動作する。
        // ワイルドカードをサポートできるのでうれしい。
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(WindowsIdentityCompleter))]
        [SupportsWildcards]
        public string[]? WindowsIdentity { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        public ulong? Skip { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(StaticTextsCompleter<Item10>))]
        public ulong? First { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(StaticTextsCompleter<LogOrderableItems>))]
        public string? OrderBy { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        public SwitchParameter OrderAscending { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string[]? Path { get; set; }

        [Parameter]
        public SwitchParameter Recurse { get; set; }

        [Parameter]
        public uint Depth { get; set; }

        private class IdCompleter : OrchArgumentCompleter
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
                var paramId = GetParameterValues(commandAst, "Id", Id_Level.Parameters, wordToComplete).Select(id => long.Parse(id));

                // パラメータで選択された State のみ対象とする
                var wpState = CreateWPListFromOtherParameters(commandAst, "State", Id_Level.Parameters);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                // キャッシュを検索するだけなので、マルチスレッド化は不要
                foreach (var (drive, folder) in drivesFolders)
                {
                    if (drive._dicJobs == null && drive._dicJobsHavingExecutionMedia == null)
                        continue;

                    if (drive._dicJobs != null && drive._dicJobs!.TryGetValue(folder.Id ?? 0, out Dictionary<Int64, Job>? folderJobs))
                    {
                        foreach (var job in folderJobs.Values
                            .ExcludeByStructValues(job => job.Id ?? 0, paramId)
                            .FilterByWildcards(job => job?.State, wpState)
                            .Where(job => wp.IsMatch((job.Id ?? 0).ToString())))
                        {
                            string tiphelp = $"{job.Id} C{job.CreationTime?.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss")}";
                            if (job.StartTime != null)
                                tiphelp += $"  S{job.StartTime?.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss").ToString()}";
                            else
                                tiphelp += $"                      ";
                            if (job.EndTime != null)
                                tiphelp += $"  E{job.EndTime?.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss").ToString()}";
                            else
                                tiphelp += $"                      ";
                            tiphelp += $" {job.State,11} {job.ReleaseName}";

                            yield return new CompletionResult(job.Id.ToString(), job.Id.ToString(), CompletionResultType.ParameterValue, tiphelp);
                        }
                    }

                    if (drive._dicJobsHavingExecutionMedia != null && drive._dicJobsHavingExecutionMedia.TryGetValue(folder.Id ?? 0, out var folderMedia))
                    {
                        foreach (var media in folderMedia
                            .ExcludeByStructValues<ExecutionMedia, Int64>(job => job.Id ?? 0, paramId)
                            //.FilterByWildcards(job => job.State!, wpState)
                            .Where(job => wp.IsMatch((job.Id ?? 0).ToString()))
                            )
                        {
                            string tiphelp = $"{media.JobId}";
                            yield return new CompletionResult(media.JobId.ToString(), media.Id.ToString(), CompletionResultType.ParameterValue, tiphelp);
                        }
                    }
                }
            }
        }

        private class LevelCompleter : OrchArgumentCompleter
        {
            public override IEnumerable<CompletionResult> CompleteArgument(
                string commandName,
                string parameterName,
                string wordToComplete,
                CommandAst commandAst,
                IDictionary fakeBoundParameters)
            {
                var wp = CreateWPFromWordToComplete(wordToComplete);

                // TODO: need to add "Debug" ?
                var candidates = new Dictionary<string, string>
                {
                    { "Trace", "Fatal + Error + Warn + Info + Trace (All)" },
                    { "Info",  "Fatal + Error + Warn + Info" },
                    { "Warn",  "Fatal + Error + Warn" },
                    { "Error", "Fatal + Error" },
                    { "Fatal", "Fatal Only" }
                };

                foreach (var candidate in candidates.Where(c => wp.IsMatch(c.Key)))
                {
                    yield return new CompletionResult(candidate.Key, candidate.Key, CompletionResultType.ParameterValue, candidate.Value);
                }
            }
        }

        private class MachineCompleter : OrchArgumentCompleter
        {
            public override IEnumerable<CompletionResult> CompleteArgument(
                string commandName,
                string parameterName,
                string wordToComplete,
                CommandAst commandAst,
                IDictionary fakeBoundParameters)
            {
                var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

                // パラメータで選択済みの Machine は、候補から除外する
                var wpName = CreateWPListFromParameter(commandAst, parameterName, Id_Level.Parameters, wordToComplete);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drivesFolders, df => df.drive.GetAssignedMachines(df.folder));

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;

                    foreach (var e in entities!
                        .Where(q => wp.IsMatch(q.Name))
                        .ExcludeByWildcards(q => q?.Name, wpName)
                        .OrderBy(q => q.Name))
                    {
                        //string tiphelp = TipHelp(e);
                        yield return new CompletionResult(PathTools.EscapePSText(e.Name), e.Name, CompletionResultType.ParameterValue, e.Name);
                    }
                }
            }
        }

        private class WindowsIdentityCompleter : OrchArgumentCompleter
        {
            public override IEnumerable<CompletionResult> CompleteArgument(
                string commandName,
                string parameterName,
                string wordToComplete,
                CommandAst commandAst,
                IDictionary fakeBoundParameters)
            {
                var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

                // パラメータで選択済みの Machine は、候補から除外する
                var wpWindowsIdentity = CreateWPListFromParameter(commandAst, parameterName, Id_Level.Parameters, wordToComplete);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drivesFolders, df => df.drive.GetUserRobots(df.folder));

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;

                    foreach (var e in entities!
                        .Where(e => !string.IsNullOrEmpty(e.UserName))
                        .Where(e => wp.IsMatch(e.UserName))
                        .ExcludeByWildcards(e => e?.UserName, wpWindowsIdentity)
                        .OrderBy(q => q.UserName))
                    {
                        //string tiphelp = TipHelp(e);
                        yield return new CompletionResult(PathTools.EscapePSText(e.UserName), e.UserName, CompletionResultType.ParameterValue, e.UserName);
                    }
                }
            }
        }

        private string? MakeFilter(OrchDriveInfo drive, Folder folder, string? jobKey = null)
        {
            List<string> filter = [];

            #region ProcessName
            if (!string.IsNullOrEmpty(ProcessName))
            {
                var releases = drive.GetReleases(folder);
                var targetReleases = releases.Where(r => string.Compare(r?.Name, ProcessName, true) == 0);
                if (!targetReleases.Any()) return "null";
                filter.AddIfNotNull(targetReleases
                    //.SelectByWildcards(r => r?.Name, [ReleaseName])
                    //.Where(r => string.Compare(r?.Name, WildcardPattern.Unescape(ReleaseName), StringComparison.OrdinalIgnoreCase) == 0)
                    .CreateOrFilter(r => $"ProcessName eq '{r.Name}'"));
            }
            #endregion

            #region JobKey
            if (jobKey != null)
            {
                filter.Add($"(JobKey eq {jobKey})");
            }
            #endregion

            #region Last
            if (Last != null)
            {
                var last = Last.ToLower() switch
                {
                    "hour" => DateTime.UtcNow.AddHours(-1),
                    "day" => DateTime.UtcNow.AddDays(-1),
                    "week" => DateTime.UtcNow.AddDays(-7),
                    "month" => DateTime.UtcNow.AddMonths(-1),
                    "3months" => DateTime.UtcNow.AddMonths(-3),
                    "6months" => DateTime.UtcNow.AddMonths(-6),
                    "year" => DateTime.UtcNow.AddYears(-1),
                    "3years" => DateTime.UtcNow.AddYears(-3),
                    _ => throw new ArgumentException("Invalid Last parameter. Valid values are 'Hour', 'Day', 'Week', 'Month', '3Months', '6Months', 'Year', '3Years'.")
                };
                filter.Add($"(TimeStamp ge {last:yyyy-MM-ddTHH:mm:ss.fffZ})");
            }
            #endregion

            #region Level
            Level ??= "Info";
            LogLevel logLevel = (LogLevel)Enum.Parse(typeof(LogLevel), Level);
            filter.Add($"(Level ge '{(int)logLevel}')");
            #endregion

            #region State
            // do nothing
            // State is used for only narrowing Id completer candidates.
            #endregion

            #region TimeStampAfter
            if (TimeStampAfter != null)
            {
                filter.Add($"(TimeStamp ge {TimeStampAfter.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ})");
            }
            #endregion

            #region TimeStampBefore
            if (TimeStampBefore != null)
            {
                filter.Add($"(TimeStamp lt {TimeStampBefore.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ})");
            }
            #endregion

            #region Machine
            if (!string.IsNullOrEmpty(Machine))
            {
                var machines = drive.GetAssignedMachines(folder);
                filter.AddIfNotNull(machines
                    //.SelectByWildcards(m => m?.Name, Machine)
                    .Where(m => string.Compare(m?.Name, WildcardPattern.Unescape(Machine), StringComparison.OrdinalIgnoreCase) == 0)
                    .CreateOrFilter(m => $"MachineKey eq {m.Key}"));
            }
            #endregion

            #region HostIdentity
            if (WindowsIdentity != null)
            {
                var userRobots = drive.GetUserRobots(folder);
                filter.AddIfNotNull(userRobots
                    .SelectByWildcards(u => u?.UserName, WindowsIdentity)
                    .Where(u => u.RobotNames != null)
                    .SelectMany(u => u?.RobotNames!)
                    .Where(r => !string.IsNullOrEmpty(r))
                    .CreateOrFilter(r => $"RobotName eq '{r}'"));
            }
            #endregion

            string ret = filter.CreateAndFilter(f => f);
            ret = "&$filter=(" + ret + ")";
            return ret;
        }

        protected override void ProcessRecord()
        {
            ulong skip = Skip ?? 0;
            ulong first = First ?? ulong.MaxValue;

            if (string.IsNullOrEmpty(OrderBy)) OrderBy = "TimeStamp";

            // すべてのパラメータが指定されていなければ、キャッシュの内容を返す
            bool bOutCache = (
                JobId == null &&
                Last == null &&
                TimeStampAfter == null &&
                TimeStampBefore == null &&
                Level == null &&
                Machine == null &&
                ProcessName == null &&
                WindowsIdentity == null &&
                Skip == null && First == null);

            var drivesFolders = OrchDriveInfo.EnumFolders(Path, Recurse.IsPresent, Depth);

            if (bOutCache)
            {
                WriteWarning("Since no filter parameters were specified, the contents of the cache will be output. To query the Orchestrator, please specify at least one filter parameter.");

                foreach (var (drive, folder) in drivesFolders)
                {
                    if (drive._dicRobotLogs?.TryGetValue(folder.Id!.Value, out var logs) ?? false)
                    {
                        switch (OrderBy)
                        {
                            case "TimeStamp":
                                if (OrderAscending.IsPresent)
                                    WriteObject(logs.OrderBy(l => l.TimeStamp), true);
                                else
                                    WriteObject(logs.OrderByDescending(l => l.TimeStamp), true);
                                break;
                            case "Level":
                                if (OrderAscending.IsPresent)
                                    WriteObject(logs.OrderBy(l => l.Level), true);
                                else
                                    WriteObject(logs.OrderByDescending(l => l.Level), true);
                                break;
                        }
                    }
                }
                return;
            }

            if (JobId != null && JobId.Length != 0)
            {
                foreach (var (drive, folder) in drivesFolders)
                {

                    foreach (var id in JobId!)
                    {
                        string? jobKey = null;
                        // キャッシュから Job.Key を取得
                        if (drive._dicJobs?.TryGetValue(folder.Id ?? 0, out var jobs) ?? false)
                        {
                            if (jobs.TryGetValue(id, out var job))
                            {
                                jobKey = job.Key;
                            }
                        }
                        // キャッシュがなければ、Job を問い合わせる
                        if (jobKey == null)
                        {
                            Job job;
                            try
                            {
                                job = drive.GetJob(folder, id);
                                jobKey = job?.Key;
                            }
                            catch (Exception ex)
                            {
                                WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "GetJobError", ErrorCategory.InvalidOperation, folder));
                                continue;
                            }
                            if (job == null)
                            {
                                WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), $"No job with Id = {id} found."), "GetJobError", ErrorCategory.InvalidOperation, folder));
                                continue;
                            }
                            if (jobKey == null)
                            {
                                WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), $"Job with Id = {id} has no Key."), "GetJobError", ErrorCategory.InvalidOperation, folder));
                                continue;
                            }
                        }

                        try
                        {
                            string query = MakeFilter(drive, folder, jobKey);
                            var logs = drive.GetRobotLogs(folder, query, skip, first, OrderBy, OrderAscending.IsPresent);
                            WriteObject(logs, true);
                        }
                        catch (Exception ex)
                        {
                            string target = System.IO.Path.Combine(folder.GetPSPath(), id.ToString());
                            WriteError(new ErrorRecord(new OrchException(target, ex), "GetLogError", ErrorCategory.InvalidOperation, folder));
                        }
                    }
                }
                return;
            }

            string msg = $"Get Log";
            using ProgressReporter reporter = new(this, 1, drivesFolders.Count, msg, msg);
            int index = 0;
            foreach (var (drive, folder) in drivesFolders)
            {
                reporter.WriteProgress(++index, $"{index:D}/{drivesFolders.Count} {folder.GetPSPath()}");
                try
                {
                    string query = MakeFilter(drive, folder);
                    if (query == "null") continue;

                    var first2 = ulong.Min(10000, first);
                    var logs = drive.GetRobotLogs(folder, query, skip, first2, OrderBy, OrderAscending.IsPresent);
                    WriteObject(logs, true);

                    first -= first2;
                    while (logs.Count == 10000)
                    {
                        var lastLog = logs.LastOrDefault();
                        if (lastLog == null) continue;

                        first2 = ulong.Min(10000, first);
                        if (OrderBy == "TimeStamp")
                        {
                            if (OrderAscending.IsPresent)
                                TimeStampAfter = lastLog.TimeStamp?.ToLocalTime();
                            else
                                TimeStampBefore = lastLog.TimeStamp?.ToLocalTime();
                        }
                        else if (OrderBy == "Level")
                        {
                            WriteError(new ErrorRecord(
                                new InvalidOperationException("When 'Level' is specified in the -OrderBy parameter, logs exceeding 10,000 rows cannot be retrieved."),
                                "LogRetrievalLimitExceeded",
                                ErrorCategory.InvalidOperation,
                                null
                            ));

                            break;
                        }

                        query = MakeFilter(drive, folder);
                        logs = drive.GetRobotLogs(folder, query, 0, first2, OrderBy, OrderAscending.IsPresent);
                        WriteObject(logs, true);
                        first -= first2;
                    }
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "GetLogError", ErrorCategory.InvalidOperation, folder));
                }
            }
        }
    }
}
