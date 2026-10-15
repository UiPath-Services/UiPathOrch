using System.Globalization;
using System.Management.Automation;
using UiPath.PowerShell.Positional;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using UiPath.OrchAPI;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Entities.JsonConverter;

namespace UiPath.PowerShell.Commands;

public class CSVParseError(int csvRow, string errorType, string csvField, string errorCode)
{
    public int CSVRow { get; set; } = csvRow;
    public string ErrorType { get; set; } = errorType;
    public string CSVField { get; set; } = csvField;
    public string ErrorCode { get; set; } = errorCode;
}

// The web upload's "Dynamic typing", reproduced from what the web stored for
// TestData/QueueItemUploadMatrix 01/02/03 (rules in that folder's README):
//   ""                         -> null
//   true / false / TRUE / FALSE -> boolean (no other spelling, no surrounding space)
//   decimal number             -> number: optional '-', digits with an optional '.',
//                                 optional exponent, surrounding whitespace allowed;
//                                 integral results beyond 2^53-1 stay strings
//   YYYY-MM-DDTHH:mm:ss[.f](Z|+HH:MM) -> "YYYY-MM-DDTHH:mm:ssZ" in UTC, fraction dropped,
//                                 JS Date rollover (04-31 -> 05-01, T24:00:00 -> next day)
// Reference takes the same typing and is stringified as JS String() would.
// Two web defects are deliberately NOT reproduced: an out-of-range date (month 13,
// day 32, hour 25) becomes "undefinedZ" on the web and stays as written here, and a
// date in Reference becomes the browser's local Date.toString() on the web and stays
// as written here.
internal static class QueueItemDynamicTyping
{
    private const double MaxSafeInteger = 9007199254740991d;

    private static readonly Regex NumberPattern = new(@"^-?([0-9]+\.?[0-9]*|\.[0-9]+)([eE][+-]?[0-9]+)?$", RegexOptions.CultureInvariant);

    private static readonly Regex DatePattern = new(
        @"^([0-9]{4})-([0-9]{2})-([0-9]{2})T([0-9]{2}):([0-5][0-9]):([0-5][0-9])(\.[0-9]+)?(Z|([+-])([0-9]{2}):([0-9]{2}))$",
        RegexOptions.CultureInvariant);

    // JS Number() trims WhiteSpace and LineTerminator, which includes U+FEFF.
    private static string JsTrim(string s) => s.Trim().Trim('\uFEFF');

    // null, bool, long, double, or the string (rewritten for a date).
    internal static object? ToValue(string value)
    {
        if (value.Length == 0) return null;
        if (value is "true" or "TRUE") return true;
        if (value is "false" or "FALSE") return false;
        if (TryNumber(value, out var number)) return number;
        if (TryDate(value, out var date)) return date;
        return value;
    }

    internal static string ToReference(string value)
    {
        if (value.Length == 0) return value;
        if (value is "true" or "TRUE") return "true";
        if (value is "false" or "FALSE") return "false";
        if (TryNumber(value, out var number))
            return number is long l ? l.ToString(CultureInfo.InvariantCulture) : JsNumberToString((double)number);
        return value;
    }

