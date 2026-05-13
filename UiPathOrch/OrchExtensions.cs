using System.Collections.ObjectModel;
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
    /// Returns the relative path from srcRootFolder.
    /// </summary>
    public static string GetRelativePath(this Folder srcFolder, Folder srcRootFolder)
    {
        if (srcFolder?.FullyQualifiedName == null || srcRootFolder?.FullyQualifiedName == null)
            throw new ArgumentNullException("srcFolder or srcRootFolder has null FullyQualifiedName.");

        string relativePath = srcFolder.FullyQualifiedName[srcRootFolder.FullyQualifiedName.Length..];
        return relativePath.TrimStart('/').TrimEnd('/');
    }

    public static string GetPSPath(this Folder entity) => Path.Combine(entity?.Path ?? "", entity?.DisplayName ?? "");

    public static string GetPSPath(this Entities.Environment entity) => Path.Combine(entity?.Path ?? "", entity?.Name ?? "");
    public static string GetPSPath(this Library entity) => Path.Combine(entity?.Path ?? "", entity?.Id ?? "");
    public static string GetPSPath(this LibraryVersion entity) => Path.Combine(entity?.Path ?? "", entity?.Id ?? "");
    public static string GetPSPath(this Package entity) => Path.Combine(entity?.Path ?? "", entity?.Id ?? "");
    public static string GetPSPath(this User entity) => Path.Combine(entity?.Path ?? "", entity?.UserName ?? "");
    public static string GetPSPath(this UserRoles entity) => Path.Combine(entity?.Path ?? "", entity?.UserEntity?.UserName ?? "");
    public static string GetPSPath(this Role entity) => Path.Combine(entity?.Path ?? "", entity?.Name ?? "");
    public static string GetPSPath(this Webhook entity) => Path.Combine(entity?.Path ?? "", entity?.Name ?? "");
    public static string GetPSPath(this Robot entity) => Path.Combine(entity?.Path ?? "", entity?.User?.Name ?? "");
    public static string GetPSPath(this ExtendedRobot entity) => Path.Combine(entity?.Path ?? "", entity?.Name ?? "");
    public static string GetPSPath(this RobotsFromFolderModel entity) => Path.Combine(entity?.Path ?? "", entity?.Name ?? "");
    public static string GetPSPath(this ExtendedCalendar entity) => Path.Combine(entity?.Path ?? "", entity?.Name ?? "");
    public static string GetPSPath(this MachineFolder entity) => Path.Combine(entity?.Path ?? "", entity?.Name ?? "");
    public static string GetPSPath(this ExtendedMachine entity) => Path.Combine(entity?.Path ?? "", entity?.Name ?? "");
    public static string GetPSPath(this CredentialStore entity) => Path.Combine(entity?.Path ?? "", entity?.Name ?? "");
    public static string GetPSPath(this QueueDefinition entity) => Path.Combine(entity?.Path ?? "", entity?.Name ?? "");
    public static string GetPSPath(this QueueItem entity) => Path.Combine(entity?.PathName ?? "", entity?.Id.ToString() ?? "");
    public static string GetPSPath(this Asset entity) => Path.Combine(entity?.Path ?? "", WildcardPattern.Escape(entity?.Name ?? ""));
    public static string GetPSPath(this Release entity) => Path.Combine(entity?.Path ?? "", entity?.Name ?? "");
    public static string GetPSPath(this PersonalWorkspace entity) => Path.Combine(entity?.Path ?? "", entity?.Name ?? "");
    public static string GetPSPath(this ProcessSchedule entity) => Path.Combine(entity?.Path ?? "", entity?.Name ?? "");
    public static string GetPSPath(this HttpTrigger entity) => Path.Combine(entity?.Path ?? "", entity?.Name ?? "");
    public static string GetPSPath(this ApiTrigger entity) => Path.Combine(entity?.Path ?? "", entity?.Name ?? "");
    public static string GetPSPath(this Session entity) => Path.Combine(entity?.Path ?? "", entity?.Robot?.Name ?? "");
    public static string GetPSPath(this TestSet entity) => Path.Combine(entity?.Path ?? "", entity?.Name ?? "");
    public static string GetPSPath(this TestCaseDefinition entity) => Path.Combine(entity?.Path ?? "", entity?.Name ?? "");
    public static string GetPSPath(this TestSetExecution entity) => Path.Combine(entity?.Path ?? "", entity?.Name ?? "");
    public static string GetPSPath(this TestSetSchedule entity) => Path.Combine(entity?.Path ?? "", entity?.Name ?? "");
    public static string GetPSPath(this TestDataQueue entity) => Path.Combine(entity?.Path ?? "", entity?.Name ?? "");
    public static string GetPSPath(this TaskCatalog entity) => Path.Combine(entity?.Path ?? "", entity?.Name ?? "");
    public static string GetPSPath(this Settings entity) => Path.Combine(entity?.Path ?? "", entity?.Name ?? "");
    public static string GetPSPath(this Bucket entity) => Path.Combine(entity?.Path ?? "", entity?.Name ?? "");
    public static string GetPSPath(this BlobFile entity) => Path.Combine(entity?.PathBucket ?? "", entity?.FullPath ?? "");
    public static string GetPSPath(this ResponseDictionaryItem entity) => Path.Combine(entity?.Path ?? "", entity?.Key ?? "");
    public static string GetPSPath(this AuditLog entity) => Path.Combine(entity?.Path ?? "", entity?.Id.ToString() ?? "");
    public static string GetPSPath(this Entities.Job entity) => Path.Combine(entity?.Path ?? "", entity?.Id.ToString() ?? "");
    public static string GetPSPath(this BusinessRule entity) => Path.Combine(entity?.Path ?? "", entity?.Name ?? "");
    public static string GetPSPath(this OrchTask entity) => Path.Combine(entity?.Path ?? "", entity?.Id.ToString() ?? "");

    /// <summary>
    /// Formats job information as a string for tooltips.
    /// </summary>
    public static string FormatTooltip(this Entities.Job job)
    {
        string tiphelp = $"{job.Id,10} C{job.CreationTime?.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss")}";
        if (job.StartTime is not null)
            tiphelp += $"  S{job.StartTime?.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss")}";
        else
            tiphelp += "                      ";
        if (job.EndTime is not null)
            tiphelp += $"  E{job.EndTime?.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss")}";
        else
            tiphelp += "                      ";
        tiphelp += $" {job.State,11} {job.Path}\\{job.ReleaseName}";
        return tiphelp;
    }

    // Org-shared entities (ListCachePerOrganization plus the
    // KeyedSingleCachePerOrganization-backed SearchPmDirectoryCache) take an
    // explicit drivePath because the entity instance is a singleton across all
    // drives in the org and can't carry a drive-local Path field.
    public static string GetPSPath(this PmUser entity, string drivePath) => Path.Combine(drivePath, entity?.userName ?? "");
    public static string GetPSPath(this PmRobotAccount entity, string drivePath) => Path.Combine(drivePath, entity?.name ?? "");
    public static string GetPSPath(this PmGroup entity, string drivePath) => Path.Combine(drivePath, entity?.name ?? "");
    public static string GetPSPath(this PmGroupMember entity, string drivePath) => Path.Combine(drivePath, entity?.groupName ?? "", entity?.name ?? "");
    public static string GetPSPath(this PmDirectoryEntityInfo entity, string drivePath) => Path.Combine(drivePath, entity?.identityName ?? "");
    public static string GetPSPath(this ExternalResource entity, string drivePath) => Path.Combine(drivePath, entity?.name ?? "");
    public static string GetPSPath(this ExternalClient entity, string drivePath) => Path.Combine(drivePath, entity?.name ?? "");
    public static string GetPSPath(this NuLicensedGroup entity, string drivePath) => Path.Combine(drivePath, entity?.name ?? "");
    public static string GetPSPath(this NuLicensedGroupMember entity, string drivePath) => Path.Combine(drivePath, entity?.name ?? "");
    public static string GetPSPath(this AccessAllowedMember entity, string drivePath) => Path.Combine(drivePath, entity?.name ?? "");

    // DirectoryUser / DirectoryRobotUser / DirectoryApplication inherit from
    // PmGroupMember (whose Path field was removed in Phase 3); their GetPSPath
    // takes drivePath the same way the parent does.
    public static string GetPSPath(this DirectoryUser entity, string drivePath) => Path.Combine(drivePath, entity?.name ?? "");
    public static string GetPSPath(this DirectoryRobotUser entity, string drivePath) => Path.Combine(drivePath, entity?.name ?? "");
    public static string GetPSPath(this DirectoryApplication entity, string drivePath) => Path.Combine(drivePath, entity?.name ?? "");

    public static string GetPSPath(this DuProject entity) => Path.Combine(entity?.Path ?? "", entity?.name ?? "");
    public static string GetPSPath(this DuRole entity) => Path.Combine(entity?.Path ?? "", entity?.name ?? "");
    public static string GetPSPath(this DuUser entity) => Path.Combine(entity?.Path ?? "", entity?.displayName ?? "");
    public static string GetPSPath(this DuDocumentType entity) => Path.Combine(entity?.Path ?? "", entity?.name ?? "");
    public static string GetPSPath(this DuClassifier entity) => Path.Combine(entity?.Path ?? "", entity?.name ?? "");
    public static string GetPSPath(this DuExtractor entity) => Path.Combine(entity?.Path ?? "", entity?.name ?? "");

    public static string GetPSPath(this TmProject entity) => Path.Combine(entity?.Path ?? "", entity?.projectPrefix ?? "");
    public static string GetPSPath(this TmRequirement entity) => Path.Combine(entity?.Path ?? "", entity?.name ?? "");
    public static string GetPSPath(this TmTestCase entity) => Path.Combine(entity?.Path ?? "", entity?.name ?? "");
    public static string GetPSPath(this TmTestSet entity) => Path.Combine(entity?.Path ?? "", entity?.name ?? "");
    public static string GetPSPath(this TmTestExecution entity) => Path.Combine(entity?.Path ?? "", entity?.name ?? "");

    public static string TipHelp(this PmGroupMember? entity) => $"{entity?.name}{(string.IsNullOrEmpty(entity?.displayName) ? "" : $" ({entity.displayName})")}";
    public static string TipHelp(this PmDirectoryEntityInfo? entity, string drivePath) => $"{entity?.GetPSPath(drivePath)}{(string.IsNullOrEmpty(entity?.displayName) ? "" : $" ({entity.displayName})")}";
    public static string TipHelp(this ExtendedRobot entity) => $"{entity?.GetPSPath()}{(string.IsNullOrEmpty(entity?.Username) ? "" : $" ({entity.Username})")}";
    public static string TipHelp(this Library? entity) => $"{entity?.GetPSPath()}";
}

