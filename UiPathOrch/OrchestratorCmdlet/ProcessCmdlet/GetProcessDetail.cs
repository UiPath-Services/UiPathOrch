using System.Management.Automation;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchProcessDetail")]
[OutputType(typeof(Release))]
public class GetProcessDetailCmdlet : OrchestratorPSCmdlet
{
    // -Name is Mandatory by design — the detail path makes one API call per
    // matched release, so accidental fan-out from a default "all releases"
    // would be expensive on large folders. Wildcards (including "*") still
    // work; the user just has to type the selector explicitly.
    [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(ProcessNameCompleter))]
    [SupportsWildcards]
    public string[] Name { get; set; } = default!;

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

    private static readonly string DefaultCsvName = "ExportedProcesses.csv";
    internal static readonly string[] CsvHeaders = [
        "Path",
        "Name",
        "Id",
        "Version",
        "Description",
        "EntryPoint",
        "InputArguments",
        "SpecificPriorityValue",
        "HiddenForAttendedUser",
        "RemoteControlAccess",
        "RetentionAction",
        "RetentionPeriod",
        "RetentionBucket",
        "StaleRetentionAction",
        "StaleRetentionPeriod",
        "StaleRetentionBucket",
        "ErrorRecordingEnabled",
        "Quality",
        "Frequency",
        "Duration",
        "AutoStartProcess",
        "AlwaysRunning",
        "A4R_Enabled",
        "A4R_HealingEnabled",
        "VideoRecordingType",
        "QueueItemVideoRecordingType",
        "MaxDurationSeconds",
        "Tags"
    ];

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(EffectivePath(Path, LiteralPath), Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();

        var (physicalCsvPath, providerCsvPath) = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
        using var writer = WriteCsvHeader(physicalCsvPath, CsvEncoding, CsvHeaders);

        EmitDetailedReleases(this, drivesFolders, wpName, writer);

        if (!string.IsNullOrEmpty(ExportCsv))
        {
            WriteCSVExportedMessage(this, providerCsvPath);
        }
    }

    /// <summary>
    /// Canonical implementation for "fetch each matched release's detail
    /// (plus EntryPointPath and retention enrichment) and emit either to
    /// caller.WriteObject or to the supplied CSV writer". Called by this
    /// cmdlet's ProcessRecord, by GetProcessCmdlet's deprecated
    /// -ExpandDetails path, and by GetProcessCmdlet's -ExportCsv path.
    /// </summary>
    internal static void EmitDetailedReleases(
        OrchestratorPSCmdlet caller,
        IEnumerable<(OrchDriveInfo drive, Folder folder)> drivesFolders,
        List<WildcardPattern>? nameWildcards,
        StreamWriter? writer)
    {
        FolderFanOut.Emit<Release, Release>(
            caller, drivesFolders, "GetProcessDetailError",
            listActivity: "Listing processes",
            fetchActivity: "Getting process details",
            list: (drive, folder) => drive.Releases.Get(folder)
                .FilterByWildcards(r => r?.Name, nameWildcards)
                .OrderBy(r => r.Name),
            itemPath: release => release.GetPSPath(),
            fetch: FetchDetail,
            onRow: (drive, folder, release) => Enrich(caller, drive, folder, release),
            emit: (drive, folder, rows) =>
            {
                // The CSV path writes record by record -- a file has no column widths -- but
                // it still gets the rows a folder at a time, which costs it nothing.
                if (writer is not null)
                {
                    foreach (var row in rows) WriteCsvContent(caller, writer, row);
                }
                else
                {
                    caller.WriteObject(rows, true);
                }
            });
    }

    /// <summary>
    /// The per-release call, on a pool thread. Besides the detail itself it WARMS the two
    /// caches <see cref="Enrich"/> reads, so on a cold cache those calls are paid four at a
    /// time here instead of one at a time on the pipeline thread — which is what made the run
    /// as slow as if nothing were parallel, and left the progress bar (which counts the pool's
    /// completions) full while the consumer was still paying.
    /// </summary>
    private static Release? FetchDetail(OrchDriveInfo drive, Folder folder, Release release)
    {
        var detailed = drive.ReleasesDetailed.Get(folder, release.Id!.Value);

        // Only ask for retention when the detail did not already bring it. From API 19 the
        // release detail goes through the GetRelease action, and that payload carries
        // RetentionAction / Period / BucketId: checked against the separate endpoint on Cloud
        // (API 20) for eight releases, identical every time. Below 19 the action is not used
        // and the plain payload has none, so this falls through to the call as before.
        //
        // Tested on the data rather than gated on the version, deliberately. A version gate
        // would have to assert something about API 19 exactly, which no Orchestrator reachable
        // from here runs -- on-prem and Automation Suite both stop at 18. This needs no such
        // claim, and stays correct whatever a later version does.
        //
        // The bulk /odata/ReleaseRetention collection looks like a better answer -- one call
        // per folder instead of one per release -- and is not. On Automation Suite 18.0 it
        // returns nothing at all for releases whose retention is unset, while asking for them
        // one at a time returns Action "None", Period 0. Switching to it would quietly turn
        // those into blanks.
        if (detailed?.RetentionAction is null)
        {
            Warm(() => drive.ReleaseRetentions.Get(folder, release.Id!.Value));
        }

        // The entry point's path normally comes from `release`, the listing row, which carries
        // the expanded EntryPoint from API 12 -- checked against the package's own entry points
        // on Automation Suite (API 18) and Cloud (API 20), every one identical. The feed lookup
        // stays as the fallback for a row without it. Not from `detailed`: the "$expand" handed
        // to GetReleaseById goes out as the request body, not the query, so the detail payload
        // never carries EntryPoint.
        if (detailed is { EntryPointId: not null })
        {
            if (release.EntryPoint is { Path: not null } listed && listed.Id == detailed.EntryPointId)
            {
                detailed.EntryPointPath = listed.Path;
            }
            else
            {
                var d = detailed;
                Warm(() => drive.PackageEntryPoints.Get(
                    (drive.FolderFeedId.Get(folder) ?? "", d.ProcessKey ?? "", d.ProcessVersion!)));
            }
        }

        return detailed;
    }

    /// <summary>
    /// Run a call for its cache entry and drop whatever happens. Warming never decides
    /// anything: Enrich repeats the call on the pipeline thread, where a hit costs nothing and
    /// a failure can be reported against the release it belongs to.
    /// </summary>
    private static void Warm(Action fetch)
    {
        try { fetch(); } catch { }
    }

    /// <summary>
    /// Fill in the fields that live behind their own endpoints. Runs on the pipeline thread,
    /// against caches <see cref="FetchDetail"/> has already warmed.
    /// </summary>
    private static void Enrich(OrchestratorPSCmdlet caller, OrchDriveInfo drive, Folder folder, Release release)
    {
        // Already set means FetchDetail took it from the listing and skipped the feed lookup.
        if (release.EntryPointId is not null && release.EntryPointPath is null)
        {
            var feedId = drive.FolderFeedId.Get(folder);
            var entryPoints = drive.PackageEntryPoints.Get((feedId ?? "", release.ProcessKey ?? "", release.ProcessVersion!));
            release.EntryPointPath = entryPoints.FirstOrDefault(e => e.Id == release.EntryPointId)?.Path;
        }

        // Non-null means the detail payload already carried it -- see FetchDetail, which then
        // skipped the warm-up too, so this would be a real call rather than a cache hit.
        if (release.RetentionAction is null)
        {
            try
            {
                var retention = drive.ReleaseRetentions.Get(folder, release.Id!.Value);
                if (retention is not null)
                {
                    release.RetentionAction = retention.Action;
                    release.RetentionPeriod = retention.Period;
                    release.RetentionBucketId = retention.BucketId;
                }
            }
            catch (Exception ex)
            {
                // Non-fatal by long standing: the release is still emitted, without retention.
                caller.WriteError(new ErrorRecord(new OrchException(release.GetPSPath(), "Get retention info failed.", ex), "GetRetentionSettingError", ErrorCategory.InvalidOperation, release));
            }
        }
    }

    private static string? GetBucketName(OrchestratorPSCmdlet caller, Release release, Int64? bucketId, string bucketIdKind)
    {
        if (bucketId is null) return null;

        OrchDriveInfo drive = null;
        Folder folder = null;
        try
        {
            (drive, folder) = caller.SessionState.EnumFolders(release.Path).FirstOrDefault();
        }
        catch
        {
            caller.WriteWarning($"Path '{release.GetPSPath()}' cannot be resolved.");
        }

        if ((drive is not null) && (folder is not null))
        {
            var buckets = drive.Buckets.Get(folder);
            var bucket = buckets.FirstOrDefault(b => b.Id == bucketId);
            if (bucket is not null)
            {
                return bucket.Name;
            }
            else
            {
                caller.WriteWarning($"\"{release.GetPSPath()}\": {bucketIdKind} {release.RetentionBucketId} cannot be resolved.");
            }
        }
        return null;
    }

    private static void WriteCsvContent(OrchestratorPSCmdlet caller, StreamWriter writer, Release release)
    {
        string? retentionBucket = GetBucketName(caller, release, release.RetentionBucketId, "RetentionBucketId");
        string? staleRetentionBucket = GetBucketName(caller, release, release.StaleRetentionBucketId, "StaleRetentionBucketId");

        string[] line = [
            EscapeCsvValue(release.Path, true),
            EscapeCsvValue(release.Name, true),
            EscapeCsvValue(release.ProcessKey),
            EscapeCsvValue(release.ProcessVersion),
            EscapeCsvValue(release.Description),
            EscapeCsvValue(release.EntryPointPath),
            EscapeCsvValue(release.InputArguments),
            EscapeCsvValue(release.SpecificPriorityValue),
            EscapeCsvValue(release.HiddenForAttendedUser),
            EscapeCsvValue(release.RemoteControlAccess),
            EscapeCsvValue(release.RetentionAction),
            EscapeCsvValue(release.RetentionPeriod),
            EscapeCsvValue(retentionBucket, true),
            EscapeCsvValue(release.StaleRetentionAction),
            EscapeCsvValue(release.StaleRetentionPeriod),
            EscapeCsvValue(staleRetentionBucket, true),
            EscapeCsvValue(release.ProcessSettings?.ErrorRecordingEnabled),
            EscapeCsvValue(release.ProcessSettings?.Quality),
            EscapeCsvValue(release.ProcessSettings?.Frequency),
            EscapeCsvValue(release.ProcessSettings?.Duration),
            EscapeCsvValue(release.ProcessSettings?.AutoStartProcess),
            EscapeCsvValue(release.ProcessSettings?.AlwaysRunning),
            EscapeCsvValue(release.ProcessSettings?.AutopilotForRobots?.Enabled),
            EscapeCsvValue(release.ProcessSettings?.AutopilotForRobots?.HealingEnabled),
            EscapeCsvValue(release.VideoRecordingSettings?.VideoRecordingType),
            EscapeCsvValue(release.VideoRecordingSettings?.QueueItemVideoRecordingType),
            EscapeCsvValue(release.VideoRecordingSettings?.MaxDurationSeconds),
            EscapeCsvValue(release.Tags)
        ];

        writer.WriteCsvLine(line);
    }
}
