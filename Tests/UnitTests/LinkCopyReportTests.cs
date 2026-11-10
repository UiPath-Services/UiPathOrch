using UiPath.PowerShell.Core;
using Xunit;

namespace UnitTests;

// LinkCopyReport lives for one Copy-Item invocation and is flushed after each source path,
// because the provider cannot tell which path is the last. These pin down that a miss is
// reported once, that a later path's link cancels a pending miss silently, and that a later
// link of an already-reported miss is corrected instead of leaving a false warning.
public class LinkCopyReportTests
{
    private static OrchProvider.LinkCopyReport NewReport() => new();

    [Fact]
    public void Link_in_the_same_path_cancels_the_miss_silently()
    {
        var host = new CapturingHost();
        var r = NewReport();
        r.NoteUnlinked("asset", 41, "zzShared", "MISS zzShared");
        r.NoteLinked("asset", 41, "zzShared");
        r.Flush(host);
        Assert.Empty(host.Warnings);
    }

    [Fact]
    public void Miss_flushed_after_one_path_is_corrected_when_a_later_path_links_it()
    {
        var host = new CapturingHost();
        var r = NewReport();

        // path A: D's counterpart does not exist yet
        r.NoteUnlinked("asset", 41, "zzShared", "MISS zzShared");
        r.Flush(host);
        Assert.Equal(new[] { "MISS zzShared" }, host.Warnings);

        // path D: links A's entity
        r.NoteLinked("asset", 41, "zzShared");
        r.Flush(host);
        Assert.Equal(2, host.Warnings.Count);
        Assert.Contains("'zzShared' is now shared", host.Warnings[1]);
    }

    [Fact]
    public void Miss_is_reported_once_across_flushes()
    {
        var host = new CapturingHost();
        var r = NewReport();
        r.NoteUnlinked("asset", 7, "X", "MISS X");
        r.Flush(host);
        r.NoteUnlinked("asset", 7, "X", "MISS X");
        r.Flush(host);
        Assert.Equal(new[] { "MISS X" }, host.Warnings);
    }

    [Fact]
    public void Flush_with_nothing_new_writes_nothing()
    {
        var host = new CapturingHost();
        var r = NewReport();
        r.NoteUnlinked("queue", 1, "Q", "MISS Q");
        r.Flush(host);
        r.Flush(host);
        Assert.Single(host.Warnings);
    }

    [Fact]
    public void Beyond_the_threshold_misses_are_summarized_once_per_new_batch()
    {
        var host = new CapturingHost();
        var r = NewReport();
        for (int i = 1; i <= 7; i++) r.NoteUnlinked("asset", i, $"A{i}", $"MISS A{i}");
        r.Flush(host);
        // 5 in full + 1 summary
        Assert.Equal(6, host.Warnings.Count);
        Assert.StartsWith("7 shared entities", host.Warnings[5]);
        Assert.Contains("2 further warning(s)", host.Warnings[5]);

        r.Flush(host); // nothing new: no repeated summary
        Assert.Equal(6, host.Warnings.Count);

        r.NoteUnlinked("asset", 8, "A8", "MISS A8");
        r.Flush(host);
        Assert.Equal(7, host.Warnings.Count);
        Assert.StartsWith("8 shared entities", host.Warnings[6]);
    }

    [Fact]
    public void Linking_a_suppressed_miss_writes_no_correction()
    {
        var host = new CapturingHost();
        var r = NewReport();
        for (int i = 1; i <= 6; i++) r.NoteUnlinked("asset", i, $"A{i}", $"MISS A{i}");
        r.Flush(host);
        int before = host.Warnings.Count;
        r.NoteLinked("asset", 6, "A6"); // A6 was only counted in the summary
        r.Flush(host);
        Assert.Equal(before, host.Warnings.Count);
    }

    [Fact]
    public void Same_tenant_summary_is_not_repeated_without_new_copies()
    {
        var host = new CapturingHost();
        var r = NewReport();
        for (int i = 0; i < 6; i++) r.NoteSameTenantCopy(host, $"SAME {i}");
        r.Flush(host);
        r.Flush(host);
        Assert.Equal(6, host.Warnings.Count); // 5 in full + 1 summary, once
        Assert.StartsWith("6 shared entities were copied without", host.Warnings[5]);
    }
}
