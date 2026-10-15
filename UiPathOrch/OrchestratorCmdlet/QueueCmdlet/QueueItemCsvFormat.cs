using System.Text.Encodings.Web;
using System.Text.Json;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// The CSV that Get-OrchQueueItem -ExportCsv writes and Import-OrchQueueItem reads back.
//
// The leading columns are the web "Upload Items" format: Reference, Priority, Deadline,
// Postpone, and one column per SpecificContent key, so the file can be uploaded as it is and
// a moved item keeps its due and postpone dates. After them come the item's history, in
// columns whose names start with '@' (@Status, @Output, ...). The web stores those as
// SpecificContent too; Import-OrchQueueItem recognizes them by name, leaves them out of
// SpecificContent, and imports only the rows whose @Status is New unless the file is edited
// (the @Status column removed, or a row's @Status set to New), so that a finished
// transaction is not queued again by accident.
internal static class QueueItemCsvFormat
{
    internal const string StatusColumn = "@Status";

    // The web's date columns: Deadline sets the item's DueDate and Postpone its DeferDate
    // (header case-sensitive, like Reference and Priority).
    internal const string DeadlineColumn = "Deadline";
    internal const string PostponeColumn = "Postpone";

    // A Deadline / Postpone value as the web accepts it (TestData/QueueItemUploadMatrix 08-0e):
    // YYYY-MM-DD, YYYY/MM/DD or YYYY MM DD (one separator throughout), optionally followed by
    // one space and HH:mm or HH:mm:ss, every part zero-padded, no surrounding spaces. The date
    // must exist (no month 13, Feb 30, Feb 29 in a non-leap year, second 60); 24:00 is the
    // next day's 00:00. The value is taken as UTC (the web does not apply the browser's time
    // zone: "2026-12-31 10:30" is stored as 2026-12-31T10:30:00Z).
    private static readonly System.Text.RegularExpressions.Regex UploadDatePattern = new(
        @"^([0-9]{4})([-/ ])([0-9]{2})\2([0-9]{2})(?: ([0-9]{2}):([0-9]{2})(?::([0-9]{2}))?)?$",
        System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    internal static bool TryParseUploadDate(string value, out DateTime utc)
    {
        utc = default;
        var m = UploadDatePattern.Match(value);
        if (!m.Success) return false;
        int Int(int g) => m.Groups[g].Success ? int.Parse(m.Groups[g].Value, System.Globalization.CultureInfo.InvariantCulture) : 0;
        int year = Int(1), month = Int(3), day = Int(4), hour = Int(5), minute = Int(6), second = Int(7);
        if (year < 1 || month is < 1 or > 12 || day < 1 || day > DateTime.DaysInMonth(year, month)) return false;
        if (minute > 59 || second > 59) return false;
        if (hour > 24 || (hour == 24 && (minute != 0 || second != 0))) return false;
        var date = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
        if (hour == 24 && date.Date == DateTime.MaxValue.Date) return false;
        utc = date.AddHours(hour).AddMinutes(minute).AddSeconds(second);
        return true;
    }

    // DueDate / DeferDate as the Deadline / Postpone cell: UTC, in a format the web accepts.
    // The entity holds them in local time (LocalDateTimeConverter).
    internal static string UploadDateText(DateTime? value)
    {
        if (value is null) return "";
        var utc = value.Value.Kind == DateTimeKind.Utc ? value.Value : value.Value.ToUniversalTime();
        return utc.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
    }

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

    // The SpecificContent (or Output / Analytics) keys and values of an item: the
    // DynamicProperties of the *Data JSON string, which holds the values as they were added,
    // else the dictionary. The server re-parses the dictionary, and on the way a date-time
    // string becomes UTC: "2026-10-15T10:00:00+09:00" comes back as "2026-10-15T01:00:00Z" in
    // SpecificContent but as written in SpecificData (25.10.2 and 20.10.16).
    internal static List<KeyValuePair<string, JsonElement>> DynamicProperties(Dictionary<string, object>? content, string? data)
    {
        var result = new List<KeyValuePair<string, JsonElement>>();
        if (!string.IsNullOrEmpty(data))
        {
            try
            {
                using var doc = JsonDocument.Parse(data);
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("DynamicProperties", out var dp)) root = dp;
                if (root.ValueKind == JsonValueKind.Object)
                {
                    foreach (var p in root.EnumerateObject())
                    {
                        result.Add(new(p.Name, p.Value.Clone()));
                    }
                    return result;
                }
            }
            catch (JsonException)
            {
                // Not JSON: use the dictionary.
            }
        }
        if (content is not null)
        {
            foreach (var (key, value) in content)
            {
                result.Add(new(key, value is JsonElement e ? e : JsonSerializer.SerializeToElement(value)));
            }
        }
        return result;
    }

    // A cell as written to the file: quoted when it holds a comma, a quote or a line break, and
    // also when it starts or ends with whitespace, which some readers drop from an unquoted
    // field (PowerShell's Import-Csv turns an unquoted "   " into "").
    internal static string CsvCell(string value)
    {
        if (value.Length == 0) return value;
        bool quote = value.IndexOfAny([',', '"', '\n', '\r']) >= 0
            || char.IsWhiteSpace(value[0]) || char.IsWhiteSpace(value[^1]);
        return quote ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
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
                if (key is "Reference" or "Priority" or DeadlineColumn or PostponeColumn || IsHistoryColumn(key))
                {
                    if (!unrepresentable.Contains(key)) unrepresentable.Add(key);
                    continue;
                }
                keyIndex[key] = keys.Count;
                keys.Add(key);
            }
            itemContents.Add((item, content));
        }

        const int Lead = 4; // Reference, Priority, Deadline, Postpone
        var headers = new List<string>(Lead + keys.Count + HistoryColumns.Length) { "Reference", "Priority", DeadlineColumn, PostponeColumn };
        headers.AddRange(keys);
        headers.AddRange(HistoryColumns);

        var rows = new List<string[]>(itemContents.Count);
        foreach (var (item, content) in itemContents)
        {
            var row = new string[headers.Count];
            row[0] = item.Reference ?? "";
            row[1] = item.Priority ?? "";
            row[2] = UploadDateText(item.DueDate);
            row[3] = UploadDateText(item.DeferDate);
            for (int i = 0; i < keys.Count; i++) row[Lead + i] = "";
            foreach (var (key, value) in content)
            {
                if (keyIndex.TryGetValue(key, out var i)) row[Lead + i] = CellText(value);
            }

            int h = Lead + keys.Count;
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
