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
[OutputType(typeof(Entities.FailedQueueItem))]
public class ImportQueueItemCommand : OrchestratorPSCmdlet
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

    public bool ParseCsvContent(string csvFilePath, out List<string> headers, out List<List<string>> contents, out List<CSVParseError> errorInfo)
    {
        headers = [];
        contents = [];
        errorInfo = [];
        bool inQuotes = false;
        StringBuilder currentField = new();
        List<string> currentRow = [];
        int currentRowNumber = 0;

        if (CsvEncoding is null)
        {
            CsvEncoding = Encoding.UTF8;
        }

        foreach (var fullLine in File.ReadLines(csvFilePath, CsvEncoding))
        {
            bool lineHasError = false; // Flag to track whether this line has an error
            string line = fullLine.TrimEnd('\r');

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                // Toggle quoting status and handle escaped quotes
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    { // Escaped quote
                        currentField.Append('"');
                        i++; // Skip the next quote
                    }
                    else if (!inQuotes && currentField.Length > 0)
                    {
                        currentField.Append('"');
                    }
                    else
                    {
                        inQuotes = !inQuotes; // Toggle quoting status
                                              // Check if the closing quote is not properly followed by a comma or end of line
                        if (!inQuotes && i < line.Length - 1 && line[i + 1] != ',' && line[i + 1] != '\n')
                        {
                            errorInfo.Add(new CSVParseError(currentRowNumber, "Quotes", "N/A", "Invalid Quotes"));
                            lineHasError = true;
                            // Break out of the loop to skip the rest of the line
                            break;
                        }
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    // End of a field
                    currentRow.Add(currentField.ToString());
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(c);
                }

                // After the loop
                if (lineHasError)
                {
                    // If an error was detected, clear the currentField and currentRow to start fresh on the next line
                    currentField.Clear();
                    currentRow.Clear();
                    break; // Skip to the next line in the outer loop
                }
            }

            if (lineHasError)
            {
                // Discard all data for lines that had errors
                currentField.Clear();
                currentRow.Clear();
                continue; // Proceed to the next line
            }

            // Add the last field if we're not inside quotes (end of line)
            if (!inQuotes && currentRow.Count != 0)
            {
                currentRow.Add(currentField.ToString());
                currentField.Clear();
                if (currentRowNumber != 0 && headers.Count() < currentRow.Count())
                {
                    currentField.Clear();
                    currentRow.Clear();
                    errorInfo.Add(new CSVParseError(currentRowNumber, "Fields Mismatch", "N/A", "Too Many Fields"));
                    continue;
                }
            }
            else
            {
                // Multiline field, add newline character and continue accumulating
                currentField.Append('\n');
                continue;
            }

            // Process the header row separately
            if (currentRowNumber == 0)
            {
                headers = new List<string>(currentRow);
                ++currentRowNumber;
            }
            else if (currentRow.Count > 0) // Avoid adding empty lines as rows
            {
                contents.Add(new List<string>(currentRow));
                ++currentRowNumber;
            }

            currentRow = new List<string>(); // Reset for the next row
        }

        // Final check for unmatched quotes
        if (inQuotes)
        {
            errorInfo.Add(new CSVParseError(currentRowNumber, "Unmatched Quotes", "", "A field contains unmatched quotes"));
        }

        return !errorInfo.Any();
    }

    private (int rowNum, string content) CreateItemData(string csvFilePath, out List<CSVParseError> errorInfo)
    {
        StringBuilder sb = new();
        sb.Append("{\"queueItems\":[\n");

        ParseCsvContent(csvFilePath, out var headers, out var contents, out errorInfo);

        int currentRowNumber = 0;
        foreach (var values in contents)
        {
            ++currentRowNumber;
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
                                    errorInfo.Add(new CSVParseError(currentRowNumber, "Field Error", header, "Invalid Data found"));
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
            string strLine = System.Text.Json.JsonSerializer.Serialize(queueItem, JsonTools.jsoWhenWritingNull);
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

        return (contents.Count, sb.ToString());
    }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(Path);
        var wpName = Name.ConvertToWildcardPatternList();

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
                queueItemData.Add((p.FullPath, CreateItemData(p.FullPath, out var errorInfo)));
                if (errorInfo is not null && errorInfo.Count != 0)
                {
                    WriteObject(errorInfo, true);
                    return;
                }
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
                    foreach (var queueItem in queueItemData)
                    {
                        cancelHandler.Token.ThrowIfCancellationRequested();

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
