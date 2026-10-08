using UiPath.PowerShell.Commands;
using Xunit;

namespace UnitTests;

// Pins NewPmExternalApplicationCmdlet.BuildCommand: the CreateExternalClientCommand payload.
public class NewPmExternalApplicationTests
{
    [Fact]
    public void Application_and_user_scopes_get_their_types()
    {
        var cmd = NewPmExternalApplicationCmdlet.BuildCommand("pg", "App", true, null, ["OR.Jobs", "OR.Assets"], ["OR.Users"]);

        Assert.Equal("pg", cmd.partitionGlobalId);
        Assert.Equal("App", cmd.name);
        Assert.True(cmd.isConfidential);
        Assert.Null(cmd.redirectUri);
        Assert.Equal([("OR.Jobs", 1), ("OR.Assets", 1), ("OR.Users", 0)], cmd.scopes!.Select(s => (s.name!, s.type!.Value)));
    }

    [Fact]
    public void Space_separated_scopes_are_split_and_deduplicated()
    {
        var cmd = NewPmExternalApplicationCmdlet.BuildCommand("pg", "App", true, null, ["OR.Jobs OR.Assets", "or.jobs"], null);
        Assert.Equal(["OR.Jobs", "OR.Assets"], cmd.scopes!.Select(s => s.name!));
    }

    [Fact]
    public void Non_confidential_with_redirect()
    {
        var cmd = NewPmExternalApplicationCmdlet.BuildCommand("pg", "App", false, "http://localhost:8765/", null, ["OR.Jobs"]);
        Assert.False(cmd.isConfidential);
        Assert.Equal("http://localhost:8765/", cmd.redirectUri);
        Assert.Single(cmd.scopes!);
        Assert.Equal(0, cmd.scopes![0].type);
    }
}
