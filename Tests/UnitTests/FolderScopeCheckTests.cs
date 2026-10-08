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
    [InlineData("PM.User.Read", null, null, false)]              // no Orchestrator scope at all: not this drive's purpose
    [InlineData("OR.Jobs", "secret-pat", null, false)]           // PAT: rights fixed server-side
    [InlineData("OR.Jobs", null, "password", false)]             // user/password
    [InlineData(null, null, null, false)]
    public void Lacks_folder_scope(string? scope, string? accessToken, string? password, bool expected)
    {
        var drive = new PSDrive { Scope = scope, AccessToken = accessToken, Password = password };
        Assert.Equal(expected, OrchProvider.LacksFolderScope(drive));
    }
}
