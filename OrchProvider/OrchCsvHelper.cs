using System.Collections;
using System.Management.Automation;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands.CsvHelper;

internal class TupleComparer<T> : IEqualityComparer<T>
{
    public bool Equals(T? x, T? y)
    {
        if (x is null || y is null) return false;

        // Tuple の全要素を比較する
        return StructuralComparisons.StructuralEqualityComparer.Equals(x, y);
    }

    public int GetHashCode(T obj)
    {
        if (obj is null) return 0;

        // Tuple の全要素を考慮したハッシュコードを生成
        return StructuralComparisons.StructuralEqualityComparer.GetHashCode(obj);
    }
}

internal class CsvLineBase
{
    // Expression<T> を使えば、もうすこし簡潔に書けそうだけど、そこまでするのもなあ。。
    // OrchDriveInfo と OrchDuDriveInfo のベースを作って、NameColonSeparator はそっちに移動すべきだ。
    // で、PSDriveInfo よりも OrchDriveInfoBase を使う方が良い。
    // このままでも動くけど、少し string の構築が無駄かな。。
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
