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

    // ---- (4) single-column CSV (no comma on any row) ------------------------
    // The end-of-line finalizer used to require a comma (currentRow.Count != 0),
    // so a single-column file never produced rows. These pin the fix.

    [Fact]
    public void ParseCsvContent_SingleColumn_ProducesOneFieldPerRow()
    {
        WithCsv("h1\na\nb\n", path =>
        {
            ImportQueueItemCmdlet.ParseCsvContent(path, Encoding.UTF8, int.MaxValue,
                out var headers, out var contents, out var errors, out var total);
            Assert.Empty(errors);
            Assert.Equal(new[] { "h1" }, headers);
            Assert.Equal(2, total);
            Assert.Equal(new[] { "a" }, contents[0]);
            Assert.Equal(new[] { "b" }, contents[1]);
        });
    }

    [Fact]
    public void ParseCsvContent_SingleColumn_QuotedValueWithComma_KeptAsOneField()
    {
        WithCsv("h1\n\"a,b\"\n", path =>
        {
            ImportQueueItemCmdlet.ParseCsvContent(path, Encoding.UTF8, int.MaxValue,
                out var headers, out var contents, out var errors, out var total);
            Assert.Empty(errors);
            Assert.Equal(new[] { "h1" }, headers);
            Assert.Equal(1, total);
            Assert.Equal(new[] { "a,b" }, contents[0]);
        });
    }

    [Fact]
    public void ParseCsvContent_SingleColumn_MultilineValue_KeptAsOneRecord()
    {
        WithCsv("h1\n\"x\ny\"\nz\n", path =>
        {
            ImportQueueItemCmdlet.ParseCsvContent(path, Encoding.UTF8, int.MaxValue,
                out _, out var contents, out var errors, out var total);
            Assert.Empty(errors);
            Assert.Equal(2, total);                 // 2 records over 3 data lines
            Assert.Equal("x\ny", contents[0][0]);
            Assert.Equal("z", contents[1][0]);
        });
    }

    // ---- (8) delimiter detection ----------------------------------------------------
    // What the web detected for TestData/QueueItemUploadMatrix 3a-3g and 28.

    [Theory]
    [InlineData("CaseId;Value\nSA;v\n", ';')]
    [InlineData("CaseId\tValue\nSB\tv\n", '\t')]
    [InlineData("CaseId|Value\nSC|v\n", '|')]
    [InlineData("CaseId,Value\nSD1,a;b;c\nSD2,d;e;f\n", ',')]      // semicolons only inside values
    [InlineData("CaseId;Value,X\nSE;v,x\n", ',')]                  // tie: comma comes first
    [InlineData("CaseId;Value;Other,X\nSF;v;o,x\n", ';')]          // more consistent fields
    [InlineData("CaseId;Value\nSG;\"a,b\"\n", ';')]
    [InlineData("h1,h2\n1,\"a\tb\"\n2,c\n", ',')]                 // tab inside a quoted value
    public void GuessDelimiter_MatchesWeb(string text, char expected)
    {
        Assert.Equal(expected, UiPath.PowerShell.Commands.CsvHelper.MultilineCsv.GuessDelimiter(text));
    }

    [Fact]
    public void GuessDelimiter_SingleColumn_IsUndetectable()
    {
        // The web rejects this with "Undetectable Delimiter"; the cmdlet falls back to ','.
        Assert.Null(UiPath.PowerShell.Commands.CsvHelper.MultilineCsv.GuessDelimiter("CaseId\nH8\n"));
    }

    [Fact]
    public void Parse_SemicolonDelimiter_KeepsQuotedComma()
    {
        WithCsv("CaseId;Value\nSG;\"a,b\"\n", path =>
        {
            UiPath.PowerShell.Commands.CsvHelper.MultilineCsv.Parse(path, Encoding.UTF8, int.MaxValue,
                out var headers, out var contents, out var errors, out _, ';');
            Assert.Empty(errors);
            Assert.Equal(new[] { "CaseId", "Value" }, headers);
            Assert.Equal(new[] { "SG", "a,b" }, contents[0]);
        });
    }

    // ---- (6) ragged rows ---------------------------------------------------------
    // The web rejects the whole file when a row has fewer or more fields than the
    // header (TestData/QueueItemUploadMatrix 31_ragged_short_row.csv).

    [Theory]
    [InlineData("h1,h2,h3\n1,2\n", "Too Few Fields")]
    [InlineData("h1,h2\n1,2,3\n", "Too Many Fields")]
    public void ParseCsvContent_RaggedRow_IsAnError(string csv, string expected)
    {
        WithCsv(csv, path =>
        {
            ImportQueueItemCmdlet.ParseCsvContent(path, Encoding.UTF8, int.MaxValue,
                out _, out _, out var errors, out _);
            Assert.Single(errors);
            Assert.Equal(expected, errors[0].ErrorCode);
            Assert.Equal(1, errors[0].CSVRow); // the first data row, as the web numbers it (31, 32)
        });
    }

    // ---- (7) error row numbers ---------------------------------------------------
    // The first data row is row 1 and a rejected row keeps its number. The web numbers
    // its field-count and Priority errors this way but its quote errors one higher
    // (TestData/QueueItemUploadMatrix 37, 39: CSV Row 2 for the first data row).

    [Theory]
    [InlineData("h1,h2\n1,\"x\"y\n", 1, "Invalid Quotes")]
    [InlineData("h1,h2\n1,a\n2,\"x\"y\n", 2, "Invalid Quotes")]
    [InlineData("h1,h2\n1,2,3\n2,\"x\"y\n", 2, "Invalid Quotes")]   // after a rejected row
    [InlineData("h1,h2\n1,a\n2,\"open\n", 2, "Missing Quotes")]
    public void ParseCsvContent_ErrorRow_CountsFromTheFirstDataRow(string csv, int row, string code)
    {
        WithCsv(csv, path =>
        {
            ImportQueueItemCmdlet.ParseCsvContent(path, Encoding.UTF8, int.MaxValue,
                out _, out _, out var errors, out _);
            var e = Assert.Single(errors, x => x.ErrorCode == code);
            Assert.Equal(row, e.CSVRow);
        });
    }

    [Fact]
    public void ParseCsvContent_EmptyLastField_IsNotShort()
    {
        // "1,2," has three fields, the last one empty.
        WithCsv("h1,h2,h3\n1,2,\n", path =>
        {
            ImportQueueItemCmdlet.ParseCsvContent(path, Encoding.UTF8, int.MaxValue,
                out _, out var contents, out var errors, out _);
            Assert.Empty(errors);
            Assert.Equal(new[] { "1", "2", "" }, contents[0]);
        });
    }

    // ---- (5) embedded newline kept as written ---------------------------------
    // The web "Upload Items" keeps a CRLF or bare CR inside a quoted field as is
    // (TestData/QueueItemUploadMatrix 01_main.csv, cases V09/V10). The parser used
    // to read physical lines with File.ReadLines and rejoin them with "\n".

    [Theory]
    [InlineData("\r\n", "\r\n")] // CRLF file, CRLF in the cell
    [InlineData("\r\n", "\n")]   // CRLF file, LF in the cell
    [InlineData("\r\n", "\r")]   // CRLF file, bare CR in the cell
    [InlineData("\n", "\r\n")]   // LF file, CRLF in the cell
    public void ParseCsvContent_EmbeddedNewline_KeptAsWritten(string fileEol, string cellEol)
    {
        WithCsv($"h1,h2{fileEol}1,\"a{cellEol}b\"{fileEol}2,c{fileEol}", path =>
        {
            ImportQueueItemCmdlet.ParseCsvContent(path, Encoding.UTF8, int.MaxValue,
                out _, out var contents, out var errors, out var total);
            Assert.Empty(errors);
            Assert.Equal(2, total);
            Assert.Equal($"a{cellEol}b", contents[0][1]);
            Assert.Equal("c", contents[1][1]);
        });
    }

    // DetectDelimiter reads only the start of the file, but past a first record longer
    // than its 64K read buffer (TestData/QueueItemUploadMatrix 54 has a 100,000-char value).
    [Theory]
    [InlineData(10)]
    [InlineData(200_000)]
    public void DetectDelimiter_LongFirstRecord(int valueLength)
    {
        var sb = new StringBuilder("CaseId;Value\n");
        sb.Append("L1;\"").Append('x', valueLength).Append(",\"\n");
        for (int i = 2; i <= 20; i++) sb.Append($"L{i};v\n");
        WithCsv(sb.ToString(), path =>
            Assert.Equal(';', UiPath.PowerShell.Commands.CsvHelper.MultilineCsv.DetectDelimiter(path, Encoding.UTF8)));
    }

    // Import-OrchTestDataQueueItem used to discard the parser's errors, so a row with
    // fewer fields than the header was dropped without a word once such rows were rejected.
    [Fact]
    public void ReadCsvCapped_ShortRow_IsReported()
    {
        WithCsv("h1,h2,h3\n1,2,3\n4,5\n", path =>
        {
            var (_, rows, _) = ImportTestDataQueueItemCmdlet.ReadCsvCapped(path, Encoding.UTF8, int.MaxValue, out var errors);
            Assert.Single(rows);
            var e = Assert.Single(errors);
            Assert.Equal("Too Few Fields", e.ErrorCode);
            Assert.Equal(2, e.CSVRow);
        });
    }
}
