using System.Management.Automation;
using UiPath.PowerShell.Core;
using Xunit;
using static UiPath.PowerShell.Core.OrchProvider;

namespace UnitTests;

// Unit tests for DropWarningBudget — the throttle that caps the per-value
// "owner not assigned, value dropped" warnings emitted while copying an asset's
// per-user values, then points once at the bulk Copy-OrchFolderUser * fix and
// suppresses the rest so a folder of assets referencing many unmapped owners
// doesn't flood the warning stream.
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

        budget.Warn("drop-1");
        budget.Warn("drop-2");
        budget.Warn("drop-3");

        Assert.Equal(new[] { "drop-1", "drop-2", "drop-3" }, host.Warnings);
    }

    [Fact]
    public void ExactlyThreshold_NoSummary()
    {
        var host = new CapturingHost();
        var budget = new DropWarningBudget(host, "s", "d", threshold: 2);

        budget.Warn("a");
        budget.Warn("b");

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
            budget.Warn($"drop-{i}");
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

        budget.Warn("d1");
        budget.Warn("d2"); // crosses the threshold, emits the summary

        var summary = host.Warnings.Last();
        Assert.Contains("-Path 'Orch1:\\O''Brien'", summary);
        Assert.Contains("-Destination 'Orch1:\\D''Ept'", summary);
    }
}
