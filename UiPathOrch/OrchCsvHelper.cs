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
    internal static void AssignStringValue(IWritableHost host, PSDriveInfo drive, string identityName, string? currentValue, string? newValue, Action<string?> setter)
    {
        if (!string.IsNullOrEmpty(newValue) && currentValue != newValue)
        {
            host.WriteWarning($"'{drive.Name}:{Path.DirectorySeparatorChar}{identityName}': '{nameof(newValue)}' has been specified multiple times. Using the previously specified value '{currentValue}'.");
        }
        else
        {
            setter(newValue);
        }
    }

    internal static void AssignIntValue(IWritableHost host, PSDriveInfo drive, string identityName, int? currentValue, int? newValue, Action<int?> setter)
    {
        if (newValue is not null && newValue != 0 && currentValue != newValue)
        {
            host.WriteWarning($"'{drive.Name}:{Path.DirectorySeparatorChar}{identityName}': '{nameof(newValue)}' has been specified multiple times. Using the previously specified value {currentValue}.");
        }
        else
        {
            setter(newValue);
        }
    }

    internal static void AssignBoolValue(IWritableHost host, PSDriveInfo drive, string identityName, bool? currentValue, string? newValue, Action<bool?> setter)
    {
        if (string.IsNullOrEmpty(newValue)) return;
        if (!bool.TryParse(newValue, out var boolValue)) return;
        if (currentValue != boolValue)
        {
            host.WriteWarning($"'{drive.Name}:{Path.DirectorySeparatorChar}{identityName}': '{nameof(newValue)}' has been specified multiple times. Using the previously specified value {currentValue}.");
        }
        else
        {
            setter(boolValue);
        }
    }
}
