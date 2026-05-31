using System.Linq;
using System.Management.Automation;
using System.Reflection;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Pins the Get-PmLicensedUser -ExportCsv row shape: one row per (user,
// license), columns Path/UserName/License, friendly license names, CSV
// escaping, and exclusion of orphan license-pool rows. Mirrors the round trip
// into Add-PmLicenseToPmLicensedUser (Position-0 -Email param, alias UserName).
// The identifier column is UserName because the License Accountant API returns
// an empty email and carries the login in 'name'. Pure unit tests — no live
// server. Symmetric with PmLicensedGroupCsvTests.
public class PmLicensedUserCsvTests
{
    [Fact]
    public void BuildLicenseDisplayNames_MapsCodesToFriendlyNames_Sorted()
    {
        var user = new NuLicensedUser
        {
            name = "ada@example.com",
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
            name = "ada@example.com",
            userBundleLicenses = ["ZZZ_UNKNOWN"],
        };

        var names = GetUserLicenseUser.BuildLicenseDisplayNames(user);

        Assert.Equal(["ZZZ_UNKNOWN"], names);
    }

    [Fact]
    public void BuildLicenseDisplayNames_NullList_ReturnsEmpty()
    {
        var user = new NuLicensedUser { name = "ada@example.com", userBundleLicenses = null };

        Assert.Empty(GetUserLicenseUser.BuildLicenseDisplayNames(user));
    }

    [Fact]
    public void BuildLicenseCsvRow_EscapesAndOrdersColumns()
    {
        // A comma in the user name must be quoted so it can't shift columns.
        var row = GetUserLicenseUser.BuildLicenseCsvRow("Orch1:", "ada, lovelace", "Attended - Named User");

        Assert.Equal(["Orch1:", "\"ada, lovelace\"", "Attended - Named User"], row);
    }

    [Fact]
    public void BuildLicenseCsvRow_NullValues_BecomeEmptyStrings()
    {
        var row = GetUserLicenseUser.BuildLicenseCsvRow(null, null, null);

        Assert.Equal(["", "", ""], row);
    }

    // ---- orphan exclusion ----

    [Fact]
    public void IsExportableUser_RealUser_IsExported()
    {
        Assert.True(GetUserLicenseUser.IsExportableUser(
            new NuLicensedUser { name = "ada@example.com", orphan = false }));
    }

    [Fact]
    public void IsExportableUser_NullOrphan_IsExported()
    {
        // Defensive: a missing orphan flag is treated as a real user.
        Assert.True(GetUserLicenseUser.IsExportableUser(
            new NuLicensedUser { name = "ada@example.com", orphan = null }));
    }

    [Fact]
    public void IsExportableUser_OrphanPool_IsExcluded()
    {
        // orphan=true rows carry a bundle display name in 'name' and no user —
        // they must not appear in the round-trippable export.
        Assert.False(GetUserLicenseUser.IsExportableUser(
            new NuLicensedUser { name = "Attended - Named User", orphan = true }));
    }

    // ---- column / parameter parity ----

    [Fact]
    public void CsvHeaders_AreExactlyPathUserNameLicense()
    {
        var field = typeof(GetUserLicenseUser).GetField("CsvHeaders",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(field);
        var headers = (string[]?)field!.GetValue(null);
        Assert.Equal(["Path", "UserName", "License"], headers);
    }

    [Fact]
    public void UserNameColumn_BindsToAddCmdletEmailParameterViaAlias()
    {
        // The CSV's UserName column binds to Add-PmLicenseToPmLicensedUser's
        // -Email parameter through its [Alias("UserName")]. Pin both the alias
        // and the License/Path parameters so the round trip can't silently break.
        var emailProp = typeof(AddPmLicenseToPmLicensedUserCmdlet).GetProperty("Email");
        Assert.NotNull(emailProp);
        var alias = emailProp!.GetCustomAttribute<AliasAttribute>();
        Assert.NotNull(alias);
        Assert.Contains("UserName", alias!.AliasNames);

        Assert.NotNull(typeof(AddPmLicenseToPmLicensedUserCmdlet).GetProperty("License"));
        Assert.NotNull(typeof(AddPmLicenseToPmLicensedUserCmdlet).GetProperty("Path"));
    }
}
