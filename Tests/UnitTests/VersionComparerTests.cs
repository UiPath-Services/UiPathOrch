using UiPath.PowerShell.Core;
using Xunit;

namespace UnitTests;

public class VersionComparerTests
{
    private readonly VersionComparer _comparer = VersionComparer.Instance;

    [Theory]
    [InlineData("1.0.0", "1.0.0", 0)]
    [InlineData("1.0.0", "2.0.0", -1)]
    [InlineData("2.0.0", "1.0.0", 1)]
    [InlineData("1.0.0", "1.1.0", -1)]
    [InlineData("1.0.0", "1.0.1", -1)]
    [InlineData("1.2.3", "1.2.3", 0)]
    public void Compare_MajorMinorPatch(string x, string y, int expectedSign)
    {
        int result = _comparer.Compare(x, y);
        Assert.Equal(expectedSign, Math.Sign(result));
    }

    [Theory]
    [InlineData("1.0.0", "1.0.0-beta", 1)]       // release > prerelease
    [InlineData("1.0.0-alpha", "1.0.0-beta", -1)] // alpha < beta
    [InlineData("1.0.0-beta.1", "1.0.0-beta.2", -1)]
    public void Compare_PreReleaseStages(string x, string y, int expectedSign)
    {
        int result = _comparer.Compare(x, y);
        Assert.Equal(expectedSign, Math.Sign(result));
    }

    [Theory]
    [InlineData("1.0.0.1", "1.0.0.2", -1)]
    [InlineData("1.0.0.5", "1.0.0.5", 0)]
    public void Compare_FourPartVersions(string x, string y, int expectedSign)
    {
        int result = _comparer.Compare(x, y);
        Assert.Equal(expectedSign, Math.Sign(result));
    }

    [Fact]
    public void Compare_NullHandling()
    {
        Assert.Equal(0, _comparer.Compare(null, null));
        Assert.True(_comparer.Compare(null, "1.0.0") > 0);
        Assert.True(_comparer.Compare("1.0.0", null) < 0);
    }

    // --- AreSameVersion ---
    //
    // Callers resolve a -Version wildcard as text, then ask this whether the release is already
    // there. The two strings come from different endpoints (Releases vs the package feed), so the
    // question is about numbers, not spelling.

    [Theory]
    [InlineData("1.0.0", "1.0.0")]
    [InlineData("1.0.0", "1.0.0.0")]     // trailing revision zero is the same release
    [InlineData("1.0.07", "1.0.7")]      // leading zero is the same number
    [InlineData("1.0.0-Beta", "1.0.0-beta")] // NuGet prerelease labels are case-insensitive
    public void AreSameVersion_True(string x, string y)
    {
        Assert.True(_comparer.AreSameVersion(x, y));
        Assert.True(_comparer.AreSameVersion(y, x));
    }

    [Theory]
    [InlineData("1.0.0", "1.0.1")]
    [InlineData("1.0.0", "1.0.0-beta")]  // release is not its own prerelease
    [InlineData("1.0.0.1", "1.0.0.2")]
    [InlineData("2.0.0", "10.0.0")]
    public void AreSameVersion_False(string x, string y)
    {
        Assert.False(_comparer.AreSameVersion(x, y));
        Assert.False(_comparer.AreSameVersion(y, x));
    }

    [Fact]
    public void AreSameVersion_UnparseableStringsDoNotCollapse()
    {
        // Both parse as 0.0.0 under the grammar, so a numeric-only check would call them equal
        // and skip a real update. They fall back to a string comparison instead.
        Assert.False(_comparer.AreSameVersion("1.0.0-beta-2", "1.0.0-rc-1"));
        Assert.True(_comparer.AreSameVersion("1.0.0-beta-2", "1.0.0-beta-2"));
    }

    [Fact]
    public void AreSameVersion_NullHandling()
    {
        Assert.True(_comparer.AreSameVersion(null, null));
        Assert.False(_comparer.AreSameVersion(null, "1.0.0"));
        Assert.False(_comparer.AreSameVersion("1.0.0", null));
    }

    [Fact]
    public void Sort_ProducesCorrectOrder()
    {
        var versions = new[] { "2.0.0", "1.0.0-beta", "1.0.0", "1.0.0-alpha", "3.1.0" };
        var sorted = versions.OrderBy(v => v, _comparer).ToArray();
        Assert.Equal("1.0.0-alpha", sorted[0]);
        Assert.Equal("1.0.0-beta", sorted[1]);
        Assert.Equal("1.0.0", sorted[2]);
        Assert.Equal("2.0.0", sorted[3]);
        Assert.Equal("3.1.0", sorted[4]);
    }
}
