using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Unit tests for OrchDriveInfo.UnionFolderUsers — the merge behind GetFolderUsersUnion.
// Rationale: in one customer AC environment GetUsersForFolder's includeInherited=true
// form omitted a directly-assigned robot account that the false form returned, so every
// consumer that read the true form alone (asset-copy gating, Set-Orch*Asset -UserName,
// completers, Get-OrchFolderUser -IncludeInherited) silently lost that assignment.
// The union must therefore keep entries that appear in EITHER view, deduplicated by
// UserEntity.Id, preferring the inherited-view entry when both have one.
public class FolderUsersUnionTests
{
    private static UserRoles UR(long? id, string userName) =>
        new() { UserEntity = new UserEntity { Id = id, UserName = userName } };

    [Fact]
    public void DirectOnlyEntry_SurvivesTheUnion()
    {
        // The customer-observed shape: robot missing from the inherited view,
        // present in the direct view — must not be lost.
        var inherited = new[] { UR(1, "human@x.com") };
        var direct = new[] { UR(2, "robot01") };

        var union = OrchDriveInfo.UnionFolderUsers(inherited, direct);

        Assert.Equal(2, union.Count);
        Assert.Contains(union, ur => ur.UserEntity!.UserName == "robot01");
    }

    [Fact]
    public void DuplicateIds_KeptOnce_InheritedViewWins()
    {
        var inherited = new[] { UR(1, "from-inherited-view") };
        var direct = new[] { UR(1, "from-direct-view") };

        var union = OrchDriveInfo.UnionFolderUsers(inherited, direct);

        var only = Assert.Single(union);
        Assert.Equal("from-inherited-view", only.UserEntity!.UserName);
    }

    [Fact]
    public void NullEntriesAndNullIds_AreSkipped()
    {
        var inherited = new UserRoles?[] { null, UR(null, "no-id"), UR(1, "a") };
        var direct = new UserRoles?[] { new UserRoles { UserEntity = null }, UR(2, "b") };

        var union = OrchDriveInfo.UnionFolderUsers(inherited, direct);

        Assert.Equal(2, union.Count);
        Assert.DoesNotContain(union, ur => ur.UserEntity?.UserName == "no-id");
    }

    [Fact]
    public void BothViewsEmpty_YieldsEmpty()
    {
        Assert.Empty(OrchDriveInfo.UnionFolderUsers([], []));
    }
}
