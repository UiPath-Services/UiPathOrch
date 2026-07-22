using System.Threading.Channels;
using System.Text;
using System.Diagnostics;

namespace UiPath.OrchAPI;

/// <summary>
/// Provides asynchronous log writing.
/// Uses a Channel-based queuing system to eliminate blocking on log writes.
/// </summary>
public sealed class AsyncLogWriter : IDisposable, IAsyncDisposable
{
    private readonly Channel<LogEntry> _logQueue;
    private readonly ChannelWriter<LogEntry> _writer;
    private readonly Task _processingTask;
    private readonly string _logFilePath;
    private readonly SemaphoreSlim _fileSemaphore;
    private readonly CancellationTokenSource _shutdownCts;
    private readonly LogMetrics _metrics;
    private volatile bool _disposed;

    // Whether the one-time "did this writer create the log file?" question has been answered.
    // Only ever read/written inside FlushBufferAsync's _fileSemaphore, so no interlocking.
    private bool _permissionsSettled;

    // Configurable parameters
    private readonly int _batchSize;
    private readonly int _flushIntervalMs;

    // Upper bound on entries held back for retry after a failed flush. Tied to maxQueueSize --
    // the module's existing declaration of how many log entries may sit in memory -- so the two
    // buffers shrink together when a caller tunes it down. A path that stays unwritable
    // therefore holds at most ~2x maxQueueSize entries (channel + retry buffer) before the
    // oldest start being dropped and counted.
    private readonly int _maxRetainedEntries;

    // What a flush attempt achieved. Retryable vs Permanent decides whether the buffer is worth
    // holding on to: a locked or momentarily unavailable file recovers on the next interval,
    // whereas a denied or malformed path never will and retrying it forever would spin.
    private enum FlushOutcome
    {
        Persisted,
        Retryable,
        Permanent,
    }

    public AsyncLogWriter(string logFilePath, int maxQueueSize = 10000, int batchSize = 100, int flushIntervalMs = 1000)
    {
        _logFilePath = logFilePath ?? throw new ArgumentNullException(nameof(logFilePath));
        _batchSize = batchSize;
        _flushIntervalMs = flushIntervalMs;
        _maxRetainedEntries = maxQueueSize;
        _fileSemaphore = new SemaphoreSlim(1, 1);
        _shutdownCts = new CancellationTokenSource();
        _metrics = new LogMetrics();

        // Channel configuration
        var options = new BoundedChannelOptions(maxQueueSize)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        };

        _logQueue = Channel.CreateBounded<LogEntry>(options);
        _writer = _logQueue.Writer;

