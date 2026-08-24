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
    [InlineData("A/B/C", 3, "")]     // the tenant root, whose FQN is ""
    [InlineData("A/B/C", 4, null)]   // can't go above the root
    [InlineData("A", 0, "A")]
    [InlineData("A", 1, "")]         // a top-level folder's parent IS the root
    [InlineData("", 1, null)]
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

    // The tenant root is a common ancestor like any other, so two subtrees that meet only
    // at the root rebase to the same absolute path on the destination. (Before the top-level
    // fix this returned null; a same-drive copy that resolves back to the source's own link
    // folder is still refused, by FindDstFolders' alias guard — see the test below.)
    [Fact]
    public void DisjointTopLevel_RebasesUnderTheTenantRoot()
        => Assert.Equal("Other/X", ComputeDstFqn("Other/X", "Sales/Q1", "Marketing/West"));

    // ---- top-level (root-level) folders: the shape that silently lost every link ----
    // Their FQNs carry no "/", so the only boundary between them is the tenant root. The
    // scan for a "/" boundary found none and the whole rebase was abandoned, so an entity
    // shared between two root-level folders — the most common Orchestrator layout — was
    // copied twice instead of linked, in either direction and with no error or warning.
    // Reproduced live cloud-to-cloud (Shared <-> Development) before the fix.

    [Fact]
    public void TopLevelSiblings_SameNamesOnBothSides()
        => Assert.Equal("Development", ComputeDstFqn("Development", "Shared", "Shared"));

    [Fact]
    public void TopLevelSiblings_ReverseDirection()
        => Assert.Equal("Shared", ComputeDstFqn("Shared", "Development", "Development"));

    [Fact]
    public void TopLevelSibling_ReparentedDestination()
        => Assert.Equal("Migration/Finance", ComputeDstFqn("Finance", "Shared", "Migration/Shared"));

    [Fact]
    public void TopLevelSibling_LinkIsASubfolderOfAnotherTopLevelFolder()
        => Assert.Equal("Dept#2/fuga", ComputeDstFqn("Dept#2/fuga", "Shared", "Shared"));

    // The rebase lands on the tenant root itself: no folder has an empty FQN, so
    // FindDstFolders matches nothing and the entity is copied rather than linked.
    [Fact]
    public void AncestorAboveATopLevelDstAnchor_ResolvesToTheRoot()
        => Assert.Equal("", ComputeDstFqn("Sales", "Sales/Q1", "Marketing"));

    [Fact]
    public void DstAnchorIsTheRoot_HasNoExpressibleCounterpart()
        => Assert.Null(ComputeDstFqn("Finance", "Shared", ""));

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

    // End-to-end shape of the live repro: TestAsset2024 lives in Shared and is shared with
    // Development (both top-level); copying Shared -> Shared must resolve Development on the
    // destination so the second folder's copy links instead of creating a second asset.
    [Fact]
    public void FindDstFolders_RebasesTopLevelSiblingLink()
    {
        var got = FindDstFolders(
            [10],
            [F(10, "Development"), F(1, "Shared")],
            [F(20, "Development"), F(2, "Shared")],
            F(1, "Shared"),
            F(2, "Shared"))!.ToList();
        Assert.Single(got);
        Assert.Equal(20, got[0].Id);
    }

    // A rebase that lands on the tenant root matches no folder (none has an empty FQN).
    [Fact]
    public void FindDstFolders_RootResult_MatchesNothing()
    {
        var got = FindDstFolders(
            [10],
            [F(10, "Sales"), F(1, "Sales/Q1")],
            [F(20, "Marketing/West"), F(2, "Marketing")],
            F(1, "Sales/Q1"),
            F(2, "Marketing"))!.ToList();
        Assert.Empty(got);
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
