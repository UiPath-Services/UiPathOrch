using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;
using TPositional = UiPath.PowerShell.Positional.Name_ExcludedDate;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Add, "OrchCalendarDate", SupportsShouldProcess = true)]
public class AddCalendarDateCommand : OrchestratorPSCmdlet
{
    private Dictionary<(OrchDriveInfo drive, string calendarName), (ExtendedCalendar calendar, List<DateTime> excludedDates)> _parameters = [];

    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(CalendarNameCompleter<TPositional>))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(CalendarDateCompleter))]
    public DateTime[]? ExcludedDate { get; set; }

    [Parameter]
    public SwitchParameter IncludePastDate { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(DriveCompleter<Positional.Name_ExcludedDate>))]
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
            var param = GetParameterValues(commandAst, parameterName, TPositional.Parameters, wordToComplete);

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
        var drives = SessionState.EnumOrchDrives(Path);

        // 指定された日時は、時刻成分をゼロにして UTC として解釈する
        ExcludedDate = ExcludedDate?.Select(d => DateTime.SpecifyKind(d.Date, DateTimeKind.Utc)).ToArray();

        // 今日以降の日付のみ追加する
        if (!IncludePastDate.IsPresent)
        {
            DateTime today = DateTime.Today;
            ExcludedDate = ExcludedDate?.Where(d => d >= today).ToArray(); // 昨日以前の日付を除外する
        }

        foreach (var drive in drives)
        {
            ICollection<ExtendedCalendar> calendars = null; ;
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

            foreach (var name in Name!)
            {
                var wpName = new WildcardPattern(name, WildcardOptions.IgnoreCase);
                var targetCalendars = calendars.Where(c => wpName.IsMatch(c.Name));
                if (!targetCalendars.Any()) // ひとつも合致しなければ、新規に作成する
                {
                    if (WildcardPattern.ContainsWildcardCharacters(name))
                    {
                        //WriteWarning($"{System.IO.Path.Combine(drive.NameColonSeparator, name)} did not match any existing calendars. A new calendar cannot be created with a name that contains wildcards.");
                        continue;
                    }
                    string unescapedName = WildcardPattern.Unescape(name);
                    if (_parameters.TryGetValue((drive, unescapedName), out var calendarExcludedDates))
                    {
                        var (calendar, excludedDates) = calendarExcludedDates; // calendar は null になっているはず
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

            if (calendar is null) // カレンダーを新規作成
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
                        drive._dicCalendars = null;
                        //if (createdCalendar is not null)
                        //{
                        //    createdCalendar.Path = drive.NameColon;
                        //    WriteObject(createdCalendar);
                        //    // 下記はうまくいかない。PostCalendar が返すデータに不足がある。TimeZoneId が null だ。
                        //    // ここでは、キャッシュを空にしておくべきだ。
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
            else // カレンダーを更新
            {
                string target = calendar.GetPSPath();

                var extendedCalendar = drive.GetCalendar(calendar); // 既存のカレンダーの ExcludedDate を取得
                if (extendedCalendar!.ExcludedDates is not null)
                {
                    //excludedDates?.AddRange(extendedCalendar.ExcludedDates.Select(d => d.ToUniversalTime()));
                    excludedDates?.AddRange(extendedCalendar.ExcludedDates);
                }

                #region 追加した日付リストが元と変化していなければ API を call しない
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
                        drive._dicCalendars = null;
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
}
