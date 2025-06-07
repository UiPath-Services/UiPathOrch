using System.Threading.Channels;
using System.Text;
using System.Diagnostics;

namespace UiPath.OrchAPI;

/// <summary>
/// 非同期ログ書き込みを提供するクラス
/// Channelベースのキューイングシステムでログ書き込みのブロッキングを解消
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

    // 設定可能なパラメータ
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

        // チャネルの設定
        var options = new BoundedChannelOptions(maxQueueSize)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        };

        _logQueue = Channel.CreateBounded<LogEntry>(options);
        _writer = _logQueue.Writer;
        
        // バックグラウンドでの処理タスクを開始
        _processingTask = ProcessLogEntriesAsync(_shutdownCts.Token);
    }

    /// <summary>
    /// ログエントリーを非同期でキューに追加します
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
            // チャネルが閉じられている場合
            _metrics.RecordDroppedEntry();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // キャンセル要求は正常なケース
        }
    }

    /// <summary>
    /// 同期版
    /// ノンブロッキングでキューに追加を試行
    /// </summary>
    //public void Write(string logContent)
    //{
    //    if (_disposed || string.IsNullOrEmpty(logContent))
    //        return;

    //    var entry = new LogEntry(DateTime.UtcNow, logContent);
        
    //    if (!_writer.TryWrite(entry))
    //    {
    //        // キューがフルまたは閉じられている場合
    //        _metrics.RecordDroppedEntry();
    //    }
    //}

    /// <summary>
    /// ログ統計情報を取得
    /// </summary>
    public LogStatistics GetStatistics() => _metrics.GetStatistics();

    /// <summary>
    /// バックグラウンドでログエントリーを処理
    /// </summary>
    private async Task ProcessLogEntriesAsync(CancellationToken cancellationToken)
    {
        var buffer = new List<LogEntry>(_batchSize);
        var lastFlushTime = DateTime.UtcNow;

        try
        {
            await foreach (var entry in _logQueue.Reader.ReadAllAsync(cancellationToken))
            {
                buffer.Add(entry);

                var shouldFlush = buffer.Count >= _batchSize ||
                                 (DateTime.UtcNow - lastFlushTime).TotalMilliseconds >= _flushIntervalMs;

                if (shouldFlush)
                {
                    await FlushBufferAsync(buffer, cancellationToken);
                    buffer.Clear();
                    lastFlushTime = DateTime.UtcNow;
                }
            }

            // シャットダウン時の残りエントリー処理
            if (buffer.Count > 0)
            {
                await FlushBufferAsync(buffer, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // 正常なシャットダウン
        }
        catch (Exception ex)
        {
            // エラーログは別の手段で記録
            Debug.WriteLine($"AsyncLogWriter processing failed: {ex}");
        }
    }

    /// <summary>
    /// バッファーの内容をファイルに書き込み
    /// </summary>
    private async Task FlushBufferAsync(List<LogEntry> buffer, CancellationToken cancellationToken)
    {
        if (buffer.Count == 0)
            return;

        await _fileSemaphore.WaitAsync(cancellationToken);
        try
        {
            // ディレクトリが存在しない場合は作成
            var directory = Path.GetDirectoryName(_logFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // バッチ書き込み用のStringBuilder
            var stringBuilder = new StringBuilder();
            int totalBytes = 0;

            foreach (var entry in buffer)
            {
                stringBuilder.Append(entry.Content);
                totalBytes += Encoding.UTF8.GetByteCount(entry.Content);
            }

            await File.AppendAllTextAsync(_logFilePath, stringBuilder.ToString(), cancellationToken);
            
            // メトリクス更新
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

    /// <summary>
    /// 非同期でのシャットダウン
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            // 新しいエントリーの受付を停止
            _writer.Complete();

            // バックグラウンド処理の完了を待機（タイムアウト付き）
            _shutdownCts.CancelAfter(TimeSpan.FromSeconds(10));
            await _processingTask;
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine("AsyncLogWriter shutdown timeout - some log entries may be lost");
        }
        finally
        {
            _fileSemaphore?.Dispose();
            _shutdownCts?.Dispose();
        }
    }

    /// <summary>
    /// 同期版のDispose（IDisposableインターフェース）
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
                disposeTask.AsTask().Wait(TimeSpan.FromSeconds(5));
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"AsyncLogWriter dispose error: {ex}");
        }
    }
}

/// <summary>
/// ログエントリーを表す内部構造体
/// </summary>
internal readonly record struct LogEntry(DateTime Timestamp, string Content);

/// <summary>
/// ログメトリクスを管理するクラス
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
/// ログ統計情報を表す構造体
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

