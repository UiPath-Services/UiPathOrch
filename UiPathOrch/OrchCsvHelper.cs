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
    // Physical lines with the terminator each one ended with ("\r\n", "\n", "\r", or "" at
    // EOF). File.ReadLines drops the terminator, so a newline inside a quoted field came
    // back as "\n" whatever it was; the web keeps CRLF and bare CR as written.
    internal static IEnumerable<(string line, string eol)> ReadLinesWithEol(TextReader reader)
    {
        var sb = new StringBuilder();
        int ch;
        while ((ch = reader.Read()) != -1)
        {
            if (ch == '\n')
            {
                yield return (sb.ToString(), "\n");
                sb.Clear();
            }
            else if (ch == '\r')
            {
                if (reader.Peek() == '\n')
                {
                    reader.Read();
                    yield return (sb.ToString(), "\r\n");
                }
                else
                {
                    yield return (sb.ToString(), "\r");
                }
                sb.Clear();
            }
            else
            {
                sb.Append((char)ch);
            }
        }
        if (sb.Length > 0) yield return (sb.ToString(), "");
    }

    // Delimiter detection as the web's CSV reader (Papa Parse) does it: each candidate
    // splits the first 10 non-blank records; the winner has the smallest change in field
    // count from record to record, then the larger average field count, which must exceed
    // 1.99; earlier candidates win ties, so ',' wins over ';' at equal counts. Returns
    // null when no candidate qualifies; the web then rejects the file with
    // "Undetectable Delimiter" (a single-column CSV), and callers use ',' instead.
    // Pinned by TestData/QueueItemUploadMatrix 3a-3g.
    internal static readonly char[] DelimiterCandidates = [',', '\t', '|', ';', '\u001E', '\u001F'];

    internal static char? GuessDelimiter(string text)
    {
        char? best = null;
        int? bestDelta = null;
        double? maxFieldCount = null;
        foreach (var delimiter in DelimiterCandidates)
        {
            var counts = PreviewFieldCounts(text, delimiter, 10);
            int delta = 0;
            int? previous = null;
            foreach (var count in counts)
            {
                if (previous is null) { previous = count; continue; }
                delta += Math.Abs(count - previous.Value);
                previous = count;
            }
            double average = counts.Count > 0 ? (double)counts.Sum() / counts.Count : 0;
            if ((bestDelta is null || delta <= bestDelta) && (maxFieldCount is null || average > maxFieldCount) && average > 1.99)
            {
                best = delimiter;
                bestDelta = delta;
                maxFieldCount = average;
            }
        }
        return best;
    }

    // Field count of each of the first 'max' non-blank records split by 'delimiter'
    // (quotes and embedded newlines honored; malformed quotes are tolerated here).
    private static List<int> PreviewFieldCounts(string text, char delimiter, int max)
    {
        var counts = new List<int>();
        int fields = 1;
        bool inQuotes = false, fieldStart = true, recordEmpty = true;
        for (int i = 0; i < text.Length && counts.Count < max; i++)
        {
            char c = text[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"') i++;
                    else inQuotes = false;
                }
                continue;
            }
            if (c == '"' && fieldStart) { inQuotes = true; fieldStart = false; recordEmpty = false; continue; }
            if (c == delimiter) { fields++; fieldStart = true; recordEmpty = false; continue; }
            if (c == '\r' || c == '\n')
            {
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                if (!recordEmpty) counts.Add(fields);
                fields = 1; fieldStart = true; recordEmpty = true;
                continue;
            }
            fieldStart = false;
            recordEmpty = false;
        }
        if (!recordEmpty && counts.Count < max) counts.Add(fields);
        return counts;
    }

    // The delimiter for a file: the guessed one, or ',' when none qualifies.
    // Reads only as far as the first 10 records go for every candidate (an 11th record has
    // started), so a file over the row limit is not held in memory in full; Parse streams it.
    internal static char DetectDelimiter(string csvFilePath, Encoding? csvEncoding)
    {
        using var reader = new StreamReader(csvFilePath, csvEncoding ?? Encoding.UTF8);
        var text = new StringBuilder();
        var buffer = new char[64 * 1024];
        int read;
        while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
        {
            text.Append(buffer, 0, read);
            var s = text.ToString();
            if (DelimiterCandidates.All(d => PreviewFieldCounts(s, d, 11).Count == 11)) break;
        }
        return GuessDelimiter(text.ToString()) ?? ',';
    }

    internal static bool Parse(string csvFilePath, Encoding? csvEncoding, int rowCap, out List<string> headers, out List<List<string>> contents, out List<CSVParseError> errorInfo, out int totalDataRows, char delimiter = ',')
    {
        headers = [];
        contents = [];
        errorInfo = [];
        totalDataRows = 0;
        bool inQuotes = false;
        StringBuilder currentField = new();
        List<string> currentRow = [];
        int currentRowNumber = 0;
        // Row number reported in errors, numbered as the web does: the header is row 1,
        // and a rejected row still takes its number (TestData/QueueItemUploadMatrix
        // 39_text_after_closing_quote.csv: the web reports row 2).
        int csvRow = 0;

        csvEncoding ??= Encoding.UTF8;

        // Same reader File.ReadLines uses: a BOM, when present, overrides csvEncoding.
        using var reader = new StreamReader(csvFilePath, csvEncoding);
        foreach (var (line, eol) in ReadLinesWithEol(reader))
        {
            bool lineHasError = false; // Flag to track whether this line has an error

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
                        if (!inQuotes && i < line.Length - 1 && line[i + 1] != delimiter && line[i + 1] != '\n')
                        {
                            errorInfo.Add(new CSVParseError(++csvRow, "Quotes", "N/A", "Invalid Quotes"));
                            lineHasError = true;
                            // Break out of the loop to skip the rest of the line
                            break;
                        }
                    }
                }
                else if (c == delimiter && !inQuotes)
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

            // Still inside a quoted field at the physical line end: the field
            // carries an embedded newline. Keep it as written and accumulate the next line.
            if (inQuotes)
            {
                currentField.Append(eol);
                continue;
            }

            // Not inside quotes: this physical line ends a record. (The previous
            // `currentRow.Count != 0` guard wrongly required a comma, so a
            // single-column row — no comma — never finalized and was treated as a
            // multiline field. End-of-line with balanced quotes always ends a row,
            // regardless of column count.) Skip a wholly blank line rather than
            // emitting an empty row.
            if (currentField.Length == 0 && currentRow.Count == 0)
            {
                continue;
            }

            currentRow.Add(currentField.ToString());
            currentField.Clear();
            ++csvRow;
            if (currentRowNumber != 0 && headers.Count < currentRow.Count)
            {
                currentRow.Clear();
                errorInfo.Add(new CSVParseError(csvRow, "Fields Mismatch", "N/A", "Too Many Fields"));
                continue;
            }
            // The web rejects a row with fewer fields than the header as well
            // (TestData/QueueItemUploadMatrix 31_ragged_short_row.csv); it used to be
            // accepted here with the missing columns silently dropped.
            if (currentRowNumber != 0 && headers.Count > currentRow.Count)
            {
                currentRow.Clear();
                errorInfo.Add(new CSVParseError(csvRow, "Fields Mismatch", "N/A", "Too Few Fields"));
                continue;
            }

            // Process the header row separately
            if (currentRowNumber == 0)
            {
                headers = new List<string>(currentRow);
                ++currentRowNumber;
            }
            else // currentRow has at least one field here
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
            errorInfo.Add(new CSVParseError(csvRow + 1, "Unmatched Quotes", "", "A field contains unmatched quotes"));
        }

        return !errorInfo.Any();
    }
}

internal class CsvLineBase
{
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
