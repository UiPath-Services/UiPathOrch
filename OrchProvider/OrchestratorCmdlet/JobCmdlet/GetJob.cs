using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;
using TPositional = UiPath.PowerShell.Positional.Id;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "OrchJob", DefaultParameterSetName = "JobId")] //, SupportsPaging = true)]
    [OutputType(typeof(Entities.Job))]
    public class GetJobCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, ParameterSetName = "JobId", ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(IdCompleter))]
        public Int64[]? Id { get; set; }

        [Parameter(ParameterSetName = "Filter", ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(StaticTextsCompleter<Hour_Day_Week_Month_3Month_6Month_Year_3Year>))]
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
        [SupportsWildcards]
        public string[]? State { get; set; }

        [Parameter(ParameterSetName = "Filter", ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(StaticTextsCompleter<JobProcessTypeItems>))]
        [SupportsWildcards]
        public string[]? ProcessType { get; set; }

        [Parameter(ParameterSetName = "Filter", ValueFromPipelineByPropertyName = true)]
        public ulong? Skip { get; set; }

        [Parameter(ParameterSetName = "Filter", ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(StaticTextsCompleter<JobOrderableItems>))]
        public string? OrderBy { get; set; }

        [Parameter(ParameterSetName = "Filter")]
        public SwitchParameter OrderAscending { get; set; }

        [Parameter(ParameterSetName = "Filter")]
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
                    if (drive._dicJobs == null)
                    {
                        continue;
                    }

                    if (!drive._dicJobs.TryGetValue(folder.Id ?? 0, out var dicJobs))
                    {
                        continue;
                    }

                    foreach (var job in dicJobs.Values.ExcludeByClassValues(j => (j?.Id ?? 0).ToString(), paramId))
                    {
                        if (!wp.IsMatch((job.Id ?? 0).ToString()))
                            continue;

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

        private string? MakeFilter(OrchDriveInfo drive, Folder folder)
        {
            List<string> filter = [];

            #region ReleaseName
            if (ReleaseName != null && ReleaseName.Length > 0 && !ReleaseName.Any(p => p == "*"))
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
                filter.Add($"(CreationTime ge {last:yyyy-MM-ddTHH:mm:ss.fffZ})");
            }
            #endregion

            #region CreationTimeAfter
            if (CreationTimeAfter != null)
            {
                filter.Add($"(CreationTime ge {CreationTimeAfter.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ})");
            }
            #endregion

            #region CreationTimeBefore
            if (CreationTimeBefore != null)
            {
                filter.Add($"(CreationTime lt {CreationTimeBefore.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ})");
            }
            #endregion

            #region StartTimeAfter
            if (StartTimeAfter != null)
            {
                filter.Add($"(StartTime ge {StartTimeAfter.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ})");
            }
            #endregion

            #region StartTimeBefore
            if (StartTimeBefore != null)
            {
                filter.Add($"(StartTime lt {StartTimeBefore.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ})");
            }
            #endregion

            #region EndTimeAfter
            if (EndTimeAfter != null)
            {
                filter.Add($"(EndTime ge {EndTimeAfter.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ})");
            }
            #endregion

            #region EndTimeBefore
            if (EndTimeBefore != null)
            {
                filter.Add($"(EndTime lt {EndTimeBefore.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ})");
            }
            #endregion

            #region ResumeTimeAfter
            if (ResumeTimeAfter != null)
            {
                filter.Add($"(ResumeTime ge {ResumeTimeAfter.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ})");
            }
            #endregion

            #region ResumeTimeBefore
            if (ResumeTimeBefore != null)
            {
                filter.Add($"(ResumeTime lt {ResumeTimeBefore.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ})");
            }
            #endregion

            #region Priority
            if (Priority!= null)
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
            if (ProcessType == null) ProcessType = ["Process"];
            filter.AddIfNotNull(JobProcessTypeItems.Parameters
                .SelectByWildcards(i => i, ProcessType)
                .CreateOrFilter(i => $"ProcessType eq '{i}'"));
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

            var drivesFolders = OrchDriveInfo.EnumFolders(Path, Recurse.IsPresent, Depth);

            if (string.IsNullOrEmpty(OrderBy)) OrderBy = "CreationTime";

            // すべてのパラメータが指定されていなければ、キャッシュの内容を返す
            bool bOutCache = (
                Id == null &&
                Last == null &&
                CreationTimeAfter == null &&
                CreationTimeBefore == null &&
                StartTimeAfter == null &&
                StartTimeBefore == null &&
                EndTimeAfter == null &&
                EndTimeBefore == null &&
                ResumeTimeAfter == null &&
                ResumeTimeBefore == null &&
                Priority == null &&
                ReleaseName == null &&
                ProcessType == null &&
                SourceType == null &&
                State == null &&
                Skip == null && First == null);

            if (bOutCache)
            {
                WriteWarning("Since no filter parameters were specified, the contents of the cache will be output. To query the Orchestrator, please specify at least one filter parameter.");

                foreach (var (drive, folder) in drivesFolders)
                {
                    if (drive._dicJobs?.TryGetValue(folder?.Id ?? 0, out var jobs) ?? false)
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
            if (Id == null || Id.Length == 0)
            {
                string msg = $"Get Job";
                using ProgressReporter reporter = new(this, 1, drivesFolders.Count, msg, msg);
                int index = 0;
                foreach (var (drive, folder) in drivesFolders)
                {
                    cancelHandler.Token.ThrowIfCancellationRequested(); // この行は try の外側に置く必要がある。

                    string filter = MakeFilter(drive, folder);
                    if (filter == "null") continue;
                    reporter.WriteProgress(++index, $"{index:D}/{drivesFolders.Count} {folder.GetPSPath()}");
                    try
                    {
                        var jobs = drive.GetJobs(folder, filter, skip, first, OrderBy, OrderAscending.IsPresent);
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
                        if (job != null)
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
}
