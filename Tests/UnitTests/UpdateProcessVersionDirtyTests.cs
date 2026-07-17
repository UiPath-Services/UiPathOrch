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
}