    private static bool TryNumber(string value, out object number)
    {
        number = 0L;
        var t = JsTrim(value);
        if (!NumberPattern.IsMatch(t)) return false;
        if (!double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) || double.IsInfinity(d)) return false;
        if (Math.Floor(d) == d)
        {
            if (Math.Abs(d) > MaxSafeInteger) return false;
            number = (long)d; // also turns -0 into 0, as JSON.stringify does
            return true;
        }
        number = d;
        return true;
    }

    private static bool TryDate(string value, out string date)
    {
        date = value;
        var m = DatePattern.Match(value);
        if (!m.Success) return false;

        int Int(int g) => int.Parse(m.Groups[g].Value, CultureInfo.InvariantCulture);
        int year = Int(1), month = Int(2), day = Int(3), hour = Int(4), minute = Int(5), second = Int(6);
        bool zeroFraction = !m.Groups[7].Success || m.Groups[7].Value.Skip(1).All(c => c == '0');

        // JS Date accepts day 1-31 in any month (rolling over) and 24:00:00 exactly.
        if (year < 1 || month is < 1 or > 12 || day is < 1 or > 31) return false;
        if (hour > 24 || (hour == 24 && (minute != 0 || second != 0 || !zeroFraction))) return false;

        int offsetMinutes = 0;
        if (m.Groups[8].Value != "Z")
        {
            int oh = Int(10), om = Int(11);
            if (oh > 23 || om > 59) return false;
            offsetMinutes = (oh * 60 + om) * (m.Groups[9].Value == "-" ? -1 : 1);
        }

        try
        {
            var utc = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc)
                .AddDays(day - 1).AddHours(hour).AddMinutes(minute - offsetMinutes).AddSeconds(second);
            date = utc.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false; // outside DateTime's years 1-9999
        }
    }

    // ECMAScript Number::toString for a finite, non-integral double (integers take the
    // long path): shortest round-trip digits, exponent form below 1e-6 or from 1e21.
    internal static string JsNumberToString(double d)
    {
        if (d == 0) return "0";
        string r = Math.Abs(d).ToString("R", CultureInfo.InvariantCulture);
        int exp = 0;
        int e = r.IndexOfAny(['E', 'e']);
        if (e >= 0)
        {
            exp = int.Parse(r[(e + 1)..], CultureInfo.InvariantCulture);
            r = r[..e];
        }
        int dot = r.IndexOf('.');
        int intDigits = dot >= 0 ? dot : r.Length;
        string digits = r.Replace(".", "");
        int lead = digits.Length - digits.TrimStart('0').Length;
        digits = digits.Trim('0');
        int n = intDigits - lead + exp; // value = 0.digits * 10^n
        int k = digits.Length;

        string s;
        if (k <= n && n <= 21) s = digits + new string('0', n - k);
        else if (0 < n && n <= 21) s = digits[..n] + "." + digits[n..];
        else if (-6 < n && n <= 0) s = "0." + new string('0', -n) + digits;
        else
        {
            int x = n - 1;
            s = (k == 1 ? digits : digits[..1] + "." + digits[1..]) + "e" + (x < 0 ? "-" : "+") + Math.Abs(x).ToString(CultureInfo.InvariantCulture);
        }
        return d < 0 ? "-" + s : s;
    }
}

