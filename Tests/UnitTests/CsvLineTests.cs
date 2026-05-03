using UiPath.PowerShell.Commands.CsvHelper;
using Xunit;

namespace UnitTests;

// R10: Regression coverage for OrchCsvHelper.CsvLine.Split — the single-line
// RFC 4180 splitter introduced when LoadUserMappingCsv / TestUserMappingCsv
// switched off `string.Split(',')`. The previous Split-based parsing destroyed
// quoted fields with embedded commas; these tests pin the new behavior so it
// can't silently regress.
public class CsvLineTests
{
    [Fact]
    public void Split_SimpleCommaSeparated_ReturnsFields()
    {
        Assert.Equal(new[] { "a", "b", "c" }, CsvLine.Split("a,b,c"));
    }

    [Fact]
    public void Split_SingleField_NoComma_ReturnsOneElement()
    {
        Assert.Equal(new[] { "abc" }, CsvLine.Split("abc"));
    }

    [Fact]
    public void Split_EmptyLine_ReturnsOneEmptyElement()
    {
        Assert.Equal(new[] { "" }, CsvLine.Split(""));
    }

    [Fact]
    public void Split_TrailingComma_PreservesEmptyLastField()
    {
        Assert.Equal(new[] { "a", "b", "" }, CsvLine.Split("a,b,"));
    }

    [Fact]
    public void Split_LeadingComma_PreservesEmptyFirstField()
    {
        Assert.Equal(new[] { "", "b", "c" }, CsvLine.Split(",b,c"));
    }

    [Fact]
    public void Split_AllEmpty_PreservesEmptyFields()
    {
        Assert.Equal(new[] { "", "", "" }, CsvLine.Split(",,"));
    }

    [Fact]
    public void Split_QuotedField_StripsSurroundingQuotes()
    {
        Assert.Equal(new[] { "a", "b", "c" }, CsvLine.Split("\"a\",\"b\",\"c\""));
    }

    [Fact]
    public void Split_QuotedFieldWithEmbeddedComma_KeepsCommaInsideField()
    {
        // The bug the new parser exists to fix: line.Split(',') would have
        // produced ["\"hello", "world\"", "z"] here.
        Assert.Equal(new[] { "hello,world", "z" }, CsvLine.Split("\"hello,world\",z"));
    }

    [Fact]
    public void Split_QuotedFieldWithEscapedDoubleQuote_UnescapesToSingleQuote()
    {
        // RFC 4180: "" inside a quoted field is a literal "
        Assert.Equal(new[] { "say \"hi\"", "z" }, CsvLine.Split("\"say \"\"hi\"\"\",z"));
    }

    [Fact]
    public void Split_MixedQuotedAndUnquoted_HandlesEach()
    {
        Assert.Equal(new[] { "a", "b,c", "d" }, CsvLine.Split("a,\"b,c\",d"));
    }

    [Fact]
    public void Split_QuotedEmptyField_ReturnsEmptyString()
    {
        Assert.Equal(new[] { "a", "", "c" }, CsvLine.Split("a,\"\",c"));
    }

    [Fact]
    public void Split_UnquotedFieldWithEmbeddedQuote_TreatsQuoteAsLiteral()
    {
        // Quote not at the start of a field is literal (sb.Length > 0 path).
        Assert.Equal(new[] { "ab\"cd", "e" }, CsvLine.Split("ab\"cd,e"));
    }

    [Fact]
    public void Split_PreservesSurroundingWhitespaceOnUnquotedFields()
    {
        // Documented: "Surrounding whitespace around unquoted fields is preserved
        // — callers that want trimming should do so explicitly."
        Assert.Equal(new[] { " a ", " b " }, CsvLine.Split(" a , b "));
    }

    [Theory]
    [InlineData("user@example.com,group,Manager", new[] { "user@example.com", "group", "Manager" })]
    [InlineData("\"Last, First\",user@x.com", new[] { "Last, First", "user@x.com" })]
    [InlineData("\"\"\"Allow\"\",\"\"Solutions\"\"\",x", new[] { "\"Allow\",\"Solutions\"", "x" })]
    public void Split_RealisticUserMappingCsvShapes(string line, string[] expected)
    {
        // Patterns drawn from the kinds of values that appear in PesterTest /
        // production user-mapping CSVs.
        Assert.Equal(expected, CsvLine.Split(line));
    }
}
