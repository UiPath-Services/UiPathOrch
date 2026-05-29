using System.Collections;
using UiPath.PowerShell.Positional;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchTestSetExecution")]
[OutputType(typeof(Entities.TestSetExecution))]
public class GetTestSetExecutionCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(NameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<TestSetExecutionStatusNames>))]
    [SupportsWildcards]
    public string[]? Status { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<Hour_Day_Week_Month_3Month_6Month_Year_3Year>))]
    [ValidateStaticCandidate<Hour_Day_Week_Month_3Month_6Month_Year_3Year>]
    public string? Last { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TimeAfterCompleter))]
    public DateTime? StartTimeAfter { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TimeBeforeCompleter))]
    public DateTime? StartTimeBefore { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<TestSetExecutionTriggerTypeNames>))]
    [SupportsWildcards]
    public string[]? TriggerType { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public ulong? Skip { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<Item10>))]
    public ulong? First { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    private class NameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var recurse = ResolveSwitchParameter(fakeBoundParameters, "Recurse");
            var depth = ResolveDepth(fakeBoundParameters);

            // Extract path from parameters. If not specified, target the current directory
            var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
            var drivesFolders = SessionState.EnumFoldersWithoutPersonalWorkspace(paramPath, recurse, depth);

            // Exclude Names already selected by parameter from candidates
            var wpName = CreateSelfExclusionList(commandAst, "Name", wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            // TestSetExecutions.Fetch() fetches from the API every time even if a cache exists.
            // Therefore, it is not suitable to call this from a completer.
            // var results = ParallelResults3.GroupBy(drivesFolders, df => df.drive.TestSetExecutions.Fetch(df.folder));

            // Only call TestSetExecutions.Fetch() when there is no cache.
            // If a cache exists, use it.
            foreach (var (drive, folder) in drivesFolders)
            {
                ICollection<TestSetExecution> testSetExecutions;
                var entities = drive.TestSetExecutions.GetCache(folder);
                if (entities is not null)
                {
                    testSetExecutions = entities.Values;
                }
                else
                {
                    testSetExecutions = drive.TestSetExecutions.Fetch(folder);
                }

                foreach (var e in testSetExecutions!
                    .Where(e => e is not null)
                    .Where(e => wp.IsMatch(e.Name!))
                    .ExcludeByWildcards(e => e?.Name, wpName)
                    .OrderBy(te => te.Name))
                {
                    string tiphelp = TipHelp(e);
                    yield return new CompletionResult(PathTools.EscapePSText(e!.Name), e.Name, CompletionResultType.ParameterValue, tiphelp);
                }
            }
        }
    }

    private string? MakeFilter()
    {
        var filter = new List<string>();

        #region Name
        //if (Name is not null)
        //{
        //    if (Name is not null && Name.Any())
        //    {
        //        var names = Name.Select(n => $"(Name%20eq%20%27{HttpUtility.UrlEncode(n)}%27)");
        //        filter.Add("(" + string.Join("%20or%20", names) + ")");
        //    }
        //}
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
            filter.Add($"(StartTime%20ge%20{last:yyyy-MM-ddTHH:mm:ss.fffZ})");
        }
        #endregion

        #region StartTimeAfter
        if (StartTimeAfter is not null)
        {
            filter.Add($"(StartTime%20ge%20{StartTimeAfter.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ})");
        }
        #endregion

        #region StartTimeBefore
        if (StartTimeBefore is not null)
        {
            filter.Add($"(StartTime%20lt%20{StartTimeBefore.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ})");
        }
        #endregion

        #region Process
        //if (Process is not null && Process.Length > 0)
        //{
        //    var wpProcess = Process.Select(p => new WildcardPattern(p, WildcardOptions.IgnoreCase)).ToList();
        //    var drivesFolders = OrchDriveInfo.EnumFolders(Path);
        //    foreach (var (drive, folder) in drivesFolders)
        //    {
        //        var processes = new List<string>();
        //        var processIds = new HashSet<Int64>();
        //        foreach (var process in drive.ListReleases(folder).FilterByWildcards(r => r.Name!, wpProcess))
        //        {
        //            if (processIds.Add(process.Id ?? 0))
        //            {
        //                processes.Add($"(Release/Id%20eq%20{process.Id})");
        //            }
        //        }
        //        if (processes.Any())
        //            filter.Add("(" + string.Join("%20or%20", processes) + ")");
        //    }
        //}
        #endregion

        #region Status
        if (Status is not null && Status.Length > 0)
        {
            var status = new List<string>();
            var wpStatus = Status.ConvertToWildcardPatternList();
            foreach (var state in Enum.GetNames(typeof(TestSetExecutionStatus)).FilterByWildcards(st => st, wpStatus))
            {
                status.Add($"(Status%20eq%20%27{(int)Enum.Parse(typeof(TestSetExecutionStatus), state)}%27)");
            }
            if (status.Any())
                filter.Add("(" + string.Join("%20or%20", status) + ")");
        }
        #endregion

        #region TriggerType
        if (TriggerType is not null && TriggerType.Length > 0)
        {
            var triggerTypes = new List<string>();
            var wpTriggerType = TriggerType.ConvertToWildcardPatternList();
            foreach (var triggerType in Enum.GetNames(typeof(TestSetExecutionTriggerType)).FilterByWildcards(st => st, wpTriggerType))
            {
                triggerTypes.Add($"(TriggerType%20eq%20%27{Enum.Parse(typeof(TestSetExecutionTriggerType), triggerType)}%27)");
            }
            if (triggerTypes.Any())
                filter.Add("(" + string.Join("%20or%20", triggerTypes) + ")");
        }
        #endregion State

        if (filter.Any())
        {
            string ret = string.Join("%20and%20", filter);
            return "&$filter=(" + ret + ")";
        }
        return null;
    }

    protected override void ProcessRecord()
    {
        ulong skip = Skip ?? 0;
        ulong first = First ?? ulong.MaxValue;

        var drivesFolders = SessionState.EnumFoldersWithoutPersonalWorkspace(Path, Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();

        // If no filter parameters are specified, return the cache contents
        bool bOutCache = (
            Last is null &&
            StartTimeAfter is null &&
            StartTimeBefore is null &&
            Status is null &&
            TriggerType is null &&
            Skip is null && First is null);

        if (bOutCache)
        {
            WriteWarning($"[{MyInvocation.MyCommand.Name}] Since no filter parameters were specified, the contents of the cache will be output. To query the Orchestrator, please specify at least one filter parameter.");

            foreach (var (drive, folder) in drivesFolders)
            {
                var entities = drive.TestSetExecutions.GetCache(folder);
                if (entities is not null)
                {
                    WriteObject(entities.Values
                        .FilterByWildcards(e => e?.Name, wpName),
                        true);
                }
            }
            return;
        }

        string? filter = MakeFilter();

        using var results = OrchThreadPool.RunForEach(drivesFolders,
            df => df.folder.GetPSPath(),
            df => df.folder,
            df => df.drive.TestSetExecutions.Fetch(df.folder, filter, skip, first)
        );

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var entities = result.GetResult(cancelHandler.Token);
                if (entities is null) continue;

                // This cmdlet supports -Skip and -First, so output must not be sorted here
                WriteObject(entities
                    .FilterByWildcards(e => e?.Name, wpName),
                    true);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetTestExecutionError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
