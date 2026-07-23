using UiPath.OrchAPI;
using Xunit;

namespace UnitTests;

// Correctness contract of LogFileWriter: every accepted entry is durable, entries survive a
// transient failure to open or write, nothing leaves the writer unrecorded, the file-sharing
// behaviour of holding the handle is what we say it is, and the handle is released once writing
// goes idle. (Replaces Tests/AsyncLogTest, a console app of perf benchmarks that never ran in
// CI; the benchmarks are deliberately not ported -- they gate nothing and would slow CI.)

public class LogFileWriterTests : IDisposable
{
    private readonly string _tempPath = Path.Combine(Path.GetTempPath(), $"logfile_test_{Guid.NewGuid():N}.log");

    public void Dispose()
    {
        try { if (File.Exists(_tempPath)) File.Delete(_tempPath); }
        catch { /* best-effort cleanup */ }
    }

    // Read the log the way a reader must while the writer still holds it open: with
    // FileShare.ReadWrite. That is what PowerShell's provider does, so `Get-Content` (the
    // documented troubleshooting step) works against a mounted drive -- pinned directly by
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

    // A failed write must not destroy the entry.
    //
    // The predecessor swallowed every exception and cleared its buffer regardless, so one
    // transient IOException -- a scanner holding the freshly created file open is enough --
    // discarded the entries, left the log file absent, and still reported DroppedEntries = 0. On
    // a lone-entry writer that meant an EMPTY LOG, exactly what the idle tests below guard
    // against, reached by a second independent route. Retention is also what makes releasing the
    // handle on idle safe (see ExclusiveHolderDuringTheIdleWindow_DoesNotCostEntries).
    //
    // Deterministic: the lock is held by this test, not by chance.
    [Fact]
    public void FailedWrite_RetainsTheEntryAndWritesItOnceTheFileIsWritableAgain()
    {
        var w = new LogFileWriter(_tempPath);
        try
        {
            using (new FileStream(_tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                w.Write("written-while-locked\n");   // cannot open: retained
            }

            // Lock released: the retained entry must go out with the next write.
            w.Write("written-after-unlock\n");
        }
        finally
        {
            w.Dispose();
        }

        Assert.Equal(new[] { "written-while-locked", "written-after-unlock" }, ReadAllLinesShared(_tempPath));
        Assert.Equal(0, w.GetStatistics().DroppedEntries);
    }

    // Nothing may leave the writer unwritten without being counted -- the statistics used to
    // report a clean run while entries were being discarded.
    [Fact]
    public void UnwritableTarget_CountsTheEntriesItCannotPersist()
    {
        // A directory can never be opened for append: a permanent failure, not a transient one.
        var dirPath = Path.Combine(Path.GetTempPath(), $"logfile_dir_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dirPath);
        try
        {
            var w = new LogFileWriter(dirPath);
            w.Write("cannot-be-written\n");
            w.Dispose();   // last attempt, then the loss is counted

            var stats = w.GetStatistics();
            Assert.Equal(0, stats.TotalEntriesWritten);
            Assert.Equal(1, stats.DroppedEntries);
        }
        finally
        {
            try { Directory.Delete(dirPath, recursive: true); } catch { /* best-effort */ }
        }
    }

    // Retention is bounded by BYTES as well as by entry count. The byte bound is the one that
    // binds in practice: a Verbose entry carries a whole HTTP response body, so one entry can be
    // megabytes and a count-only limit would not actually bound the memory held during an outage.
    [Fact]
    public void RetentionIsBoundedByBytes_NotJustEntryCount()
    {
        var big = new string('x', 64 * 1024) + "\n";   // 64 KB per entry

        // Room for 100 entries but only ~256 KB, so the byte cap is what stops it.
        using var w = new LogFileWriter(_tempPath, maxRetainedEntries: 100, maxRetainedBytes: 256 * 1024);

        using (new FileStream(_tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            for (int i = 0; i < 20; i++)   // 1.25 MB offered, far past the byte cap
                w.Write(big);
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
    public void AnOversizedEntryIsStillRetained()
    {
        var huge = new string('x', 128 * 1024) + "\n";

        var w = new LogFileWriter(_tempPath, maxRetainedBytes: 16 * 1024);
        using (new FileStream(_tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            w.Write(huge);
        }
        w.Write("after\n");   // lock released: the retained entry goes out with this
        w.Dispose();

        var lines = ReadAllLinesShared(_tempPath);
        Assert.Equal(2, lines.Length);
        Assert.Equal(128 * 1024, lines[0].Length);
        Assert.Equal("after", lines[1]);
        Assert.Equal(0, w.GetStatistics().DroppedEntries);
    }

    // Sequential writes from one thread must appear in the order they were made.
    //
    // This is what the Task.Run offload at the call sites destroyed: each block became an
    // independent thread-pool work item, so two log blocks produced back-to-back on the SAME
    // thread could reach the writer in either order and the sequence numbers in the log came out
    // shuffled. Writing inline is what restores it, and this pins the property at the writer.
    [Fact]
    public void SequentialWrites_AppearInTheOrderTheyWereMade()
    {
        using var w = new LogFileWriter(_tempPath);
        for (int i = 0; i < 200; i++)
            w.Write($"#{i:D4}\n");

        var lines = ReadAllLinesShared(_tempPath);
        Assert.Equal(200, lines.Length);
        for (int i = 0; i < 200; i++)
            Assert.Equal($"#{i:D4}", lines[i]);
    }

    [Fact]
    public void BasicWrites_AllPersisted()
    {
        // Smallest contract: 10 messages in, 10 lines out, in order, with the
        // exact content we wrote (no dropped chars, no extra newlines added).
        using (var w = new LogFileWriter(_tempPath))
        {
            for (int i = 0; i < 10; i++)
                w.Write($"line-{i}\n");
        }

        var lines = ReadAllLinesShared(_tempPath);
        Assert.Equal(10, lines.Length);
        for (int i = 0; i < 10; i++)
            Assert.Equal($"line-{i}", lines[i]);
    }

    [Fact]
    public void Statistics_ReflectActualWriteCount()
    {
        const int n = 50;
        LogFileWriter w;
        using (w = new LogFileWriter(_tempPath))
        {
            for (int i = 0; i < n; i++)
                w.Write($"msg-{i}\n");
        }

        var stats = w.GetStatistics();
        Assert.Equal(n, stats.TotalEntriesWritten);
        Assert.Equal(0, stats.DroppedEntries);
        Assert.True(stats.BatchesWritten >= 1);
        Assert.True(stats.AverageEntriesPerBatch > 0);
        Assert.True(stats.TotalBytesWritten > 0);
    }

    [Fact]
    public void Dispose_LeavesEverythingOnDisk()
    {
        // Write many messages and Dispose without any explicit wait: every entry must be on
        // disk by the time Dispose returns, with nothing left behind it.
        const int n = 200;
        using (var w = new LogFileWriter(_tempPath))
        {
            for (int i = 0; i < n; i++)
                w.Write($"drain-{i}\n");
        }

        Assert.Equal(n, ReadAllLinesShared(_tempPath).Length);
    }

    [Fact]
    public void WriteAfterDispose_ReturnsSilentlyWithoutThrowing()
    {
        var w = new LogFileWriter(_tempPath);
        w.Write("before-dispose\n");
        w.Dispose();

        // Post-dispose writes return silently: the `_disposed` gate in Write short-circuits
        // before anything is queued, so there is nothing to account for and DroppedEntries stays
        // at 0. A cmdlet logging during teardown must never see an exception from the logger.
        w.Write("after-dispose-1\n");
        w.Write("after-dispose-2\n");

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
        // 10 parallel writers × 100 messages each — none must be lost.
        const int taskCount = 10;
        const int perTask = 100;
        using (var w = new LogFileWriter(_tempPath, maxRetainedEntries: 5000))
        {
            var tasks = Enumerable.Range(0, taskCount)
                .Select(t => Task.Run(() =>
                {
                    for (int i = 0; i < perTask; i++)
                        w.Write($"task-{t:D2}-msg-{i:D3}\n");
                }));
            await Task.WhenAll(tasks);
        }

        var lines = ReadAllLinesShared(_tempPath);
        Assert.Equal(taskCount * perTask, lines.Length);

        // Each writer's messages must all appear (we don't assert global order across writers --
        // they race for the lock -- only that no message is lost or interleaved mid-entry).
        for (int t = 0; t < taskCount; t++)
        {
            var taskLines = lines.Where(l => l.StartsWith($"task-{t:D2}-")).ToList();
            Assert.Equal(perTask, taskLines.Count);
        }
    }

    [Fact]
    public void EmptyOrNullMessage_IsIgnored()
    {
        // Write returns early on null/empty (alongside the _disposed check), so neither the file
        // nor the statistics get an entry for them.
        LogFileWriter w;
        using (w = new LogFileWriter(_tempPath))
        {
            w.Write(null!);
            w.Write("");
            w.Write("real\n");
        }

        Assert.Equal(1, w.GetStatistics().TotalEntriesWritten);

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
    // has to poll for it; keeping it means any future reintroduction of deferred flushing has to
    // satisfy this first. Every other test here disposes before asserting, so none would notice.
    [Fact]
    public void LoneEntry_IsOnDiskBeforeWriteReturns()
    {
        var w = new LogFileWriter(_tempPath);
        try
        {
            w.Write("lone-idle-entry\n");

            // Read immediately: no polling, no disposing, no second write.
            Assert.True(File.Exists(_tempPath),
                "log file was not created by the write itself (deferred flushing reintroduced?)");
            var lines = ReadAllLinesShared(_tempPath);
            Assert.Single(lines);
            Assert.Equal("lone-idle-entry", lines[0]);
        }
        finally
        {
            w.Dispose();
        }
    }

    // An entry already persisted must not be written a second time by Dispose.
    [Fact]
    public void DisposeAfterAWrite_DoesNotDuplicateTheEntry()
    {
        var w = new LogFileWriter(_tempPath);
        w.Write("once\n");

        w.Dispose();

        var lines = ReadAllLinesShared(_tempPath);
        Assert.Single(lines);
        Assert.Equal("once", lines[0]);
    }

    // Dispose is idempotent and a second call must not re-emit anything.
    [Fact]
    public void DoubleDispose_IsSafeAndDoesNotDuplicate()
    {
        var w = new LogFileWriter(_tempPath);
        w.Write("only-once\n");

        w.Dispose();
        w.Dispose();

        var lines = ReadAllLinesShared(_tempPath);
        Assert.Single(lines);
        Assert.Equal("only-once", lines[0]);
    }

    // The file-sharing contract of holding the handle open while writing is active.
    //
    // A reader that shares the file (FileShare.ReadWrite) sees the log live -- that is what
    // PowerShell's provider does, so `Get-Content` / `Show-TextFiles` / `Select-String` /
    // `Copy-Item` on a mounted drive keep working. A reader that demands FileShare.Read instead
    // (the default for File.ReadAllText, StreamReader(path), Get-FileHash, Remove-Item and
    // Compress-Archive) is refused, because that share mode denies the writer's existing write
    // access. Both halves are pinned: the first is the promise, the second is the cost that the
    // idle release below exists to bound, and neither should change silently.
    [Fact]
    public void Reader_SharingTheFile_CanReadWhileTheWriterHoldsIt()
    {
        using var w = new LogFileWriter(_tempPath);
        w.Write("first\n");

        Assert.Equal(new[] { "first" }, ReadAllLinesShared(_tempPath));

        w.Write("second\n");
        Assert.Equal(new[] { "first", "second" }, ReadAllLinesShared(_tempPath));
    }

    [Fact]
    public void Reader_DemandingExclusiveShare_IsRefusedWhileTheWriterHoldsIt()
    {
        // Windows-only: FileShare is mandatory here, so an opener that denies our write share is
        // refused while we hold the handle. On Unix .NET emulates FileShare with advisory flock,
        // where our shared lock and the reader's are compatible and nothing is refused -- the very
        // behaviour that made holding the handle a non-issue there. See LogFileWriter's remarks.
        if (!OperatingSystem.IsWindows()) return;

        using var w = new LogFileWriter(_tempPath);
        w.Write("first\n");

        Assert.Throws<IOException>(() => File.ReadAllText(_tempPath));
    }

    // The handle is released once writing goes idle, so the log stops blocking Compress-Archive /
    // Get-FileHash / Remove-Item -- the "collect the logs" step of a support request.
    [Fact]
    public async Task IdleWriter_ReleasesTheFileAndReopensOnTheNextWrite()
    {
        // Windows-only: the "held while active" assertion below depends on FileShare being
        // mandatory. On Unix the shared advisory lock never refuses the reader, so there is no
        // held-vs-released transition to observe. See LogFileWriter's remarks.
        if (!OperatingSystem.IsWindows()) return;

        using var w = new LogFileWriter(_tempPath, idleCloseMs: 150);
        w.Write("before-idle\n");
        Assert.Throws<IOException>(() => File.ReadAllText(_tempPath));   // held while active

        await WaitUntilExclusivelyOpenable(_tempPath);

        // An exclusive-share opener now succeeds: this is the whole point of closing on idle.
        Assert.Equal("before-idle\n", File.ReadAllText(_tempPath).Replace("\r\n", "\n"));

        // ...and the next write transparently reopens.
        w.Write("after-idle\n");
        Assert.Equal(new[] { "before-idle", "after-idle" }, ReadAllLinesShared(_tempPath));
    }

    // Closing on idle opens a window the always-open design did not have: something else can take
    // the file EXCLUSIVELY while we are closed. Writes made during that outage must survive it --
    // the retention path is what makes closing on idle safe to do at all.
    [Fact]
    public async Task ExclusiveHolderDuringTheIdleWindow_DoesNotCostEntries()
    {
        // Windows-only: the outage this models is another process taking the file with
        // FileShare.None. On Unix that maps to an exclusive advisory flock whose interaction with
        // our reopening write handle is not the mandatory-lock outage the test asserts. The
        // retention path it exercises is platform-agnostic and covered by the failure-path tests
        // above; here we pin the Windows behaviour specifically. See LogFileWriter's remarks.
        if (!OperatingSystem.IsWindows()) return;

        using var w = new LogFileWriter(_tempPath, idleCloseMs: 150);
        w.Write("before-outage\n");

        await WaitUntilExclusivelyOpenable(_tempPath);

        // An editor grabs it exclusively while we are closed, and we keep logging throughout.
        using (new FileStream(_tempPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            w.Write("during-outage-1\n");
            w.Write("during-outage-2\n");
        }

        w.Write("after-outage\n");

        Assert.Equal(
            new[] { "before-outage", "during-outage-1", "during-outage-2", "after-outage" },
            ReadAllLinesShared(_tempPath));
        Assert.Equal(0, w.GetStatistics().DroppedEntries);
    }

    /// Wait for the writer's idle timer to release the handle. Polls rather than sleeping a fixed
    /// interval so the test does not depend on timer precision under CI load.
    private static async Task WaitUntilExclusivelyOpenable(string path)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < deadline)
        {
            try { File.ReadAllText(path); return; }
            catch (IOException) { await Task.Delay(25); }
        }
        Assert.Fail("the writer never released the file handle after going idle");
    }
}
