using System.Text;
using System.Diagnostics;

namespace UiPath.OrchAPI;

/// <summary>
/// Writes the drive's HTTP diagnostic log.
///
/// Holds the log file OPEN for the writer's lifetime and appends under a lock. That choice is
/// the whole design, and it is worth recording why, because this class used to be a
/// Channel + background processor + batching + shutdown-budget pipeline instead:
///
/// Measured on the maintainer's machine, 2 KB blocks, 1000 iterations:
///   open + append + close (what File.AppendAllText does) .... 3.3 ms  (13.7 ms under %TEMP%)
///   write + flush through a handle already open ............. 0.30 ms
///
/// The cost is the OPEN, not the write -- an on-access scanner charges per file open, which is
/// also why %TEMP% is several times worse than a developer folder. Holding the handle removes
/// 90-98% of the cost outright. At this module's own cap of 15 requests/second per drive that
/// is ~4.5 ms per second of logging work, i.e. under 1% of a single HTTP round trip, so there
/// is nothing left for a background pipeline to save: the batching it existed to provide was
/// amortising an open/close that no longer happens.
///
/// What the pipeline cost instead: a silent-data-loss bug (a failed flush cleared the buffer
/// and reported DroppedEntries = 0), ~430 lines of concurrency code, and load-sensitive tests.
/// It was not even buying non-blocking writes: every caller wrapped its write in
/// `_ = Task.Run(...)` as well, so the benefit that normally justifies an async logger was
/// being provided twice -- and the outer copy cost the log its order. Both are gone; callers
/// now write inline through OrchAPISession.WriteLogBlock.
///
/// Durability is also strictly better: an entry is on its way to the OS before WriteAsync
/// returns, instead of sitting in a channel and an in-memory buffer for up to a flush interval.
///
/// The file is opened with FileShare.ReadWrite so `Get-Content -Wait` can still tail it and a
/// second PowerShell session using the same drive name can still append.
///
/// It is also CLOSED once writing goes idle, because holding a write handle is not free to the
/// rest of the system even though it is free to us: Windows refuses any open that declares
/// FileShare.Read (which denies our write access), and that is the default for File.ReadAllText,
/// StreamReader(path), Get-FileHash, Remove-Item -- and Compress-Archive, i.e. the "zip your logs
/// and send them" step of a support request. PowerShell's own provider shares ReadWrite, so
/// Get-Content / Show-TextFiles / Select-String / Copy-Item are unaffected either way.
/// Closing on idle keeps the batching benefit exactly where it matters (a burst writes through
/// one open) while leaving the file free whenever the user is actually in a position to grab it.
/// Measured cost of a reopen in the default log directory: ~11.7 ms, once per idle period.
/// </summary>
public sealed class AsyncLogWriter : IDisposable, IAsyncDisposable
{
    private readonly string _logFilePath;
    private readonly object _lock = new();
    private readonly LogMetrics _metrics = new();

    // Opened on first write and kept open. Null when not yet opened, or dropped after a failure
    // so the next write reopens.
    private StreamWriter? _writer;
    private bool _disposed;

    // Whether the one-time "did this writer create the file?" question has been answered; the
    // answer drives the owner-only permission tightening on Unix. Guarded by _lock.
    private bool _permissionsSettled;

    // Entries a failed write could not persist. Retried on the next write and once more on
    // dispose -- without this a lone entry written during a transient failure would be lost
    // exactly the way the old pipeline lost it, and closing the handle on idle would not be safe
    // to do at all (something else can take the file exclusively while we are closed). A plain
    // List under the same lock; no channel.
    private readonly record struct PendingEntry(string Content, int Bytes);
    private readonly List<PendingEntry> _pending = [];
    private long _pendingBytes;

    // Two independent bounds, because a log entry is a whole HTTP block: at Verbose the response
    // body goes in uncapped, so one entry can be megabytes and a count-only limit would not
    // actually bound anything. Past either cap the OLDEST go first -- the surviving tail is the
    // most recent context, which is what a reader of a truncated diagnostic log needs -- and the
    // newest entry is always retained regardless of size.
    private readonly int _maxPending;
    private readonly long _maxPendingBytes;

