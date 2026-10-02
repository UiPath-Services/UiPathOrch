using System.Collections;
using System.Collections.Concurrent;
using System.Data;
using System.Management.Automation;
using System.Text.RegularExpressions;
using UiPath.PowerShell.Commands;

namespace UiPath.PowerShell.Core;

public static class PathTools
{
    // Use this when displaying candidates in a cmdlet completer.
    public static string EscapePSText(string? input)
    {
        //            return "'" + WildcardPattern.Escape(input) + "'";
        if (string.IsNullOrEmpty(input))
        {
            return "";
        }

        string ret = input
            .Replace("`", "``")
            .Replace("'", "''")
            .Replace("*", "`*")
            .Replace("?", "`?")
            .Replace("[", "`[")
            .Replace("]", "`]");

        if (input != ret || input.Contains(' ') || input.Contains(',') || input.Contains(Path.DirectorySeparatorChar) || input.Contains('\'') || input.Contains('"')) //★★★
            //ret = '\'' + ret + '\'';
            ret = $"'{ret}'";

        return ret;
    }

    // Quotes a completer candidate WITHOUT escaping wildcard metacharacters (unlike
    // EscapePSText): use when the value binds literally and `* ? [ ]` must stay literal
    // inside the surrounding single quotes.
    public static string EscapeNonWildcardText(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return "''";
        return "'" + input.Replace("'", "''") + "'";
    }

    // Escape a value for insertion INSIDE an OData single-quoted string literal:
    // OData escapes a single quote by doubling it (''). Callers supply the
    // surrounding quotes — this does NOT add them and does NOT URL-encode. Apply
    // exactly once on the RAW value (before any Uri.EscapeDataString); escaping an
    // already-escaped value would double the doubling.
    public static string EscapeODataLiteral(string? input) => (input ?? string.Empty).Replace("'", "''");

    // Shared syntactic path validation for the Orchestrator providers (the hierarchical
    // OrchProvider and the flat DU/TM shadows). Reports whether `path` is well-formed enough to
    // NAME an item — it is NOT an existence check (that is the provider's ItemExists). Mirrors the
    // built-in providers: reject null/empty, accept the drive root, and reject control characters
    // (which can never appear in a real folder/project name; spaces and ordinary punctuation are
    // fine). Both OrchProvider and OrchShadowProviderBase delegate here so the rule lives in one
    // place. Reached from `Test-Path -IsValid` and from each provider's NormalizeRelativePath guard.
    public static bool IsValidProviderPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        // Reduce to the Orchestrator path (drive qualifier stripped, separators normalized to '/',
        // leading/trailing separators trimmed). An empty result is the drive root — a valid location.
        string orchPath = OrchDriveInfo.PSPathToOrchPath(path);
        if (orchPath.Length == 0)
            return true;

        foreach (char c in orchPath)
            if (char.IsControl(c))
                return false;

