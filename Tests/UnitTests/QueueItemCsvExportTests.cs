using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Get-OrchQueueItem -ExportCsv (QueueItemCsvFormat) and how Import-OrchQueueItem reads the
// file back: the web "Upload Items" columns, the '@' history columns, and the @Status guard.
public class QueueItemCsvExportTests
{
    private static QueueItem Item(long id, string status, string? reference, params (string key, object? value)[] content) => new()
    {
        Id = id,
        Status = status,
        Reference = reference,
        Priority = "Normal",
        SpecificContent = content.ToDictionary(c => c.key, c => (object)JsonSerializer.SerializeToElement(c.value)),
    };

    [Fact]
    public void Build_UploadColumnsThenHistory_KeysInFirstSeenOrder()
    {
        var (headers, rows) = QueueItemCsvFormat.Build(
        [
            Item(1, "Successful", "R-1", ("A", "a1"), ("B", "b1")),
            Item(2, "New", null, ("C", "c2"), ("A", "a2")),
        ], out var unrepresentable);

        Assert.Empty(unrepresentable);
        Assert.Equal(new[] { "Reference", "Priority", "A", "B", "C" }, headers.Take(5));
        Assert.Equal(QueueItemCsvFormat.HistoryColumns, headers.Skip(5));
        Assert.Equal(new[] { "R-1", "Normal", "a1", "b1", "" }, rows[0].Take(5));
        Assert.Equal(new[] { "", "Normal", "a2", "", "c2" }, rows[1].Take(5));
        int status = headers.IndexOf("@Status"), id = headers.IndexOf("@Id");
        Assert.Equal("Successful", rows[0][status]);
        Assert.Equal("2", rows[1][id]);
    }

    [Fact]
    public void Build_ValueKinds()
    {
        var (headers, rows) = QueueItemCsvFormat.Build(
        [
            Item(1, "New", "R",
                ("s", "007"), ("n", 1.5), ("t", true), ("f", false), ("z", null),
                ("o", new Dictionary<string, object> { ["k"] = "日本" }), ("a", new[] { 1, 2 })),
        ], out _);
        var cell = headers.Zip(rows[0]).ToDictionary(p => p.First, p => p.Second);
        Assert.Equal("007", cell["s"]);
        Assert.Equal("1.5", cell["n"]);
        Assert.Equal("true", cell["t"]);
        Assert.Equal("false", cell["f"]);
        Assert.Equal("", cell["z"]);
        Assert.Equal("{\"k\":\"日本\"}", cell["o"]);
        Assert.Equal("[1,2]", cell["a"]);
    }

    [Fact]
    public void Build_FallsBackToSpecificDataAndOutputData()
    {
        var item = new QueueItem
        {
            Id = 5,
            Status = "Failed",
            SpecificData = "{\"DynamicProperties\":{\"N\":3,\"Name\":\"x\"}}",
            OutputData = "{\"DynamicProperties\":{\"Result\":\"NG\"}}",
            ProcessingExceptionType = "BusinessException",
            ProcessingException = new ProcessingException { Reason = "bad", Details = "d" },
        };
        var (headers, rows) = QueueItemCsvFormat.Build([item], out _);
        var cell = headers.Zip(rows[0]).ToDictionary(p => p.First, p => p.Second);
        Assert.Equal("3", cell["N"]);
        Assert.Equal("x", cell["Name"]);
        Assert.Equal("{\"Result\":\"NG\"}", cell["@Output"]);
        Assert.Equal("BusinessException", cell["@ProcessingExceptionType"]);
        Assert.Equal("bad", cell["@ProcessingExceptionReason"]);
    }

    // The server returns a date-time string in SpecificContent converted to UTC but keeps it as
    // written in SpecificData (seen on 25.10.2 and 20.10.16); the export takes SpecificData.
    [Fact]
    public void Build_PrefersSpecificDataOverTheParsedDictionary()
    {
        var item = Item(1, "New", "R", ("Value", "2026-10-15T01:00:00Z"));
        item.SpecificData = "{\"DynamicProperties\":{\"Value\":\"2026-10-15T10:00:00+09:00\"}}";
        var (headers, rows) = QueueItemCsvFormat.Build([item], out _);
        Assert.Equal("2026-10-15T10:00:00+09:00", rows[0][headers.IndexOf("Value")]);
    }

    [Theory]
    [InlineData("plain", "plain")]
    [InlineData("", "")]
    [InlineData("   ", "\"   \"")]          // Import-Csv drops unquoted whitespace
    [InlineData(" lead", "\" lead\"")]
    [InlineData("trail ", "\"trail \"")]
    [InlineData("in side", "in side")]
    [InlineData("a,b", "\"a,b\"")]
    [InlineData("say \"hi\"", "\"say \"\"hi\"\"\"")]
    [InlineData("x\r\ny", "\"x\r\ny\"")]
    public void CsvCell_QuotesWhenNeeded(string value, string expected)
    {
        Assert.Equal(expected, QueueItemCsvFormat.CsvCell(value));
    }

