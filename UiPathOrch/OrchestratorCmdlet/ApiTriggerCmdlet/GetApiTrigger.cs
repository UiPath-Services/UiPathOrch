using System.Management.Automation;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchApiTrigger")]
[OutputType(typeof(HttpTrigger))]
public class GetApiTriggerCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(ApiTriggerNameCompleter))]
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

    private static readonly string DefaultCsvName = "ExportedApiTriggers.csv";

    // Column names line up with New-OrchApiTrigger / Update-OrchApiTrigger
    // parameter names so the exported CSV can be re-imported via
    // Import-Csv | New-OrchApiTrigger (round-trip).
    private static readonly string[] CsvHeaders = [
        "Path",
        "Name",
        "Release",
        "Description",
        "Enabled",
        "Method",
        "Slug",
        "CallingMode",
        "RunAsCaller",
        "RuntimeType",
        "ResumeOnSameContext",
        "StopStrategy",
        "StopJobAfterSeconds",
        "KillJobAfterSeconds",
        "AlertPendingJobAfterSeconds",
        "AlertRunningJobAfterSeconds",
        "RemoteControlAccess",
        "ConsecutiveJobFailuresThreshold",
        "InputArguments",
        "MachineRobots",
    ];

    // Cache (drive, ReleaseKey) -> ReleaseName lookups within one ProcessRecord
    // run so we don't fetch the Releases listing once per trigger row.
    private readonly Dictionary<(OrchDriveInfo, string), string?> _releaseNameCache = new();

    private string? ResolveReleaseName(OrchDriveInfo drive, Folder folder, string? releaseKey)
    {
        if (string.IsNullOrEmpty(releaseKey)) return null;
        if (_releaseNameCache.TryGetValue((drive, releaseKey), out var cached)) return cached;

        string? name = null;
        try
        {
            var releases = drive.Releases.Get(folder);
            name = releases.FirstOrDefault(r => string.Equals(r.Key, releaseKey, StringComparison.OrdinalIgnoreCase))?.Name;
        }
        catch
        {
            // Swallow: a missing Release is a soft failure for export; the
            // CSV row just lacks the name (the user can still re-import
            // by ReleaseKey directly via a separate column if needed).
        }
        _releaseNameCache[(drive, releaseKey)] = name;
        return name;
    }

    private void WriteCsvContent(StreamWriter writer, OrchDriveInfo drive, Folder folder, IEnumerable<HttpTrigger> triggers)
    {
        foreach (var t in triggers)
        {
            var release = ResolveReleaseName(drive, folder, t.ReleaseKey);
            var machineRobots = SerializeMachineRobotSessions(drive, folder, t.MachineRobots);

            string[] line = [
                EscapeCsvValue(t.Path, true),
                EscapeCsvValue(t.Name, true),
                EscapeCsvValue(release),
                EscapeCsvValue(t.Description),
                EscapeCsvValue(t.Enabled),
                EscapeCsvValue(t.Method),
                EscapeCsvValue(t.Slug),
                EscapeCsvValue(t.CallingMode),
                EscapeCsvValue(t.RunAsCaller),
                EscapeCsvValue(t.RuntimeType),
                EscapeCsvValue(t.ResumeOnSameContext),
                EscapeCsvValue(t.StopStrategy),
                EscapeCsvValue(t.StopJobAfterSeconds),
                EscapeCsvValue(t.KillJobAfterSeconds),
                EscapeCsvValue(t.AlertPendingJobAfterSeconds),
                EscapeCsvValue(t.AlertRunningJobAfterSeconds),
                EscapeCsvValue(t.RemoteControlAccess),
                EscapeCsvValue(t.ConsecutiveJobFailuresThreshold),
                EscapeCsvValue(t.InputArguments),
                EscapeCsvValue(machineRobots),
            ];
            writer.WriteCsvLine(line);
        }
    }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(EffectivePath(Path, LiteralPath), Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();

        var (physicalCsvPath, providerCsvPath) = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
        using var writer = WriteCsvHeader(physicalCsvPath, CsvEncoding, CsvHeaders);

        using var results = OrchThreadPool.RunForEach(drivesFolders,
            df => df.folder.GetPSPath(),
            df => df,
            df => df.drive.ApiTriggers.Get(df.folder));

        using var cancelHandler = new ConsoleCancelHandler();
        using var reporter = new ProgressReporter(this, 1, results.Count, "Getting API triggers");
        foreach (var result in results)
        {
            try
            {
                var triggers = results.GetResultWithProgress(result, reporter, cancelHandler.Token);
                if (triggers is null) continue;

                var filtered = triggers
                    .FilterByWildcards(s => s?.Name, wpName)
                    .OrderBy(s => s.Name);

                if (writer is not null)
                {
                    // result.Source is the (drive, folder) tuple we
                    // submitted to RunForEach. Use it directly rather
                    // than rewalking SessionState.EnumFolders for the
                    // same path.
                    WriteCsvContent(writer, result.Source.drive, result.Source.folder, filtered);
                }
                else
                {
                    foreach (var t in filtered)
                    {
                        // The triggers endpoint rejects $expand=Release, so Release is null and the
                        // default table's Release column would be blank. Fill it from the same
                        // ReleaseKey->name lookup the CSV path uses (cached per run).
                        if (t.Release is null && !string.IsNullOrEmpty(t.ReleaseKey))
                        {
                            var releaseName = ResolveReleaseName(result.Source.drive, result.Source.folder, t.ReleaseKey);
                            if (releaseName is not null)
                            {
                                t.Release = new TriggerRelease { Name = releaseName };
                            }
                        }
                        WriteObject(t);
                    }
                }
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetApiTriggerError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }

        if (writer is not null)
        {
            WriteCSVExportedMessage(this, providerCsvPath);
        }
    }
}
