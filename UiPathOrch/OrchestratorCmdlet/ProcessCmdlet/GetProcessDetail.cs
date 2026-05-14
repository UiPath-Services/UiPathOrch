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
        var drivesFolders = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth);
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
        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            IEnumerable<Release> targetReleases;
            try
            {
                var releases = drive.Releases.Get(folder);
                targetReleases = releases
                    .FilterByWildcards(r => r?.Name, nameWildcards)
                    .OrderBy(r => r.Name);
            }
            catch (Exception ex)
            {
                caller.WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "GetProcessDetailError", ErrorCategory.InvalidOperation, folder));
                continue;
            }

            using var results = OrchThreadPool.RunForEach(targetReleases,
                release => release.GetPSPath(),
                release => release,
                release => drive.ReleasesDetailed.Get(folder, release.Id!.Value));

            foreach (var result in results.WithCancellation(cancelHandler.Token))
            {
                try
                {
                    var releaseDetailed = result.GetResult(cancelHandler.Token);
                    if (releaseDetailed is null) continue;

                    if (releaseDetailed.EntryPointId is not null)
                    {
                        var feedId = drive.FolderFeedId.Get(folder);
                        var entryPoints = drive.PackageEntryPoints.Get((feedId ?? "", releaseDetailed.ProcessKey ?? "", releaseDetailed.ProcessVersion!));
                        var entryPath = entryPoints.FirstOrDefault(e => e.Id == releaseDetailed.EntryPointId)?.Path;
                        releaseDetailed.EntryPointPath = entryPath;
                    }

                    try
                    {
                        var retention = drive.OrchAPISession.GetReleaseRetention(folder.Id!.Value, releaseDetailed.Id!.Value);
                        if (retention is not null)
                        {
                            releaseDetailed.RetentionAction = retention.Action;
                            releaseDetailed.RetentionPeriod = retention.Period;
                            releaseDetailed.RetentionBucketId = retention.BucketId;
                        }
                    }
                    catch (Exception ex)
                    {
                        caller.WriteError(new ErrorRecord(new OrchException(releaseDetailed.GetPSPath(), "Get retention info failed.", ex), "GetRetentionSettingError", ErrorCategory.InvalidOperation, releaseDetailed));
                    }

                    if (writer is not null) { WriteCsvContent(caller, writer, releaseDetailed); }
                    else { caller.WriteObject(releaseDetailed); }
                }
                catch (OrchException ex)
                {
                    caller.WriteError(new ErrorRecord(ex, "GetProcessDetailError", ErrorCategory.InvalidOperation, ex.Target));
                }
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
                caller.WriteWarning($"{release.GetPSPath()}: {bucketIdKind} {release.RetentionBucketId} cannot be resolved.");
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
