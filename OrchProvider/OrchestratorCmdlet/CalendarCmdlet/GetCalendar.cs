using System.Management.Automation;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchCalendar")]
[OutputType(typeof(ExtendedCalendar))]
[OutputType(typeof(ExcludedDateNamed))]
public class GetCalendarCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(CalendarNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter]
    public SwitchParameter ExpandExcludedDate { get; set; }

    [Parameter]
    public SwitchParameter IncludePastDate { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public string? ExportCsv { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(EncodingCompleter))]
    [EncodingArgumentTransformation]
    public Encoding? CsvEncoding { get; set; }

    private static readonly string DefaultCsvName = "ExportedCalendars.csv";
    private static readonly string[] CsvHeaders = ["Path", "Name", "ExcludedDate"];

    private static void WriteCsvContent(StreamWriter writer, ExcludedDateNamed output)
    {
        // Write a data row for each calendar
        string[] line = [
            EscapeCsvValue(output.Path, true),
            EscapeCsvValue(output.Name, true),
            EscapeCsvValue(output.ExcludedDate?.ToShortDateString())
        ];
        writer.WriteCsvLine(line);
    }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumOrchDrives(Path);
        var wpName = Name.ConvertToWildcardPatternList();

        var (physicalCsvPath, providerCsvPath) = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
        using var writer = WriteCsvHeader(physicalCsvPath, CsvEncoding, CsvHeaders);

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives)
        {
            try
            {
                var calendars = drive.GetCalendars();
                var targetCalendars = calendars?.FilterByWildcards(c => c?.Name, wpName).OrderBy(c => c.Name).ToList();

                if (targetCalendars is null || targetCalendars.Count == 0) continue;

                if (!ExpandExcludedDate.IsPresent && writer is null)
                {
                    WriteObject(targetCalendars, true);
                }
                else
                {
                    using var results = OrchThreadPool.RunForEach(targetCalendars,
                        calendar => calendar.GetPSPath(),
                        calendar => calendar,
                        calendar => drive.GetCalendar(calendar));

                    foreach (var result in results)
                    {
                        try
                        {
                            var detailedCalendar = result.GetResult(cancelHandler.Token);
                            if (detailedCalendar is null) continue;

                            if (detailedCalendar.ExcludedDates is not null)
                            {
                                string pathName = detailedCalendar.GetPSPath();
                                foreach (var date in detailedCalendar.ExcludedDates
                                    .Where(d => (IncludePastDate.IsPresent || d >= DateTime.Today))
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
                                    else                    { WriteObject(output); }
                                }
                            }
                        }
                        catch (OrchException ex)
                        {
                            WriteError(new ErrorRecord(ex, "GetCalendarError", ErrorCategory.InvalidOperation, ex.Target));
                            continue;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetCalendarError", ErrorCategory.InvalidOperation, drive));
                continue;
            }
        }

        if (!string.IsNullOrEmpty(ExportCsv))
        {
            WriteCSVExportedMessage(this, providerCsvPath);
        }
    }
}
