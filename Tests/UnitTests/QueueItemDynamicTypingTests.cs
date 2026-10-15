using System.Collections.Generic;
using System.Text.Json;
using UiPath.PowerShell.Commands;
using Xunit;

namespace UnitTests;

// Import-OrchQueueItem -DynamicTyping against what the web "Upload Items" with Dynamic
// typing ON stored for TestData/QueueItemUploadMatrix 01_main.csv,
// 02_dynamic_typing.csv and 03_dynamic_typing_followup.csv (Automation Cloud,
// 2026-10-15). Expected values are the stored SpecificContent JSON.
public class QueueItemDynamicTypingTests
{
    private static string Value(string cell)
    {
        var json = ImportQueueItemCmdlet.BuildQueueItemJson(new[] { "Value" }, new[] { cell }, 2, new List<CSVParseError>(), dynamicTyping: true);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("SpecificContent").GetProperty("Value").GetRawText();
    }

    private static string Reference(string cell)
    {
        var json = ImportQueueItemCmdlet.BuildQueueItemJson(new[] { "Reference" }, new[] { cell }, 2, new List<CSVParseError>(), dynamicTyping: true);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("Reference").GetString()!;
    }

    [Theory]
    // null
    [InlineData("", "null")]
    [InlineData("   ", "\"   \"")]
    [InlineData("null", "\"null\"")]
    [InlineData("NULL", "\"NULL\"")]
    [InlineData("undefined", "\"undefined\"")]
    // booleans
    [InlineData("true", "true")]
    [InlineData("false", "false")]
    [InlineData("TRUE", "true")]
    [InlineData("FALSE", "false")]
    [InlineData("True", "\"True\"")]
    [InlineData("False", "\"False\"")]
    [InlineData("tRuE", "\"tRuE\"")]
    [InlineData("yes", "\"yes\"")]
    [InlineData("on", "\"on\"")]
    [InlineData("t", "\"t\"")]
    [InlineData("true ", "\"true \"")]
    [InlineData(" false", "\" false\"")]
    [InlineData("TRUE1", "\"TRUE1\"")]
    // numbers
    [InlineData("0", "0")]
    [InlineData("-5", "-5")]
    [InlineData("007", "7")]
    [InlineData("-007", "-7")]
    [InlineData("00", "0")]
    [InlineData("-0", "0")]
    [InlineData("1.0", "1")]
    [InlineData("1.50", "1.5")]
    [InlineData("100.50", "100.5")]
    [InlineData(".5", "0.5")]
    [InlineData("-.5", "-0.5")]
    [InlineData("5.", "5")]
    [InlineData("1.", "1")]
    [InlineData("1e3", "1000")]
    [InlineData("1E3", "1000")]
    [InlineData("1e+3", "1000")]
    [InlineData("0.1e1", "1")]
    [InlineData("-1e-3", "-0.001")]
    [InlineData("0.1", "0.1")]
    [InlineData("3.14159265358979323846", "3.141592653589793")]
    [InlineData(" 5", "5")]
    [InlineData("5 ", "5")]
    [InlineData("\t5", "5")]
    [InlineData("20261015", "20261015")]
    [InlineData("2026", "2026")]
    [InlineData("1e15", "1000000000000000")]
    [InlineData("9007199254740991", "9007199254740991")]
    [InlineData("-9007199254740991", "-9007199254740991")]
    [InlineData("1234567890123456", "1234567890123456")]
    [InlineData("9007199254740992", "\"9007199254740992\"")]
    [InlineData("-9007199254740992", "\"-9007199254740992\"")]
    [InlineData("9007199254740991.5", "\"9007199254740991.5\"")]
    [InlineData("12345678901234567", "\"12345678901234567\"")]
    [InlineData("12345678901234567890", "\"12345678901234567890\"")]
    [InlineData("1e16", "\"1e16\"")]
    [InlineData("1e20", "\"1e20\"")]
    [InlineData("1e400", "\"1e400\"")]
    [InlineData("+5", "\"+5\"")]
    [InlineData("0x1F", "\"0x1F\"")]
    [InlineData("0b101", "\"0b101\"")]
    [InlineData("0o17", "\"0o17\"")]
    [InlineData("1,000", "\"1,000\"")]
    [InlineData("1_000", "\"1_000\"")]
    [InlineData("５", "\"５\"")]
    [InlineData("Infinity", "\"Infinity\"")]
    [InlineData("NaN", "\"NaN\"")]
    [InlineData("123abc", "\"123abc\"")]
    [InlineData("1e", "\"1e\"")]
    [InlineData("-", "\"-\"")]
    [InlineData("$5", "\"$5\"")]
    [InlineData("1/2", "\"1/2\"")]
    // dates
    [InlineData("2026-10-15T10:00:00+09:00", "\"2026-10-15T01:00:00Z\"")]
    [InlineData("2026-10-15T10:00:00.123+09:00", "\"2026-10-15T01:00:00Z\"")]
    [InlineData("2026-10-15T10:00:00.5+09:00", "\"2026-10-15T01:00:00Z\"")]
    [InlineData("2026-10-15T10:00:00-05:00", "\"2026-10-15T15:00:00Z\"")]
    [InlineData("2026-10-15T23:30:00-01:00", "\"2026-10-16T00:30:00Z\"")]
    [InlineData("2026-10-15T10:00:00+14:00", "\"2026-10-14T20:00:00Z\"")]
    [InlineData("2026-10-15T10:00:00Z", "\"2026-10-15T10:00:00Z\"")]
    [InlineData("2026-10-15T10:00:00.1234567Z", "\"2026-10-15T10:00:00Z\"")]
    [InlineData("2026-10-15T10:00:00.999Z", "\"2026-10-15T10:00:00Z\"")]
    [InlineData("2026-02-30T00:00:00Z", "\"2026-03-02T00:00:00Z\"")]
    [InlineData("2026-04-31T00:00:00Z", "\"2026-05-01T00:00:00Z\"")]
    [InlineData("2026-02-29T00:00:00Z", "\"2026-03-01T00:00:00Z\"")]
    [InlineData("2026-10-15T24:00:00Z", "\"2026-10-16T00:00:00Z\"")]
    [InlineData("1970-01-01T00:00:00Z", "\"1970-01-01T00:00:00Z\"")]
    [InlineData("0001-01-01T00:00:00Z", "\"0001-01-01T00:00:00Z\"")]
    [InlineData("2026-10-15T10:60:00Z", "\"2026-10-15T10:60:00Z\"")]
    [InlineData("2026-10-15T10:00:60Z", "\"2026-10-15T10:00:60Z\"")]
    [InlineData("2026-10-15T10:00:00", "\"2026-10-15T10:00:00\"")]
    [InlineData("2026-10-15T10:00", "\"2026-10-15T10:00\"")]
    [InlineData("2026-10-15 10:00:00", "\"2026-10-15 10:00:00\"")]
    [InlineData("2026-10-15 10:00:00+09:00", "\"2026-10-15 10:00:00+09:00\"")]
    [InlineData("2026-10-15T10:00:00+0900", "\"2026-10-15T10:00:00+0900\"")]
    [InlineData("2026-10-15T10:00:00+09", "\"2026-10-15T10:00:00+09\"")]
    [InlineData("2026-10-15t10:00:00z", "\"2026-10-15t10:00:00z\"")]
    [InlineData("+002026-10-15T10:00:00Z", "\"+002026-10-15T10:00:00Z\"")]
    [InlineData("2026-10-15", "\"2026-10-15\"")]
    [InlineData("2026/10/15", "\"2026/10/15\"")]
    [InlineData("10/15/2026", "\"10/15/2026\"")]
    [InlineData("2026-10", "\"2026-10\"")]
    // strings untouched
    [InlineData("{\"a\":1,\"b\":[1,2]}", "\"{\\u0022a\\u0022:1,\\u0022b\\u0022:[1,2]}\"")]
    [InlineData("  lead and trail  ", "\"  lead and trail  \"")]
    public void Value_MatchesWeb(string cell, string expectedJson)
    {
        Assert.Equal(Normalize(expectedJson), Normalize(Value(cell)));
    }

