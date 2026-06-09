using System.Collections.Generic;
using System.Linq;
using UiPath.PowerShell.Entities;
using Xunit;
using static UiPath.PowerShell.Core.OrchProvider;

namespace UnitTests;

// Unit tests for the cross-tenant folder-rebasing logic behind Link* (LinkAsset/LinkQueue/
// LinkBucket -> FindDstFolders -> ComputeDstFqn -> WalkUp). This is the highest-value/highest-risk
// migration path and was previously CI-untested (live Pester only). ComputeDstFqn replaced an older
// FQN-equality match that was broken for same-drive copies (it shared the SOURCE entity into dst
// folders); these tests lock that fix, including the same-drive alias guard in FindDstFolders.
public class ComputeDstFqnTests
{
    // ---------------- WalkUp ----------------

    [Theory]
    [InlineData("A/B/C", 0, "A/B/C")]
    [InlineData("A/B/C", 1, "A/B")]
    [InlineData("A/B/C", 2, "A")]
    [InlineData("A/B/C", 3, null)]   // can't go above the top segment
    [InlineData("A", 0, "A")]
    [InlineData("A", 1, null)]
    public void WalkUp_StripsTrailingSegments(string fqn, int upSteps, string? expected)
        => Assert.Equal(expected, WalkUp(fqn, upSteps));

    // ---------------- ComputeDstFqn ----------------

    [Fact]
    public void Identical_MapsToDstAnchor()
        => Assert.Equal("Marketing/Q1", ComputeDstFqn("Sales/Q1", "Sales/Q1", "Marketing/Q1"));

    [Fact]
    public void Descendant_ReplacesAnchorPrefix()
        => Assert.Equal("Marketing/West/Leads", ComputeDstFqn("Sales/Q1/Leads", "Sales/Q1", "Marketing/West"));

    [Fact]
    public void Descendant_TopLevelAnchor()
        => Assert.Equal("Marketing/Q1", ComputeDstFqn("Sales/Q1", "Sales", "Marketing"));

    [Fact]
    public void Ancestor_WalksDstUp()
        => Assert.Equal("Marketing", ComputeDstFqn("Sales", "Sales/Q1", "Marketing/West"));

    [Fact]
    public void Sibling_RebasesUnderCommonPrefix()
        => Assert.Equal("Marketing/Q2", ComputeDstFqn("Sales/Q2", "Sales/Q1", "Marketing/West"));

    [Fact]
    public void Cousin_RebasesUnderCommonPrefix()
        => Assert.Equal("Marketing/East/Q2", ComputeDstFqn("Sales/East/Q2", "Sales/West/Q1", "Marketing/HQ/Team"));

    [Fact]
    public void DisjointTopLevel_ReturnsNull()
        => Assert.Null(ComputeDstFqn("Other/X", "Sales/Q1", "Marketing/West"));

    [Fact]
    public void CaseInsensitive_AnchorMatch_PreservesLinkTailCase()
        => Assert.Equal("Marketing/q1", ComputeDstFqn("sales/q1", "Sales", "Marketing"));

    // ---------------- FindDstFolders ----------------

    private static Folder F(long id, string fqn) => new() { Id = id, FullyQualifiedName = fqn };

    [Fact]
    public void FindDstFolders_NullIds_ReturnsNull()
        => Assert.Null(FindDstFolders(null, [], [], F(1, "A"), F(2, "B")));

    [Fact]
    public void FindDstFolders_NoSelectedSrc_ReturnsEmpty()
    {
        var got = FindDstFolders([999], [F(10, "Sales/Q1/Leads")], [F(20, "Marketing/West/Leads")],
            F(1, "Sales/Q1"), F(2, "Marketing/West"));
        Assert.NotNull(got);
        Assert.Empty(got!);
    }

    [Fact]
    public void FindDstFolders_RebasesDescendantLink()
    {
        var got = FindDstFolders(
            [10],
            [F(10, "Sales/Q1/Leads"), F(1, "Sales/Q1")],
            [F(20, "Marketing/West/Leads"), F(2, "Marketing/West")],
            F(1, "Sales/Q1"),
            F(2, "Marketing/West"))!.ToList();
        Assert.Single(got);
        Assert.Equal(20, got[0].Id);
    }

    [Fact]
    public void FindDstFolders_SameDriveAlias_IsRefused()
    {
        // Same folder pool (src == dst), identical anchors: the rebased FQN resolves back to the
        // src link folder itself. The guard must refuse it (the old equality match's foot-gun).
        var pool = new List<Folder> { F(10, "A/Link") };
        var got = FindDstFolders([10], pool, pool, F(1, "A"), F(1, "A"))!.ToList();
        Assert.Empty(got);
    }
}
