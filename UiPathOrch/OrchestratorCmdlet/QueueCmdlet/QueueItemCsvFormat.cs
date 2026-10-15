using System.Text.Encodings.Web;
using System.Text.Json;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// The CSV that Get-OrchQueueItem -ExportCsv writes and Import-OrchQueueItem reads back.
//
// The leading columns are the web "Upload Items" format: Reference, Priority, and one column
// per SpecificContent key, so the file can be uploaded as it is. After them come the item's
// history, in columns whose names start with '@' (@Status, @Output, ...). The web stores
// those as SpecificContent too; Import-OrchQueueItem recognizes them by name, leaves them out
// of SpecificContent, and imports only the rows whose @Status is New unless
// -IncludeProcessedItems is given, so that a finished transaction is not queued again by
// accident.
internal static class QueueItemCsvFormat
{
    internal const string StatusColumn = "@Status";

    // The history columns, in file order. Import-OrchQueueItem never stores these names as
    // SpecificContent.
    internal static readonly string[] HistoryColumns =
    [
        "@Id",
        StatusColumn,
        "@ReviewStatus",
        "@ProcessingExceptionType",
        "@ProcessingExceptionReason",
        "@ProcessingExceptionDetails",
        "@Output",
        "@Analytics",
        "@Robot",
        "@RetryNumber",
        "@Progress",
        "@CreationTime",
        "@StartProcessing",
        "@EndProcessing",
        "@DeferDate",
        "@DueDate",
        "@RiskSlaDate",
        "@Key",
    ];

    private static readonly HashSet<string> s_historyColumns = new(HistoryColumns, StringComparer.Ordinal);

    internal static bool IsHistoryColumn(string header) => s_historyColumns.Contains(header);

    private static readonly JsonSerializerOptions s_json = new()
    {
        // Keep Japanese and other non-ASCII text readable in the @Output cell.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    // The SpecificContent (or Output / Analytics) keys and values of an item: the dictionary
    // when the listing filled it, else the DynamicProperties of the *Data JSON string.
    internal static List<KeyValuePair<string, JsonElement>> DynamicProperties(Dictionary<string, object>? content, string? data)
    {
        var result = new List<KeyValuePair<string, JsonElement>>();
        if (content is not null)
        {
            foreach (var (key, value) in content)
            {
                result.Add(new(key, value is JsonElement e ? e : JsonSerializer.SerializeToElement(value)));
            }
            return result;
        }
        if (string.IsNullOrEmpty(data)) return result;
        try
        {
            using var doc = JsonDocument.Parse(data);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("DynamicProperties", out var dp)) root = dp;
            if (root.ValueKind != JsonValueKind.Object) return result;
            foreach (var p in root.EnumerateObject())
            {
                result.Add(new(p.Name, p.Value.Clone()));
            }
        }
        catch (JsonException)
        {
            // Not JSON: nothing to split into columns.
        }
        return result;
    }

    // A SpecificContent value as a cell: a string as it is, a number as written, true/false in
    // lower case (what -DynamicTyping turns back into a boolean), null as an empty cell, and an
    // object or array as JSON text.
    internal static string CellText(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString() ?? "",
        JsonValueKind.Null or JsonValueKind.Undefined => "",
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Number => value.GetRawText(),
        _ => JsonSerializer.Serialize(value, s_json),
    };

    private static string JsonText(List<KeyValuePair<string, JsonElement>> properties)
    {
        if (properties.Count == 0) return "";
        var dict = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var (key, value) in properties) dict[key] = value;
        return JsonSerializer.Serialize(dict, s_json);
    }

    // The header and the rows (unescaped cell text) for the items of one queue. A SpecificContent
    // key that cannot have a column of its own (Reference, Priority, or a history column name)
    // is reported in 'unrepresentable' and left out.
    internal static (List<string> headers, List<string[]> rows) Build(IEnumerable<QueueItem> items, out List<string> unrepresentable)
    {
        unrepresentable = [];
        var keys = new List<string>();
        var keyIndex = new Dictionary<string, int>(StringComparer.Ordinal);
        var itemContents = new List<(QueueItem item, List<KeyValuePair<string, JsonElement>> content)>();

        foreach (var item in items)
        {
            var content = DynamicProperties(item.SpecificContent, item.SpecificData);
            foreach (var (key, _) in content)
            {
                if (keyIndex.ContainsKey(key)) continue;
                if (key is "Reference" or "Priority" || IsHistoryColumn(key))
                {
                    if (!unrepresentable.Contains(key)) unrepresentable.Add(key);
                    continue;
                }
                keyIndex[key] = keys.Count;
                keys.Add(key);
            }
            itemContents.Add((item, content));
        }

        var headers = new List<string>(2 + keys.Count + HistoryColumns.Length) { "Reference", "Priority" };
        headers.AddRange(keys);
        headers.AddRange(HistoryColumns);

        var rows = new List<string[]>(itemContents.Count);
        foreach (var (item, content) in itemContents)
        {
            var row = new string[headers.Count];
            row[0] = item.Reference ?? "";
            row[1] = item.Priority ?? "";
            for (int i = 0; i < keys.Count; i++) row[2 + i] = "";
            foreach (var (key, value) in content)
            {
                if (keyIndex.TryGetValue(key, out var i)) row[2 + i] = CellText(value);
            }

            int h = 2 + keys.Count;
            row[h++] = item.Id?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "";
            row[h++] = item.Status ?? "";
            row[h++] = item.ReviewStatus ?? "";
            row[h++] = item.ProcessingExceptionType ?? item.ProcessingException?.Type ?? "";
            row[h++] = item.ProcessingException?.Reason ?? "";
            row[h++] = item.ProcessingException?.Details ?? "";
            row[h++] = JsonText(DynamicProperties(item.Output, item.OutputData));
            row[h++] = JsonText(DynamicProperties(item.Analytics, item.AnalyticsData));
            row[h++] = item.Robot?.Name ?? "";
            row[h++] = item.RetryNumber?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "";
            row[h++] = item.Progress ?? "";
            row[h++] = OrchestratorPSCmdlet.FormatDateTimeWithKind(item.CreationTime) ?? "";
            row[h++] = OrchestratorPSCmdlet.FormatDateTimeWithKind(item.StartProcessing) ?? "";
            row[h++] = OrchestratorPSCmdlet.FormatDateTimeWithKind(item.EndProcessing) ?? "";
            row[h++] = OrchestratorPSCmdlet.FormatDateTimeWithKind(item.DeferDate) ?? "";
            row[h++] = OrchestratorPSCmdlet.FormatDateTimeWithKind(item.DueDate) ?? "";
            row[h++] = OrchestratorPSCmdlet.FormatDateTimeWithKind(item.RiskSlaDate) ?? "";
            row[h++] = item.Key ?? "";
            rows.Add(row);
        }
        return (headers, rows);
    }

    // The file for one queue when -ExportCsv names a directory: "<drive>_<folders>_<queue>.csv",
    // e.g. Orch1:\Shared\Sub + Invoices -> "Orch1_Shared_Sub_Invoices.csv".
    internal static string FileNameFor(string folderPSPath, string queueName)
    {
        var parts = folderPSPath.Replace(":", "").Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries).ToList();
        parts.Add(queueName);
        var name = string.Join("_", parts);
        var invalid = Path.GetInvalidFileNameChars();
        return new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray()) + ".csv";
    }
}
