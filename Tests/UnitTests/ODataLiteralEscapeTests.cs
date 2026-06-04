using System;
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

    // Get-OrchLog (ProcessName/RobotName) and the TestSetExecution by-name lookup
    // build a $filter value as Uri.EscapeDataString(EscapeODataLiteral(value)):
    // the OData single quote is doubled FIRST, then the whole value is URL-encoded
    // so URL-reserved characters (& # space ...) can't break the query. Order
    // matters -- URL-encoding first would percent-encode the quote to %27 and the
    // OData doubling would no longer apply. These pin the exact wire form.
    [Theory]
    [InlineData("plain", "plain")]
    [InlineData("O'Brien", "O%27%27Brien")]              // '  -> '' -> %27%27
    [InlineData("A & B", "A%20%26%20B")]                 // space + & url-encoded
    [InlineData("C#Bot", "C%23Bot")]                     // # url-encoded
    [InlineData("O'Brien & Co", "O%27%27Brien%20%26%20Co")] // combined
    public void Filter_value_is_odata_escaped_then_url_encoded(string input, string expected)
        => Assert.Equal(expected, Uri.EscapeDataString(PathTools.EscapeODataLiteral(input)));
}
