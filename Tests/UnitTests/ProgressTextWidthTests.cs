using System.Management.Automation;
using UiPath.PowerShell.Core;
using Xunit;

namespace UnitTests;

// Unit tests for the Write-Progress width-safety layer that works around PowerShell
// #21293 (the console host sizes the bar by string.Length, so a BMP character that is one
// code unit but two cells wraps the bar's trailing ']'). EastAsianWidth.CollapseWide
// replaces each run of such characters with "[N]" (keeping everything else); ProgressReporter
// applies it to both the status and the activity line unless the host renders wide text
// correctly.
public class ProgressTextWidthTests
{
    private sealed class CapturingHost(bool rendersWideProgress = false) : IWritableHost
    {
        public ProgressRecord? Last { get; private set; }
        public List<ProgressRecord> All { get; } = new();
        public bool RendersWideProgress { get; } = rendersWideProgress;
        public void WriteProgress(ProgressRecord progressRecord)
        {
            Last = progressRecord;
            // ProgressReporter mutates one shared record, so snapshot the fields we assert on.
            All.Add(new ProgressRecord(progressRecord.ActivityId, progressRecord.Activity, progressRecord.StatusDescription)
            {
                RecordType = progressRecord.RecordType,
            });
        }
        public void WriteWarning(string text) { }
        public void WriteError(ErrorRecord errorRecord) { }
        public bool ShouldProcess(string target, string action) => true;
    }

    [Fact]
    public void WithProgressBar_YieldsAllItems_ReportsIndexTotalName_AndCompletes()
    {
        var host = new CapturingHost();
        var items = new[] { "a", "b", "c" };

        var seen = items.WithProgressBar(host, "Doing things", x => x).ToList();

        Assert.Equal(items, seen); // pass-through, unchanged order

        var processing = host.All.FindAll(r => r.RecordType == ProgressRecordType.Processing);
        Assert.Equal(3, processing.Count);
        Assert.Equal("Doing things", processing[0].Activity);
        Assert.Equal("1/3 a", processing[0].StatusDescription);
        Assert.Equal("2/3 b", processing[1].StatusDescription);
        Assert.Equal("3/3 c", processing[2].StatusDescription);
        Assert.Contains(host.All, r => r.RecordType == ProgressRecordType.Completed);
    }

    [Fact]
    public void WithProgressBar_NoGetName_ShowsIndexTotalOnly()
    {
        var host = new CapturingHost();

        _ = new[] { 10, 20 }.WithProgressBar(host, "Counting").ToList();

        var processing = host.All.FindAll(r => r.RecordType == ProgressRecordType.Processing);
        Assert.Equal(new[] { "1/2", "2/2" }, processing.ConvertAll(r => r.StatusDescription));
    }

    [Theory]
    [InlineData("Invoice")]              // plain ASCII
    [InlineData("Folder_2024-06")]       // ASCII with digits/punct
    [InlineData("Ménière")]              // accented Latin (Ambiguous letters kept -> shown)
    [InlineData("Папка")]                // Cyrillic (Narrow)
    [InlineData("ｾﾞﾝｶｸ")]               // halfwidth katakana (one cell)
    [InlineData("½ Ω")]                  // Ambiguous outside the curated symbol set: shown
    [InlineData("\U00020BB7")]           // CJK Ext B (U+20BB7): 2 code units = 2 cells
    [InlineData("\U0001F600")]           // supplementary emoji: 2 code units = 2 cells
    [InlineData("")]
    public void ContainsWideChar_NarrowText_False(string s)
        => Assert.False(EastAsianWidth.ContainsWideChar(s));

    [Theory]
    [InlineData("請求書キュー")]          // kanji + katakana
    [InlineData("Folder_請求")]           // mixed ASCII + kanji
    [InlineData("ＦＵＬＬＷＩＤＴＨ")]    // fullwidth Latin
    [InlineData("진행")]                  // Hangul syllables
    [InlineData("\u1100")]                // Hangul Jamo leading consonant (BMP Wide)
    [InlineData("Build\u26A1")]           // BMP emoji with Wide presentation
    [InlineData("\u3000")]                // ideographic space (Fullwidth)
    public void ContainsWideChar_WideText_True(string s)
        => Assert.True(EastAsianWidth.ContainsWideChar(s));

