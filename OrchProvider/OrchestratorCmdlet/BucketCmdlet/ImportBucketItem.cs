using System.Globalization;
using System.Management.Automation;
using System.Xml.Linq;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;
using TPositional = UiPath.PowerShell.Positional.Source_Name;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsData.Import, "OrchBucketItem", SupportsShouldProcess = true)]
[OutputType(typeof(Bucket))]
class ImportBucketItemCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Source { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BucketNameCompleter<TPositional, True>))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    private IEnumerable<(string filePath, string destination, string bucketName)> ResolveSources()
    {
        var cmp = StringComparer.CurrentCultureIgnoreCase;
        var dirSeps = new[] { System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar };

        // Recurse=false → 深さ0。Recurse=true かつ Depth 未指定 → 無制限
        uint effDepth = !Recurse ? 0u
            : (MyInvocation.BoundParameters.ContainsKey(nameof(Depth)) ? Depth : uint.MaxValue);

        static string LastDirName(string path)
            => System.IO.Path.GetFileName(System.IO.Path.TrimEndingDirectorySeparator(path));

        // .NET 8 標準の深さ制御を利用
        static EnumerationOptions MakeEnumOptions(uint maxDepth) => new EnumerationOptions
        {
            RecurseSubdirectories = maxDepth == uint.MaxValue || maxDepth > 0u,
            MaxRecursionDepth = maxDepth == uint.MaxValue ? int.MaxValue : unchecked((int)maxDepth),
            IgnoreInaccessible = true,
            ReturnSpecialDirectories = false,
        };

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var rows = new List<(string filePath, string destination, string bucketName)>();

        foreach (var input in Source ?? Array.Empty<string>())
        {
            var resolved = GetResolvedProviderPathFromPSPath(input, out ProviderInfo provider);
            if (!provider.Name.Equals("FileSystem", StringComparison.OrdinalIgnoreCase))
                throw new PSArgumentException(
                    $"-Source supports only FileSystem provider paths. Actual: {provider.Name} ({input})");

            foreach (var src in resolved)
            {
                if (System.IO.File.Exists(src))
                {
                    // 単一ファイル：destination は常に空、bucket は直近親フォルダ名
                    var f = System.IO.Path.GetFullPath(src);
                    if (!seen.Add(f)) continue;

                    var parent = System.IO.Path.GetDirectoryName(f)!;
                    var bucket = LastDirName(parent);
                    rows.Add((f, "", bucket));
                }
                else if (System.IO.Directory.Exists(src))
                {
                    // フォルダ：そのフォルダをアンカーにして配下を列挙（Depth/Recurse 適用）
                    var anchor = System.IO.Path.GetFullPath(src);

                    IEnumerable<string> files;
                    try
                    {
                        files = System.IO.Directory
                            .EnumerateFiles(anchor, "*", MakeEnumOptions(effDepth))
                            .Select(System.IO.Path.GetFullPath);
                    }
                    catch
                    {
                        files = Array.Empty<string>();
                    }

                    foreach (var f in files)
                    {
                        if (!seen.Add(f)) continue;

                        // アンカーからの相対パスで destination / bucket を決める
                        var rel = System.IO.Path.GetRelativePath(anchor, f);          // 例: "sub\subsub\桶\file.csv"
                        var relDir = System.IO.Path.GetDirectoryName(rel) ?? "";
                        var parts = relDir.Split(dirSeps, StringSplitOptions.RemoveEmptyEntries);

                        string bucket, dest;
                        if (parts.Length == 0)
                        {
                            bucket = LastDirName(anchor);   // 直下ファイル：バケットはアンカー名
                            dest = "";
                        }
                        else
                        {
                            bucket = parts[^1];             // 直近親をバケットに
                            dest = parts.Length > 1       // それ以前を destination に（'/' 連結）
                                ? string.Join('/', parts, 0, parts.Length - 1)
                                : "";
                        }

                        rows.Add((f, dest, bucket));
                    }
                }
                else
                {
                    WriteVerbose($"Path not found: {src}");
                }
            }
        }

        // 安定した出力順：destination → bucket → ファイル名 → フルパス
        foreach (var r in rows
            .OrderBy(t => t.destination, cmp)
            .ThenBy(t => t.bucketName, cmp)
            .ThenBy(t => System.IO.Path.GetFileName(t.filePath), cmp)
            .ThenBy(t => t.filePath, cmp))
        {
            yield return r;
        }
    }

    protected override void ProcessRecord()
    {
        if ((Recurse || Depth > 1) && (Name is not null && Name.Length > 0))
        {
            throw new PSArgumentException("You cannot specify -Recurse and -Name at the same time.");
        }

        var drivesFolders = SessionState.EnumFolders(Path, false, 0, true);
        var wpName = Name.ConvertToWildcardPatternList();
        var sources = ResolveSources();

        using var cancelHandler = new ConsoleCancelHandler();

        foreach (var (drive, folder) in drivesFolders)
        {
            foreach (var (filePath, dir, bucketName) in sources)
            {
                //WriteObject($"'{filePath}' '{dir}' '{bucketName}'");
                //continue;

                // -Recurse が指定されている場合に限り、dir を考慮する
                string targetFolderPath;
                if (Recurse)
                {
                    targetFolderPath =
                        string.IsNullOrEmpty(folder.FullyQualifiedName) ? dir :
                        string.IsNullOrEmpty(dir) ? folder.FullyQualifiedName :
                        $"{folder.FullyQualifiedName}/{dir}";
                }
                else
                {
                    targetFolderPath = folder.FullyQualifiedName;
                }

                if (string.IsNullOrEmpty(targetFolderPath))
                {
                    WriteWarning($"'{filePath}': -Name is required for file sources. Skipping.");
                    continue;
                }

                Folder targetFolder = drive.GetFolder(targetFolderPath);
                if (targetFolder is null)
                {
                    WriteWarning($"'{filePath}': Folder '{drive.NameColon}/{targetFolderPath}' not found. Skipping.");
                    continue;
                }

                var buckets = drive.Buckets.Get(targetFolder);

                IEnumerable<Bucket> targetBuckets = null;

                if (wpName is not null)
                {
                    // -Name が指定されていなければ、それを使う
                    targetBuckets = buckets
                        .FilterByWildcards(b => b!.Name, wpName)
                        .OrderBy(b => b.Name);
                    if (!targetBuckets.Any())
                    {
                        WriteWarning($"'{filePath}': No buckets matched under '{targetFolder.GetPSPath()}'. Skipping.");
                        continue;
                    }
                }
                else
                {
                    // -Name が指定されていなければ、ローカルフォルダから抽出

                    // 1) 大小無視の完全一致を優先
                    var bucket = buckets.FirstOrDefault(b =>
                        string.Equals(b.Name, bucketName, StringComparison.OrdinalIgnoreCase));

                    if (bucket is not null)
                    {
                        targetBuckets = [bucket];
                    }
                    else
                    {
                        // 2) 置換エクスポート対策: '_' を 1文字ワイルドカードとして解釈
                        var bucketWildcard = bucketName.Replace('_', '?');
                        var wp = new WildcardPattern(bucketWildcard,
                            WildcardOptions.IgnoreCase | WildcardOptions.CultureInvariant);

                        // 0/1/複数 を一度の列挙で判定
                        var matches = buckets.Where(b => wp.IsMatch(b.Name)).Take(2).ToList();

                        if (matches.Count == 0)
                        {
                            WriteWarning($"'{filePath}': Bucket '{bucketName}' under '{targetFolder.GetPSPath()}' not found. Skipping.");
                            continue;
                        }
                        if (matches.Count > 1)
                        {
                            WriteWarning($"'{filePath}': Bucket pattern '{bucketName}' matched multiple buckets under '{targetFolder.GetPSPath()}'. Skipping.");
                            continue;
                        }

                        targetBuckets = [matches[0]];
                    }
                }

                foreach (var targetBucket in targetBuckets ?? [])
                {
                    // targetFolder にある bucket に、filePath をアップロードする
                    if (ShouldProcess($"Item: '{filePath}' Folder: '{targetFolder.GetPSPath()}' Bucket: '{targetBucket.Name}'", "Import BucketItem"))
                    {
                        var access = drive.OrchAPISession.GetBucketWriteUri(targetFolder.Id!.Value, targetBucket.Id!.Value, System.IO.Path.GetFileName(filePath));
                        drive.OrchAPISession.WriteBucketItem(access!, filePath);
                        //WriteObject(access);
                    }
                }
            }
        }
    }
}
