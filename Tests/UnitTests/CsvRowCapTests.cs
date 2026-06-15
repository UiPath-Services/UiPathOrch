using System;
using System.IO;
using System.Text;
using UiPath.PowerShell.Commands;
using Xunit;

namespace UnitTests;

// Covers this round's CSV-import changes:
//
//  (1) Cap-aware streaming parse — Import-OrchQueueItem.ParseCsvContent and
//      Import-OrchTestDataQueueItem.ReadCsvCapped retain at most rowCap rows
//      (so an oversized file is never fully materialized) yet report the TRUE
//      total row count. The true total is what feeds the web-parity
//      "{rowCount} records" rejection message — so it must NOT be the capped
//      count. The pinned uncapped overloads still retain every row.
//
//  (2) Parser unification — Import-OrchTestDataQueueItem now shares
//      Import-OrchQueueItem's multi-line/quote-aware parser (was line-based
//      CsvLine.Split). So a quoted field with an embedded newline is ONE field
//      and the row count is records, not physical lines.
public class CsvRowCapTests
{
    // Write content to a temp .csv (no BOM), run the assertion, delete the file.
    private static void WithCsv(string content, Action<string> use)
    {
        var path = Path.Combine(Path.GetTempPath(), "uiorch_csvcap_" + Guid.NewGuid().ToString("N") + ".csv");
        File.WriteAllText(path, content, new UTF8Encoding(false));
        try { use(path); }
        finally { File.Delete(path); }
    }

    // Header + 5 data rows.
    private const string FiveRows =
        "h1,h2\n" +
        "a1,a2\n" +
        "b1,b2\n" +
        "c1,c2\n" +
        "d1,d2\n" +
        "e1,e2\n";

    // One quoted field spans two physical lines -> 2 records over 3 data lines.
    private const string MultilineCsv =
        "id,note\n" +
        "1,\"alpha\nbeta\"\n" +
        "2,gamma\n";

    // ---- (1) cap-aware streaming parse -------------------------------------

    [Fact]
    public void ParseCsvContent_RetainsAtMostRowCap_ButCountsTrueTotal()
    {
        WithCsv(FiveRows, path =>
        {
            ImportQueueItemCmdlet.ParseCsvContent(path, Encoding.UTF8, rowCap: 2,
                out var headers, out var contents, out var errors, out var total);
            Assert.Empty(errors);
            Assert.Equal(new[] { "h1", "h2" }, headers);
            Assert.Equal(2, contents.Count); // retained rows capped at rowCap
            Assert.Equal(5, total);          // true total still counted
        });
    }

    [Fact]
    public void ParseCsvContent_UncappedOverload_RetainsEveryRow()
    {
        WithCsv(FiveRows, path =>
        {
            // The pinned 5-arg overload delegates with rowCap = int.MaxValue.
            ImportQueueItemCmdlet.ParseCsvContent(path, Encoding.UTF8,
                out _, out var contents, out var errors);
            Assert.Empty(errors);
            Assert.Equal(5, contents.Count);
        });
    }

    [Fact]
    public void ReadCsvCapped_RetainsAtMostRowCap_ButCountsTrueTotal()
    {
        WithCsv(FiveRows, path =>
        {
            var (headers, rows, total) = ImportTestDataQueueItemCmdlet.ReadCsvCapped(path, Encoding.UTF8, rowCap: 2);
            Assert.Equal(new[] { "h1", "h2" }, headers);
            Assert.Equal(2, rows.Count);
            Assert.Equal(5, total);
        });
    }

    [Fact]
    public void ReadCsv_Uncapped_RetainsEveryRow()
    {
        WithCsv(FiveRows, path =>
        {
            var (_, rows) = ImportTestDataQueueItemCmdlet.ReadCsv(path, Encoding.UTF8);
            Assert.Equal(5, rows.Count);
        });
    }

    // ---- (2) multi-line: count records, not physical lines -----------------

    [Fact]
    public void ParseCsvContent_CountsRecordsNotLines_ForMultilineField()
    {
        WithCsv(MultilineCsv, path =>
        {
            ImportQueueItemCmdlet.ParseCsvContent(path, Encoding.UTF8, int.MaxValue,
                out var headers, out var contents, out var errors, out var total);
            Assert.Empty(errors);
            Assert.Equal(new[] { "id", "note" }, headers);
            Assert.Equal(2, total);                       // 2 records, NOT 3 physical lines
            Assert.Equal(2, contents.Count);
            Assert.Equal("alpha\nbeta", contents[0][1]);  // embedded newline kept in one field
            Assert.Equal("gamma", contents[1][1]);
        });
    }

