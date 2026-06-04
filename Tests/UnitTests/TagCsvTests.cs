using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Tag serializes to "DisplayName=DisplayValue" for CSV via ConvertToString. That logic used
// to live on Tag.ToString(), but the override made ConvertTo-Json emit the string form for a
// Tag nested beyond -Depth instead of the structured object. These tests pin that the CSV
// form is preserved AND that Tag no longer overrides ToString.
public class TagCsvTests
{
    [Fact]
    public void ConvertToString_joins_name_value_pairs()
    {
        var tags = new[]
        {
            new Tag { Name = "env", DisplayName = "env", Value = "prod", DisplayValue = "prod" },
            new Tag { Name = "team", DisplayName = "team", Value = "x", DisplayValue = "x" },
        };
        Assert.Equal("env=prod,team=x", tags.ConvertToString());
    }

    [Fact]
    public void ConvertToString_handles_null_and_partial_tags()
    {
        Assert.Null(((Tag[]?)null).ConvertToString());
        // empty DisplayName -> empty element
        Assert.Equal("", new[] { new Tag { DisplayName = "", Value = "v", DisplayValue = "v" } }.ConvertToString());
        // empty Value -> just the DisplayName, no '='
        Assert.Equal("env", new[] { new Tag { DisplayName = "env", Value = "", DisplayValue = "" } }.ConvertToString());
    }

    [Fact]
    public void Tag_does_not_override_ToString()
    {
        // Regression guard: a ToString override would re-introduce the beyond-depth
        // ConvertTo-Json bug. Without it, ToString falls back to the type name.
        var t = new Tag { DisplayName = "env", Value = "prod", DisplayValue = "prod" };
        Assert.Equal(typeof(Tag).ToString(), t.ToString());
    }
}
