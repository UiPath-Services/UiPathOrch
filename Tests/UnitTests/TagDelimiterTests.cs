using System.Linq;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// A1 regression: ConvertToTags split each element on a raw ',', so a tag whose value contained a
// comma was mis-split into two garbage tags (and the CSV / pipe round-trip lost it). FormatTag now
// backtick-escapes commas and ConvertToTags splits on unescaped commas + unescapes, so such a tag
// survives the round-trip; an unescaped comma still separates tags (the CSV multi-tag convention).
public class TagDelimiterTests
{
    [Fact]
    public void Tag_with_comma_in_value_round_trips()
    {
        var tag = new Tag { Name = "env", Value = "a,b", DisplayName = "env", DisplayValue = "a,b" };
        string formatted = OrchStringExtensions.FormatTag(tag)!;     // "env=a`,b"
        var back = OrchStringExtensions.ConvertToTags(new[] { formatted }).ToList();
        Assert.Single(back);
        Assert.Equal("env", back[0].Name);
        Assert.Equal("a,b", back[0].Value);
    }

    [Fact]
    public void Unescaped_comma_still_separates_tags()
    {
        var back = OrchStringExtensions.ConvertToTags(new[] { "k1=v1,k2=v2" }).ToList();
        Assert.Equal(2, back.Count);
        Assert.Equal("k1", back[0].Name);
        Assert.Equal("v1", back[0].Value);
        Assert.Equal("k2", back[1].Name);
        Assert.Equal("v2", back[1].Value);
    }

    [Fact]
    public void Escaped_comma_keeps_one_tag()
    {
        var back = OrchStringExtensions.ConvertToTags(new[] { "k=a`,b" }).ToList();
        Assert.Single(back);
        Assert.Equal("k", back[0].Name);
        Assert.Equal("a,b", back[0].Value);
    }

    [Fact]
    public void Equals_in_value_is_preserved()
    {
        // '=' inside a value is safe -- ConvertToTags splits on the first '=' only.
        var back = OrchStringExtensions.ConvertToTags(new[] { "url=https://x?a=b" }).ToList();
        Assert.Single(back);
        Assert.Equal("url", back[0].Name);
        Assert.Equal("https://x?a=b", back[0].Value);
    }
}
