using System.Management.Automation;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

/// <summary>
/// The shared shape of every "walk a set of folders, fetch one thing per entity in them,
/// print the results a folder at a time" cmdlet — Get-OrchProcessDetail and its kin.
/// </summary>
/// <remarks>
/// Two phases, each one pool:
///
///   1. List the entities of every folder. One cheap call per folder (the folder tree itself
///      is a single cached listing). Doing it up front is what lets phase 2 be a single pool
///      and the one progress bar carry a denominator for the whole run.
///   2. Every (folder, entity) pair in ONE pool, so the four slots never idle at a folder
///      boundary: once a folder has three entities left, the free worker is already pulling
///      the next folder's first. A pool per folder cannot do that, and on a tenant whose
///      folders hold one or two entities each it leaves most of the parallelism unused.
///
/// Do not expect four-wide to be four times faster. The pool is not what decides: the
/// RateLimiter on OrchAPISession allows 15 requests a second, and one thread issuing a fast
/// call reaches that on its own, so the other three workers end up waiting for tokens rather
/// than for the server. Measured on Get-OrchTriggerDetail over 200 triggers, 25.6 s four-wide
/// against 27.3 s serial, with the pool genuinely running 3.99 at once. What this shape buys
/// is the ORDER and the TIMING of the output. For throughput, cut the NUMBER of requests
/// instead -- see the comment on that RateLimiter, and GetProcessSchedules for what it looks
/// like when 401 requests become 1.
///
/// This depends on <see cref="OrchThreadPoolImpl{TSource,TResult}.RunForEach"/> starting its
/// work in source order — which it does, a fixed crew pulling the next index. An earlier
/// version of that pool queued one Task.Run per source behind a semaphore, and the order they
/// reached the semaphore was whatever order the thread pool started them in: a scramble across
/// the whole list. Every folder's entities then spread over the entire run, so every folder's
/// LAST entity landed near the end and almost nothing could be printed until then. Measured on
/// 212 releases in 28 folders: the first two folders printed at 8.5 s and 10.5 s, and the other
/// 200 rows all arrived in the final second of a 49 s run. With ordered starts that is gone:
/// the pool still runs ahead of the consumer whenever one call is slow — the other three
/// workers keep pulling, with no bound on how far they get — but it is now one slow call
/// holding one folder, not every folder's last entity landing at the end of the run.
///
/// Rows go out one folder at a time because the table views group by Path and size their
/// columns from the first batch of a group; emitting row by row let the first row of a folder
/// set the column widths for every longer value behind it. Where each folder's run of entities
/// ends is taken from the phase 1 group sizes, so a folder is emitted on its OWN last entity —
/// waiting to notice a change of path cannot fire until the NEXT folder's first entity is back.
///
/// Progress is one bar, handed from phase 1 to phase 2. It counts API calls that have returned
/// (<see cref="OrchThreadPoolImpl{TSource,TResult}.CompletedCount"/>), never rows printed:
/// slots drain in submission order, so a consumer waiting on one slot emits nothing while the
/// other three workers finish behind it, and counting emissions holds the bar still through
/// that and then snaps it forward. The status is the PSPath of the entity the consumer is on,
/// so it names a call that just came back without ever naming one from a folder whose rows
/// have not been printed. While one call is slow the count keeps climbing and that name holds
/// — the honest reading of "still waiting on this one, N others came back meanwhile".
///
/// Fetching happens on pool threads, so <paramref name="fetch"/> must not touch the cmdlet
/// (no WriteError, no WriteObject). Anything that has to run on the pipeline thread — reading
/// a cache the fetch warmed, reporting a non-fatal problem — belongs in
/// <paramref name="onRow"/>.
/// </remarks>
internal static class FolderFanOut
{
    /// <param name="errorId">ErrorRecord id for both phases, e.g. "GetProcessDetailError".</param>
    /// <param name="listActivity">Progress activity during phase 1, e.g. "Listing processes".</param>
    /// <param name="fetchActivity">Progress activity during phase 2.</param>
    /// <param name="list">A folder's entities, in output order. Runs on a pool thread.</param>
    /// <param name="itemPath">An entity's PSPath — the progress status and the error path.</param>
    /// <param name="fetch">The per-entity call. Runs on a pool thread. Null drops the row.</param>
    /// <param name="emit">One folder's rows, never empty. Runs on the pipeline thread.</param>
    /// <param name="onRow">Optional per-row step on the pipeline thread, before the row joins
    /// its folder's batch.</param>
    public static void Emit<TItem, TRow>(
        OrchestratorPSCmdlet caller,
        IEnumerable<(OrchDriveInfo drive, Folder folder)> drivesFolders,
        string errorId,
        string listActivity,
        string fetchActivity,
        Func<OrchDriveInfo, Folder, IEnumerable<TItem>> list,
        Func<TItem, string> itemPath,
        Func<OrchDriveInfo, Folder, TItem, TRow?> fetch,
        Action<OrchDriveInfo, Folder, List<TRow>> emit,
        Action<OrchDriveInfo, Folder, TRow>? onRow = null)
        where TItem : class
        where TRow : class
    {
        using var cancelHandler = new ConsoleCancelHandler();

        var folders = drivesFolders.ToList();

        // Phase 1. Folders that matched nothing drop out here.
        //
        // One bar per phase, the first taken down before the second goes up, as every other
        // list-then-fetch cmdlet does. Relabelling one bar between the phases rewrote the
        // label of a bar already on screen, which is what moving every varying value out of
        // the activity line was meant to stop.
        var groups = new List<(OrchDriveInfo drive, Folder folder, List<TItem> items)>();

        using (var reporter = new ProgressReporter(caller, folders.Count, listActivity))
        using (var listPool = OrchThreadPool.RunForEach(folders,
            df => df.folder.GetPSPath(),
            df => df.folder,
            df => list(df.drive, df.folder).ToList()))
        {
            int f = 0;
            foreach (var task in listPool)
            {
                // The pool yields its slots in submission order, so this walks `folders` in
                // step with it -- and unlike task.Source it is readable even when the task
                // threw.
                var (drive, folder) = folders[f++];
                try
                {
                    var items = listPool.GetResultWithProgress(task, reporter, cancelHandler.Token);
                    if (items is { Count: > 0 }) groups.Add((drive, folder, items));
                }
                catch (OrchException ex)
                {
                    caller.WriteError(new ErrorRecord(ex, errorId, ErrorCategory.InvalidOperation, ex.Target));
                }
            }
        }

        // Flatten, recording each folder's last position as we go: the group sizes say exactly
        // where the boundaries are, so nothing has to be compared later.
        var targets = new List<(OrchDriveInfo drive, Folder folder, TItem item)>();
        var endsFolder = new List<bool>();

        foreach (var (drive, folder, items) in groups)
        {
            for (int i = 0; i < items.Count; i++)
            {
                targets.Add((drive, folder, items[i]));
                endsFolder.Add(i == items.Count - 1);
            }
        }

        // Phase 2. Nothing to fetch puts no bar up; the reporter only shows once written to.
        using var fetchReporter = new ProgressReporter(caller, targets.Count, fetchActivity);

        using var results = OrchThreadPool.RunForEach(targets,
            t => itemPath(t.item),
            t => t.item,
            t => fetch(t.drive, t.folder, t.item));

        var batch = new List<TRow>();
        int index = 0;

        foreach (var result in results.WithCancellation(cancelHandler.Token))
        {
            // targets[index], not result.Source: the pool drains in submission order, so the
            // two are the same tuple -- but Source may only be read after GetResult, which
            // rules it out of the catch and the finally.
            var (drive, folder, _) = targets[index];
            try
            {
                var row = results.GetResultWithProgress(result, fetchReporter, cancelHandler.Token);
                if (row is null) continue;

                onRow?.Invoke(drive, folder, row);
                batch.Add(row);
            }
            catch (OrchException ex)
            {
                caller.WriteError(new ErrorRecord(ex, errorId, ErrorCategory.InvalidOperation, ex.Target));
            }
            finally
            {
                // In a finally so an entity that was dropped or that failed still closes its
                // folder: otherwise one bad entity would strand every row ahead of it in the
                // same folder, with nothing left to trigger the emit. Ctrl+C throws past this
                // loop, abandoning a part-built batch.
                if (endsFolder[index])
                {
                    if (batch.Count > 0) emit(drive, folder, batch);
                    batch.Clear();
                }
                index++;
            }
        }
    }

