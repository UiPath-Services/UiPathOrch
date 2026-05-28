using System.Management.Automation;
using UiPath.PowerShell.Commands;
using Xunit;

namespace UnitTests;

// Pins AddPmLicenseToPmLicensedUserCmdlet.ResolveLicenseCodes — the pure -License
// wildcard → bundle-code resolver. Matches by friendly name OR raw code so
// users can pass either form; an empty match yields an empty set (the cmdlet
// then silently skips, matching Add-PmLicenseToPmLicensedGroup).
public class ResolveLicenseCodesTests
{
    private static List<WildcardPattern> Patterns(params string[] patterns) =>
        patterns.Select(p => new WildcardPattern(p, WildcardOptions.IgnoreCase)).ToList();

    [Fact]
    public void ExactCode_Matches()
    {
        var got = AddPmLicenseToPmLicensedUserCmdlet.ResolveLicenseCodes(Patterns("CTZDEVNU"));
        Assert.Equal(new HashSet<string> { "CTZDEVNU" }, got);
    }

    [Fact]
    public void ExactFriendlyName_Matches()
    {
        var got = AddPmLicenseToPmLicensedUserCmdlet.ResolveLicenseCodes(Patterns("Attended - Named User"));
        Assert.Equal(new HashSet<string> { "ATTUNU" }, got);
    }

    [Fact]
    public void NameWildcard_Matches_AllAttendedBundles()
    {
        var got = AddPmLicenseToPmLicensedUserCmdlet.ResolveLicenseCodes(Patterns("Attended*"));
        Assert.Contains("ATTUNU", got);   // Attended - Named User
        Assert.Contains("ATTUCU", got);   // Attended - Multiuser
        Assert.DoesNotContain("CTZDEVNU", got);
    }

    [Fact]
    public void MultiplePatterns_Union()
    {
        var got = AddPmLicenseToPmLicensedUserCmdlet.ResolveLicenseCodes(
            Patterns("ATTUNU", "Citizen Developer - Named User"));
        Assert.Contains("ATTUNU", got);
        Assert.Contains("CTZDEVNU", got);
    }

    [Fact]
    public void NoMatch_ReturnsEmpty_NotNull()
    {
        var got = AddPmLicenseToPmLicensedUserCmdlet.ResolveLicenseCodes(Patterns("NoSuchBundle*"));
        Assert.NotNull(got);
        Assert.Empty(got);
    }

    [Fact]
    public void CaseInsensitive_ByFriendlyName()
    {
        var got = AddPmLicenseToPmLicensedUserCmdlet.ResolveLicenseCodes(Patterns("attended - named user"));
        Assert.Contains("ATTUNU", got);
    }

    [Fact]
    public void NullPatterns_ReturnsEmpty()
    {
        var got = AddPmLicenseToPmLicensedUserCmdlet.ResolveLicenseCodes(null!);
        Assert.NotNull(got);
        Assert.Empty(got);
    }
}
