using System.Collections;
using System.Management.Automation;
using System.Text;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands.CsvHelper;

internal class TupleComparer<T> : IEqualityComparer<T>
{
    public bool Equals(T? x, T? y)
    {
        if (x is null || y is null) return false;

        // Compare every element of the tuple structurally.
        return StructuralComparisons.StructuralEqualityComparer.Equals(x, y);
    }

    public int GetHashCode(T obj)
    {
        if (obj is null) return 0;

        // Build a hash code from every element value of the tuple.
        return StructuralComparisons.StructuralEqualityComparer.GetHashCode(obj);
    }
}

internal static class CsvLine
{
    /// <summary>
    /// Splits a single CSV line into fields, handling quoted fields and "" escaped quotes
    /// per RFC 4180. Does not support fields that span multiple lines (the only callers pass
    /// a single physical line). Surrounding whitespace around unquoted fields is preserved
    /// — callers that want trimming should do so explicitly.
    /// </summary>
    public static List<string> Split(string line)
    {
        var fields = new List<string>();
        var sb = new System.Text.StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            else
            {
                if (c == ',')
                {
                    fields.Add(sb.ToString());
                    sb.Clear();
                }
                else if (c == '"' && sb.Length == 0)
                {
                    inQuotes = true;
                }
                else
                {
                    sb.Append(c);
                }
            }
        }
        fields.Add(sb.ToString());
        return fields;
    }
}

// Full multi-line / quote-aware CSV reader (RFC-4180-ish). Unlike CsvLine.Split
// (a single physical line), it handles a quoted field that spans multiple lines
// (an embedded newline) and "" escaped quotes — matching the Orchestrator web
// "Upload Items" parser. Shared by Import-OrchQueueItem and
// Import-OrchTestDataQueueItem so both read CSVs identically.
//
// rowCap bounds how many data rows are RETAINED in 'contents' (so an oversized
// file is never fully materialized); totalDataRows reports the TRUE row count
// regardless. Pass int.MaxValue to retain every row. Returns true when no parse
// error was recorded.
internal static class MultilineCsv
{
    internal static bool Parse(string csvFilePath, Encoding? csvEncoding, int rowCap, out List<string> headers, out List<List<string>> contents, out List<CSVParseError> errorInfo, out int totalDataRows)
    {
        headers = [];
        contents = [];
        errorInfo = [];
        totalDataRows = 0;
        bool inQuotes = false;
        StringBuilder currentField = new();
        List<string> currentRow = [];
        int currentRowNumber = 0;

        csvEncoding ??= Encoding.UTF8;

        foreach (var fullLine in File.ReadLines(csvFilePath, csvEncoding))
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
                ++totalDataRows;
                // Stop retaining rows past the cap (the file will be rejected),
                // but keep counting so the exact total reaches the caller.
                if (contents.Count < rowCap)
                {
                    contents.Add(new List<string>(currentRow));
                }
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
}

internal class CsvLineBase
{
    // Expression<T> ���g���΁A�����������Ȍ��ɏ������������ǁA�����܂ł���̂��Ȃ��B�B
    // OrchDriveInfo �� OrchDuDriveInfo �̃x�[�X������āANameColonSeparator �͂������Ɉړ����ׂ����B
    // �ŁAPSDriveInfo ���� OrchDriveInfoBase ���g�������ǂ��B
    // ���̂܂܂ł��������ǁA���� string �̍\�z�����ʂ��ȁB�B
    // Multi-row CSV-aggregation helpers used by Add-OrchUser when several CSV rows
    // identify the same user (e.g. one row per role): the first row populates the
    // CsvLine via the constructor, subsequent rows pass through Update() which calls
    // these helpers to detect collisions. The intended semantic is "first-row wins,
    // warn on disagreement". All three skip null/unspecified inputs (null/empty for
    // strings, null/0 for ints, null/empty/unparseable for bools) so that an empty
    // cell on a later row does NOT silently clobber a value set by an earlier row.
    //
    // Note on the int 0 sentinel: when a CSV column is empty, the PowerShell binder
    // coerces it to default(int) = 0 for int? parameters, so we cannot distinguish
    // "user specified 0" from "user specified nothing" — we treat both as unspecified
    // here, consistent with AssignNumberIfNotNullOrZero in OrchExtensions.

    internal static void AssignStringValue(IWritableHost host, string driveName, string identityName, string? currentValue, string? newValue, Action<string?> setter)
    {
        if (string.IsNullOrEmpty(newValue)) return; // unspecified on this row → leave prior row's value alone
        if (string.IsNullOrEmpty(currentValue))
        {
            setter(newValue); // first real value across rows; not a collision
            return;
        }
        if (currentValue != newValue)
        {
            host.WriteWarning($"'{driveName}:{Path.DirectorySeparatorChar}{identityName}': '{nameof(newValue)}' has been specified multiple times. Using the previously specified value '{currentValue}'.");
        }
        else
        {
            setter(newValue);
        }
    }

    internal static void AssignIntValue(IWritableHost host, string driveName, string identityName, int? currentValue, int? newValue, Action<int?> setter)
    {
        if (newValue is null || newValue == 0) return; // unspecified on this row (CSV empty cell coerces to 0)
        if (currentValue is null || currentValue == 0)
        {
            setter(newValue); // first real value across rows; not a collision
            return;
        }
        if (currentValue != newValue)
        {
            host.WriteWarning($"'{driveName}:{Path.DirectorySeparatorChar}{identityName}': '{nameof(newValue)}' has been specified multiple times. Using the previously specified value {currentValue}.");
        }
        else
        {
            setter(newValue);
        }
    }

    internal static void AssignBoolValue(IWritableHost host, string driveName, string identityName, bool? currentValue, string? newValue, Action<bool?> setter)
    {
        if (string.IsNullOrEmpty(newValue)) return;
        if (!bool.TryParse(newValue, out var boolValue)) return;
        if (currentValue is null)
        {
            setter(boolValue); // first real value across rows; not a collision
            return;
        }
        if (currentValue != boolValue)
        {
            host.WriteWarning($"'{driveName}:{Path.DirectorySeparatorChar}{identityName}': '{nameof(newValue)}' has been specified multiple times. Using the previously specified value {currentValue}.");
        }
        else
        {
            setter(boolValue);
        }
    }
}
