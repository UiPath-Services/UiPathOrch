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
}
