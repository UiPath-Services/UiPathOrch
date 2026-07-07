using System.Collections.Generic;
using System.Linq;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Pins OrchProvider.SelectChildItems — the depth-filter + parent-grouped ordering behind
// dir / dir -Recurse. The grouping (stable sort on parent path only) is what keeps each
// "Directory:" section contiguous under -Recurse instead of interleaving grandchildren
// between siblings. Pure over the catalog; no live drive.
public class SelectChildItemsTests
{
    private static Folder F(string fqn) => new() { FullyQualifiedName = fqn, DisplayName = fqn };
    private static Folder PW(string fqn) => new() { FullyQualifiedName = fqn, DisplayName = fqn, FolderType = "Personal" };
    private static List<string> Fqns(IEnumerable<Folder> fs) => fs.Select(f => f.FullyQualifiedName!).ToList();

    // Deliberately scrambled input order so the re-grouping is observable (not a pass-through).
    private static List<Folder> Scrambled() =>
    [
        F("Shared/A/Deep"),
        F("Apple"),
        F("Shared/A"),
        F("Apple/X"),
        F("Shared"),
        F("Shared/B"),
    ];

    [Fact]
    public void Root_Depth0_ReturnsDepth1FoldersOnly()
    {
        var got = OrchProvider.SelectChildItems(Scrambled(), orchPath: "", depth: 0);
        Assert.Equal(new[] { "Apple", "Shared" }, Fqns(got));
    }

    [Fact]
    public void Subpath_Depth0_ReturnsDirectChildrenOnly()
    {
        var got = OrchProvider.SelectChildItems(Scrambled(), orchPath: "Shared", depth: 0);
        Assert.Equal(new[] { "Shared/A", "Shared/B" }, Fqns(got)); // Shared/A/Deep is a grandchild — excluded
    }

    [Fact]
    public void Recurse_FromRoot_RegroupsChildrenContiguouslyUnderEachParent()
    {
        var got = OrchProvider.SelectChildItems(Scrambled(), orchPath: "", depth: 3);
        // Grouped by parent (stable): root group, then Apple's children, then Shared's, then Shared/A's.
        Assert.Equal(
            new[] { "Apple", "Shared", "Apple/X", "Shared/A", "Shared/B", "Shared/A/Deep" },
            Fqns(got));
    }

    [Fact]
    public void PrefixFilter_DoesNotLeakAcrossSiblingNameOverlap()
    {
        // "SharedX" must not be treated as inside "Shared" even though it shares the prefix.
        var catalog = new List<Folder> { F("Shared"), F("Shared/A"), F("SharedX"), F("SharedX/Y") };
        var got = OrchProvider.SelectChildItems(catalog, orchPath: "Shared", depth: 5);
        Assert.Equal(new[] { "Shared/A" }, Fqns(got));
    }

    [Fact]
    public void Recurse_PersonalWorkspaceSubtree_FollowsRootLevel_BeforeOtherNestedFolders()
    {
        // "yworkspace" is a personal workspace (FolderType Personal); its Solution subfolders must
        // group right after the root-level listing, ahead of regular folders' subtrees — matching
        // the non-recurse view / web UI, which list personal workspaces first. (Grafted PW
        // subfolders are NOT FolderType Personal — they are recognized by their FQN prefix.)
        var catalog = new List<Folder>
        {
            PW("yworkspace"),          // PW root (catalog puts it first)
            F("Apple"),                // regular root-level
            F("Apple/X"),              // regular nested
            F("yworkspace/Sol"),       // PW subfolder
            F("yworkspace/Sol/Deep"),  // deeper PW subfolder
            F("Zebra"),                // regular root-level (sorts after "yworkspace")
            F("Zebra/Y"),
        };
        var got = OrchProvider.SelectChildItems(catalog, orchPath: "", depth: 5);
        Assert.Equal(
            new[]
            {
                "yworkspace", "Apple", "Zebra",           // rank 0: root-level, catalog order (PW first)
                "yworkspace/Sol", "yworkspace/Sol/Deep",  // rank 1: PW subtree, right after root
                "Apple/X", "Zebra/Y",                     // rank 2: other nested folders
            },
            Fqns(got));
    }

    [Fact]
    public void Depth1_FromRoot_IncludesChildrenAndGrandchildren_NotGreatGrandchildren()
    {
        var catalog = new List<Folder> { F("A"), F("A/B"), F("A/B/C"), F("A/B/C/D") };
        var got = OrchProvider.SelectChildItems(catalog, orchPath: "", depth: 1);
        Assert.Equal(new[] { "A", "A/B" }, Fqns(got)); // depth 0 (A) + 1 extra level (A/B)
    }
}
