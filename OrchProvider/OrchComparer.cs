using System.Management.Automation;
using System.Text.RegularExpressions;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Core;

internal class PathInfoComparer : IEqualityComparer<PathInfo>
{
    bool IEqualityComparer<PathInfo>.Equals(PathInfo? x, PathInfo? y)
    {
        return StringComparer.OrdinalIgnoreCase.Equals(x!.Path, y!.Path);
    }

    int IEqualityComparer<PathInfo>.GetHashCode(PathInfo obj)
    {
        return obj.GetHashCode();
    }
}

internal class FolderComparer : IComparer<Folder?>
{
    public int Compare(Folder? x, Folder? y)
    {
        if (x is null && y is null) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        // OrchDirectory に対して比較
        int orchDirectoryComparison = string.Compare(x.Path, y.Path, StringComparison.OrdinalIgnoreCase);
        if (orchDirectoryComparison != 0) return orchDirectoryComparison;

        // OrchDirectory が等しい場合、DisplayName に対して比較
        return string.Compare(x.DisplayName, y.DisplayName, StringComparison.OrdinalIgnoreCase);
    }
}

// usage: new EntityComparer<Package, string>(p => p.Id)
public class EntityComparer<T, TKey> : IComparer<T> where TKey : IComparable<TKey>
{
    private readonly Func<T, TKey> _keySelector;

    public EntityComparer(Func<T, TKey> keySelector)
    {
        _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
    }

    public int Compare(T? x, T? y)
    {
        if (x is null && y is null) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        return _keySelector(x).CompareTo(_keySelector(y));
    }
}

// usage: new EntityEqualityComparer<Package, string>(p => p.Id)
public class EntityEqualityComparer<T, TKey> : EqualityComparer<T>
{
    private readonly Func<T, TKey> _keySelector;

    public EntityEqualityComparer(Func<T, TKey> keySelector)
    {
        _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
    }

    public override bool Equals(T? x, T? y)
    {
        if (x is null && y is null) return true;
        if (x is null || y is null) return false;

        return EqualityComparer<TKey>.Default.Equals(_keySelector(x), _keySelector(y));
    }

    public override int GetHashCode(T obj)
    {
        return EqualityComparer<TKey>.Default.GetHashCode(_keySelector(obj)!);
    }
}

// 3要素の tuple を比較する Comparer
// 最後の要素は必ず string で、case を無視して比較する
internal class ThirdItemIgnoreCaseComparer<T1, T2> : IEqualityComparer<(T1 Item1, T2 Item2, string UserName)>
{
    public bool Equals((T1 Item1, T2 Item2, string UserName) x,
                       (T1 Item1, T2 Item2, string UserName) y)
    {
        return EqualityComparer<T1>.Default.Equals(x.Item1, y.Item1) &&
               EqualityComparer<T2>.Default.Equals(x.Item2, y.Item2) &&
               string.Equals(x.UserName, y.UserName, StringComparison.OrdinalIgnoreCase);
    }

    public int GetHashCode((T1 Item1, T2 Item2, string UserName) obj)
    {
        return HashCode.Combine(
            EqualityComparer<T1>.Default.GetHashCode(obj.Item1 ?? default!),
            EqualityComparer<T2>.Default.GetHashCode(obj.Item2 ?? default!),
            StringComparer.OrdinalIgnoreCase.GetHashCode(obj.UserName)
        );
    }
}

public partial class VersionComparer : IComparer<string>
{
    public static readonly VersionComparer Instance = new();
    private VersionComparer() { }

    public int Compare(string? x, string? y)
    {
        if (x is null && y is null) return 0;
        if (x is null && y is not null) return 1;
        if (x is not null && y is null) return -1;

        var xParts = ParseVersion(x!);
        var yParts = ParseVersion(y!);

        int majorComparison = xParts.major.CompareTo(yParts.major);
        if (majorComparison != 0) return majorComparison;

        int minorComparison = xParts.minor.CompareTo(yParts.minor);
        if (minorComparison != 0) return minorComparison;

        int patchComparison = xParts.patch.CompareTo(yParts.patch);
        if (patchComparison != 0) return patchComparison;

        // Stageの有無に基づく比較
        if (xParts.stage is null && yParts.stage is not null) return 1;
        if (xParts.stage is not null && yParts.stage is null) return -1;

        // Compare stages
        int stageComparison = string.Compare(xParts.stage, yParts.stage, StringComparison.Ordinal);
        if (stageComparison != 0) return stageComparison;

        // Compare num after stage
        int numComparison = xParts.num.CompareTo(yParts.num);
        if (numComparison != 0) return numComparison;

        return 0;
    }

