using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;
using TPositional = UiPath.PowerShell.Positional.Id;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchJob", DefaultParameterSetName = "JobId")] //, SupportsPaging = true)]
[OutputType(typeof(Entities.Job))]
public class GetJobCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ParameterSetName = "JobId", ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(IdCompleter))]
    public Int64[]? Id { get; set; }

    [Parameter(ParameterSetName = "Filter", ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<Hour_Day_Week_Month_3Month_6Month_Year_3Year>))]
    [ValidatePositionalParameter<Hour_Day_Week_Month_3Month_6Month_Year_3Year>]
    public string? Last { get; set; }

    [Parameter(ParameterSetName = "Filter", ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TimeAfterCompleter))]
    public DateTime? CreationTimeAfter { get; set; }

    [Parameter(ParameterSetName = "Filter", ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TimeBeforeCompleter))]
    public DateTime? CreationTimeBefore { get; set; }

    [Parameter(ParameterSetName = "Filter", ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TimeAfterCompleter))]
    public DateTime? StartTimeAfter { get; set; }

    [Parameter(ParameterSetName = "Filter", ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TimeBeforeCompleter))]
    public DateTime? StartTimeBefore { get; set; }

    [Parameter(ParameterSetName = "Filter", ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TimeAfterCompleter))]
    public DateTime? EndTimeAfter { get; set; }

    [Parameter(ParameterSetName = "Filter", ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TimeBeforeCompleter))]
    public DateTime? EndTimeBefore { get; set; }

    [Parameter(ParameterSetName = "Filter", ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TimeAfterCompleter))]
    public DateTime? ResumeTimeAfter { get; set; }

    [Parameter(ParameterSetName = "Filter", ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TimeBeforeCompleter))]
    public DateTime? ResumeTimeBefore { get; set; }

    [Parameter(ParameterSetName = "Filter", ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PriorityCompleter))]
    public string? Priority { get; set; }

    [Parameter(ParameterSetName = "Filter", ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(ListReleasesCompleter<Id>))]
    [SupportsWildcards]
    public string[]? ReleaseName { get; set; }

    [Parameter(ParameterSetName = "Filter", ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(KeyOfDictionaryCompleter<JobSourceTypeItems, int>))]
    [SupportsWildcards]
    public string[]? SourceType { get; set; }

    [Parameter(ParameterSetName = "Filter", ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(KeyOfDictionaryCompleter<JobStateItems, int>))]
    [ValidateDictionaryKey<JobStateItems, int>]
    public string[]? State { get; set; }

    [Parameter(ParameterSetName = "Filter", ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<JobProcessTypeItems>))]
    [SupportsWildcards]
    public string[]? ProcessType { get; set; }

    [Parameter(ParameterSetName = "Filter", ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(RobotCompleter))]
    [SupportsWildcards]
    public string[]? Robot { get; set; }

    [Parameter(ParameterSetName = "Filter", ValueFromPipelineByPropertyName = true)]
    public ulong? Skip { get; set; }

    [Parameter(ParameterSetName = "Filter", ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<JobOrderableItems>))]
    public string? OrderBy { get; set; }

    [Parameter(ParameterSetName = "Filter")]
    public SwitchParameter OrderAscending { get; set; }

    [Parameter(ParameterSetName = "Filter", ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<Item10>))]
    public ulong? First { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
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
            var paramId = GetParameterValues(commandAst, parameterName, TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            foreach (var (drive, folder) in drivesFolders)
            {
                var jobs = drive.Jobs.GetCache(folder);
                if (jobs is null) continue;

                foreach (var job in jobs.Values.ExcludeByClassValues(j => (j?.Id ?? 0).ToString(), paramId))
                {
                    if (!wp.IsMatch((job.Id ?? 0).ToString()))
                        continue;

                    yield return new CompletionResult(job.Id.ToString(), job.Id.ToString(), CompletionResultType.ParameterValue, job.FormatTooltip());
                }
            }
        }
    }

    // TODO: StaticTextCompleter で書き直す
    // あるいは KeyOfDictionaryCompleter を使うか。
    private class PriorityCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var wp = CreateWPFromWordToComplete(wordToComplete);

            string[] candidates = {
                "Critical",
                "Highest",
                "VeryHigh",
                "High",
                "MediumHigh",
                "Medium",
                "MediumLow",
                "Low",
                "VeryLow",
                "Lowest",
            };

            foreach (var candidate in candidates.Where(c => wp.IsMatch(c)))
            {
                yield return new CompletionResult(candidate);
            }
        }
    }


    private class RobotCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drives = ResolveOrchDrives(fakeBoundParameters);

            var wpRobot = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults3.GroupBy(drives, drive => drive.Robots.Get());

            foreach (var result in results)
            {
                foreach (var robot in result
                    .Where(r => wp.IsMatch(r.Name))
                    .ExcludeByWildcards(r => r?.Name, wpRobot)
                    .OrderBy(r => r.Name))
                {
                    string tiphelp = robot.GetPSPath();
                    yield return new CompletionResult(PathTools.EscapePSText(robot.Name), robot.Name, CompletionResultType.ParameterValue, tiphelp);
                }
            }
        }
    }

    private string? MakeFilter(OrchDriveInfo drive, Folder folder)
    {
        List<string> filter = [];

        #region ReleaseName
        if (ReleaseName is not null && ReleaseName.Length > 0 && !ReleaseName.Any(p => p == "*"))
        {
            var wpProcess = ReleaseName.ConvertToWildcardPatternList();
            var releases = drive.GetReleases(folder);
            var targetReleases = releases.SelectByWildcards(r => r?.Name, wpProcess);
            if (!targetReleases.Any()) return "null";
            var processes = new List<string>();
            var visitedProcessIds = new HashSet<Int64>();
            foreach (var process in targetReleases)
            {
                if (visitedProcessIds.Add(process.Id ?? 0))
                {
                    processes.Add($"(Release/Id eq {process.Id})");
                }
            }
            if (processes.Count != 0)
                filter.Add("(" + string.Join(" or ", processes) + ")");
        }
        #endregion

        #region Last
        if (Last is not null)
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
            filter.Add($"(CreationTime ge {last:yyyy-MM-ddTHH:mm:ss.fffZ})");
        }
        #endregion

        #region CreationTimeAfter
        if (CreationTimeAfter is not null)
        {
            filter.Add($"(CreationTime ge {CreationTimeAfter.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ})");
        }
        #endregion

        #region CreationTimeBefore
        if (CreationTimeBefore is not null)
        {
            filter.Add($"(CreationTime lt {CreationTimeBefore.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ})");
        }
        #endregion

        #region StartTimeAfter
        if (StartTimeAfter is not null)
        {
            filter.Add($"(StartTime ge {StartTimeAfter.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ})");
        }
        #endregion

        #region StartTimeBefore
        if (StartTimeBefore is not null)
        {
            filter.Add($"(StartTime lt {StartTimeBefore.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ})");
        }
        #endregion

        #region EndTimeAfter
        if (EndTimeAfter is not null)
        {
            filter.Add($"(EndTime ge {EndTimeAfter.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ})");
        }
        #endregion

        #region EndTimeBefore
        if (EndTimeBefore is not null)
        {
            filter.Add($"(EndTime lt {EndTimeBefore.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ})");
        }
        #endregion

        #region ResumeTimeAfter
        if (ResumeTimeAfter is not null)
        {
            filter.Add($"(ResumeTime ge {ResumeTimeAfter.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ})");
        }
        #endregion

        #region ResumeTimeBefore
        if (ResumeTimeBefore is not null)
        {
            filter.Add($"(ResumeTime lt {ResumeTimeBefore.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ})");
        }
        #endregion

        #region Priority
        if (Priority is not null)
        {
            Priority = Priority.ToLower();
            switch (Priority)
            {
                case "critical":
                    filter.Add("(SpecificPriorityValue ge 91) and (SpecificPriorityValue le 100)");
                    break;
                case "highest":
                    filter.Add("(SpecificPriorityValue ge 81) and (SpecificPriorityValue le 90)");
                    break;
                case "veryhigh":
                    filter.Add("(SpecificPriorityValue ge 71) and (SpecificPriorityValue le 80)");
                    break;
                case "high":
                    filter.Add("(SpecificPriorityValue ge 61) and (SpecificPriorityValue le 70)");
                    break;
                case "mediumhigh":
                    filter.Add("(SpecificPriorityValue ge 51) and (SpecificPriorityValue le 60)");
                    break;
                case "medium":
                    filter.Add("(SpecificPriorityValue ge 41) and (SpecificPriorityValue le 50)");
                    break;
                case "mediumlow":
                    filter.Add("(SpecificPriorityValue ge 31) and (SpecificPriorityValue le 40)");
                    break;
                case "low":
                    filter.Add("(SpecificPriorityValue ge 21) and (SpecificPriorityValue le 30)");
                    break;
                case "verylow":
                    filter.Add("(SpecificPriorityValue ge 11) and (SpecificPriorityValue le 20)");
                    break;
                case "lowest":
                    filter.Add("(SpecificPriorityValue ge 1) and (SpecificPriorityValue le 10)");
                    break;
            }
        }
        #endregion

        #region ProcessType
        if (ProcessType is null) ProcessType = ["Process"];
        filter.AddIfNotNull(JobProcessTypeItems.Parameters
            .SelectByWildcards(i => i, ProcessType)
            .CreateOrFilter(i => $"ProcessType eq '{i}'"));
        #endregion

        #region Robot
        if (Robot is not null && Robot.Length > 0 && !Robot.Any(r => r == "*"))
        {
            var wpRobot= Robot.ConvertToWildcardPatternList();
            var robots = drive.Robots.Get();
            var targetRobots = robots.SelectByWildcards(r => r?.Name, wpRobot);
            if (!targetRobots.Any()) return "null";
            filter.AddIfNotNull(targetRobots.Select(r => r.Id).CreateOrFilter(i => $"Robot/Id eq {i}"));
        }
        #endregion

        filter.AddIfNotNull(JobSourceTypeItems.Items
            .SelectByWildcards(i => i.Key, SourceType)
            .CreateOrFilter(i => $"SourceType eq '{i.Value}'"));

        filter.AddIfNotNull(JobStateItems.Items
            .SelectByWildcards(i => i.Key, State)
            .CreateOrFilter(i => $"State eq '{i.Value}'"));

        string ret = string.Join(" and ", filter);
        return "&$filter=(" + ret + ")";
    }

    protected override void ProcessRecord()
    {
        ulong skip = Skip ?? 0;
        ulong first = First ?? ulong.MaxValue;

        var drivesFolders = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth);

        if (string.IsNullOrEmpty(OrderBy)) OrderBy = "CreationTime";

        // すべてのパラメータが指定されていなければ、キャッシュの内容を返す
        bool bOutCache = (
            Id is null &&
            Last is null &&
            CreationTimeAfter is null &&
            CreationTimeBefore is null &&
            StartTimeAfter is null &&
            StartTimeBefore is null &&
            EndTimeAfter is null &&
            EndTimeBefore is null &&
            ResumeTimeAfter is null &&
            ResumeTimeBefore is null &&
            Priority is null &&
            ReleaseName is null &&
            ProcessType is null &&
            SourceType is null &&
            Robot is null &&
            State is null &&
            Skip is null && First is null);

        if (bOutCache)
        {
            WriteWarning("Since no filter parameters were specified, the contents of the cache will be output. To query the Orchestrator, please specify at least one filter parameter.");

            foreach (var (drive, folder) in drivesFolders)
            {
                var jobs = drive.Jobs.GetCache(folder);
                if (jobs is not null)
                {
                    switch (OrderBy)
                    {
                        case "CreationTime":
                            if (OrderAscending.IsPresent)
                                WriteObject(jobs.Values.OrderBy(j => j.CreationTime), true);
                            else
                                WriteObject(jobs.Values.OrderByDescending(j => j.CreationTime), true);
                            break;
                        case "Release/Name":
                            if (OrderAscending.IsPresent)
                                WriteObject(jobs.Values.OrderBy(j => j.ReleaseName), true);
                            else
                                WriteObject(jobs.Values.OrderByDescending(j => j.ReleaseName), true);
                            break;
                        case "State":
                            if (OrderAscending.IsPresent)
                                WriteObject(jobs.Values.OrderBy(j => j.State), true);
                            else
                                WriteObject(jobs.Values.OrderByDescending(j => j.State), true);
                            break;
                        case "SpecificPriorityValue":
                            if (OrderAscending.IsPresent)
                                WriteObject(jobs.Values.OrderBy(j => j.SpecificPriorityValue), true);
                            else
                                WriteObject(jobs.Values.OrderByDescending(j => j.SpecificPriorityValue), true);
                            break;
                        case "StartTime":
                            if (OrderAscending.IsPresent)
                                WriteObject(jobs.Values.OrderBy(j => j.StartTime), true);
                            else
                                WriteObject(jobs.Values.OrderByDescending(j => j.StartTime), true);
                            break;
                        case "EndTime":
                            if (OrderAscending.IsPresent)
                                WriteObject(jobs.Values.OrderBy(j => j.EndTime), true);
                            else
                                WriteObject(jobs.Values.OrderByDescending(j => j.EndTime), true);
                            break;
                        case "SourceType":
                            if (OrderAscending.IsPresent)
                                WriteObject(jobs.Values.OrderBy(j => j.SourceType), true);
                            else
                                WriteObject(jobs.Values.OrderByDescending(j => j.SourceType), true);
                            break;
                    }
                }
            }
            return;
        }

        using var cancelHandler = new ConsoleCancelHandler();
        if (Id is null || Id.Length == 0)
        {
            using ProgressReporter reporter = new(this, 1, drivesFolders.Count, "Get Job");
            int index = 0;
            foreach (var (drive, folder) in drivesFolders)
            {
                cancelHandler.Token.ThrowIfCancellationRequested(); // この行は try の外側に置く必要がある。

                string filter = MakeFilter(drive, folder);
                if (filter == "null") continue;
                reporter.WriteProgress(++index, $"{folder.GetPSPath()}");
                try
                {
                    //var jobs = drive.GetJobs(folder, filter, skip, first, OrderBy, OrderAscending.IsPresent);
                    var jobs = drive.Jobs.Fetch(folder, filter, skip, first, OrderBy, OrderAscending.IsPresent);
                    WriteObject(jobs, true);
                }
                catch (Exception ex)
                {
                    string target = folder.GetPSPath();
                    var errorRecord = new ErrorRecord(new OrchException(target, ex), "GetJobError", ErrorCategory.InvalidOperation, target);
                    WriteError(errorRecord);
                }
                Thread.Sleep(600); // API call rate limit を回避するため待機する
            }
            return;
        }

        // Id の指定がある場合は、その Job のみ取得する
        foreach (var (drive, folder) in drivesFolders)
        {
            foreach (var jobId in Id!)
            {
                cancelHandler.Token.ThrowIfCancellationRequested(); // この行は try の外側に置く必要がある。
                try
                {
                    var job = drive.GetJob(folder, jobId);
                    if (job is not null)
                    {
                        WriteObject(job);
                    }
                }
                catch (Exception ex)
                {
                    string target = System.IO.Path.Combine(folder.GetPSPath(), jobId.ToString());
                    var errorRecord = new ErrorRecord(new OrchException(target, ex), "GetJobError", ErrorCategory.InvalidOperation, target);
                    WriteError(errorRecord);
                }
                Thread.Sleep(600); // API call rate limit を回避するため待機する
            }
        }
    }
}