    /// <param name="maxRetainedEntries">
    /// Upper bound on entries held back after a failed write. The previous <c>batchSize</c> and
    /// <c>flushIntervalMs</c> parameters are gone: entries are written and flushed as they
    /// arrive, so there is no batch to size and no deferred flush to schedule.
    /// </param>
    /// <param name="idleCloseMs">
    /// Release the file handle after this long without a write, so the log stops blocking
    /// FileShare.Read openers (Compress-Archive, Get-FileHash, Remove-Item). The next write
    /// reopens it. Lower values free the file sooner at the cost of more reopens.
    /// </param>
    /// <param name="maxRetainedBytes">
    /// Byte-size counterpart to <paramref name="maxRetainedEntries"/>. The binding limit in
    /// practice: a single Verbose entry carries a whole response body.
    /// </param>
    public AsyncLogWriter(
        string logFilePath,
        int maxRetainedEntries = DefaultMaxRetainedEntries,
        int idleCloseMs = DefaultIdleCloseMs,
        long maxRetainedBytes = DefaultMaxRetainedBytes)
    {
        _logFilePath = logFilePath ?? throw new ArgumentNullException(nameof(logFilePath));
        _maxPending = maxRetainedEntries > 0 ? maxRetainedEntries : DefaultMaxRetainedEntries;
        _maxPendingBytes = maxRetainedBytes > 0 ? maxRetainedBytes : DefaultMaxRetainedBytes;
        _idleCloseMs = idleCloseMs > 0 ? idleCloseMs : DefaultIdleCloseMs;
        // Poll often enough that the file is released promptly after the idle window, but never
        // more than once a second. Created disarmed: a drive that never logs pays nothing.
        _idlePollMs = Math.Max(1, Math.Min(1000, _idleCloseMs / 2));
        _idleTimer = new Timer(OnIdleTick, null, Timeout.Infinite, Timeout.Infinite);
    }

    private const int DefaultMaxRetainedEntries = 10000;

    // 8 MB of retained log text. Generous for any realistic outage, small enough that a drive
    // stuck against an unwritable path cannot meaningfully grow the host process.
    private const long DefaultMaxRetainedBytes = 8L * 1024 * 1024;

    // Five seconds: long enough that an interactive sequence of cmdlets keeps writing through one
    // open, short enough that a user who stops to collect the logs is not left waiting.
    private const int DefaultIdleCloseMs = 5000;

    private readonly int _idleCloseMs;
    private readonly int _idlePollMs;
    private readonly Timer _idleTimer;
    private long _lastWriteTick;   // Environment.TickCount64 of the last successful write

    /// <summary>
    /// Close the handle once writing has gone quiet. Runs on a timer thread, so it takes the same
    /// lock every write takes -- a close can never land between a write's open and its flush.
    /// </summary>
    private void OnIdleTick(object? state)
    {
        lock (_lock)
        {
            if (_disposed || _writer is null) return;

            if (Environment.TickCount64 - _lastWriteTick < _idleCloseMs) return;

            // Anything a previous write could not persist gets one more attempt before the handle
            // goes away; whatever remains stays queued for the next write, which reopens.
            DrainPendingLocked();
            if (_writer is not null) CloseWriterLocked();
            StopIdlePollingLocked();
        }
    }

    private void StartIdlePollingLocked() => _idleTimer.Change(_idlePollMs, _idlePollMs);

    private void StopIdlePollingLocked() => _idleTimer.Change(Timeout.Infinite, Timeout.Infinite);

    /// <summary>
    /// Appends a log entry. Completes synchronously; the ValueTask return is vestigial and kept
    /// only so the existing tests and any external caller compile unchanged.
    /// </summary>
    public ValueTask WriteAsync(string logContent, CancellationToken cancellationToken = default)
    {
        if (_disposed || string.IsNullOrEmpty(logContent))
            return ValueTask.CompletedTask;

        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            // Re-check inside the lock: Dispose may have run while we waited, and writing through
            // a StreamWriter it already disposed would throw.
            if (_disposed) return ValueTask.CompletedTask;

            _pending.Add(new PendingEntry(logContent, Encoding.UTF8.GetByteCount(logContent)));
            _pendingBytes += _pending[^1].Bytes;
            DrainPendingLocked();
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Attempt to persist everything in <see cref="_pending"/>. Anything still unwritten stays
    /// queued for the next call. Must be called under <see cref="_lock"/>.
    /// </summary>
    private void DrainPendingLocked()
    {
        if (_pending.Count == 0) return;

        try
        {
            StreamWriter writer = _writer ??= OpenWriterLocked();

            long bytes = _pendingBytes;
            foreach (var entry in _pending)
            {
                writer.Write(entry.Content);
            }
            // Flush to the OS on every write: a diagnostic log is read after something went
            // wrong, so buffered-but-unwritten bytes are the one outcome that defeats it. This
            // is the 0.30 ms measured above -- the expensive part was the open, not this.
            writer.Flush();

            _metrics.RecordBatchWritten(_pending.Count, bytes);
            _pending.Clear();
            _pendingBytes = 0;

            // Arm the idle poll now that a handle is open and has just been used.
            _lastWriteTick = Environment.TickCount64;
            StartIdlePollingLocked();
        }
        catch (Exception ex)
        {
            // Drop the handle so the next attempt reopens -- the failure may be the handle
            // itself (a share that went away, a file replaced underneath us).
            CloseWriterLocked();

            Debug.WriteLine($"Log write failed ({ex.GetType().Name}): {ex.Message}");

            // Keep the entries for the next write. Anything evicted past a cap is COUNTED -- the
            // previous implementation discarded silently and still reported a clean run.
            TrimPendingLocked();
        }
    }

    /// <summary>
    /// Enforce both retention caps by dropping the oldest entries, counting whatever goes. The
    /// newest entry is always kept, even on its own over the byte cap: a single oversized block
    /// is still the most useful thing to have. Under <see cref="_lock"/>.
    /// </summary>
    private void TrimPendingLocked()
    {
        int drop = 0;
        long bytes = _pendingBytes;
        while (_pending.Count - drop > 1 &&
               (_pending.Count - drop > _maxPending || bytes > _maxPendingBytes))
        {
            bytes -= _pending[drop].Bytes;
            drop++;
        }

        if (drop == 0) return;

        // RemoveRange once rather than RemoveAt in a loop -- the latter is quadratic.
        _pending.RemoveRange(0, drop);
        _pendingBytes = bytes;
        _metrics.RecordDroppedEntries(drop);
    }

    /// <summary>Open the log file for append, creating the directory if needed. Under <see cref="_lock"/>.</summary>
    private StreamWriter OpenWriterLocked()
    {
        var directory = Path.GetDirectoryName(_logFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            OwnerOnlyPath.CreateRestrictedDirectory(directory);
        }

        // Settle the permission question before the open creates the file.
        bool creating = false;
        if (!_permissionsSettled)
        {
            creating = !File.Exists(_logFilePath);
            _permissionsSettled = true;
        }

        // FileShare.ReadWrite: `Get-Content -Wait` must still tail this, and a second session on
        // the same drive name must still be able to append.
        var stream = new FileStream(
            _logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite,
            bufferSize: 4096, useAsync: false);

        if (creating)
        {
            // HTTP bodies land here, including credentials a cmdlet submitted.
            OwnerOnlyPath.RestrictFile(_logFilePath);
        }

        // UTF-8 without a BOM, matching what File.AppendAllText produced before, so an existing
        // log keeps its encoding and mid-file BOMs never appear.
        return new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
        {
            AutoFlush = false,
        };
    }

    private void CloseWriterLocked()
    {
        try { _writer?.Dispose(); }
        catch (Exception ex) { Debug.WriteLine($"Log handle close failed: {ex.Message}"); }
        _writer = null;
    }

    /// <summary>
    /// Gets log statistics.
    /// </summary>
    public LogStatistics GetStatistics() => _metrics.GetStatistics();

    /// <summary>
    /// Flush anything still queued, then release the file handle. Idempotent.
    /// </summary>
    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed) return;

