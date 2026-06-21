using System.Management.Automation;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchTestSetSchedule")]
[OutputType(typeof(TestSetSchedule))]
public class GetTestSetScheduleCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TestScheduleNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    [Parameter]
    public string? ExportCsv { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(EncodingCompleter))]
    [EncodingArgumentTransformation]
    public Encoding? CsvEncoding { get; set; }

    private static readonly string DefaultCsvName = "ExportedTestSetSchedules.csv";

    // Columns align with New-OrchTestSetSchedule parameter names so
    // Get | Export-Csv | Import-Csv | New-OrchTestSetSchedule round-trips.
    // TestSetName / CalendarName are emitted as human-readable values
    // rather than IDs; the LIST endpoint usually doesn't populate them
    // by default, so the cross-resolution below falls back to looking up
    // by TestSetId / CalendarId. Same pattern as Get-OrchQueue -ExportCsv
    // resolves ReleaseId → Release name.
    private static readonly string[] CsvHeaders = [
        "Path",
        "Name",
        "Description",
        "Enabled",
        "TestSetName",
        "CronExpression",
        "TimeZoneId",
        "CalendarName",
    ];

    private void WriteCsvContent(StreamWriter writer, OrchDriveInfo drive, Folder folder, IEnumerable<TestSetSchedule> schedules)
    {
        // Build per-folder name lookup tables once; ResolveTestSetName /
        // ResolveCalendarName below reuse them across all rows in the
        // folder so we don't re-fetch per-row.
        Lazy<Dictionary<long, string>> testSetIdToName = new(() =>
        {
            try
            {
                return drive.TestSets.Get(folder)
                    .Where(t => t.Id is not null && !string.IsNullOrEmpty(t.Name))
                    .ToDictionary(t => t.Id!.Value, t => t.Name!);
            }
            catch
            {
                return new Dictionary<long, string>();
            }
        });
        Lazy<Dictionary<long, string>> calendarIdToName = new(() =>
        {
            try
            {
                return drive.Calendars.Get()
                    .Where(c => c.Id is not null && !string.IsNullOrEmpty(c.Name))
                    .ToDictionary(c => c.Id!.Value, c => c.Name!);
            }
            catch
            {
                return new Dictionary<long, string>();
            }
        });

        foreach (var s in schedules)
        {
            // Prefer the server-provided field if non-empty; otherwise
            // fall back to the per-folder lookup. Either way, the CSV
            // cell carries a human-readable name suitable for re-import.
            string? testSetName = !string.IsNullOrEmpty(s.TestSetName)
                ? s.TestSetName
                : (s.TestSetId is not null && testSetIdToName.Value.TryGetValue(s.TestSetId.Value, out var tn) ? tn : null);
            string? calendarName = !string.IsNullOrEmpty(s.CalendarName)
                ? s.CalendarName
                : (s.CalendarId is not null && calendarIdToName.Value.TryGetValue(s.CalendarId.Value, out var cn) ? cn : null);

            string[] line = [
                EscapeCsvValue(s.Path, true),
                EscapeCsvValue(s.Name, true),
                EscapeCsvValue(s.Description),
                EscapeCsvValue(s.Enabled),
                EscapeCsvValue(testSetName),
                EscapeCsvValue(s.CronExpression),
                EscapeCsvValue(s.TimeZoneId),
                EscapeCsvValue(calendarName),
            ];
            writer.WriteCsvLine(line);
        }
    }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFoldersWithoutPersonalWorkspace(EffectivePath(Path, LiteralPath), Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();

        var (physicalCsvPath, providerCsvPath) = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
        using var writer = WriteCsvHeader(physicalCsvPath, CsvEncoding, CsvHeaders);

        using var results = OrchThreadPool.RunForEach(drivesFolders,
            df => df.folder.GetPSPath(),
            df => df,
            df => df.drive.TestSetSchedules.Get(df.folder));

        using var cancelHandler = new ConsoleCancelHandler();
        using var reporter = new ProgressReporter(this, 1, results.Count, "Getting test schedules");
        foreach (var result in results)
        {
            try
            {
                var entities = results.GetResultWithProgress(result, reporter, cancelHandler.Token);
                if (entities is null) continue;

                var filtered = entities
                    .FilterByWildcards(ts => ts?.Name, wpName)
                    .OrderBy(ts => ts.Name);

                if (writer is not null) { WriteCsvContent(writer, result.Source.drive, result.Source.folder, filtered); }
                else { WriteObject(filtered, true); }
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetTestSetScheduleError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }

        if (writer is not null)
        {
            WriteCSVExportedMessage(this, providerCsvPath);
        }
    }
}
