using System.Management.Automation;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

/// <summary>
/// Phase one of the feed-side package readers: lists the packages of every feed in scope and
/// resolves which versions of them to read.
///
/// Shared by Get-OrchPackageDependency and Get-OrchPackageWorkflow, which differ only in what
/// they read out of the package afterwards. The listing is parallel because it is cheap; the
/// downloads it feeds are not (see PackageContentsLoader).
/// </summary>
internal static class FeedPackageTargets
{
    internal record Target(OrchDriveInfo Drive, Folder Folder, string FolderPath, Package Package, string Version);

    /// <summary>
    /// Returns one target per package version to read, in feed then package order. Without
    /// <paramref name="versionRequested"/> only each package's latest version is taken — the
    /// version a deployment would pick up; with it, every version of the feed that matches.
    /// </summary>
    internal static List<Target> List(
        OrchestratorPSCmdlet cmdlet,
        IEnumerable<(OrchDriveInfo drive, Folder folder)> drivesFolders,
        List<WildcardPattern>? wpId,
        List<WildcardPattern>? wpVersion,
        bool versionRequested,
        ConsoleCancelHandler cancelHandler,
        string activity,
        string errorId)
    {
        var targets = new List<Target>();

        using var pool = OrchThreadPool.RunForEach(
            drivesFolders,
            df => df.folder.GetPSPath(),
            df => df,
            df => df.drive.GetPackages(df.folder)
                .FilterByWildcards(p => p?.Id, wpId)
                .OrderBy(p => p.Id!.ToLower())
                .ToList());

        using var reporter = new ProgressReporter(cmdlet, pool.Count, activity);
        foreach (var task in pool)
        {
            try
            {
                // GetResult() first: Source is only readable once the result was collected.
                var packages = pool.GetResultWithProgress(task, reporter, cancelHandler.Token);
                var (drive, folder) = task.Source;
                if (packages is null) continue;

                string folderPath = folder.GetPSPath();

                foreach (var package in packages.WithCancellation(cancelHandler.Token))
                {
                    if (string.IsNullOrEmpty(package.Id)) continue;

                    var versions = versionRequested
                        ? drive.GetPackageVersions(folder, package.Id)
                            .FilterByWildcards(v => v?.Version, wpVersion)
                            .Select(v => v.Version)
                            .ToArray()
                        : [package.Version];

                    foreach (var version in versions)
                    {
                        if (string.IsNullOrEmpty(version)) continue;
                        targets.Add(new Target(drive, folder, folderPath, package, version));
                    }
                }
            }
            catch (OrchException ex)
            {
                cmdlet.WriteError(new ErrorRecord(ex, errorId, ErrorCategory.InvalidOperation, ex.Target));
            }
        }

        return targets;
    }
}
