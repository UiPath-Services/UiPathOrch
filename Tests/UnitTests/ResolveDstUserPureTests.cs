using UiPath.PowerShell.Entities;
using Xunit;
using static UiPath.PowerShell.Core.OrchProvider;

namespace UnitTests;

// Unit tests for ResolveDstUserPure -- the name-resolution + folder-assignment
// check extracted from CopyItem.cs's FindDstUser. The extraction was motivated
// by the long-standing "TODO: Is this implementation incomplete?" comment on
// FindDstUser; pinning the matching policy down with tests makes it possible
// to audit / amend that policy with confidence.
//
// The policy under test (preserved verbatim from the original FindDstUser):
//   1. UserMappingCsv lookup. If userMapping[srcUser.UserName] exists and
//      is non-empty, use the mapped name as the search name. Otherwise fall
//      through to srcUser.UserName.
//   2. Case-insensitive UserName match against dstUsers.
//   3. (allowEmailFallback only) case-insensitive UserName=srcUser.EmailAddress
//      match. This is asymmetric in the original FindDstUser: the second
//      retry pass (post cache clear) only tries name match, no email.
//   4. If a user is found, verify their Id is in assignedFolderUserIds;
//      if not, return (user, NotAssignedToFolder). The dstUser is still
//      returned in the tuple for the caller's warning message context.
public class ResolveDstUserPureTests
{
    private static User U(long id, string userName, string? email = null) =>
        new() { Id = id, UserName = userName, EmailAddress = email };

    private static HashSet<long> Assigned(params long[] ids) => new(ids);

    [Fact]
    public void FoundByExactNameMatch_WhenAssignedToFolder()
    {
        var src = U(1, "alice");
        var dst = new[] { U(10, "alice"), U(11, "bob") };
        var (user, result) = ResolveDstUserPure(src, dst, userMapping: null, Assigned(10));
        Assert.Equal(FindDstUserResult.Found, result);
        Assert.Equal(10, user!.Id);
    }

    [Fact]
    public void MatchIsCaseInsensitive()
    {
        var src = U(1, "Alice");
        var dst = new[] { U(10, "ALICE") };
        var (user, result) = ResolveDstUserPure(src, dst, userMapping: null, Assigned(10));
        Assert.Equal(FindDstUserResult.Found, result);
        Assert.Equal(10, user!.Id);
    }

    [Fact]
    public void UserMappingTakesPrecedenceOverOriginalName()
    {
        var src = U(1, "alice@old");
        var dst = new[] { U(10, "alice@old"), U(11, "alice@new") };
        var mapping = new Dictionary<string, string> { ["alice@old"] = "alice@new" };
        var (user, result) = ResolveDstUserPure(src, dst, mapping, Assigned(11));
        Assert.Equal(FindDstUserResult.Found, result);
        Assert.Equal(11, user!.Id);
    }

    [Fact]
    public void UserMappingEntryIgnoredWhenMappedValueIsEmpty()
    {
        var src = U(1, "alice");
        var dst = new[] { U(10, "alice") };
        var mapping = new Dictionary<string, string> { ["alice"] = "" };
        var (user, result) = ResolveDstUserPure(src, dst, mapping, Assigned(10));
        Assert.Equal(FindDstUserResult.Found, result);
        Assert.Equal(10, user!.Id);
    }

    [Fact]
    public void UserMappingMissForUnrelatedKeyDoesNotInfluenceMatch()
    {
        var src = U(1, "alice");
        var dst = new[] { U(10, "alice") };
        var mapping = new Dictionary<string, string> { ["bob"] = "renamed-bob" };
        var (user, result) = ResolveDstUserPure(src, dst, mapping, Assigned(10));
        Assert.Equal(FindDstUserResult.Found, result);
        Assert.Equal(10, user!.Id);
    }