    [Theory]
    [InlineData("Invoice", "Invoice")]                  // all narrow -> unchanged
    [InlineData("請求書", "[3]")]                         // all wide -> [count]
    [InlineData("請求Invoice", "[2]Invoice")]            // leading wide run
    [InlineData("Invoice請求", "Invoice[2]")]            // trailing wide run
    [InlineData("Invoice請求Folder", "Invoice[2]Folder")] // interior wide run keeps both sides
    [InlineData("A請B求C", "A[1]B[1]C")]                 // multiple wide runs, each counted
    [InlineData("Folder 請求", "Folder [2]")]            // narrow space before the run is kept
    [InlineData("队列\U0001F600x", "[2]\U0001F600x")]   // supplementary emoji ends the run, kept
    [InlineData("\U00020BB7野家", "\U00020BB7[2]")]     // U+20BB7 kept, BMP kanji collapsed
    [InlineData("項目※注意", "[5]")]                    // Ambiguous ※ is unsafe too -> one run
    [InlineData("A→B", "A[1]B")]                        // Ambiguous arrow (conhost + CJK font: 2 cells)
    public void CollapseWide_ReplacesEachWideRunWithCount(string input, string expected)
        => Assert.Equal(expected, EastAsianWidth.CollapseWide(input));

    // A host with the #21293 fix counts BMP W/F correctly, but its LengthInBufferCells table
    // still miscounts Ambiguous symbols and post-Unicode-5 W emoji, so only those collapse.
    [Theory]
    [InlineData("請求書キュー", "請求書キュー")]          // BMP W/F: the fixed host counts them
    [InlineData("\U00020BB7野家", "\U00020BB7野家")]     // surrogate pair: always fine
    [InlineData("項目※注意", "項目[1]注意")]             // Ambiguous ※: still miscounted
    [InlineData("Build⭐ 25℃", "Build[1] 25[1]")] // ⭐ (W, not in host table), ℃ (Ambiguous)
    public void CollapseWide_FixedHost_CollapsesOnlyHostMiscounted(string input, string expected)
        => Assert.Equal(expected, EastAsianWidth.CollapseWide(input, hostCountsWide: true));

    [Fact]
    public void WriteProgress_BuggyHost_LeadingWideName_ShowsWideRunCount()
    {
        var host = new CapturingHost(rendersWideProgress: false);
        using var reporter = new ProgressReporter(host, 10, "Copy");

        reporter.WriteProgress(3, "請求書キュー");

        // Fully wide (6 chars) -> "[6]" (still signals a hidden name, vs. passing none).
        Assert.Equal("3/10 [6]", host.Last!.StatusDescription);
    }

    [Fact]
    public void WriteProgress_BuggyHost_NoNamePassed_NoEllipsis()
    {
        var host = new CapturingHost(rendersWideProgress: false);
        using var reporter = new ProgressReporter(host, 10, "Copy");

        reporter.WriteProgress(3);

        // No name at all -> just index/total, no marker (distinct from a hidden wide name).
        Assert.Equal("3/10", host.Last!.StatusDescription);
    }

    [Fact]
    public void WriteProgress_BuggyHost_InteriorWideRun_KeepsBothAsciiSides()
    {
        var host = new CapturingHost(rendersWideProgress: false);
        using var reporter = new ProgressReporter(host, 10, "Copy");

        reporter.WriteProgress(3, "Invoice請求Folder");

        Assert.Equal("3/10 Invoice[2]Folder", host.Last!.StatusDescription);
    }

    [Fact]
    public void WriteProgress_BuggyHost_KeepsNarrowName()
    {
        var host = new CapturingHost(rendersWideProgress: false);
        using var reporter = new ProgressReporter(host, 10, "Copy");

        reporter.WriteProgress(3, "Invoice");

        Assert.Equal("3/10 Invoice", host.Last!.StatusDescription);
    }

    [Fact]
    public void Constructor_Activity_IsSanitizedOnBuggyHost()
    {
        var host = new CapturingHost(rendersWideProgress: false);
        using var reporter = new ProgressReporter(host, 10, "給与 assets");

        reporter.WriteProgress(1, "A");

        // The label is fixed at construction, so that is where it has to be collapsed.
        Assert.Equal("[2] assets", host.Last!.Activity);
    }

    [Fact]
    public void Context_IsSanitizedOnBuggyHost_AndSitsBetweenCountAndName()
    {
        var host = new CapturingHost(rendersWideProgress: false);
        using var reporter = new ProgressReporter(host, 10, "Assets");
        reporter.Context = "Orch2:\\営業";

        reporter.WriteProgress(3, "Invoice");

        // A Japanese destination folder must be collapsed in Context as well, otherwise the
        // bug just moves from the item name to the destination.
        Assert.Equal("3/10 Orch2:\\[2] Invoice", host.Last!.StatusDescription);
        Assert.Equal("Assets", host.Last!.Activity);
    }

