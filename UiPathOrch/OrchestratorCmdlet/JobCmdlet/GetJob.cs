using System.Collections;
using UiPath.PowerShell.Positional;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchJob", DefaultParameterSetName = "JobId")] //, SupportsPaging = true)]
[OutputType(typeof(Entities.Job))]
public class GetJobCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ParameterSetName = "JobId", ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(JobIdCompleter))]
    public Int64[]? Id { get; set; }

    [Parameter(ParameterSetName = "Filter", ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<Hour_Day_Week_Month_3Month_6Month_Year_3Year>))]
    [ValidateStaticCandidate<Hour_Day_Week_Month_3Month_6Month_Year_3Year>]
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
    [ArgumentCompleter(typeof(StaticTextsCompleter<JobPriorityItems>))]
    public string? Priority { get; set; }

    [Parameter(ParameterSetName = "Filter", ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(ListReleasesCompleter))]
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

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    private class RobotCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drives = ResolveOrchDrives(fakeBoundParameters);

            var wpRobot = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drives, drive => drive.Robots.Get());

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

    // Robot/Id eq is a navigation property, so the API returns 400 when there are 17 or more
    private const int RobotFilterBatchSize = 10;

    private IReadOnlyList<long?>? ResolveRobotIds(OrchDriveInfo drive)
    {
        if (Robot is null || Robot.Length == 0 || Robot.Any(r => r == "*"))
            return null; // No robot filter (target all robots)
        var wpRobot = Robot.ConvertToWildcardPatternList();
        var robots = drive.Robots.Get();
        return robots.SelectByWildcards(r => r?.Name, wpRobot).Select(r => r.Id).ToList();
    }

    private string? MakeFilter(OrchDriveInfo drive, Folder folder, IReadOnlyList<long?>? robotIdBatch = null)
    {
        List<string> filter = [];

        #region ReleaseName
        if (ReleaseName is not null && ReleaseName.Length > 0 && !ReleaseName.Any(p => p == "*"))
        {
            var releases = drive.Releases.Get(folder);
            var targetReleases = releases.SelectByNames(r => r?.Name, ReleaseName);
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
            filter.Add($"(CreationTime ge {last.ToODataUtc()})");
        }
        #endregion

        filter.AddTimeRange("CreationTime", CreationTimeAfter, CreationTimeBefore);
        filter.AddTimeRange("StartTime", StartTimeAfter, StartTimeBefore);
        filter.AddTimeRange("EndTime", EndTimeAfter, EndTimeBefore);
        filter.AddTimeRange("ResumeTime", ResumeTimeAfter, ResumeTimeBefore);

        #region Priority
        if (Priority is not null)
        {
            var priority = Priority.ToLowerInvariant();
            switch (priority)
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
        // TODO: Are these numbers correct?
        // Confirmed that when ApiVersion == 11, ProcessType should be null
        if (drive.OrchAPISession.ApiVersion >= 12)
        {
            var processType = ProcessType ?? ["Process"];
            filter.AddIfNotNull(JobProcessTypeItems.Items
                .SelectByWildcards(i => i, processType)
                .CreateOrFilter(i => $"ProcessType eq '{i}'"));
        }
        #endregion

        #region Robot
        if (robotIdBatch is not null)
        {
            filter.AddIfNotNull(robotIdBatch.CreateOrFilter(i => $"Robot/Id eq {i}"));
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

    // Ordering for the no-filter cache-output path. Extracted as a pure,
    // testable function. The default arm is the regression guard: -OrderBy is
    // offered via JobOrderableItems but is a completer, not a ValidateSet, so an
    // unlisted value previously matched no case and the switch fell through with
    // no output, silently dropping every cached job. "CreationTime" (the
    // default) and any other unlisted value fall back to CreationTime ordering,
    // descending unless -OrderAscending.
    internal static IEnumerable<Entities.Job> OrderJobsForOutput(
        IEnumerable<Entities.Job> items, string? orderBy, bool ascending) =>
        orderBy switch
        {
            "Release/Name" => ascending ? items.OrderBy(j => j.ReleaseName) : items.OrderByDescending(j => j.ReleaseName),
            "State" => ascending ? items.OrderBy(j => j.State) : items.OrderByDescending(j => j.State),
            "SpecificPriorityValue" => ascending ? items.OrderBy(j => j.SpecificPriorityValue) : items.OrderByDescending(j => j.SpecificPriorityValue),
            "StartTime" => ascending ? items.OrderBy(j => j.StartTime) : items.OrderByDescending(j => j.StartTime),
            "EndTime" => ascending ? items.OrderBy(j => j.EndTime) : items.OrderByDescending(j => j.EndTime),
            "SourceType" => ascending ? items.OrderBy(j => j.SourceType) : items.OrderByDescending(j => j.SourceType),
            _ => ascending ? items.OrderBy(j => j.CreationTime) : items.OrderByDescending(j => j.CreationTime),
        };

    protected override void ProcessRecord()
    {
        ulong skip = Skip ?? 0;
        ulong first = First ?? ulong.MaxValue;

        var drivesFolders = SessionState.EnumFolders(EffectivePath(Path, LiteralPath), Recurse.IsPresent, Depth);

        var orderBy = string.IsNullOrEmpty(OrderBy) ? "CreationTime" : OrderBy;

        // If no parameters are specified, return the contents of the cache
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
            WriteWarning($"[{MyInvocation.MyCommand.Name}] Since no filter parameters were specified, the contents of the cache will be output. To query the Orchestrator, please specify at least one filter parameter.");

            foreach (var (drive, folder) in drivesFolders)
            {
                var jobs = drive.Jobs.GetCache(folder);
                if (jobs is not null)
                {
                    WriteObject(OrderJobsForOutput(jobs.Values, orderBy, OrderAscending.IsPresent), true);
                }
            }
            return;
        }

        using var cancelHandler = new ConsoleCancelHandler();
        if (Id is null || Id.Length == 0)
        {
            using ProgressReporter reporter = new(this, 1, drivesFolders.Count, "Get Job");
            int index = 0;
            foreach (var (drive, folder) in drivesFolders.WithCancellation(cancelHandler.Token))
            {
                var targetRobotIds = ResolveRobotIds(drive);
                if (targetRobotIds is not null && targetRobotIds.Count == 0)
                {
                    reporter.WriteProgress(++index, $"{folder.GetPSPath()}");
                    continue; // No matching robots
                }
                reporter.WriteProgress(++index, $"{folder.GetPSPath()}");

                // Split robot IDs into batches (multiple API calls when exceeding RobotFilterBatchSize)
                IEnumerable<IReadOnlyList<long?>?> robotBatches;
                bool isBatched;
                if (targetRobotIds is null || targetRobotIds.Count <= RobotFilterBatchSize)
                {
                    robotBatches = [(IReadOnlyList<long?>?)targetRobotIds];
                    isBatched = false;
                }
                else
                {
                    robotBatches = targetRobotIds.Chunk(RobotFilterBatchSize).Select(b => (IReadOnlyList<long?>)b.ToList());
                    isBatched = true;
                }

                var allJobs = isBatched ? new List<object>() : null;
                foreach (var robotBatch in robotBatches.WithCancellation(cancelHandler.Token))
                {
                    string filter = MakeFilter(drive, folder, robotBatch);
                    if (filter == "null") continue;
                    try
                    {
                        //var jobs = drive.GetJobs(folder, filter, skip, first, OrderBy, OrderAscending.IsPresent);
                        var batchSkip = isBatched ? 0UL : skip;
                        var batchFirst = isBatched ? ulong.MaxValue : first;
                        var jobs = drive.Jobs.Fetch(folder, filter, batchSkip, batchFirst, orderBy, OrderAscending.IsPresent);
                        if (allJobs is not null)
                            foreach (var j in jobs) allJobs.Add(j);
                        else
                            WriteObject(jobs, true);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        string target = folder.GetPSPath();
                        var errorRecord = new ErrorRecord(new OrchException(target, ex), "GetJobError", ErrorCategory.InvalidOperation, target);
                        WriteError(errorRecord);
                    }
                    // Cancellable rate-limit wait — Ctrl+C bails promptly.
                    cancelHandler.Token.Sleep(600);
                }
                if (allJobs is not null)
                {
                    // Each robot batch was $orderby-sorted server-side independently; re-sort the merged
                    // list so -First/-Skip slice the global order, not the per-batch concatenation. Clamp
                    // the ulong Skip/First to int before LINQ Skip/Take so a huge -First cannot overflow to
                    // a negative count (which would silently drop all output).
                    IEnumerable<object> result = OrderJobsForOutput(allJobs.Cast<Entities.Job>(), orderBy, OrderAscending.IsPresent);
                    if (skip > 0) result = result.Skip((int)Math.Min(skip, int.MaxValue));
                    if (first < ulong.MaxValue) result = result.Take((int)Math.Min(first, int.MaxValue));
                    WriteObject(result, true);
                }
            }
            return;
        }

        // If Id is specified, retrieve only that specific Job
        foreach (var (drive, folder) in drivesFolders)
        {
            foreach (var jobId in Id!.WithCancellation(cancelHandler.Token))
            {
                try
                {
                    var job = drive.GetJob(folder, jobId);
                    if (job is not null)
                    {
                        WriteObject(job);
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    string target = System.IO.Path.Combine(folder.GetPSPath(), jobId.ToString());
                    var errorRecord = new ErrorRecord(new OrchException(target, ex), "GetJobError", ErrorCategory.InvalidOperation, target);
                    WriteError(errorRecord);
                }
                // Cancellable rate-limit wait — Ctrl+C bails promptly.
                cancelHandler.Token.Sleep(600);
            }
        }
    }
}