    [Fact]
    public void EmailFallbackResolvesWhenNameDoesNotMatchAndEmailFallbackEnabled()
    {
        var src = U(1, userName: "alice@b2b.example", email: "alice@b2b.example");
        // B2B: dst UserName might be the resolver-token UPN rather than the email
        var dst = new[] { U(10, "alice@directory.local") };
        // No name match, but srcUser.EmailAddress matches dstUser.UserName
        var srcWithEmail = U(1, userName: "different", email: "alice@directory.local");
        var (user, result) = ResolveDstUserPure(srcWithEmail, dst, userMapping: null, Assigned(10));
        Assert.Equal(FindDstUserResult.Found, result);
        Assert.Equal(10, user!.Id);
    }

    [Fact]
    public void EmailFallbackDisabledByAllowEmailFallbackFalse()
    {
        // The allowEmailFallback parameter still exists for any future
        // caller that legitimately wants to skip the email fallback. The
        // current FindDstUser callers both pass true (the first-pass
        // default and the post-cache-clear retry both use email fallback
        // -- the original implementation deliberately skipped email on the
        // retry but that was an inconsistency, fixed during the audit).
        var src = U(1, userName: "different", email: "alice@directory.local");
        var dst = new[] { U(10, "alice@directory.local") };
        var (user, result) = ResolveDstUserPure(src, dst, userMapping: null, Assigned(10),
            allowEmailFallback: false);
        Assert.Equal(FindDstUserResult.NotFound, result);
        Assert.Null(user);
    }

    [Fact]
    public void EmailFallbackSkippedWhenEmailIsNullOrEmpty()
    {
        var src = U(1, userName: "different", email: null);
        var dst = new[] { U(10, "different-than-src") };
        var (user, result) = ResolveDstUserPure(src, dst, userMapping: null, Assigned(10));
        Assert.Equal(FindDstUserResult.NotFound, result);
        Assert.Null(user);
    }

    [Fact]
    public void NotFoundWhenNoCandidateMatchesNameOrEmail()
    {
        var src = U(1, "alice", "alice@x.com");
        var dst = new[] { U(10, "bob"), U(11, "carol") };
        var (user, result) = ResolveDstUserPure(src, dst, userMapping: null, Assigned(10, 11));
        Assert.Equal(FindDstUserResult.NotFound, result);
        Assert.Null(user);
    }

    [Fact]
    public void NotAssignedToFolderEvenWhenNameMatches()
    {
        var src = U(1, "alice");
        var dst = new[] { U(10, "alice") };
        var (user, result) = ResolveDstUserPure(src, dst, userMapping: null, Assigned(/* empty */));
        Assert.Equal(FindDstUserResult.NotAssignedToFolder, result);
        // The matched user is still returned -- the caller uses it for the
        // warning message context ("user 'alice' is not assigned in '<folder>'").
        Assert.NotNull(user);
        Assert.Equal(10, user!.Id);
    }

    [Fact]
    public void NotAssignedToFolderWhenMatchedUserHasNullId()
    {
        // Defensive: if the API returned a user with no Id, we cannot verify
        // folder assignment so treat as not assigned.
        var src = U(1, "alice");
        var dst = new[] { new User { Id = null, UserName = "alice" } };
        var (user, result) = ResolveDstUserPure(src, dst, userMapping: null, Assigned(10));
        Assert.Equal(FindDstUserResult.NotAssignedToFolder, result);
        Assert.NotNull(user);
    }

    [Fact]
    public void EmptySrcUserNameWithNoMappingMatchesEmptyDstUserName()
    {
        // Edge: srcUser has a null UserName. searchName becomes "" and matches
        // dst users with "" or null UserName. This is current behaviour; this
        // test pins it down so any future change is intentional.
        var src = new User { Id = 1, UserName = null };
        var dstWithEmpty = new[] { new User { Id = 10, UserName = "" } };
        var (user, result) = ResolveDstUserPure(src, dstWithEmpty, userMapping: null, Assigned(10));
        Assert.Equal(FindDstUserResult.Found, result);
        Assert.Equal(10, user!.Id);
    }
}
