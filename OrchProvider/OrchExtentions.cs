using Microsoft.ApplicationInsights.DataContracts;
using System.Collections.ObjectModel;
using System.Data;
using System.Management.Automation;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;
using Path = System.IO.Path;
using SessionState = System.Management.Automation.SessionState;

namespace UiPath.PowerShell.Core;

internal static class FolderExtensions
{
    // returns orchPath
    public static string GetPackageFeedFolder(this Folder folder)
    {
        if (folder is null || folder.FeedType != "FolderHierarchy")
            return "";

        return OrchDriveInfo.GetTopParentPath(folder.FullyQualifiedName!);
    }

    /// <summary>
    /// srcRootFolder からの相対パスを返す
    /// </summary>
    public static string GetRelativePath(this Folder srcFolder, Folder srcRootFolder)
    {
        if (srcFolder?.FullyQualifiedName == null || srcRootFolder?.FullyQualifiedName == null)
            throw new ArgumentNullException("srcFolder or srcRootFolder has null FullyQualifiedName.");

        string relativePath = srcFolder.FullyQualifiedName[srcRootFolder.FullyQualifiedName.Length..];
        return relativePath.TrimStart('/').TrimEnd('/');
    }

    public static string GetPSPath(this Folder entity) => Path.Combine(entity?.Path ?? "", entity?.DisplayName ?? "");

    public static string GetPSPath(this Entities.Environment entity)   => Path.Combine(entity?.Path ?? "", entity?.Name ?? "");
    public static string GetPSPath(this Library entity)                => Path.Combine(entity?.Path ?? "", entity?.Id ?? "");
    public static string GetPSPath(this LibraryVersion entity)         => Path.Combine(entity?.Path ?? "", entity?.Id ?? "");
    public static string GetPSPath(this Package entity)                => Path.Combine(entity?.Path ?? "", entity?.Id ?? "");
    public static string GetPSPath(this User entity)                   => Path.Combine(entity?.Path ?? "", entity?.UserName ?? "");
    public static string GetPSPath(this UserRoles entity)              => Path.Combine(entity?.Path ?? "", entity?.UserEntity?.UserName ?? "");
    public static string GetPSPath(this Role entity)                   => Path.Combine(entity?.Path ?? "", entity?.Name ?? "");
    public static string GetPSPath(this Webhook entity)                => Path.Combine(entity?.Path ?? "", entity?.Name ?? "");
    public static string GetPSPath(this Robot entity)                  => Path.Combine(entity?.Path ?? "", entity?.User?.Name ?? "");
    public static string GetPSPath(this ExtendedRobot entity)          => Path.Combine(entity?.Path ?? "", entity?.Name ?? "");
    public static string GetPSPath(this RobotsFromFolderModel entity)  => Path.Combine(entity?.Path ?? "", entity?.Name ?? "");
    public static string GetPSPath(this ExtendedCalendar entity)       => Path.Combine(entity?.Path ?? "", entity?.Name ?? "");
    public static string GetPSPath(this MachineFolder entity)          => Path.Combine(entity?.Path ?? "", entity?.Name ?? "");
    public static string GetPSPath(this ExtendedMachine entity)        => Path.Combine(entity?.Path ?? "", entity?.Name ?? "");
    public static string GetPSPath(this CredentialStore entity)        => Path.Combine(entity?.Path ?? "", entity?.Name ?? "");
    public static string GetPSPath(this QueueDefinition entity)        => Path.Combine(entity?.Path ?? "", entity?.Name ?? "");
    public static string GetPSPath(this QueueItem entity)              => Path.Combine(entity?.PathName ?? "", entity?.Id.ToString() ?? "");
    public static string GetPSPath(this Asset entity)                  => Path.Combine(entity?.Path ?? "", WildcardPattern.Escape(entity?.Name ?? ""));
    public static string GetPSPath(this Release entity)                => Path.Combine(entity?.Path ?? "", entity?.Name ?? "");
    public static string GetPSPath(this PersonalWorkspace entity)      => Path.Combine(entity?.Path ?? "", entity?.Name ?? "");
    public static string GetPSPath(this ProcessSchedule entity)        => Path.Combine(entity?.Path ?? "", entity?.Name ?? "");
    public static string GetPSPath(this HttpTrigger entity)            => Path.Combine(entity?.Path ?? "", entity?.Name ?? "");
    public static string GetPSPath(this Session entity)                => Path.Combine(entity?.Path ?? "", entity?.Robot?.Name ?? "");
    public static string GetPSPath(this TestSet entity)                => Path.Combine(entity?.Path ?? "", entity?.Name ?? "");
    public static string GetPSPath(this TestCaseDefinition entity)     => Path.Combine(entity?.Path ?? "", entity?.Name ?? "");
    public static string GetPSPath(this TestSetExecution entity)       => Path.Combine(entity?.Path ?? "", entity?.Name ?? "");
    public static string GetPSPath(this TestSetSchedule entity)        => Path.Combine(entity?.Path ?? "", entity?.Name ?? "");
    public static string GetPSPath(this TestDataQueue entity)          => Path.Combine(entity?.Path ?? "", entity?.Name ?? "");
    public static string GetPSPath(this TaskCatalog entity)            => Path.Combine(entity?.Path ?? "", entity?.Name ?? "");
    public static string GetPSPath(this Settings entity)               => Path.Combine(entity?.Path ?? "", entity?.Name ?? "");
    public static string GetPSPath(this Bucket entity)                 => Path.Combine(entity?.Path ?? "", entity?.Name ?? "");
    public static string GetPSPath(this BlobFile entity)               => Path.Combine(entity?.PathBucket ?? "", entity?.FullPath ?? "");
    public static string GetPSPath(this ResponseDictionaryItem entity) => Path.Combine(entity?.Path ?? "", entity?.Key ?? "");
    public static string GetPSPath(this AuditLog entity)               => Path.Combine(entity?.Path ?? "", entity?.Id.ToString() ?? "");

    public static string GetPSPath(this PmUser entity)                => Path.Combine(entity?.Path ?? "", entity?.userName ?? "");
    public static string GetPSPath(this PmRobotAccount entity)        => Path.Combine(entity?.Path ?? "", entity?.name ?? "");
    public static string GetPSPath(this PmGroup entity)               => Path.Combine(entity?.Path ?? "", entity?.name ?? "");
    public static string GetPSPath(this PmGroupMember entity)         => Path.Combine(entity?.Path ?? "", entity?.groupName ?? "", entity?.name ?? "");
    public static string GetPSPath(this PmDirectoryEntityInfo entity) => Path.Combine(entity?.Path ?? "", entity?.identityName ?? "");
    public static string GetPSPath(this ExternalResource entity)      => Path.Combine(entity?.Path ?? "", entity?.name ?? "");
    public static string GetPSPath(this ExternalClient entity)        => Path.Combine(entity?.Path ?? "", entity?.name ?? "");
    public static string GetPSPath(this NuLicensedGroup entity)       => Path.Combine(entity?.Path ?? "", entity?.name ?? "");
    public static string GetPSPath(this NuLicensedGroupMember entity) => Path.Combine(entity?.Path ?? "", entity?.name ?? "");

