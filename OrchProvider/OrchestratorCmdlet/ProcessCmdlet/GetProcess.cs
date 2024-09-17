using System.Management.Automation;
using System.Text;
using System.Text.Json;
using UiPath.OrchAPI;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "OrchProcess")]
    [OutputType(typeof(Entities.Release))]
    public class GetProcessCommand : OrchestratorPSCmdlet
    {
        [Parameter (Position = 0)]
        [ArgumentCompleter(typeof(ProcessNameCompleter<Positional.Name>))]
        [SupportsWildcards]
        public string[]? Name { get; set; }

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

        private void WriteCsvContent(StreamWriter writer, IEnumerable<Entities.Release> output)
        {
            // 各プロセスに対してデータ行を書き込む
            foreach (var p in output)
            {
                string retentionBucket = null;
                if (p.RetentionBucketId != null)
                {
                    OrchDriveInfo drive = null;
                    Folder folder = null;
                    try
                    {
                        (drive, folder) = OrchDriveInfo.EnumFolders(p.Path).FirstOrDefault();
                    }
                    catch
                    {
                        WriteWarning($"Path '{p.GetPSPath()}' cannot be resolved.");
                    }

                    if ((drive != null) && (folder != null))
                    {
                        var buckets = drive.GetBuckets(folder);
                        var bucket = buckets.FirstOrDefault(b => b.Id == p.RetentionBucketId);
                        if (bucket != null)
                        {
                            retentionBucket = bucket.Name;
                        }
                        else
                        {
                            WriteWarning($"{p.GetPSPath()}: RetentionBucketId {p.RetentionBucketId.ToString() ?? ""} cannot be resolved.");
                        }
                    }
                }

                var line = new StringBuilder();

                line.Append($"{EscapeCsvValue(p.Path, true)},");
                line.Append($"{EscapeCsvValue(p?.Name, true)},");
                line.Append($"{EscapeCsvValue(p?.ProcessKey)},");
                line.Append($"{EscapeCsvValue(p?.ProcessVersion)},");
                line.Append($"{EscapeCsvValue(p?.Description)},");
                line.Append($"{EscapeCsvValue(p?.EntryPoint?.Path)},");
                line.Append($"{EscapeCsvValue(p?.InputArguments)},");
                line.Append($"{p?.SpecificPriorityValue.ToString() ?? ""},");
                line.Append($"{EscapeCsvValue(p?.HiddenForAttendedUser?.ToString())},");
                line.Append($"{EscapeCsvValue(p?.RemoteControlAccess)},");
                line.Append($"{EscapeCsvValue(p?.RetentionAction)},");
                line.Append($"{p?.RetentionPeriod?.ToString() ?? ""},");
                line.Append($"{EscapeCsvValue(retentionBucket)},");
                line.Append($"{p?.ProcessSettings?.ErrorRecordingEnabled},");
                line.Append($"{p?.ProcessSettings?.Quality?.ToString() ?? ""},");
                line.Append($"{p?.ProcessSettings?.Frequency?.ToString() ?? ""},");
                line.Append($"{p?.ProcessSettings?.Duration?.ToString() ?? ""},");
                line.Append($"{p?.ProcessSettings?.AutoStartProcess?.ToString() ?? ""},");
                line.Append($"{p?.ProcessSettings?.AlwaysRunning?.ToString() ?? ""},");
                line.Append($"{EscapeCsvValue(p?.VideoRecordingSettings?.VideoRecordingType)},");
                line.Append($"{EscapeCsvValue(p?.VideoRecordingSettings?.QueueItemVideoRecordingType)},");
                line.Append($"{p?.VideoRecordingSettings?.MaxDurationSeconds?.ToString() ?? ""},");
                line.Append($"{EscapeCsvValue(JsonSerializer.Serialize(p?.Tags, OrchAPISession.jsoWhenWritingNull), false)}");

                writer.WriteLine(line.ToString());
            }
        }

        private void Output(StreamWriter? writer, IEnumerable<Release> releases)
        {
            if (writer != null)
            {
                WriteCsvContent(writer, releases);
            }
            else
            {
                WriteObject(releases, true);
            }
        }

        protected override void ProcessRecord()
        {
            var drivesFolders = OrchDriveInfo.EnumFolders(Path, Recurse.IsPresent, Depth);
            var wpName = Name?.Select(name => new WildcardPattern(name, WildcardOptions.IgnoreCase)).ToList();

            ExportCsv = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
            using var writer = WriteCsvHeader(ExportCsv, CsvEncoding, CsvHeaders);

            using var results = OrchThreadPool.RunForEach(drivesFolders,
                df => df.folder.GetPSPath(),
                df => df.folder,
                df => df.drive.GetReleases(df.folder));

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var result in results)
            {
                try
                {
                    var releases = result.GetResult(cancelHandler.Token);
                    if (releases == null) continue;

                    Output(writer, releases
                        .FilterByWildcards(m => m?.Name, wpName)
                        .OrderBy(m => m.Name));
                }
                catch (OrchException ex)
                {
                    WriteError(new ErrorRecord(ex, "GetProcessError", ErrorCategory.InvalidOperation, ex.Target));
                }
            }

            WriteCSVExportedMessage(this, ExportCsv);
        }
    }
}
