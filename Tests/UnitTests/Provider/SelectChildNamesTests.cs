using System.Collections.Generic;
using System.Linq;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Pins OrchProvider.SelectChildNames — backs Get-ChildItem -Name and wildcard resolution
// (cd t*, rmdir *). Root lists depth-1 folders; non-root lists folders whose ParentId matches
// the resolved parent. GetFolders() masks top-level ParentId to null, so the root branch must
// key off depth, not ParentId. Source order (web-UI order) must be preserved.
public class SelectChildNamesTests
{
    private static Folder F(string fqn, long id, long? parentId) => new()
    {
        FullyQualifiedName = fqn,
        DisplayName = fqn.Contains('/') ? fqn[(fqn.LastIndexOf('/') + 1)..] : fqn,
        Id = id,
        ParentId = parentId,
    };

    //  Shared(1)        Other(2)
    //   |                |
    //  Shared/A(11)     Other/X(21)
    //   |
    //  Shared/A/Deep(111)
    private static readonly List<Folder> Catalog =
    [
        F("Shared", 1, null),
        F("Other", 2, null),
        F("Shared/A", 11, 1),
        F("Other/X", 21, 2),
        F("Shared/A/Deep", 111, 11),
    ];

    private static List<string> Fqns(IEnumerable<Folder> fs) => fs.Select(f => f.FullyQualifiedName!).ToList();

    [Fact]
    public void Root_ReturnsDepth1Folders_InSourceOrder()
    {
        // parentFolderId is ignored at the root.
        var got = OrchProvider.SelectChildNames(Catalog, ocPath: "", parentFolderId: 0);
        Assert.Equal(new[] { "Shared", "Other" }, Fqns(got));
    }

    [Fact]
    public void NonRoot_ReturnsFoldersWithMatchingParentId()
    {
        var got = OrchProvider.SelectChildNames(Catalog, ocPath: "Shared", parentFolderId: 1);
        Assert.Equal(new[] { "Shared/A" }, Fqns(got));
    }

    [Fact]
    public void NonRoot_GrandchildLevel_ReturnsOnlyDirectChild()
    {
        var got = OrchProvider.SelectChildNames(Catalog, ocPath: "Shared/A", parentFolderId: 11);
        Assert.Equal(new[] { "Shared/A/Deep" }, Fqns(got));
    }

    [Fact]
    public void NonRoot_LeafFolder_ReturnsNothing()
    {
        var got = OrchProvider.SelectChildNames(Catalog, ocPath: "Other/X", parentFolderId: 21);
        Assert.Empty(got);
    }
}
