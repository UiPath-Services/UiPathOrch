using System.Collections;
using System.Data;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using TPositional = UiPath.PowerShell.Positional.Name_ExcludedDate;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchCalendarDate", SupportsShouldProcess = true)]
public class RemoveCalendarDateCommand : OrchestratorPSCmdlet
{
    private Dictionary<(OrchDriveInfo drive, string calendarName), (ExtendedCalendar calendar, List<DateTime> excludedDates)> _parameters = [];

    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(CalendarNameCompleter<TPositional>))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(CalendarDateCompleter))]
    public DateTime[]? ExcludedDate { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    private class CalendarDateCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drives = ResolveDrives(fakeBoundParameters);

            var wpCalendarName = CreateWPListFromOtherParameters(commandAst, "Name", TPositional.Parameters);
            var paramExcludedDate = GetParameterValues(commandAst, parameterName, TPositional.Parameters, wordToComplete)
                .Select(dateStr =>
                    {
                        if (DateTime.TryParse(dateStr, out DateTime parsedDate)) { return parsedDate; }
                        else                                                     { return (DateTime?)null; }
                    })
                .Where(date => date.HasValue)
                .Select(date => date!.Value)
                .ToList();

            var calendarsResults = ParallelResults.ForEach(drives, drive => drive.GetCalendars());

            foreach (var calendarsResult in calendarsResults)
            {
                if (calendarsResult.Result is null) continue;

                var drive = calendarsResult.Source;

                var detailedCalendarResults = ParallelResults.ForEach(calendarsResult.Result
                    .FilterByWildcards(c => c?.Name, wpCalendarName), calendar => drive.GetCalendar(calendar));
                foreach (var detailedCalendarResult in detailedCalendarResults)
                {
                    if (detailedCalendarResult.Result is null) continue;

                    var targetDates = detailedCalendarResult.Result.ExcludedDates?
                        .Where(d => d >= DateTime.Today)
                        .Select(d => d.Date)
                        .OrderBy(d => d);
                    if (targetDates is null) continue;

                    foreach (var date in targetDates)
                    {
                        if (paramExcludedDate.Contains(date)) continue; // 指定済みの日付を除外
                        string dateString = date.Date.ToShortDateString();
                        string tiphelp = detailedCalendarResult.Result.GetPSPath();
                        if (string.IsNullOrEmpty(tiphelp)) tiphelp = dateString;
                        yield return new CompletionResult(PathTools.EscapePSText(dateString), dateString, CompletionResultType.ParameterValue, tiphelp);
                    }
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drives = OrchDriveInfo.EnumOrchDrives(Path);
        var wpName = Name.ConvertToWildcardPatternList();

        // 時刻成分をゼロにしておく
        ExcludedDate = ExcludedDate!.Select(d => d.Date).ToArray();

        foreach (var drive in drives)
        {
            ICollection<ExtendedCalendar> calendars = null;
            try
            {
                calendars = drive.GetCalendars();
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "GetCalendarError", ErrorCategory.InvalidOperation, drive));
                continue;
            }
            if (calendars is null) continue;

            var targetCalendars = calendars.FilterByWildcards(c => c?.Name, wpName);

            foreach (var targetCalendar in targetCalendars)
            {
                if (_parameters.TryGetValue((drive, targetCalendar.Name!), out var calendarExcludedDates))
                {
                    var (calendar, excludedDates) = calendarExcludedDates;
                    excludedDates?.AddRange(ExcludedDate ?? []);
                }
                else
                {
                    var detailedCalendar = drive.GetCalendar(targetCalendar);
                    _parameters[(drive, targetCalendar.Name!)] = (detailedCalendar!, ExcludedDate?.ToList() ?? []);
                }
            }
        }
    }

    protected override void EndProcessing()
    {
        foreach (var p in _parameters)
        {
            var (drive, calendarName) = p.Key;
            var (calendar, excludedDates) = p.Value;

            if (calendar.ExcludedDates is null) continue;
            excludedDates = excludedDates.Select(d => d.ToLocalTime()).ToList();

            DateTime[] keepDates = calendar.ExcludedDates
                .Except(excludedDates)
                .Select(d => d.ToUniversalTime())
                .OrderBy(d => d)
                .ToArray();

            // 追加した日付リストが元と変化していなければ API を call しない
            if (keepDates.Length == calendar.ExcludedDates.Length) continue;

            string target = calendar.GetPSPath();
            if (ShouldProcess(target, "Update Calendar"))
            {
                try
                {
                    ExtendedCalendar postingCalendar = new()
                    {
                        Id = calendar.Id,
                        Name = calendarName,
                        ExcludedDates = keepDates
                    };

                    var updatedCalendar = drive.OrchAPISession.PutCalendar(postingCalendar);
                    drive._dicCalendars = null;

                    // updatedCalendar の内容が不足している。Key も空っぽだ。
                    // WriteObject() はしない方が良い。
                    //if (updatedCalendar is not null)
                    //{
                    //    updatedCalendar.Path = drive.NameColon;
                    //    WriteObject(updatedCalendar);
                    //    // 下記はうまくいかない。PutCalendar が返すデータに不足がある。TimeZoneId が null だ。
                    //    // ここでは、キャッシュを空にしておくべきだ。
                    //    //drive._dicCalendars ??= [];
                    //    //drive._dicCalendars[updatedCalendar.Id!.Value] = updatedCalendar;
                    //}
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(target, ex), "UpdateCalendarError", ErrorCategory.InvalidOperation, drive));
                }
            }
        }
    }
}
