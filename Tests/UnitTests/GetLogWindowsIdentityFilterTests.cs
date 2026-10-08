using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Pins RobotLogFilterCmdlet.BuildWindowsIdentityClause: one plain value filters the log's own
// WindowsIdentity; a wildcard or several values go through the folder's robots; no match
// yields null, which the caller turns into "this folder holds no matching log" -- it used to
// drop the condition and return every log.
public class GetLogWindowsIdentityFilterTests
{
    private static readonly UserRobots[] FolderRobots =
    [
        new() { UserName = @"uipath\yoshifumi.tsuda", RobotNames = ["ytsuda@gmail.com-unattended"] },
        new() { UserName = @"hoge\ff", RobotNames = ["myrobot2-unattended"] },
        new() { UserName = @"autogen\ytsuda@gmail.com_local", RobotNames = ["ytsuda@gmail.com-attended"] },
    ];

    [Fact]
    public void One_plain_value_filters_the_WindowsIdentity_field()
    {
        var clause = RobotLogFilterCmdlet.BuildWindowsIdentityClause([@"UIPATH\yoshifumi.tsuda"], FolderRobots);
        Assert.Equal("(WindowsIdentity eq 'UIPATH%5Cyoshifumi.tsuda')", clause);
    }

    [Fact]
    public void One_plain_value_unknown_to_the_folder_still_filters_by_field()
    {
        // An attended login is not a configured robot user; the field still finds its logs.
        var clause = RobotLogFilterCmdlet.BuildWindowsIdentityClause([@"CORP\alice"], FolderRobots);
        Assert.Equal("(WindowsIdentity eq 'CORP%5Calice')", clause);
    }

    [Fact]
    public void A_quote_in_the_value_is_escaped()
    {
        var clause = RobotLogFilterCmdlet.BuildWindowsIdentityClause([@"CORP\o'brien"], FolderRobots);
        // OData doubles the quote; the URL escape then encodes both.
        Assert.Equal("(WindowsIdentity eq 'CORP%5Co%27%27brien')", clause);
    }

    [Fact]
    public void A_wildcard_goes_through_the_folders_robots()
    {
        var clause = RobotLogFilterCmdlet.BuildWindowsIdentityClause([@"uipath\*"], FolderRobots);
        Assert.Equal("(RobotName eq 'ytsuda%40gmail.com-unattended')", clause);
    }

    [Fact]
    public void Several_values_go_through_the_folders_robots()
    {
        var clause = RobotLogFilterCmdlet.BuildWindowsIdentityClause([@"uipath\yoshifumi.tsuda", @"hoge\ff"], FolderRobots);
        Assert.Equal("(RobotName eq 'ytsuda%40gmail.com-unattended' or RobotName eq 'myrobot2-unattended')", clause);
    }

    [Fact]
    public void A_wildcard_matching_nothing_gives_null()
    {
        Assert.Null(RobotLogFilterCmdlet.BuildWindowsIdentityClause([@"nobody\*"], FolderRobots));
    }
}
