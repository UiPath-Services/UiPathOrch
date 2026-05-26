using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using UiPath.PowerShell.Commands;
using Xunit;

namespace UnitTests;

// Equivalence guard: Import-OrchTestDataQueueItem must build the SAME items
// JSON that the Orchestrator web "Upload Items" dialog builds from the same
// CSV. Both clients POST to the identical endpoint
// (/api/TestDataQueueActions/BulkAddItems), so if the items array matches, the
// created items are guaranteed identical — no live tenant needed to prove it.
//
// The web's ground truth is captured once as a golden fixture (see
// TestData/WebEquivalence/README.md). Until the golden is supplied the
// equivalence fact skips; the JsonDeepEquals self-tests below always run so
// the comparer itself stays trustworthy.
public class WebUploadEquivalenceTests
{
    // ---- comparer self-tests (always run) ----------------------------------

    [Fact]
    public void JsonDeepEquals_IgnoresObjectKeyOrder()
    {
        Assert.True(DeepEquals("""{"a":1,"b":"x"}""", """{"b":"x","a":1}"""));
    }

    [Fact]
    public void JsonDeepEquals_NormalizesNumberFormatting()
    {
        // 100 and 100.0 are the same JSON number; integer vs decimal spelling
        // must not make the equivalence test flap.
        Assert.True(DeepEquals("""{"n":100}""", """{"n":100.0}"""));
    }

    [Fact]
    public void JsonDeepEquals_DistinguishesStringFromNumber()
    {
        // Schema-driven coercion is the whole point: "3" (string) must NOT
        // equal 3 (number).
        Assert.False(DeepEquals("""{"v":"3"}""", """{"v":3}"""));
    }

    [Fact]
    public void JsonDeepEquals_DetectsMissingKey()
    {
        Assert.False(DeepEquals("""[{"a":1,"b":2}]""", """[{"a":1}]"""));
    }

    // ---- the equivalence fact (skips until the web golden is captured) ------

    // Each case asserts Import-OrchTestDataQueueItem builds the same items array
    // the Orchestrator web "Upload Items" dialog POSTs for the same CSV.

    // Triangle schema: integer (番号) + string (辺A..). Golden captured.
    [Fact]
    public void TestDataQueueItem_TriangleSchema_MatchesWebUpload()
        => AssertCmdletItemsMatchWeb("tdq_triangle_sample.csv", "tdq_triangle_schema.json", "tdq_triangle_web_golden.json");

    // Kitchen-sink: integer / number / boolean / string / date / date-time in
    // one schema. Golden captured — confirms the web drops number trailing
    // zeros (100.50 -> 100.5) and passes date / date-time strings through
    // verbatim (no ISO normalization; +09:00 offset preserved), which matches
    // the cmdlet's string passthrough.
    [Fact]
    public void TestDataQueueItem_KitchenSinkSchema_MatchesWebUpload()
        => AssertCmdletItemsMatchWeb("kitchensink_sample.csv", "kitchensink_schema.json", "kitchensink_web_golden.json");

    // Empty string cell: golden-verified that the web includes every column —
    // an empty strField appears as "" (not omitted). BuildItemJson no longer
    // drops empty cells, so the cmdlet matches.
    [Fact]
    public void TestDataQueueItem_EmptyStringCell_MatchesWebUpload()
        => AssertCmdletItemsMatchWeb("kitchensink_empty_sample.csv", "kitchensink_schema.json", "kitchensink_empty_web_golden.json");

    // Multi-line cell: a quoted field containing an embedded newline. Golden
    // captured from the web (Orch1:\Shared queue "tdq") — the web reads it as
    // ONE field ("alpha\nbeta"), proving its CSV reader is multi-line/quote
    // aware. The cmdlet now shares Import-OrchQueueItem's multi-line parser
    // (was line-based CsvLine.Split, which split the row in three), so it matches.
    [Fact]
    public void TestDataQueueItem_MultilineCell_MatchesWebUpload()
        => AssertCmdletItemsMatchWeb("tdq_multiline_sample.csv", "tdq_multiline_schema.json", "tdq_multiline_web_golden.json");

