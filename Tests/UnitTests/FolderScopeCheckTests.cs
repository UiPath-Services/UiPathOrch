using UiPath.PowerShell.Core;
using Xunit;

namespace UnitTests;

// The drive whose Scope lacks a folder scope signs in and then cannot list a folder. The same check
// drives the mount-time warning and the note on the refused listing.
public class FolderScopeCheckTests
{
    [Theory]
    [InlineData("OR.Jobs OR.Settings.Read OR.Users.Read", null, null, true)]
    [InlineData("OR.Folders OR.Settings", null, null, false)]
    [InlineData("OR.Folders.Read OR.Settings.Read", null, null, false)]
    [InlineData("OR.Folders.Write OR.Users.Read", null, null, true)]   // Write alone cannot list (21.10.4)
    [InlineData("or.folders.read", null, null, false)]                 // case-insensitive
    [InlineData("OR.FoldersX OR.Jobs", null, null, true)]              // whole names, not substrings
    [InlineData("PM.User.Read", null, null, false)]              // no Orchestrator scope at all: not this drive's purpose
    [InlineData("OR.Jobs", "secret-pat", null, false)]           // PAT: rights fixed server-side
    [InlineData("OR.Jobs", null, "password", false)]             // user/password
    [InlineData(null, null, null, false)]
    public void Lacks_folder_scope(string? scope, string? accessToken, string? password, bool expected)
    {
        var drive = new PSDrive { Scope = scope, AccessToken = accessToken, Password = password };
        Assert.Equal(expected, OrchProvider.LacksFolderScope(drive));
    }

    [Theory]
    [InlineData("OR.Users", true)]
    [InlineData("OR.Users.Read", true)]
    [InlineData("OR.Users.Write", false)]
    [InlineData("OR.UsersX.Read", false)]
    [InlineData("", false)]
    public void Can_read_needs_the_resource_or_its_read_scope(string scope, bool expected)
        => Assert.Equal(expected, OrchProvider.CanRead(scope, "OR.Users"));
}