    /// <summary>
    /// The one-phase shape, for when a folder's listing already IS the detail — one request
    /// per folder answers everything, as with Get-OrchTriggerDetail's expanded listing.
    /// </summary>
    /// <remarks>
    /// Do not pass such a listing to <see cref="Emit{TItem,TRow}"/> with a fetch that returns
    /// its argument. Phase 2 then issues no requests, so all the waiting sits in phase 1,
    /// which lists EVERY folder before anything is printed. Here each folder is emitted the
    /// moment its own listing is back; the pool drains in submission order, so the folders
    /// still come out in the order they were given.
    /// </remarks>
    /// <param name="errorId">ErrorRecord id, e.g. "GetTriggerDetailError".</param>
    /// <param name="activity">Progress activity, e.g. "Getting trigger details".</param>
    /// <param name="list">A folder's rows, in output order. Runs on a pool thread.</param>
    /// <param name="emit">One folder's rows, never empty. Runs on the pipeline thread.</param>
    public static void EmitListed<TRow>(
        OrchestratorPSCmdlet caller,
        IEnumerable<(OrchDriveInfo drive, Folder folder)> drivesFolders,
        string errorId,
        string activity,
        Func<OrchDriveInfo, Folder, IEnumerable<TRow>> list,
        Action<OrchDriveInfo, Folder, List<TRow>> emit)
        where TRow : class
    {
        using var cancelHandler = new ConsoleCancelHandler();

        var folders = drivesFolders.ToList();

        using var reporter = new ProgressReporter(caller, folders.Count, activity);
        using var results = OrchThreadPool.RunForEach(folders,
            df => df.folder.GetPSPath(),
            df => df.folder,
            df => list(df.drive, df.folder).ToList());

        int f = 0;
        foreach (var result in results.WithCancellation(cancelHandler.Token))
        {
            // folders[f], not result.Source, for the same reason as in Emit's phase 2.
            var (drive, folder) = folders[f++];
            try
            {
                var rows = results.GetResultWithProgress(result, reporter, cancelHandler.Token);
                if (rows is { Count: > 0 }) emit(drive, folder, rows);
            }
            catch (OrchException ex)
            {
                caller.WriteError(new ErrorRecord(ex, errorId, ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