        // Start the background processing task
        _processingTask = ProcessLogEntriesAsync(_shutdownCts.Token);
    }

    /// <summary>
    /// Asynchronously enqueues a log entry.
    /// </summary>
    public async ValueTask WriteAsync(string logContent, CancellationToken cancellationToken = default)
    {
        if (_disposed || string.IsNullOrEmpty(logContent))
            return;

        var entry = new LogEntry(DateTime.UtcNow, logContent);

        try
        {
            await _writer.WriteAsync(entry, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            // Channel has been closed
            _metrics.RecordDroppedEntry();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Cancellation request is a normal case
        }
    }

    /// <summary>
    /// Synchronous version.
    /// Attempts to enqueue in a non-blocking manner.
    /// </summary>
    //public void Write(string logContent)
    //{
    //    if (_disposed || string.IsNullOrEmpty(logContent))
    //        return;

    //    var entry = new LogEntry(DateTime.UtcNow, logContent);

    //    if (!_writer.TryWrite(entry))
    //    {
    //        // Queue is full or has been closed
    //        _metrics.RecordDroppedEntry();
    //    }
    //}

    /// <summary>
    /// Gets log statistics.
    /// </summary>
    public LogStatistics GetStatistics() => _metrics.GetStatistics();

    /// <summary>
    /// Processes log entries in the background.
    /// </summary>
    private async Task ProcessLogEntriesAsync(CancellationToken cancellationToken)
    {
        var buffer = new List<LogEntry>(_batchSize);
        var lastFlushTime = DateTime.UtcNow;

        try
        {
            var reader = _logQueue.Reader;
            var completed = false;

            while (!completed)
            {
                if (buffer.Count == 0)
                {
                    // Nothing buffered: wait indefinitely for the next entry
                    // (no idle wake-ups when there is nothing to flush).
                    completed = !await reader.WaitToReadAsync(cancellationToken);
                }
                else
                {
                    // Entries are buffered: never wait past the flush deadline, so a
                    // time-based flush still fires when no further entries arrive.
                    // Previously the interval was only re-evaluated when the *next*
                    // entry was dequeued, so a lone buffered entry -- e.g. the pre-auth
                    // diagnostics block written right before a hanging PKCE listener --
                    // was never persisted (folder created, log file empty).
                    // Cap the wait at the time remaining until the next flush is due
                    // (not a full interval each time) so flush latency stays within
                    // _flushIntervalMs even under sustained sub-batch traffic.
                    var elapsedMs = (DateTime.UtcNow - lastFlushTime).TotalMilliseconds;
                    var remainingMs = (int)Math.Clamp(_flushIntervalMs - elapsedMs, 0d, (double)_flushIntervalMs);
                    using var waitCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    waitCts.CancelAfter(remainingMs);
                    try
                    {
                        completed = !await reader.WaitToReadAsync(waitCts.Token);
                    }
                    catch (OperationCanceledException) when (waitCts.IsCancellationRequested
                                                            && !cancellationToken.IsCancellationRequested)
                    {
                        // Flush deadline reached with no new entry -- fall through
                        // to the time-based flush below.
                    }
                }

                // Once a flush fails, stop attempting further ones until the next interval:
                // without this the batch flush below would re-attempt on EVERY subsequent
                // TryRead (the buffer stays at or above _batchSize when it is not cleared),
                // hammering a filesystem that just told us it was busy.
                bool flushBlocked = false;

                // Drain everything currently available without blocking.
                while (reader.TryRead(out var entry))
                {
                    buffer.Add(entry);
                    if (!flushBlocked && buffer.Count >= _batchSize)
                    {
                        flushBlocked = !ApplyFlushOutcome(
                            buffer, await FlushBufferAsync(buffer, cancellationToken));
                        lastFlushTime = DateTime.UtcNow;
                    }
                }

                // Time-based flush: persist whatever remains once the interval has
                // elapsed, even when no new entries are arriving. A retained buffer is
                // retried here on the next interval, which doubles as the retry backoff.
                if (!flushBlocked && buffer.Count > 0 &&
                    (DateTime.UtcNow - lastFlushTime).TotalMilliseconds >= _flushIntervalMs)
                {
                    ApplyFlushOutcome(buffer, await FlushBufferAsync(buffer, cancellationToken));
                    // Advance regardless of the outcome so a failing flush waits a full interval
                    // before the next attempt instead of retrying in a tight loop.
                    lastFlushTime = DateTime.UtcNow;
                }
            }

            // Process remaining entries during shutdown. This is the last attempt -- the loop has
            // exited, so anything not persisted here is genuinely lost and must be counted.
            if (buffer.Count > 0)
            {
                if (!ApplyFlushOutcome(buffer, await FlushBufferAsync(buffer, cancellationToken)))
                {
                    _metrics.RecordDroppedEntries(buffer.Count);
                    buffer.Clear();
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal shutdown. Whatever is still buffered missed the shutdown budget and will
            // never be written -- count it rather than letting the statistics claim success.
            _metrics.RecordDroppedEntries(buffer.Count);
            buffer.Clear();
        }
        catch (Exception ex)
        {
            // Record error logs through an alternative means
            Debug.WriteLine($"AsyncLogWriter processing failed: {ex}");
        }
    }

    /// <summary>
    /// Writes the buffer contents to a file and reports whether they were persisted.
    ///
    /// The outcome matters: this used to return void after swallowing every exception, and the
    /// callers cleared the buffer unconditionally -- so a single transient IOException (a virus
    /// scanner holding the new file open is enough) silently destroyed the entries, left the log
    /// file absent, and did not even count them as dropped. The caller now decides what to do
    /// from the outcome; NOTHING is discarded without being recorded.
    /// </summary>
    private async Task<FlushOutcome> FlushBufferAsync(List<LogEntry> buffer, CancellationToken cancellationToken)
    {
        if (buffer.Count == 0)
            return FlushOutcome.Persisted;

        await _fileSemaphore.WaitAsync(cancellationToken);
        try
        {
            // Create the directory if it does not exist
            var directory = Path.GetDirectoryName(_logFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                OwnerOnlyPath.CreateRestrictedDirectory(directory);
            }

            // StringBuilder for batch writing
            var stringBuilder = new StringBuilder();
            int totalBytes = 0;

            foreach (var entry in buffer)
            {
                stringBuilder.Append(entry.Content);
                totalBytes += Encoding.UTF8.GetByteCount(entry.Content);
            }

            // AppendAllTextAsync creates the file with umask-derived permissions, so tighten it to
            // owner-only right after — HTTP bodies (including credentials a cmdlet submitted) go
            // in here. Only when WE created it: an operator who widened an existing log
            // deliberately keeps their mode.
            //
            // The stat can only matter on the flush that creates the file, so it is settled once
            // and never repeated for the writer's lifetime rather than paid on every flush. Safe
            // to test-then-act: the whole block runs under _fileSemaphore and this writer owns the
            // path, so _permissionsSettled needs no interlocking.
            bool createdByThisFlush = false;
            if (!_permissionsSettled)
            {
                createdByThisFlush = !File.Exists(_logFilePath);
                _permissionsSettled = true;
            }

            await File.AppendAllTextAsync(_logFilePath, stringBuilder.ToString(), cancellationToken);

            if (createdByThisFlush)
            {
                OwnerOnlyPath.RestrictFile(_logFilePath);
            }

            // Update metrics
            _metrics.RecordBatchWritten(buffer.Count, totalBytes);
            return FlushOutcome.Persisted;
        }
        // Shutdown budget exhausted mid-write. Propagate so the processing loop's own handler
        // treats it as the normal shutdown it is; that handler accounts for what is left over.
        catch (OperationCanceledException)
        {
            throw;
        }
        // PathTooLong / DirectoryNotFound derive from IOException, so they must precede it.
        catch (PathTooLongException ex)
        {
            Debug.WriteLine($"Log path too long: {ex.Message}");
            return FlushOutcome.Permanent;
        }
        // Retryable: the directory is (re)created on every flush above, and a network path can
        // come back. The retention cap stops this from retrying without bound.
        catch (DirectoryNotFoundException ex)
        {
            Debug.WriteLine($"Log directory not found: {ex.Message}");
            return FlushOutcome.Retryable;
        }
        catch (UnauthorizedAccessException ex)
        {
            Debug.WriteLine($"Log file access denied: {ex.Message}");
            return FlushOutcome.Permanent;
        }
        catch (NotSupportedException ex)
        {
            Debug.WriteLine($"Log path is not supported: {ex.Message}");
            return FlushOutcome.Permanent;
        }
        // The transient case this whole outcome mechanism exists for: the file is locked by a
        // scanner or another handle, the volume hiccuped, the share dropped. Next interval.
        catch (IOException ex)
        {
            Debug.WriteLine($"Log file I/O error: {ex.Message}");
            return FlushOutcome.Retryable;
        }
        catch (Exception ex)
        {
            // Unknown failure: do not spin on it, but do not hide it either -- the caller counts
            // the discarded entries.
            Debug.WriteLine($"Unexpected log write error: {ex}");
            return FlushOutcome.Permanent;
        }
        finally
        {
            _fileSemaphore.Release();
        }
    }

    /// <summary>
    /// Apply a flush outcome to the buffer. Returns true when the entries were persisted.
    /// Every entry that leaves the buffer unwritten is counted in <see cref="LogMetrics"/>.
    /// </summary>
    private bool ApplyFlushOutcome(List<LogEntry> buffer, FlushOutcome outcome)
    {
        switch (outcome)
        {
            case FlushOutcome.Persisted:
                buffer.Clear();
                return true;

            case FlushOutcome.Permanent:
                // The path itself is unusable; retrying would spin forever. Account for the loss
                // rather than hiding it, which is exactly what the old code did.
                _metrics.RecordDroppedEntries(buffer.Count);
                buffer.Clear();
                return false;

            default: // Retryable -- keep the entries for the next interval.
                if (buffer.Count > _maxRetainedEntries)
                {
                    // A path that never recovers must not grow this list until the process dies.
                    // Evict the OLDEST so the surviving tail is the most recent context, which is
                    // what a reader of a truncated diagnostic log actually needs.
                    int excess = buffer.Count - _maxRetainedEntries;
                    buffer.RemoveRange(0, excess);
                    _metrics.RecordDroppedEntries(excess);
                }
                return false;
        }
    }

    // Shutdown budget. The background processor must finish flushing within this window;
    // the synchronous wait is given a small extra buffer so that DisposeAsync's finally
    // (which releases the semaphore and CTS) actually runs before Dispose() returns.
    private static readonly TimeSpan ShutdownProcessingTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan SyncDisposeWait = TimeSpan.FromSeconds(12);

    /// <summary>
    /// Asynchronous shutdown.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            // Stop accepting new entries
            _writer.Complete();

            // Wait for background processing to complete (with timeout)
            _shutdownCts.CancelAfter(ShutdownProcessingTimeout);
            await _processingTask;
        }
        catch (OperationCanceledException)
        {
            var stats = _metrics.GetStatistics();
            Debug.WriteLine($"AsyncLogWriter shutdown timeout - flushed {stats.TotalEntriesWritten} entries, dropped {stats.DroppedEntries}");
        }
        finally
        {
            _fileSemaphore?.Dispose();
            _shutdownCts?.Dispose();
        }
    }

    /// <summary>
    /// Synchronous Dispose (IDisposable interface).
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        try
        {
            var disposeTask = DisposeAsync();
            if (!disposeTask.IsCompleted)
            {
                // Wait long enough for DisposeAsync's CancelAfter window plus finally to run.
                disposeTask.AsTask().Wait(SyncDisposeWait);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"AsyncLogWriter dispose error: {ex}");
        }
    }
}

/// <summary>
/// Internal struct representing a log entry.
/// </summary>
internal readonly record struct LogEntry(DateTime Timestamp, string Content);

/// <summary>
/// Class that manages log metrics.
/// </summary>
public sealed class LogMetrics
{
    private long _totalEntriesWritten;
    private long _totalBytesWritten;
    private long _droppedEntries;
    private long _batchesWritten;

    public void RecordBatchWritten(int entryCount, int byteCount)
    {
        Interlocked.Add(ref _totalEntriesWritten, entryCount);
        Interlocked.Add(ref _totalBytesWritten, byteCount);
        Interlocked.Increment(ref _batchesWritten);
    }

    public void RecordDroppedEntry() => RecordDroppedEntries(1);

    /// <summary>
    /// Record entries that were accepted but could not be persisted -- a flush that failed
    /// permanently, retained entries evicted past the retry cap, or a buffer still unwritten
    /// when the shutdown budget ran out. Without this the statistics reported everything as
    /// written while entries were being discarded.
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
/// Struct representing log statistics.
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

