using System.Collections;
using System.Data;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchCalendarDate", SupportsShouldProcess = true)]
public class RemoveCalendarDateCmdlet : OrchestratorPSCmdlet
{
    private Dictionary<(OrchDriveInfo drive, string calendarName), (ExtendedCalendar calendar, List<DateTime> excludedDates)> _parameters = [];

    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(CalendarNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(CalendarDateCompleter))]
    public DateTime[]? ExcludedDate { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
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
            var drives = ResolveOrchDrives(fakeBoundParameters);

            var wpCalendarName = GetFakeBoundParameters(fakeBoundParameters, "Name").ConvertToWildcardPatternList();
            var paramExcludedDate = GetSelfExclusionValues(commandAst, parameterName, wordToComplete)
                .Select(dateStr =>
                    {
                        if (DateTime.TryParse(dateStr, out DateTime parsedDate)) { return parsedDate; }
                        else { return (DateTime?)null; }
                    })
                .Where(date => date.HasValue)
                .Select(date => date!.Value)
                .ToList();

            var calendarsResults = ParallelResults.GroupBy(drives, drive => drive.GetCalendars());

            foreach (var calendarsResult in calendarsResults)
            {
                var drive = calendarsResult.Source;

                var detailedCalendarResults = ParallelResults.ForEach(calendarsResult
                    .FilterByWildcards(c => c?.Name, wpCalendarName), calendar => drive.GetCalendar(calendar));

                foreach (var (calendar, calendarDetailed) in detailedCalendarResults)
                {
                    var targetDates = calendarDetailed.ExcludedDates?
                        .Where(d => d >= DateTime.Today)
                        .Select(d => d.Date)
                        .OrderBy(d => d);
                    if (targetDates is null) continue;

                    foreach (var date in targetDates)
                    {
                        if (paramExcludedDate.Contains(date)) continue; // Exclude already-specified dates
                        string dateString = date.Date.ToShortDateString();
                        string tiphelp = calendarDetailed.GetPSPath();
                        if (string.IsNullOrEmpty(tiphelp)) tiphelp = dateString;
                        yield return new CompletionResult(PathTools.EscapePSText(dateString), dateString, CompletionResultType.ParameterValue, tiphelp);
                    }
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumOrchDrives(Path);
        var wpName = Name.ConvertToWildcardPatternList();

        // Zero out the time component
        ExcludedDate = ExcludedDate!.Select(d => d.Date).ToArray();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives.WithCancellation(cancelHandler.Token))
        {
            ICollection<ExtendedCalendar> calendars = null;
            try
            {
                calendars = drive.GetCalendars();
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetCalendarError", ErrorCategory.InvalidOperation, drive));
                continue;
            }
            if (calendars is null) continue;

            var targetCalendars = calendars.FilterByWildcards(c => c?.Name, wpName);

            foreach (var targetCalendar in targetCalendars.WithCancellation(cancelHandler.Token))
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
        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var p in _parameters.WithCancellation(cancelHandler.Token))
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

            // Skip the API call if the date list has not changed from the original
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
                    drive.Calendars.ClearCache();
                    drive.CalendarsDetailed.ClearCache();

                    // updatedCalendar has incomplete data. Key is also empty.
                    // It's better not to call WriteObject().
                    //if (updatedCalendar is not null)
                    //{
                    //    updatedCalendar.Path = drive.NameColon;
                    //    WriteObject(updatedCalendar);
                    //    // The below doesn't work. PutCalendar returns incomplete data. TimeZoneId is null.
                    //    // We should clear the cache here instead.
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
