using System.Linq;
using UiPath.PowerShell.Core;
using Xunit;

namespace UnitTests;

// Orchestrator takes Windows time-zone ids only. On Linux/macOS the OS database is IANA:
// New-OrchTrigger's default sent "Etc/UTC" and was rejected, and -TimeZone resolved
// against, and completed from, the OS's IANA list (seen 2026-11-21 on RHEL 8.5).
public class OrchTimeZonesTests
{
    [Theory]
    [InlineData("Etc/UTC", "UTC")]
    [InlineData("Asia/Tokyo", "Tokyo Standard Time")]
    [InlineData("America/New_York", "Eastern Standard Time")]
    public void Linux_local_ids_become_windows_ids(string iana, string windows)
        => Assert.Equal(windows, OrchTimeZones.ToWindowsId(iana, isWindows: false));

    [Fact]
    public void An_id_with_no_windows_equivalent_falls_back_to_utc()
        => Assert.Equal("UTC", OrchTimeZones.ToWindowsId("Not/AZone", isWindows: false));

    [Fact]
    public void Windows_ids_are_kept_as_they_are()
        => Assert.Equal("Tokyo Standard Time", OrchTimeZones.ToWindowsId("Tokyo Standard Time", isWindows: true));

    [Fact]
    public void Every_listed_zone_has_a_windows_id()
    {
        // The list -TimeZone is resolved against and completed from: on every OS it must
        // yield ids Orchestrator accepts, with the Windows display names users see in the web.
        var zones = OrchTimeZones.All().ToList();

        Assert.Contains(zones, z => z.Id == "Tokyo Standard Time");
        Assert.Contains(zones, z => z.Id == "UTC");
        Assert.DoesNotContain(zones, z => z.Id.Contains('/'));
    }

    [Fact]
    public void The_local_default_is_a_windows_id()
        => Assert.DoesNotContain('/', OrchTimeZones.LocalId());
}
