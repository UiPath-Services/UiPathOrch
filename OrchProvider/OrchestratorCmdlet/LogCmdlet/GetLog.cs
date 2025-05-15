using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;
using TPositional = UiPath.PowerShell.Positional.Empty;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchLog")]
[OutputType(typeof(Log))]
public class GetLogCommand : OrchestratorPSCmdlet
{
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<Hour_Day_Week_Month_3Month_6Month_Year_3Year>))]
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
    [ArgumentCompleter(typeof(JobKeyCompleter))]
    [Alias("Key")]
    public string? JobKey { get; set; }

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
            var wpName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.ForEach(drivesFolders, df => df.drive.FolderMachinesAssigned.Get(df.folder));

            foreach (var result in results)
            {
                if (result.Result is null) continue;

                foreach (var e in result.Result
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
            var wpWindowsIdentity = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.ForEach(drivesFolders, df => df.drive.UserRobots.Get(df.folder));

            foreach (var result in results)
            {
                if (result.Result is null) continue;

                foreach (var e in result.Result
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

    private class JobKeyCompleter : OrchArgumentCompleter
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
            var wpJobKey = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            // API call はしないので、スレッドを起こす必要はない

            foreach (var (drive, folder) in drivesFolders)
            {
                if (drive._dicJobs?.TryGetValue(folder.Id!.Value, out var jobs) ?? false)
                {
                    foreach (var job in jobs.Values
                        .Where(l => wp.IsMatch(l.Key))
                        .OrderBy(l => l.Key))
                    {
                        string tiphelp = System.IO.Path.Combine(folder.GetPSPath(), job.Id?.ToString() ?? "") + $" ({job.ReleaseName} {job.CreationTime})";
                        yield return new CompletionResult(PathTools.EscapePSText(job.Key), job.Key, CompletionResultType.ParameterValue, tiphelp);
                    }
                }
            }
        }
    }

    private string? MakeFilter(OrchDriveInfo drive, Folder folder)
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
        if (!string.IsNullOrEmpty(JobKey))
        {
            filter.Add($"(JobKey eq {JobKey})");
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
        if (TimeStampAfter is not null)
        {
            filter.Add($"(TimeStamp ge {TimeStampAfter.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ})");
        }
        #endregion

        #region TimeStampBefore
        if (TimeStampBefore is not null)
        {
            filter.Add($"(TimeStamp lt {TimeStampBefore.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ})");
        }
        #endregion

        #region Machine
        if (!string.IsNullOrEmpty(Machine))
        {
            var machines = drive.FolderMachinesAssigned.Get(folder);
            filter.AddIfNotNull(machines
                //.SelectByWildcards(m => m?.Name, Machine)
                .Where(m => string.Compare(m?.Name, WildcardPattern.Unescape(Machine), StringComparison.OrdinalIgnoreCase) == 0)
                .CreateOrFilter(m => $"MachineKey eq {m.Key}"));
        }
        #endregion

        #region HostIdentity
        if (WindowsIdentity is not null)
        {
            var userRobots = drive.UserRobots.Get(folder);
            filter.AddIfNotNull(userRobots
                .SelectByWildcards(u => u?.UserName, WindowsIdentity)
                .Where(u => u.RobotNames is not null)
                .SelectMany(u => u?.RobotNames!)
                .Where(r => !string.IsNullOrEmpty(r))
                .CreateOrFilter(r => $"RobotName eq '{r}'"));
        }
        #endregion

        string ret = filter.CreateAndFilter(f => f);
        ret = $"&$filter={ret}";
        return ret;
    }

    protected override void ProcessRecord()
    {
        ulong first = First ?? ulong.MaxValue;
        ulong skip = Skip ?? 0;
        if (string.IsNullOrEmpty(OrderBy)) OrderBy = "TimeStamp";

        // すべてのパラメータが指定されていなければ、キャッシュの内容を返す
        bool bOutCache = (
            Last is null &&
            TimeStampAfter is null &&
            TimeStampBefore is null &&
            Level is null &&
            Machine is null &&
            ProcessName is null &&
            WindowsIdentity is null &&
            JobKey is null &&
            Skip is null && First is null);

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

        using ProgressReporter reporter = new(this, 1, drivesFolders.Count, "Get log");
        int index = 0;
        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            cancelHandler.Token.ThrowIfCancellationRequested();

            reporter.WriteProgress(++index, folder.GetPSPath());

            string query = MakeFilter(drive, folder);
            if (query == "null") continue;

            try
            {
                var logs = drive.GetRobotLogs(folder, query, skip, first, OrderBy, OrderAscending.IsPresent);
                WriteObject(logs, true);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "GetLogError", ErrorCategory.InvalidOperation, folder));
            }
        }
    }
}