internal static class OrchCollectionExtensions
{
    // The following method has worse performance than == when comparing T& references. It also does not check for null.
    // Stop using this and compare with == instead.
    // Be careful not to confuse when to use ==, SafeEquals(), and SafeSequenceEquals().
    //public static bool SafeEquals<T>(this T? obj1, T obj2) where T : struct
    //{
    //    return obj1.Equals(obj2);
    //}

    // This is useful when comparing reference type values.
    public static bool SafeEquals<T>(this T? obj1, T? obj2) where T : class
    {
        return ReferenceEquals(obj1, obj2) || (obj1 is not null && obj1.Equals(obj2));
    }

    // Use this when comparing arrays or lists.
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

    // Is this needed?
    //public static WildcardPattern ConvertToWildcardPattern(this string input)
    //{
    //    return new WildcardPattern(input, WildcardOptions.IgnoreCase);
    //}

    // Since WildcardPattern enumeration is used repeatedly, it is better to return it as a List here.
    public static List<WildcardPattern>? ConvertToWildcardPatternList(this IEnumerable<string?>? input)
    {
        // Is it OK to not call PathTools.UnescapePSText(n)?
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

    // If patterns is empty, return all elements of source as-is
    public static IEnumerable<T> FilterByWildcards<T>(
        this IEnumerable<T> source,
        Func<T?, string?> selector,
        IReadOnlyList<WildcardPattern>? patterns)
    {
        if (patterns is null || patterns.Count == 0) return source;
        return source.Where(item => patterns.Any(pattern => pattern.IsMatch(selector(item))));
    }

    /// <summary>
    /// Multi-selector variant: keep an item if any pattern matches the
    /// projection of any selector. Useful when an entity has several
    /// equivalent identifiers (e.g. a User's <c>UserName</c> vs
    /// <c>EmailAddress</c> — Azure AD B2B guests get a mangled
    /// <c>xxx#ext#@tenant.onmicrosoft.com</c> UserName but keep their
    /// canonical EmailAddress, and callers should be able to pass either
    /// form interchangeably).
    ///
    /// Named <c>FilterByWildcardsAny</c> (not an overload of
    /// <c>FilterByWildcards</c>) so collection-expression callers don't
    /// race the single-selector overload during type inference. C# 12
    /// resolves a bare <c>[a, b]</c> against the single-selector signature
    /// for certain nested generic T, even though it's a valid two-element
    /// collection — having a distinct name sidesteps that ambiguity.
    /// </summary>
    public static IEnumerable<T> FilterByWildcardsAny<T>(
        this IEnumerable<T> source,
        IReadOnlyList<Func<T?, string?>> selectors,
        IReadOnlyList<WildcardPattern>? patterns)
    {
        if (patterns is null || patterns.Count == 0) return source;
        return source.Where(item =>
            patterns.Any(pattern => selectors.Any(sel => pattern.IsMatch(sel(item)))));
    }

    /// <summary>
    /// Filter a Folder <c>UserRoles</c> list by <c>-UserName</c> wildcards.
    /// Matching considers both <c>UserName</c> (tenant form) and
    /// <c>EmailAddress</c> (canonical) by indirecting through
    /// <c>drive.Users.Get()</c> — the Folder users API returns
    /// <c>UserEntity</c> without an <c>EmailAddress</c> field, so direct
    /// Folder-level matching can't honor Azure AD B2B guest aliases.
    /// A folder user whose <c>UserEntity.Id</c> has no matching tenant
    /// user is excluded. Returns the source unchanged when patterns is
    /// null/empty (= no UserName filter applied).
    /// </summary>
    public static IEnumerable<UserRoles> FilterFolderUsersByUserName(
        this IEnumerable<UserRoles> source,
        OrchDriveInfo drive,
        IReadOnlyList<WildcardPattern>? wpUserName)
    {
        if (wpUserName is null || wpUserName.Count == 0) return source;
        var matchedIds = drive.Users.Get()
            .Where(u => u.Id is not null &&
                wpUserName.Any(p => p.IsMatch(u.UserName ?? "") || p.IsMatch(u.EmailAddress ?? "")))
            .Select(u => u.Id!.Value)
            .ToHashSet();
        return source.Where(fu =>
            fu.UserEntity?.Id is not null && matchedIds.Contains(fu.UserEntity.Id.Value));
    }

    // If patterns is empty, return all elements of source as-is
    public static IEnumerable<T> FilterByWildcards<T>(
        this IEnumerable<T> source,
        Func<T?, string?> selector,
        string[]? patterns)
    {
        if (patterns is null || patterns.Length == 0) return source;
        var wpPatterns = patterns.ConvertToWildcardPatternList();
        return source.FilterByWildcards(selector, wpPatterns);
    }

    // If patterns is empty, return empty
    public static IEnumerable<T> SelectByWildcards<T>(
        this IEnumerable<T> source,
        Func<T?, string?> selector,
        List<WildcardPattern>? patterns)
    {
        if (patterns is null || patterns.Count == 0) return [];
        return source.Where(item => patterns.Any(pattern => pattern.IsMatch(selector(item))));
    }

    // If patterns is empty, return empty
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
        // Unescape backtick escaping
        return input.Replace("``", "`").Replace("`", "");
    }

