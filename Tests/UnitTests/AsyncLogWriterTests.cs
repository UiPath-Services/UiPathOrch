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

    // Read the log the way a reader must while the writer still holds it open: with
    // FileShare.ReadWrite. That is what PowerShell's provider does, so `Get-Content` (the
    // documented troubleshooting step) works against a mounted drive -- verified directly by
    // Reader_SharingTheFile_CanReadWhileTheWriterHoldsIt below. File.ReadAllLines and
    // File.OpenRead default to FileShare.Read, which denies the writer's existing write access
    // and throws; tests that read AFTER Dispose could use them, but one helper everywhere keeps
    // the harness from depending on whether a given test disposed first.
    private static string[] ReadAllLinesShared(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(fs);
        var lines = new List<string>();
        while (reader.ReadLine() is { } line) lines.Add(line);
        return [.. lines];
    }

    // A failed flush must not destroy the buffered entries.
    //
    // This is the bug behind the intermittent IdleFlush_LoneEntry failures: FlushBufferAsync
    // swallowed every exception and returned void, and the callers cleared the buffer regardless
    // -- so one transient IOException (a scanner holding the freshly created file open is enough)
    // discarded the entries, left the log file absent, and still reported DroppedEntries = 0. On
    // a lone-entry writer that meant an EMPTY LOG, which is precisely the failure the idle-flush
    // tests below exist to prevent, reached by a second independent route.
    //
    // Deterministic: the lock is held by this test, not by chance.
    [Fact]
    public async Task FailedFlush_RetainsEntriesAndWritesThemOnceTheFileIsWritableAgain()
    {
        var w = new AsyncLogWriter(_tempPath);
        try
        {
            using (new FileStream(_tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await w.WriteAsync("written-while-locked\n");   // cannot open: retained
            }

            // Lock released: the retained entry must go out with the next write.
            await w.WriteAsync("written-after-unlock\n");
        }
        finally
        {
            await w.DisposeAsync();
        }

        var lines = ReadAllLinesShared(_tempPath);
        Assert.Equal(new[] { "written-while-locked", "written-after-unlock" }, lines);
        Assert.Equal(0, w.GetStatistics().DroppedEntries);
    }

    // Nothing may leave the buffer unwritten without being counted -- the statistics used to
    // report a clean run while entries were being discarded.
    [Fact]
    public async Task UnwritableTarget_CountsTheEntriesItCannotPersist()
    {
        // A directory can never be opened for append: a permanent failure, not a transient one.
        var dirPath = Path.Combine(Path.GetTempPath(), $"asynclog_dir_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dirPath);
        try
        {
            var w = new AsyncLogWriter(dirPath);
            await w.WriteAsync("cannot-be-written\n");
            await w.DisposeAsync();   // last attempt, then the loss is counted

            var stats = w.GetStatistics();
            Assert.Equal(0, stats.TotalEntriesWritten);
            Assert.Equal(1, stats.DroppedEntries);
        }
        finally
        {
            try { Directory.Delete(dirPath, recursive: true); } catch { /* best-effort */ }
        }
    }

    // Retention is bounded by BYTES as well as by entry count. The byte cap is the one that
    // binds in practice: a Verbose entry carries a whole HTTP response body, so one entry can be
    // megabytes and a count-only limit would not actually bound the memory held during an outage.
    [Fact]
    public async Task RetentionIsBoundedByBytes_NotJustEntryCount()
    {
        var big = new string('x', 64 * 1024) + "\n";   // 64 KB per entry

        // Room for 100 entries but only ~256 KB, so the byte cap is what stops it.
        await using var w = new AsyncLogWriter(
            _tempPath, maxRetainedEntries: 100, maxRetainedBytes: 256 * 1024);

        using (new FileStream(_tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            for (int i = 0; i < 20; i++)   // 1.25 MB offered, far past the byte cap
                await w.WriteAsync(big);
        }

        var stats = w.GetStatistics();
        Assert.True(stats.DroppedEntries > 0,
            "the byte cap never evicted anything, so retention is effectively unbounded");
        // Well short of the 100-entry cap: the eviction was driven by size.
        Assert.True(stats.DroppedEntries >= 15, $"expected most entries evicted, got {stats.DroppedEntries}");
    }

    // A single entry larger than the whole byte cap must still be retained -- it is the most
    // recent context, and dropping it would leave nothing at all.
    [Fact]
    public async Task AnOversizedEntryIsStillRetained()
    {
        var huge = new string('x', 128 * 1024) + "\n";

        var w = new AsyncLogWriter(_tempPath, maxRetainedBytes: 16 * 1024);
        using (new FileStream(_tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await w.WriteAsync(huge);
        }
        await w.WriteAsync("after\n");   // lock released: the retained entry goes out with this
        await w.DisposeAsync();

        var lines = ReadAllLinesShared(_tempPath);
        Assert.Equal(2, lines.Length);
        Assert.Equal(128 * 1024, lines[0].Length);
        Assert.Equal("after", lines[1]);
        Assert.Equal(0, w.GetStatistics().DroppedEntries);
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

        var lines = ReadAllLinesShared(_tempPath);
        Assert.Equal(10, lines.Length);
        for (int i = 0; i < 10; i++)
            Assert.Equal($"line-{i}", lines[i]);
    }

    [Fact]
    public async Task Statistics_ReflectActualWriteCount()
    {
        const int n = 50;
        AsyncLogWriter w;
        using (w = new AsyncLogWriter(_tempPath))
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
        // Write many messages and Dispose without any explicit wait: every entry must be on
        // disk by the time Dispose returns, with nothing left queued behind it.
        const int n = 200;
        await using (var w = new AsyncLogWriter(_tempPath))
        {
            for (int i = 0; i < n; i++)
                await w.WriteAsync($"drain-{i}\n");
        }

        var lines = ReadAllLinesShared(_tempPath);
        Assert.Equal(n, lines.Length);
    }

    [Fact]
    public async Task WriteAfterDispose_ReturnsSilentlyWithoutThrowing()
    {
        var w = new AsyncLogWriter(_tempPath);
        await w.WriteAsync("before-dispose\n");
        await w.DisposeAsync();

        // Post-dispose writes return silently: the `_disposed` gate in WriteAsync short-circuits
        // before anything is queued, so there is nothing to account for and DroppedEntries stays
        // at 0. A cmdlet logging during teardown must never see an exception from the logger.
        await w.WriteAsync("after-dispose-1\n");
        await w.WriteAsync("after-dispose-2\n");

        var stats = w.GetStatistics();
        Assert.Equal(1, stats.TotalEntriesWritten);
        Assert.Equal(0, stats.DroppedEntries);

        var lines = ReadAllLinesShared(_tempPath);
        Assert.Single(lines);
        Assert.Equal("before-dispose", lines[0]);
    }

    [Fact]
    public async Task ConcurrentWrites_AllMessagesPersisted()
    {
        // 10 parallel tasks × 100 messages each — none must be lost.
        const int taskCount = 10;
        const int perTask = 100;
        await using (var w = new AsyncLogWriter(_tempPath, maxRetainedEntries: 5000))
        {
            var tasks = Enumerable.Range(0, taskCount).Select(async t =>
            {
                for (int i = 0; i < perTask; i++)
                    await w.WriteAsync($"task-{t:D2}-msg-{i:D3}\n");
            });
            await Task.WhenAll(tasks);
        }

        var lines = ReadAllLinesShared(_tempPath);
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
        // WriteAsync returns early on null/empty (alongside the _disposed check), so neither
        // the file nor the statistics get an entry for them.
        AsyncLogWriter w;
        await using ((w = new AsyncLogWriter(_tempPath)).ConfigureAwait(false))
        {
            await w.WriteAsync(null!);
            await w.WriteAsync("");
            await w.WriteAsync("real\n");
        }

        var stats = w.GetStatistics();
        Assert.Equal(1, stats.TotalEntriesWritten);

        var lines = ReadAllLinesShared(_tempPath);
        Assert.Single(lines);
        Assert.Equal("real", lines[0]);
    }

    // A lone entry must reach disk with NO further write and NO Dispose.
    //
    // This guarded a real failure: the writer used to defer a sub-batch entry to a time-based
    // flush, and a bug in that path left a single entry unwritten until a second one arrived --
    // the empty log seen on a hanging PKCE auth, i.e. exactly when the diagnostics were needed.
    // Writes are now synchronous, so the property holds by construction and the test no longer
    // has to poll for it; keeping it means any future reintroduction of deferred flushing has
    // to satisfy this first. Every other test here disposes before asserting, so none of them
    // would notice.
    [Fact]
    public async Task LoneEntry_IsOnDiskBeforeWriteAsyncReturns()
    {
        var w = new AsyncLogWriter(_tempPath);
        try
        {
            await w.WriteAsync("lone-idle-entry\n");

            // Read immediately: no polling, no disposing, no second write.
            Assert.True(File.Exists(_tempPath),
                "log file was not created by the write itself (deferred flushing reintroduced?)");
            var lines = ReadAllLinesShared(_tempPath);
            Assert.Single(lines);
            Assert.Equal("lone-idle-entry", lines[0]);
        }
        finally
        {
            await w.DisposeAsync();
        }
    }

    // An entry already persisted must not be written a second time by Dispose.
    [Fact]
    public async Task DisposeAfterAWrite_DoesNotDuplicateTheEntry()
    {
        var w = new AsyncLogWriter(_tempPath);
        await w.WriteAsync("once\n");

        await w.DisposeAsync();

        var lines = ReadAllLinesShared(_tempPath);
        Assert.Single(lines);
        Assert.Equal("once", lines[0]);
    }

    // Dispose is idempotent and a second call must not re-emit anything.
    [Fact]
    public async Task DoubleDispose_IsSafeAndDoesNotDuplicate()
    {
        var w = new AsyncLogWriter(_tempPath);
        await w.WriteAsync("only-once\n");

        await w.DisposeAsync();
        await w.DisposeAsync();
        w.Dispose();

        var lines = ReadAllLinesShared(_tempPath);
        Assert.Single(lines);
        Assert.Equal("only-once", lines[0]);
    }

    // The file-sharing contract of holding the handle open for the writer's lifetime.
    //
    // A reader that shares the file (FileShare.ReadWrite) sees the log live -- that is what
    // PowerShell's provider does, so `Get-Content` / `Get-Content -Wait` on a mounted drive, the
    // documented troubleshooting step, keeps working. A reader that demands FileShare.Read
    // instead (the default for File.ReadAllText / File.OpenRead) is refused, because that share
    // mode denies the writer's existing write access. Both halves are pinned: the first is the
    // promise, the second is the cost, and neither should change silently.
    [Fact]
    public async Task Reader_SharingTheFile_CanReadWhileTheWriterHoldsIt()
    {
        await using var w = new AsyncLogWriter(_tempPath);
        await w.WriteAsync("first\n");

        Assert.Equal(new[] { "first" }, ReadAllLinesShared(_tempPath));

        await w.WriteAsync("second\n");
        Assert.Equal(new[] { "first", "second" }, ReadAllLinesShared(_tempPath));
    }

    [Fact]
    public async Task Reader_DemandingExclusiveShare_IsRefusedWhileTheWriterHoldsIt()
    {
        await using var w = new AsyncLogWriter(_tempPath);
        await w.WriteAsync("first\n");

        Assert.Throws<IOException>(() => File.ReadAllText(_tempPath));
    }

    // The handle is released once writing goes idle, so the log stops blocking Compress-Archive /
    // Get-FileHash / Remove-Item -- the "collect the logs" step of a support request.
    [Fact]
    public async Task IdleWriter_ReleasesTheFileAndReopensOnTheNextWrite()
    {
        await using var w = new AsyncLogWriter(_tempPath, idleCloseMs: 150);
        await w.WriteAsync("before-idle\n");
        Assert.Throws<IOException>(() => File.ReadAllText(_tempPath));   // held while active

        // Wait past the idle window plus a poll interval.
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < deadline)
        {
            try { File.ReadAllText(_tempPath); break; } catch (IOException) { await Task.Delay(25); }
        }

        // An exclusive-share opener now succeeds: this is the whole point of closing on idle.
        Assert.Equal("before-idle\n", File.ReadAllText(_tempPath).Replace("\r\n", "\n"));

        // ...and the next write transparently reopens.
        await w.WriteAsync("after-idle\n");
        Assert.Equal(new[] { "before-idle", "after-idle" }, ReadAllLinesShared(_tempPath));
    }

    // Closing on idle opens a window the always-open design did not have: something else can take
    // the file EXCLUSIVELY while we are closed. Writes made during that outage must survive it --
    // the retention path is what makes closing on idle safe to do at all.
    [Fact]
    public async Task ExclusiveHolderDuringTheIdleWindow_DoesNotCostEntries()
    {
        await using var w = new AsyncLogWriter(_tempPath, idleCloseMs: 150);
        await w.WriteAsync("before-outage\n");

        // Let the writer go idle and release the file.
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < deadline)
        {
            try { File.ReadAllText(_tempPath); break; } catch (IOException) { await Task.Delay(25); }
        }

        // An editor grabs it exclusively while we are closed, and we keep logging throughout.
        using (new FileStream(_tempPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            await w.WriteAsync("during-outage-1\n");
            await w.WriteAsync("during-outage-2\n");
        }

        await w.WriteAsync("after-outage\n");

        Assert.Equal(
            new[] { "before-outage", "during-outage-1", "during-outage-2", "after-outage" },
            ReadAllLinesShared(_tempPath));
        Assert.Equal(0, w.GetStatistics().DroppedEntries);
    }
}
