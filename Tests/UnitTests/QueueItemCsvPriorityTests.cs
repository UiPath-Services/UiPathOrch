using System.Collections.Generic;
using System.Text.Json;
using UiPath.PowerShell.Commands;
using Xunit;

namespace UnitTests;

// Priority mapping of Import-OrchQueueItem, pinned against what the web "Upload Items"
// did with TestData/QueueItemUploadMatrix (01_main.csv, 11_priority_medium.csv,
// 14_priority_spaced.csv).
public class QueueItemCsvPriorityTests
{
    private static (string? priority, List<CSVParseError> errors) Build(string value)
    {
        var errors = new List<CSVParseError>();
        var json = ImportQueueItemCmdlet.BuildQueueItemJson(new[] { "Priority" }, new[] { value }, 1, errors);
        using var doc = JsonDocument.Parse(json);
        return (doc.RootElement.TryGetProperty("Priority", out var p) ? p.GetString() : null, errors);
    }

    [Theory]
    [InlineData("High", "High")]
    [InlineData("high", "High")]
    [InlineData("HIGH", "High")]
    [InlineData("nOrMaL", "Normal")]
    [InlineData("1", "Low")]
    [InlineData("2", "Normal")]
    [InlineData("3", "High")]
    [InlineData(" High ", "High")]   // the web trims Priority
    [InlineData("\tLow\t", "Low")]
    public void AcceptedSpellings(string value, string expected)
    {
        var (priority, errors) = Build(value);
        Assert.Empty(errors);
        Assert.Equal(expected, priority);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]  // blank after trimming: omitted, like an empty cell
    public void Empty_IsOmitted(string value)
    {
        var (priority, errors) = Build(value);
        Assert.Empty(errors);
        Assert.Null(priority);
    }

    // Repeated headers, as the web renamed them (TestData/QueueItemUploadMatrix 23, 2g, 2h, 2i).
    [Theory]
    [InlineData("CaseId,Value,Value", "CaseId,Value,Value_1")]
    [InlineData("Value,Value,Value", "Value,Value_1,Value_2")]
    [InlineData("Value,Value_1,Value", "Value,Value_1,Value_2")]
    [InlineData("Value_1,Value,Value", "Value_1,Value,Value_2")]
    [InlineData("Reference,Reference,Priority,Priority", "Reference,Reference_1,Priority,Priority_1")]
    [InlineData("Value,Value,Value_1", "Value,Value_2,Value_1")]
    public void RenameDuplicateHeaders_MatchesWeb(string headers, string expected)
    {
        Assert.Equal(expected.Split(','), ImportQueueItemCmdlet.RenameDuplicateHeaders(headers.Split(',')));
    }

    [Fact]
    public void DuplicateReferenceAndPriority_SecondGoesToSpecificContent()
    {
        var errors = new List<CSVParseError>();
        var json = ImportQueueItemCmdlet.BuildQueueItemsArrayJson(
            new[] { "Reference", "Reference", "Priority", "Priority" },
            new IReadOnlyList<string>[] { new[] { "R-1", "R-2", "High", "Low" } }, errors);
        using var doc = JsonDocument.Parse(json);
        var item = doc.RootElement[0];
        Assert.Empty(errors);
        Assert.Equal("R-1", item.GetProperty("Reference").GetString());
        Assert.Equal("High", item.GetProperty("Priority").GetString());
        Assert.Equal("R-2", item.GetProperty("SpecificContent").GetProperty("Reference_1").GetString());
        Assert.Equal("Low", item.GetProperty("SpecificContent").GetProperty("Priority_1").GetString());
    }

    // An invalid Priority is numbered from the first data row, as the web does
    // ("CSV Row: 2" for line 3 in 11_priority_medium.csv and 12_priority_zero.csv).
    [Fact]
    public void InvalidPriority_CountsFromTheFirstDataRow()
    {
        var errors = new List<CSVParseError>();
        ImportQueueItemCmdlet.BuildQueueItemsArrayJson(
            new[] { "CaseId", "Priority" },
            new IReadOnlyList<string>[] { new[] { "A1", "Normal" }, new[] { "A2", "0" }, new[] { "A3", "Low" } }, errors);
        var e = Assert.Single(errors);
        Assert.Equal(2, e.CSVRow);
    }

    // 15,000 rows in one request failed on the server after 6 minutes, leaving part of
    // the items created (TestData/QueueItemUploadMatrix 51); rows now go 1,000 per request.
    [Theory]
    [InlineData(15000, "ProcessAllIndependently", 15, 1000)]
    [InlineData(2500, "StopOnFirstFailure", 3, 1000)]
    [InlineData(1000, "ProcessAllIndependently", 1, 1000)]
    [InlineData(15000, "AllOrNothing", 1, 15000)]   // splitting would break all-or-nothing
    [InlineData(15000, "allornothing", 1, 15000)]   // -CommitType is case-insensitive
    [InlineData(0, "ProcessAllIndependently", 0, 0)]
    public void SplitIntoRequests_ChunksExceptAllOrNothing(int rows, string commitType, int requests, int firstSize)
    {
        var items = new List<string>();
        for (int i = 0; i < rows; i++) items.Add($"{{\"n\":{i}}}");
        var split = ImportQueueItemCmdlet.SplitIntoRequests(items, commitType);
        Assert.Equal(requests, split.Count);
        if (requests > 0)
        {
            Assert.Equal(firstSize, split[0].items.Count);
            Assert.Equal(rows, split.Sum(r => r.items.Count));
            Assert.Equal(rows - split[^1].items.Count, split[^1].start);
        }
    }

    [Theory]
    [InlineData(null, "ProcessAllIndependently")]
    [InlineData("", "ProcessAllIndependently")]
    [InlineData("allornothing", "AllOrNothing")]
    [InlineData("STOPONFIRSTFAILURE", "StopOnFirstFailure")]
    [InlineData("Bogus", "Bogus")]   // left for the server to reject
    public void NormalizeCommitType_CanonicalSpelling(string? value, string expected)
    {
        Assert.Equal(expected, ImportQueueItemCmdlet.NormalizeCommitType(value));
    }

    [Theory]
    [InlineData("Medium")]
    [InlineData("0")]
    [InlineData("4")]
    public void Invalid_IsAFieldError(string value)
    {
        var (_, errors) = Build(value);
        Assert.Single(errors);
        Assert.Equal("Priority", errors[0].CSVField);
    }
}
