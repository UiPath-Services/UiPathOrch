using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

/// <summary>
/// Downloads the packages a cmdlet is about to read, one at a time, while its caller emits
/// rows for each as it arrives.
///
/// Reading dependencies or workflows means downloading whole `.nupkg` files (Orchestrator has
/// no lighter endpoint), and a tenant-wide sweep can touch dozens. Listing folders and
/// processes is cheap and runs in parallel; the downloads are deliberately sequential, so a
/// sweep is one request at a time against the server rather than four.
///
/// An earlier version downloaded everything first and emitted afterwards, which left the
/// console empty behind a progress bar for the whole sweep. Rows now appear as their package
/// arrives. The bar keeps a real denominator anyway: the distinct packages still to fetch are
/// counted up front — before the first request — so an unexpectedly large sweep is visible
/// immediately and can be cancelled, and a package already in the cache advances nothing.
/// </summary>
internal sealed class PackageContentsLoader : IDisposable
{
    private readonly ConsoleCancelHandler _cancelHandler;
    private readonly ProgressReporter? _reporter;
    private readonly HashSet<(string drive, string feed, string id, string version)> _pending;
    private int _done;

    /// <param name="targets">Every package the caller will read, in the order it will read them.</param>
    internal PackageContentsLoader(
        OrchestratorPSCmdlet cmdlet,
        IEnumerable<(OrchDriveInfo drive, Folder folder, string packageId, string version)> targets,
        ConsoleCancelHandler cancelHandler,
        string activity)
    {
        _cancelHandler = cancelHandler;

        // What is already cached costs nothing, so it is not part of the count the user sees.
        var cachedByDrive = new Dictionary<string, HashSet<(string, string, string)>>();
        _pending = [];

        foreach (var (drive, folder, packageId, version) in targets)
        {
            string driveName = drive.NameColonSeparator;
            if (!cachedByDrive.TryGetValue(driveName, out var cached))
            {
                cached = drive.PackageContents.CachedEntries.Select(e => e.Key).ToHashSet();
                cachedByDrive[driveName] = cached;
            }

            string feedId = drive.FolderFeedId.Get(folder) ?? "";
            if (cached.Contains((feedId, packageId, version))) continue;

            _pending.Add((driveName, feedId, packageId, version));
        }

        if (_pending.Count > 0)
        {
            _reporter = new ProgressReporter(cmdlet, 2, _pending.Count, activity);
        }
    }

    /// <summary>
    /// The package's contents, downloading it if this is the first ask. Advances the bar only
    /// for a real download, so the count the caller was shown stays honest.
    /// </summary>
    internal PackageContents? Load(OrchDriveInfo drive, Folder folder, string packageId, string version)
    {
        _cancelHandler.Token.ThrowIfCancellationRequested();

        string feedId = drive.FolderFeedId.Get(folder) ?? "";
        var key = (drive.NameColonSeparator, feedId, packageId, version);

        if (_pending.Remove(key))
        {
            _reporter?.WriteProgress(++_done, $"{packageId}:{version}");
        }

        return drive.GetPackageContents(folder, packageId, version);
    }

    public void Dispose() => _reporter?.Dispose();
}
