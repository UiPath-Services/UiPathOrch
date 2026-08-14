using System;
using System.IO;
using UiPath.PowerShell.Core;
using Xunit;

namespace UnitTests;

// Tests for OrchProvider.TryReadConfigFile — the half of the configuration-path
// feature that settles whether UIPATHORCH_CONFIG_PATH named the file or the folder
// holding it. The resolver offers both readings (ConfigPathResolutionTests); this
// one decides, by trying the file reading first and the folder reading only when
// the file is NOT THERE.
//
// Every call passes bypassMemo so the module-load memo — shared static state, and
// xunit runs classes in parallel — cannot leak one test's answer into another.
//
// The temp tree is a class fixture, not per-test state: xunit builds a fresh instance
// for every [Fact], and creating then recursively deleting a directory seven times is
// slow enough to show up in the suite's wall clock on a machine with on-access
// scanning. Each test owns a distinct name underneath, so one tree is enough.
public sealed class ConfigPathTempTree : IDisposable
{
    public string Root { get; }

    public ConfigPathTempTree()
    {
        Root = Path.Combine(Path.GetTempPath(), "UiPathOrchConfigPathTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
    }

    public void Dispose()
    {
        try { Directory.Delete(Root, recursive: true); } catch { /* best effort */ }
    }
}

public sealed class ConfigPathReadFallbackTests : IClassFixture<ConfigPathTempTree>
{
    private readonly string _root;

    public ConfigPathReadFallbackTests(ConfigPathTempTree tree) => _root = tree.Root;

    private static OrchProvider.ConfigPathResolution Resolve(string value) =>
        OrchProvider.ResolveConfigPath(value, Path.Combine(Path.GetTempPath(), "unused-default.json"));

    [Fact]
    public void APathThatIsAFile_IsReadAsAFile()
    {
        string file = Path.Combine(_root, "prod.json");
        File.WriteAllText(file, "{ \"marker\": \"file\" }");

        bool ok = OrchProvider.TryReadConfigFile(
            Resolve(file), out string? json, out string effective, out string? error, bypassMemo: true);

        Assert.True(ok, error ?? "");
        Assert.Equal(file, effective);
        Assert.Contains("\"file\"", json);
    }

    [Fact]
    public void APathThatIsAFolder_FallsBackToTheStandardFileNameInside()
    {
        string folder = Path.Combine(_root, "team");
        Directory.CreateDirectory(folder);
        string inside = Path.Combine(folder, "UiPathOrchConfig.json");
        File.WriteAllText(inside, "{ \"marker\": \"folder\" }");

        bool ok = OrchProvider.TryReadConfigFile(
            Resolve(folder), out string? json, out string effective, out string? error, bypassMemo: true);

        Assert.True(ok, error ?? "");
        Assert.Equal(inside, effective);
        Assert.Contains("\"folder\"", json);
    }

    // The two shapes the old lexical rule got wrong. Trying rather than guessing is the
    // whole point of the fallback, so both have to work.
    [Fact]
    public void AnExtensionlessFile_IsStillReadAsAFile()
    {
        string file = Path.Combine(_root, "myconfig");
        File.WriteAllText(file, "{ \"marker\": \"extensionless-file\" }");

        bool ok = OrchProvider.TryReadConfigFile(
            Resolve(file), out string? json, out string effective, out string? error, bypassMemo: true);

        Assert.True(ok, error ?? "");
        Assert.Equal(file, effective);
        Assert.Contains("extensionless-file", json);
    }

    [Fact]
    public void AFolderNamedLikeAFile_IsStillReadAsAFolder()
    {
        string folder = Path.Combine(_root, "v1.2");
        Directory.CreateDirectory(folder);
        string inside = Path.Combine(folder, "UiPathOrchConfig.json");
        File.WriteAllText(inside, "{ \"marker\": \"dotted-folder\" }");

        bool ok = OrchProvider.TryReadConfigFile(
            Resolve(folder), out string? json, out string effective, out string? error, bypassMemo: true);

        Assert.True(ok, error ?? "");
        Assert.Equal(inside, effective);
        Assert.Contains("dotted-folder", json);
    }

    // A folder that exists but holds no configuration file is the case most likely to
    // send someone looking in the wrong place, so the error names both attempts.
    [Fact]
    public void NeitherReadingAnswers_TheErrorNamesBothPaths()
    {
        string missing = Path.Combine(_root, "nothing-here");

        bool ok = OrchProvider.TryReadConfigFile(
            Resolve(missing), out string? json, out string effective, out string? error, bypassMemo: true);

        Assert.False(ok);
        Assert.Null(json);
        Assert.Equal(missing, effective);   // report the path as written, not the invented one
        Assert.NotNull(error);
        Assert.Contains(missing, error);
        Assert.Contains(Path.Combine(missing, "UiPathOrchConfig.json"), error);
    }

    // A trailing separator resolves to the folder reading up front, so there is no
    // second candidate and the error must not offer one.
    [Fact]
    public void AnExplicitFolderThatIsEmpty_ReportsOnlyThatPath()
    {
        string folder = Path.Combine(_root, "empty") + Path.DirectorySeparatorChar;
        Directory.CreateDirectory(folder);

        bool ok = OrchProvider.TryReadConfigFile(
            Resolve(folder), out _, out string effective, out string? error, bypassMemo: true);

        Assert.False(ok);
        Assert.Equal(Path.Combine(folder, "UiPathOrchConfig.json"), effective);
        Assert.DoesNotContain("read as a file", error);
    }

    // Malformed content is an answer, not a reason to look elsewhere: the file reading
    // succeeded, so the folder reading must not run and shadow it.
    [Fact]
    public void AFileThatExists_IsReturnedEvenWhenItsContentIsNotJson()
    {
        string folder = Path.Combine(_root, "both");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "UiPathOrchConfig.json"), "{ \"marker\": \"inside\" }");

        string file = Path.Combine(_root, "both.json");
        File.WriteAllText(file, "not json at all");

        bool ok = OrchProvider.TryReadConfigFile(
            Resolve(file), out string? json, out string effective, out string? error, bypassMemo: true);

        Assert.True(ok, error ?? "");
        Assert.Equal(file, effective);
        Assert.Equal("not json at all", json);
    }
}
