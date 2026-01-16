using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchTestSetExecution")]
[OutputType(typeof(Entities.TestSetExecution))]
public class GetTestSetExecutionCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(NameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StatusCompleter))]
    [SupportsWildcards]
    public string[]? Status { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<Hour_Day_Week_Month_3Month_6Month_Year_3Year>))]
    [ValidatePositionalParameter<Hour_Day_Week_Month_3Month_6Month_Year_3Year>]
    public string? Last { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TimeAfterCompleter))]
    public DateTime? StartTimeAfter { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TimeBeforeCompleter))]
    public DateTime? StartTimeBefore { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TriggerTypeCompleter))]
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
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var recurse = GetSwitchParameterValue(commandAst, "Recurse");
            var paramDepth = GetParameterValue(commandAst, "Depth");
            uint.TryParse(paramDepth, out uint depth);

            // パラメータからパスを抽出する。指定がなければ、カレントディレクトリを対象にする
            var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
            var drivesFolders = SessionState.EnumFoldersWithoutPersonalWorkspace(paramPath, recurse, depth);

            // パラメータで選択済みの Name は、候補から除外する
            var wpName = CreateWPListFromParameter(commandAst, "Name", TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            // GetTestSetExecutions() は、キャッシュがあっても毎回取りにいく。
            // そのため、completer でこれを呼び出すのは不適。
            // var results = ParallelResults.ForEach(drivesFolders, df => df.drive.GetTestSetExecutions(df.folder));

            // キャッシュがない場合に限り、GetTestSetExecutions() を呼ぶ。
            // キャッシュがあれば、それを使う。
            foreach (var (drive, folder) in drivesFolders)
            {
                ICollection<TestSetExecution> testSetExecutions;
                if (drive._dicTestSetExecutions is not null && drive._dicTestSetExecutions.TryGetValue(folder.Id ?? 0, out var entities))
                {
                    testSetExecutions = entities!.Values;
                }
                else
                {
                    testSetExecutions = drive.GetTestSetExecutions(folder);
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

    // TODO: StaticTextCompleter で書き直す
    private class StatusCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            // パラメータで選択済みの SourceType は、候補から除外する
            var wpStatus = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            foreach (var state in Enum.GetNames(typeof(TestSetExecutionStatus)).ExcludeByWildcards(st => st, wpStatus).Where(st => wp.IsMatch(st)))
            {
                yield return new CompletionResult(state);
            }
        }
    }

    // TODO: StaticTextCompleter で書き直す
    private class TriggerTypeCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            // パラメータで選択済みの TriggerType は、候補から除外する
            var paramStatus = GetParameterValues(commandAst, parameterName, TPositional.Parameters, wordToComplete);
            var wpStatus = paramStatus.ConvertToWildcardPatternList();

            var wp = CreateWPFromWordToComplete(wordToComplete);

            foreach (var state in Enum.GetNames(typeof(TestSetExecutionTriggerType)).ExcludeByWildcards(st => st, wpStatus).Where(st => wp.IsMatch(st)))
            {
                yield return new CompletionResult(state);
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

        // すべてのフィルタパラメータが指定されていなければ、キャッシュの内容を返す
        bool bOutCache = (
            Last is null &&
            StartTimeAfter is null &&
            StartTimeBefore is null &&
            Status is null &&
            TriggerType is null &&
            Skip is null && First is null);

        if (bOutCache)
        {
            WriteWarning("Since no filter parameters were specified, the contents of the cache will be output. To query the Orchestrator, please specify at least one filter parameter.");

            foreach (var (drive, folder) in drivesFolders)
            {
                if (drive._dicTestSetExecutions?.TryGetValue(folder.Id ?? 0, out var entities) ?? false)
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
            df => df.drive.GetTestSetExecutions(df.folder, filter, skip, first)
        );

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var entities = result.GetResult(cancelHandler.Token);
                if (entities is null) continue;

                // この cmdlet は -Skip と -First をサポートするため、ここで出力をソートしてはいけない
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