    public static string GetPSPath(this DirectoryUser entity)        => Path.Combine(entity?.Path ?? "", entity?.name ?? "");
    public static string GetPSPath(this DirectoryRobotUser entity)   => Path.Combine(entity?.Path ?? "", entity?.name ?? "");
    public static string GetPSPath(this DirectoryApplication entity) => Path.Combine(entity?.Path ?? "", entity?.name ?? "");

    public static string GetPSPath(this DuProject entity)      => Path.Combine(entity?.Path ?? "", entity?.name ?? "");
    public static string GetPSPath(this DuRole entity)         => Path.Combine(entity?.Path ?? "", entity?.name ?? "");
    public static string GetPSPath(this DuUser entity)         => Path.Combine(entity?.Path ?? "", entity?.displayName ?? "");
    public static string GetPSPath(this DuDocumentType entity) => Path.Combine(entity?.Path ?? "", entity?.name ?? "");
    public static string GetPSPath(this DuClassifier entity)   => Path.Combine(entity?.Path ?? "", entity?.name ?? "");

    public static string GetPSPath(this TmProject entity)     => Path.Combine(entity?.Path ?? "", entity?.projectPrefix ?? "");
    public static string GetPSPath(this TmRequirement entity) => Path.Combine(entity?.Path ?? "", entity?.name ?? "");
    public static string GetPSPath(this TmTestCase entity)    => Path.Combine(entity?.Path ?? "", entity?.name ?? "");
    public static string GetPSPath(this TmTestSet entity)     => Path.Combine(entity?.Path ?? "", entity?.name ?? "");

    public static string TipHelp(this PmGroupMember? entity)         => $"{entity?.name}{(string.IsNullOrEmpty(entity?.displayName) ? "" : $" ({entity.displayName})")}";
    public static string TipHelp(this PmDirectoryEntityInfo? entity) => $"{entity?.GetPSPath()}{(string.IsNullOrEmpty(entity?.displayName) ? "" : $" ({entity.displayName})")}";
    public static string TipHelp(this ExtendedRobot entity)          => $"{entity?.GetPSPath()}{(string.IsNullOrEmpty(entity?.Username) ? "" : $" ({entity.Username})")}";
    public static string TipHelp(this Library? entity)               => $"{entity?.GetPSPath()}";
}

internal static class OrchCollectionExtensions
{
    // 次のメソッドは、T& を比較するときにパフォーマンスが == よりも悪くなる。null もチェックしてないな。。
    // これを使うのはやめて、== で比較すべきだ。
    // == と SafeEquals() と SafeSequenceEquals() のどれを使うべきか、間違えないようにしないと。
    //public static bool SafeEquals<T>(this T? obj1, T obj2) where T : struct
    //{
    //    return obj1.Equals(obj2);
    //}

    // 参照型の値を比較するときには、これが便利だ。
    public static bool SafeEquals<T>(this T? obj1, T? obj2) where T : class
    {
        return ReferenceEquals(obj1, obj2) || (obj1 is not null && obj1.Equals(obj2));
    }

    // 配列とかリストを比べるときは、これだ。
    public static bool SafeSequenceEquals<T>(this IEnumerable<T>? collection1, IEnumerable<T>? collection2)
    {
        if (ReferenceEquals(collection1, collection2)) return true;
        if (collection1 is null && collection2 is null) return true;
        if (collection1 is null || collection2 is null) return false;
        return collection1.SequenceEqual(collection2);
    }

    public static void AddIfNotNull<T>(this List<T> list, T? item) where T : class
    {
        if (item is not null)
        {
            list.Add(item);
        }
    }

    public static string? CreateOrFilter<T>(this IEnumerable<T> param, Func<T, string> converter)
    {
        if (param is null || !param.Any()) return null;
        string filter = $"({string.Join(" or ", param.Select(converter))})";
        return string.IsNullOrEmpty(filter) ? null : filter;
    }

    public static string? CreateAndFilter<T>(this IEnumerable<T> param, Func<T, string> converter)
    {
        if (param is null || !param.Any()) return null;
        string filter = $"({string.Join(" and ", param.Select(converter))})";
        return string.IsNullOrEmpty(filter) ? null : filter;
    }

    public static string RemoveEnd(this string input, string? endToRemove)
    {
        if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(endToRemove))
            return input;

        if (input.EndsWith(endToRemove))
            return input.Substring(0, input.Length - endToRemove.Length);

