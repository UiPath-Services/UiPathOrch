using System.Management.Automation;
using UiPath.PowerShell.Core;
using Xunit;
using static UiPath.PowerShell.Core.OrchProvider;

namespace UnitTests;

// Unit tests for DropWarningBudget — the throttle that caps the per-value
// "owner not assigned, value dropped" warnings emitted while copying an asset's
// per-user values, then points once at the bulk Copy-OrchFolderUser * fix and
// suppresses the rest so a folder of assets referencing many unmapped owners
// doesn't flood the warning stream. WriteSummary is the end-of-copy counterpart:
// one warning listing the distinct dropped owners, even the throttled ones.
public class DropWarningBudgetTests
{
    private sealed class CapturingHost : IWritableHost
    {
        public List<string> Warnings { get; } = new();
        public void WriteWarning(string text) => Warnings.Add(text);
        public void WriteError(ErrorRecord errorRecord) { }
        public void WriteProgress(ProgressRecord progressRecord) { }
        public bool ShouldProcess(string target, string action) => true;
    }

    [Fact]
    public void UnderThreshold_AllWarningsForwardedVerbatim()
    {
        var host = new CapturingHost();
        var budget = new DropWarningBudget(host, "Orch1:\\Src", "Orch1:\\Dst", threshold: 3);

        budget.Warn("user 'u1'", "drop-1");
        budget.Warn("user 'u2'", "drop-2");
        budget.Warn("user 'u3'", "drop-3");

        Assert.Equal(new[] { "drop-1", "drop-2", "drop-3" }, host.Warnings);
    }

    [Fact]
    public void ExactlyThreshold_NoSummary()
    {
        var host = new CapturingHost();
        var budget = new DropWarningBudget(host, "s", "d", threshold: 2);

        budget.Warn("user 'a'", "a");
        budget.Warn("user 'b'", "b");

        Assert.Equal(2, host.Warnings.Count);
        Assert.DoesNotContain(host.Warnings, w => w.Contains("suppressed"));
    }

    [Fact]
    public void OverThreshold_EmitsSingleBulkSummaryThenSuppresses()
    {
        var host = new CapturingHost();
        var budget = new DropWarningBudget(host, "Orch1:\\Src", "Orch1:\\Dst", threshold: 3);

        for (int i = 1; i <= 7; i++)
        {
            budget.Warn($"user 'u{i}'", $"drop-{i}");
        }

        // First 3 detailed + exactly one summary; the remaining 4 are suppressed.
        Assert.Equal(4, host.Warnings.Count);
        Assert.Equal(new[] { "drop-1", "drop-2", "drop-3" }, host.Warnings.Take(3));

        var summary = host.Warnings[3];
        Assert.Contains("Copy-OrchFolderUser -Path 'Orch1:\\Src' * -Destination 'Orch1:\\Dst'", summary);
        Assert.Contains("Copy-OrchFolderMachine *", summary);
        Assert.DoesNotContain(host.Warnings, w => w.Contains("drop-4"));
    }

    [Fact]
    public void Summary_DoublesSingleQuotesInPaths_ForCopyPasteSafety()
    {
        var host = new CapturingHost();
        var budget = new DropWarningBudget(host, "Orch1:\\O'Brien", "Orch1:\\D'Ept", threshold: 1);

        budget.Warn("user 'a'", "d1");
        budget.Warn("user 'b'", "d2"); // crosses the threshold, emits the summary

        var summary = host.Warnings.Last();
        Assert.Contains("-Path 'Orch1:\\O''Brien'", summary);
        Assert.Contains("-Destination 'Orch1:\\D''Ept'", summary);
    }

    [Fact]
    public void WriteSummary_NoDrops_EmitsNothing()
    {
        var host = new CapturingHost();
        var budget = new DropWarningBudget(host, "s", "d", threshold: 3);

        budget.WriteSummary();

        Assert.Empty(host.Warnings);
    }

    [Fact]
    public void WriteSummary_ListsDistinctOwners_IncludingThrottledOnes()
    {
        var host = new CapturingHost();
        var budget = new DropWarningBudget(host, "Orch1:\\Src", "Orch1:\\Dst", threshold: 2);

        // 5 drops over 3 distinct owners; drops 3..5 are past the threshold, so
        // "machine 'm1'" never appears in a per-value warning — only the summary names it.
        budget.Warn("user 'alice'", "d1");
        budget.Warn("user 'bob'", "d2");
        budget.Warn("user 'alice'", "d3");
        budget.Warn("user 'ALICE'", "d4"); // same owner, different case — not a new entry
        budget.Warn("machine 'm1'", "d5");
        budget.WriteSummary();

        var summary = host.Warnings.Last();
        Assert.Contains("5 per-user value(s) were dropped", summary);
        Assert.Contains("user 'alice'", summary);
        Assert.Contains("user 'bob'", summary);
        Assert.Contains("machine 'm1'", summary);
        Assert.DoesNotContain("+", summary.Substring(summary.IndexOf("owner(s):")));
    }

    [Fact]
    public void WriteSummary_CollapsesOwnersBeyondLimitIntoMoreTail()
    {
        var host = new CapturingHost();
        var budget = new DropWarningBudget(host, "s", "d", threshold: 1);

        for (int i = 1; i <= 25; i++)
        {
            budget.Warn($"user 'u{i:00}'", $"d{i}");
        }
        budget.WriteSummary();

        var summary = host.Warnings.Last();
        Assert.Contains("user 'u01'", summary);
        Assert.Contains("user 'u20'", summary);
        Assert.DoesNotContain("user 'u21'", summary);
        Assert.Contains("(+5 more)", summary);
    }
}
