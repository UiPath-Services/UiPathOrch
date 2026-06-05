using System.Collections;
using UiPath.PowerShell.Positional;
using System.Data;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchAlert")]
[OutputType(typeof(Entities.Alert))]
public class GetAlertCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<Hour_Day_Week_Month_3Month_6Month_Year_3Year>))]
    [ValidateStaticCandidate<Hour_Day_Week_Month_3Month_6Month_Year_3Year>]
    public string? Last { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(SeverityCompleter))]
    public string? Severity { get; set; }

    [Parameter(Position = 2, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(KeyOfDictionaryCompleter<AlertComponentItems, int>))]
    [SupportsWildcards]
    public string[]? Component { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TimeAfterCompleter))]
    public DateTime? CreationTimeAfter { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TimeBeforeCompleter))]
    public DateTime? CreationTimeBefore { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public ulong? Skip { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<Item10>))]
    public ulong? First { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    private class SeverityCompleter : OrchArgumentCompleter
    {
        private static readonly Dictionary<string, string> candidates = new()
            {
                { "Info",    "Fatal + Error + Warn + Success + Info (All)" },
                { "Success", "Fatal + Error + Warn + Success" },
                { "Warn",    "Fatal + Error + Warn" },
                { "Error",   "Fatal + Error" },
                { "Fatal",   "Fatal Only" }
            };

        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var wp = CreateWPFromWordToComplete(wordToComplete);

            foreach (var candidate in candidates
                .Where(c => wp.IsMatch(c.Key)))
            {
                yield return new CompletionResult(candidate.Key, candidate.Key, CompletionResultType.ParameterValue, candidate.Value);
            }
        }
    }

    private string? MakeFilter()
    {
        List<string> filter = [];

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
            filter.Add($"(CreationTime%20ge%20{last:yyyy-MM-ddTHH:mm:ss.fffZ})");
        }
        #endregion

        #region Success
        Severity ??= "Success";
        filter.AddIfNotNull(AlertSeverityItems.Items
            .SelectByWildcards(i => i.Key, [Severity])
            .CreateOrFilter(i => $"Severity ge '{i.Value}'"));
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
            filter.Add($"(CreationTime%20lt%20{CreationTimeBefore.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ})");
        }
        #endregion

        filter.AddIfNotNull(AlertComponentItems.Items
            .SelectByWildcards(i => i.Key, Component)
            .CreateOrFilter(i => $"Component eq '{i.Value}'"));

        string ret = "&$orderby=CreationTime desc";
        if (filter.Count != 0)
        {
            var strFilter = string.Join(" and ", filter);
            ret += "&$filter=(" + strFilter + ")";
        }
        return ret;
    }

    protected override void ProcessRecord()
    {
        ulong skip = Skip ?? 0;
        ulong first = First ?? ulong.MaxValue;

        var drives = SessionState.EnumOrchDrives(EffectivePath(Path, LiteralPath));
        var query = MakeFilter();

        using var results = OrchThreadPool.RunForEach(drives,
            drive => drive.NameColonSeparator,
            drive => drive,
            drive => drive.Alerts.Fetch(query, skip, first)
        );

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var alerts = result.GetResult(cancelHandler.Token);
                if (alerts is null) continue;

                WriteObject(alerts, true);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetAlertError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
