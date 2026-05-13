using System.Management.Automation;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchCalendar")]
[OutputType(typeof(ExtendedCalendar))]
[OutputType(typeof(ExcludedDateNamed))]
public class GetCalendarCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(CalendarNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    // Deprecated. Routes to Get-OrchCalendarDate via the shared helper. Kept
    // for backward compat; will be removed in a future major release.
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

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumOrchDrives(Path);
        var wpName = Name.ConvertToWildcardPatternList();

        // Both -ExpandExcludedDate and -ExportCsv historically emitted
        // ExcludedDateNamed rows (CSV header [Path,Name,ExcludedDate]) so
        // the file round-trips with Add-OrchCalendarDate. Both paths now
        // delegate to Get-OrchCalendarDate via the shared helper and emit
        // a deprecation warning pointing the user there. The CSV format
        // itself is unchanged, so existing exported files keep importing.
        bool useDetailPath = ExpandExcludedDate.IsPresent || !string.IsNullOrEmpty(ExportCsv);

        if (useDetailPath)
        {
            string trigger = ExpandExcludedDate.IsPresent ? "'-ExpandExcludedDate'" : "'-ExportCsv'";
            string suggestion = !string.IsNullOrEmpty(ExportCsv)
                ? "Use 'Get-OrchCalendarDate -ExportCsv' instead."
                : "Use 'Get-OrchCalendarDate' instead.";
            WriteWarning(
                $"{trigger} on Get-OrchCalendar is deprecated and will be removed in a future " +
                $"major release. {suggestion}");

            var (physicalCsvPath, providerCsvPath) = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
            using var writer = WriteCsvHeader(physicalCsvPath, CsvEncoding, GetCalendarDateCmdlet.CsvHeaders);

            GetCalendarDateCmdlet.EmitCalendarDates(this, drives, wpName, IncludePastDate.IsPresent, writer);

            if (!string.IsNullOrEmpty(ExportCsv))
            {
                WriteCSVExportedMessage(this, providerCsvPath);
            }
            return;
        }

        // List-only path: emit shallow ExtendedCalendar entries from each drive.
        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives)
        {
            try
            {
                var calendars = drive.GetCalendars();
                var targetCalendars = calendars?
                    .FilterByWildcards(c => c?.Name, wpName)
                    .OrderBy(c => c.Name)
                    .ToList();

                if (targetCalendars is null || targetCalendars.Count == 0) continue;

                WriteObject(targetCalendars, true);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetCalendarError", ErrorCategory.InvalidOperation, drive));
                continue;
            }
        }
    }
}