        return input;
    }

    // これあった方がいいかな。。
    //public static WildcardPattern ConvertToWildcardPattern(this string input)
    //{
    //    return new WildcardPattern(input, WildcardOptions.IgnoreCase);
    //}

    // WildcardPattern の列挙は、なんども繰り返して使用することになるので、ここで List にして返しておく方が良い。
    public static List<WildcardPattern>? ConvertToWildcardPatternList(this IEnumerable<string?>? input)
    {
        // PathTools.UnescapePSText(n) はしなくても良いのだっけ？
        return input?.Select(n => new WildcardPattern(n, WildcardOptions.IgnoreCase)).ToList();
    }

    public static IEnumerable<T> FilterByClassValues<T, TKey>(
        this IEnumerable<T> source,
        Func<T, TKey?> selector,
        IEnumerable<TKey>? values) where TKey : class
    {
        if (values is null || !values.Any()) return source;
        return source.Where(item => values.Contains(selector(item)));
    }

    public static IEnumerable<T> FilterByStructValues<T, TKey>(
        this IEnumerable<T> source,
        Func<T, TKey> selector,
        IEnumerable<TKey>? values) where TKey : struct
    {
        if (values is null || !values.Any()) return source;
        return source.Where(item => values.Contains(selector(item)));
    }

    // patterns が空であれば、source のすべての要素をそのまま返す
    public static IEnumerable<T> FilterByWildcards<T>(
        this IEnumerable<T> source,
        Func<T?, string?> selector,
        List<WildcardPattern>? patterns)
    {
        if (patterns is null || patterns.Count == 0) return source;
        return source.Where(item => patterns.Any(pattern => pattern.IsMatch(selector(item))));
    }

    // patterns が空であれば、source のすべての要素をそのまま返す
    public static IEnumerable<T> FilterByWildcards<T>(
        this IEnumerable<T> source,
        Func<T?, string?> selector,
        string[]? patterns)
    {
        if (patterns is null || patterns.Length == 0) return source;
        var wpPatterns = patterns.ConvertToWildcardPatternList();
        return source.FilterByWildcards(selector, wpPatterns);
    }

    // patterns が空であれば、空を返す
    public static IEnumerable<T> SelectByWildcards<T>(
        this IEnumerable<T> source,
        Func<T?, string?> selector,
        List<WildcardPattern>? patterns)
    {
        if (patterns is null || patterns.Count == 0) return [];
        return source.Where(item => patterns.Any(pattern => pattern.IsMatch(selector(item))));
    }

    // patterns が空であれば、空を返す
    public static IEnumerable<T> SelectByWildcards<T>(
        this IEnumerable<T> source,
        Func<T?, string?> selector,
        string[]? patterns)
    {
        if (patterns is null || patterns.Length == 0) return [];
        var wpPatterns = patterns.ConvertToWildcardPatternList();
        return source.SelectByWildcards(selector, wpPatterns);
    }

    public static IEnumerable<T> ExcludeByClassValues<T, TKey>(
        this IEnumerable<T> source,
        Func<T?, TKey?> selector,
        IEnumerable<TKey?>? values) where TKey : class
    {
        if (values is null || !values.Any()) return source;
        return source.Where(item => !values.Contains(selector(item)));
    }

    public static IEnumerable<T> ExcludeByStructValues<T, TKey>(
        this IEnumerable<T> source,
        Func<T, TKey> selector,
        IEnumerable<TKey>? values) where TKey : struct
    {
        if (values is null || !values.Any()) return source;
        return source.Where(item => !values.Contains(selector(item)));

        //return source
        //    .Where(item => item is not null)
        //    .Select(item => item!)
        //    .Where(item => !values.Contains(selector(item!)));
    }

    public static IEnumerable<T> ExcludeByWildcards<T>(
        this IEnumerable<T> source,
        Func<T?, string?> selector,
        List<WildcardPattern>? patterns)
    {
        if (patterns is null || patterns.Count == 0) return source;
        return source.Where(item => !patterns.Any(pattern => pattern.IsMatch(selector(item))));
    }

    //public static bool TryAddIfNotNullOrEmpty(this Dictionary<string, object> dic, string key, string? value)
    //{
    //    if (string.IsNullOrEmpty(value)) return false;
    //    return dic.TryAdd(key, value);
    //}

    #region
    private static string UnescapeBackticks(string input)
    {
        // バッククォートのエスケープを解除
        return input.Replace("``", "`").Replace("`", "");
    }

    public static IEnumerable<string> SplitByUnescapedCommas(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return Enumerable.Empty<string>();
        }

        // 正規表現パターン
        var pattern = @"((?:[^`]|``|`.)+?)(?:,|$)";
        var matches = Regex.Matches(input, pattern);

        var result = matches.Cast<Match>()
            .Select(m => UnescapeBackticks(m.Groups[1].Value.Trim()))
            .Where(s => !string.IsNullOrWhiteSpace(s));

        return result;
    }

    // CSV から入力されている可能性があるので、先頭の要素をカンマで区切る。残りの要素はそのまま使用する。
    // 次のような処理なんだけど、エスケープされたカンマでは区切らないように工夫しておく
    // sourceArray = Version[0].Split(',').Concat(Version.Skip(1)).ToArray();
    internal static IEnumerable<string>? Split1stValueByUnescapedCommas(this IEnumerable<string>? source)
    {
        if (source is null) return null;
        return SplitByUnescapedCommas(source.FirstOrDefault()).Concat(source.Skip(1));
    }
    #endregion

    public static T DeepCopy<T>(T obj)
    {
        var jsonString = JsonSerializer.Serialize(obj);
        return JsonSerializer.Deserialize<T>(jsonString)!;
    }

    public static TDst DeepCopyAsSubClass<TSrc, TDst>(TSrc obj)
    {
        var jsonString = JsonSerializer.Serialize(obj);
        return JsonSerializer.Deserialize<TDst>(jsonString)!;
    }
}

//internal static class OrchObjectExtensions
//{
//    public static bool AreAllPropertiesNull<T>(this T obj)
//    {
//        if (obj is null)
//            return true;

//        // Get all properties of the object
//        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

//        // Check if all properties are null
//        return properties.All(property => property.GetValue(obj) is null);
//    }
//}

internal static class OrchStringExtensions
{
    internal static string MakeValidFolderName(this string originalString)
    {
        string invalidChars = new string(Path.GetInvalidFileNameChars())
                            + new string(Path.GetInvalidPathChars());

        // 無効な文字を '_' に置換
        string validString = new(originalString
          .Select(ch => invalidChars.Contains(ch) ? '_' : ch)
          .ToArray());

        return validString;
    }

    internal static string MakeValidFileName(this string originalString)
    {
        string invalidChars = new string(Path.GetInvalidFileNameChars())
                            + new string(Path.GetInvalidPathChars());

        string validString = new(originalString
          .Select(ch => invalidChars.Contains(ch) ? '_' : ch)
          .ToArray());

        // 末尾の . やスペースは削除
        validString = validString.TrimEnd('.', ' ');

        // Windows の予約語チェック
        string[] reservedNames = {
            "CON","PRN","AUX","NUL",
            "COM1","COM2","COM3","COM4","COM5","COM6","COM7","COM8","COM9",
            "LPT1","LPT2","LPT3","LPT4","LPT5","LPT6","LPT7","LPT8","LPT9"
        };
        if (reservedNames.Contains(validString.ToUpperInvariant()))
        {
            validString = "_" + validString;
        }

        // 長さ制限 (255)
        if (validString.Length > 255)
            validString = validString.Substring(0, 255);

        return validString;
    }

    public delegate bool TryParseHandler<T>(string str, out T result);

    public static T? ToNullable<T>(this string? str, TryParseHandler<T> tryParse) where T : struct
    {
        if (str is not null && tryParse(str, out var result))
        {
            return result;
        }
        return null;
    }

    public static bool? ToNullableBool(this string? str)
    {
        return str.ToNullable<bool>(bool.TryParse);
    }

    public static DateTime? ToNullableDateTime(this string? str)
    {
        return str.ToNullable<DateTime>(DateTime.TryParse);
    }

    public static T? ToNullable<T>(this T? value) where T : struct, IComparable
    {
        if (value.HasValue && value.Value.CompareTo(default(T)) == 0)
        {
            return null;
        }
        return value;
    }

    public static WildcardPattern? ConvertToWildcardPattern(this string? src)
    {
        if (string.IsNullOrEmpty(src)) return null;
        return new WildcardPattern(src, WildcardOptions.IgnoreCase);
    }

    // Method for string properties
    public static void AssignStringIfNotNull<T>(this T target, string? value, Action<T, string?> setter)
    {
        if (value is not null)
        {
            setter(target, value);
        }
    }

    public static void AssignStringIfNotNullOrEmpty<T>(this T target, string? value, Action<T, string?> setter)
    {
        if (!string.IsNullOrEmpty(value))
        {
            setter(target, value);
        }
    }

