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

    // Configurable parameters
    private readonly int _batchSize;
    private readonly int _flushIntervalMs;

    public AsyncLogWriter(string logFilePath, int maxQueueSize = 10000, int batchSize = 100, int flushIntervalMs = 1000)
    {
        _logFilePath = logFilePath ?? throw new ArgumentNullException(nameof(logFilePath));
        _batchSize = batchSize;
        _flushIntervalMs = flushIntervalMs;
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
                    // Entries are buffered: never wait past the flush interval, so a
                    // time-based flush still fires when no further entries arrive.
                    // Previously the interval was only re-evaluated when the *next*
                    // entry was dequeued, so a lone buffered entry — e.g. the pre-auth
                    // diagnostics block written right before a hanging PKCE listener —
                    // was never persisted (folder created, log file empty).
                    using var waitCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    waitCts.CancelAfter(_flushIntervalMs);
                    try
                    {
                        completed = !await reader.WaitToReadAsync(waitCts.Token);
                    }
                    catch (OperationCanceledException) when (waitCts.IsCancellationRequested
                                                            && !cancellationToken.IsCancellationRequested)
                    {
                        // Flush interval elapsed with no new entry — fall through
                        // to the time-based flush below.
                    }
                }

                // Drain everything currently available without blocking.
                while (reader.TryRead(out var entry))
                {
                    buffer.Add(entry);
                    if (buffer.Count >= _batchSize)
                    {
                        await FlushBufferAsync(buffer, cancellationToken);
                        buffer.Clear();
                        lastFlushTime = DateTime.UtcNow;
                    }
                }

                // Time-based flush: persist whatever remains once the interval has
                // elapsed, even when no new entries are arriving.
                if (buffer.Count > 0 &&
                    (DateTime.UtcNow - lastFlushTime).TotalMilliseconds >= _flushIntervalMs)
                {
                    await FlushBufferAsync(buffer, cancellationToken);
                    buffer.Clear();
                    lastFlushTime = DateTime.UtcNow;
                }
            }

            // Process remaining entries during shutdown
            if (buffer.Count > 0)
            {
                await FlushBufferAsync(buffer, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            // Record error logs through an alternative means
            Debug.WriteLine($"AsyncLogWriter processing failed: {ex}");
        }
    }

    /// <summary>
    /// Writes the buffer contents to a file.
    /// </summary>
    private async Task FlushBufferAsync(List<LogEntry> buffer, CancellationToken cancellationToken)
    {
        if (buffer.Count == 0)
            return;

        await _fileSemaphore.WaitAsync(cancellationToken);
        try
        {
            // Create the directory if it does not exist
            var directory = Path.GetDirectoryName(_logFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // StringBuilder for batch writing
            var stringBuilder = new StringBuilder();
            int totalBytes = 0;

            foreach (var entry in buffer)
            {
                stringBuilder.Append(entry.Content);
                totalBytes += Encoding.UTF8.GetByteCount(entry.Content);
            }

            await File.AppendAllTextAsync(_logFilePath, stringBuilder.ToString(), cancellationToken);

            // Update metrics
            _metrics.RecordBatchWritten(buffer.Count, totalBytes);
        }
        catch (DirectoryNotFoundException ex)
        {
            Debug.WriteLine($"Log directory not found: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            Debug.WriteLine($"Log file access denied: {ex.Message}");
        }
        catch (IOException ex)
        {
            Debug.WriteLine($"Log file I/O error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Unexpected log write error: {ex}");
        }
        finally
        {
            _fileSemaphore.Release();
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

    public void RecordDroppedEntry()
    {
        Interlocked.Increment(ref _droppedEntries);
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

