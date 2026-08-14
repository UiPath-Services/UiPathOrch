using System.IO;
using UiPath.PowerShell.Core;
using Xunit;

namespace UnitTests;

// Tests for OrchProvider.ResolveConfigPath — the resolver behind the
// UIPATHORCH_CONFIG_PATH override and Import-OrchConfig -ConfigPath.
//
// The resolver is deliberately file-system-free: it runs on every
// GetConfigFilePath call, including provider initialization during module
// load, where probing a path that turns out to be an unreachable network
// share would block with no way to report why. That is what makes it a pure
// function worth testing directly. It does not decide whether a value names
// the file or the folder holding it — it offers both readings, and the read
// (TryReadConfigFile, exercised in ConfigPathReadFallbackTests) picks.
public class ConfigPathResolutionTests
{
    private static readonly string Default =
        Path.Combine(Path.GetTempPath(), "default", "UiPathOrchConfig.json");

    private static string Rooted(params string[] parts) =>
        Path.Combine(Path.GetTempPath(), Path.Combine(parts));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void UnsetOrBlank_UsesDefault(string? raw)
    {
        var r = OrchProvider.ResolveConfigPath(raw, Default);

        Assert.Equal(Default, r.Path);
        Assert.False(r.IsOverride);
        Assert.Null(r.Warning);
    }

    [Fact]
    public void RootedFile_IsUsedAsIs()
    {
        string value = Rooted("share", "UiPathOrchConfig.json");

        var r = OrchProvider.ResolveConfigPath(value, Default);

        Assert.Equal(value, r.Path);
        Assert.True(r.IsOverride);
        Assert.Null(r.Warning);
    }

    [Fact]
    public void SurroundingWhitespace_IsTrimmed()
    {
        string value = Rooted("share", "UiPathOrchConfig.json");

        var r = OrchProvider.ResolveConfigPath($"  {value}  ", Default);

        Assert.Equal(value, r.Path);
        Assert.True(r.IsOverride);
    }

    // A trailing separator says "folder" outright, so there is nothing left to try.
    [Fact]
    public void TrailingSeparator_AppendsTheDefaultFileNameAndOffersNoAlternative()
    {
        string folder = Rooted("share", "team") + Path.DirectorySeparatorChar;

        var r = OrchProvider.ResolveConfigPath(folder, Default);

        Assert.Equal(Path.Combine(folder, "UiPathOrchConfig.json"), r.Path);
        Assert.Null(r.FolderCandidate);
        Assert.True(r.IsOverride);
    }

    // Without a trailing separator the value could be either, and the resolver does not
    // guess from the spelling: it offers both, file first. The read picks, falling back
    // to the folder reading only when the file reading finds nothing.
    [Theory]
    [InlineData("team")]                    // looks like a folder
    [InlineData("UiPathOrchConfig.json")]   // looks like a file
    [InlineData("prod.json")]
    [InlineData("v1.2")]                    // a folder whose name looks like a file
    [InlineData("myconfig")]                // a file whose name looks like a folder
    public void WithoutATrailingSeparator_BothReadingsAreOffered(string leaf)
    {
        string value = Rooted("share", leaf);

        var r = OrchProvider.ResolveConfigPath(value, Default);

        Assert.Equal(value, r.Path);
        Assert.Equal(Path.Combine(value, "UiPathOrchConfig.json"), r.FolderCandidate);
        Assert.True(r.IsOverride);
    }

    [Fact]
    public void DriveRoot_IsAFolder()
    {
        if (!OperatingSystem.IsWindows()) return;

        var r = OrchProvider.ResolveConfigPath(@"C:\", Default);

        Assert.Equal(@"C:\UiPathOrchConfig.json", r.Path);
        Assert.Null(r.FolderCandidate);
        Assert.True(r.IsOverride);
    }

    // Nothing to disambiguate for the default location, so no second candidate — and
    // the read must not wander off to <default>\UiPathOrchConfig.json\UiPathOrchConfig.json.
    [Fact]
    public void DefaultLocation_HasNoFolderCandidate()
    {
        var r = OrchProvider.ResolveConfigPath(null, Default);

        Assert.Null(r.FolderCandidate);
        Assert.False(r.IsOverride);
    }

    // Falling back to the default rather than erroring keeps a typo in a machine-wide
    // variable from bricking every session on the box; the warning is how the user finds out.
    [Theory]
    [InlineData("UiPathOrchConfig.json")]
    [InlineData(@".\UiPathOrchConfig.json")]
    [InlineData("sub/UiPathOrchConfig.json")]
    public void RelativePath_IsIgnoredWithAWarning(string raw)
    {
        var r = OrchProvider.ResolveConfigPath(raw, Default);

        Assert.Equal(Default, r.Path);
        Assert.False(r.IsOverride);
        Assert.NotNull(r.Warning);
        Assert.Contains(raw, r.Warning);
        Assert.Contains("UIPATHORCH_CONFIG_PATH", r.Warning);
    }

    // SetEnvironmentVariable writes REG_SZ, which the OS does NOT expand at process
    // start — unlike a REG_EXPAND_SZ value typed into the System Properties UI. Both
    // spellings have to behave the same.
    [Fact]
    public void EmbeddedEnvironmentVariables_AreExpanded()
    {
        // %NAME% is the Windows spelling; skip rather than assert a shape that only
        // matters on the platform where the System Properties UI can produce it.
        if (!OperatingSystem.IsWindows()) return;

        const string name = "UIPATHORCH_TEST_CONFIG_ROOT";
        string root = Rooted("expanded");
        System.Environment.SetEnvironmentVariable(name, root);
        try
        {
            var r = OrchProvider.ResolveConfigPath($"%{name}%{Path.DirectorySeparatorChar}UiPathOrchConfig.json", Default);

            Assert.Equal(Path.Combine(root, "UiPathOrchConfig.json"), r.Path);
            Assert.True(r.IsOverride);
        }
        finally
        {
            System.Environment.SetEnvironmentVariable(name, null);
        }
    }

    [Fact]
    public void UnresolvableEnvironmentVariable_LeavesTheLiteralAndIsRejectedAsRelative()
    {
        if (!OperatingSystem.IsWindows()) return;

        var r = OrchProvider.ResolveConfigPath("%UIPATHORCH_TEST_NO_SUCH_VAR%/UiPathOrchConfig.json", Default);

        // ExpandEnvironmentVariables leaves an unknown %NAME% in place, so the value stays
        // relative — better to fall back with a warning than to read from a literal "%NAME%" dir.
        Assert.Equal(Default, r.Path);
        Assert.False(r.IsOverride);
        Assert.NotNull(r.Warning);
    }

    [Fact]
    public void UncPath_IsRootedAndOverrides()
    {
        // A backslash is not a separator on Unix, so \\server\share is a relative
        // path there — the UNC contract only exists on Windows.
        if (!OperatingSystem.IsWindows()) return;

        const string unc = @"\\server\share\team\UiPathOrchConfig.json";

        var r = OrchProvider.ResolveConfigPath(unc, Default);

        Assert.Equal(unc, r.Path);
        Assert.True(r.IsOverride);
        Assert.Null(r.Warning);
    }
}
