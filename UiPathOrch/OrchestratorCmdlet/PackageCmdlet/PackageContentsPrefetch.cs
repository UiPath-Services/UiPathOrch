using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

/// <summary>
/// Downloads, one at a time, every package a cmdlet is about to read — so the progress bar
/// counts the work that actually costs something.
///
/// Reading dependencies or workflows means downloading whole `.nupkg` files (Orchestrator has
/// no lighter endpoint), and a tenant-wide sweep can touch dozens. Listing folders and
/// processes is cheap and runs in parallel; the downloads are deliberately sequential, so that
/// a sweep is one request at a time against the server rather than four.
///
/// Running them here, before anything is emitted, buys two things: the bar's denominator is
/// the real number of downloads — visible before the first one starts, so an unexpectedly
/// large sweep can be cancelled — and the emit pass that follows keeps its natural order
/// (folder, then process) while hitting only the cache.
///
/// Failures are swallowed: the emit pass calls the same cache and reports the error against
/// the row it belongs to, where the user can see which process or package it came from.
/// </summary>
internal static class PackageContentsPrefetch
{
    /// <summary>
    /// Warms the cache for every distinct feed + package id + version among
    /// <paramref name="targets"/>. Distinct is what matters: a package shared by twenty
    /// processes is downloaded once, and the same id and version in two different feeds are
    /// two downloads, because they are two packages.
    /// </summary>
    internal static void Warm(
        OrchestratorPSCmdlet cmdlet,
        IEnumerable<(OrchDriveInfo drive, Folder folder, string packageId, string version)> targets,
        ConsoleCancelHandler cancelHandler,
        string activity)
    {
        var distinct = targets
            .GroupBy(t => (
                drive: t.drive.NameColonSeparator,
                feed: t.drive.FolderFeedId.Get(t.folder) ?? "",
                t.packageId,
                t.version))
            .Select(g => g.First())
            .ToList();

        if (distinct.Count == 0) return;

        using var reporter = new ProgressReporter(cmdlet, 2, distinct.Count, activity);
        int index = 0;
        foreach (var (drive, folder, packageId, version) in distinct)
        {
            cancelHandler.Token.ThrowIfCancellationRequested();
            reporter.WriteProgress(++index, $"{packageId}:{version}");

            try
            {
                drive.GetPackageContents(folder, packageId, version);
            }
            catch (Exception)
            {
                // Reported by the emit pass, against the row it concerns.
            }
        }
    }
}
