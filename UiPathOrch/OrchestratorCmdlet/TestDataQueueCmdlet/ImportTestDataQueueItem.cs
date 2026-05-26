using System.Globalization;
using System.Management.Automation;
using System.Text;
using System.Text.Json;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

// Import-OrchTestDataQueueItem -- bulk-adds test data queue items from a CSV
// in the SAME format the Orchestrator web UI accepts under "Upload Items":
// the header row holds the queue's ContentJsonSchema property names and each
// subsequent row is one item. Each cell is coerced to the JSON type the
// schema declares (integer / number / boolean -> JSON literal; everything
// else -> string), then all rows are POSTed in a single bulk call per queue
// (/api/TestDataQueueActions/BulkAddItems).
//
// Counterpart of Import-OrchQueueItem (file -> bulk add). It shares the
// multi-line, quote-aware CSV reader (CsvHelper.MultilineCsv) with
// Import-OrchQueueItem so a quoted field with an embedded newline is read as
// one field, exactly like the web "Upload Items" dialog (web-golden-verified).
// Each item's shape is derived from the queue's schema rather than fixed columns.
[Cmdlet(VerbsData.Import, "OrchTestDataQueueItem", SupportsShouldProcess = true)]
public class ImportTestDataQueueItemCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TestDataQueueNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? ImportCsv { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(EncodingCompleter))]
    [EncodingArgumentTransformation]
    public Encoding? CsvEncoding { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    // Parse a queue's ContentJsonSchema into propertyName -> JSON type
    // ("integer" / "number" / "boolean" / "string"). A "{}" schema (any
    // shape) yields an empty map, so every column is treated as a string.
    // internal: the CSV->items-JSON conversion is unit-tested directly
    // against a captured Orchestrator-web POST body (WebUploadEquivalenceTests).
    internal static Dictionary<string, string> ParseSchemaTypes(string? contentJsonSchema)
    {
        var types = new Dictionary<string, string>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(contentJsonSchema)) return types;
        try
        {
            using var doc = JsonDocument.Parse(contentJsonSchema);
            if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                doc.RootElement.TryGetProperty("properties", out var props) &&
                props.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in props.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.Object &&
                        prop.Value.TryGetProperty("type", out var t) &&
                        t.ValueKind == JsonValueKind.String)
                    {
                        types[prop.Name] = t.GetString()!;
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Malformed schema -> treat all columns as strings.
        }
        return types;
    }

    // Coerce a CSV cell to the JSON value the schema expects. On a parse
    // failure we fall back to the raw string and let the server validate,
    // rather than silently dropping a row.
    internal static object? Coerce(string value, string? type)
    {
        switch (type)
        {
            case "integer":
                return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l) ? l : value;
            case "number":
                return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : value;
            case "boolean":
                return bool.TryParse(value, out var b) ? b : value;
            default:
                return value;
        }
    }

    internal static string BuildItemJson(IReadOnlyList<string> headers, IReadOnlyList<string> values, IReadOnlyDictionary<string, string> schemaTypes)
    {
        var obj = new Dictionary<string, object?>(StringComparer.Ordinal);
        int n = Math.Min(headers.Count, values.Count);
        for (int i = 0; i < n; i++)
        {
            var header = headers[i];
            if (string.IsNullOrEmpty(header)) continue;
            var value = values[i];
            // Empty cells are NOT dropped — the web includes every CSV column in
            // every item (golden-verified: an empty string cell appears as "").
            // A non-string empty value stays "" and is rejected by the server's
            // schema validation, exactly as the web behaves (e.g. empty integer).
            schemaTypes.TryGetValue(header, out var type);
            obj[header] = Coerce(value, type);
        }
        return JsonSerializer.Serialize(obj);
    }

    // The full "items" JSON array POSTed to /BulkAddItems. This is exactly the
    // value the Orchestrator web UI builds client-side from the same CSV, so a
    // unit test pins it against a captured web POST body.
    internal static string BuildItemsArrayJson(IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string>> rows, IReadOnlyDictionary<string, string> schemaTypes)
    {
        return "[" + string.Join(",", rows.Select(r => BuildItemJson(headers, r, schemaTypes))) + "]";
    }

    // Streaming read with a row cap: data rows beyond rowCap are counted but
    // NOT retained, so an oversized file (rejected by the 15,000-row cap) never
    // materializes more than rowCap rows in memory. totalRows is the true count
    // so the caller can report the web's "{totalRows} records" rejection.
    internal static (List<string> headers, List<List<string>> rows, int totalRows) ReadCsvCapped(string path, Encoding encoding, int rowCap)
    {
        // Share the multi-line, quote-aware CSV reader (CsvHelper.MultilineCsv)
        // so a quoted field with an embedded newline is read as one field,
        // exactly like the web "Upload Items" dialog (web-golden-verified). The
        // capped overload retains at most rowCap rows but counts the true total.
        CsvHelper.MultilineCsv.Parse(path, encoding, rowCap, out var headers, out var rows, out _, out var totalRows);
        return (headers, rows, totalRows);
    }

    // Uncapped read (retains every row). Pinned signature used by the
    // web-equivalence unit test.
    internal static (List<string> headers, List<List<string>> rows) ReadCsv(string path, Encoding encoding)
    {
        var (headers, rows, _) = ReadCsvCapped(path, encoding, int.MaxValue);
        return (headers, rows);
    }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(Path);
        var wpName = Name.ConvertToWildcardPatternList();
        var encoding = CsvEncoding ?? Encoding.UTF8;

        // Parse each CSV file once (schema-independent: headers + raw rows).
        var parsedFiles = new List<(string path, List<string> headers, List<List<string>> rows)>();
        foreach (var p in SessionState.ExpandLocalPath(ImportCsv, "*.csv"))
        {
            try
            {
                // Match the web "Upload Items" 15,000-row cap (reject, don't chunk).
                // /BulkAddItems would accept far more, so this client-side check
                // is what keeps the cmdlet equivalent to the web. ReadCsvCapped
                // retains at most MaxRows rows, so an oversized file is rejected
                // here without being fully materialized.
                var (headers, rows, totalRows) = ReadCsvCapped(p.FullPath, encoding, CsvUploadLimit.MaxRows);
                var limitError = CsvUploadLimit.RowLimitError(totalRows);
                if (limitError is not null)
                {
                    WriteError(new ErrorRecord(new OrchException(p.FullPath, limitError), "MaxRowsExceeded", ErrorCategory.LimitsExceeded, p.FullPath));
                    continue;
                }
                parsedFiles.Add((p.FullPath, headers, rows));
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(p.FullPath, ex), "ImportTestDataQueueItemError", ErrorCategory.InvalidOperation, p.FullPath));
            }
        }

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            try
            {
                var queues = drive.TestDataQueues.Get(folder)
                    .FilterByWildcards(e => e?.Name, wpName)
                    .OrderBy(e => e.Name);
                foreach (var queue in queues.WithCancellation(cancelHandler.Token))
                {
                    var schemaTypes = ParseSchemaTypes(queue.ContentJsonSchema);
                    foreach (var (csvPath, headers, rows) in parsedFiles)
                    {
                        if (rows.Count == 0) continue;
                        string target = queue.GetPSPath();
                        if (ShouldProcess(target, $"Import {rows.Count} item(s) from {csvPath}"))
                        {
                            try
                            {
                                var itemJsonArray = BuildItemsArrayJson(headers, rows, schemaTypes);
                                drive.OrchAPISession.AddTestDataQueueItems(folder.Id ?? 0, queue.Name ?? "", itemJsonArray);
                                drive.TestDataQueueItems.ClearCache(folder);
                                WriteVerbose($"Added {rows.Count} item(s) to {target} from {csvPath}.");
                            }
                            catch (Exception ex)
                            {
                                WriteError(new ErrorRecord(new OrchException(target, ex), "ImportTestDataQueueItemError", ErrorCategory.InvalidOperation, queue));
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "GetTestDataQueueError", ErrorCategory.InvalidOperation, folder));
            }
        }
    }
}