    // Generic method for nullable numeric types
    // ゼロは設定しない。CSV の空列を int? のパラメータで受け取ると、ゼロになってしまうため。
    // このメソッドを使えば、CSV で int のパラメータに空欄を指定してもゼロで既存データを上書きすることはない。
    // TODO: いや、CSV でもコマンドラインでも、"" を指定した場合には null を設定すべきではないか？
    // このメソッドは廃止して AssignNumberIfNotNull() で置換した方が良さそうな気がする。
    public static void AssignNumberIfNotNullOrZero<T, N>(this T target, N? value, Action<T, N?> setter) where N : struct, IComparable
    {
        if (value.HasValue && !value.Value.Equals(default(N)))
        {
            setter(target, value);
        }
    }

    // ゼロを受け付けるメンバの場合には、こちらを使う。
    // CSV で空列を指定した場合には、パラメータにゼロが渡されるので注意が必要。
    // 既存の値が不明な場合には、CSV からその列を削除しておく。そうすれば、その列の int 型パラメータには null が渡される。
    // TODO: cmdlet の int パラメータは、すべて string? に修正すべきだ。
    public static void AssignNumberIfNotNull<T, N>(this T target, N? value, Action<T, N?> setter) where N : struct, IComparable
    {
        if (value.HasValue)
        {
            setter(target, value);
        }
    }

    // string 値を int に変換して設定する。"" などの数値でない値を指定した場合には、null を設定する。
    // null を指定した場合には何もしない
    public static void AssignNumberIfNotNull<T>(this T target, string? value, Action<T, int?> setter)
    {
        if (value is not null)
        {
            if (int.TryParse(value, out var result))
            {
                setter(target, result);
            }
            else if (value == "")
            {
                setter(target, null);
            }
        }
    }

    // Method for bool properties
    public static void AssignBoolIfNotNull<T>(this T target, string? value, Action<T, bool?> setter)
    {
        if (value is not null)
        {
            if (bool.TryParse(value, out var result))
            {
                setter(target, result);
            }
            else
            {
                setter(target, null);
            }
        }
    }

    public static void AssignBoolIfNotNull<T>(this T target, bool? value, Action<T, bool?> setter)
    {
        if (value is not null)
        {
            setter(target, value);
        }
    }

    // 現在の値が null であれば、false を代入しないように工夫されたバージョン
    public static void AssignBoolIfNotFalse<T>(this T target, string? value, Func<T, bool?> getter, Action<T, bool?> setter)
    {
        if (value is not null && bool.TryParse(value, out var result))
        {
            // 代入先が null で、かつ代入する値が false の場合には何もしない
            if (getter(target) is null && !result) return;

            setter(target, result);
        }
    }

    public static void AssignDateTimeIfNotNull<T>(this T target, DateTime? value, Action<T, DateTime?> setter, bool convertToUniversalTime = true)
    {
        if (value is not null)
        {
            if (convertToUniversalTime)
            {
                setter(target, value?.ToUniversalTime());
            }
            else
            {
                setter(target, value);
            }
        }
    }

    // nameKind には capitalized の名前を渡すこと。
    // 処理が継続できない場合に false を返す。
    public static bool AssignIdFromName<T, TElement, TId>(
        this T target,
        string? name,
        Func<IEnumerable<TElement>> getEntitiesFunc,
        Func<TElement, string> getNameFunc,
        Func<TElement, TId> getIdFunc,
        Action<T, TId?> setter,
        IWritableHost host,
        string? targetName,
        string nameKind)
    {
        if (name is null) return true;

        if (name == "")
        {
            setter(target, default);
            return true;
        }
        try
        {
            var wpName = new WildcardPattern(name, WildcardOptions.IgnoreCase);
            var entities = getEntitiesFunc().Where(e => wpName.IsMatch(getNameFunc(e)));
            switch (entities.Take(2).Count())
            {
                case 1:
                    setter(target, getIdFunc(entities.First()));
                    return true;
                case 0:
                    host.WriteError(new ErrorRecord(new OrchException(targetName, $"No {nameKind} found with \"{name}\". Please ensure that the specified {nameKind} name is correct."), $"Get{nameKind}Error", ErrorCategory.InvalidOperation, target));
                    return false;
                default:
                    host.WriteError(new ErrorRecord(new OrchException(targetName, $"Multiple {nameKind} found with \"{name}\". Please specify a unique {nameKind} name."), $"Get{nameKind}Error", ErrorCategory.InvalidOperation, target));
                    return false;
            }
        }
        catch (Exception ex)
        {
            host.WriteError(new ErrorRecord(new OrchException(targetName, ex), $"Get{nameKind}Error", ErrorCategory.InvalidOperation, target));
        }
        return false;
    }

    private static string? ReplaceLastPartWithAsterisk(string? input)
    {
        if (input is null) return null;

        // ピリオドで分割
        string[] parts = input.Split('.');

        if (parts.Length >= 3)
        {
            parts[2] = "*";
            return string.Join(".", parts.Take(3));
        }
        return input;
    }

    // 次のふたつのメソッドは、ラムダ式を使って実装を完全に共通にすることもできるが、とても使いにくくなってしまう。
    // ReplaceLastNumberWithAsterisk() の実装を共通にしたことで良しとするか。。
    public static void AssignUpdatePolicy(this User target, string? typeValue, string? versionValue)
    {
        if (!string.IsNullOrEmpty(typeValue) || !string.IsNullOrEmpty(versionValue))
        {
            target.UpdatePolicy ??= new();
            target.UpdatePolicy.AssignStringIfNotNullOrEmpty(typeValue, (u, v) => u.Type = v);
            if (typeValue == "None" || typeValue == "LatestVersion")
            {
                target.UpdatePolicy.SpecificVersion = null;
            }
            else
            {
                if (typeValue == "LatestPatch")
                {
                    versionValue = ReplaceLastPartWithAsterisk(versionValue);
                }
                target.UpdatePolicy.AssignStringIfNotNullOrEmpty(versionValue, (u, v) => u.SpecificVersion = v);
            }
            //postingUser.UpdatePolicy.Type ??= "None";
        }
    }

    public static void AssignUpdatePolicy(this ExtendedMachine target, string? typeValue, string? versionValue)
    {
        if (!string.IsNullOrEmpty(typeValue) || !string.IsNullOrEmpty(versionValue))
        {
            target.UpdatePolicy ??= new();
            target.UpdatePolicy.AssignStringIfNotNullOrEmpty(typeValue, (u, v) => u.Type = v);
            if (typeValue == "None" || typeValue == "LatestVersion")
            {
                target.UpdatePolicy.SpecificVersion = null;
            }
            else
            {
                if (typeValue == "LatestPatch")
                {
                    versionValue = ReplaceLastPartWithAsterisk(versionValue);
                }
                target.UpdatePolicy.AssignStringIfNotNullOrEmpty(versionValue, (u, v) => u.SpecificVersion = v);
            }
        }
    }

