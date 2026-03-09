using System.Management.Automation;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
//using TPositional = UiPath.PowerShell.Positional.Source_Path;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsData.Import, "OrchPackage", SupportsShouldProcess = true)]
[OutputType(typeof(Entities.BulkItemDtoOfString))]
public class ImportPackageCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    public string[]? Source { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    private static bool PackageExists(OrchDriveInfo drive, Folder folder, string fullPath)
    {
        try
        {
            var (id, version) = ExtractPackageIdVersionFromFilePath(fullPath);
            if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(version))
            {
                var dstExistingVersions = drive.GetPackageVersions(folder, id);
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
        var drivesFolders = SessionState.EnumPackageFeedFolders(Path);
        if (Recurse && drivesFolders.Any(df => df.folder != df.drive.RootFolder))
        {
            throw new Exception("The -Recurse parameter can only be specified for the tenant's root folder.");
        }

        Source = Source?.Select(s => SessionState.Path.GetUnresolvedProviderPathFromPSPath(s)).ToArray();

        var pkgFilePaths = SessionState.ExpandLocalPath(Source, "*.nupkg", Recurse, Recurse ? 1 : 0)
            .OrderByFileNameVersion();

        var tasks = drivesFolders
            .SelectMany(df => pkgFilePaths, (df, pkgFilePath) =>
                (df.drive, df.folder, pkgFilePath.FullPath, pkgFilePath.RelativePath))
            .ToList();

        int totalNum = tasks.Count;

        using var reporter = new ProgressReporter(this, 1, totalNum, "Importing Packages");

        int index = 0;
        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var task in tasks)
        {
            var (drive, folder, fullPath, relativePath) = task;

            HashSet<string> ignoredFolders = new();
            Entities.Folder targetFolder = null;
            if (relativePath == "." || string.IsNullOrEmpty(relativePath))
            {
                targetFolder = folder;
            }
            else
            {
                if (ignoredFolders.Contains(System.IO.Path.GetDirectoryName(fullPath)!))
                {
                    continue;
                }

                targetFolder = drive!.GetFolder(relativePath);
                if (targetFolder is null)
                {
                    WriteWarning($"Folder {relativePath} does not exist on {drive.NameColonSeparator}. Ignored.");
                    ignoredFolders.Add(System.IO.Path.GetDirectoryName(fullPath)!);
                    continue;
                }
                if (targetFolder.FeedType != "FolderHierarchy")
                {
                    WriteWarning($"Folder {drive.NameColonSeparator}{relativePath} exists, but its FeedType is {targetFolder.FeedType}. Ignored.");
                    ignoredFolders.Add(System.IO.Path.GetDirectoryName(fullPath)!);
                    continue;
                }
            }

            cancelHandler.Token.ThrowIfCancellationRequested();

            string target = targetFolder.GetPSPath();
            string target2 = target + System.IO.Path.GetFileName(fullPath);
            if (ShouldProcess(target, $"Import Package {fullPath}"))
            {
                reporter.WriteProgress(++index);
                try
                {
                    // If a package with the same name already exists in targetFolder, show a warning and skip the copy
                    if (PackageExists(drive, targetFolder, fullPath))
                    {
                        WriteError(new ErrorRecord(new InvalidOperationException($"\"{fullPath}\": Package already exists in {targetFolder.GetPSPath()}. Skipping the import."), "ImportPackageError", ErrorCategory.WriteError, targetFolder));
                        continue;
                    }

                    string? feedId = drive.FolderFeedId.Get(targetFolder);
                    var result = drive.OrchAPISession.UploadPackage(feedId!, fullPath);
                    if (result is not null)
                    {
                        result.Path = target;
                        WriteObject(result);
                    }
                    drive._dicPackages?.TryRemove(feedId ?? "", out var _);
                    drive._dicPackageVersions?.TryRemove(feedId ?? "", out var _);
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(target2, ex), "ImportPackageError", ErrorCategory.InvalidOperation, target2));
                }
            }
        }
    }
}