        return true;
    }

    // Shared GetChildName for the Orchestrator providers, mirroring FileSystemProvider.GetChildName
    // + EnsureDriveIsRooted: trim a trailing separator, return the last segment, but re-root a bare
    // drive ("Orch1:" -> "Orch1:\") so a drive-root leaf round-trips as a usable path. The base
    // NavigationCmdletProvider trims but does not re-root, so without this `Split-Path X:\ -Leaf`
    // yields "X:". Throws on null/empty like the built-in providers.
    public static string GetChildNameWithDriveRoot(string path)
    {
        if (string.IsNullOrEmpty(path))
            throw new ArgumentException("The path cannot be null or empty.", nameof(path));

        char sep = Path.DirectorySeparatorChar;
        string normalized = path.Replace(Path.AltDirectorySeparatorChar, sep).TrimEnd(sep);

        int separatorIndex = normalized.LastIndexOf(sep);
        if (separatorIndex == -1)
            return normalized.EndsWith(':') ? normalized + sep : normalized;

        return normalized.Substring(separatorIndex + 1);
    }

    // Symmetric to GetChildNameWithDriveRoot, for the parent side. The base
    // NavigationCmdletProvider.GetParentPath trims the drive root to a bare "Orch1:", so a
    // top-level item's parent loses its separator: PSParentPath renders "Orch1:" and the Folder
    // table's "Directory:" group header shows "Orch1:" instead of "Orch1:\" (and Split-Path
    // -Parent of a root child yields "Orch1:"). FileSystemProvider keeps the root separator;
    // re-root a bare drive here so each provider's GetParentPath matches. Only a bare drive
    // qualifier ends with ':' (folder/project names cannot contain ':'), so the guard is exact.
    // `parentPath` is the base-computed parent; an empty parent (above the root) is left as-is.
    public static string ParentPathWithDriveRoot(string parentPath)
        => !string.IsNullOrEmpty(parentPath) && parentPath.EndsWith(':')
            ? parentPath + Path.DirectorySeparatorChar
            : parentPath;

    // The drive-root half of NormalizeRelativePath, shared by OrchProvider and the DU/TM shadow
    // providers. Returns the relative path when `basePath` IS the drive root, else null to mean
    // "not my case — fall through to the base NavigationCmdletProvider".
    //
    // This is the mandatory counterpart to ParentPathWithDriveRoot above. The base provider
    // relativizes by walking up GetParentPath and comparing against the base; that walk compares
    // against the TRIMMED base ("Orch1:"), while our GetParentPath deliberately returns the ROOTED
    // "Orch1:\" — so the walk never matches, gives up, and hands back the path unrelativized. Tab
    // completion then renders the result as ".\Orch1:\Autopilot" instead of ".\Autopilot".
    // FileSystemProvider avoids the whole problem by relativizing on a string prefix; do the same.
    //
    // Any provider that adopts ParentPathWithDriveRoot MUST also call this, or its relative paths
    // (hence its tab completion) break. Keeping both halves here is what stops the pair from
    // drifting apart again — the shadow providers once took the re-rooting without this.
    public static string? RelativizeFromDriveRoot(string? path, string? basePath)
    {
        if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(basePath)) return null;

        // Not the drive root (a nested base) — the base provider's walk handles it correctly.
        if (OrchDriveInfo.PSPathToOrchPath(basePath).Length != 0) return null;

        return OrchDriveInfo.PSPathToOrchPath(path).Replace('/', Path.DirectorySeparatorChar);
    }

    // Normalize Rename-Item's -NewName, matching the FileSystem provider. PowerShell passes the raw
    // argument straight to RenameItem; tab completion commonly yields ".\Foo" (which would otherwise
    // be stored verbatim, so `ren .\Shared .\Shared2` would name the folder ".\Shared2"). Strip a
    // leading "./" / ".\"; allow a fully-qualified new name only when it stays in the same directory
    // as the source; otherwise the name must be a bare leaf. `path` is the source item's path (for
    // the same-directory check). Returns null when the new name is empty, "."/".." or still carries
    // directory information (e.g. `ren .\sub3 ..\sub5`) — Rename-Item renames in place, not moves —
    // so the caller can raise a clear error.
    public static string? RenameLeaf(string? path, string? newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            return null;

        newName = newName.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        // Strip a leading "./" or ".\" — what tab completion adds (`ren .\Foo .\Bar`).
        if (newName.StartsWith(".\\", StringComparison.Ordinal) || newName.StartsWith("./", StringComparison.Ordinal))
        {
            newName = newName.Substring(2);
        }
        // Allow a fully-qualified new name only when it stays in the SAME directory as the source
        // (`ren X\foo X\bar` -> bar). Anything pointing elsewhere is rejected below.
        else if (!string.IsNullOrEmpty(path) &&
                 string.Equals(Path.GetDirectoryName(path), Path.GetDirectoryName(newName), StringComparison.OrdinalIgnoreCase))
        {
            newName = Path.GetFileName(newName);
        }

        // The result must be a bare leaf. A name that still carries directory information
        // (e.g. `..\sub5`, `sub\x`, a different folder's path) is rejected: Rename-Item renames
        // in place, it does not move. Caller turns null into a clear error.
        if (string.IsNullOrEmpty(newName) || newName == "." || newName == ".." ||
            !string.Equals(Path.GetFileName(newName), newName, StringComparison.Ordinal))
        {
            return null;
        }

        return newName;
    }

    public static bool IsEscapedPSText(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return false;
        }

        return (input.Length >= 2 && input.StartsWith('\'') && input.EndsWith('\''));
    }

    // this return value should be passed to WildcardPattern ctor
    public static string UnescapePSText(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return "";
        }

        if (IsEscapedPSText(input))
        {
            input = input.Substring(1, input.Length - 2);
            input = input.Replace("''", "'");
        }

        return input;
    }

    private static string ExtractVersion(string fullPath)
    {
        string fileName = System.IO.Path.GetFileName(fullPath);
        var match = Regex.Match(fileName, @"(\d+\.\d+\.\d+(-[a-zA-Z0-9]+)?)");
        return match.Success ? match.Value : "";
    }

    // Used in combination with OrchDriveInfo.ExpandLocalPath()
    public static IEnumerable<(string FullPath, string RelativePath)> OrderByFileNameVersion(
        this IEnumerable<(string FullPath, string RelativePath)> files)
    {
        return files
            .Select(file => new
            {
                file.FullPath,
                file.RelativePath,
                Version = ExtractVersion(file.FullPath)
            })
            .OrderBy(item => System.IO.Path.GetDirectoryName(item.FullPath))
            .ThenBy(item => item.Version, VersionComparer.Instance)
            .Select(item => (item.FullPath, item.RelativePath));
    }
}