    public static void AssignTags<T>(this T target, string[]? value, Action<T, Tag[]?> setter)
    {
        if (value is not null && value.Length != 0)
        {
            Tag[] tags = value.ConvertToTags().ToArray();
            if (tags is not null) setter(target, tags);
        }
    }

    // keyValues は = で区切られた key と value。"tag1" とか "tag2=value" とか。
    // Add-OrchHoge とか、Update-OrchHoge の中で使う。
    public static IEnumerable<Tag> ConvertToTags(this string[]? keyValues)
    {
        foreach (var keyValue in keyValues?.SelectMany(elem => elem.Split(',')) ?? [])
        {
            if (string.IsNullOrEmpty(keyValue)) continue;
            string[] parts = keyValue.Split('=', 2);

            // キーと値を取得
            string key = parts[0];
            string value = parts.Length > 1 ? parts[1] : null;
            Tag tag = new()
            {
                Name = key,
                Value = value
            };
            yield return tag;
        }
    }

    // これは、WriteCsvContent() の中から呼び出す。
    internal static string? ConvertToString(this Tag[]? tags)
    {
        if (tags is null) return null;
        return string.Join(',', tags.Select(t => t.ToString()));
    }

    // obsoleted: ConvertToTags() を使うべし
    //public static Tag[]? DeserializeFromJson(this string? jsonText)
    //{
    //    if (string.IsNullOrWhiteSpace(jsonText))
    //    {
    //        //return Array.Empty<Tag>();
    //        return null;
    //    }

    //    try
    //    {
    //        return JsonSerializer.Deserialize<Tag[]>(jsonText);
    //    }
    //    //catch (JsonException)
    //    catch
    //    {
    //        return null;
    //    }
    //}
    public static StringBuilder AppendLineLf(this StringBuilder sb)
    {
        return sb.Append('\n');
    }

    /// <summary>
    /// LF 改行（\n）を使用して、指定された文字列と改行を追加します。
    /// </summary>
    public static StringBuilder AppendLineLf(this StringBuilder sb, string? value)
    {
        sb.Append(value);
        sb.Append('\n');
        return sb;
    }
}

internal static class OrchDriveFolderExtensions
{
    public static int CountEntities<T>(
        this IEnumerable<(OrchDriveInfo drive, Folder folder)> drivesFolders,
        Func<OrchDriveInfo, ListCachePerFolder<T>> list,
        Func<IEnumerable<T>, IEnumerable<T>>? applyFilter = null)
    {
        return drivesFolders.Sum(df =>
        {
            var items = list(df.drive).Get(df.folder);
            return (applyFilter?.Invoke(items) ?? items).Count();
        });
    }
}

internal static class OrchWriterExtensions
{
    // writer.WriteLine(string.Join(',', values) とすると、内部で string を連結してしまう。
    // 逐次 writer.Write() を呼ぶ方が効率的だ。
    internal static void WriteCsvLine(this TextWriter? writer, IEnumerable<string?> values)
    {
        if (writer is null) return;

        bool first = true;
        foreach (var value in values)
        {
            if (!first)
            {
                writer.Write(','); // 2個目以降はカンマを入れる
            }
            else first = false;
            writer.Write(value);
        }
        writer.WriteLine(); // 最後に改行を追加
    }
}

// PSCmdlet の文脈においては、PSCmdlet.SessionState に対して呼び出すべきだ。
// PSCmdlet.SessionState にアクセスできない文脈においては、OrchDriveInfo.SessionState に対して呼び出すが良い。
// OrchDriveInfo.SessionState は OrchProvider.Start() で設定しているが、この値が不正になる状況があった。
// 通常使っている限りにおいては一度も遭遇したことがないが、PowerShell.MCP から使うときに current location が正しく取れないことがあった。
internal static class SessionStateExtentios
{
    public static IEnumerable<OrchDriveInfo> EnumAllOrchDrives(this SessionState? sessionState)
    {
        OrchDriveInfo.SessionState = sessionState;

        return sessionState!.Drive.GetAllForProvider("UiPathOrch")
            .Cast<OrchDriveInfo>()
            .OrderBy(d => d.Name);
    }

    public static IEnumerable<OrchDuDriveInfo> EnumAllDuDrives(this SessionState? sessionState)
    {
        OrchDriveInfo.SessionState = sessionState;

        return sessionState!.Drive.GetAllForProvider("UiPathOrchDu")
            .Cast<OrchDuDriveInfo>()
            .OrderBy(d => d.Name);
    }

    public static IEnumerable<OrchTmDriveInfo> EnumAllTmDrives(this SessionState? sessionState)
    {
        OrchDriveInfo.SessionState = sessionState;

        return sessionState!.Drive.GetAllForProvider("UiPathOrchTm")
            .Cast<OrchTmDriveInfo>()
            .OrderBy(d => d.Name);
    }

    // paths を指定しない場合、カレントドライブのみを返す
    // T には OrchDriveInfo, OrchDuDriveInfo などを指定できる
    public static List<T> EnumOrchDrivesImpl<T>(this SessionState? sessionState, IEnumerable<string?>? path = null) where T : PSDriveInfo
    {
        OrchDriveInfo.SessionState = sessionState;

        var drives = new List<T>();
        if (path is null || !path.Any() || path.All(p => p is null))
        {
            if (sessionState!.Path.CurrentLocation.Drive is T orchDrive)
                drives.Add(orchDrive);
        }
        else
        {
            var psPaths = path.Select(p => sessionState!.Path.GetResolvedPSPathFromPSPath(p)).SelectMany(p => p);
            foreach (var p in psPaths)
            {
                if (p.Drive is T orchDrive)
                    drives.Add(orchDrive);
            }
        }
        return drives.Distinct().ToList();
    }

    // paths を指定しない場合、カレントドライブのみを返す
    public static List<OrchDriveInfo> EnumOrchDrives(this SessionState? sessionState, IEnumerable<string?>? path = null)
    {
        return sessionState.EnumOrchDrivesImpl<OrchDriveInfo>(path);
    }

    // paths を指定しない場合、カレントドライブのみを返す
    public static List<OrchDuDriveInfo> EnumDuDrives(this SessionState? sessionState, IEnumerable<string?>? path = null)
    {
        return sessionState.EnumOrchDrivesImpl<OrchDuDriveInfo>(path);
    }

    public static List<OrchTmDriveInfo> EnumTmDrives(this SessionState? sessionState, IEnumerable<string?>? path = null)
    {
        return sessionState.EnumOrchDrivesImpl<OrchTmDriveInfo>(path);
    }

    // paths を指定しない場合、カレントドライブのみを返す
    // PmDrive は、任意のドライブから検索できないといけないから実装が別だ。

