using System.Collections.Generic;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Pins OrchProvider.HasSubfolders — the engine asks this (via HasChildItems) to decide
// whether to enumerate a container during wildcard path resolution (Resolve-Path Orch1:\Shar*,
// Get-ChildItem Orch1:\*) and whether Remove-Item must prompt for -Recurse. Returning a constant
// here once broke ALL wildcard resolution (52bea61a -> aa1eeee7), so the accuracy is load-bearing.
public class HasSubfoldersTests
{
    private static Folder F(string fqn) => new() { FullyQualifiedName = fqn, DisplayName = fqn };

    // A small catalog:  Shared, Shared/A, Shared/A/Deep, Empty, Other
    private static readonly List<Folder> Catalog =
    [
        F("Shared"),
        F("Shared/A"),
        F("Shared/A/Deep"),
        F("Empty"),
        F("Other"),
    ];

    [Theory]
    [InlineData("", true)]                 // drive root has depth-1 children (Shared, Empty, Other)
    [InlineData("Shared", true)]           // has Shared/A
    [InlineData("Shared/A", true)]         // has Shared/A/Deep
    [InlineData("Shared/A/Deep", false)]   // leaf
    [InlineData("Empty", false)]           // no children -> Remove-Item must NOT prompt for -Recurse
    [InlineData("Other", false)]
    public void HasSubfolders_ReportsDirectChildrenAccurately(string fqn, bool expected)
    {
        Assert.Equal(expected, OrchProvider.HasSubfolders(Catalog, fqn));
    }

    [Fact]
    public void HasSubfolders_MatchesOnlyDirectChildren_NotGrandchildren()
    {
        // "Shared" with ONLY a grandchild present (Shared/A/Deep) but no direct child must be false:
        // a prefix match is not enough; the child must be exactly one level deeper.
        var grandchildOnly = new List<Folder> { F("Shared"), F("Shared/A/Deep") };
        Assert.False(OrchProvider.HasSubfolders(grandchildOnly, "Shared"));
    }

    [Fact]
    public void HasSubfolders_PrefixIsNotConfusedBySiblingNameOverlap()
    {
        // "Share" must not count "Shared" as its child even though "Shared".StartsWith("Share").
        var folders = new List<Folder> { F("Share"), F("Shared") };
        Assert.False(OrchProvider.HasSubfolders(folders, "Share"));
    }

    [Fact]
    public void HasSubfolders_IsCaseInsensitiveOnPath()
    {
        var folders = new List<Folder> { F("Shared"), F("Shared/Sub") };
        Assert.True(OrchProvider.HasSubfolders(folders, "shared"));
    }
}