    // Import-OrchQueueItem (regular queue — no schema): Priority maps to
    // High/Low/Normal, Reference always set, everything else goes into
    // SpecificContent as STRING values. Golden captured — confirms the web
    // also keeps SpecificContent values as strings (Amount stays "100", not
    // coerced to 100), matching the cmdlet.
    [Fact]
    public void QueueItem_MatchesWebUpload()
    {
        var dir = LocateWebEquivalenceDir();
        var csvPath = System.IO.Path.Combine(dir, "queueitem_sample.csv");
        var goldenPath = System.IO.Path.Combine(dir, "queueitem_web_golden.json");

        Assert.True(System.IO.File.Exists(csvPath), $"missing sample CSV: {csvPath}");
        Assert.True(System.IO.File.Exists(goldenPath), $"missing web golden: {goldenPath} — see TestData/WebEquivalence/README.md");

        ImportQueueItemCmdlet.ParseCsvContent(csvPath, Encoding.UTF8, out var headers, out var contents, out var parseErrors);
        Assert.Empty(parseErrors);

        var buildErrors = new List<CSVParseError>();
        var cmdletItems = ImportQueueItemCmdlet.BuildQueueItemsArrayJson(
            headers, contents.Cast<IReadOnlyList<string>>().ToList(), buildErrors);
        Assert.Empty(buildErrors);

        using var cmdletDoc = JsonDocument.Parse(cmdletItems);
        using var goldenDoc = JsonDocument.Parse(System.IO.File.ReadAllText(goldenPath));

        // The golden may be the whole BulkAddQueueItem body
        // ({"queueItems":[...],"commitType":...,"queueName":...}) or just the array.
        var goldenItems = goldenDoc.RootElement.ValueKind == JsonValueKind.Array
            ? goldenDoc.RootElement
            : goldenDoc.RootElement.TryGetProperty("queueItems", out var qi)
                ? qi
                : goldenDoc.RootElement.GetProperty("items");

        Assert.True(JsonDeepEquals(cmdletDoc.RootElement, goldenItems),
            "Import-OrchQueueItem built a different queueItems array than the web upload.\n" +
            $"cmdlet : {cmdletItems}\n" +
            $"golden : {goldenItems.GetRawText()}");
    }

    private void AssertCmdletItemsMatchWeb(string csvFile, string schemaFile, string goldenFile)
    {
        var dir = LocateWebEquivalenceDir();
        var csvPath = System.IO.Path.Combine(dir, csvFile);
        var schemaPath = System.IO.Path.Combine(dir, schemaFile);
        var goldenPath = System.IO.Path.Combine(dir, goldenFile);

        Assert.True(System.IO.File.Exists(csvPath), $"missing sample CSV: {csvPath}");
        Assert.True(System.IO.File.Exists(schemaPath), $"missing schema fixture: {schemaPath}");
        Assert.True(System.IO.File.Exists(goldenPath), $"missing web golden: {goldenPath} — see TestData/WebEquivalence/README.md");

        var (headers, rows) = ImportTestDataQueueItemCmdlet.ReadCsv(csvPath, Encoding.UTF8);
        var schemaTypes = ImportTestDataQueueItemCmdlet.ParseSchemaTypes(System.IO.File.ReadAllText(schemaPath));
        var cmdletItems = ImportTestDataQueueItemCmdlet.BuildItemsArrayJson(
            headers, rows.Cast<IReadOnlyList<string>>(), schemaTypes);

        using var cmdletDoc = JsonDocument.Parse(cmdletItems);
        using var goldenDoc = JsonDocument.Parse(System.IO.File.ReadAllText(goldenPath));

        // The golden may be the whole POST body ({"queueName":...,"items":[...]})
        // or just the items array — accept either.
        var goldenItems = goldenDoc.RootElement.ValueKind == JsonValueKind.Array
            ? goldenDoc.RootElement
            : goldenDoc.RootElement.GetProperty("items");

        Assert.True(JsonDeepEquals(cmdletDoc.RootElement, goldenItems),
            $"Import-OrchTestDataQueueItem built a different items array than the web upload ({csvFile}).\n" +
            $"cmdlet : {cmdletItems}\n" +
            $"golden : {goldenItems.GetRawText()}");
    }

    // ---- helpers -----------------------------------------------------------

    private static bool DeepEquals(string a, string b)
    {
        using var da = JsonDocument.Parse(a);
        using var db = JsonDocument.Parse(b);
        return JsonDeepEquals(da.RootElement, db.RootElement);
    }

    // Order-insensitive (object keys), number-format-insensitive deep compare.
    // Arrays are compared in order: both the cmdlet and the web preserve CSV
    // row order, so a reorder is a real difference worth failing on.
    internal static bool JsonDeepEquals(JsonElement a, JsonElement b)
    {
        if (a.ValueKind != b.ValueKind) return false;
        switch (a.ValueKind)
        {
            case JsonValueKind.Object:
                var ap = a.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);
                var bp = b.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);
                if (ap.Count != bp.Count) return false;
                foreach (var kv in ap)
                {
                    if (!bp.TryGetValue(kv.Key, out var bv)) return false;
                    if (!JsonDeepEquals(kv.Value, bv)) return false;
                }
                return true;
            case JsonValueKind.Array:
                var aa = a.EnumerateArray().ToList();
                var ba = b.EnumerateArray().ToList();
                if (aa.Count != ba.Count) return false;
                for (int i = 0; i < aa.Count; i++)
                    if (!JsonDeepEquals(aa[i], ba[i])) return false;
                return true;
            case JsonValueKind.String:
                return a.GetString() == b.GetString();
            case JsonValueKind.Number:
                if (a.TryGetDecimal(out var ad) && b.TryGetDecimal(out var bd)) return ad == bd;
                return a.GetDouble() == b.GetDouble();
            default: // True / False / Null
                return true;
        }
    }

    private static string LocateWebEquivalenceDir()
    {
        var dir = new System.IO.DirectoryInfo(System.AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = System.IO.Path.Combine(dir.FullName, "TestData", "WebEquivalence");
            if (System.IO.Directory.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        throw new System.IO.DirectoryNotFoundException(
            "TestData/WebEquivalence not found above " + System.AppContext.BaseDirectory);
    }
}
