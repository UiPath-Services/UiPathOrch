using UiPath.PowerShell.Entities;
using Xunit;
using static UiPath.PowerShell.Core.OrchProvider;

namespace UnitTests;

// Unit tests for ResolveDstUserPure -- the name-resolution extracted from
// CopyItem.cs's FindDstUser. The extraction was motivated by the long-standing
// "TODO: Is this implementation incomplete?" comment on FindDstUser; pinning the
// matching policy down with tests makes it possible to audit / amend it safely.
//
// Folder-scope authorization was REMOVED from this resolver (PR #20): the
// Orchestrator API is the authority for whether a resolved tenant user may hold a
// per-user value in the destination folder, and it accepts any existing tenant user
// regardless of folder assignment (verified by direct PUT probes on cloud Orch1 and
// on-prem 22.10.1). The resolver now only maps src -> dst user by name/email.
//
// The policy under test:
//   1. UserMappingCsv lookup. If userMapping[srcUser.UserName] exists and
//      is non-empty, use the mapped name as the search name. Otherwise fall
//      through to srcUser.UserName.
//   2. Case-insensitive UserName match against dstUsers.
//   3. (allowEmailFallback only) case-insensitive UserName=srcUser.EmailAddress
//      match.
//   4. A matched user with a null Id is unusable (no integer UserId to reference),
//      so it is reported as NotFound.
public class ResolveDstUserPureTests
{
    private static User U(long id, string userName, string? email = null) =>
        new() { Id = id, UserName = userName, EmailAddress = email };

    [Fact]
    public void FoundByExactNameMatch()
    {
        var src = U(1, "alice");
        var dst = new[] { U(10, "alice"), U(11, "bob") };
        var (user, result) = ResolveDstUserPure(src, dst, userMapping: null);
        Assert.Equal(FindDstUserResult.Found, result);
        Assert.Equal(10, user!.Id);
    }

    [Fact]
    public void MatchIsCaseInsensitive()
    {
        var src = U(1, "Alice");
        var dst = new[] { U(10, "ALICE") };
        var (user, result) = ResolveDstUserPure(src, dst, userMapping: null);
        Assert.Equal(FindDstUserResult.Found, result);
        Assert.Equal(10, user!.Id);
    }

    [Fact]
    public void UserMappingTakesPrecedenceOverOriginalName()
    {
        var src = U(1, "alice@old");
        var dst = new[] { U(10, "alice@old"), U(11, "alice@new") };
        var mapping = new Dictionary<string, string> { ["alice@old"] = "alice@new" };
        var (user, result) = ResolveDstUserPure(src, dst, mapping);
        Assert.Equal(FindDstUserResult.Found, result);
        Assert.Equal(11, user!.Id);
    }

    [Fact]
    public void UserMappingEntryIgnoredWhenMappedValueIsEmpty()
    {
        var src = U(1, "alice");
        var dst = new[] { U(10, "alice") };
        var mapping = new Dictionary<string, string> { ["alice"] = "" };
        var (user, result) = ResolveDstUserPure(src, dst, mapping);
        Assert.Equal(FindDstUserResult.Found, result);
        Assert.Equal(10, user!.Id);
    }

    [Fact]
    public void UserMappingMissForUnrelatedKeyDoesNotInfluenceMatch()
    {
        var src = U(1, "alice");
        var dst = new[] { U(10, "alice") };
        var mapping = new Dictionary<string, string> { ["bob"] = "renamed-bob" };
        var (user, result) = ResolveDstUserPure(src, dst, mapping);
        Assert.Equal(FindDstUserResult.Found, result);
        Assert.Equal(10, user!.Id);
    }

    [Fact]
    public void EmailFallbackResolvesWhenNameDoesNotMatchAndEmailFallbackEnabled()
    {
        // No name match, but srcUser.EmailAddress matches dstUser.UserName
        var dst = new[] { U(10, "alice@directory.local") };
        var srcWithEmail = U(1, userName: "different", email: "alice@directory.local");
        var (user, result) = ResolveDstUserPure(srcWithEmail, dst, userMapping: null);
        Assert.Equal(FindDstUserResult.Found, result);
        Assert.Equal(10, user!.Id);
    }

    [Fact]
    public void EmailFallbackDisabledByAllowEmailFallbackFalse()
    {
        var src = U(1, userName: "different", email: "alice@directory.local");
        var dst = new[] { U(10, "alice@directory.local") };
        var (user, result) = ResolveDstUserPure(src, dst, userMapping: null,
            allowEmailFallback: false);
        Assert.Equal(FindDstUserResult.NotFound, result);
        Assert.Null(user);
    }

    [Fact]
    public void EmailFallbackSkippedWhenEmailIsNullOrEmpty()
    {
        var src = U(1, userName: "different", email: null);
        var dst = new[] { U(10, "different-than-src") };
        var (user, result) = ResolveDstUserPure(src, dst, userMapping: null);
        Assert.Equal(FindDstUserResult.NotFound, result);
        Assert.Null(user);
    }

    [Fact]
    public void NotFoundWhenNoCandidateMatchesNameOrEmail()
    {
        var src = U(1, "alice", "alice@x.com");
        var dst = new[] { U(10, "bob"), U(11, "carol") };
        var (user, result) = ResolveDstUserPure(src, dst, userMapping: null);
        Assert.Equal(FindDstUserResult.NotFound, result);
        Assert.Null(user);
    }

    [Fact]
    public void FoundEvenWhenNotDirectlyFolderAssigned()
    {
        // Post-PR#20: folder-scope is NOT checked here. A name match resolves to
        // Found regardless of whether the user is directly assigned to the target
        // folder -- this is exactly the group-/role-reachable case the old
        // folder-scope filter wrongly rejected. The API decides on PUT.
        var src = U(1, "alice");
        var dst = new[] { U(10, "alice") };
        var (user, result) = ResolveDstUserPure(src, dst, userMapping: null);
        Assert.Equal(FindDstUserResult.Found, result);
        Assert.Equal(10, user!.Id);
    }

    [Fact]
    public void NotFoundWhenMatchedUserHasNullId()
    {
        // A name match to a user with no Id is unusable (the per-user value is keyed
        // by integer UserId), so it is reported as NotFound rather than returned.
        var src = U(1, "alice");
        var dst = new[] { new User { Id = null, UserName = "alice" } };
        var (user, result) = ResolveDstUserPure(src, dst, userMapping: null);
        Assert.Equal(FindDstUserResult.NotFound, result);
        Assert.Null(user);
    }

    [Fact]
    public void EmptySrcUserNameWithNoMappingMatchesEmptyDstUserName()
    {
        // Edge: srcUser has a null UserName. searchName becomes "" and matches
        // dst users with "" or null UserName. This is current behaviour; this
        // test pins it down so any future change is intentional.
        var src = new User { Id = 1, UserName = null };
        var dstWithEmpty = new[] { new User { Id = 10, UserName = "" } };
        var (user, result) = ResolveDstUserPure(src, dstWithEmpty, userMapping: null);
        Assert.Equal(FindDstUserResult.Found, result);
        Assert.Equal(10, user!.Id);
    }
}
