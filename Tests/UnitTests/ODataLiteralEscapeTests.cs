using UiPath.PowerShell.Core;
using Xunit;

namespace UnitTests;

// PathTools.EscapeODataLiteral escapes a value for insertion INSIDE an OData
// single-quoted string literal (e.g. ProcessName eq '<value>'). OData escapes a
// single quote by doubling it, so a name like "O'Brien" must become "O''Brien"
// or the filter is malformed. The helper adds NO surrounding quotes and does NOT
// URL-encode; it must be applied exactly once on the raw value.
public class ODataLiteralEscapeTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("plain", "plain")]
    [InlineData("O'Brien", "O''Brien")]
    [InlineData("a'b'c", "a''b''c")]
    [InlineData("'", "''")]
    [InlineData("no quotes here", "no quotes here")]
    public void Doubles_single_quotes(string? input, string expected)
        => Assert.Equal(expected, PathTools.EscapeODataLiteral(input));

    [Fact]
    public void Does_not_add_surrounding_quotes()
    {
        // The caller supplies the quotes (eq '<value>'); the helper must not.
        Assert.Equal("plain", PathTools.EscapeODataLiteral("plain"));
    }

    [Fact]
    public void Applying_twice_double_escapes_dont_do_this()
    {
        // Contract guard: escape the RAW value exactly once. Applying the helper
        // to an already-escaped value doubles the doubling, which is wrong.
        var once = PathTools.EscapeODataLiteral("O'Brien");
        Assert.Equal("O''Brien", once);
        Assert.Equal("O''''Brien", PathTools.EscapeODataLiteral(once));
    }
}
