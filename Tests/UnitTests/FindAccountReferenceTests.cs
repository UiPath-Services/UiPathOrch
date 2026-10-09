using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Pins Find-OrchAccountReference: the account matcher and the three scans it runs.
public class FindAccountReferenceTests
{
    [Theory]
    [InlineData("svc_rpa", @"CORP\svc_rpa", true)]
    [InlineData("svc_rpa", "svc_rpa@corp.example.com", true)]
    [InlineData("svc_rpa", "svc_rpa", true)]
    [InlineData("SVC_RPA", @"corp\svc_rpa", true)]
    [InlineData("svc_*", @"CORP\svc_batch", true)]
    [InlineData("svc_rpa", @"CORP\svc_rpa2", false)]
    [InlineData("corp", @"CORP\svc_rpa", false)]           // a bare pattern never matches the domain part
    [InlineData(@"CORP\svc_rpa", @"CORP\svc_rpa", true)]
    [InlineData(@"CORP\svc_rpa", @"OTHER\svc_rpa", false)]
    [InlineData(@"CORP\svc_rpa", "svc_rpa", false)]       // a qualified pattern needs the whole string
    [InlineData(@"*\svc_rpa", @"OTHER\svc_rpa", true)]
    [InlineData("svc_rpa@corp.example.com", "svc_rpa@corp.example.com", true)]
    [InlineData("*@corp.example.com", "a@corp.example.com", true)]
    [InlineData("svc_rpa", "", false)]
    [InlineData("svc_rpa", null, false)]
    public void Matcher(string pattern, string? account, bool expected)
    {
        Assert.Equal(expected, new AccountMatcher([pattern]).IsMatch(account));
    }

    [Fact]
    public void Matcher_any_of_several_patterns()
    {
        var m = new AccountMatcher(["nobody", @"CORP\svc_rpa"]);
        Assert.True(m.IsMatch(@"CORP\svc_rpa"));
        Assert.False(m.IsMatch(@"CORP\someone"));
    }

    // A directory user can carry its domain in UserName and in Domain at once; the sign-in must
    // not repeat it ("CORP\CORP\alice" matches nothing).
    [Theory]
    [InlineData("alice", "CORP", @"CORP\alice")]
    [InlineData(@"CORP\alice", "CORP", @"CORP\alice")]
    [InlineData(@"CORP\alice", "corp.example.com", @"CORP\alice")]
    [InlineData("alice@corp.example.com", "CORP", "alice@corp.example.com")]
    [InlineData("alice", null, "alice")]
    [InlineData(null, "CORP", null)]
    public void User_sign_in(string? userName, string? domain, string? expected)
    {
        Assert.Equal(expected, FindAccountReferenceCmdlet.UserSignIn(new User { UserName = userName, Domain = domain }));
    }

    private static readonly CredentialStore[] Stores =[new() { Id = 1, Name = "Orchestrator Database" }, new() { Id = 7, Name = "CyberArk" }];