[Cmdlet(VerbsData.Import, "OrchQueueItem", SupportsShouldProcess = true)]
[OutputType(typeof(Entities.BulkOperationResponseDtoOfFailedQueueItem), typeof(Entities.FailedQueueItem), typeof(CSVParseError))]
public class ImportQueueItemCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(QueueNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? ImportCsv { get; set; }

    [Parameter(Position = 2, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(EncodingCompleter))]
    [EncodingArgumentTransformation]
    public Encoding? CsvEncoding { get; set; }

    [Parameter(Position = 3, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<QueueItemCommitTypeItems>))]
    public string? CommitType { get; set; }

    // The web upload's "Dynamic typing" check box: numbers, booleans, empty cells and
    // ISO date-times become typed values instead of strings (QueueItemDynamicTyping).
    [Parameter(ValueFromPipelineByPropertyName = true)]
    public SwitchParameter DynamicTyping { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    // Delegates to the shared multi-line CSV reader; kept here (test-pinned name)
    // so the web-equivalence test and CreateItemData call it unchanged.
    internal static bool ParseCsvContent(string csvFilePath, Encoding? csvEncoding, int rowCap, out List<string> headers, out List<List<string>> contents, out List<CSVParseError> errorInfo, out int totalDataRows)
        => CsvHelper.MultilineCsv.Parse(csvFilePath, csvEncoding, rowCap, out headers, out contents, out errorInfo, out totalDataRows);

    // Uncapped overload (retains every row). Pinned signature used by the
    // web-equivalence unit test.
    internal static bool ParseCsvContent(string csvFilePath, Encoding? csvEncoding, out List<string> headers, out List<List<string>> contents, out List<CSVParseError> errorInfo)
        => ParseCsvContent(csvFilePath, csvEncoding, int.MaxValue, out headers, out contents, out errorInfo, out _);

    // Rows per BulkAddQueueItems request. The web sends a whole file (up to 15,000 rows) as
    // one request; on Automation Cloud that took 5.5 minutes once and failed with a 500 after
    // 6 minutes another time, leaving 10,903 of the 15,000 items created
    // (TestData/QueueItemUploadMatrix 51). 1,000 rows take about 20 seconds.
    internal const int ItemsPerRequest = 1000;

    // AllOrNothing cannot be split without losing its all-or-nothing guarantee, so it still
    // goes as one request; above this many rows that request risks the server's time limit.
    internal const int AllOrNothingWarningRows = 5000;

    // The canonical spelling, so "allornothing" is not split like ProcessAllIndependently
    // (SplitIntoRequests and ImportItems compare ordinally). Empty means the default;
    // an unknown value is passed on for the server to reject.
    internal static string NormalizeCommitType(string? commitType)
    {
        if (string.IsNullOrEmpty(commitType)) return "ProcessAllIndependently";
        return QueueItemCommitTypeItems.Items.FirstOrDefault(c => c.Equals(commitType, StringComparison.OrdinalIgnoreCase)) ?? commitType;
    }

    // The per-row item JSON, in row order, split into requests: one for AllOrNothing, else
    // ItemsPerRequest each. Returns (start index, items) per request.
    internal static List<(int start, List<string> items)> SplitIntoRequests(IReadOnlyList<string> items, string commitType)
    {
        int size = NormalizeCommitType(commitType) == "AllOrNothing" ?Math.Max(items.Count, 1) : ItemsPerRequest;
        var requests = new List<(int, List<string>)>();
        for (int i = 0; i < items.Count; i += size)
        {
            requests.Add((i, items.Skip(i).Take(size).ToList()));
        }
        return requests;
    }

    private static string BuildRequestBody(IEnumerable<string> items, string commitType, string queueName)
        => "{\"queueItems\":[\n" + string.Join(",\n", items) + "],\n"
            + "\"commitType\":" + JsonSerializer.Serialize(commitType) + ",\n"
            + "\"queueName\":" + JsonSerializer.Serialize(queueName) + "\n}";

    private (int rowNum, List<string> items) CreateItemData(string csvFilePath, out List<CSVParseError> errorInfo)
    {
        // The web detects the delimiter (';', tab, '|', ...), so a file it accepts reads the same here.
        var delimiter = CsvHelper.MultilineCsv.DetectDelimiter(csvFilePath, CsvEncoding);
        CsvHelper.MultilineCsv.Parse(csvFilePath, CsvEncoding, CsvUploadLimit.MaxRows, out var rawHeaders, out var contents, out errorInfo, out var totalDataRows, delimiter);
        var headers = RenameDuplicateHeaders(rawHeaders);

        // Reject oversized files before building the (potentially large) JSON
        // payload, while keeping the exact row count for the caller's web-parity
        // "{rowCount} records" message. ParseCsvContent retained at most MaxRows
        // rows, so an oversized file is never materialized in full.
        if (CsvUploadLimit.RowLimitError(totalDataRows) is not null)
        {
            return (totalDataRows, []);
        }

        var items = new List<string>(contents.Count);
        int currentRowNumber = 0;
        foreach (var values in contents)
        {
            ++currentRowNumber;
            items.Add(BuildQueueItemJson(headers, values, currentRowNumber + 1, errorInfo, DynamicTyping.IsPresent));
        }

        if (errorInfo.Any())
        {
            return (0, []);
        }

        return (totalDataRows, items);
    }

    // A repeated header is renamed the way the web's CSV reader (Papa Parse) does:
    // the n-th repeat of "Value" becomes "Value_n", skipping any name already in the
    // header row or given out before. Value,Value,Value -> Value,Value_1,Value_2;
    // Value_1,Value,Value -> Value_1,Value,Value_2. A repeated Reference or Priority
    // thus becomes an ordinary SpecificContent key (Reference_1). Without this, the
    // later column overwrote the earlier one's value
    // (TestData/QueueItemUploadMatrix 23, 2g, 2h, 2i).
    internal static List<string> RenameDuplicateHeaders(IReadOnlyList<string> headers)
    {
        var used = new HashSet<string>(headers, StringComparer.Ordinal);
        var count = new Dictionary<string, int>(StringComparer.Ordinal);
        var result = new List<string>(headers.Count);
        foreach (var header in headers)
        {
            if (!count.TryGetValue(header, out var n))
            {
                count[header] = 1;
                result.Add(header);
                continue;
            }
            string renamed;
            do { renamed = $"{header}_{n++}"; } while (used.Contains(renamed));
            used.Add(renamed);
            count[header] = count[header] + 1;
            result.Add(renamed);
        }
        return result;
    }

    // CSV row -> queue item JSON (Priority text/number mapping, Reference, and
    // everything else as SpecificContent string values). Extracted from the
    // CreateItemData loop verbatim so the web-equivalence test can compare the
    // cmdlet's items against a captured web BulkAddQueueItem body offline.
    // rowNumber counts the header as row 1, like every other error this cmdlet reports.
    // The web is inconsistent here: it numbers a Priority "Field Error" from the first data
    // row (TestData/QueueItemUploadMatrix 12: "CSV Row: 2" for file row 3) but its quote
    // errors from the header; this cmdlet uses the header-based number throughout.
    internal static string BuildQueueItemJson(IReadOnlyList<string> headers, IReadOnlyList<string> values, int rowNumber, List<CSVParseError> errorInfo, bool dynamicTyping = false)
    {
        var queueItem = new QueueItemData4CsvImport();
        foreach (var column in headers.Zip(values, (header, value) => (header, value)))
        {
            var (header, value) = column;
            switch (header)
            {
                case "Priority":
                    // The web trims Priority (" High " is accepted as High) but no other
                    // column (TestData/QueueItemUploadMatrix 14_priority_spaced.csv).
                    value = value.Trim();
                    if (!string.IsNullOrEmpty(value))
                    {
                        switch (value.ToLower())
                        {
                            case "1":
                            case "low":
                                queueItem.Priority = "Low";
                                break;
                            case "2":
                            case "normal":
                                queueItem.Priority = "Normal";
                                break;
                            case "3":
                            case "high":
                                queueItem.Priority = "High";
                                break;
                            default:
                                errorInfo.Add(new CSVParseError(rowNumber, "Field Error", header, "Invalid Data found"));
                                break;
                        }
                    }
                    break;
                case "Reference":
                    // Must be set even when the value is ""
                    queueItem.Reference = dynamicTyping ? QueueItemDynamicTyping.ToReference(value) : value;
                    break;
                default: // SpecificContent
                    if (!string.IsNullOrEmpty(header))
                    {
                        queueItem.SpecificContent ??= new Dictionary<string, object>();
                        queueItem.SpecificContent[header] = dynamicTyping ? QueueItemDynamicTyping.ToValue(value)! : value;
                    }
                    break;
            }
        }
        return System.Text.Json.JsonSerializer.Serialize(queueItem, JsonTools.jsoWhenWritingNull);
    }

    // The "queueItems" array as the cmdlet builds it from parsed CSV rows.
    internal static string BuildQueueItemsArrayJson(IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows, List<CSVParseError> errorInfo, bool dynamicTyping = false)
    {
        var lines = new List<string>();
        var renamed = RenameDuplicateHeaders(headers);
        int rowNumber = 0;
        foreach (var values in rows)
        {
            ++rowNumber;
            lines.Add(BuildQueueItemJson(renamed, values, rowNumber + 1, errorInfo, dynamicTyping));
        }
        return "[" + string.Join(",", lines) + "]";
    }

    // Sends one file's items to one queue in requests of ItemsPerRequest (one request for
    // AllOrNothing) and emits a single response for the file, as before: Success only when
    // every request succeeded, FailedItems from all of them with Ordinal counted from the
    // start of the file. StopOnFirstFailure stops after the request that had a failure.
    // A request that throws stops the file; the error names the CSV rows already imported,
    // the rows whose outcome is unknown, and the rows not sent, so the file can be resumed.
    private void ImportItems(OrchDriveInfo drive, Folder folder, QueueDefinition queue, string target, string csvPath, List<string> items, CancellationToken token)
    {
        string commitType = CommitType!;
        if (commitType == "AllOrNothing" && items.Count > AllOrNothingWarningRows)
        {
            WriteWarning($"{target}: {items.Count} rows are sent as one AllOrNothing request, which the server may not finish within its time limit. Split the file, or use -CommitType ProcessAllIndependently to send {ItemsPerRequest} rows per request.");
        }

        var requests = SplitIntoRequests(items, commitType);
        bool success = true;
        string? message = null;
        var failed = new List<FailedQueueItem>();

        // Row numbers in the messages below: item i is CSV row i + 2 (the header is row 1;
        // blank lines are not counted and a value with line breaks is one row).
        int lastFileRow = items.Count + 1;
        string SentSoFar(int start) => start == 0 ? ""
            : $"rows 2-{start + 1} were sent" + (failed.Count > 0 ? $" ({failed.Count} of them failed and are output above)" : "") + "; ";

        using ProgressReporter? reporter = requests.Count > 1 ? new ProgressReporter(this, items.Count, "Items") : null;
        foreach (var (start, chunk) in requests)
        {
            if (token.IsCancellationRequested && start > 0)
            {
                // Stopped between requests: say what was sent so the file can be resumed.
                WriteFailedItems(failed, queue, csvPath);
                throw new OperationCanceledException($"Import cancelled: {SentSoFar(start)}rows {start + 2}-{lastFileRow} were not sent.", token);
            }
            token.ThrowIfCancellationRequested();
            reporter?.WriteProgress(start, target);

            BulkOperationResponseDtoOfFailedQueueItem? response;
            try
            {
                response = drive.OrchAPISession.BulkAddQueueItem(folder.Id ?? 0, BuildRequestBody(chunk, commitType, queue.Name ?? ""));
            }
            catch (Exception ex) when (requests.Count > 1)
            {
                // Includes the HttpClient timeout, a TaskCanceledException: the request does not
                // take 'token', so an OperationCanceledException here is never Ctrl+C.
                WriteFailedItems(failed, queue, csvPath);
                int firstRow = start + 2, lastRow = start + chunk.Count + 1;
                string rest = lastRow < lastFileRow ? $"; rows {lastRow + 1}-{lastFileRow} were not sent" : "";
                throw new InvalidOperationException($"{SentSoFar(start)}rows {firstRow}-{lastRow} may be partly imported{rest}. {ex.Message}", ex);
            }
            if (response is null) continue;

            if (!response.Success.GetValueOrDefault())
            {
                success = false;
                message ??= response.Message;
            }
            foreach (var failedItem in response.FailedItems ?? [])
            {
                failedItem.Ordinal += start;
                failed.Add(failedItem);
            }
            if (commitType == "StopOnFirstFailure" && (!response.Success.GetValueOrDefault() || response.FailedItems?.Length > 0))
            {
                break;
            }
        }

        var result = new BulkOperationResponseDtoOfFailedQueueItem { Success = success, Message = message, FailedItems = [.. failed] };
        if (success)
        {
            // Emit the bulk-operation response on success so callers can
            // assert .Success / read the summary; rejected rows are surfaced
            // below as FailedQueueItem.
            WriteObject(result);
        }
        else
        {
            WriteError(new ErrorRecord(new OrchException(target, message!), "ImportQueueItemError", ErrorCategory.InvalidOperation, target));
            WriteFailedItems(failed, queue, csvPath);
        }
    }

    private void WriteFailedItems(List<FailedQueueItem> failed, QueueDefinition queue, string csvPath)
    {
        foreach (var failedItem in failed)
        {
            failedItem.Category = $"{queue.Name} ({csvPath})";
            failedItem.QueueName = queue.Name;
            failedItem.CsvPath = csvPath;
            WriteObject(failedItem);
        }
    }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(EffectivePath(Path, LiteralPath));
        var wpName = Name.ConvertToWildcardPatternList();

        CommitType = NormalizeCommitType(CommitType);

        var csvPathExpanded = SessionState.ExpandLocalPath(ImportCsv, "*.csv");

        var queueItemData = new List<(string csvPath, (int rowNum, List<string> items))>();
        foreach (var p in csvPathExpanded)
        {
            try
            {
                var data = CreateItemData(p.FullPath, out var errorInfo);
                if (errorInfo is not null && errorInfo.Count != 0)
                {
                    WriteObject(errorInfo, true);
                    return;
                }
                // Match the web "Upload Items" 15,000-row cap (reject, don't chunk).
                var limitError = CsvUploadLimit.RowLimitError(data.rowNum);
                if (limitError is not null)
                {
                    WriteError(new ErrorRecord(new OrchException(p.FullPath, limitError), "MaxRowsExceeded", ErrorCategory.LimitsExceeded, p.FullPath));
                    continue;
                }
                queueItemData.Add((p.FullPath, data));
            }
            catch (Exception ex)
            {
                string target;
                if (ex.Data.Contains("target"))
                {
                    target = ex.Data["target"]!.ToString();
                }
                else
                {
                    target = p.FullPath;
                }
                WriteError(new ErrorRecord(new OrchException(target!, ex), "ImportQueueItemError", ErrorCategory.InvalidOperation, target));
            }
        }

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            string targetFolder = folder.GetPSPath();
            try
            {
                var queues = drive.Queues.Get(folder);
                foreach (var queue in queues.FilterByWildcards(q => q?.Name, wpName))
                {
                    foreach (var queueItem in queueItemData.WithCancellation(cancelHandler.Token))
                    {
                        string target = System.IO.Path.Combine(targetFolder, queue.Name ?? "");
                        try
                        {
                            if (ShouldProcess(target, $"Import Queue Item {queueItem.csvPath} ({queueItem.Item2.rowNum} rows)"))
                            {
                                ImportItems(drive, folder, queue, target, queueItem.csvPath, queueItem.Item2.items, cancelHandler.Token);
                            }
                        }
                        catch (Exception ex)
                        {
                            var errorRecord = new ErrorRecord(new OrchException(target, ex), "ImportQueueItemError", ErrorCategory.InvalidOperation, target);
                            WriteError(errorRecord);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var errorRecord = new ErrorRecord(new OrchException(targetFolder, ex), "RemoveQueueError", ErrorCategory.InvalidOperation, targetFolder);
                WriteError(errorRecord);
            }
        }
    }
}
