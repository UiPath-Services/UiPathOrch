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
        public bool RendersWideProgress { get; } = rendersWideProgress;
        public void WriteProgress(ProgressRecord progressRecord) => Last = progressRecord;
        public void WriteWarning(string text) { }
        public void WriteError(ErrorRecord errorRecord) { }
        public bool ShouldProcess(string target, string action) => true;
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
    [InlineData("Invoice", "Invoice")]                 // all narrow -> unchanged
    [InlineData("請求書", "...")]                       // all wide -> single "..."
    [InlineData("請求Invoice", "...Invoice")]           // leading wide run
    [InlineData("Invoice請求", "Invoice...")]           // trailing wide run
    [InlineData("Invoice請求Folder", "Invoice...Folder")] // interior wide run keeps both sides
    [InlineData("A請B求C", "A...B...C")]                // multiple wide runs
    [InlineData("Folder 請求", "Folder ...")]           // narrow space before the run is kept
    [InlineData("队列\U0001F600x", "...x")]             // surrogate-pair emoji collapses too
    public void CollapseWide_ReplacesEachWideRunWithEllipsis(string input, string expected)
        => Assert.Equal(expected, EastAsianWidth.CollapseWide(input));

    [Fact]
    public void WriteProgress_BuggyHost_LeadingWideName_ShowsBareEllipsis()
    {
        var host = new CapturingHost(rendersWideProgress: false);
        using var reporter = new ProgressReporter(host, id: 1, totalNum: 10, activity: "Copy");

        reporter.WriteProgress(3, "請求書キュー");

        // Fully wide -> single "..." (still signals a hidden name, vs. passing none).
        Assert.Equal("3/10 ...", host.Last!.StatusDescription);
    }

    [Fact]
    public void WriteProgress_BuggyHost_NoNamePassed_NoEllipsis()
    {
        var host = new CapturingHost(rendersWideProgress: false);
        using var reporter = new ProgressReporter(host, id: 1, totalNum: 10, activity: "Copy");

        reporter.WriteProgress(3);

        // No name at all -> just index/total, no "..." (distinct from a hidden wide name).
        Assert.Equal("3/10", host.Last!.StatusDescription);
    }

    [Fact]
    public void WriteProgress_BuggyHost_InteriorWideRun_KeepsBothAsciiSides()
    {
        var host = new CapturingHost(rendersWideProgress: false);
        using var reporter = new ProgressReporter(host, id: 1, totalNum: 10, activity: "Copy");

        reporter.WriteProgress(3, "Invoice請求Folder");

        Assert.Equal("3/10 Invoice...Folder", host.Last!.StatusDescription);
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

        Assert.Equal("Copying assets to Orch2:\\...", host.Last!.Activity);
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

        Assert.Equal("Copying assets to Orch2:\\...", host.Last!.Activity);
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
