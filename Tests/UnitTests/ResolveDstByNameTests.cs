using Xunit;
using static UiPath.PowerShell.Core.OrchProvider;

namespace UnitTests;

// Unit tests for ResolveDstByName<T> -- the generic name-match helper used
// by the simple FindDst* family in CopyItem.cs (FindDstRobot,
// FindDstMachine, FindDstQueue, FindDstRelease, FindDstCalendar,
// FindDstCredentialStore, FindDstBucket).
//
// The policy pinned down here:
//   - null / empty candidates -> null
//   - null / empty srcName    -> null (don't accidentally match the first
//                                       candidate with a null name)
//   - null entries in candidates are tolerated (filtered out)
//   - default StringComparison is OrdinalIgnoreCase (matches the bulk of
//     the FindDst* family). Callers can pass Ordinal explicitly
//     (FindDstBucket does, preserving its original case-sensitive
//     behaviour pending deliberate review -- see BugDiscovery test).
public class ResolveDstByNameTests
{
    private sealed record Item(string? Name);

    [Fact]
    public void MatchesByExactName_CaseInsensitiveByDefault()
    {
        var candidates = new[] { new Item("Alice"), new Item("Bob") };
        var got = ResolveDstByName(candidates, "alice", c => c.Name);
        Assert.NotNull(got);
        Assert.Equal("Alice", got!.Name);
    }

    [Fact]
    public void DoesNotMatchWhenCaseSensitiveAndCaseDiffers()
    {
        var candidates = new[] { new Item("Alice") };
        var got = ResolveDstByName(candidates, "alice", c => c.Name, StringComparison.Ordinal);
        Assert.Null(got);
    }

    [Fact]
    public void MatchesWhenCaseSensitiveAndCaseMatches()
    {
        var candidates = new[] { new Item("Alice") };
        var got = ResolveDstByName(candidates, "Alice", c => c.Name, StringComparison.Ordinal);
        Assert.NotNull(got);
    }

    [Fact]
    public void ReturnsNullWhenSrcNameIsNull()
    {
        var candidates = new[] { new Item(null), new Item("Alice") };
        var got = ResolveDstByName(candidates, null, c => c.Name);
        Assert.Null(got);
    }

    [Fact]
    public void ReturnsNullWhenSrcNameIsEmpty()
    {
        var candidates = new[] { new Item(""), new Item("Alice") };
        var got = ResolveDstByName(candidates, "", c => c.Name);
        Assert.Null(got);
    }

    [Fact]
    public void ReturnsNullWhenCandidatesIsNull()
    {
        var got = ResolveDstByName<Item>(null, "Alice", c => c.Name);
        Assert.Null(got);
    }

    [Fact]
    public void ReturnsNullWhenCandidatesIsEmpty()
    {
        var got = ResolveDstByName(Array.Empty<Item>(), "Alice", c => c.Name);
        Assert.Null(got);
    }

    [Fact]
    public void TolerantOfNullEntriesInCandidates()
    {
        var candidates = new Item?[] { null, new Item("Alice"), null };
        var got = ResolveDstByName(candidates!, "Alice", c => c!.Name);
        Assert.NotNull(got);
        Assert.Equal("Alice", got!.Name);
    }

    [Fact]
    public void FirstMatchWinsWhenMultipleCandidatesShareName()
    {
        // Domain entities should have unique names in practice; pin down
        // FirstOrDefault semantics so any policy change is visible.
        var first = new Item("Alice");
        var second = new Item("Alice");
        var candidates = new[] { first, second };
        var got = ResolveDstByName(candidates, "Alice", c => c.Name);
        Assert.Same(first, got);
    }

    [Fact]
    public void BugDiscovery_FindDstBucketIsCaseSensitiveWhileOthersAreNot()
    {
        // Documents an inconsistency that the refactor preserved verbatim:
        // FindDstBucket uses StringComparison.Ordinal (case-sensitive)
        // while every other FindDst* uses OrdinalIgnoreCase. The original
        // FindDstBucket implementation used '==' for name comparison,
        // which is case-sensitive in C#.
        //
        // If this is intentional (bucket names ARE case-sensitive in the
        // Orchestrator API), the test below documents the policy. If not,
        // FindDstBucket needs to switch to OrdinalIgnoreCase, the bug
        // would be 'Copy-Item on a bucket whose name only differs in case
        // from a destination bucket gets silently treated as a different
        // entity'. Awaiting deliberate decision; do not "fix" by changing
        // FindDstBucket without first confirming server semantics.

        var candidates = new[] { new Item("MyBucket") };
        var caseInsensitive = ResolveDstByName(candidates, "mybucket", c => c.Name);
        var caseSensitive   = ResolveDstByName(candidates, "mybucket", c => c.Name, StringComparison.Ordinal);
        Assert.NotNull(caseInsensitive);  // FindDstQueue / FindDstRelease / etc. mode
        Assert.Null(caseSensitive);       // FindDstBucket mode
    }
}
