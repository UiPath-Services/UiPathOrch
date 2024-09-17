using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.Name;
using LastItems = UiPath.PowerShell.Positional.Hour_Day_Week_Month_3Month_6Month_Year_3Year;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "OrchTestSetExecution")]
    [OutputType(typeof(Entities.TestSetExecution))]
    public class GetTestSetExecutionCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(NameCompleter))]
        [SupportsWildcards]
        public string[]? Name { get; set; }

        [Parameter]
        [ArgumentCompleter(typeof(StatusCompleter))]
        [SupportsWildcards]
        public string[]? Status { get; set; }

        [Parameter]
        [ArgumentCompleter(typeof(StaticTextsCompleter<LastItems>))]
        public string? Last { get; set; }

        [Parameter]
        [ArgumentCompleter(typeof(TimeAfterCompleter))]
        public DateTime? StartTimeAfter { get; set; }

        [Parameter]
        [ArgumentCompleter(typeof(TimeBeforeCompleter))]
        public DateTime? StartTimeBefore { get; set; }

        [Parameter]
        [ArgumentCompleter(typeof(TriggerTypeCompleter))]
        [SupportsWildcards]
        public string[]? TriggerType { get; set; }

        [Parameter]
        public ulong? Skip { get; set; }

        [Parameter]
        [ArgumentCompleter(typeof(StaticTextsCompleter<Item10>))]
        public ulong? First { get; set; }

        [Parameter]
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
                var drivesFolders = OrchDriveInfo.EnumFoldersWithoutPersonalWorkspace(paramPath, recurse, depth);

                // パラメータで選択済みの Name は、候補から除外する
                var wpName = CreateWPListFromParameter(commandAst, "Name", Positional.Name.Parameters, wordToComplete);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                // GetTestSetExecutions() は、キャッシュがあっても毎回取りにいく。
                // そのため、completer でこれを呼び出すのは不適。
                // var results = ParallelResults.ForEach(drivesFolders, df => df.drive.GetTestSetExecutions(df.folder));

                // キャッシュがない場合に限り、GetTestSetExecutions() を呼ぶ。
                // キャッシュがあれば、それを使う。
                foreach (var (drive, folder) in drivesFolders)
                {
                    ICollection<TestSetExecution> testSetExecutions;
                    if (drive._dicTestSetExecutions != null && drive._dicTestSetExecutions.TryGetValue(folder.Id ?? 0, out var entities))
                    {
                        testSetExecutions = entities!.Values;
                    }
                    else
                    {
                        testSetExecutions = drive.GetTestSetExecutions(folder);
                    }

                    foreach (var e in testSetExecutions!
                        .Where(e => e != null)
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
                var paramStatus = GetParameterValues(commandAst, "Status", null, wordToComplete);
                var wpStatus = paramStatus.Select(fn => new WildcardPattern(fn, WildcardOptions.IgnoreCase)).ToList();

                var wp = CreateWPFromWordToComplete(wordToComplete);

                foreach (var state in Enum.GetNames(typeof(TestSetExecutionStatus)).ExcludeByWildcards(st => st, wpStatus).Where(st => wp.IsMatch(st)))
                {
                    yield return new CompletionResult(state);
                }
            }
        }

        private class TriggerTypeCompleter : OrchArgumentCompleter
        {
            public override IEnumerable<CompletionResult> CompleteArgument(
                string commandName,
                string parameterName,
                string wordToComplete,
                CommandAst commandAst,
                IDictionary fakeBoundParameters)
            {
                // パラメータで選択済みの SourceType は、候補から除外する
                var paramStatus = GetParameterValues(commandAst, "Source", null, wordToComplete);
                var wpStatus = paramStatus.Select(fn => new WildcardPattern(fn, WildcardOptions.IgnoreCase)).ToList();

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
            //if (Name != null)
            //{
            //    if (Name != null && Name.Any())
            //    {
            //        var names = Name.Select(n => $"(Name%20eq%20%27{HttpUtility.UrlEncode(n)}%27)");
            //        filter.Add("(" + string.Join("%20or%20", names) + ")");
            //    }
            //}
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
                filter.Add($"(StartTime%20ge%20{last:yyyy-MM-ddTHH:mm:ss.fffZ})");
            }
            #endregion

            #region StartTimeAfter
            if (StartTimeAfter != null)
            {
                filter.Add($"(StartTime%20ge%20{StartTimeAfter.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ})");
            }
            #endregion

            #region StartTimeBefore
            if (StartTimeBefore != null)
            {
                filter.Add($"(StartTime%20lt%20{StartTimeBefore.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ})");
            }
            #endregion

            #region Process
            //if (Process != null && Process.Length > 0)
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
            if (Status != null && Status.Length > 0)
            {
                var status = new List<string>();
                var wpStatus = Status.Select(st => new WildcardPattern(st, WildcardOptions.IgnoreCase)).ToList();
                foreach (var state in Enum.GetNames(typeof(TestSetExecutionStatus)).FilterByWildcards(st => st, wpStatus))
                {
                    status.Add($"(Status%20eq%20%27{(int)Enum.Parse(typeof(TestSetExecutionStatus), state)}%27)");
                }
                if (status.Any())
                    filter.Add("(" + string.Join("%20or%20", status) + ")");
            }
            #endregion

            #region TriggerType
            if (TriggerType != null && TriggerType.Length > 0)
            {
                var triggerTypes = new List<string>();
                var wpTriggerType = TriggerType.Select(st => new WildcardPattern(st, WildcardOptions.IgnoreCase)).ToList();
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

            var drivesFolders = OrchDriveInfo.EnumFoldersWithoutPersonalWorkspace(Path, Recurse.IsPresent, Depth);
            var wpName = Name.ConvertToWildcardPatternList();

            string filter = MakeFilter();

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
                    if (entities == null) continue;

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
}
