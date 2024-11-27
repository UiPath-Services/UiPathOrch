using System.Data;
using System.Management.Automation;
using System.Text.Json;
using System.Text.RegularExpressions;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;
using Path = System.IO.Path;

namespace UiPath.PowerShell.Core
{
    public static class FolderExtensions
    {
        // returns orchPath
        public static string GetPackageFeedFolder(this Folder folder)
        {
            if (folder == null || folder.FeedType != "FolderHierarchy")
                return "";

            return OrchDriveInfo.GetTopParentPath(folder.FullyQualifiedName!);
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
        public static string GetPSPath(this DuUser entity)         => Path.Combine(entity?.PathProject ?? "", entity?.displayName ?? "");
        public static string GetPSPath(this DuDocumentType entity) => Path.Combine(entity?.PathProject ?? "", entity?.name ?? "");
        public static string GetPSPath(this DuClassifier entity)   => Path.Combine(entity?.PathProject ?? "", entity?.name ?? "");

        public static string GetPSPath(this TmProject entity)     => Path.Combine(entity?.Path ?? "", entity?.projectPrefix ?? "");
        public static string GetPSPath(this TmRequirement entity) => Path.Combine(entity?.Path ?? "", entity?.name ?? "");
        public static string GetPSPath(this TmTestCase entity)    => Path.Combine(entity?.Path ?? "", entity?.name ?? "");
        public static string GetPSPath(this TmTestSet entity)     => Path.Combine(entity?.Path ?? "", entity?.name ?? "");
    }

    public static class OrchCollectionExtensions
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
            return ReferenceEquals(obj1, obj2) || (obj1 != null && obj1.Equals(obj2));
        }

        // 配列とかリストを比べるときは、これだ。
        public static bool SafeSequenceEquals<T>(this IEnumerable<T>? collection1, IEnumerable<T>? collection2)
        {
            if (ReferenceEquals(collection1, collection2)) return true;
            if (collection1 == null && collection2 == null) return true;
            if (collection1 == null || collection2 == null) return false;
            return collection1.SequenceEqual(collection2);
        }

        public static void AddIfNotNull<T>(this List<T> list, T? item) where T : class
        {
            if (item != null)
            {
                list.Add(item);
            }
        }

        public static string? CreateOrFilter<T>(this IEnumerable<T> param, Func<T, string> converter)
        {
            if (param == null || !param.Any()) return null;
            string filter = $"({string.Join(" or ", param.Select(converter))})";
            return string.IsNullOrEmpty(filter) ? null : filter;
        }

