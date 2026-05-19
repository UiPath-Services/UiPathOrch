using UiPath.OrchAPI;
using Xunit;

namespace UnitTests;

// Replaces Tests/AsyncLogTest (a console app of perf benchmarks that hadn't been
// run in CI). These tests cover the correctness contract of AsyncLogWriter:
// every queued message is durable, statistics reflect actual writes, dispose
// drains the buffer, post-dispose writes are recorded as drops, and concurrent
// writers don't lose messages. Performance/throughput benchmarks (the bulk of
// the old console app) are intentionally not ported — they don't gate behaviour
// and would slow CI without catching regressions.

public class AsyncLogWriterTests : IDisposable
{
    private readonly string _tempPath = Path.Combine(Path.GetTempPath(), $"asynclog_test_{Guid.NewGuid():N}.log");

    public void Dispose()
    {
        try { if (File.Exists(_tempPath)) File.Delete(_tempPath); }
        catch { /* best-effort cleanup */ }
    }

    [Fact]
    public async Task BasicWrites_AllPersistedAfterDispose()
    {
        // Smallest contract: 10 messages in, 10 lines out, in order, with the
        // exact content we wrote (no dropped chars, no extra newlines added).
        await using (var w = new AsyncLogWriter(_tempPath))
        {
            for (int i = 0; i < 10; i++)
                await w.WriteAsync($"line-{i}\n");
        } // DisposeAsync waits for the background writer to drain.

        var lines = await File.ReadAllLinesAsync(_tempPath);
        Assert.Equal(10, lines.Length);
        for (int i = 0; i < 10; i++)
            Assert.Equal($"line-{i}", lines[i]);
    }

    [Fact]
    public async Task Statistics_ReflectActualWriteCount()
    {
        const int n = 50;
        AsyncLogWriter w;
        using (w = new AsyncLogWriter(_tempPath, batchSize: 7))
        {
            for (int i = 0; i < n; i++)
                await w.WriteAsync($"msg-{i}\n");
        } // sync Dispose → DisposeAsync internally → drain

        var stats = w.GetStatistics();
        Assert.Equal(n, stats.TotalEntriesWritten);
        Assert.Equal(0, stats.DroppedEntries);
        Assert.True(stats.BatchesWritten >= 1);
        Assert.True(stats.AverageEntriesPerBatch > 0);
        Assert.True(stats.TotalBytesWritten > 0);
    }

    [Fact]
    public async Task DisposeAsync_DrainsBufferedMessages()
    {
        // Write many messages and Dispose without explicit Task.Delay — the
        // dispose contract must wait for the background processor to finish.
        const int n = 200;
        await using (var w = new AsyncLogWriter(_tempPath, batchSize: 50, flushIntervalMs: 100))
        {
            for (int i = 0; i < n; i++)
                await w.WriteAsync($"drain-{i}\n");
        }

        var lines = await File.ReadAllLinesAsync(_tempPath);
        Assert.Equal(n, lines.Length);
    }

    [Fact]
    public async Task WriteAfterDispose_ReturnsSilentlyWithoutThrowing()
    {
        var w = new AsyncLogWriter(_tempPath);
        await w.WriteAsync("before-dispose\n");
        await w.DisposeAsync();

        // Post-dispose writes return silently — the early `_disposed` check
        // in WriteAsync short-circuits before the channel's drop-tracking
        // path, so DroppedEntries stays at 0. (RecordDroppedEntry only
        // fires when WriteAsync gets through the _disposed gate but then
        // sees the channel already Complete()d — a narrow race between
        // _writer.Complete() and _disposed being set in DisposeAsync.)
        await w.WriteAsync("after-dispose-1\n");
        await w.WriteAsync("after-dispose-2\n");

        var stats = w.GetStatistics();
        Assert.Equal(1, stats.TotalEntriesWritten);
        Assert.Equal(0, stats.DroppedEntries);

        var lines = await File.ReadAllLinesAsync(_tempPath);
        Assert.Single(lines);
        Assert.Equal("before-dispose", lines[0]);
    }

