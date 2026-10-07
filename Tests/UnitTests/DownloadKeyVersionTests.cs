using UiPath.OrchAPI;
using Xunit;

namespace UnitTests;

// The library and package listings report versions with SemVer build metadata
// ("1.1.1+1385.220515020318.release.39f4e48"), but DownloadPackage's key takes the version
// without it: the full string is a 404 on 24.10.8, raw or with '+' escaped, and "1.1.1" downloads
// (2026-10-07). Get-OrchLibraryDependency failed on such a library for that reason.
public class DownloadKeyVersionTests
{
    [Theory]
    [InlineData("1.1.1+1385.220515020318.release.39f4e48", "1.1.1")]
    [InlineData("2.0.0-beta.1+build.5", "2.0.0-beta.1")]
    [InlineData("1.0.3", "1.0.3")]
    [InlineData("24.10.0-preview", "24.10.0-preview")]
    [InlineData("1.0.0+", "1.0.0")]
    public void Drops_build_metadata_only(string version, string expected)
        => Assert.Equal(expected, OrchAPISession.DownloadKeyVersion(version));
}