    [Fact]
    public void Build_KeysThatCannotHaveAColumn_AreReported()
    {
        var (headers, _) = QueueItemCsvFormat.Build(
            [Item(1, "New", "R", ("Reference", "x"), ("@Status", "y"), ("Ok", "z"))], out var unrepresentable);
        Assert.Equal(new[] { "Reference", "@Status" }, unrepresentable);
        Assert.Single(headers, "Reference");
        Assert.Single(headers, "@Status");
        Assert.Contains("Ok", headers);
    }

    [Theory]
    [InlineData(@"Orch1:\Shared\Sub", "Invoices", "Orch1_Shared_Sub_Invoices.csv")]
    [InlineData(@"Orch1:\", "Q", "Orch1_Q.csv")]
    [InlineData(@"Orch1:\Dev", "a/b", "Orch1_Dev_a_b.csv")]
    public void FileNameFor_FlattensThePath(string folder, string queue, string expected)
    {
        Assert.Equal(expected, QueueItemCsvFormat.FileNameFor(folder, queue));
    }

    [Fact]
    public void SelectRows_SkipsRowsThatAreNotNew()
    {
        var headers = new List<string> { "Reference", "@Status" };
        var contents = new List<List<string>>
        {
            new() { "a", "New" }, new() { "b", "Successful" }, new() { "c", "" }, new() { "d", "Failed" }, new() { "e", "new" },
        };
        var selected = ImportQueueItemCmdlet.SelectRows(headers, contents, out var skipped);
        Assert.Equal(new[] { 1, 3, 5 }, selected.Select(s => s.row));
        Assert.Equal(new[] { 2, 4 }, skipped);

        // To queue them again, the user removes the @Status column: every row is selected.
        var all = ImportQueueItemCmdlet.SelectRows(
            new List<string> { "Reference" }, contents.Select(r => new List<string> { r[0] }).ToList(), out skipped);
        Assert.Equal(5, all.Count);
        Assert.Empty(skipped);
    }

    [Fact]
    public void SelectRows_WithoutStatusColumn_KeepsEveryRow()
    {
        var selected = ImportQueueItemCmdlet.SelectRows(
            new List<string> { "Reference", "Status" },
            new List<List<string>> { new() { "a", "Successful" } }, out var skipped);
        Assert.Single(selected);
        Assert.Empty(skipped);
    }

    [Fact]
    public void ImportJson_LeavesHistoryColumnsOutOfSpecificContent()
    {
        var errors = new List<CSVParseError>();
        var json = ImportQueueItemCmdlet.BuildQueueItemJson(
            new[] { "Reference", "Value", "@Status", "@Output", "@Other" },
            new[] { "R", "v", "New", "{}", "x" }, 2, errors);
        using var doc = JsonDocument.Parse(json);
        var sc = doc.RootElement.GetProperty("SpecificContent");
        Assert.Equal(new[] { "Value", "@Other" }, sc.EnumerateObject().Select(p => p.Name));
    }

    // Export, then read the file the way Import-OrchQueueItem does: the same Reference and
    // SpecificContent text come back, and only the New item is selected.
    [Fact]
    public void RoundTrip_ThroughTheImportParser()
    {
        var items = new[]
        {
            Item(1, "New", "R,1", ("Text", "a \"quoted\", value"), ("Lines", "x\r\ny"), ("Space", "  s  ")),
            Item(2, "Successful", "R2", ("Text", "t"), ("Num", 7)),
        };
        var (headers, rows) = QueueItemCsvFormat.Build(items, out _);

        var path = Path.Combine(Path.GetTempPath(), $"qi-export-{System.Guid.NewGuid():N}.csv");
        try
        {
            using (var writer = OrchestratorPSCmdlet.WriteCsvHeader(path, null, headers.Select(QueueItemCsvFormat.CsvCell).ToArray())!)
            {
                foreach (var row in rows) writer.WriteCsvLine(row.Select(QueueItemCsvFormat.CsvCell));
            }

            Assert.Equal(',', UiPath.PowerShell.Commands.CsvHelper.MultilineCsv.DetectDelimiter(path, Encoding.UTF8));
            ImportQueueItemCmdlet.ParseCsvContent(path, Encoding.UTF8, out var readHeaders, out var contents, out var errors);
            Assert.Empty(errors);
            Assert.Equal(headers, readHeaders);
            Assert.Equal(rows.Select(r => r.ToList()), contents);

            var selected = ImportQueueItemCmdlet.SelectRows(readHeaders, contents, out var skipped);
            Assert.Equal(new[] { 2 }, skipped);
            var json = ImportQueueItemCmdlet.BuildQueueItemJson(readHeaders, selected.Single().values, 2, new List<CSVParseError>());
            using var doc = JsonDocument.Parse(json);
            Assert.Equal("R,1", doc.RootElement.GetProperty("Reference").GetString());
            var sc = doc.RootElement.GetProperty("SpecificContent");
            Assert.Equal("a \"quoted\", value", sc.GetProperty("Text").GetString());
            Assert.Equal("x\r\ny", sc.GetProperty("Lines").GetString());
            Assert.Equal("  s  ", sc.GetProperty("Space").GetString());
            Assert.DoesNotContain(sc.EnumerateObject(), p => p.Name.StartsWith('@'));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
