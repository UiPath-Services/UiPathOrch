using System.Management.Automation;
using UiPath.PowerShell.Core;
using Xunit;

namespace UnitTests;

// Unit tests for the Write-Progress width-safety layer that works around PowerShell
// #21293 (the console host miscounts East Asian Wide characters and wraps the bar's
// trailing ']'). EastAsianWidth.CollapseWide replaces each run of wide characters with an
// ASCII "..." (keeping the narrow segments); ProgressReporter applies it to both the status
// and the activity line unless the host reports it renders wide text correctly.
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
    [InlineData("")]
    public void ContainsWideChar_NarrowText_False(string s)
        => Assert.False(EastAsianWidth.ContainsWideChar(s));

    [Theory]
    [InlineData("請求書キュー")]          // kanji + katakana
    [InlineData("Folder_請求")]           // mixed ASCII + kanji
    [InlineData("ＦＵＬＬＷＩＤＴＨ")]    // fullwidth Latin
    [InlineData("項目※注意")]            // reference mark (curated ambiguous)
    [InlineData("A→B")]                   // arrow (curated ambiguous)
    [InlineData("진행")]                  // Hangul
    [InlineData("队列\U0001F600")]        // emoji (supplementary plane, surrogate pair)
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
    [InlineData("队列\U0001F600x", "[3]x")]              // 2 CJK + 1 surrogate-pair emoji = one run of 3
    public void CollapseWide_ReplacesEachWideRunWithCount(string input, string expected)
        => Assert.Equal(expected, EastAsianWidth.CollapseWide(input));

    [Fact]
    public void WriteProgress_BuggyHost_LeadingWideName_ShowsWideRunCount()
    {
        var host = new CapturingHost(rendersWideProgress: false);
        using var reporter = new ProgressReporter(host, id: 1, totalNum: 10, activity: "Copy");

        reporter.WriteProgress(3, "請求書キュー");

        // Fully wide (6 chars) -> "[6]" (still signals a hidden name, vs. passing none).
        Assert.Equal("3/10 [6]", host.Last!.StatusDescription);
    }

    [Fact]
    public void WriteProgress_BuggyHost_NoNamePassed_NoEllipsis()
    {
        var host = new CapturingHost(rendersWideProgress: false);
        using var reporter = new ProgressReporter(host, id: 1, totalNum: 10, activity: "Copy");

        reporter.WriteProgress(3);

        // No name at all -> just index/total, no marker (distinct from a hidden wide name).
        Assert.Equal("3/10", host.Last!.StatusDescription);
    }

    [Fact]
    public void WriteProgress_BuggyHost_InteriorWideRun_KeepsBothAsciiSides()
    {
        var host = new CapturingHost(rendersWideProgress: false);
        using var reporter = new ProgressReporter(host, id: 1, totalNum: 10, activity: "Copy");

        reporter.WriteProgress(3, "Invoice請求Folder");

        Assert.Equal("3/10 Invoice[2]Folder", host.Last!.StatusDescription);
    }

    [Fact]
    public void WriteProgress_BuggyHost_KeepsNarrowName()
    {
        var host = new CapturingHost(rendersWideProgress: false);
        using var reporter = new ProgressReporter(host, id: 1, totalNum: 10, activity: "Copy");

        reporter.WriteProgress(3, "Invoice");

        Assert.Equal("3/10 Invoice", host.Last!.StatusDescription);
    }

    [Fact]
    public void WriteProgress_BuggyHost_SanitizesActivityLineToo()
    {
        var host = new CapturingHost(rendersWideProgress: false);
        using var reporter = new ProgressReporter(host, id: 1, totalNum: 10, activity: "Copy");

        // A Japanese destination folder name in the activity line must be collapsed as well,
        // otherwise the bug just moves from the status line to the activity line.
        reporter.WriteProgress(3, "Invoice", "Copying assets to Orch2:\\営業");

        Assert.Equal("Copying assets to Orch2:\\[2]", host.Last!.Activity);
        Assert.Equal("3/10 Invoice", host.Last!.StatusDescription);
    }

    [Fact]
    public void Activity_Setter_IsSanitizedOnBuggyHost()
    {
        var host = new CapturingHost(rendersWideProgress: false);
        using var reporter = new ProgressReporter(host, id: 1, totalNum: 10, activity: "Copy")
        {
            Activity = "Copying assets to Orch2:\\給与"
        };

        reporter.WriteProgress(1, "A");

        Assert.Equal("Copying assets to Orch2:\\[2]", host.Last!.Activity);
    }

    [Fact]
    public void WriteProgress_FixedHost_KeepsWideTextInBothFields()
    {
        var host = new CapturingHost(rendersWideProgress: true);
        using var reporter = new ProgressReporter(host, id: 1, totalNum: 10, activity: "Copy");

        reporter.WriteProgress(3, "請求書キュー", "Copying assets to Orch2:\\営業");

        Assert.Equal("3/10 請求書キュー", host.Last!.StatusDescription);
        Assert.Equal("Copying assets to Orch2:\\営業", host.Last!.Activity);
    }

    [Fact]
    public void HostRendersWideProgress_NoKnownFixedVersion_AlwaysFalse()
    {
        // The #26185 fix version is still unset (PR open), so every host is treated as buggy.
        Assert.Null(ProgressRendering.Pwsh26185FixedVersion);
        Assert.False(ProgressRendering.HostRendersWideProgress(new Version(99, 0, 0)));
    }
}