public class ConsoleCancelHandler : IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private readonly ConsoleCancelEventHandler _handler;
    private bool _cancelKeyPressed;

    // Property indicating whether the cancel key was pressed
    public bool CancelKeyPressed => _cancelKeyPressed;

    // Property exposing the CancellationTokenSource
    public CancellationToken Token => _cts.Token;

    public ConsoleCancelHandler()
    {
        _handler = (sender, args) =>
        {
            _cancelKeyPressed = true;
            _cts.Cancel();
            args.Cancel = true; // prevent the process from terminating
        };
        Console.CancelKeyPress += _handler;
    }

    // Dispose pattern implementation
    public void Dispose()
    {
        Console.CancelKeyPress -= _handler;
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Sugar for the two cancel-related boilerplates that crop up in nearly
/// every cmdlet that uses <see cref="ConsoleCancelHandler"/>.
/// </summary>
public static class CancellationExtensions
{
    /// <summary>
    /// Wraps an enumerable so each iteration throws
    /// <see cref="OperationCanceledException"/> if the token is signaled
    /// before the next element is produced. Drop-in for the boilerplate
    /// <c>foreach (var x in xs) { token.ThrowIfCancellationRequested(); ... }</c>:
    /// write <c>foreach (var x in xs.WithCancellation(token)) { ... }</c>
    /// instead. Place the call on the source the loop iterates so the body
    /// stays uncluttered.
    /// </summary>
    public static IEnumerable<T> WithCancellation<T>(this IEnumerable<T> source, CancellationToken token)
    {
        foreach (var item in source)
        {
            token.ThrowIfCancellationRequested();
            yield return item;
        }
    }

    /// <summary>
    /// Cancellable replacement for <see cref="Thread.Sleep(int)"/>. Blocks
    /// for at most <paramref name="millisecondsTimeout"/> ms, returning early
    /// if the token is signaled. Useful for rate-limit pacing between API
    /// calls where Ctrl+C should not be blocked for the full delay.
    /// </summary>
    public static void Sleep(this CancellationToken token, int millisecondsTimeout)
    {
        token.WaitHandle.WaitOne(millisecondsTimeout);
    }
}

/// <summary>
/// Sugar for attaching a progress bar to a single-threaded loop, mirroring
/// <see cref="CancellationExtensions.WithCancellation{T}"/>. Replaces the
/// manual <c>reporter</c> + <c>++index</c> + <c>WriteProgress</c> boilerplate:
/// write <c>foreach (var x in xs.WithProgressBar(this, "Doing things", n =&gt; n.Name)) { ... }</c>.
/// Compose with <see cref="CancellationExtensions.WithCancellation{T}"/> on the OUTSIDE so the
/// cancel check runs per body iteration: <c>xs.WithProgressBar(...).WithCancellation(token)</c>.
/// For PARALLEL <c>RunForEach</c> loops use <see cref="OrchThreadPoolImpl{TSource,TResult}.GetResultWithProgress"/>
/// instead (it shows true background-completion progress).
/// </summary>
public static class ProgressExtensions
{
    /// <summary>
    /// Wraps a sequence so each element, as it is yielded, advances a bar showing
    /// "{index}/{total} {name}". The bar auto-completes when enumeration ends or the iterator
    /// is disposed (e.g. an early <c>break</c>). Width-unsafe names (PowerShell #21293) are
    /// sanitized by <see cref="ProgressReporter"/>.
    /// </summary>
    /// <param name="host">The cmdlet / provider that renders the bar.</param>
    /// <param name="activity">The activity line (operation, optionally with destination).</param>
    /// <param name="getName">Per-item status name; null shows just "{index}/{total}".</param>
    /// <param name="total">Item count for the percentage. Defaults to the source count,
    /// materializing the source once if it isn't already an <see cref="ICollection{T}"/>.
    /// Pass a known total to avoid materializing a lazy/expensive source.</param>
    /// <param name="id">Progress activity id; give nested bars distinct ids (1, 2, 3...).</param>
    public static IEnumerable<T> WithProgressBar<T>(
        this IEnumerable<T> source,
        IWritableHost host,
        string activity,
        Func<T, string?>? getName = null,
        int total = -1,
        int id = 1)
    {
        IEnumerable<T> sequence = source;
        if (total < 0)
        {
            var materialized = source as ICollection<T> ?? source.ToList();
            total = materialized.Count;
            sequence = materialized;
        }

        using var reporter = new ProgressReporter(host, id, total, activity);
        int index = 0;
        foreach (var item in sequence)
        {
            reporter.WriteProgress(++index, getName?.Invoke(item));
            yield return item;
        }
    }
}

public class OrchTask<TSource, TResult> : IDisposable
{
    public ManualResetEventSlim CompletedEvent { get; }
    private TSource? _source;
    public TSource Source => _source ?? throw new InvalidOperationException("Call GetResult() before accessing Source.");
    public string? Path { get; private set; }
    public object? Target { get; private set; }
    public TResult? Result { get; private set; }
    public Exception? Exception { get; private set; }

    // Display label (the getPathFunc result), stamped the moment a worker picks this slot up
    // and before its API call runs, so a consumer that is blocked draining this
    // not-yet-completed slot can still show "what am I waiting on". (Path above is only set on
    // the exception path; Label is set on every slot a worker reaches.)
    public string? Label { get; private set; }
    internal void SetLabel(string label) => Label = label;

    public OrchTask()
    {
        CompletedEvent = new ManualResetEventSlim(false);
    }

    public void SetResult(TSource source, TResult result)
    {
        _source = source;
        Result = result;
        CompletedEvent.Set(); // Signal that processing is complete
    }

    public void SetException(TSource source, string path, object target, Exception ex)
    {
        _source = source;
        Path = path;
        Target = target;
        Exception = ex;
        CompletedEvent.Set(); // Signal that processing is complete
    }

    // Blocks until this slot's background fetch completes, then returns its
    // result (or rethrows the captured error as OrchException). Call this
    // directly when no progress bar is needed; when you do want one, call
    // OrchThreadPoolImpl.GetResultWithProgress() instead so the bar keeps
    // advancing (with the true completed-count) while this slot is awaited.
    public TResult? GetResult(CancellationToken token)
    {
        // ManualResetEventSlim.Wait returns immediately if IsSet is already
        // true and never observes the token in that fast path. When the
        // consumer is draining a backlog of already-completed tasks (e.g.
        // -Recurse over many small folders), Ctrl+C wouldn't fire until the
        // consumer hit a not-yet-complete task. Explicit check up front so
        // cancel propagates on every iteration.
        token.ThrowIfCancellationRequested();
        CompletedEvent.Wait(token);

        if (Exception is not null)
        {
            var e = new OrchException(Path!, Exception!)
            {
                Target = Target!
            };
            throw e;
        }
        return Result;
    }

    public void Dispose()
    {
        CompletedEvent.Dispose();
        GC.SuppressFinalize(this);
    }
}

// RunForEach() of this class does not block the main thread.
// When calling GetResult() on each item retrieved via foreach from an instance of this class,
// it waits until the corresponding thread has finished.
public class OrchThreadPoolImpl<TSource, TResult> : IDisposable, IEnumerable<OrchTask<TSource, TResult>>
{
    private readonly OrchTask<TSource, TResult>[] _threads;

    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentBag<Task> _backgroundTasks = [];

    // Token that cancels when the pool is disposed. Used by RunForEach's
    // internal Task.Run lambdas so they bail out promptly on Ctrl+C / consumer
    // abandonment. Internal: external code constructs pools via the
    // OrchThreadPool static class only.
    internal CancellationToken Token => _cts.Token;

    // Total number of source work items.
    public int Count => _threads.Length;

    // Number of background queries that have actually finished, independent of the sorted
    // drain order -- so a consumer can show TRUE progress while it is blocked draining an
    // early-but-slow item (the rest fetch in parallel behind it). Cheap IsSet reads; n is the
    // source count and the caller throttles how often it polls this.
    public int CompletedCount
    {
        get
        {
            int n = 0;
            foreach (var t in _threads)
            {
                if (t.CompletedEvent.IsSet) n++;
            }
            return n;
        }
    }


    // Drains one slot in sorted order while keeping the bar filled with the TRUE number of
    // background fetches completed (CompletedCount) -- so it advances even while output is
    // blocked on an early-but-slow item, the rest fetching in parallel behind it. The status
    // name is this slot's label (what we are currently waiting on). Must be called from the
    // pipeline thread, since it calls reporter.WriteProgress. When no progress bar is needed,
    // call result.GetResult(token) directly instead -- it skips the polling wait and the
    // WriteProgress calls.
    // The bar follows CompletedCount -- API calls that have come back -- and never the number
    // of items the consumer has emitted. The two are not the same: slots drain in sorted
    // order, so a consumer blocked on slot 0 emits nothing while the other three workers
    // finish behind it, and counting emissions would hold the bar at zero for that whole
    // stretch and then snap it to full as the backlog flushed.
    //
    public TResult? GetResultWithProgress(OrchTask<TSource, TResult> result, ProgressReporter reporter, CancellationToken token)
    {
        // The count is every completion; the label is only ever THIS slot -- the one the
        // consumer is on. That is what keeps the two readable together: the consumer walks the
        // slots in order, so the name can never run ahead into a folder whose rows have not
        // been printed, and the write that follows GetResult below names the call that just
        // came back.
        //
        // The two can therefore drift: the crew does not wait for the consumer, so while one
        // slow call is out the other three workers keep pulling, and the count climbs while
        // this label holds. There is no bound on that -- one call slow enough lets the rest of
        // the crew finish the list -- and it should hold: "still waiting on this one, N others
        // came back meanwhile" is what is actually happening.
        //
        // Naming the pool's most recent completion instead was tried, and it reads as a folder
        // being finished while its rows are still queued. It was also only ever a workaround
        // for an unordered pool, where EVERY slot drifted that way because starts were
        // scrambled across the whole list, not just the slot behind a slow call.
        //
        // Label is re-read on every poll: a worker stamps it when it picks the slot up, so it
        // can still be null for a moment on the first look.
        while (!result.CompletedEvent.Wait(150, token))
        {
            reporter.WriteProgress(CompletedCount, result.Label);
        }
        TResult? value = result.GetResult(token);
        reporter.WriteProgress(CompletedCount, result.Label);
        return value;
    }

    // Track a worker Task so Dispose can give the crew a moment to signal the slots it has
    // not reached before the OrchTasks are torn down under it. Internal for the same reason
    // as Token.
    internal void TrackTask(Task task) => _backgroundTasks.Add(task);

    private OrchThreadPoolImpl(OrchTask<TSource, TResult>[] threads)
    {
        _threads = threads;
    }

    // A fixed crew of workers, each pulling the next index in order, rather than one Task.Run
    // per source queued behind a SemaphoreSlim. Same ceiling on concurrent API calls, but the
    // work STARTS in source order: item i cannot begin before i-4 has, so completions stay
    // within a few places of submission order.
    //
    // Queueing every source at once did not. Each body ran only as far as its first await, so
    // all of them reached the semaphore within a moment, in whatever order the thread pool
    // happened to hand them to its workers -- a scramble across the whole list, not a local
    // one -- and the semaphore's FIFO then honoured that scrambled order for the rest of the
    // run. Every folder's items ended up spread over the entire run, so every folder's LAST
    // item landed near the end, and a consumer that emits a folder once its items are all in
    // could emit almost nothing until then. Measured on 212 releases in 28 folders: the first
    // two folders printed at 8.5 s and 10.5 s, and the other 200 rows all arrived in the final
    // second of a 49 s run.
    //
    // SetResult is called directly on the worker thread (no SynchronizationContext.Post
    // marshaling). Each item writes to its own pre-allocated slot in `threads`, so there is no
    // write race; calling Post would only enqueue work to a SyncContext that the consumer
    // thread isn't pumping (it's blocked in CompletedEvent.Wait), risking a message-pump
    // deadlock in WPF-style hosts. PowerShell pipeline threads have no SyncContext, so the
    // marshaling was a no-op anyway.
    internal static OrchThreadPoolImpl<TSource, TResult> RunForEach(IEnumerable<TSource> sources,
        Func<TSource, string> getPathFunc,
        Func<TSource, object> getTargetFunc,
        Func<TSource, TResult> getResultFunc, int maxDegreeOfParallelism = 4)
    {
        var srcList = sources as IList<TSource> ?? sources.ToList();
        var threads = new OrchTask<TSource, TResult>[srcList.Count];

        for (int i = 0; i < threads.Length; i++)
        {
            threads[i] = new OrchTask<TSource, TResult>();
        }

        var pool = new OrchThreadPoolImpl<TSource, TResult>(threads);
        var token = pool.Token;

        int next = -1;
        int crew = Math.Min(maxDegreeOfParallelism, srcList.Count);

        for (int w = 0; w < crew; w++)
        {
            pool.TrackTask(Task.Run(() =>
            {
                while (true)
                {
                    int index = Interlocked.Increment(ref next);
                    if (index >= srcList.Count) return;

                    var source = srcList[index];

                    // Pre-compute path/target so the SetException calls below can't themselves
                    // throw. If the getter funcs fault (e.g. GetPSPath on a stale entity) we
                    // still need to signal the slot — otherwise the consumer's
                    // CompletedEvent.Wait would block forever.
                    string pathStr;
                    object targetObj;
                    try
                    {
                        pathStr = getPathFunc(source);
                        targetObj = getTargetFunc(source);
                    }
                    catch (Exception funcEx)
                    {
                        threads[index].SetException(source, "<getPathFunc/getTargetFunc threw>", source!, funcEx);
                        continue;
                    }

                    threads[index].SetLabel(pathStr);

                    // After a cancel the crew keeps pulling, but only to signal what is left:
                    // a consumer still reading would otherwise block on a slot no one will run.
                    if (token.IsCancellationRequested)
                    {
                        threads[index].SetException(source, pathStr, targetObj, new OperationCanceledException(token));
                        continue;
                    }

                    try
                    {
                        var result = getResultFunc(source);
                        threads[index].SetResult(source, result);
                    }
                    catch (Exception ex)
                    {
                        threads[index].SetException(source, pathStr, targetObj, ex);
                    }
                }
            }));
        }

        return pool;
    }

    public void Dispose()
    {
        // Cancel so the crew stops taking new work and races through what is left, signalling
        // each remaining slot as cancelled.
        try { _cts.Cancel(); } catch { }

        // Brief wait so that sweep (microseconds per slot) can finish. Don't wait for an
        // in-flight synchronous API call — it can't be cancelled mid-flight (no token plumbed
        // through OrchAPISession), so waiting only delays Ctrl+C response without changing
        // what the user observes. A straggler calling task.SetResult after dispose throws
        // ObjectDisposedException, which is silent under the .NET 5+ UnobservedTaskException
        // default.
        try { Task.WaitAll([.. _backgroundTasks], TimeSpan.FromMilliseconds(100)); } catch { }

        foreach (var thread in _threads)
        {
            thread.Dispose(); // Release resources
        }
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }

    public IEnumerator<OrchTask<TSource, TResult>> GetEnumerator()
    {
        foreach (var thread in _threads)
        {
            yield return thread;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

public static class OrchThreadPool
{
    public static OrchThreadPoolImpl<TSource, TResult> RunForEach<TSource, TResult>(
        IEnumerable<TSource> sources,
        Func<TSource, string> getPathFunc,
        Func<TSource, object> getTargetFunc,
        Func<TSource, TResult> getResultFunc)
    {
        return OrchThreadPoolImpl<TSource, TResult>.RunForEach(sources, getPathFunc, getTargetFunc, getResultFunc);
    }
}

/// <summary>
/// Holds a <typeparamref name="TItem"/> and its associated <typeparamref name="TSource"/>.<br/>
/// - Can be used directly as the item (e.g., bucket.Name) via implicit conversion.<br/>
/// - The original data is accessible via bucket.Source.
/// </summary>
public sealed class WithSource<TSource, TItem>
{
    public TSource Source { get; }
    public TItem Item { get; }

    public WithSource(TSource source, TItem item)
    {
        Source = source;
        Item = item;
    }

    /// <summary>
    /// Provides implicit conversion from <c>WithSource</c> to <typeparamref name="TItem"/>
    /// so it can be used directly in LINQ as <c>b.Name</c>.
    /// </summary>
    public static implicit operator TItem(WithSource<TSource, TItem> self) => self.Item;

    /// <summary>
    /// Allows pattern-matching deconstruction like <code>var (source, item) = withSource;</code>.
    /// </summary>
    public void Deconstruct(out TSource source, out TItem item)
    {
        source = Source;
        item = Item;
    }

    public override string ToString() => Item?.ToString() ?? string.Empty;
}

public sealed class SourceGroup<TSource, TItem> : IEnumerable<TItem>
{
    public TSource Source { get; }
    private readonly IReadOnlyList<WithSource<TSource, TItem>> _items;

    public SourceGroup(TSource source, IReadOnlyList<WithSource<TSource, TItem>> items)
    {
        Source = source;
        _items = items;
    }

    public IEnumerator<TItem> GetEnumerator() => _items.Select(x => x.Item).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerable<WithSource<TSource, TItem>> WithSourceItems => _items;
}

/// <summary>
/// Utility that executes in parallel for each source and returns results grouped by source.
/// </summary>
public static class ParallelResults
{
    public static IEnumerable<SourceGroup<TSource, TItem>> GroupBy<TSource, TItem>(
        IEnumerable<TSource> sources,
        Func<TSource, IEnumerable<TItem>?> selector,
        int maxDegreeOfParallelism = 4,
        CancellationToken token = default)
    {
        var srcList = sources as IList<TSource> ?? sources.ToList();
        int count = srcList.Count;

        var slotArr = new List<WithSource<TSource, TItem>>[count];
        for (int i = 0; i < count; i++) slotArr[i] = [];

        using var sem = new SemaphoreSlim(maxDegreeOfParallelism);
        var tasks = new Task[count];

        for (int i = 0; i < count; i++)
        {
            int idx = i;
            tasks[i] = Task.Run(async () =>
            {
                await sem.WaitAsync(token).ConfigureAwait(false);
                try
                {
                    var src = srcList[idx];
                    var items = selector(src);
                    if (items != null)
                    {
                        var slot = slotArr[idx];
                        foreach (var it in items)
                            slot.Add(new WithSource<TSource, TItem>(src, it));
                    }
                }
                finally { sem.Release(); }
            }, token);
        }

        Task.WaitAll(tasks, token);

        for (int i = 0; i < count; i++)
        {
            yield return new SourceGroup<TSource, TItem>(srcList[i], slotArr[i]);
        }
    }

    public static List<WithSource<TSource, TItem>> ForEach<TSource, TItem>(
            IEnumerable<TSource> sources,
            Func<TSource, TItem?> selector,
            int maxDegreeOfParallelism = 4,
            CancellationToken token = default)
            where TItem : class
    {
        var srcList = sources as IList<TSource> ?? sources.ToList();
        int count = srcList.Count;

        var results = new List<WithSource<TSource, TItem>>[count];
        for (int i = 0; i < count; i++) results[i] = [];

        using var sem = new SemaphoreSlim(maxDegreeOfParallelism);
        var tasks = new Task[count];

        for (int i = 0; i < count; i++)
        {
            int idx = i;
            tasks[i] = Task.Run(async () =>
            {
                await sem.WaitAsync(token).ConfigureAwait(false);
                try
                {
                    var src = srcList[idx];
                    var item = selector(src);
                    if (item != null)
                    {
                        results[idx].Add(new WithSource<TSource, TItem>(src, item));
                    }
                }
                finally { sem.Release(); }
            }, token);
        }

        Task.WaitAll(tasks, token);

        return results.SelectMany(x => x).ToList();
    }
}

// When the variable holding an instance of this class goes out of scope, the progress bar is automatically disposed.
// This variable must be declared with a using statement.
//
// WriteProgress's first argument is simply the numerator the bar shows. Both readings are in
// use in this module and both read correctly beside a status that names the item:
// OrchThreadPoolImpl.GetResultWithProgress passes the number FINISHED, while the Copy* cmdlets
// pass a 1-based "now on this one". Pick one per cmdlet and stay with it.
//
// Call it from the pipeline thread only -- it ends in Cmdlet.WriteProgress.
public class ProgressReporter : IDisposable
{
    // Activity ids only have to be distinct among the bars that are alive at the same time, so
    // new code lets them be allocated and uses the constructor without one. Choosing them by
    // hand produced a registry of magic numbers -- 200, 500, 600, 900, 1000, 1300, one per
    // Copy* cmdlet -- that existed only in the callers' heads and was maintained so that
    // nested operations would not collide. Starts above that range so an allocated id can
    // never land on one still written out by hand.
    private static int nextId = 10000;

    private IWritableHost? provider;
    private readonly ProgressRecord progressRecord;
    private int? totalNum;

    /// <param name="totalNum">Units of work, or null when the total is not knowable. The bar
    /// then shows the count on its own and no percentage, rather than making the caller invent
    /// a denominator -- Int32.MaxValue was the one in use, which pins the bar at 0% forever.</param>
    public ProgressReporter(IWritableHost provider, int? totalNum, string activity)
        : this(provider, Interlocked.Increment(ref nextId), totalNum, activity)
    {
    }

    /// <param name="parentId">The activity id of the bar this one sits under, or -1 for a bar
    /// that stands on its own. A bar whose work is a step of another bar's work says so: the
    /// host then draws one tree, indenting this bar under its parent, instead of two unrelated
    /// activities that happen to be on screen together. Copy-Item's per-entity bars are the
    /// case -- they are stages of the folder the parent bar is counting.</param>
    public ProgressReporter(IWritableHost provider, int id, int? totalNum, string activity, int parentId = -1)
    {
        this.provider = provider;
        this.totalNum = totalNum;
        // Sanitized here too, not only in the Activity setter: an activity passed to the
        // constructor used to reach the host raw, and a wide character in it breaks the bar
        // exactly as one in the status does.
        progressRecord = new ProgressRecord(id, SafeText(activity)!, SafeText(activity)!)
        {
            ParentActivityId = parentId,
        };
    }

    public int? TotalNum
    {
        get { return totalNum; }
        set { totalNum = value; }
    }

    // The activity line is a LABEL: short, and padded by the caller to one width across every
    // bar it shares the screen with, so two bars start their "[" in the same column. See the
    // stage table in CopyItem.Recurse for a set of them. Anything that varies -- a destination
    // folder, a drive -- goes in Context instead, inside the bar. Width-sanitized like the
    // status (see SafeText), since a label can still carry a Japanese word.
    public string Activity
    {
        set { progressRecord.Activity = SafeText(value)!; }
    }

    // Set once per batch: the part of the status that does not change item by item, such as
    // the folder a copy is writing into. It sits between the count and the item name, so the
    // line reads "3/10 ASTest:\Shared queue-emails". This used to live in the activity, which
    // made every bar a different width and pushed the bars out of alignment.
    //
    // Set by whoever OWNS the bar, never by a shared helper that was handed one. The Copy*
    // methods in CopyItem.Entities used to set it themselves, which was right for the
    // standalone Copy-Orch* cmdlets -- one bar, so it has to carry the destination -- and
    // wrong under Copy-Item, where the folder bar above already names the folder and every
    // child bar repeated the same long path. Only the owner knows what else is on screen.
    private string? context;
    public string? Context
    {
        set { context = SafeText(value); }
    }

    // PowerShell #21293: the console host miscounts East Asian Wide characters when sizing the
    // progress bar, so any wide text (in the status OR the activity line) pushes the bar's
    // closing ']' onto the next line. Unless the host is known-fixed (PR #26185), collapse each
    // run of wide characters to "[N]" (N = hidden wide-char count) -- leaving the narrow
    // segments intact and the whole string one-cell-per-char. "請求書" -> "[3]",
    // "Invoice請求Folder" -> "Invoice[2]Folder". A null/empty stays as-is (no name -> no marker).
    private string? SafeText(string? text)
        => provider?.RendersWideProgress == true ? text : EastAsianWidth.CollapseWide(text);

    private void WriteProgress()
    {
        provider?.WriteProgress(progressRecord);
    }

    public void WriteProgress(int index, string? statusDescription = null, string? activity = null)
    {
        // -1 is PowerShell's "no percentage": an unknown total gets a bar without one, instead
        // of a fake 0%. Widened and clamped for the known case -- PercentComplete throws
        // outside 0..100, so a caller whose collection outgrew the total it declared would
        // otherwise take the cmdlet down over a progress bar.
        progressRecord.PercentComplete = totalNum is int total && total > 0
            ? (int)Math.Clamp(index * 100L / total, 0L, 100L)
            : -1;

        if (!string.IsNullOrEmpty(activity))
        {
            progressRecord.Activity = SafeText(activity)!;
        }

        string line = totalNum is int n ? $"{index:D}/{n}" : $"{index:D}";
        if (!string.IsNullOrEmpty(context)) line += " " + context;

        string? status = SafeText(statusDescription);
        if (!string.IsNullOrEmpty(status)) line += " " + status;

        progressRecord.StatusDescription = line;

        reported = true;
        WriteProgress();
    }

    // A bar that never reported anything is never taken down either. A reporter is routinely
    // created for work that turns out to be empty -- a Copy-Item stage for a folder that has
    // no queues, say -- and disposing it used to send a Completed record for an activity the
    // host had never been shown: twelve of a folder's thirteen stage bars were completed
    // without a single Processing record, their status still the activity name they were
    // constructed with.
    private bool reported;

    private void CompleteProgress()
    {
        if (!reported) return;

        progressRecord.RecordType = ProgressRecordType.Completed;
        WriteProgress();
    }

    public void Dispose()
    {
        CompleteProgress();
        GC.SuppressFinalize(this); // Suppress the finalizer after Dispose has been called
    }
}
