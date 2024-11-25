using System.Management.Automation;
using System.Text;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "OrchProcess")]
    [OutputType(typeof(Entities.Release))]
    public class GetProcessCommand : OrchestratorPSCmdlet
    {
        [Parameter (Position = 0)]
        [ArgumentCompleter(typeof(ProcessNameCompleter<TPositional>))]
        [SupportsWildcards]
        public string[]? Name { get; set; }

        [Parameter]
        public SwitchParameter ExpandDetails { get; set; }

        [Parameter]
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
            "PackageId",
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
            "ErrorRecordingEnabled",
            "Quality",
            "Frequency",
            "Duration",
            "AutoStartProcess",
            "AlwaysRunning",
            "VideoRecordingType",
            "QueueItemVideoRecordingType",
            "MaxDurationSeconds",
            "Tags"
        ];

        private void WriteCsvContent(StreamWriter writer, Entities.Release release)
        {
            // 各プロセスに対してデータ行を書き込む
            string retentionBucket = null;
            if (release.RetentionBucketId != null)
            {
                OrchDriveInfo drive = null;
                Folder folder = null;
                try
                {
                    (drive, folder) = OrchDriveInfo.EnumFolders(release.Path).FirstOrDefault();
                }
                catch
                {
                    WriteWarning($"Path '{release.GetPSPath()}' cannot be resolved.");
                }

                if ((drive != null) && (folder != null))
                {
                    var buckets = drive.Buckets.Get(folder);
                    var bucket = buckets.FirstOrDefault(b => b.Id == release.RetentionBucketId);
                    if (bucket != null)
                    {
                        retentionBucket = bucket.Name;
                    }
                    else
                    {
                        WriteWarning($"{release.GetPSPath()}: RetentionBucketId {release.RetentionBucketId.ToString() ?? ""} cannot be resolved.");
                    }
                }
            }

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
                EscapeCsvValue(release.ProcessSettings?.ErrorRecordingEnabled),
                EscapeCsvValue(release.ProcessSettings?.Quality),
                EscapeCsvValue(release.ProcessSettings?.Frequency),
                EscapeCsvValue(release.ProcessSettings?.Duration),
                EscapeCsvValue(release.ProcessSettings?.AutoStartProcess),
                EscapeCsvValue(release.ProcessSettings?.AlwaysRunning),
                EscapeCsvValue(release.VideoRecordingSettings?.VideoRecordingType),
                EscapeCsvValue(release.VideoRecordingSettings?.QueueItemVideoRecordingType),
                EscapeCsvValue(release.VideoRecordingSettings?.MaxDurationSeconds),
                EscapeCsvValue(release.Tags)
            ];

            WriteCsvLine(writer, line);
        }

        private void Output(StreamWriter? writer, Release release)
        {
            if (writer != null)
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
            if (writer != null)
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
            var drivesFolders = OrchDriveInfo.EnumFolders(Path, Recurse.IsPresent, Depth);
            var wpName = Name.ConvertToWildcardPatternList();

            ExportCsv = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
            using var writer = WriteCsvHeader(ExportCsv, CsvEncoding, CsvHeaders);


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

                if (ExpandDetails.IsPresent || writer != null)
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
                            if (releaseDetailed == null) continue;

                            if (releaseDetailed.EntryPointId != null)
                            {
                                var feedId = drive.FolderFeedId.Get(folder);
                                var entryPoints = drive.GetPackageEntryPoints(feedId, releaseDetailed.Name!, releaseDetailed.ProcessVersion!);
                                var entryPath = entryPoints.FirstOrDefault(e => e.Id == releaseDetailed.EntryPointId)?.Path;
                                releaseDetailed.EntryPointPath = entryPath;
                            }

                            ReleaseRetentionSetting retention = null;
                            // API ver が 15.0 の場合には、リテンションポリシーを読み取れなかった。この正しい数字は、もっと大きいかもしれない。
                            if (drive.OrchAPISession.ApiVersion >= 16)
                            {
                                try
                                {
                                    retention = drive.OrchAPISession.GetReleaseRetention(folder.Id!.Value, releaseDetailed.Id!.Value);
                                    if (retention != null)
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

            WriteCSVExportedMessage(this, ExportCsv);
        }
    }
}
