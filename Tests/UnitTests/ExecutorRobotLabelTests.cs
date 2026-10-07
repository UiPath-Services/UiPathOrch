using System.Linq;
using System.Management.Automation;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// A modern-folder robot on 20.10 (API 11) is user-based: /odata/Robots lists it with Username
// ("localhost\hoge") and NO Name. The -ExportCsv ExecutorRobots cell was written from Name alone,
// so it came out empty, and re-importing the row cleared the trigger's assignment. Found on
// 20.10.16, 2026-10-06; 1.18.0 behaves the same.
public class ExecutorRobotLabelTests
{
    private static readonly Robot Named = new() { Id = 1, Name = "Robot A", Username = "corp\\a" };
    private static readonly Robot Nameless = new() { Id = 11, Name = null, Username = "localhost\\hoge" };

    [Fact]
    public void A_named_robot_is_written_by_its_name()
        => Assert.Equal("Robot A", OrchestratorPSCmdlet.ExecutorRobotLabel(Named));

    [Fact]
    public void A_robot_without_a_name_is_written_by_its_username()
        => Assert.Equal("localhost\\hoge", OrchestratorPSCmdlet.ExecutorRobotLabel(Nameless));

    [Fact]
    public void A_robot_with_neither_writes_nothing()
        => Assert.Null(OrchestratorPSCmdlet.ExecutorRobotLabel(new Robot { Id = 2, Name = "", Username = "" }));

    [Fact]
    public void Reading_back_matches_a_name_first()
    {
        // "corp\a" is Named's username; a robot NAMED that must win over the username match.
        var namedLikeUser = new Robot { Id = 3, Name = "corp\\a" };
        var hits = OrchestratorPSCmdlet.MatchExecutorRobots([Named, namedLikeUser],
            new WildcardPattern(WildcardPattern.Escape("corp\\a"), WildcardOptions.IgnoreCase)).ToList();
        Assert.Equal(new long?[] { 3 }, hits.Select(r => r.Id).ToArray());
    }

    [Fact]
    public void Reading_back_falls_back_to_the_username()
    {
        var hits = OrchestratorPSCmdlet.MatchExecutorRobots([Named, Nameless],
            new WildcardPattern(WildcardPattern.Escape(OrchestratorPSCmdlet.ExecutorRobotLabel(Nameless)!), WildcardOptions.IgnoreCase)).ToList();
        Assert.Equal(new long?[] { 11 }, hits.Select(r => r.Id).ToArray());
    }

    [Fact]
    public void Local_account_types_are_the_20_10_ones()
    {
        Assert.True(OrchProvider.IsLocalAccountType("User"));
        Assert.True(OrchProvider.IsLocalAccountType("robot"));
        Assert.False(OrchProvider.IsLocalAccountType("DirectoryUser"));
        Assert.False(OrchProvider.IsLocalAccountType("DirectoryRobot"));
        Assert.False(OrchProvider.IsLocalAccountType(null));
    }
}
