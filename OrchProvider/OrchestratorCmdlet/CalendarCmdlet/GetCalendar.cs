using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "OrchCalendar")]
    [OutputType(typeof(ExtendedCalendar))]
    [OutputType(typeof(ExcludedDateNamed))]
    public class GetCalendarCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(CalendarNameCompleter<TPositional>))]
        [SupportsWildcards]
        public string[]? Name { get; set; }

        [Parameter]
        public SwitchParameter ExpandExcludedDate { get; set; }

        [Parameter]
        public SwitchParameter IncludePastDate { get; set; }

        [Parameter]
        [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
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
            // 各カレンダーに対してデータ行を書き込む
            string[] line = [
                EscapeCsvValue(output.Path, true),
                EscapeCsvValue(output.Name, true),
                EscapeCsvValue(output.ExcludedDate?.ToShortDateString())
            ];
            WriteCsvLine(writer, line);
        }

        protected override void ProcessRecord()
        {
            var drives = OrchDriveInfo.EnumOrchDrives(Path);
            var wpName = Name.ConvertToWildcardPatternList();

            ExportCsv = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
            using var writer = WriteCsvHeader(ExportCsv, CsvEncoding, CsvHeaders);

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var drive in drives)
            {
                try
                {
                    var calendars = drive.GetCalendars();
                    var targetCalendars = calendars?.FilterByWildcards(c => c?.Name, wpName).OrderBy(c => c.Name).ToList();

                    if (targetCalendars == null || targetCalendars.Count == 0) continue;

                    if (!ExpandExcludedDate.IsPresent && writer == null)
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
                                if (detailedCalendar == null) continue;

                                if (detailedCalendar.ExcludedDates != null)
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

                                        if (writer != null) { WriteCsvContent(writer, output); }
                                        else                { WriteObject(output); }
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
                WriteCSVExportedMessage(this, ExportCsv);
            }
        }
    }
}
