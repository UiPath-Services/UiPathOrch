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

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

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
        var drives = SessionState.EnumOrchDrives(EffectivePath(Path, LiteralPath));
        var pkgFilePaths = SessionState.ExpandLocalPath(Source, "*.nupkg").OrderByFileNameVersion();

        // This implementation should be fine as is.
        var importTasks = drives
            .SelectMany(drive => pkgFilePaths, (drive, pkgFilePath) =>
                (drive, pkgFilePath.FullPath, pkgFilePath.RelativePath))
            .ToList();

        int totalNum = importTasks.Count;

        // Fixed label, destination in Context: see CopyCalendar for why the destination may
        // not go in the activity of a bar that is already on screen.
        using var reporter = new ProgressReporter(this, 1, totalNum, "Libraries");

        int index = 0;
        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var importTask in importTasks.WithCancellation(cancelHandler.Token))
        {
            var (drive, fullPath, relativePath) = importTask;
            string target = drive.NameColonSeparator;
            if (ShouldProcess(target, $"Import Library {fullPath}"))
            {
                reporter.Context = drive.NameColonSeparator;
                reporter.WriteProgress(++index, System.IO.Path.GetFileName(fullPath));
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
                // Ctrl+C is not a failure of this file. The upload does observe the token, so
                // without this the press produced two messages: "<file>: The operation was
                // canceled" from here, and then the real terminating one from the loop above.
                // Filtered on the token rather than catching every OperationCanceledException,
                // because HttpClient.Timeout also surfaces as a cancellation (see OrchHttp) --
                // a request that timed out IS a failure of this file and must stay a
                // per-file error that lets the remaining files import.
                catch (OperationCanceledException) when (cancelHandler.Token.IsCancellationRequested)
                {
                    throw;
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
