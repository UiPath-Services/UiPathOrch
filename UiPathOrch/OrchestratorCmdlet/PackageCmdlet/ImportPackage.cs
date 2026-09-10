using System.Management.Automation;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsData.Import, "OrchPackage", SupportsShouldProcess = true)]
[OutputType(typeof(Entities.BulkItemDtoOfString))]
public class ImportPackageCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    public string[]? Source { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

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
        var drivesFolders = SessionState.EnumPackageFeedFolders(EffectivePath(Path, LiteralPath));
        if (Recurse && drivesFolders.Any(df => df.folder != df.drive.RootFolder))
        {
            throw new ArgumentException("The -Recurse parameter can only be specified for the tenant's root folder.");
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
        // Warn once per (destination drive, source directory) instead of once per .nupkg: a
        // migrated folder holds dozens of packages, and the folder-level problems below are
        // properties of the folder, not of each file. Keyed by drive as well as directory
        // because the same local directory can be fine on one destination drive and not on
        // another -- keying by directory alone would suppress the warning for the second drive
        // and, worse, skip an import that should have gone through.
        HashSet<(string drive, string dir)> ignoredFolders = new();
        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var task in tasks.WithCancellation(cancelHandler.Token))
        {
            var (drive, folder, fullPath, relativePath) = task;

            Entities.Folder targetFolder = null;
            if (relativePath == "." || string.IsNullOrEmpty(relativePath))
            {
                targetFolder = folder;
            }
            else
            {
                var ignoreKey = (drive!.NameColonSeparator, System.IO.Path.GetDirectoryName(fullPath)!);
                if (ignoredFolders.Contains(ignoreKey))
                {
                    continue;
                }

                targetFolder = drive!.GetFolder(relativePath);
                if (targetFolder is null)
                {
                    WriteWarning($"Folder {relativePath} does not exist on {drive.NameColonSeparator}. Ignored.");
                    ignoredFolders.Add(ignoreKey);
                    continue;
                }
                // -Recurse replays a tree produced by Export-OrchPackage -Recurse, whose
                // subdirectories are the source tenant's feed-owning folders. A destination
                // folder without its own feed cannot receive that directory: its packages would
                // land in the tenant feed, silently merging what was a separate feed on the
                // source. That is usually a folder created without a package feed by mistake,
                // so say so and skip rather than quietly changing the destination's topology.
                if (targetFolder.FeedType != "FolderHierarchy")
                {
                    WriteWarning(
                        $"Folder {drive.NameColonSeparator}{relativePath} exists but has no package feed of its own " +
                        $"(FeedType: {targetFolder.FeedType}), so its packages would be merged into the tenant feed. Skipped. " +
                        $"Re-create it as a folder with its own package feed, or import this directory with " +
                        $"-Path {drive.NameColonSeparator} if merging into the tenant feed is intended.");
                    ignoredFolders.Add(ignoreKey);
                    continue;
                }
            }

            cancelHandler.Token.ThrowIfCancellationRequested();

            string target = targetFolder.GetPSPath();
            string target2 = target + System.IO.Path.GetFileName(fullPath);
            if (ShouldProcess(target, $"Import Package {fullPath}"))
            {
                reporter.WriteProgress(++index, System.IO.Path.GetFileName(fullPath), $"Importing packages to {target}");
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
                    drive.Packages.ClearCache(feedId ?? "");
                    drive.PackageVersions.ClearCache(k => k.feedId == (feedId ?? ""));
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(target2, ex), "ImportPackageError", ErrorCategory.InvalidOperation, target2));
                }
            }
        }
    }
}
