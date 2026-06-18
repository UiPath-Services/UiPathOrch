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

    // Use this when calling WriteItemObject() in the provider's GetChildItems.
    // Characters like [ and ] seem to be handled automatically. * and ? are presumably not expected in file names.
    public static string EscapePSText2(string? input)
    {
        //            return "'" + WildcardPattern.Escape(input) + "'";
        if (string.IsNullOrEmpty(input))
        {
            return "";
        }

        string ret = input
            .Replace("*", "`*")
            .Replace("?", "`?");
        //.TrimStart('\\');

        //if (input != ret || input.Contains(' ')) //★★★
        //    ret = '\'' + ret + '\'';

        return ret;
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
    private readonly CancellationTokenSource _cts;
    private readonly ConsoleCancelEventHandler _handler;
    private bool _cancelKeyPressed;

    // Property indicating whether the cancel key was pressed
    public bool CancelKeyPressed => _cancelKeyPressed;

    // Property exposing the CancellationTokenSource
    public CancellationToken Token => _cts.Token;

    // Default constructor
    public ConsoleCancelHandler()
    {
        _cts = new CancellationTokenSource();
        _handler = CreateHandler(null);
        Console.CancelKeyPress += _handler;
    }

    // Constructor that accepts a delegate for cancellation handling
    public ConsoleCancelHandler(Action onCancel)
    {
        _cts = new CancellationTokenSource();
        _handler = CreateHandler(onCancel);
        Console.CancelKeyPress += _handler;
    }

    // Common initialization logic
    private ConsoleCancelEventHandler CreateHandler(Action? onCancel)
    {
        return (sender, args) =>
        {
            _cancelKeyPressed = true;
            try
            {
                onCancel?.Invoke();
            }
            finally
            {
                _cts.Cancel(); // Ensure the token is cancelled
                args.Cancel = true; // Prevent the process from terminating
            }
        };
    }

    // Dispose pattern implementation
    public void Dispose()
    {
        Console.CancelKeyPress -= _handler;
        _cts?.Dispose();
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

public class OrchTask<TSource, TResult> : IDisposable
{
    public ManualResetEventSlim CompletedEvent { get; }
    private TSource? _source;
    public TSource Source => _source ?? throw new InvalidOperationException("Call GetResult() before accessing Source.");
    public string? Path { get; private set; }
    public object? Target { get; private set; }
    public TResult? Result { get; private set; }
    public Exception? Exception { get; private set; }

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

    private readonly SemaphoreSlim _semaphore;
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentBag<Task> _backgroundTasks = [];

    // Token that cancels when the pool is disposed. Used by RunForEach's
    // internal Task.Run lambdas so they bail out promptly on Ctrl+C / consumer
    // abandonment. Internal: external code constructs pools via the
    // OrchThreadPool static class only.
    internal CancellationToken Token => _cts.Token;

    // Track a background Task so Dispose can wait for it before tearing down
    // the semaphore (otherwise tasks blocked on WaitAsync would race with
    // SemaphoreSlim.Dispose and throw ObjectDisposedException). Internal for
    // the same reason as Token.
    internal void TrackTask(Task task) => _backgroundTasks.Add(task);

    private OrchThreadPoolImpl(OrchTask<TSource, TResult>[] threads, SemaphoreSlim semaphore)
    {
        _threads = threads;
        _semaphore = semaphore;
    }

    // The semaphore caps in-flight API calls and keeps thread-pool threads
    // available; without it, all sources would race to start at once and
    // could starve the thread pool while the consumer's GetResult is blocked
    // on CompletedEvent.Wait.
    //
    // SetResult is called directly on the background thread (no
    // SynchronizationContext.Post marshaling). Each task writes to its own
    // pre-allocated slot in `threads`, so there is no write race; calling
    // Post would only enqueue work to a SyncContext that the consumer thread
    // isn't pumping (it's blocked in CompletedEvent.Wait), risking a
    // message-pump deadlock in WPF-style hosts. PowerShell pipeline threads
    // have no SyncContext, so the marshaling was a no-op anyway.
    internal static OrchThreadPoolImpl<TSource, TResult> RunForEach(IEnumerable<TSource> sources,
        Func<TSource, string> getPathFunc,
        Func<TSource, object> getTargetFunc,
        Func<TSource, TResult> getResultFunc, int maxDegreeOfParallelism = 4)
    {
        var srcList = sources as IList<TSource> ?? sources.ToList();
        var threads = new OrchTask<TSource, TResult>[srcList.Count];
        var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);

        for (int i = 0; i < threads.Length; i++)
        {
            threads[i] = new OrchTask<TSource, TResult>();
        }

        var pool = new OrchThreadPoolImpl<TSource, TResult>(threads, semaphore);
        var token = pool.Token;

        foreach (var (source, index) in srcList.Select((source, index) => (source, index)))
        {
            var bgTask = Task.Run(async () =>
            {
                // Pre-compute path/target so the SetException calls below
                // can't themselves throw. If the getter funcs fault (e.g.
                // GetPSPath on a stale entity) we still need to signal the
                // task — otherwise the consumer's CompletedEvent.Wait would
                // block forever.
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
                    return;
                }

                try
                {
                    await semaphore.WaitAsync(token);
                }
                catch (OperationCanceledException ex)
                {
                    // Mark the task cancelled so the consumer's GetResult
                    // observes the cancel instead of blocking forever.
                    threads[index].SetException(source, pathStr, targetObj, ex);
                    return;
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
                finally
                {
                    semaphore.Release();
                }
            });
            pool.TrackTask(bgTask);
        }

        return pool;
    }

    public void Dispose()
    {
        // Cancel so background tasks waiting on the semaphore bail immediately.
        try { _cts.Cancel(); } catch { }

        // Brief wait so tasks that bail via OCE on WaitAsync (microseconds)
        // can release their slot cleanly. Don't wait for in-flight synchronous
        // API calls — they can't be cancelled mid-flight (no token plumbed
        // through OrchAPISession), so waiting only delays Ctrl+C response
        // without changing what the user observes. Stragglers calling
        // semaphore.Release / task.SetResult after dispose throw
        // ObjectDisposedException which is silent under .NET 5+
        // UnobservedTaskException default.
        try { Task.WaitAll([.. _backgroundTasks], TimeSpan.FromMilliseconds(100)); } catch { }

        foreach (var thread in _threads)
        {
            thread.Dispose(); // Release resources
        }
        _semaphore.Dispose();
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

    /// <summary>
    /// Two-phase variant of <see cref="RunForEach"/>. Phase 1 runs the
    /// fanout function per source (typically a list-fetch + projection to
    /// per-item Phase 2 sources). Phase 2 runs the per-item fetcher. Both
    /// phases share a single <see cref="SemaphoreSlim"/> capped at 4 so the
    /// total in-flight API call budget stays at 4 across the entire chain
    /// (avoiding the cap-multiplication trap of nesting two
    /// <see cref="RunForEach"/> calls).
    ///
    /// Streaming: Phase 2 tasks are enqueued as Phase 1 results arrive; the
    /// consumer foreach starts emitting in flat order (Phase 1 source order
    /// × per-source enumeration order) without waiting for all Phase 1 to
    /// finish.
    ///
    /// Phase 1 errors don't appear in the foreach stream — they're collected
    /// in <see cref="ChainedThreadPool{TSource, TFlat, TResult}.Phase1Errors"/>
    /// for the caller to drain after the main loop. This lets the caller
    /// emit a different ErrorRecord ErrorId for Phase 1 vs Phase 2 errors.
    /// </summary>
    public static ChainedThreadPool<TSource, TFlat, TResult> RunForEachChained<TSource, TFlat, TResult>(
        IEnumerable<TSource> sources,
        Func<TSource, string> phase1PathFunc,
        Func<TSource, object> phase1TargetFunc,
        Func<TSource, IEnumerable<TFlat>> phase1Fanout,
        Func<TFlat, string> phase2PathFunc,
        Func<TFlat, object> phase2TargetFunc,
        Func<TFlat, TResult> phase2,
        CancellationToken cancellationToken = default)
    {
        return new ChainedThreadPool<TSource, TFlat, TResult>(
            sources, phase1PathFunc, phase1TargetFunc, phase1Fanout,
            phase2PathFunc, phase2TargetFunc, phase2, cancellationToken);
    }
}

public sealed class ChainedThreadPool<TSource, TFlat, TResult> : IDisposable, IEnumerable<OrchTask<TFlat, TResult>>
{
    // Cap=4 hardcoded by design: matches the previous bespoke link-cmdlet
    // algorithm and the documented Orchestrator-friendly parallelism budget.
    private const int CapacityHardcoded = 4;

    private readonly BlockingCollection<OrchTask<TFlat, TResult>> _queue = [];
    private readonly SemaphoreSlim _semaphore = new(CapacityHardcoded);
    private readonly List<(TSource source, OrchException exception)> _phase1Errors = [];
    private readonly ConcurrentBag<Task> _phase2Tasks = [];
    // Track every OrchTask the producer creates so Dispose can release the
    // ManualResetEventSlim each one owns. BlockingCollection.Dispose does
    // not dispose its items, and the queue may still contain undrained
    // tasks if the consumer abandons the loop early.
    private readonly ConcurrentBag<OrchTask<TFlat, TResult>> _allTasks = [];
    private readonly CancellationTokenSource _cts;
    private readonly Task _producer;

    /// <summary>
    /// Phase 1 errors that occurred during fanout. Safe to enumerate only
    /// after the main consumer loop drains all Phase 2 tasks (the getter
    /// blocks briefly for the producer to finalize). Bounded by a 5 s
    /// timeout so a hung producer can't permanently stall the cmdlet —
    /// any Phase 1 errors recorded before the timeout are still returned.
    /// </summary>
    public IEnumerable<(TSource source, OrchException exception)> Phase1Errors
    {
        get
        {
            try { _producer.Wait(TimeSpan.FromSeconds(5)); }
            catch { /* producer-internal failures already recorded in _phase1Errors */ }
            return _phase1Errors;
        }
    }

    internal ChainedThreadPool(
        IEnumerable<TSource> sources,
        Func<TSource, string> phase1PathFunc,
        Func<TSource, object> phase1TargetFunc,
        Func<TSource, IEnumerable<TFlat>> phase1Fanout,
        Func<TFlat, string> phase2PathFunc,
        Func<TFlat, object> phase2TargetFunc,
        Func<TFlat, TResult> phase2,
        CancellationToken externalToken = default)
    {
        // Link the internal CTS to the caller-supplied token so consumer
        // Ctrl+C (signaled into externalToken) propagates instantly into the
        // pool's internals without waiting for Dispose. Without this link
        // GetConsumingEnumerable would block on an empty queue while the
        // producer is mid-Phase 1 (synchronous API calls aren't cancellable),
        // yielding a multi-second cancel delay.
        _cts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);

        var token = _cts.Token;

        _producer = Task.Run(async () =>
        {
            try
            {
                foreach (var source in sources)
                {
                    if (token.IsCancellationRequested) break;

                    List<TFlat>? items = null;
                    try
                    {
                        await _semaphore.WaitAsync(token);
                    }
                    catch (OperationCanceledException) { break; }

                    try
                    {
                        // Materialize while the slot is held so the API call
                        // (and any deferred LINQ wrapping it) is counted
                        // against the cap, not deferred to consumer time.
                        items = phase1Fanout(source).ToList();
                    }
                    catch (Exception ex)
                    {
                        var oex = new OrchException(phase1PathFunc(source), ex)
                        {
                            Target = phase1TargetFunc(source)
                        };
                        lock (_phase1Errors) { _phase1Errors.Add((source, oex)); }
                    }
                    finally { _semaphore.Release(); }

                    if (items is null) continue;

                    foreach (var flat in items)
                    {
                        if (token.IsCancellationRequested) break;

                        var task = new OrchTask<TFlat, TResult>();
                        _allTasks.Add(task);
                        _queue.Add(task);

                        // Fire-and-forget Phase 2 task; tracked in
                        // _phase2Tasks so Dispose can wait on stragglers.
                        var phase2Task = Task.Run(async () =>
                        {
                            // Pre-compute path/target so the SetException
                            // calls below can't themselves throw and leave
                            // the consumer's CompletedEvent.Wait blocking
                            // forever. See OrchThreadPoolImpl.RunForEach.
                            string pathStr;
                            object targetObj;
                            try
                            {
                                pathStr = phase2PathFunc(flat);
                                targetObj = phase2TargetFunc(flat);
                            }
                            catch (Exception funcEx)
                            {
                                task.SetException(flat, "<phase2PathFunc/phase2TargetFunc threw>", flat!, funcEx);
                                return;
                            }

                            try
                            {
                                await _semaphore.WaitAsync(token);
                            }
                            catch (OperationCanceledException ex)
                            {
                                // Mark the task cancelled so the consumer's
                                // GetResult observes it (and the task can be
                                // disposed cleanly).
                                task.SetException(flat, pathStr, targetObj, ex);
                                return;
                            }

                            try
                            {
                                var result = phase2(flat);
                                // Direct SetResult on the background thread.
                                // See OrchThreadPoolImpl.RunForEach for why
                                // SyncContext.Post is not used.
                                task.SetResult(flat, result);
                            }
                            catch (Exception ex)
                            {
                                task.SetException(flat, pathStr, targetObj, ex);
                            }
                            finally { _semaphore.Release(); }
                        });
                        _phase2Tasks.Add(phase2Task);
                    }
                }
            }
            finally
            {
                _queue.CompleteAdding();
            }
        });
    }

    public IEnumerator<OrchTask<TFlat, TResult>> GetEnumerator()
    {
        // Pass _cts.Token (linked to the external cancellation token in the
        // ctor) so MoveNext bails immediately on Ctrl+C even when the queue
        // is empty (producer is mid-Phase 1 synchronous API call).
        foreach (var task in _queue.GetConsumingEnumerable(_cts.Token))
            yield return task;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Dispose()
    {
        // Cancel so producer + Phase 2 tasks waiting on the semaphore bail
        // immediately. See OrchThreadPoolImpl.Dispose for the rationale on
        // not waiting for in-flight synchronous API calls.
        try { _cts.Cancel(); } catch { }

        // Brief wait so cancellation-bail tasks (microseconds) release
        // cleanly; in-flight API calls are abandoned to UnobservedTaskException.
        try { _producer.Wait(TimeSpan.FromMilliseconds(100)); } catch { }
        try { Task.WaitAll([.. _phase2Tasks], TimeSpan.FromMilliseconds(100)); } catch { }

        // Dispose every OrchTask the producer created. Tasks the producer
        // adds after this snapshot (still racing past the cancel) leak their
        // CompletedEvent — acceptable since MRES uses a kernel handle only
        // on contended Wait, and the GC will reclaim them eventually.
        foreach (var task in _allTasks)
        {
            try { task.Dispose(); } catch { }
        }

        _queue.Dispose();
        _semaphore.Dispose();
        _cts.Dispose();
        GC.SuppressFinalize(this);
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
public class ProgressReporter(IWritableHost provider, int id, int totalNum, string activity) : IDisposable
{
    private IWritableHost? provider = provider;
    //private Cmdlet? cmdlet;
    private readonly ProgressRecord progressRecord = new(id, activity, activity);
    private int totalNum = totalNum;

    public int TotalNum
    {
        get { return totalNum; }
        set { totalNum = value; }
    }

    private void WriteProgress()
    {
        provider?.WriteProgress(progressRecord);
    }

    public void WriteProgress(int index, string? statusDescription = null, string? activity = null)
    {
        progressRecord.PercentComplete = totalNum > 0 ? (index * 100) / totalNum : 0;
        if (!string.IsNullOrEmpty(activity))
        {
            progressRecord.Activity = activity;
        }
        progressRecord.StatusDescription = $"{index:D}/{totalNum} {statusDescription}".TrimEnd();
        WriteProgress();
    }

    private void CompleteProgress()
    {
        progressRecord.RecordType = ProgressRecordType.Completed;
        WriteProgress();
    }

    public void Dispose()
    {
        CompleteProgress();
        GC.SuppressFinalize(this); // Suppress the finalizer after Dispose has been called
    }
}
