using System.Management.Automation;
using System.Text;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchProcess")]
[OutputType(typeof(Entities.Release))]
public class GetProcessCommand : OrchestratorPSCmdlet
{
    [Parameter (Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(ProcessNameCompleter<TPositional>))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter]
    public SwitchParameter ExpandDetails { get; set; }

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
    private static readonly string[] CsvHeaders = [
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

    private string? GetBucketName(Entities.Release release, Int64? bucketId, string bucketIdKind)
    {
        if (bucketId is null) return null;

        OrchDriveInfo drive = null;
        Folder folder = null;
        try
        {
            (drive, folder) = SessionState.EnumFolders(release.Path).FirstOrDefault();
        }
        catch
        {
            WriteWarning($"Path '{release.GetPSPath()}' cannot be resolved.");
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
                WriteWarning($"{release.GetPSPath()}: {bucketIdKind} {release.RetentionBucketId} cannot be resolved.");
            }
        }
        return null;
    }

    private void WriteCsvContent(StreamWriter writer, Entities.Release release)
    {
        // 各プロセスに対してデータ行を書き込む
        string? retentionBucket = GetBucketName(release, release.RetentionBucketId, "RetentionBucketId");
        string? staleRetentionBucket = GetBucketName(release, release.StaleRetentionBucketId, "StaleRetentionBucketId");

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

    private void Output(StreamWriter? writer, Release release)
    {
        if (writer is not null)
        {
            WriteCsvContent(writer, release);
        }
        else
        {
            WriteObject(release);
        }
    }

    private void Output(StreamWriter? writer, IEnumerable<Release> releases)
    {
        if (writer is not null)
        {
            foreach (var release in releases)
            {
                WriteCsvContent(writer, release);
            }
        }
        else
        {
            WriteObject(releases, true);
        }
    }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();

        var (physicalCsvPath, providerCsvPath) = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
        using var writer = WriteCsvHeader(physicalCsvPath, CsvEncoding, CsvHeaders);

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            IEnumerable<Release> targetReleases;
            try
            {
                var releases = drive.GetReleases(folder);
                targetReleases = releases
                    .FilterByWildcards(r => r?.Name, wpName)
                    .OrderBy(r => r.Name);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "GetProcessError", ErrorCategory.InvalidOperation, folder));
                continue;
            }

            if (ExpandDetails.IsPresent || writer is not null)
            {
                using var results = OrchThreadPool.RunForEach(targetReleases,
                    release => release.GetPSPath(),
                    release => release,
                    release => drive.GetReleaseById(folder, release.Id!.Value));

                foreach (var result in results)
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();
                    try
                    {
                        var releaseDetailed = result.GetResult(cancelHandler.Token);
                        if (releaseDetailed is null) continue;

                        if (releaseDetailed.EntryPointId is not null)
                        {
                            var feedId = drive.FolderFeedId.Get(folder);
                            var entryPoints = drive.GetPackageEntryPoints(feedId, releaseDetailed.ProcessKey ?? "", releaseDetailed.ProcessVersion!);
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
                            WriteError(new ErrorRecord(new OrchException(releaseDetailed.GetPSPath(), "Get retention info failed.", ex), "GetRetentionSettingError", ErrorCategory.InvalidOperation, releaseDetailed));
                        }

                        Output(writer, releaseDetailed);
                    }
                    catch (OrchException ex)
                    {
                        WriteError(new ErrorRecord(ex, "GetProcessError", ErrorCategory.InvalidOperation, ex.Target));
                    }
                }
            }
            else
            {
                cancelHandler.Token.ThrowIfCancellationRequested();
                Output(writer, targetReleases);
            }
        }

        WriteCSVExportedMessage(this, providerCsvPath);
    }
}
