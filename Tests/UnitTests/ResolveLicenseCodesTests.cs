using System.Management.Automation;
using UiPath.PowerShell.Commands;
using Xunit;

namespace UnitTests;

// Pins AddPmUserLicenseCmdlet.ResolveLicenseCodes — the pure -License resolver for users.
// Matches by friendly name OR raw code so users can pass either form, and is restricted to
// the codes the ORGANISATION actually holds, which the caller reads from the live inventory
// (LicenseInventory.userLicensingBundles). That restriction is the point of these tests: the
// resolver used to iterate the static catalog instead, so a code the catalog had never heard
// of — every App Testing bundle — resolved to nothing and was dropped in silence.
//
// The catalog keeps one job: turning a code into words. The API's own bundle `name` is empty
// (verified on Orch1 2026-05-31 and again on an App Testing org 2026-10-16), which is why a
// friendly name can be matched at all.
public class ResolveLicenseCodesTests
{
    private static List<WildcardPattern> Patterns(params string[] patterns) =>
        patterns.Select(p => new WildcardPattern(p, WildcardOptions.IgnoreCase)).ToList();

    // A plausible organisation: two catalog bundles and one the catalog does know about now.
    private static readonly string?[] Available =
        ["ATTUNU", "ATTUCU", "CTZDEVNU", "APPTESTNU", "APPTESTDEVNU"];

    [Fact]
    public void ExactCode_Matches()
    {
        var got = AddPmUserLicenseCmdlet.ResolveLicenseCodes(Patterns("CTZDEVNU"), Available);
        Assert.Equal(new HashSet<string> { "CTZDEVNU" }, got);
    }

    [Fact]
    public void ExactFriendlyName_Matches()
    {
        var got = AddPmUserLicenseCmdlet.ResolveLicenseCodes(Patterns("Attended - Named User"), Available);
        Assert.Equal(new HashSet<string> { "ATTUNU" }, got);
    }

    [Fact]
    public void NameWildcard_Matches_AllAttendedBundles()
    {
        var got = AddPmUserLicenseCmdlet.ResolveLicenseCodes(Patterns("Attended*"), Available);
        Assert.Contains("ATTUNU", got);   // Attended - Named User
        Assert.Contains("ATTUCU", got);   // Attended - Multiuser
        Assert.DoesNotContain("CTZDEVNU", got);
    }

    [Fact]
    public void MultiplePatterns_Union()
    {
        var got = AddPmUserLicenseCmdlet.ResolveLicenseCodes(
            Patterns("ATTUNU", "Citizen Developer - Named User"), Available);
        Assert.Contains("ATTUNU", got);
        Assert.Contains("CTZDEVNU", got);
    }

    [Fact]
    public void NoMatch_ReturnsEmpty_NotNull()
    {
        var got = AddPmUserLicenseCmdlet.ResolveLicenseCodes(Patterns("NoSuchBundle*"), Available);
        Assert.NotNull(got);
        Assert.Empty(got);
    }

    [Fact]
    public void CaseInsensitive_ByFriendlyName()
    {
        var got = AddPmUserLicenseCmdlet.ResolveLicenseCodes(Patterns("attended - named user"), Available);
        Assert.Contains("ATTUNU", got);
    }

    [Fact]
    public void NullPatterns_ReturnsEmpty()
    {
        var got = AddPmUserLicenseCmdlet.ResolveLicenseCodes(null!, Available);
        Assert.NotNull(got);
        Assert.Empty(got);
    }

    [Fact]
    public void NullAvailable_ReturnsEmpty()
    {
        var got = AddPmUserLicenseCmdlet.ResolveLicenseCodes(Patterns("ATTUNU"), null);
        Assert.NotNull(got);
        Assert.Empty(got);
    }

    // The regression. APPTESTDEVNU is in the organisation's inventory, so it must resolve --
    // it did not when resolution went through the catalog, and the request was dropped without
    // an error, which is how a license assignment appeared to succeed while doing nothing.
    [Fact]
    public void AppTestingCode_Resolves_WhenOrganisationHasIt()
    {
        var got = AddPmUserLicenseCmdlet.ResolveLicenseCodes(Patterns("APPTESTDEVNU"), Available);
        Assert.Equal(new HashSet<string> { "APPTESTDEVNU" }, got);
    }

    [Fact]
    public void AppTestingFriendlyName_Resolves()
    {
        var got = AddPmUserLicenseCmdlet.ResolveLicenseCodes(
            Patterns("App Test Developer - Named User"), Available);
        Assert.Equal(new HashSet<string> { "APPTESTDEVNU" }, got);
    }

    // A code the catalog has never heard of still resolves when the organisation holds it:
    // the catalog is for display, and an unknown code simply has no friendly name to match.
    [Fact]
    public void CodeMissingFromCatalog_StillResolves_ByRawCode()
    {
        var got = AddPmUserLicenseCmdlet.ResolveLicenseCodes(
            Patterns("SOMENEWSKU"), ["SOMENEWSKU", "ATTUNU"]);
        Assert.Equal(new HashSet<string> { "SOMENEWSKU" }, got);
    }

    // Held by the catalog but NOT by this organisation: resolving it would promise a seat
    // that cannot be granted.
    [Fact]
    public void CatalogBundle_NotInOrganisation_DoesNotResolve()
    {
        var got = AddPmUserLicenseCmdlet.ResolveLicenseCodes(Patterns("RPADEVPRONU"), Available);
        Assert.Empty(got);
    }
}