    private static User[] Users() =>
    [
        new() { Id = 1, Path = @"T:\", UserName = "alice", Domain = "CORP",
                UnattendedRobot = new() { UserName = @"CORP\svc_rpa", CredentialStoreId = 1 } },
        new() { Id = 2, Path = @"T:\", UserName = "bob", Domain = "CORP",
                RobotProvision = new() { UserName = @"CORP\bob" } },
        new() { Id = 3, Path = @"T:\", UserName = "svc_rpa", Domain = "CORP" },
        new() { Id = 4, Path = @"T:\", UserName = "carol@example.com" },
    ];

    private static Robot[] Robots() =>
    [
        new() { Id = 11, Name = "alice-unattended", Username = @"CORP\svc_rpa", UserId = 1, CredentialStoreId = 1 },
        new() { Id = 12, Name = "classic-1", Username = @"CORP\svc_rpa", CredentialStoreId = 7 },
        new() { Id = 13, Name = "classic-2", Username = @"CORP\other" },
        new() { Id = 14, Name = "svc-attended", Username = @"CORP\svc_desk", UserId = 3 },
    ];

    [Fact]
    public void Tenant_reports_sign_in_unattended_and_classic_robot()
    {
        var match = FindAccountReferenceCmdlet.ScanTenant(Users(), Robots(), Stores, new AccountMatcher(["svc_rpa"]), @"T:\");

        Assert.Collection(match.References,
            r => { Assert.Equal("alice", r.Name); Assert.Equal("UnattendedRobot.UserName", r.Property); Assert.Equal("Orchestrator Database", r.CredentialStore); },
            r => { Assert.Equal("svc_rpa", r.Name); Assert.Equal("UserName", r.Property); Assert.Equal(@"CORP\svc_rpa", r.Account); Assert.Null(r.CredentialStore); },
            r => { Assert.Equal("Robot", r.Type); Assert.Equal("classic-1", r.Name); Assert.Equal("CyberArk", r.CredentialStore); Assert.Equal(@"T:\classic-1", r.Path); });

        // The modern robot is alice's own and is not reported twice, but triggers can still name it.
        Assert.DoesNotContain(match.References, r => r.Name == "alice-unattended");
        Assert.Equal(("alice-unattended", @"CORP\svc_rpa"), match.Robots[11]);
        // user svc_rpa matched by its sign-in, so its robot (another account) is followed too
        Assert.Equal(("svc-attended", @"CORP\svc_rpa"), match.Robots[14]);
        Assert.False(match.Robots.ContainsKey(13));
        Assert.Contains("alice", match.UserNames.Keys);
        Assert.Contains("svc_rpa", match.UserNames.Keys);
        Assert.DoesNotContain("bob", match.UserNames.Keys);
    }

    [Fact]
    public void Tenant_attended_robot_and_email_users()
    {
        var match = FindAccountReferenceCmdlet.ScanTenant(Users(), Robots(), Stores, new AccountMatcher(["bob", "carol"]), @"T:\");
        Assert.Contains(match.References, r => r.Name == "bob" && r.Property == "RobotProvision.UserName");
        Assert.Contains(match.References, r => r.Name == "bob" && r.Property == "UserName");
        Assert.Contains(match.References, r => r.Name == "carol@example.com" && r.Property == "UserName" && r.Account == "carol@example.com");
    }

    [Fact]
    public void Tenant_without_store_permission_still_reports()
    {
        var match = FindAccountReferenceCmdlet.ScanTenant(Users(), Robots(), null, new AccountMatcher(["svc_rpa"]), @"T:\");
        Assert.Equal("#1", match.References.First(r => r.Property == "UnattendedRobot.UserName").CredentialStore);
    }

    [Fact]
    public void Assets_global_per_user_and_owner_rows()
    {
        var matcher = new AccountMatcher(["svc_rpa"]);
        var tenant = FindAccountReferenceCmdlet.ScanTenant(Users(), Robots(), Stores, matcher, @"T:\");
        Asset[] assets =
        [
            new() { Name = "Cred", Path = @"T:\F", ValueType = "Credential", CredentialUsername = "svc_rpa@corp.example.com", CredentialStoreId = 1,
                    UserValues = [ new() { UserName = "bob", ValueType = "Credential", CredentialUsername = @"CORP\svc_rpa", CredentialStoreId = 7 } ] },
            new() { Name = "Text", Path = @"T:\F", ValueType = "Text", Value = "svc_rpa",
                    UserValues = [ new() { UserName = "alice", ValueType = "Text", Value = "x" }, new() { UserName = "bob", ValueType = "Text", Value = "y" } ] },
        ];

        var rows = FindAccountReferenceCmdlet.ScanAssets(assets, matcher, tenant).ToList();
        Assert.Collection(rows,
            r => { Assert.Equal("CredentialUsername", r.Property); Assert.Equal("Orchestrator Database", r.CredentialStore); Assert.Equal(@"T:\F\Cred", r.Path); },
            r => { Assert.Equal("UserValues.CredentialUsername", r.Property); Assert.Equal("bob", r.Via); Assert.Equal("CyberArk", r.CredentialStore); },
            r => { Assert.Equal("UserValues.UserName", r.Property); Assert.Equal("alice", r.Via); Assert.Equal(@"CORP\svc_rpa", r.Account); Assert.Equal("Text", r.Name); });
    }

    [Fact]
    public void Triggers_follow_matched_users_and_robots()
    {
        var tenant = FindAccountReferenceCmdlet.ScanTenant(Users(), Robots(), Stores, new AccountMatcher(["svc_rpa"]), @"T:\");
        ProcessSchedule[] triggers =
        [
            // the trigger list carries RobotId only; RobotUserName comes back empty
            new() { Name = "Modern", Path = @"T:\F", MachineRobots = [ new() { RobotId = 11, MachineName = "M1" }, new() { RobotId = 99 } ] },
            new() { Name = "Classic", Path = @"T:\F", ExecutorRobots = [ new() { Id = 12, Name = "classic-1" }, new() { Id = 13, Name = "classic-2" } ] },
            new() { Name = "Any", Path = @"T:\F" },
        ];

        var rows = FindAccountReferenceCmdlet.ScanTriggers(triggers, tenant).ToList();
        Assert.Collection(rows,
            r => { Assert.Equal("Classic", r.Name); Assert.Equal("ExecutorRobots", r.Property); Assert.Equal("classic-1", r.Via); },
            r => { Assert.Equal("Modern", r.Name); Assert.Equal("MachineRobots", r.Property); Assert.Equal("alice-unattended", r.Via); Assert.Equal(@"CORP\svc_rpa", r.Account); });
    }
}