        public static string? CreateAndFilter<T>(this IEnumerable<T> param, Func<T, string> converter)
        {
            if (param == null || !param.Any()) return null;
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
        public static List<WildcardPattern>? ConvertToWildcardPatternList(this IEnumerable<string>? input)
        {
            // PathTools.UnescapePSText(n) はしなくても良いのだっけ？
            return input?.Select(n => new WildcardPattern(n, WildcardOptions.IgnoreCase)).ToList();
        }

        public static IEnumerable<T> FilterByClassValues<T, TKey>(
            this IEnumerable<T> source,
            Func<T, TKey?> selector,
            IEnumerable<TKey>? values) where TKey : class
        {
            if (values == null || !values.Any()) return source;
            return source.Where(item => values.Contains(selector(item)));
        }

        public static IEnumerable<T> FilterByStructValues<T, TKey>(
            this IEnumerable<T> source,
            Func<T, TKey> selector,
            IEnumerable<TKey>? values) where TKey : struct
        {
            if (values == null || !values.Any()) return source;
            return source.Where(item => values.Contains(selector(item)));
        }

        // patterns が空であれば、source のすべての要素をそのまま返す
        public static IEnumerable<T> FilterByWildcards<T>(
            this IEnumerable<T> source,
            Func<T?, string?> selector,
            List<WildcardPattern>? patterns)
        {
            if (patterns == null || patterns.Count == 0) return source;
            return source.Where(item => patterns.Any(pattern => pattern.IsMatch(selector(item))));
        }

        // patterns が空であれば、source のすべての要素をそのまま返す
        public static IEnumerable<T> FilterByWildcards<T>(
            this IEnumerable<T> source,
            Func<T?, string?> selector,
            string[]? patterns)
        {
            if (patterns == null || patterns.Length == 0) return source;
            var wpPatterns = patterns.ConvertToWildcardPatternList();
            return source.FilterByWildcards(selector, wpPatterns);
        }

        // patterns が空であれば、空を返す
        public static IEnumerable<T> SelectByWildcards<T>(
            this IEnumerable<T> source,
            Func<T?, string?> selector,
            List<WildcardPattern>? patterns)
        {
            if (patterns == null || patterns.Count == 0) return [];
            return source.Where(item => patterns.Any(pattern => pattern.IsMatch(selector(item))));
        }

        // patterns が空であれば、空を返す
        public static IEnumerable<T> SelectByWildcards<T>(
            this IEnumerable<T> source,
            Func<T?, string?> selector,
            string[]? patterns)
        {
            if (patterns == null || patterns.Length == 0) return [];
            var wpPatterns = patterns.ConvertToWildcardPatternList();
            return source.SelectByWildcards(selector, wpPatterns);
        }

        public static IEnumerable<T> ExcludeByClassValues<T, TKey>(
            this IEnumerable<T> source,
            Func<T?, TKey?> selector,
            IEnumerable<TKey?>? values) where TKey : class
        {
            if (values == null || !values.Any()) return source;
            return source.Where(item => !values.Contains(selector(item)));
        }

        public static IEnumerable<T> ExcludeByStructValues<T, TKey>(
            this IEnumerable<T> source,
            Func<T, TKey> selector,
            IEnumerable<TKey>? values) where TKey : struct
        {
            if (values == null || !values.Any()) return source;
            return source.Where(item => !values.Contains(selector(item)));
        }

        public static IEnumerable<T> ExcludeByWildcards<T>(
            this IEnumerable<T> source,
            Func<T?, string?> selector,
            List<WildcardPattern>? patterns)
        {
            if (patterns == null || !patterns.Any()) return source;
            return source.Where(item => !patterns.Any(pattern => pattern.IsMatch(selector(item))));
        }

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
            if (source == null) return null;
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

    //public static class OrchObjectExtentions
    //{
    //    public static bool AreAllPropertiesNull<T>(this T obj)
    //    {
    //        if (obj == null)
    //            return true;

    //        // Get all properties of the object
    //        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

    //        // Check if all properties are null
    //        return properties.All(property => property.GetValue(obj) == null);
    //    }
    //}

    public static class OrchStringExtensions
    {
        public delegate bool TryParseHandler<T>(string str, out T result);

        public static T? ToNullable<T>(this string? str, TryParseHandler<T> tryParse) where T : struct
        {
            if (str != null && tryParse(str, out var result))
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
            if (value != null)
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
        public static void AssignNumberIfNotNull<T, N>(this T target, N? value, Action<T, N?> setter) where N : struct, IComparable
        {
            if (value.HasValue)
            {
                setter(target, value);
            }
        }

        // Method for bool properties
        public static void AssignBoolIfNotNull<T>(this T target, string? value, Action<T, bool?> setter)
        {
            if (value != null && bool.TryParse(value, out var result))
            {
                setter(target, result);
            }
        }

        public static void AssignBoolIfNotNull<T>(this T target, bool? value, Action<T, bool?> setter)
        {
            if (value != null)
            {
                setter(target, value);
            }
        }

        // 現在の値が null であれば、false を代入しないように工夫されたバージョン
        public static void AssignBoolIfNotFalse<T>(this T target, string? value, Func<T, bool?> getter, Action<T, bool?> setter)
        {
            if (value != null && bool.TryParse(value, out var result))
            {
                // 代入先が null で、かつ代入する値が false の場合には何もしない
                if (getter(target) == null && !result) return;

                setter(target, result);
            }
        }

        public static void AssignDateTimeIfNotNull<T>(this T target, DateTime? value, Action<T, DateTime?> setter, bool convertToUniversalTime = true)
        {
            if (value != null)
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
        public static void AssignIdFromName<T, TElement, TId>(
            this T target,
            string? name,
            Func<IEnumerable<TElement>> getEntitiesFunc,
            Func<TElement, string> getNameFunc,
            Func<TElement, TId> getIdFunc,
            Action<T, TId> setter,
            IWritableHost host,
            string targetName,
            string nameKind)
        {
            if (!string.IsNullOrEmpty(name))
            {
                try
                {
                    var wpName = new WildcardPattern(name, WildcardOptions.IgnoreCase);
                    var entities = getEntitiesFunc().Where(e => wpName.IsMatch(getNameFunc(e)));
                    switch (entities.Take(2).Count())
                    {
                        case 1:
                            setter(target, getIdFunc(entities.First()));
                            break;
                        case 0:
                            host.WriteError(new ErrorRecord(new OrchException(targetName, $"No {nameKind} found with \"{name}\". Please ensure that the specified {nameKind} name is correct."), $"Get{nameKind}Error", ErrorCategory.InvalidOperation, target));
                            break;
                        default:
                            host.WriteError(new ErrorRecord(new OrchException(targetName, $"Multiple {nameKind} found with \"{name}\". Please specify a unique {nameKind} name."), $"Get{nameKind}Error", ErrorCategory.InvalidOperation, target));
                            break;
                    }
                }
                catch (Exception ex)
                {
                    host.WriteError(new ErrorRecord(new OrchException(targetName, ex), $"Get{nameKind}Error", ErrorCategory.InvalidOperation, target));
                }
            }
        }

        private static string? ReplaceLastPartWithAsterisk(string? input)
        {
            if (input == null) return null;

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
            if (value != null && value.Length != 0)
            {
                Tag[] tags = value.ConvertToTags().ToArray();
                if (tags != null) setter(target, tags);
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
            if (tags == null) return null;
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
    } 
}
