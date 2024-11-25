using System.Management.Automation;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Completer;
using TPositional = UiPath.PowerShell.Positional.Source_Path;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsData.Import, "OrchLibrary", SupportsShouldProcess = true)]
    [OutputType(typeof(Entities.BulkItemDtoOfString))]
    public class ImportLibraryCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public string[]? Source { get; set; }

        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
        public string[]? Path { get; set; }

        private static bool LibraryExists(OrchDriveInfo drive, string fullPath)
        {
            try
            {
                var (id, version) = ExtractPackageIdVersionFromFilePath(fullPath);
                if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(version))
                {
                    var dstExistingVersions = drive.GetLibraryVersions(id);
                    if (dstExistingVersions != null)
                    {
                        return dstExistingVersions.Any(v => v.Version == version);
                    }
                }
            }
            catch { } // この例外は握りつぶす

            return false;
        }

        protected override void ProcessRecord()
        {
            var drives = OrchDriveInfo.EnumOrchDrives(Path);
            var pkgFilePaths = OrchDriveInfo.ExpandLocalPath(Source, "*.nupkg").OrderByFileNameVersion();

            // この実装は、このままで良いか。
            var importTasks = drives
                .SelectMany(drive => pkgFilePaths, (drive, pkgFilePath) => 
                    (drive, pkgFilePath.FullPath, pkgFilePath.RelativePath))
                .ToList();

            int totalNum = importTasks.Count;

            string msg = "Importing Libraries";
            using var reporter = new ProgressReporter(this, 1, totalNum, msg, msg);

            int index = 0;
            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var importTask in importTasks)
            {
                cancelHandler.Token.ThrowIfCancellationRequested();

                var (drive, fullPath, relativePath) = importTask;
                string target = drive.NameColonSeparator;
                if (ShouldProcess(target, $"Import Library {fullPath}"))
                {
                    reporter.WriteProgress(++index, $"{index:D}/{totalNum}");
                    try
                    {
                        // drive に同名のライブラリがあれば、警告を表示してインポートをスキップする
                        if (LibraryExists(drive, fullPath))
                        {
                            WriteError(new ErrorRecord(new InvalidOperationException($"\"{fullPath}\": Library already exists in {drive.NameColonSeparator}. Skipping the import."), "ImportLibraryError", ErrorCategory.WriteError, drive));
                            continue;
                        }

                        var result = drive.OrchAPISession.UploadLibrary(fullPath);
                        if (result != null)
                        {
                            result.Path = drive.NameColonSeparator;
                            WriteObject(result);
                            drive._dicLibraries = null;
                            drive._dicLibraryVersions = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        string target2 = target + System.IO.Path.GetFileName(fullPath);
                        WriteError(new ErrorRecord(new OrchException(target2, ex), "ImportLibraryError", ErrorCategory.InvalidOperation, target2));
                    }
                }
            }
        }
    }
}
