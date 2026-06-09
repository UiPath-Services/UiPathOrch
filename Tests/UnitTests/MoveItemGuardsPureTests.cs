using Xunit;
using static UiPath.PowerShell.Core.OrchProvider;

namespace UnitTests;

// Unit tests for the MoveItem guard helpers (IsCrossDriveMovePure / IsMoveIntoSelfOrDescendantPure)
// extracted from OrchProvider.MoveItem. Move-Item is single-drive (unlike Copy-Item), and a folder
// must not be moved into itself or a descendant. These pin the two rejection decisions — including
// the sibling-boundary and case-insensitivity edges — without needing a live drive.
public class MoveItemGuardsPureTests
{
    // ---- cross-drive ----

    [Theory]
    [InlineData("Orch1", "Orch2", true)]   // different drive -> cross-drive, reject
    [InlineData("Orch1", "Orch1", false)]  // same drive -> allowed
    [InlineData("Orch1", "orch1", false)]  // same drive, different case -> allowed (case-insensitive)
    [InlineData("Orch1", null, false)]     // unqualified destination -> same-drive
    [InlineData("Orch1", "", false)]       // empty destination drive -> same-drive
    public void IsCrossDriveMove(string srcDrive, string? dstDrive, bool expected)
        => Assert.Equal(expected, IsCrossDriveMovePure(srcDrive, dstDrive));

    // ---- self / descendant ----

    [Theory]
    [InlineData("Foo", "Foo", true)]            // self
    [InlineData("Foo", "Foo/Bar", true)]        // direct child
    [InlineData("Foo", "Foo/Bar/Baz", true)]    // deep descendant
    [InlineData("foo", "FOO/bar", true)]        // case-insensitive
    [InlineData("Foo", "Foo2", false)]          // sibling — NOT a descendant (the '/' boundary)
    [InlineData("Foo", "FooBar", false)]        // name-prefix but not a path descendant
    [InlineData("Foo", "Bar", false)]           // unrelated
    [InlineData("Foo/Bar", "Foo", false)]       // moving INTO an ancestor is allowed
    [InlineData("A/B", "A/B/C", true)]          // nested source, descendant
    [InlineData("A/B", "A/C", false)]           // nested source, sibling subtree
    [InlineData(null, "Foo", false)]            // null source FQN (e.g. root) -> never a match
    [InlineData("", "Foo", false)]              // empty source FQN -> never a match
    [InlineData("Foo", null, false)]            // null destination FQN
    public void IsMoveIntoSelfOrDescendant(string? srcFqn, string? dstFqn, bool expected)
        => Assert.Equal(expected, IsMoveIntoSelfOrDescendantPure(srcFqn, dstFqn));
}
