using UiPath.PowerShell.Entities;
using Xunit;
using static UiPath.PowerShell.Core.OrchProvider;

namespace UnitTests;

// Reproduction + policy tests for ResolveDstEntryPointByPath -- the named entry-point
// resolver CopyProcesses uses to remap a Release's EntryPointId across feeds.
//
// The bug: CopyProcesses matched the destination entry point with a case-sensitive
// inline '==' on Path. When the dst feed exposed the same entry point under a
// different case (e.g. a package rebuilt so "Main.xaml" became "main.xaml"), the
// match missed, ResolveDstEntryPointByPath-equivalent returned null, and the copied
// process got a null EntryPointId -- losing its configured entry point. The match now
// routes through ResolveDstByName (OrdinalIgnoreCase), consistent with the FindDst*
// family.
//
// ResolvesDifferentlyCasedPath is the regression test: it FAILS against the original
// case-sensitive '==' and passes with the case-insensitive resolver.
public class ResolveDstEntryPointByPathTests
{
    private static PackageEntryPoint Ep(string? path, long id) =>
        new() { Path = path, Id = id };

    [Fact]
    public void ResolvesExactPath()
    {
        var dst = new[] { Ep("Main.xaml", 7), Ep("Sub/Flow.xaml", 8) };
        var got = ResolveDstEntryPointByPath(dst, "Main.xaml");
        Assert.NotNull(got);
        Assert.Equal(7, got!.Id);
    }

    [Fact]
    public void ResolvesDifferentlyCasedPath()
    {
        // The regression: src path "main.xaml" must resolve the dst "Main.xaml".
        // Case-sensitive '==' (the original bug) returns null here -> null EntryPointId.
        var dst = new[] { Ep("Sub/Flow.xaml", 8), Ep("Main.xaml", 7) };
        var got = ResolveDstEntryPointByPath(dst, "main.xaml");
        Assert.NotNull(got);
        Assert.Equal(7, got!.Id);
    }

    [Fact]
    public void ReturnsNullWhenNoPathMatches()
    {
        var dst = new[] { Ep("Main.xaml", 7) };
        var got = ResolveDstEntryPointByPath(dst, "Other.xaml");
        Assert.Null(got);
    }

    [Fact]
    public void ReturnsNullWhenSrcPathIsNull()
    {
        var dst = new[] { Ep("Main.xaml", 7) };
        var got = ResolveDstEntryPointByPath(dst, null);
        Assert.Null(got);
    }

    [Fact]
    public void ReturnsNullWhenDstEntryPointsIsNull()
    {
        var got = ResolveDstEntryPointByPath(null, "Main.xaml");
        Assert.Null(got);
    }
}
