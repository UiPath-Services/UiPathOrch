using System.Management.Automation;
using UiPath.PowerShell.Commands;
using Xunit;

namespace UnitTests;

// Pins RemovePmLicenseFromPmLicensedUserCmdlet.ResolveLicenseCodesToRemove —
// the pure -License wildcard ∩ current-bundles resolver. Unlike Add's resolver
// (which matches against the full catalog), Remove matches against ONLY what
// the user currently holds, so non-matching patterns are a silent no-op and
// '*' strips exactly the held set.
public class ResolveLicenseCodesToRemoveTests
{
    private static List<WildcardPattern> Patterns(params string[] patterns) =>
        patterns.Select(p => new WildcardPattern(p, WildcardOptions.IgnoreCase)).ToList();

    [Fact]
    public void StarPattern_StripsExactlyTheHeldSet()
    {
        var held = new[] { "ATTUNU", "RPADEVNU", "CTZDEVNU" };
        var got = RemovePmLicenseFromPmLicensedUserCmdlet.ResolveLicenseCodesToRemove(
            Patterns("*"), held);
        Assert.Equal(new HashSet<string>(held), got);
    }

    [Fact]
    public void NameWildcard_MatchesOnlyHeldBundles()
    {
        // User holds Citizen + Attended; pattern matches both by friendly name.
        var held = new[] { "ATTUNU", "CTZDEVNU" };
        var got = RemovePmLicenseFromPmLicensedUserCmdlet.ResolveLicenseCodesToRemove(
            Patterns("Citizen*"), held);
        Assert.Equal(new HashSet<string> { "CTZDEVNU" }, got);
    }

    [Fact]
    public void ExactCode_MatchesByCode()
    {
        var held = new[] { "ATTUNU", "RPADEVNU", "CTZDEVNU" };
        var got = RemovePmLicenseFromPmLicensedUserCmdlet.ResolveLicenseCodesToRemove(
            Patterns("RPADEVNU"), held);
        Assert.Equal(new HashSet<string> { "RPADEVNU" }, got);
    }

    [Fact]
    public void PatternMatchesCatalogButNotHeld_ReturnsEmpty()
    {
        // User holds only Attended; pattern matches Citizen in catalog — but
        // Remove cares only about what's actually held, so this is a no-op.
        var held = new[] { "ATTUNU" };
        var got = RemovePmLicenseFromPmLicensedUserCmdlet.ResolveLicenseCodesToRemove(
            Patterns("Citizen*"), held);
        Assert.Empty(got);
    }

    [Fact]
    public void MultiplePatterns_Union()
    {
        var held = new[] { "ATTUNU", "RPADEVNU", "CTZDEVNU" };
        var got = RemovePmLicenseFromPmLicensedUserCmdlet.ResolveLicenseCodesToRemove(
            Patterns("ATTUNU", "Citizen - Named User"), held);
        Assert.Contains("ATTUNU", got);
        // "Citizen - Named User" doesn't match any friendly name (real name is
        // "Citizen Developer - Named User"), so only ATTUNU matches.
        Assert.Single(got);

        var got2 = RemovePmLicenseFromPmLicensedUserCmdlet.ResolveLicenseCodesToRemove(
            Patterns("ATTUNU", "Citizen Developer - Named User"), held);
        Assert.Equal(new HashSet<string> { "ATTUNU", "CTZDEVNU" }, got2);
    }

    [Fact]
    public void CaseInsensitive_ByFriendlyName()
    {
        var held = new[] { "ATTUNU" };
        var got = RemovePmLicenseFromPmLicensedUserCmdlet.ResolveLicenseCodesToRemove(
            Patterns("attended - named user"), held);
        Assert.Equal(new HashSet<string> { "ATTUNU" }, got);
    }

    [Fact]
    public void UnknownHeldCode_StillRemovableByExactCode()
    {
        // If a user somehow holds a code not in the friendly-name catalog, we
        // can still remove it by passing the exact code (raw-code match).
        var held = new[] { "FUTURE_BUNDLE" };
        var got = RemovePmLicenseFromPmLicensedUserCmdlet.ResolveLicenseCodesToRemove(
            Patterns("FUTURE_BUNDLE"), held);
        Assert.Equal(new HashSet<string> { "FUTURE_BUNDLE" }, got);
    }

    [Fact]
    public void NullPatterns_ReturnsEmpty()
    {
        var got = RemovePmLicenseFromPmLicensedUserCmdlet.ResolveLicenseCodesToRemove(
            null, new[] { "ATTUNU" });
        Assert.Empty(got);
    }

    [Fact]
    public void NullHeld_ReturnsEmpty()
    {
        var got = RemovePmLicenseFromPmLicensedUserCmdlet.ResolveLicenseCodesToRemove(
            Patterns("*"), null);
        Assert.Empty(got);
    }
}