    // The web turns these into "undefinedZ" (a web defect); the cmdlet keeps them as written.
    [Theory]
    [InlineData("2026-13-01T00:00:00Z")]
    [InlineData("2026-00-10T00:00:00Z")]
    [InlineData("2026-10-32T00:00:00Z")]
    [InlineData("2026-10-15T25:00:00Z")]
    public void Value_OutOfRangeDate_KeptAsWritten(string cell)
    {
        Assert.Equal(cell, JsonSerializer.Deserialize<string>(Value(cell)));
    }

    [Theory]
    [InlineData("007", "7")]
    [InlineData("1.50", "1.5")]
    [InlineData(" 5 ", "5")]
    [InlineData("TRUE", "true")]
    [InlineData("true", "true")]
    [InlineData("9007199254740993", "9007199254740993")]
    [InlineData("", "")]
    [InlineData("R-01", "R-01")]
    // The web stores the browser's local Date.toString() here (a web defect); kept as written.
    [InlineData("2026-10-15T10:00:00+09:00", "2026-10-15T10:00:00+09:00")]
    [InlineData("2026-10-15T10:00:00Z", "2026-10-15T10:00:00Z")]
    public void Reference_MatchesWeb(string cell, string expected)
    {
        Assert.Equal(expected, Reference(cell));
    }

    [Fact]
    public void Off_KeepsEveryValueAsString()
    {
        var json = ImportQueueItemCmdlet.BuildQueueItemJson(new[] { "Reference", "Value" }, new[] { "007", "" }, 2, new List<CSVParseError>());
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("007", doc.RootElement.GetProperty("Reference").GetString());
        Assert.Equal("", doc.RootElement.GetProperty("SpecificContent").GetProperty("Value").GetString());
    }

    [Theory]
    [InlineData(0.5, "0.5")]
    [InlineData(-0.001, "-0.001")]
    [InlineData(1e-7, "1e-7")]
    [InlineData(1.5e-10, "1.5e-10")]
    [InlineData(123.456, "123.456")]
    [InlineData(3.141592653589793, "3.141592653589793")]
    [InlineData(0.000001, "0.000001")]
    public void JsNumberToString_MatchesEcmaScript(double d, string expected)
    {
        Assert.Equal(expected, QueueItemDynamicTyping.JsNumberToString(d));
    }

    // Compare JSON tokens, not escaping style.
    private static string Normalize(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.ValueKind == JsonValueKind.String
            ? "S:" + doc.RootElement.GetString()
            : doc.RootElement.ValueKind + ":" + doc.RootElement.GetRawText();
    }
}