    public static List<OrchDriveInfo> EnumPmDrives(this SessionState? sessionState, IEnumerable<string?>? path = null)
    {
        OrchDriveInfo.SessionState = sessionState;

        static void AddOrchDrive(SessionState sessionState, HashSet<OrchDriveInfo> drives, PSDriveInfo drive)
        {
            if (drive is OrchDriveInfo orchDrive)
            {
                drives.Add(orchDrive);
            }
            else if (sessionState!.Path.CurrentLocation.Drive is OrchDuDriveInfo orchDuDrive)
            {
                drives.Add(orchDuDrive.ParentDrive);
            }
            else if (sessionState!.Path.CurrentLocation.Drive is OrchTmDriveInfo orchTmDrive)
            {
                drives.Add(orchTmDrive.ParentDrive);
            }
        }

        var drives = new HashSet<OrchDriveInfo>();
        if (path is null || !path.Any() || path.All(p => p is null))
        {
            if (sessionState is not null)
            {
                AddOrchDrive(sessionState, drives, sessionState.Path.CurrentLocation.Drive);
            }
        }
        else
        {
            var psPaths = path.Select(p => sessionState!.Path.GetResolvedPSPathFromPSPath(p)).SelectMany(p => p);
            foreach (var p in psPaths)
            {
                AddOrchDrive(sessionState!, drives, p.Drive);
            }
        }
        return drives.OrderBy(d => d.Name).ToList();
    }


    // EnumOrchDrive と似ているけど、こちらはカレントドライブを考慮しない。
    // Destination を解決するのに使う。
    public static List<OrchDriveInfo> EnumDestinationDrives(this SessionState? sessionState, IEnumerable<string> paths)
    {
        OrchDriveInfo.SessionState = sessionState;

        return paths
            .Select(p => sessionState!.Path.GetResolvedPSPathFromPSPath(p))
            .SelectMany(p => p)
            .Where(p => p.Drive is OrchDriveInfo orchDrive)
            .Select(p => p.Drive)
            .DistinctBy(p => p.Name)
            .Cast<OrchDriveInfo>()
            .ToList();
    }

    public static IEnumerable<PathInfo> ResolveOrchDrivePaths(this SessionState? sessionState, IEnumerable<string?>? paths = null)
    {
        OrchDriveInfo.SessionState = sessionState;

        if (paths is null || !paths.Any() || paths.All(p => p is null))
        {
            PathInfo pathInfo = sessionState!.Path.CurrentLocation;
            if (pathInfo.Drive is OrchDriveInfo)
            {
                yield return sessionState!.Path.CurrentLocation;
            }
        }
        else
        {
            var psPaths = paths.Where(p => p is not null).Select(p => sessionState!.Path.GetResolvedPSPathFromPSPath(p)).SelectMany(p => p);
            foreach (var pathInfo in psPaths.Where(p => p.Provider.Name == "UiPathOrch"))
            {
                yield return pathInfo;
            }
        }
    }