    [Fact]
    public async Task ConcurrentWrites_AllMessagesPersisted()
    {
        // 10 parallel tasks × 100 messages each — none must be lost.
        const int taskCount = 10;
        const int perTask = 100;
        await using (var w = new AsyncLogWriter(_tempPath, maxQueueSize: 5000, batchSize: 100))
        {
            var tasks = Enumerable.Range(0, taskCount).Select(async t =>
            {
                for (int i = 0; i < perTask; i++)
                    await w.WriteAsync($"task-{t:D2}-msg-{i:D3}\n");
            });
            await Task.WhenAll(tasks);
        }

        var lines = await File.ReadAllLinesAsync(_tempPath);
        Assert.Equal(taskCount * perTask, lines.Length);

        // Each task's messages must all appear (we don't assert global order
        // across tasks — concurrent writers race — only that no message is lost).
        for (int t = 0; t < taskCount; t++)
        {
            var taskLines = lines.Where(l => l.StartsWith($"task-{t:D2}-")).ToList();
            Assert.Equal(perTask, taskLines.Count);
        }
    }

    [Fact]
    public async Task EmptyOrNullMessage_NotEnqueued()
    {
        // WriteAsync returns early on null/empty (alongside the _disposed
        // check), so neither the queue nor the file gets an entry for them.
        AsyncLogWriter w;
        await using ((w = new AsyncLogWriter(_tempPath)).ConfigureAwait(false))
        {
            await w.WriteAsync(null!);
            await w.WriteAsync("");
            await w.WriteAsync("real\n");
        }

        var stats = w.GetStatistics();
        Assert.Equal(1, stats.TotalEntriesWritten);

        var lines = await File.ReadAllLinesAsync(_tempPath);
        Assert.Single(lines);
        Assert.Equal("real", lines[0]);
    }

    [Fact]
    public async Task IdleFlush_LoneEntry_PersistedWithoutSecondWriteOrDispose()
    {
        // Regression guard for the time-based flush. A single buffered entry,
        // with NO further writes and NO Dispose, must still reach disk within a
        // bounded time. Before the fix the flush interval was only re-evaluated
        // when the *next* entry was dequeued, so a lone entry was never written
        // until a 2nd entry arrived or the writer was disposed -- exactly the
        // empty log seen on a hanging PKCE auth. Every other test here disposes
        // before asserting, so none of them would catch this regression.
        var w = new AsyncLogWriter(_tempPath, batchSize: 100, flushIntervalMs: 150);
        try
        {
            await w.WriteAsync("lone-idle-entry\n");

            // Poll WITHOUT disposing and WITHOUT writing anything else.
            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (DateTime.UtcNow < deadline &&
                   !(File.Exists(_tempPath) && new FileInfo(_tempPath).Length > 0))
            {
                await Task.Delay(25);
            }

            Assert.True(File.Exists(_tempPath),
                "log file was never created while the producer stayed idle (interval flush regressed)");
            var lines = await File.ReadAllLinesAsync(_tempPath);
            Assert.Single(lines);
            Assert.Equal("lone-idle-entry", lines[0]);
        }
        finally
        {
            await w.DisposeAsync();
        }
    }

    [Fact]
    public async Task IdleFlush_ThenDispose_DoesNotDuplicateEntry()
    {
        // After a time-based flush the buffer must be cleared, so a later
        // shutdown drain does not re-emit the already-written entry.
        var w = new AsyncLogWriter(_tempPath, batchSize: 100, flushIntervalMs: 150);
        await w.WriteAsync("once\n");

        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < deadline &&
               !(File.Exists(_tempPath) && new FileInfo(_tempPath).Length > 0))
        {
            await Task.Delay(25);
        }
        Assert.True(File.Exists(_tempPath), "idle flush did not occur");

        await w.DisposeAsync(); // shutdown drain must not write "once" a second time

        var lines = await File.ReadAllLinesAsync(_tempPath);
        Assert.Single(lines);
        Assert.Equal("once", lines[0]);
    }
}
