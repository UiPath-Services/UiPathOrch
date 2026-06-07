using System.Management.Automation;
using System.Text.RegularExpressions;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Core;

internal class PathInfoComparer : IEqualityComparer<PathInfo>
{
    bool IEqualityComparer<PathInfo>.Equals(PathInfo? x, PathInfo? y)
    {
        if (x is null || y is null) return ReferenceEquals(x, y);
        return StringComparer.OrdinalIgnoreCase.Equals(x.Path, y.Path);
    }

    int IEqualityComparer<PathInfo>.GetHashCode(PathInfo obj)
    {
        // Must agree with Equals (Path, OrdinalIgnoreCase) so HashSet/Distinct dedup
        // correctly; the default reference hash here silently broke that contract.
        return obj.Path is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Path);
    }
}

internal class FolderComparer : IComparer<Folder?>
{
    public int Compare(Folder? x, Folder? y)
    {
        if (x is null && y is null) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        // Compare by OrchDirectory
        int orchDirectoryComparison = string.Compare(x.FullName, y.FullName, StringComparison.OrdinalIgnoreCase);
        if (orchDirectoryComparison != 0) return orchDirectoryComparison;

        // If OrchDirectory is equal, compare by DisplayName
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

// Comparer for 3-element tuples.
// The 2nd and 3rd elements are always strings and are compared case-insensitively.
internal class SecondAndThirdItemIgnoreCaseComparer<T1, T2> : IEqualityComparer<((T1 drive, T2 folder), string Item2, string Item3)>
{
    public bool Equals(
        ((T1 drive, T2 folder), string Item2, string Item3) x,
        ((T1 drive, T2 folder), string Item2, string Item3) y)
    {
        return EqualityComparer<T1>.Default.Equals(x.Item1.drive, y.Item1.drive) &&
               EqualityComparer<T2>.Default.Equals(x.Item1.folder, y.Item1.folder) &&
               string.Equals(x.Item2, y.Item2, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(x.Item3, y.Item3, StringComparison.OrdinalIgnoreCase);
    }

    public int GetHashCode(((T1 drive, T2 folder), string Item2, string Item3) obj)
    {
        int hashDrive = obj.Item1.drive != null ? EqualityComparer<T1>.Default.GetHashCode(obj.Item1.drive) : 0;
        int hashFolder = obj.Item1.folder != null ? EqualityComparer<T2>.Default.GetHashCode(obj.Item1.folder) : 0;
        int hashItem2 = obj.Item2 != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Item2) : 0;
        int hashItem3 = obj.Item3 != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Item3) : 0;
        return HashCode.Combine(hashDrive, hashFolder, hashItem2, hashItem3);
    }
}

// Comparer for 4-element tuples.
// The last element is always a string and is compared case-insensitively.
internal class ForthItemIgnoreCaseComparer<T1, T2, T3> : IEqualityComparer<(T1 Item1, T2 Item2, T3 Item3, string UserName)>
{
    public bool Equals((T1 Item1, T2 Item2, T3 Item3, string UserName) x,
                       (T1 Item1, T2 Item2, T3 Item3, string UserName) y)
    {
        return EqualityComparer<T1>.Default.Equals(x.Item1, y.Item1) &&
               EqualityComparer<T2>.Default.Equals(x.Item2, y.Item2) &&
               EqualityComparer<T3>.Default.Equals(x.Item3, y.Item3) &&
               string.Equals(x.UserName, y.UserName, StringComparison.OrdinalIgnoreCase);
    }

    public int GetHashCode((T1 Item1, T2 Item2, T3 Item3, string UserName) obj)
    {
        return HashCode.Combine(
            EqualityComparer<T1>.Default.GetHashCode(obj.Item1 ?? default!),
            EqualityComparer<T2>.Default.GetHashCode(obj.Item2 ?? default!),
            EqualityComparer<T3>.Default.GetHashCode(obj.Item3 ?? default!),
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

        // Compare based on presence/absence of Stage
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

// Property-level diff engine shared by the Compare-Orch* family. Each cmdlet supplies a
// comparator list — (display name, value extractor) pairs over a curated, migration-relevant
// subset of the entity's properties. Volatile tenant-local fields (Id, Key, timestamps,
// CreatorUserId, ...) are deliberately NOT in those lists: they always differ across tenants
// and would drown the real diff. This is what makes Compare-Orch* useful versus a bare
// Get-Orch* | Compare-Object, which would flag every entity as different on Id alone.
public static class EntityComparison
{
    public const string ReferenceOnly = "<=";
    public const string DifferenceOnly = "=>";
    public const string Different = "<>";
    public const string Equal = "==";

    // Returns the comparators whose extracted values differ between reference and difference.
    // When "only" is non-empty, restricts the comparison to those property names (the
    // -Property parameter, mirroring Compare-Object -Property), case-insensitively.
    public static List<PropertyDifference> DiffProperties<T>(
        T reference,
        T difference,
        IReadOnlyList<(string Name, Func<T, object?> Get)> comparators,
        IReadOnlyCollection<string>? only)
    {
        var diffs = new List<PropertyDifference>();
        foreach (var (name, get) in comparators)
        {
            if (only is { Count: > 0 } && !only.Contains(name, StringComparer.OrdinalIgnoreCase))
                continue;

            var rv = get(reference);
            var dv = get(difference);
            if (!ValueEquals(rv, dv))
            {
                diffs.Add(new PropertyDifference
                {
                    Property = name,
                    ReferenceValue = rv,
                    DifferenceValue = dv,
                });
            }
        }
        return diffs;
    }

    // Order-independent normalized form of an entity's Tags, shared by the Compare-Orch*
    // family so tag set equality (not list order) drives the comparison.
    public static string? NormalizeTags(Tag[]? tags)
        => tags is null || tags.Length == 0
            ? null
            : string.Join(";", tags.Select(t => $"{t.Name}={t.Value}").OrderBy(s => s, StringComparer.Ordinal));

    // Equality used for a single compared property. null and "" are treated as equal for
    // string-valued extractors (an absent Description vs an empty one is not a real drift);
    // non-string values fall back to object Equals (so bool? false vs null IS a difference).
    public static bool ValueEquals(object? a, object? b)
    {
        if (a is null && b is null) return true;
        if ((a is null || a is string) && (b is null || b is string))
            return string.Equals((string?)a ?? "", (string?)b ?? "", StringComparison.Ordinal);
        if (a is null || b is null) return false;
        return a.Equals(b);
    }
}

// Comparer that allows List<string> to be used as a dictionary key.
// Keys must be sorted in lexicographic order.
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

        // Generate a hash code by combining all elements in the list individually
        int hash = 17;
        foreach (var item in obj)
        {
            hash = HashCode.Combine(hash, item?.GetHashCode() ?? 0);
        }
        return hash;
    }
}

