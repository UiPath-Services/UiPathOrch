using System.Management.Automation;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchCalendarDate")]
[OutputType(typeof(ExcludedDateNamed))]
public class GetCalendarDateCmdlet : OrchestratorPSCmdlet
{
    // -Name is Mandatory by design — the detail path makes one API call per
    // matched calendar, so accidental fan-out from a default "all calendars"
    // would be expensive on large tenants. Wildcards (including "*") still
    // work; the user just has to type the selector explicitly.
    [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(CalendarNameCompleter))]
    [SupportsWildcards]
    public string[] Name { get; set; } = default!;

    [Parameter]
    public SwitchParameter IncludePastDate { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    [Parameter]
    public string? ExportCsv { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(EncodingCompleter))]
    [EncodingArgumentTransformation]
    public Encoding? CsvEncoding { get; set; }

    private static readonly string DefaultCsvName = "ExportedCalendarDates.csv";
    internal static readonly string[] CsvHeaders = ["Path", "Name", "ExcludedDate"];

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumOrchDrives(EffectivePath(Path, LiteralPath));
        var wpName = Name.ConvertToWildcardPatternList();

        var (physicalCsvPath, providerCsvPath) = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
        using var writer = WriteCsvHeader(physicalCsvPath, CsvEncoding, CsvHeaders);

        EmitCalendarDates(this, drives, wpName, IncludePastDate.IsPresent, writer);

        if (!string.IsNullOrEmpty(ExportCsv))
        {
            WriteCSVExportedMessage(this, providerCsvPath);
        }
    }

    /// <summary>
    /// Canonical implementation for "fetch each matched calendar's detail
    /// and emit one ExcludedDateNamed per excluded date". Called by this
    /// cmdlet's ProcessRecord, by GetCalendarCmdlet's deprecated
    /// -ExpandExcludedDate path, and by GetCalendarCmdlet's -ExportCsv
    /// path (the existing CSV format must round-trip with
    /// Add-OrchCalendarDate, so it stays supported as-is).
    /// </summary>
    internal static void EmitCalendarDates(
        OrchestratorPSCmdlet caller,
        IEnumerable<OrchDriveInfo> drives,
        List<WildcardPattern>? calendarWildcards,
        bool includePastDate,
        StreamWriter? writer)
    {
        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives)
        {
            try
            {
                var calendars = drive.Calendars.Get();
                var targetCalendars = calendars?
                    .FilterByWildcards(c => c?.Name, calendarWildcards)
                    .OrderBy(c => c.Name)
                    .ToList();

                if (targetCalendars is null || targetCalendars.Count == 0) continue;

                using var results = OrchThreadPool.RunForEach(targetCalendars,
                    calendar => calendar.GetPSPath(),
                    calendar => calendar,
                    calendar => drive.CalendarsDetailed.Get(calendar.Id!.Value));

                foreach (var result in results)
                {
                    try
                    {
                        var detailedCalendar = result.GetResult(cancelHandler.Token);
                        if (detailedCalendar is null) continue;
                        if (detailedCalendar.ExcludedDates is null) continue;

                        string pathName = detailedCalendar.GetPSPath();
                        foreach (var date in detailedCalendar.ExcludedDates
                            .Where(d => includePastDate || d >= DateTime.Today)
                            .OrderBy(d => d))
                        {
                            var output = new ExcludedDateNamed()
                            {
                                Path = drive.NameColonSeparator,
                                Name = detailedCalendar.Name,
                                PathName = pathName,
                                ExcludedDate = date
                            };

                            if (writer is not null) { WriteCsvContent(writer, output); }
                            else { caller.WriteObject(output); }
                        }
                    }
                    catch (OrchException ex)
                    {
                        caller.WriteError(new ErrorRecord(ex, "GetCalendarDateError", ErrorCategory.InvalidOperation, ex.Target));
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                caller.WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetCalendarDateError", ErrorCategory.InvalidOperation, drive));
                continue;
            }
        }
    }

    private static void WriteCsvContent(StreamWriter writer, ExcludedDateNamed output)
    {
        string[] line = [
            EscapeCsvValue(output.Path, true),
            EscapeCsvValue(output.Name, true),
            EscapeCsvValue(output.ExcludedDate?.ToShortDateString())
        ];
        writer.WriteCsvLine(line);
    }
}
