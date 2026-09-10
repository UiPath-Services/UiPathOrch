using System.Management.Automation;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Update-OrchProcessVersion must not re-issue a version change when the release is already on
// the requested (or latest) version — otherwise every invocation is a no-op audit entry. These
// pin the pure, API-free decision predicates used by both the -Id and -Name code paths.
public class UpdateProcessVersionDirtyTests
{
    private static Release Rel(bool isLatest, string? currentVersion) => new()
    {
        IsLatestVersion = isLatest,
        CurrentVersion = currentVersion is null ? null : new ReleaseVersion { VersionNumber = currentVersion },
    };

    // --- ShouldUpdateReleaseToLatest ---

    [Fact]
    public void ToLatest_AlreadyLatest_IsFalse()
    {
        Assert.False(UpdateProcessVersionCmdlet.ShouldUpdateReleaseToLatest(Rel(isLatest: true, "1.0.0")));
    }

    [Fact]
    public void ToLatest_NotLatest_IsTrue()
    {
        Assert.True(UpdateProcessVersionCmdlet.ShouldUpdateReleaseToLatest(Rel(isLatest: false, "1.0.0")));
    }

    [Fact]
    public void ToLatest_NullIsLatest_IsTrue()
    {
        // IsLatestVersion unknown (null) -> treat as "not known latest" -> proceed.
        Assert.True(UpdateProcessVersionCmdlet.ShouldUpdateReleaseToLatest(new Release { IsLatestVersion = null }));
    }

    [Fact]
    public void ToLatest_NullRelease_IsTrue()
    {
        // Release not found in the folder -> proceed and let the API report it.
        Assert.True(UpdateProcessVersionCmdlet.ShouldUpdateReleaseToLatest(null));
    }

    // --- ShouldUpdateReleaseToVersion ---

    [Fact]
    public void ToVersion_SameAsCurrent_IsFalse()
    {
        Assert.False(UpdateProcessVersionCmdlet.ShouldUpdateReleaseToVersion(Rel(isLatest: false, "1.2.3"), "1.2.3"));
    }

    [Fact]
    public void ToVersion_DifferentFromCurrent_IsTrue()
    {
        Assert.True(UpdateProcessVersionCmdlet.ShouldUpdateReleaseToVersion(Rel(isLatest: false, "1.2.3"), "2.0.0"));
    }

    [Fact]
    public void ToVersion_NullCurrentVersion_IsTrue()
    {
        Assert.True(UpdateProcessVersionCmdlet.ShouldUpdateReleaseToVersion(Rel(isLatest: false, null), "1.0.0"));
    }

    [Fact]
    public void ToVersion_NullRelease_IsTrue()
    {
        Assert.True(UpdateProcessVersionCmdlet.ShouldUpdateReleaseToVersion(null, "1.0.0"));
    }

    [Theory]
    [InlineData("1.0.0", "1.0.0.0")]
    [InlineData("1.0.0.0", "1.0.0")]
    [InlineData("1.0.07", "1.0.7")]
    public void ToVersion_SameNumberDifferentText_IsFalse(string deployed, string resolved)
    {
        // The deployed version comes from the Releases endpoint and the resolved one from the
        // package feed. Comparing them as text re-issues an update that changes nothing.
        Assert.False(UpdateProcessVersionCmdlet.ShouldUpdateReleaseToVersion(Rel(isLatest: false, deployed), resolved));
    }

    // --- ResolveTargetVersion ---
    //
    // Both the -Id and -Name paths resolve a wildcard -Version through this. It takes the LAST
    // match because OrchDriveInfo's PackageVersions cache hands back versions sorted ASCENDING
    // with VersionComparer -- these feed it in that same order.

    private static string? Resolve(string pattern, params string[] versions) =>
        UpdateProcessVersionCmdlet.ResolveTargetVersion(versions, new WildcardPattern(pattern, WildcardOptions.IgnoreCase));

    [Fact]
    public void Resolve_Wildcard_PicksNewestMatch()
    {
        // "the newest 2.0.x", not the newest overall and not the first 2.0.x.
        Assert.Equal("2.0.7", Resolve("2.0.*", "1.9.0", "2.0.1", "2.0.7", "3.1.0"));
    }

    [Fact]
    public void Resolve_Wildcard_IgnoresNewerNonMatching()
    {
        Assert.Equal("2.0.1", Resolve("2.0.*", "2.0.1", "2.1.0", "10.0.0"));
    }

    [Fact]
    public void Resolve_ExactVersion_PicksThatVersion()
    {
        // A -Version with no wildcard character still goes through the pattern.
        Assert.Equal("1.0.4", Resolve("1.0.4", "1.0.3", "1.0.4", "1.0.5"));
    }

    [Fact]
    public void Resolve_NoMatch_IsNull()
    {
        // Caller must skip (and say so under -Verbose) rather than send the pattern to the API.
        Assert.Null(Resolve("9.*", "1.0.0", "2.0.0"));
    }

    [Fact]
    public void Resolve_Empty_IsNull()
    {
        Assert.Null(Resolve("1.*"));
    }

    [Fact]
    public void Resolve_IsCaseInsensitive()
    {
        Assert.Equal("1.0.0-Beta.2", Resolve("1.0.0-beta.*", "1.0.0-Beta.1", "1.0.0-Beta.2"));
    }

    [Fact]
    public void Resolve_StarMatchesEverything_PicksLast()
    {
        // "*" degenerates to "the newest version in the feed".
        Assert.Equal("3.0.0", Resolve("*", "1.0.0", "2.0.0", "3.0.0"));
    }
}