            // Last chance for entries a previous write could not persist.
            DrainPendingLocked();
            if (_pending.Count > 0)
            {
                // No further writes will come, so these are genuinely lost -- count them rather
                // than let the statistics claim a clean run.
                _metrics.RecordDroppedEntries(_pending.Count);
                _pending.Clear();
                _pendingBytes = 0;
            }

            CloseWriterLocked();
            StopIdlePollingLocked();
            _idleTimer.Dispose();
            _disposed = true;
        }
    }

    /// <summary>
    /// Asynchronous shutdown. Nothing is queued in the background any more, so this is just
    /// <see cref="Dispose"/>; the API is kept so call sites and tests need no change.
    /// </summary>
    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Class that manages log metrics.
/// </summary>
public sealed class LogMetrics
{
    private long _totalEntriesWritten;
    private long _totalBytesWritten;
    private long _droppedEntries;
    private long _batchesWritten;

    public void RecordBatchWritten(int entryCount, long byteCount)
    {
        Interlocked.Add(ref _totalEntriesWritten, entryCount);
        Interlocked.Add(ref _totalBytesWritten, byteCount);
        Interlocked.Increment(ref _batchesWritten);
    }

    public void RecordDroppedEntry() => RecordDroppedEntries(1);

    /// <summary>
    /// Record entries that were accepted but could not be persisted -- entries evicted past the
    /// retry cap, or still queued when the writer was disposed. Without this the statistics
    /// reported everything as written while entries were being discarded.
    /// </summary>
    public void RecordDroppedEntries(int count)
    {
        if (count <= 0) return;
        Interlocked.Add(ref _droppedEntries, count);
    }

    public LogStatistics GetStatistics()
    {
        return new LogStatistics(
            Interlocked.Read(ref _totalEntriesWritten),
            Interlocked.Read(ref _totalBytesWritten),
            Interlocked.Read(ref _droppedEntries),
            Interlocked.Read(ref _batchesWritten)
        );
    }
}

/// <summary>
/// Snapshot of a writer's counters. <c>BatchesWritten</c> now counts write operations rather
/// than deferred batches -- with a write per entry it tracks <c>TotalEntriesWritten</c> except
/// where a retained entry went out alongside a later one.
/// </summary>
public readonly record struct LogStatistics(
    long TotalEntriesWritten,
    long TotalBytesWritten,
    long DroppedEntries,
    long BatchesWritten
)
{
    public double AverageEntriesPerBatch => BatchesWritten > 0 ? (double)TotalEntriesWritten / BatchesWritten : 0;
    public double AverageBytesPerEntry => TotalEntriesWritten > 0 ? (double)TotalBytesWritten / TotalEntriesWritten : 0;
}