    public static IEnumerable<string> SplitByUnescapedCommas(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return Enumerable.Empty<string>();
        }

        // Regular expression pattern
        var pattern = @"((?:[^`]|``|`.)+?)(?:,|$)";
        var matches = Regex.Matches(input, pattern);

        var result = matches.Cast<Match>()
            .Select(m => UnescapeBackticks(m.Groups[1].Value.Trim()))
            .Where(s => !string.IsNullOrWhiteSpace(s));

        return result;
    }

    // Since the input may come from CSV, split the first element by commas. Use the remaining elements as-is.
    // The following processing ensures that escaped commas are not used as delimiters.
    // sourceArray = Version[0].Split(',').Concat(Version.Skip(1)).ToArray();
    internal static IEnumerable<string>? Split1stValueByUnescapedCommas(this IEnumerable<string>? source)
    {
        if (source is null) return null;
        return SplitByUnescapedCommas(source.FirstOrDefault()).Concat(source.Skip(1));
    }
    #endregion

    /// <summary>
    /// Wrap <paramref name="entity"/> in a <see cref="PSObject"/> with a
    /// per-output <c>Path</c> note property. Used by cmdlets that emit
    /// org-shared entities (<see cref="SingleCachePerOrganization{T}"/>,
    /// <see cref="ListCachePerOrganization{T}"/>) so each <c>WriteObject</c>
    /// can carry its own drive context without mutating the shared cached
    /// instance. <c>$x[0].BaseObject</c> stays the singleton; <c>$x[0].Path</c>
    /// resolves through the note property and is independent per emit.
    ///
    /// Returns <c>null</c> when the entity is null — convenient for collection
    /// callers like <c>seq.Select(e =&gt; e.WithPath(...))</c> where the filter
    /// chain may leave nulls in place (PowerShell's <c>WriteObject(null, true)</c>
    /// silently drops them).
    /// </summary>
    public static PSObject? WithPath<T>(this T? entity, string path) where T : class
    {
        if (entity is null) return null;
        var pso = new PSObject(entity);
        pso.Properties.Add(new PSNoteProperty("Path", path));
        return pso;
    }

    /// <summary>
    /// Attach an additional note property to a wrapped PSObject. Designed to
    /// chain after <see cref="WithPath{T}"/> when an entity needs more than
    /// one drive-local property (e.g. <c>PathGroupName</c> on
    /// <c>PmGroupMember</c>). Null-safe to match WithPath.
    /// </summary>
    public static PSObject? WithNoteProperty(this PSObject? pso, string name, object? value)
    {
        if (pso is null) return null;
        pso.Properties.Add(new PSNoteProperty(name, value));
        return pso;
    }

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

        // Replace invalid characters with '_'
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

        // Remove trailing dots and spaces
        validString = validString.TrimEnd('.', ' ');

        // Check for Windows reserved words
        string[] reservedNames = {
            "CON","PRN","AUX","NUL",
            "COM1","COM2","COM3","COM4","COM5","COM6","COM7","COM8","COM9",
            "LPT1","LPT2","LPT3","LPT4","LPT5","LPT6","LPT7","LPT8","LPT9"
        };
        if (reservedNames.Contains(validString.ToUpperInvariant()))
        {
            validString = "_" + validString;
        }

        // Length limit (255)
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

    /// <summary>Compare against <paramref name="source"/>, set on <paramref name="target"/>. For PATCH payloads.</summary>
    public static bool AssignStringIfNotNull<T>(this T target, string? value, T source, Func<T, string?> getter, Action<T, string?> setter)
    {
        if (value is null) return false;
        var current = getter(source);
        if (string.IsNullOrEmpty(current) && string.IsNullOrEmpty(value)) return false;
        if (current == value) return false;
        setter(target, value);
        return true;
    }

    public static void AssignStringIfNotNullOrEmpty<T>(this T target, string? value, Action<T, string?> setter)
    {
        if (!string.IsNullOrEmpty(value))
        {
            setter(target, value);
        }
    }

    // Generic method for nullable numeric types
    // Do not set zero. When a CSV empty column is received by an int? parameter, it becomes zero.
    // Using this method, specifying a blank for an int parameter in CSV will not overwrite existing data with zero.
    // TODO: Actually, should null be set when "" is specified from either CSV or command line?
    // It might be better to deprecate this method and replace with AssignNumberIfNotNull().
    public static void AssignNumberIfNotNullOrZero<T, N>(this T target, N? value, Action<T, N?> setter) where N : struct, IComparable
    {
        if (value.HasValue && !value.Value.Equals(default(N)))
        {
            setter(target, value);
        }
    }

    /// <summary>Compare against <paramref name="source"/>, set on <paramref name="target"/>. For PATCH payloads.</summary>
    public static bool AssignNumberIfNotNullOrZero<T, N>(this T target, N? value, T source, Func<T, N?> getter, Action<T, N?> setter) where N : struct, IComparable
    {
        if (!value.HasValue || value.Value.Equals(default(N))) return false;
        if (Nullable.Equals(getter(source), value)) return false;
        setter(target, value);
        return true;
    }

    // Use this for members that accept zero.
    // Note that when a CSV empty column is specified, zero is passed to the parameter.
    // If the existing value is unknown, remove that column from the CSV. Then null will be passed to the int parameter.
    public static void AssignNumberIfNotNull<T, N>(this T target, N? value, Action<T, N?> setter) where N : struct, IComparable
    {
        if (value.HasValue)
        {
            setter(target, value);
        }
    }

    public static bool AssignNumberIfNotNull<T, N>(this T target, N? value, T source, Func<T, N?> getter, Action<T, N?> setter) where N : struct, IComparable
    {
        if (!value.HasValue) return false;
        if (Nullable.Equals(getter(source), value)) return false;
        setter(target, value);
        return true;
    }

    // Convert a string value to int and assign. If a non-numeric value like "" is specified, set null.
    // If null is specified, do nothing.
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
        if (string.IsNullOrEmpty(value)) return; // null or "" means "not specified"

        if (bool.TryParse(value, out var result))
        {
            setter(target, result);
        }
        else
        {
            setter(target, null);
        }
    }

    /// <summary>Compare against <paramref name="source"/>, set on <paramref name="target"/>. For PATCH payloads.</summary>
    public static bool AssignBoolIfNotNull<T>(this T target, string? value, T source, Func<T, bool?> getter, Action<T, bool?> setter)
    {
        if (string.IsNullOrEmpty(value)) return false;

        bool? newValue;
        if (bool.TryParse(value, out var result))
            newValue = result;
        else
            newValue = null;

        if (getter(source) == newValue) return false;
        setter(target, newValue);
        return true;
    }

    public static void AssignBoolIfNotNull<T>(this T target, bool? value, Action<T, bool?> setter)
    {
        if (value is not null)
        {
            setter(target, value);
        }
    }

    // Version that avoids assigning false when the current value is null
    public static void AssignBoolIfNotFalse<T>(this T target, string? value, Func<T, bool?> getter, Action<T, bool?> setter)
    {
        if (string.IsNullOrEmpty(value)) return; // null or "" means "not specified"

        if (bool.TryParse(value, out var result))
        {
            // Do nothing if the target is null and the value to assign is false
            if (getter(target) is null && !result) return;

            setter(target, result);
        }
    }

    /// <summary>Compare against <paramref name="source"/>, set on <paramref name="target"/>. For dirty-flag tracking with PUT payloads.</summary>
    public static bool AssignBoolIfNotFalse<T>(this T target, string? value, T source, Func<T, bool?> getter, Action<T, bool?> setter)
    {
        if (string.IsNullOrEmpty(value)) return false;

        if (bool.TryParse(value, out var result))
        {
            // Skip if source already has the same value
            if (getter(source) == result) return false;

            // Apply the original "not false" guard against the target (DeepCopy),
            // not the source, to preserve the existing behavior.
            if (getter(target) is null && !result) return false;

            setter(target, result);
            return true;
        }
        return false;
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

    // Pass a capitalized name for nameKind.
    // Returns false if processing cannot continue.
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
            var entities = getEntitiesFunc().Where(e => wpName.IsMatch(getNameFunc(e))).Take(2).ToList();
            switch (entities.Count)
            {
                case 1:
                    setter(target, getIdFunc(entities[0]));
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

        // Split by period
        string[] parts = input.Split('.');

        if (parts.Length >= 3)
        {
            parts[2] = "*";
            return string.Join(".", parts.Take(3));
        }
        return input;
    }

    // The following two methods could share a fully common implementation using lambda expressions, but that would be very difficult to use.
    // Having a shared implementation of ReplaceLastNumberWithAsterisk() should suffice.
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

    /// <summary>
    /// Multi-row CSV-aggregation merge for a string field. Records the "best opinion seen so far"
    /// per <paramref name="key"/> with priority <c>non-empty &gt; "" &gt; null</c>, last-writer-wins
    /// among non-empty values. Used by Set-OrchAsset / Set-OrchCredentialAsset / Set-OrchSecretAsset
    /// for Description, where the CSV exporter writes the value on the first row and leaves later
    /// rows empty: the lone non-empty value on row 1 wins over the empty cells, while a single
    /// direct call with <c>""</c> still records the empty as a "clear" intent for that key.
    /// <para>
    /// null incoming values are ignored entirely (the row simply didn't carry the column).
    /// </para>
    /// </summary>
    public static void MergeNonEmptyValue<TKey>(this IDictionary<TKey, string> resolvedValues, TKey key, string? rowValue)
        where TKey : notnull
    {
        if (rowValue is null) return;
        if (!resolvedValues.TryGetValue(key, out var existing)
            || (existing.Length == 0 && rowValue.Length > 0)
            || (existing.Length > 0 && rowValue.Length > 0))
        {
            resolvedValues[key] = rowValue;
        }
        // else: existing is non-empty and incoming is "", or both are "" — keep existing.
    }

    // keyValues are key-value pairs separated by =. e.g., "tag1" or "tag2=value".
    // Used inside Add-OrchHoge, Update-OrchHoge, etc.
    public static IEnumerable<Tag> ConvertToTags(this string[]? keyValues)
    {
        foreach (var keyValue in keyValues?.SelectMany(elem => elem.Split(',')) ?? [])
        {
            if (string.IsNullOrEmpty(keyValue)) continue;
            string[] parts = keyValue.Split('=', 2);

            // Get the key and value
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

    // This is called from within WriteCsvContent().
    internal static string? ConvertToString(this Tag[]? tags)
    {
        if (tags is null) return null;
        return string.Join(',', tags.Select(t => t.ToString()));
    }

    // obsoleted: Use ConvertToTags() instead
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
    /// Appends the specified string followed by a LF newline (\n).
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
    // Using writer.WriteLine(string.Join(',', values) would concatenate strings internally.
    // Calling writer.Write() sequentially is more efficient.
    internal static void WriteCsvLine(this TextWriter? writer, IEnumerable<string?> values)
    {
        if (writer is null) return;

        bool first = true;
        foreach (var value in values)
        {
            if (!first)
            {
                writer.Write(','); // Insert comma after the first element
            }
            else first = false;
            writer.Write(value);
        }
        writer.WriteLine(); // Add a newline at the end
    }
}

// In the context of PSCmdlet, this should be called on PSCmdlet.SessionState.
// In contexts where PSCmdlet.SessionState is not accessible, use OrchDriveInfo.SessionState instead.
// OrchDriveInfo.SessionState is set in OrchProvider.Start(), but there have been situations where this value becomes invalid.
// This has never been encountered in normal usage, but the current location could not be correctly retrieved when using PowerShell.MCP.
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

    // If paths is not specified, return only the current drive.
    // T can be OrchDriveInfo, OrchDuDriveInfo, etc.
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
        // Sort by drive name so multi-drive cmdlets emit results in a
        // predictable order regardless of -Path argument order
        // (matches the behavior already in EnumPmDrives).
        return drives.Distinct().OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    // If paths is not specified, return only the current drive
    public static List<OrchDriveInfo> EnumOrchDrives(this SessionState? sessionState, IEnumerable<string?>? path = null)
    {
        return sessionState.EnumOrchDrivesImpl<OrchDriveInfo>(path);
    }

    // If paths is not specified, return only the current drive
    public static List<OrchDuDriveInfo> EnumDuDrives(this SessionState? sessionState, IEnumerable<string?>? path = null)
    {
        return sessionState.EnumOrchDrivesImpl<OrchDuDriveInfo>(path);
    }

    public static List<OrchTmDriveInfo> EnumTmDrives(this SessionState? sessionState, IEnumerable<string?>? path = null)
    {
        return sessionState.EnumOrchDrivesImpl<OrchTmDriveInfo>(path);
    }

    // If paths is not specified, return only the current drive
    // PmDrive has a separate implementation because it must be searchable from any drive.

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


    // Similar to EnumOrchDrive, but this does not consider the current drive.
    // Used to resolve Destination.
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
            if (dicFolders is null || dicFolders.Count == 0) continue;

            Folder folder = drive.GetFolder(OrchDriveInfo.PSPathToOrchPath(WildcardPattern.Unescape(p.ProviderPath)));
            if (folder is null) continue;

            string orchPathStart;
            if (folder.FullyQualifiedName == "")
                orchPathStart = "";
            else
                orchPathStart = folder.FullyQualifiedName + "/";

            uint currentDepth = OrchProvider.FolderDepth(folder.FullyQualifiedName!);

            HashSet<string> visited = [];

            // dicFolders does not contain the root folder, so search for and add root first here
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
            .Where(df => df.folder.FolderType != "Personal" && df.folder.FeedType != "PersonalWorkspace").ToList();
    }

    public static List<(OrchDriveInfo drive, Folder folder)> EnumFoldersWithoutPersonalWorkspace(this SessionState? sessionState, string? path, bool recurse = false, uint depth = 0, bool includeRoot = false)
    {
        return sessionState.EnumFoldersWithoutPersonalWorkspace(path is null ? null : [path], recurse, depth, includeRoot);
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
            // EnumOrchDrives() should have already thrown an exception, so this probably will not execute.
            throw new Exception($"Cannot find path '{path}' because it does not exist.");
        }
        return srcDrives[0];
    }

    // The implementation has significant duplication. Would like to clean up, but EnumOrchDrives() and EnumPmDrives() differ considerably.
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
            // EnumPmDrives() should have already thrown an exception, so this probably will not execute.
            throw new Exception($"Cannot find path '{path}' because it does not exist.");
        }
        return srcDrives[0];
    }

    // planned to be obsoleted
    // This should be rewritten using ResolveToSingleFolder().
    public static List<(OrchDriveInfo drive, Folder folder)> EnumFolders(this SessionState? sessionState, string? path, bool recurse = false, uint depth = 0, bool includeRoot = false)
    {
        return sessionState.EnumFolders([path], recurse, depth, includeRoot);
    }

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

            // Enumerate child folders only when folder is the root directory and recurse is specified
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
        // First, resolve to a single folder
        var ret = sessionState.EnumPackageFeedFolders([path], false);
        if (ret.Count == 0)
        {
            throw new Exception($"Cannot find path '{path}'.");
        }
        else if (ret.Count > 1)
        {
            throw new Exception($"Path '{path}' resolved to multiple folders.");
        }

        // If recurse and the resolved folder is the root folder, also get subfolders
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
            var headers = UiPath.PowerShell.Commands.CsvHelper.CsvLine.Split(headerLine);

            int sourceIndex = headers.FindIndex(h => h.Trim().Equals("SourceUserName", StringComparison.OrdinalIgnoreCase));
            int destinationIndex = headers.FindIndex(h => h.Trim().Equals("DestinationUserName", StringComparison.OrdinalIgnoreCase));

            if (sourceIndex == -1 || destinationIndex == -1)
                throw new InvalidOperationException("The CSV file does not contain the required columns: 'SourceUserName' and 'DestinationUserName'.");

            // Process remaining lines
            while (enumerator.MoveNext())
            {
                var line = enumerator.Current;

                // Skip empty lines and comments
                if (string.IsNullOrWhiteSpace(line)) continue;

                // Split the line into columns
                var columns = UiPath.PowerShell.Commands.CsvHelper.CsvLine.Split(line);

                // Ensure the line has enough columns
                if (columns.Count <= Math.Max(sourceIndex, destinationIndex))
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
            if (dicProjects is null || dicProjects.Length == 0) continue;

            //Folder folder = null; // drive?.GetFolder(OrchDriveInfo.PSPathToOrchPath(WildcardPattern.Unescape(p.ProviderPath)));
            //if (folder is null) continue;

            // If Recurse is specified and this is the root folder, just return all projects
            if (recurse && p.Path.EndsWith(System.IO.Path.DirectorySeparatorChar))
            {
                foreach (var project in dicProjects)
                {
                    if (!visited.Add(project.id!)) continue;
                    ret.Add((drive!, project));
                }
                continue;
            }

            // dicFolders does not contain the root folder, so search for and add root first here
            //if (includeRoot)
            //{
            //    ret.Add((drive!, null));
            //}

            // Extract the project name from p
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
            if (dicProjects is null || dicProjects.Count == 0) continue;

            //Folder folder = null; // drive?.GetFolder(OrchDriveInfo.PSPathToOrchPath(WildcardPattern.Unescape(p.ProviderPath)));
            //if (folder is null) continue;

            // If Recurse is specified and this is the root folder, just return all projects
            if (recurse && p.Path.EndsWith(System.IO.Path.DirectorySeparatorChar))
            {
                foreach (var project in dicProjects)
                {
                    if (!visited.Add(project.id!)) continue;
                    ret.Add((drive!, project));
                }
                continue;
            }

            // dicFolders does not contain the root folder, so search for and add root first here
            //if (includeRoot)
            //{
            //    ret.Add((drive!, null));
            //}

            // Extract the project name from p
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
