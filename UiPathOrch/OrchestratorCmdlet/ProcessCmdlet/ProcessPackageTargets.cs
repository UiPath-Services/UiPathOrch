using System.Management.Automation;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

/// <summary>
/// Phase one of the process-side package readers: lists the processes of every folder in
/// scope and pairs each with the package version that folder deployed.
///
/// Shared by Get-OrchProcessDependency and Get-OrchProcessWorkflow, which differ only in what
/// they read out of the package afterwards. The listing is parallel because it is cheap; the
/// downloads it feeds are not (see PackageContentsPrefetch).
/// </summary>
internal static class ProcessPackageTargets
{
    internal record Target(
        OrchDriveInfo Drive,
        Folder Folder,
        string FolderPath,
        Release Release,
        string PackageId,
        string Version);

    /// <summary>
    /// Returns one target per matching process, in folder then process order — the order the
    /// rows are emitted in. A process whose release names no package version is reported and
    /// skipped: there is nothing to download for it.
    /// </summary>
    internal static List<Target> List(
        OrchestratorPSCmdlet cmdlet,
        IEnumerable<(OrchDriveInfo drive, Folder folder)> drivesFolders,
        List<WildcardPattern>? wpName,
        ConsoleCancelHandler cancelHandler,
        string activity,
        string errorId)
    {
        var targets = new List<Target>();

        using var pool = OrchThreadPool.RunForEach(
            drivesFolders,
            df => df.folder.GetPSPath(),
            df => df,
            df => df.drive.Releases.Get(df.folder));

        using var reporter = new ProgressReporter(cmdlet, 1, pool.Count, activity);
        foreach (var task in pool)
        {
            try
            {
                // GetResult() first: Source is only readable once the result was collected.
                var releases = pool.GetResultWithProgress(task, reporter, cancelHandler.Token);
                var (drive, folder) = task.Source;
                if (releases is null) continue;

                string folderPath = folder.GetPSPath();

                foreach (var release in releases
                    .FilterByWildcards(r => r?.Name, wpName)
                    .OrderBy(r => r.Name)
                    .WithCancellation(cancelHandler.Token))
                {
                    // ProcessKey is the package id, ProcessVersion the version this folder runs.
                    if (string.IsNullOrEmpty(release.ProcessKey) || string.IsNullOrEmpty(release.ProcessVersion))
                    {
                        cmdlet.WriteWarning($"\"{release.GetPSPath()}\": the process does not name a package version; skipping.");
                        continue;
                    }

                    targets.Add(new Target(drive, folder, folderPath, release, release.ProcessKey, release.ProcessVersion));
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
