using System.Management.Automation;
using UiPath.PowerShell.Positional;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsData.Import, "OrchBucketItem", SupportsShouldProcess = true)]
public class ImportBucketItemCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Source { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BucketNameCompleter<True>))]
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

        // Recurse=false -> depth 0. Recurse=true with Depth unspecified -> unlimited
        uint effDepth = !Recurse ? 0u
            : (MyInvocation.BoundParameters.ContainsKey(nameof(Depth)) ? Depth : uint.MaxValue);

        static string LastDirName(string path)
            => System.IO.Path.GetFileName(System.IO.Path.TrimEndingDirectorySeparator(path));

        // Use .NET 8 standard depth control
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
                    // Single file: destination is always empty, bucket is the nearest parent folder name
                    var f = System.IO.Path.GetFullPath(src);
                    if (!seen.Add(f)) continue;

                    var parent = System.IO.Path.GetDirectoryName(f)!;
                    var bucket = LastDirName(parent);
                    rows.Add((f, "", bucket));
                }
                else if (System.IO.Directory.Exists(src))
                {
                    // Folder: enumerate descendants using this folder as anchor (applying Depth/Recurse)
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

                        // Determine destination / bucket from the relative path to the anchor
                        var rel = System.IO.Path.GetRelativePath(anchor, f);          // e.g.: "sub\subsub\bucket\file.csv"
                        var relDir = System.IO.Path.GetDirectoryName(rel) ?? "";
                        var parts = relDir.Split(dirSeps, StringSplitOptions.RemoveEmptyEntries);

                        string bucket, dest;
                        if (parts.Length == 0)
                        {
                            bucket = LastDirName(anchor);   // Direct child file: bucket is the anchor name
                            dest = "";
                        }
                        else
                        {
                            bucket = parts[^1];             // Use the nearest parent as bucket
                            dest = parts.Length > 1       // Use preceding parts as destination (joined with '/')
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

        // Stable output order: destination -> bucket -> file name -> full path
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

                // Only consider dir when -Recurse is specified
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

                List<Bucket>? buckets = null;
                try
                {
                    buckets = drive.Buckets.Get(targetFolder);
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "ImportBucketItemError", ErrorCategory.InvalidOperation, folder));
                    continue;
                }

                IEnumerable<Bucket> targetBuckets = null;

                if (wpName is not null)
                {
                    // If -Name is specified, use it
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
                    // If -Name is not specified, extract from local folder

                    // 1) Prioritize case-insensitive exact match
                    var bucket = buckets.FirstOrDefault(b =>
                        string.Equals(b.Name, bucketName, StringComparison.OrdinalIgnoreCase));

                    if (bucket is not null)
                    {
                        targetBuckets = [bucket];
                    }
                    else
                    {
                        // 2) Handle character replacement from export: interpret '_' as single-character wildcard
                        var bucketWildcard = bucketName.Replace('_', '?');
                        var wp = new WildcardPattern(bucketWildcard,
                            WildcardOptions.IgnoreCase | WildcardOptions.CultureInvariant);

                        // Determine 0/1/multiple matches in a single enumeration
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
                    cancelHandler.Token.ThrowIfCancellationRequested();

                    // Upload filePath to the bucket in targetFolder
                    string target = $"Item: '{filePath}' Folder: '{targetFolder.GetPSPath()}' Bucket: '{targetBucket.Name}'";
                    if (ShouldProcess(target, "Import BucketItem"))
                    {
                        try
                        {
                            var access = drive.OrchAPISession.GetBucketWriteUri(targetFolder.Id!.Value, targetBucket.Id!.Value, System.IO.Path.GetFileName(filePath));
                            drive.OrchAPISession.WriteBucketItem(access!, filePath, cancelHandler.Token);
                            drive.BucketFiles.ClearCache(folder, targetBucket.Id.Value);
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(target, ex), "ImportBucketItemError", ErrorCategory.InvalidOperation, targetBucket));
                        }
                    }
                }
            }
        }
    }
}
