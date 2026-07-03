using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Unit tests for TestUserMappingCsvCmdlet.IsDestinationTenantUser — the
// tenant-user reachability check added on top of the directory check.
// Rationale: Test-OrchUserMappingCsv used to validate DestinationUserName
// against the destination *directory* only, while the asset copy re-homes
// per-user values against the destination *tenant user list*
// (ResolveDstUserPure). A mapping could therefore validate clean and still
// drop every per-user value at copy time. This helper mirrors the copy-time
// matching policy (mapped-name match, then source-email fallback) so the
// validator warns about that gap up front.
public class UserMappingCsvTenantUserCheckTests
{
    private static User U(long id, string userName) => new() { Id = id, UserName = userName };

    [Fact]
    public void MatchByName_CaseInsensitive()
    {
        var dst = new[] { U(1, "Alice@Contoso.com"), U(2, "bob@contoso.com") };
        Assert.True(TestUserMappingCsvCmdlet.IsDestinationTenantUser("alice@contoso.com", null, dst));
    }

    [Fact]
    public void NoNameMatch_FallsBackToSourceEmail()
    {
        // Destination tenant knows the user by email while the CSV maps to a
        // different spelling — same fallback ResolveDstUserPure applies.
        var dst = new[] { U(1, "jsmith@contoso.com") };
        Assert.True(TestUserMappingCsvCmdlet.IsDestinationTenantUser("CONTOSO\\jsmith", "jsmith@contoso.com", dst));
    }

    [Fact]
    public void NotFound_WhenNeitherNameNorEmailMatches()
    {
        var dst = new[] { U(1, "someone@contoso.com") };
        Assert.False(TestUserMappingCsvCmdlet.IsDestinationTenantUser("jsmith@contoso.com", "old@fabrikam.com", dst));
    }

    [Fact]
    public void EmptySourceEmail_DoesNotMatchAnything()
    {
        // Guard: a destination user with an empty UserName must not match an
        // empty/null source email.
        var dst = new[] { U(1, "") };
        Assert.False(TestUserMappingCsvCmdlet.IsDestinationTenantUser("jsmith@contoso.com", null, dst));
        Assert.False(TestUserMappingCsvCmdlet.IsDestinationTenantUser("jsmith@contoso.com", "", dst));
    }

    [Fact]
    public void EmptyTenantUserList_NotFound()
    {
        Assert.False(TestUserMappingCsvCmdlet.IsDestinationTenantUser("jsmith@contoso.com", "jsmith@contoso.com", Array.Empty<User>()));
    }

    // --- ResolveRobotDestination: New-OrchUserMappingCsv's robot rows resolve against
    // the destination TENANT user list (the directory search may not return robot
    // accounts) — same-named robots auto-fill with the destination's own spelling. ---

    [Fact]
    public void RobotResolution_SameName_AutoFills_WithDestinationCasing()
    {
        var dst = new[] { U(1, "ALVA-RPAAEU01"), U(2, "other") };
        Assert.Equal("ALVA-RPAAEU01", NewUserMappingCsvCmdlet.ResolveRobotDestination("alva-rpaaeu01", dst));
    }

    [Fact]
    public void RobotResolution_NoMatch_LeavesNull()
    {
        var dst = new[] { U(1, "alva-rpaaeu01") };
        Assert.Null(NewUserMappingCsvCmdlet.ResolveRobotDestination("migrated aeu_alva-rpaaeu01", dst));
        Assert.Null(NewUserMappingCsvCmdlet.ResolveRobotDestination(null, dst));
    }

    // --- FormatPendingAssignmentWarning: the aggregated once-per-run line replacing
    // per-row warnings (fresh cross-org destinations would otherwise warn on nearly
    // every directory-user row). ---

    [Fact]
    public void PendingWarning_ListsAllEntries_UnderLimit()
    {
        var entries = new[] { "'a@x.com' (mapped from 'A\\a')", "'b@x.com' (mapped from 'A\\b')" };
        var text = TestUserMappingCsvCmdlet.FormatPendingAssignmentWarning(entries, "Dst:");

        Assert.StartsWith("2 DestinationUserName(s)", text);
        Assert.Contains("not yet tenant users in 'Dst:'", text);
        Assert.Contains("'a@x.com' (mapped from 'A\\a')", text);
        Assert.Contains("'b@x.com' (mapped from 'A\\b')", text);
        Assert.DoesNotContain("more)", text);
        Assert.Contains("Copy-OrchFolderUser", text);
    }

    [Fact]
    public void PendingWarning_CollapsesEntriesBeyondLimitIntoMoreTail()
    {
        var entries = Enumerable.Range(1, 25).Select(i => $"'u{i:00}@x.com'").ToList();
        var text = TestUserMappingCsvCmdlet.FormatPendingAssignmentWarning(entries, "Dst:");

        Assert.StartsWith("25 DestinationUserName(s)", text);
        Assert.Contains("'u20@x.com'", text);
        Assert.DoesNotContain("'u21@x.com'", text);
        Assert.Contains("(+5 more)", text);
    }
}