    [Fact]
    public void WriteProgress_FixedHost_KeepsWideTextInEveryField()
    {
        var host = new CapturingHost(rendersWideProgress: true);
        using var reporter = new ProgressReporter(host, 10, "給与");
        reporter.Context = "Orch2:\\営業";

        reporter.WriteProgress(3, "請求書キュー");

        Assert.Equal("3/10 Orch2:\\営業 請求書キュー", host.Last!.StatusDescription);
        Assert.Equal("給与", host.Last!.Activity);
    }

    [Fact]
    public void WriteProgress_FixedHost_StillCollapsesAmbiguousSymbols()
    {
        var host = new CapturingHost(rendersWideProgress: true);
        using var reporter = new ProgressReporter(host, 10, "※給与");

        reporter.WriteProgress(3, "請求→承認");

        Assert.Equal("3/10 請求[1]承認", host.Last!.StatusDescription);
        Assert.Equal("[1]給与", host.Last!.Activity);
    }

    // --- shape of the bar: total, ids, nesting, completion -------------------------------

    [Fact]
    public void NoTotal_ShowsCountAlone_AndNoPercentage()
    {
        var host = new CapturingHost();
        using var reporter = new ProgressReporter(host, null, "Queues");

        reporter.WriteProgress(5, "q");

        Assert.Equal("5 q", host.Last!.StatusDescription);
        Assert.Equal(-1, host.Last!.PercentComplete);
    }

    [Fact]
    public void Percentage_IsClampedWhenTheCountOutgrowsTheTotal()
    {
        var host = new CapturingHost();
        using var reporter = new ProgressReporter(host, 4, "Queues");

        reporter.WriteProgress(9);

        Assert.Equal(100, host.Last!.PercentComplete);
    }

    [Fact]
    public void Ids_AreAllocated_AndAChildNamesItsParent()
    {
        var host = new CapturingHost();
        using var parent = new ProgressReporter(host, 2, "Folders");
        using var child = new ProgressReporter(host, null, "Queues", parent);
        using var loose = new ProgressReporter(host, null, "Assets");

        Assert.NotEqual(parent.Id, child.Id);
        Assert.NotEqual(child.Id, loose.Id);

        child.WriteProgress(1);
        Assert.Equal(parent.Id, host.Last!.ParentActivityId);

        loose.WriteProgress(1);
        Assert.Equal(-1, host.Last!.ParentActivityId);
    }

    [Fact]
    public void BarNeverWritten_IsNeverCompleted_NorConjuredByWriteCompleted()
    {
        var host = new CapturingHost();
        using (var reporter = new ProgressReporter(host, null, "Queues"))
        {
            reporter.WriteCompleted();
        }

        Assert.Empty(host.All);
    }

    [Fact]
    public void WriteCompleted_KnownTotal_FillsTheBar()
    {
        var host = new CapturingHost();
        using var reporter = new ProgressReporter(host, 3, "Queues");

        reporter.WriteProgress(2, "q2");
        reporter.WriteCompleted();

        Assert.Equal("3/3 COMPLETED", host.Last!.StatusDescription);
        Assert.Equal(100, host.Last!.PercentComplete);
    }

    [Fact]
    public void WriteCompleted_NoTotal_KeepsTheLastCount()
    {
        var host = new CapturingHost();
        using var reporter = new ProgressReporter(host, null, "Queues");

        reporter.WriteProgress(12, "q12");
        reporter.WriteCompleted();

        Assert.Equal("12 COMPLETED", host.Last!.StatusDescription);
    }

    [Fact]
    public void Dispose_AfterAWrite_SendsOneCompletedRecord()
    {
        var host = new CapturingHost();
        using (var reporter = new ProgressReporter(host, 2, "Queues"))
        {
            reporter.WriteProgress(1);
        }

        Assert.Single(host.All, r => r.RecordType == ProgressRecordType.Completed);
    }

    [Theory]
    [InlineData(7, 6, 9, false)]
    [InlineData(7, 4, 22, false)]
    [InlineData(7, 7, 0, true)]
    [InlineData(7, 8, 1, true)]
    public void HostRendersWideProgress_FixedFrom770(int major, int minor, int build, bool expected)
    {
        // The #26185 fix first ships in 7.7.0 (not backported to 7.4 / 7.5 / 7.6).
        Assert.Equal(expected, ProgressRendering.HostRendersWideProgress(new Version(major, minor, build)));
    }

    [Fact]
    public void HostRendersWideProgress_NullVersion_False()
        => Assert.False(ProgressRendering.HostRendersWideProgress(null));
}