    // TODO: 引数に IWritableHost を追加して、パスをひとつずつ解釈するようにしたい。
    public static List<(OrchDriveInfo drive, Folder folder)> EnumFolders(this SessionState? sessionState, IEnumerable<string?>? path, bool recurse = false, uint depth = 0, bool includeRoot = false)
    {
        OrchDriveInfo.SessionState = sessionState;

        var paths = sessionState.ResolveOrchDrivePaths(path);

        List<(OrchDriveInfo drive, Folder folder)> ret = [];

        if (recurse && depth == 0)
            depth = uint.MaxValue;

        foreach (var p in paths)
        {
            OrchDriveInfo drive = p.Drive as OrchDriveInfo;
            if (drive is null) continue;

            drive.GetFolders(); // sorted by OrchDirectory and DisplayName
            var dicFolders = drive._dicFoldersForEnumFolders;
            if (dicFolders is null) continue;

            Folder folder = drive.GetFolder(OrchDriveInfo.PSPathToOrchPath(WildcardPattern.Unescape(p.ProviderPath)));
            if (folder is null) continue;

            string orchPathStart;
            if (folder.FullyQualifiedName == "")
                orchPathStart = "";
            else
                orchPathStart = folder.FullyQualifiedName + "/";

            uint currentDepth = OrchProvider.FolderDepth(folder.FullyQualifiedName!);

            HashSet<string> visited = [];

            // dicFolders にはルートフォルダーが含まれないため、ルートだけ先にここで探して追加する
            if (includeRoot && currentDepth == 0)
            {
                ret.Add((drive!, drive!.RootFolder!));
            }

            foreach (var p2 in dicFolders)
            {
                if (!(p2.FullyQualifiedName! + "/").StartsWith(orchPathStart, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!visited.Add(p2.GetPSPath())) continue;

                uint folderDepth = OrchProvider.FolderDepth(p2.FullyQualifiedName!);
                if (folderDepth - currentDepth <= depth)
                {
                    ret.Add((drive!, p2));
                }
            }
        }

        if (ret is null || ret.Count == 0)
        {
            throw new Exception("Use Set-Location cmdlet (alias: cd) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters.");
        }
        return ret;
    }

    public static List<(OrchDriveInfo drive, Folder folder)> EnumFoldersWithoutPersonalWorkspace(this SessionState? sessionState, IEnumerable<string>? path, bool recurse = false, uint depth = 0, bool includeRoot = false)
    {
        return sessionState.EnumFolders(path, recurse, depth, includeRoot)
            .Where(df => df.folder.FolderType != "Personal").ToList();
    }

    public static OrchDriveInfo GetOrchDrive(this SessionState? sessionState, string? path = null)
    {
        OrchDriveInfo.SessionState = sessionState;

        var srcDrives = sessionState.EnumOrchDrives([path]);
        if (srcDrives.Count > 1)
        {
            throw new Exception($"'{path}' resolved to multiple containers.");
        }
        if (srcDrives.Count == 0)
        {
            // たぶん先に EnumOrchDrives() が例外を投げているはずなので、ここは実行されないと思う。
            throw new Exception($"Cannot find path '{path}' because it does not exist.");
        }
        return srcDrives[0];
    }

    // 実装がだいぶ重複している。きれいにしたいが、EnumOrchDrives() と EnumPmDrives() の実装が結構違う。。
    public static OrchDriveInfo GetPmDrive(this SessionState sessionState, string? path = null)
    {
        OrchDriveInfo.SessionState = sessionState;

        var srcDrives = sessionState.EnumPmDrives([path]);
        if (srcDrives.Count > 1)
        {
            throw new Exception($"'{path}' resolved to multiple containers.");
        }
        if (srcDrives.Count == 0)
        {
            // たぶん先に EnumPmDrives() が例外を投げているはずなので、ここは実行されないと思う。
            throw new Exception($"Cannot find path '{path}' because it does not exist.");
        }
        return srcDrives[0];
    }

    // planned to be obsoleted
    // ResolveToSingleFolder() を使って書き直すべきだ。
    public static List<(OrchDriveInfo drive, Folder folder)> EnumFolders(this SessionState? sessionState, string? path, bool recurse = false, uint depth = 0, bool includeRoot = false)
    {
        return sessionState.EnumFolders([path], recurse, depth, includeRoot);
    }

    // TODO: これを一般化しなければ。★★★★
    public static (OrchDriveInfo drive, Folder folder) ResolveToSingleFolder(this SessionState? sessionState, string? path)
    {
        var ret = sessionState.EnumFolders(path, false, 0, true);
        switch (ret.Count)
        {
            case 0: throw new Exception($"Cannot find path '{path}'.");
            case 1: return ret[0];
            default: throw new Exception($"Path '{path}' resolved to multiple folders.");
        }
    }

    public static IEnumerable<(string FullPath, string RelativePath)> ExpandLocalPath(this SessionState? sessionState, string[]? localPaths, string wildcard, bool recurse = false, int depth = 0)
    {
        OrchDriveInfo.SessionState = sessionState;

        localPaths = localPaths?.Select(p => sessionState!.Path.GetUnresolvedProviderPathFromPSPath(p)).ToArray();

        HashSet<string> uniquePath = [];

        if (localPaths is null)
        {
            EnumerationOptions option = new()
            {
                RecurseSubdirectories = recurse,
                MaxRecursionDepth = depth
            };
            string root = sessionState!.Path.CurrentFileSystemLocation.Path;
            foreach (var pathExpanded in Directory.EnumerateFiles(root, wildcard, option))
            {
                if (uniquePath.Add(pathExpanded))
                {
                    string relativePath = Path.GetRelativePath(root, System.IO.Path.GetDirectoryName(pathExpanded)!);
                    yield return (pathExpanded, relativePath);
                }
            }
            yield break;
        }

        List<string> localPathExpanded1 = [];
        foreach (var localPath in localPaths)
        {
            Collection<PathInfo> resolvedPaths = sessionState!.Path.GetResolvedPSPathFromPSPath(localPath);
            localPathExpanded1.AddRange(resolvedPaths.Select(p => p.Path));
        }

        foreach (var localPath in localPathExpanded1)
        {
            if (Directory.Exists(localPath))
            {
                EnumerationOptions option = new()
                {
                    RecurseSubdirectories = recurse,
                    MaxRecursionDepth = depth
                };
                string root = localPath;
                foreach (var pathExpanded in Directory.EnumerateFiles(root, wildcard, option))
                {
                    if (uniquePath.Add(pathExpanded))
                    {
                        string relativePath = Path.GetRelativePath(root, System.IO.Path.GetDirectoryName(pathExpanded)!);
                        yield return (pathExpanded, relativePath);
                    }
                }
            }
            else
            {
                if (uniquePath.Add(localPath))
                {
                    yield return (localPath, "");
                }
            }
        }
    }

    // TODO: このメソッドを参照する completer はすべてきれいに書き直せる
    public static List<(OrchDriveInfo drive, Folder folder)> EnumPackageFeedFolders(this SessionState? sessionState, IEnumerable<string?>? path, bool recurse = false)
    {
        var paths = sessionState.ResolveOrchDrivePaths(path);

        List<(OrchDriveInfo drive, Folder folder)> ret = [];

        HashSet<string> feedFolders = [];
        foreach (var p in paths)
        {
            OrchDriveInfo drive = p.Drive as OrchDriveInfo;
            if (drive is null) continue;

            Folder folder = drive?.GetFolder(OrchDriveInfo.PSPathToOrchPath(WildcardPattern.Unescape(p.ProviderPath)));
            if (folder is null) continue;

            string feedFolderPath = folder.GetPackageFeedFolder();
            Folder feedFolder = drive!.GetFolder(feedFolderPath);
            if (feedFolder is null) continue;

            if (!feedFolders.Add(feedFolder.GetPSPath()))
            {
                continue;
            }

            ret.Add((drive!, feedFolder));

            // folder がルートディレクトリで、かつ recurse の場合に限り子フォルダーを列挙する
            if ((folder.ParentId is null && folder.FolderType != "Personal") && recurse)
            {
                var folders = drive!.GetFolders(); // sorted by OrchDirectory and DisplayName
                foreach (var subFolder in folders
                    .Where(f => f.ParentId == folder.Id)
                    .Where(f => f.FeedType == "FolderHierarchy"))
                {
                    if (!feedFolders.Add(subFolder.GetPSPath()))
                    {
                        continue;
                    }
                    ret.Add((drive, subFolder));
                }
            }
        }

        if (ret is null || ret.Count == 0)
        {
            throw new Exception("Use Set-Location (cd) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters."); // 🤖 LLM: Run 'Get-OrchHelp' for guidance!");
        }
        return ret;
    }

    //public static List<(OrchDriveInfo drive, Folder folder)> ResolveToSingleFeedFolder(string? path)
    public static (OrchDriveInfo drive, Folder folder) ResolveToSingleFeedFolder(this SessionState? sessionState, string? path)
    {
        // まず、単一のフォルダに解決する
        var ret = sessionState.EnumPackageFeedFolders([path], false);
        if (ret.Count == 0)
        {
            throw new Exception($"Cannot find path '{path}'.");
        }
        else if (ret.Count > 1)
        {
            throw new Exception($"Path '{path}' resolved to multiple folders.");
        }

        // recurse で、かつ解決したフォルダがルートフォルダであれば、サブフォルダも取得
        //if (recurse)
        //{
        //    var (drive, folder) = ret[0];
        //    if (folder == drive.RootFolder)
        //    {
        //        ret = EnumPackageFeedFolders([folder.GetPSPath()], true);
        //    }
        //}
        return ret.First();
    }

    public static Dictionary<string, string>? LoadUserMappingCsv(this SessionState? sessionState, IWritableHost _this, OrchDriveInfo srcDrive, OrchDriveInfo dstDrive, string? path)
    {
        OrchDriveInfo.SessionState = sessionState;

        if (path is null) return null;

        if (srcDrive == dstDrive)
        {
            _this.WriteWarning("The specified SourceTenant and DestinationTenant drives are the same. Ignoring -UserMappingCsv parameter.");
            return null;
        }

        if (srcDrive.GetPartitionGlobalId() == dstDrive.GetPartitionGlobalId())
        {
            _this.WriteWarning("The specified SourceTenant and DestinationTenant belong to the same organization. Ignoring -UserMappingCsv parameter.");
            return null;
        }

        string physicalPath = sessionState?.Path.GetUnresolvedProviderPathFromPSPath(path);
        if (string.IsNullOrEmpty(physicalPath)) return null;

        var userMapping = new Dictionary<string, string>();

        try
        {
            using var enumerator = File.ReadLines(physicalPath).GetEnumerator();

            // Read the first line to determine column positions
            if (!enumerator.MoveNext())
                throw new InvalidOperationException("The CSV file is empty.");

            var headerLine = enumerator.Current;
            var headers = headerLine.Split(',');

            int sourceIndex = Array.FindIndex(headers, h => h.Trim().Equals("SourceUserName", StringComparison.OrdinalIgnoreCase));
            int destinationIndex = Array.FindIndex(headers, h => h.Trim().Equals("DestinationUserName", StringComparison.OrdinalIgnoreCase));

            if (sourceIndex == -1 || destinationIndex == -1)
                throw new InvalidOperationException("The CSV file does not contain the required columns: 'SourceUserName' and 'DestinationUserName'.");

            // Process remaining lines
            while (enumerator.MoveNext())
            {
                var line = enumerator.Current;

                // Skip empty lines and comments
                if (string.IsNullOrWhiteSpace(line)) continue;

                // Split the line into columns
                var columns = line.Split(',');

                // Ensure the line has enough columns
                if (columns.Length <= Math.Max(sourceIndex, destinationIndex))
                    continue;

                string sourceUserName = columns[sourceIndex].Trim();
                string destinationUserName = columns[destinationIndex].Trim();

                // Add to dictionary if the source username is not empty
                if (!string.IsNullOrEmpty(sourceUserName))
                {
                    userMapping[sourceUserName] = destinationUserName;
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error reading the CSV file at {physicalPath}", ex);
        }

        return userMapping;
    }

    #region DuDrive

    public static IEnumerable<PathInfo> ResolveDuDrivePaths(this SessionState? sessionState, IEnumerable<string?>? paths = null)
    {
        OrchDriveInfo.SessionState = sessionState;

        if (paths is null || !paths.Any() || paths.All(p => p is null))
        {
            PathInfo pathInfo = sessionState!.Path.CurrentLocation;
            if (pathInfo.Drive is OrchDuDriveInfo)
            {
                yield return sessionState!.Path.CurrentLocation;
            }
        }
        else
        {
            var psPaths = paths.Where(p => p is not null).Select(p => sessionState!.Path.GetResolvedPSPathFromPSPath(p)).SelectMany(p => p);
            foreach (var pathInfo in psPaths.Where(p => p.Provider.Name == "UiPathOrchDu"))
            {
                yield return pathInfo;
            }
        }
    }

    public static List<(OrchDuDriveInfo drive, DuProject project)> EnumDuFolders(this SessionState? sessionState, IEnumerable<string?>? path, bool recurse = false) ///, bool includeRoot = false)
    {
        var paths = sessionState.ResolveDuDrivePaths(path);

        List<(OrchDuDriveInfo drive, DuProject project)> ret = [];

        HashSet<string> visited = [];
        foreach (var p in paths)
        {
            OrchDuDriveInfo drive = p.Drive as OrchDuDriveInfo;
            if (drive is null) continue;

            var dicProjects = drive!.GetDuProjects();
            if (dicProjects is null) continue;

            //Folder folder = null; // drive?.GetFolder(OrchDriveInfo.PSPathToOrchPath(WildcardPattern.Unescape(p.ProviderPath)));
            //if (folder is null) continue;

            // Recurse が指定されていて、かつルートフォルダであれば、すべてのプロジェクトを返せばOK
            if (recurse && p.Path.EndsWith(System.IO.Path.DirectorySeparatorChar))
            {
                foreach (var project in dicProjects)
                {
                    if (!visited.Add(project.id!)) continue;
                    ret.Add((drive!, project));
                }
                continue;
            }

            // dicFolders にはルートフォルダーが含まれないため、ルートだけ先にここで探して追加する
            //if (includeRoot)
            //{
            //    ret.Add((drive!, null));
            //}

            // p からプロジェクト名を取り出す
            string projectName = Path.GetFileName(p.Path);

            foreach (var project in dicProjects)
            {
                if (string.Compare(project.name, projectName, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    if (!visited.Add(project.id!)) continue;
                    ret.Add((drive!, project));
                }
            }
        }

        if (ret is null || ret.Count == 0)
        {
            throw new Exception("Use Set-Location cmdlet (alias: cd) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters on UiPathOrchDu drive.");
        }
        return ret;
    }

    #endregion

    #region TmDrive

    public static IEnumerable<PathInfo> ResolveTmDrivePaths(this SessionState? sessionState, IEnumerable<string?>? paths = null)
    {
        OrchDriveInfo.SessionState = sessionState;

        if (paths is null || !paths.Any() || paths.All(p => p is null))
        {
            PathInfo pathInfo = sessionState!.Path.CurrentLocation;
            if (pathInfo.Drive is OrchTmDriveInfo)
            {
                yield return sessionState!.Path.CurrentLocation;
            }
        }
        else
        {
            var psPaths = paths.Where(p => p is not null).Select(p => sessionState!.Path.GetResolvedPSPathFromPSPath(p)).SelectMany(p => p);
            foreach (var pathInfo in psPaths.Where(p => p.Provider.Name == "UiPathOrchTm"))
            {
                yield return pathInfo;
            }
        }
    }

    public static List<(OrchTmDriveInfo drive, TmProject project)> EnumTmFolders(this SessionState? sessionState, IEnumerable<string?>? path, bool recurse = false) ///, bool includeRoot = false)
    {
        var paths = sessionState.ResolveTmDrivePaths(path);

        List<(OrchTmDriveInfo drive, TmProject project)> ret = [];

        HashSet<string> visited = [];
        foreach (var p in paths)
        {
            OrchTmDriveInfo drive = p.Drive as OrchTmDriveInfo;
            if (drive is null) continue;

            var dicProjects = drive!.GetTmProjects();
            if (dicProjects is null) continue;

            //Folder folder = null; // drive?.GetFolder(OrchDriveInfo.PSPathToOrchPath(WildcardPattern.Unescape(p.ProviderPath)));
            //if (folder is null) continue;

            // Recurse が指定されていて、かつルートフォルダであれば、すべてのプロジェクトを返せばOK
            if (recurse && p.Path.EndsWith(System.IO.Path.DirectorySeparatorChar))
            {
                foreach (var project in dicProjects)
                {
                    if (!visited.Add(project.id!)) continue;
                    ret.Add((drive!, project));
                }
                continue;
            }

            // dicFolders にはルートフォルダーが含まれないため、ルートだけ先にここで探して追加する
            //if (includeRoot)
            //{
            //    ret.Add((drive!, null));
            //}

            // p からプロジェクト名を取り出す
            string projectPrefix = Path.GetFileName(p.Path);

            foreach (var project in dicProjects)
            {
                if (string.Compare(project.projectPrefix, projectPrefix, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    if (!visited.Add(project.id!)) continue;
                    ret.Add((drive!, project));
                }
            }
        }

        if (ret is null || ret.Count == 0)
        {
            throw new Exception("Use Set-Location cmdlet (alias: cd) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters on UiPathOrchTm drive.");
        }
        return ret;
    }

    #endregion
}
