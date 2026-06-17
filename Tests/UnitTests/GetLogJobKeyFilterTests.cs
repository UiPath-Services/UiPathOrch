using System;
using UiPath.PowerShell.Commands;
using Xunit;

namespace UnitTests;

// Pins Get-OrchLog -JobKey OData $filter construction (v1.9.3). JobKey maps to an
// Edm.Guid field and is interpolated UNQUOTED into the $filter, so it must be
// validated as a GUID — otherwise a crafted value could alter the filter
// expression. Parsing also normalizes the value to its canonical form, which is
// what neutralizes any injection payload.
public class GetLogJobKeyFilterTests
{
    [Theory]
    [InlineData("12345678-1234-1234-1234-1234567890ab")]   // canonical "D"
    [InlineData("12345678-1234-1234-1234-1234567890AB")]   // uppercase -> lowercased
    [InlineData("{12345678-1234-1234-1234-1234567890ab}")] // "B" braces -> stripped
    [InlineData("(12345678-1234-1234-1234-1234567890ab)")] // "P" parens -> stripped
    [InlineData("123456781234123412341234567890ab")]       // "N" no-hyphens -> hyphenated
    public void Valid_guid_produces_canonical_unquoted_clause(string input)
    {
        var clause = GetLogCmdlet.BuildJobKeyFilterClause(input);
        // Every accepted spelling normalizes to the same canonical form, and no
        // braces / parens / quotes from the input leak into the filter.
        Assert.Equal($"(JobKey eq {Guid.Parse(input)})", clause);
        Assert.DoesNotContain("{", clause);
        Assert.DoesNotContain("(JobKey eq (", clause);
        Assert.DoesNotContain("'", clause);
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("123")]
    [InlineData("12345678-1234-1234-1234-1234567890ab) or (1 eq 1")] // OData injection attempt
    [InlineData("') or LogLevel eq 'Error")]                         // quote-break injection attempt
    [InlineData("12345678-1234-1234-1234-1234567890ab eq 1")]
    public void Non_guid_throws_argument_exception(string input)
    {
        var ex = Assert.Throws<ArgumentException>(() => GetLogCmdlet.BuildJobKeyFilterClause(input));
        Assert.Contains(input, ex.Message);
    }
}
