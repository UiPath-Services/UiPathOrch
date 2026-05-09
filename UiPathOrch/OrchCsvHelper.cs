using System.Collections;
using System.Management.Automation;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands.CsvHelper;

internal class TupleComparer<T> : IEqualityComparer<T>
{
    public bool Equals(T? x, T? y)
    {
        if (x is null || y is null) return false;

        // Tuple �̑S�v�f���r����
        return StructuralComparisons.StructuralEqualityComparer.Equals(x, y);
    }

    public int GetHashCode(T obj)
    {
        if (obj is null) return 0;

        // Tuple �̑S�v�f���l�������n�b�V���R�[�h�𐶐�
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
