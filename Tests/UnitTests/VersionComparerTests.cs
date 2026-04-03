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
