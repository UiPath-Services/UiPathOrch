using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Pins the same-tenant use of the user mapping CSV: Copy-OrchUser / Copy-OrchFolderUser re-home
// a tenant's users onto other users of the same tenant (a domain or identity-provider change).
public class SameTenantUserMappingTests
{
    [Fact]
    public void KeepRenamingRows_drops_empty_and_self_rows()
    {
        var m = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [@"OLD\taro"] = @"NEW\taro",
            [@"OLD\hanako"] = "",
            [@"OLD\jiro"] = @"old\JIRO",
            ["a@old.example"] = "a@new.example",
        };
        var kept = SessionStateExtensions.KeepRenamingRows(m);
        Assert.Equal(2, kept.Count);
        Assert.Equal(@"NEW\taro", kept[@"old\TARO"]);   // still case-insensitive
        Assert.Equal("a@new.example", kept["a@old.example"]);
    }

    [Theory]
    [InlineData(@"OLD\taro", "OLD", "NEW", @"NEW\taro")]
    [InlineData(@"old\taro", "OLD", "NEW", @"NEW\taro")]
    [InlineData(@"OTHER\taro", "OLD", "NEW", null)]
    [InlineData("taro@old.example", "old.example", "new.example", "taro@new.example")]
    [InlineData("taro@sub.old.example", "old.example", "new.example", null)]   // exact domain only
    [InlineData("taro", "OLD", "NEW", null)]                                    // a bare name names no domain
    [InlineData(@"OLD\RPA Users", "OLD", "NEW", @"NEW\RPA Users")]
    public void RewriteDomain(string name, string src, string dst, string? expected)
    {
        Assert.Equal(expected, NewUserMappingCsvCmdlet.RewriteDomain(name, src, dst));
    }

    private static readonly Role[] TenantRoles =
    [
        new() { Name = "Administrator", Type = "Mixed" },
        new() { Name = "Allow to be Automation User", Type = "Tenant" },
        new() { Name = "Folder Administrator", Type = "Folder" },
    ];

    [Fact]
    public void MissingTenantRoles_adds_only_what_is_missing()
    {
        var missing = CopyUserCmdlet.MissingTenantRoles(
            ["Administrator", "Allow to be Automation User", "Folder Administrator", "Gone"],
            ["administrator"],
            TenantRoles);
        Assert.Equal(["Allow to be Automation User"], missing);
    }

    [Fact]
    public void MissingTenantRoles_none()
    {
        Assert.Empty(CopyUserCmdlet.MissingTenantRoles(null, ["Administrator"], TenantRoles));
        Assert.Empty(CopyUserCmdlet.MissingTenantRoles(["Administrator"], ["Administrator"], TenantRoles));
    }

    [Theory]
    [InlineData(@"CORP\svc", null, null, true)]
    [InlineData(@"CORP\svc", "Default", null, true)]
    [InlineData(@"CORP\svc", "Default", "vault/svc", false)]   // the password is in the external store
    [InlineData(@"CORP\svc", "SmartCard", null, false)]
    [InlineData(@"CORP\svc", "NoCredential", null, false)]
    [InlineData("", "Default", null, false)]
    public void NeedsUnattendedPassword(string userName, string? type, string? external, bool expected)
    {
        var ur = new UnattendedRobot { UserName = userName, CredentialType = type, CredentialExternalName = external };
        Assert.Equal(expected, CopyUserCmdlet.NeedsUnattendedPassword(ur));
    }

    [Fact]
    public void NeedsUnattendedPassword_no_robot()
    {
        Assert.False(CopyUserCmdlet.NeedsUnattendedPassword(null));
    }

    [Fact]
    public void FindTenantUser_by_name_or_email_and_type()
    {
        User[] users =
        [
            new() { UserName = @"NEW\taro", Type = "DirectoryUser" },
            new() { UserName = "b2b#ext#", EmailAddress = "a@new.example", Type = "DirectoryUser" },
            new() { UserName = @"NEW\RPA", Type = "DirectoryGroup" },
        ];
        Assert.NotNull(CopyUserCmdlet.FindTenantUser(users, @"new\TARO", "DirectoryUser"));
        Assert.NotNull(CopyUserCmdlet.FindTenantUser(users, "a@new.example", "DirectoryUser"));
        Assert.Null(CopyUserCmdlet.FindTenantUser(users, @"NEW\RPA", "DirectoryUser"));   // a group is not a user
        Assert.NotNull(CopyUserCmdlet.FindTenantUser(users, @"NEW\RPA", "DirectoryGroup"));
    }
}
