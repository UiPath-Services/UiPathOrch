using UiPath.PowerShell.Core;
using Xunit;

namespace UnitTests;

// The cmdlets that open an Orchestrator page returned without a word over SSH to a Linux
// server (no X11/Wayland): the URL has to reach the user when no browser can.
public class BrowserLauncherTests
{
    [Theory]
    [InlineData(null, null, false)]
    [InlineData("", "", false)]
    [InlineData(":0", null, true)]
    [InlineData(null, "wayland-0", true)]
    public void Graphical_session_needs_display_or_wayland(string? display, string? wayland, bool expected)
        => Assert.Equal(expected, BrowserLauncher.HasGraphicalSession(display, wayland));

    [Fact]
    public void No_browser_message_carries_the_reason_and_the_url()
    {
        var msg = BrowserLauncher.BuildNoBrowserMessage("https://cloud.uipath.com/acme/t/orchestrator_/jobs?fid=1", "neither DISPLAY nor WAYLAND_DISPLAY is set");

        Assert.Contains("neither DISPLAY nor WAYLAND_DISPLAY is set", msg);
        Assert.EndsWith("https://cloud.uipath.com/acme/t/orchestrator_/jobs?fid=1", msg);
    }
}