    [Fact]
    public void ReadCsvCapped_SharesMultilineParser_CountsRecordsAndJoinsField()
    {
        WithCsv(MultilineCsv, path =>
        {
            // Under the old line-based CsvLine.Split this CSV split into 3 rows
            // with a broken quoted field; the shared multi-line parser reads 2.
            var (_, rows, total) = ImportTestDataQueueItemCmdlet.ReadCsvCapped(path, Encoding.UTF8, int.MaxValue);
            Assert.Equal(2, total);
            Assert.Equal(2, rows.Count);
            Assert.Equal("alpha\nbeta", rows[0][1]);
        });
    }

    [Fact]
    public void Cap_CombinedWithMultiline_CapsRecords_ButCountsAll()
    {
        WithCsv(MultilineCsv, path =>
        {
            ImportQueueItemCmdlet.ParseCsvContent(path, Encoding.UTF8, rowCap: 1,
                out _, out var contents, out _, out var total);
            Assert.Single(contents);         // capped to one record
            Assert.Equal(2, total);          // both records still counted
        });
    }

    // ---- (3) double-quote (RFC 4180) handling in the multi-line parser -------
    // The earlier inline parser mangled values containing double quotes; the
    // shared MultilineCsv parser handles "" escaping, quote-wrapped commas, and
    // fields that begin with an escaped quote. These pin that behavior.

    [Fact]
    public void ParseCsvContent_EscapedDoubleQuote_UnescapesToSingleQuote()
    {
        // 1,"say ""hi"""  ->  field = say "hi"
        WithCsv("h1,h2\n1,\"say \"\"hi\"\"\"\n", path =>
        {
            ImportQueueItemCmdlet.ParseCsvContent(path, Encoding.UTF8, int.MaxValue,
                out _, out var contents, out var errors, out var total);
            Assert.Empty(errors);
            Assert.Equal(1, total);
            Assert.Equal("say \"hi\"", contents[0][1]);
        });
    }

    [Fact]
    public void ParseCsvContent_QuotedFieldWithEmbeddedComma_KeepsComma()
    {
        // 1,"a,b"  ->  field = a,b (one field, comma preserved)
        WithCsv("h1,h2\n1,\"a,b\"\n", path =>
        {
            ImportQueueItemCmdlet.ParseCsvContent(path, Encoding.UTF8, int.MaxValue,
                out _, out var contents, out var errors, out _);
            Assert.Empty(errors);
            Assert.Equal(new[] { "1", "a,b" }, contents[0]);
        });
    }

    [Fact]
    public void ParseCsvContent_FieldStartingWithEscapedQuotes_Unescapes()
    {
        // 1,"""Allow"",""Solutions"""  ->  field = "Allow","Solutions"
        WithCsv("h1,h2\n1,\"\"\"Allow\"\",\"\"Solutions\"\"\"\n", path =>
        {
            ImportQueueItemCmdlet.ParseCsvContent(path, Encoding.UTF8, int.MaxValue,
                out _, out var contents, out var errors, out _);
            Assert.Empty(errors);
            Assert.Equal("\"Allow\",\"Solutions\"", contents[0][1]);
        });
    }

    [Fact]
    public void ParseCsvContent_QuotedEmptyField_IsEmptyString()
    {
        WithCsv("h1,h2\n1,\"\"\n", path =>
        {
            ImportQueueItemCmdlet.ParseCsvContent(path, Encoding.UTF8, int.MaxValue,
                out _, out var contents, out var errors, out _);
            Assert.Empty(errors);
            Assert.Equal(new[] { "1", "" }, contents[0]);
        });
    }

    [Fact]
    public void ParseCsvContent_MultilineFieldWithEscapedQuote_Combined()
    {
        // 1,"alpha<NL>""q"""  ->  field = alpha\n"q"   (embedded newline + escaped quote)
        WithCsv("h1,h2\n1,\"alpha\n\"\"q\"\"\"\n2,gamma\n", path =>
        {
            ImportQueueItemCmdlet.ParseCsvContent(path, Encoding.UTF8, int.MaxValue,
                out _, out var contents, out var errors, out var total);
            Assert.Empty(errors);
            Assert.Equal(2, total);
            Assert.Equal("alpha\n\"q\"", contents[0][1]);
            Assert.Equal("gamma", contents[1][1]);
        });
    }
}
