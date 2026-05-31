using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Pins the Get-PmLicensedUser -ExportCsv row shape: one row per (user,
// license), columns Path/Email/License, friendly license names, and CSV
// escaping of the email. Mirrors the round trip into
// Add-PmLicenseToPmLicensedUser (whose parameters are Email / License / Path,
// with -UserName as an alias of Email). Pure unit tests — no live server.
// Symmetric with PmLicensedGroupCsvTests.
public class PmLicensedUserCsvTests
{
    [Fact]
    public void BuildLicenseDisplayNames_MapsCodesToFriendlyNames_Sorted()
    {
        var user = new NuLicensedUser
        {
            email = "ada@example.com",
            userBundleLicenses = ["RPADEVPRONU", "ATTUNU"],
        };

        var names = GetUserLicenseUser.BuildLicenseDisplayNames(user);

        // ATTUNU → "Attended - Named User", RPADEVPRONU → "Automation Developer - Named User"
        // sorted alphabetically by display name.
        Assert.Equal(["Attended - Named User", "Automation Developer - Named User"], names);
    }

    [Fact]
    public void BuildLicenseDisplayNames_UnknownCode_FallsBackToRawCode()
    {
        var user = new NuLicensedUser
        {
            email = "ada@example.com",
            userBundleLicenses = ["ZZZ_UNKNOWN"],
        };

        var names = GetUserLicenseUser.BuildLicenseDisplayNames(user);

        Assert.Equal(["ZZZ_UNKNOWN"], names);
    }

    [Fact]
    public void BuildLicenseDisplayNames_NullList_ReturnsEmpty()
    {
        var user = new NuLicensedUser { email = "ada@example.com", userBundleLicenses = null };

        Assert.Empty(GetUserLicenseUser.BuildLicenseDisplayNames(user));
    }

    [Fact]
    public void BuildLicenseCsvRow_EscapesAndOrdersColumns()
    {
        // Email with a comma must be quoted so it can't shift columns.
        var row = GetUserLicenseUser.BuildLicenseCsvRow("Orch1:", "ada, lovelace@example.com", "Attended - Named User");

        Assert.Equal(["Orch1:", "\"ada, lovelace@example.com\"", "Attended - Named User"], row);
    }

    [Fact]
    public void BuildLicenseCsvRow_NullValues_BecomeEmptyStrings()
    {
        var row = GetUserLicenseUser.BuildLicenseCsvRow(null, null, null);

        Assert.Equal(["", "", ""], row);
    }
}
