using UiPath.PowerShell.Entities;
using Xunit;
using static UiPath.PowerShell.Core.OrchProvider;

namespace UnitTests;

// Reproduction + policy tests for ResolveDstDirectoryUserPure -- the
// destination-directory resolution extracted from CopyItem.cs's CopyFolderUsers.
//
// Why these exist: dstDrive.SearchDirectory(name) calls
//   /api/DirectoryService/SearchForUsersAndGroups?prefix=<name>
// which is a PREFIX search. The original CopyFolderUsers primary path filtered
// the result by `type` ONLY -- it never narrowed back down to an exact
// identityName == name match (unlike the email-fallback branch right below it,
// and unlike TestUserMappingCsv.cs, both of which DO filter on identityName).
//
// Consequences of the missing exact filter (the bug these tests pin):
//   * A lone prefix sibling (search "john", directory has only "johnathan")
//     resolves to the WRONG user -> the wrong directory principal silently gets
//     the folder role assignment.
//   * Several prefix siblings (search "john", directory has "john" + "john.doe")
//     yield Count > 1 -> a spurious "Duplicated" error skips a user whose exact
//     match was actually unambiguous.
//
// PrefixOnlySibling_MustNotResolveToWrongUser and
// ExactMatchAmongPrefixSiblings_ResolvesNotDuplicated FAIL against the original
// type-only filter and pass once the exact identityName filter is restored.
public class ResolveDstDirectoryUserPureTests
{
    // DirectoryObject.type: 0=User, 1=Group, 2=Machine, 3=Robot, 4=ExternalApp.
    private const int User = 0;
    private const int Group = 1;

    private static DirectoryObject D(string identityName, int type = User) =>
        new() { identityName = identityName, type = type, identifier = $"dir|{identityName}" };

    // ---- baseline behaviour (green before and after the fix) ----

    [Fact]
    public void ExactSingleMatch_Resolves()
    {
        var results = new[] { D("alice") };
        var (resolved, result) = ResolveDstDirectoryUserPure(results, "alice", User);
        Assert.Equal(FindDstDirectoryUserResult.Resolved, result);
        Assert.Equal("alice", resolved!.identityName);
    }

    [Fact]
    public void NoResults_NotFound()
    {
        var (resolved, result) = ResolveDstDirectoryUserPure(Array.Empty<DirectoryObject>(), "alice", User);
        Assert.Equal(FindDstDirectoryUserResult.NotFound, result);
        Assert.Null(resolved);
    }

    [Fact]
    public void NullResults_NotFound()
    {
        var (resolved, result) = ResolveDstDirectoryUserPure(null, "alice", User);
        Assert.Equal(FindDstDirectoryUserResult.NotFound, result);
        Assert.Null(resolved);
    }

    [Fact]
    public void WrongTypeFilteredOut_NotFound()
    {
        // A group named "alice" must not satisfy a user lookup.
        var results = new[] { D("alice", type: Group) };
        var (resolved, result) = ResolveDstDirectoryUserPure(results, "alice", User);
        Assert.Equal(FindDstDirectoryUserResult.NotFound, result);
        Assert.Null(resolved);
    }

    [Fact]
    public void MatchIsCaseInsensitive()
    {
        var results = new[] { D("Alice") };
        var (resolved, result) = ResolveDstDirectoryUserPure(results, "alice", User);
        Assert.Equal(FindDstDirectoryUserResult.Resolved, result);
        Assert.Equal("Alice", resolved!.identityName);
    }

    // ---- bug reproduction (RED on the original type-only filter) ----

    [Fact]
    public void PrefixOnlySibling_MustNotResolveToWrongUser()
    {
        // Server prefix-search for "john" returns only "johnathan" (john*)...
        var results = new[] { D("johnathan") };
        var (resolved, result) = ResolveDstDirectoryUserPure(results, "john", User);

        // ...there is NO exact "john", so this must be NotFound (the caller then
        // falls back to email search / emits "not found"). The buggy type-only
        // filter instead returns Resolved=johnathan -> wrong principal assigned.
        Assert.Equal(FindDstDirectoryUserResult.NotFound, result);
        Assert.Null(resolved);
    }

    [Fact]
    public void ExactMatchAmongPrefixSiblings_ResolvesNotDuplicated()
    {
        // Server prefix-search for "john" returns the exact "john" plus siblings.
        var results = new[] { D("john"), D("john.doe"), D("johnathan") };
        var (resolved, result) = ResolveDstDirectoryUserPure(results, "john", User);

        // The exact "john" is unambiguous; must resolve to it. The buggy
        // type-only filter sees Count == 3 -> Duplicated -> the user is skipped.
        Assert.Equal(FindDstDirectoryUserResult.Resolved, result);
        Assert.Equal("john", resolved!.identityName);
    }

    [Fact]
    public void GenuineExactDuplicates_StillDuplicated()
    {
        // Two directory users with the SAME exact identityName (e.g. across
        // domains) is the only legitimate Duplicated case after the fix.
        var results = new[] { D("john"), D("john") };
        var (_, result) = ResolveDstDirectoryUserPure(results, "john", User);
        Assert.Equal(FindDstDirectoryUserResult.Duplicated, result);
    }
}
