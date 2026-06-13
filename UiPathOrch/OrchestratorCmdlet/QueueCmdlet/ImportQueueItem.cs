using System.Management.Automation;
using UiPath.PowerShell.Positional;
using System.Text;
using System.Text.Json;
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

    private (int rowNum, string content) CreateItemData(string csvFilePath, out List<CSVParseError> errorInfo)
    {
        ParseCsvContent(csvFilePath, CsvEncoding, CsvUploadLimit.MaxRows, out var headers, out var contents, out errorInfo, out var totalDataRows);

        // Reject oversized files before building the (potentially large) JSON
        // payload, while keeping the exact row count for the caller's web-parity
        // "{rowCount} records" message. ParseCsvContent retained at most MaxRows
        // rows, so an oversized file is never materialized in full.
        if (CsvUploadLimit.RowLimitError(totalDataRows) is not null)
        {
            return (totalDataRows, "");
        }

        StringBuilder sb = new();
        sb.Append("{\"queueItems\":[\n");

        int currentRowNumber = 0;
        foreach (var values in contents)
        {
            ++currentRowNumber;
            string strLine = BuildQueueItemJson(headers, values, currentRowNumber, errorInfo);
            if (currentRowNumber == 1)
            {
                sb.Append(strLine);
            }
            else
            {
                sb.Append(",\n" + strLine);
            }
        }

        sb.Append("],\n");

        if (!string.IsNullOrEmpty(CommitType))
        {
            sb.Append("\"commitType\":" + JsonSerializer.Serialize(CommitType) + ",\n");
        }

        if (errorInfo.Any())
        {
            return (0, "");
        }

        return (totalDataRows, sb.ToString());
    }

    // CSV row -> queue item JSON (Priority text/number mapping, Reference, and
    // everything else as SpecificContent string values). Extracted from the
    // CreateItemData loop verbatim so the web-equivalence test can compare the
    // cmdlet's items against a captured web BulkAddQueueItem body offline.
    internal static string BuildQueueItemJson(IReadOnlyList<string> headers, IReadOnlyList<string> values, int rowNumber, List<CSVParseError> errorInfo)
    {
        var queueItem = new QueueItemData4CsvImport();
        foreach (var column in headers.Zip(values, (header, value) => (header, value)))
        {
            var (header, value) = column;
            switch (header)
            {
                case "Priority":
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
                    queueItem.Reference = value;
                    break;
                default: // SpecificContent
                    if (!string.IsNullOrEmpty(header))
                    {
                        queueItem.SpecificContent ??= new Dictionary<string, object>();
                        queueItem.SpecificContent[header] = value;
                    }
                    break;
            }
        }
        return System.Text.Json.JsonSerializer.Serialize(queueItem, JsonTools.jsoWhenWritingNull);
    }

    // The "queueItems" array as the cmdlet builds it from parsed CSV rows.
    internal static string BuildQueueItemsArrayJson(IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows, List<CSVParseError> errorInfo)
    {
        var lines = new List<string>();
        int rowNumber = 0;
        foreach (var values in rows)
        {
            ++rowNumber;
            lines.Add(BuildQueueItemJson(headers, values, rowNumber, errorInfo));
        }
        return "[" + string.Join(",", lines) + "]";
    }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(EffectivePath(Path, LiteralPath));

        if (string.IsNullOrEmpty(CommitType))
        {
            CommitType = "ProcessAllIndependently";
        }

        var csvPathExpanded = SessionState.ExpandLocalPath(ImportCsv, "*.csv");

        var queueItemData = new List<(string csvPath, (int rowNum, string content))>();
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
                foreach (var queue in queues.FilterByNames(q => q?.Name, Name))
                {
                    foreach (var queueItem in queueItemData.WithCancellation(cancelHandler.Token))
                    {
                        string target = System.IO.Path.Combine(targetFolder, queue.Name ?? "");
                        try
                        {
                            if (ShouldProcess(target, $"Import Queue Item {queueItem.csvPath} ({queueItem.Item2.rowNum} rows)"))
                            {
                                var response = drive.OrchAPISession.BulkAddQueueItem(folder.Id ?? 0, queueItem.Item2.content + $"\"queueName\":{JsonSerializer.Serialize(queue.Name ?? "")}\n}}");
                                if (response is not null)
                                {
                                    if (response.Success.GetValueOrDefault())
                                    {
                                        // Emit the bulk-operation response on success so callers can
                                        // assert .Success / read the summary; rejected rows are surfaced
                                        // below as FailedQueueItem.
                                        WriteObject(response);
                                    }
                                    else
                                    {
                                        WriteError(new ErrorRecord(new OrchException(target, response.Message!), "ImportQueueItemError", ErrorCategory.InvalidOperation, target));
                                        if (response.FailedItems is not null)
                                        {
                                            foreach (var failedItem in response.FailedItems!)
                                            {
                                                failedItem.Category = $"{queue.Name} ({queueItem.csvPath})";
                                                failedItem.QueueName = queue.Name;
                                                failedItem.CsvPath = queueItem.csvPath;
                                                WriteObject(failedItem);
                                            }
                                        }
                                    }
                                }
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
