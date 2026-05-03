using System.Collections;
using UiPath.PowerShell.Positional;
using System.Data;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchAuditLog", DefaultParameterSetName = "Filter")] //, SupportsPaging = true)]
[OutputType(typeof(AuditLog))]
[OutputType(typeof(AuditLogEntity))]
public class GetAuditLogCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ParameterSetName = "Filter", ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<Hour_Day_Week_Month_3Month_6Month_Year_3Year>))]
    [ValidateStaticCandidate<Hour_Day_Week_Month_3Month_6Month_Year_3Year>]
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
    [ArgumentCompleter(typeof(DriveCompleter))]
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
            var drives = ResolveOrchDrives(fakeBoundParameters);

            // Exclude Ids already selected by the parameter from the candidates
            var paramId = GetSelfExclusionValues(commandAst, "Id", wordToComplete);
            var wpId = paramId.ConvertToWildcardPatternList();

            var wp = CreateWPFromWordToComplete(wordToComplete);

            // Using the cache, so there is no need to use multiple threads
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
            var drives = ResolveOrchDrives(fakeBoundParameters);

            // Exclude UserNames already selected by the parameter from the candidates
            var paramUserName = GetSelfExclusionValues(commandAst, "UserName", wordToComplete);
            var wpUserName = paramUserName.ConvertToWildcardPatternList();

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drives, drive => drive.GetUsers());

            foreach (var result in results)
            {
                foreach (var user in result
                    .Where(user => wp.IsMatch(user.UserName))
                    .ExcludeByWildcards(user => user?.UserName, wpUserName)
                    .OrderBy(user => user.UserName))
                {
                    string tiphelp = TipHelp2(user);
                    yield return new CompletionResult(PathTools.EscapePSText(user.UserName), user.UserName, CompletionResultType.Text, tiphelp);
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
            foreach (var component in AuditLogComponentItems.Items.FilterByWildcards(st => st, wpComponent))
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
            var drives = SessionState.EnumOrchDrives(Path);
            var wpUserName = UserName.ConvertToWildcardPatternList();
            var userIds = new HashSet<Int64>();
            foreach (var drive in drives)
            {
                userIds.UnionWith(drive.GetUsers()
                    .FilterByWildcards(u => u?.UserName, wpUserName)
                    .Select(u => u.Id ?? 0));
            }
            // When UserName is specified but resolves to no users, narrow the result to nothing
            // rather than silently returning logs for all users.
            filter.Add(userIds.Count != 0
                ? "(" + string.Join("%20or%20", userIds.Select(id => $"(UserId%20eq%20{id})")) + ")"
                : "(UserId%20eq%20-1)");
        }
        #endregion

        #region Action
        if (Action is not null && Action.Length > 0)
        {
            var actions = new List<string>();
            var wpAction = Action.ConvertToWildcardPatternList();
            foreach (var action in AuditLogActionItems.Items.FilterByWildcards(st => st, wpAction))
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

    // Hmm? I recall there was something somewhere that seemed to need this...
    //private string DecodeBase64_ExtractGzip(string encodedData)
    //{
    //    // Base64 decode
    //    byte[] decodedData = Convert.FromBase64String(encodedData);

    //    // GZip decompress
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

            // Single-threaded version
            //foreach (var log in logs)
            //{
            //    try
            //    {
            //        var apiCalled = drive.GetAuditLogDetails(log);
            //        WriteObject(log?.Details, true);
            //        // When trying to get Details, it always fails on the same audit log entries.
            //        // I thought it might be due to API call rate limiting,
            //        // but the same issue occurs in the web interface, so it's not caused by rate limiting.
            //        // It appears safe to use multiple threads here.
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
        var drives = SessionState.EnumOrchDrives(Path);

        // If no parameters are specified, return the contents of the cache
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
            WriteWarning($"[{MyInvocation.MyCommand.Name}] Since no filter parameters were specified, the contents of the cache will be output. To query the Orchestrator, please specify at least one filter parameter.");

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
                    // Cmdlets that support Skip and First must not sort here;
                    // the order returned by the server must be respected.
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
