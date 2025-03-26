using System.Collections;
using System.Data;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;
using TPositional = UiPath.PowerShell.Positional.Last_Component_UserName_Action;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchAuditLog", DefaultParameterSetName = "Filter")] //, SupportsPaging = true)]
[OutputType(typeof(AuditLog))]
[OutputType(typeof(AuditLogEntity))]
public class GetAuditLogCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ParameterSetName = "Filter", ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<Hour_Day_Week_Month_3Month_6Month_Year_3Year>))]
    public string? Last { get; set; }

    [Parameter(Position = 1, ParameterSetName = "Filter", ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<AuditLogComponentItems>))]
    public string[]? Component { get; set; }

    [Parameter(Position = 2, ParameterSetName = "Filter", ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(UserNameCompleter))]
    public string[]? UserName { get; set; }

    [Parameter(Position = 3, ParameterSetName = "Filter", ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<AuditLogActionItems>))]
    public string[]? Action { get; set; }

    [Parameter(ParameterSetName = "Filter", ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TimeAfterCompleter))]
    public DateTime? ExecutionTimeAfter { get; set; }

    [Parameter(ParameterSetName = "Filter", ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TimeBeforeCompleter))]
    public DateTime? ExecutionTimeBefore { get; set; }

    [Parameter(ParameterSetName = "Id", ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(IdCompleter))]
    [SupportsWildcards]
    public string[]? Id { get; set; }

    [Parameter]
    public SwitchParameter ExpandEntity { get; set; }

    [Parameter]
    public SwitchParameter ExpandDetails { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public ulong? Skip { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<Item10>))]
    public ulong? First { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
    public string[]? Path { get; set; }

    private class IdCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drives = ResolveDrives(fakeBoundParameters);

            // パラメータで選択済みの Id は、候補から除外する
            var paramId = GetParameterValues(commandAst, "Id", TPositional.Parameters, wordToComplete);
            var wpId = paramId.ConvertToWildcardPatternList();

            var wp = CreateWPFromWordToComplete(wordToComplete);

            // キャッシュを使うので、マルチスレッド化する必要はない
            var results = new List<AuditLog>();
            foreach (var drive in drives)
            {
                if (drive._dicAuditLogs is null)
                    continue;
                results.AddRange(drive._dicAuditLogs.Values
                    .Where(log => wp.IsMatch(log.Id.ToString()))
                    .ExcludeByWildcards(log => log?.Id.ToString(), wpId));
            }

            foreach (var log in results.OrderBy(log => log.Id))
            {
                string logId = log.Id.ToString();
                string tiphelp = $"{logId} {log.ExecutionTime!.Value.ToLocalTime():yyyy/MM/dd} {log.OperationText}";
                yield return new CompletionResult(logId, logId, CompletionResultType.ParameterValue, tiphelp);
            }
        }
    }

    private class UserNameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drives = ResolveDrives(fakeBoundParameters);

            // パラメータで選択済みの User は、候補から除外する
            var paramUserName = GetParameterValues(commandAst, "UserName", TPositional.Parameters, wordToComplete);
            var wpUserName = paramUserName.ConvertToWildcardPatternList();

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.ForEach(drives, drive => drive.GetUsers());

            foreach (var result in results)
            {
                if (result.Result is null) continue;

                foreach (var e in result.Result
                    .Where(user => wp.IsMatch(user.UserName))
                    .ExcludeByWildcards(user => user?.UserName, wpUserName)
                    .OrderBy(user => user.UserName))
                {
                    string tiphelp = TipHelp2(e);
                    yield return new CompletionResult(PathTools.EscapePSText(e.UserName), e.UserName, CompletionResultType.Text, tiphelp);
                }
            }
        }
    }

    private string? MakeFilter()
    {
        var filter = new List<string>();

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
            filter.Add($"(ExecutionTime%20ge%20{last:yyyy-MM-ddTHH:mm:ss.fffZ})");
        }
        #endregion

        #region ExecutionTimeAfter
        if (ExecutionTimeAfter is not null)
        {
            filter.Add($"(ExecutionTime%20ge%20{ExecutionTimeAfter.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ})");
        }
        #endregion

        #region ExecutionTimeBefore
        if (ExecutionTimeBefore is not null)
        {
            filter.Add($"(ExecutionTime%20lt%20{ExecutionTimeBefore.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ})");
        }
        #endregion

        #region Component
        if (Component is not null && Component.Length > 0)
        {
            var components = new List<string>();
            var wpComponent = Component.ConvertToWildcardPatternList();
            foreach (var component in AuditLogComponentItems.Parameters.FilterByWildcards(st => st, wpComponent))
            {
                components.Add($"(Component%20eq%20%27{component}%27)");
            }
            if (components.Count != 0)
                filter.Add("(" + string.Join("%20or%20", components) + ")");
        }
        #endregion

        #region UserName
        if (UserName is not null && UserName.Length > 0)
        {
            try
            {
                var drives = OrchDriveInfo.EnumOrchDrives(Path);
                var wpUserName = UserName.ConvertToWildcardPatternList();
                var userIds = new HashSet<Int64>();
                foreach (var drive in drives)
                {
                    userIds.UnionWith(drive.GetUsers()
                        .FilterByWildcards(u => u?.UserName, wpUserName)
                        .Select(u => u.Id ?? 0));
                }
                if (userIds.Count != 0)
                    filter.Add("(" + string.Join("%20or%20", userIds.Select(id => $"(UserId%20eq%20{id})")) + ")");
            }
            catch { }
        }
        #endregion

        #region Action
        if (Action is not null && Action.Length > 0)
        {
            var actions = new List<string>();
            var wpAction = Action.ConvertToWildcardPatternList();
            foreach (var action in AuditLogActionItems.Parameters.FilterByWildcards(st => st, wpAction))
            {
                actions.Add($"(Action%20eq%20%27{action}%27)");
            }
            if (actions.Count != 0)
                filter.Add("(" + string.Join("%20or%20", actions) + ")");
        }
        #endregion

        string order = "&$orderby=ExecutionTime%20desc";

        string ret = string.Join("%20and%20", filter);
        if (!string.IsNullOrEmpty(ret))
            return "&$filter=(" + ret + ")" + order;
        else
            return order;
    }

    // あれ？ どこかにこんなのが必要そうなのがあった気がするが、、
    //private string DecodeBase64_ExtractGzip(string encodedData)
    //{
    //    // Base64 デコード
    //    byte[] decodedData = Convert.FromBase64String(encodedData);

    //    // GZip 展開
    //    using (var inputStream = new MemoryStream(decodedData))
    //    using (var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress))
    //    using (var reader = new StreamReader(gzipStream))
    //    {
    //        return reader.ReadToEnd();
    //    }
    //}

    protected void WriteLog(OrchDriveInfo drive, IEnumerable<AuditLog>? logs)
    {
        if (logs is null) return;

        if (ExpandEntity)
        {
            foreach (var log in logs)
            {
                if (log.Entities is not null && log.Entities.Length > 0)
                {
                    WriteObject(log.Entities, true);
                }
            }
        }
        else if (ExpandDetails)
        {
            using var results = OrchThreadPool.RunForEach(logs,
                log => log.GetPSPath(),
                log => log,
                log => drive.GetAuditLogDetails(log));

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var result in results)
            {
                cancelHandler.Token.ThrowIfCancellationRequested();

                try
                {
                    result.GetResult(cancelHandler.Token);
                    var logDetailed = result.Source;

                    WriteObject(logDetailed?.Details, true);
                }
                catch (OrchException ex)
                {
                    WriteError(new ErrorRecord(ex, "GetAuditLogDetailsError", ErrorCategory.InvalidOperation, result.Target));
                }
            }

            // シングルスレッド版
            //foreach (var log in logs)
            //{
            //    try
            //    {
            //        var apiCalled = drive.GetAuditLogDetails(log);
            //        WriteObject(log?.Details, true);
            //        // Details を取得しようとすると、いつも同じ監査ログでエラーになる。
            //        // API call rate limit のためにエラーになるのかと思ったけど、
            //        // web interface でも同じ症状になるので、これは rate limit のせいじゃないな。
            //        // マルチスレッド化しておいて問題ないようだ。
            //        if (apiCalled) Thread.Sleep(600); 
            //    }
            //    catch (Exception ex)
            //    {
            //        WriteError(new ErrorRecord(new OrchException(System.IO.Path.Combine(drive.NameColonSeparator, log.Id!.Value.ToString()), ex), "GetAuditLogDetailsError", ErrorCategory.InvalidOperation, log));
            //    }
            //}
        }
        else
        {
            WriteObject(logs, true);
        }
    }

    protected override void ProcessRecord()
    {
        var drives = OrchDriveInfo.EnumOrchDrives(Path);

        // すべてのパラメータが指定されていなければ、キャッシュの内容を返す
        bool bOutCache = (
            Last is null &&
            Component is null &&
            UserName is null &&
            Action is null &&
            ExecutionTimeAfter is null &&
            ExecutionTimeBefore is null &&
            Skip is null && First is null);

        if (bOutCache)
        {
            WriteWarning("Since no filter parameters were specified, the contents of the cache will be output. To query the Orchestrator, please specify at least one filter parameter.");

            var wpId = Id.ConvertToWildcardPatternList();
            foreach (var drive in drives)
            {
                WriteLog(drive, drive._dicAuditLogs?.Values
                    .FilterByWildcards(log => (log?.Id ?? 0).ToString(), wpId)
                    .OrderByDescending(log => log.Id));
            }
            return;
        }

        ulong skip = Skip ?? 0;
        ulong first = First ?? ulong.MaxValue;

        string filter = MakeFilter();

        foreach (var drive in drives)
        {
            //if (ParameterSetName == "Filter")
            {
                try
                {
                    // Skip と First をサポートする cmdlet は、ここでソートしてはいけない
                    // サーバーから返された順を尊重しないと。
                    WriteLog(drive, drive.GetAuditLogs(filter, skip, first));
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetAuditLogError", ErrorCategory.InvalidOperation, drive));
                }
            }
            //else // ParameterSetName == "Id"
            //{
            //    if (drive._dicAuditLogs is null)
            //    {
            //        WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, "The -Id parameter searches only within the local cache. Please execute Get-OrchAuditLog without the -Id parameter first."), "GetAuditLogError", ErrorCategory.InvalidOperation, drive));
            //        continue;
            //    }
            //    WriteLog(drive, drive._dicAuditLogs.Values
            //        .FilterByWildcards(log => (log?.Id ?? 0).ToString(), wpId)
            //        .OrderByDescending(log => log.Id));
            //}
        }
    }
}