    private static (int major, int minor, int patch, int num, string? stage) ParseVersion(string version)
    {
        var match = PackageVersionRegex().Match(version);
        if (!match.Success) return (0, 0, 0, 0, null);

        var major = int.Parse(match.Groups[1].Value);
        var minor = int.Parse(match.Groups[2].Value);
        var patch = int.Parse(match.Groups[3].Value);
        var num = match.Groups[4].Success ? int.Parse(match.Groups[4].Value) : match.Groups[6].Success ? int.Parse(match.Groups[6].Value) : 0;
        var stage = match.Groups[5].Success ? match.Groups[5].Value : null;

        return (major, minor, patch, num, stage);
    }

    [GeneratedRegex(@"^(\d+)\.(\d+)\.(\d+)(?:\.(\d+)|-(\w+)(?:\.(\d+))?)?$")]
    private static partial Regex PackageVersionRegex();
}

public class ObjKeyComparer : IComparer<string>
{
    public static readonly ObjKeyComparer Instance = new();
    private ObjKeyComparer() { }

    public int Compare(string? x, string? y)
    {
        if (x is null && y is null) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        // Extract the numeric parts from the strings
        int xValue = ExtractNumber(x);
        int yValue = ExtractNumber(y);

        // Compare the numeric values
        return xValue.CompareTo(yValue);
    }

    private int ExtractNumber(string str)
    {
        // Find the position of the colon
        int colonIndex = str.IndexOf(':');
        if (colonIndex == -1)
        {
            return 0; // Return 0 if there is no colon
        }

        // Extract the numeric part after the colon
        string numberPart = str.Substring(colonIndex + 1);
        if (int.TryParse(numberPart, out int result))
        {
            return result;
        }
        else
        {
            return 0; // Return 0 if the number part is not a valid number
        }
    }
}

// List<string> を辞書のキーとして使えるようにするための comparer
// キーは辞書順にソートしておかなければいけない
public class ListStringComparer : IEqualityComparer<List<string>>
{
    public bool Equals(List<string>? x, List<string>? y)
    {
        if (x is null || y is null)
        {
            return x == y;
        }
        return x.SequenceEqual(y);
    }

    public int GetHashCode(List<string> obj)
    {
        if (obj is null || obj.Count == 0)
        {
            return 0;
        }

        // リスト内のすべての要素を個別に組み合わせてハッシュコードを生成
        int hash = 17;
        foreach (var item in obj)
        {
            hash = HashCode.Combine(hash, item?.GetHashCode() ?? 0);
        }
        return hash;
    }
}

// todo: 次の形式を正しくソートできるようにしたい。
// 1.0.8
// 1.0.8-alpha.1
// 1.0.8-alpha  
// 1.0.7
// 1.0.6
//public class PackageVersion : IComparable<PackageVersion>
//{
//    public string Version { get; set; }
//    public int Major { get; private set; }
//    public int Minor { get; private set; }
//    public int Patch { get; private set; }
//    public string Stage { get; private set; }

//    public override string ToString() { return Version; }

//    public PackageVersion(string version)
//    {
//        Version = version;

//        string[] parts = version.Split('-');
//        string[] mainParts = parts[0].Split('.');

//        int major = 0;
//        int minor = 0;
//        int patch = 0;

//        if (mainParts.Length >= 1) int.TryParse(mainParts[0], out major);
//        if (mainParts.Length >= 2) int.TryParse(mainParts[1], out minor);
//        if (mainParts.Length >= 3) int.TryParse(mainParts[2], out patch);


//        Major = major;
//        Minor = minor;
//        Patch = patch;

//        Stage = parts.Length > 1 ? parts[1] : string.Empty;
//    }

//    public int CompareTo(PackageVersion? other)
//    {
//        int majorComparison = Major.CompareTo(other.Major);
//        if (majorComparison != 0) return majorComparison;

//        int minorComparison = Minor.CompareTo(other.Minor);
//        if (minorComparison != 0) return minorComparison;

//        int patchComparison = Patch.CompareTo(other.Patch);
//        if (patchComparison != 0) return patchComparison;

//        if (Stage is null && other.Stage is null) return 0;
//        if (Stage is null) return 1;
//        if (other.Stage is null) return -1;

//        return Stage.CompareTo(other.Stage);
//    }
//}
