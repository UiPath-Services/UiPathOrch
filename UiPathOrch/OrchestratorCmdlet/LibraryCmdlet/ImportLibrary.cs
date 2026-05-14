using System.Management.Automation;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Completer;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsData.Import, "OrchLibrary", SupportsShouldProcess = true)]
[OutputType(typeof(Entities.BulkItemDtoOfString))]
public class ImportLibraryCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    public string[]? Source { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    private static bool LibraryExists(OrchDriveInfo drive, string fullPath)
    {
        try
        {
            var (id, version) = ExtractPackageIdVersionFromFilePath(fullPath);
            if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(version))
            {
                var dstExistingVersions = drive.LibraryVersions.Get(id);
                if (dstExistingVersions is not null)
                {
                    return dstExistingVersions.Any(v => v.Version == version);
                }
            }
        }
        catch { } // Swallow this exception

        return false;
    }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumOrchDrives(Path);
        var pkgFilePaths = SessionState.ExpandLocalPath(Source, "*.nupkg").OrderByFileNameVersion();

        // This implementation should be fine as is.
        var importTasks = drives
            .SelectMany(drive => pkgFilePaths, (drive, pkgFilePath) =>
                (drive, pkgFilePath.FullPath, pkgFilePath.RelativePath))
            .ToList();

        int totalNum = importTasks.Count;

        using var reporter = new ProgressReporter(this, 1, totalNum, "Importing libraries");

        int index = 0;
        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var importTask in importTasks.WithCancellation(cancelHandler.Token))
        {
            var (drive, fullPath, relativePath) = importTask;
            string target = drive.NameColonSeparator;
            if (ShouldProcess(target, $"Import Library {fullPath}"))
            {
                reporter.WriteProgress(++index);
                try
                {
                    // If a library with the same name already exists on the drive, show a warning and skip the import
                    if (LibraryExists(drive, fullPath))
                    {
                        WriteError(new ErrorRecord(new InvalidOperationException($"\"{fullPath}\": Library already exists in {drive.NameColonSeparator}. Skipping the import."), "ImportLibraryError", ErrorCategory.WriteError, drive));
                        continue;
                    }

                    var result = drive.OrchAPISession.UploadLibrary(fullPath);
                    if (result is not null)
                    {
                        result.Path = drive.NameColonSeparator;
                        WriteObject(result);
                        drive.LibrariesInTenant.ClearCache();
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
