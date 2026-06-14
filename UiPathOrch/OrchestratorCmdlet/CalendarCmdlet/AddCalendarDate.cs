using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Add, "OrchCalendarDate", SupportsShouldProcess = true)]
public class AddCalendarDateCmdlet : OrchestratorPSCmdlet
{
    private Dictionary<(OrchDriveInfo drive, string calendarName), (ExtendedCalendar calendar, List<DateTime> excludedDates)> _parameters = [];

    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(CalendarNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(CalendarDateCompleter))]
    public DateTime[]? ExcludedDate { get; set; }

    [Parameter]
    public SwitchParameter IncludePastDate { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(DriveCompleter))]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    private class CalendarDateCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var param = GetSelfExclusionValues(commandAst, parameterName, wordToComplete);

            if (!DateTime.TryParse(param?.LastOrDefault(), out DateTime theLast))
            {
                theLast = DateTime.Today;
            }
            DateTime dt = theLast.AddDays(7);
            yield return new CompletionResult($"'{dt.ToShortDateString()}'");
        }
    }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumOrchDrives(EffectivePath(Path, LiteralPath));

        // Zero out the time component of the specified dates and interpret them as UTC
        ExcludedDate = ExcludedDate?.Select(d => DateTime.SpecifyKind(d.Date, DateTimeKind.Utc)).ToArray();

        // Only add dates from today onward. ExcludedDate values were stamped as UTC above,
        // so compare against UTC-based today to avoid an off-by-one day at TZ boundaries.
        if (!IncludePastDate.IsPresent)
        {
            DateTime today = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
            ExcludedDate = ExcludedDate?.Where(d => d >= today).ToArray();
        }

        foreach (var drive in drives)
        {
            ICollection<ExtendedCalendar> calendars = null;
            try
            {
                calendars = drive.Calendars.Get();
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetCalendarError", ErrorCategory.InvalidOperation, drive));
                continue;
            }
            if (calendars is null) continue;

            foreach (var name in Name!)
            {
                var wpName = new WildcardPattern(name, WildcardOptions.IgnoreCase);
                var targetCalendars = calendars.Where(c => wpName.IsMatch(c.Name));
                if (!targetCalendars.Any()) // If no matches, create a new calendar
                {
                    if (WildcardPattern.ContainsWildcardCharacters(name))
                    {
                        WriteWarning($"\"{System.IO.Path.Combine(drive.NameColonSeparator, name)}\": no existing calendar matched, and a new calendar cannot be created with a name that contains wildcard characters. "
                            + $"To create a calendar with this literal name, escape the wildcard characters with a backtick: -Name '{WildcardPattern.Escape(name)}'.");
                        continue;
                    }
                    string unescapedName = WildcardPattern.Unescape(name);
                    if (_parameters.TryGetValue((drive, unescapedName), out var calendarExcludedDates))
                    {
                        var (calendar, excludedDates) = calendarExcludedDates; // calendar should be null at this point
                        excludedDates.AddRange(ExcludedDate ?? []);
                    }
                    else
                    {
                        _parameters[(drive, unescapedName)] = (null, ExcludedDate?.ToList() ?? [])!;
                    }
                }
                else
                {
                    foreach (var targetCalendar in targetCalendars)
                    {
                        if (_parameters.TryGetValue((drive, targetCalendar.Name!), out var calendarExcludedDates))
                        {
                            var (calendar, excludedDates) = calendarExcludedDates;
                            excludedDates?.AddRange(ExcludedDate ?? []);
                        }
                        else
                        {
                            _parameters[(drive, targetCalendar.Name!)] = (targetCalendar, ExcludedDate?.ToList() ?? []);
                        }
                    }
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

            if (calendar is null) // Create a new calendar
            {
                string target = System.IO.Path.Combine(drive.NameColonSeparator, calendarName);
                if (ShouldProcess(target, "Add Calendar"))
                {
                    try
                    {
                        ExtendedCalendar postingCalendar = new()
                        {
                            Name = calendarName,
                            ExcludedDates = excludedDates?
                                .Distinct()
                                .OrderBy(d => d)
                                .ToArray() ?? []
                        };
                        var createdCalendar = drive.OrchAPISession.PostCalendar(postingCalendar);
                        drive.Calendars.ClearCache();
                        drive.CalendarsDetailed.ClearCache();
                        //if (createdCalendar is not null)
                        //{
                        //    createdCalendar.Path = drive.NameColon;
                        //    WriteObject(createdCalendar);
                        //    // The below doesn't work. PostCalendar returns incomplete data. TimeZoneId is null.
                        //    // We should clear the cache here instead.
                        //    //drive._dicCalendars ??= [];
                        //    //drive._dicCalendars[createdCalendar.Id!.Value] = createdCalendar;
                        //}
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(target, ex), "AddCalendarError", ErrorCategory.InvalidOperation, drive));
                    }
                }
            }
            else // Update the calendar
            {
                string target = calendar.GetPSPath();

                var extendedCalendar = drive.CalendarsDetailed.Get(calendar.Id!.Value); // Get ExcludedDates from the existing calendar
                if (extendedCalendar!.ExcludedDates is not null)
                {
                    // The existing ExcludedDates are read back as local time (DateTimeArrayJsonConverter),
                    // while the user's input was stamped UTC above. Normalize the existing dates to UTC
                    // too, so Distinct() and the no-op guard below see the same calendar day as one
                    // instant -- otherwise re-adding a date that is already excluded looks "new" and
                    // issues a needless PUT carrying a duplicated date.
                    excludedDates?.AddRange(extendedCalendar.ExcludedDates.Select(d => d.ToUniversalTime()));
                }

                #region Skip the API call if the date list has not changed from the original
                excludedDates = excludedDates?.Distinct().ToList();
                if (excludedDates?.Count == extendedCalendar?.ExcludedDates?.Length) continue;
                #endregion

                if (ShouldProcess(target, "Update Calendar"))
                {
                    try
                    {
                        ExtendedCalendar postingCalendar = new()
                        {
                            Id = calendar.Id,
                            Name = calendarName,
                            ExcludedDates = excludedDates?
                                .OrderBy(d => d)
                                .ToArray() ?? []
                        };

                        var updatedCalendar = drive.OrchAPISession.PutCalendar(postingCalendar);
                        drive.Calendars.ClearCache();
                        drive.CalendarsDetailed.ClearCache();
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
}
