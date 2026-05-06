using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchBucketLink")]
[OutputType(typeof(BucketLink))]
public class GetBucketLinkCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BucketNameCompleter<False>))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    private const int MaxDegreeOfParallelism = 4;
    private const int FolderLookaheadWindow = 4;

    private record FolderRow(Bucket Bucket, AccessibleFoldersDto? Accessible);

    // SimpleFolder has no GetPSPath() — it's a value type returned by the
    // GetFoldersFor* family. Build the PSPath as drive prefix + FullyQualifiedName,
    // mirroring Folder.GetPSPath()'s output.
    private static string LinkFolderPSPath(OrchDriveInfo drive, SimpleFolder linkFolder)
    {
        var fqn = (linkFolder.FullyQualifiedName ?? "").Replace('/', System.IO.Path.DirectorySeparatorChar);
        return drive.NameColonSeparator + fqn;
    }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth).ToList();
        var wpName = Name.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        var token = cancelHandler.Token;

        // Same streaming pattern as Get-OrchAssetLink: each folder's chain is
        // self-contained (Phase 1 = Buckets.Get → Phase 2 = parallel
        // GetFoldersForBucket per bucket), and a fixed lookahead window
        // pre-launches the next folders' chains so consumer-side drain
        // overlaps producer-side work. A single semaphore caps total
        // in-flight API calls at MaxDegreeOfParallelism.
        using var sem = new SemaphoreSlim(MaxDegreeOfParallelism);

        var folderTasks = new Task<FolderRow[]>?[drivesFolders.Count];

        Func<int, Task<FolderRow[]>> startFolder = idx => Task.Run(async () =>
        {
            var (drive, folder) = drivesFolders[idx];

            await sem.WaitAsync(token);
            List<Bucket> buckets;
            try
            {
                buckets = drive.Buckets.Get(folder)
                    .FilterByWildcards(b => b?.Name, wpName)
                    .OrderBy(b => b.Name)
                    .ToList();
            }
            finally { sem.Release(); }

            return await Task.WhenAll(buckets.Select(bucket => Task.Run<FolderRow>(async () =>
            {
                await sem.WaitAsync(token);
                try
                {
                    return new FolderRow(bucket, drive.GetFoldersForBucket(folder, bucket));
                }
                finally { sem.Release(); }
            })));
        });

        for (int i = 0; i < Math.Min(FolderLookaheadWindow, folderTasks.Length); i++)
        {
            folderTasks[i] = startFolder(i);
        }

        var emitted = new HashSet<(Int64 srcFolderId, Int64 bucketId, Int64 linkFolderId)>();

        for (int i = 0; i < drivesFolders.Count; i++)
        {
            // Throw rather than swallow the cancel — OperationCanceledException
            // propagates to PowerShell which surfaces the standard "Operation
            // was canceled" message. See GetAssetLink for the rationale.
            token.ThrowIfCancellationRequested();

            int next = i + FolderLookaheadWindow;
            if (next < folderTasks.Length && folderTasks[next] is null)
            {
                folderTasks[next] = startFolder(next);
            }

            var (drive, folder) = drivesFolders[i];
            FolderRow[] rows;
            try
            {
                // Wait(token) instead of GetAwaiter().GetResult() so Ctrl+C
                // bails out of the wait promptly. In-flight workers still
                // complete their current API call (no token plumbed through
                // OrchAPISession), but new sem.WaitAsync(token) calls bail —
                // and per-key cache atomicity (_dicBucketLinks set only after
                // a full successful response) keeps the cache clean either way.
                folderTasks[i]!.Wait(token);
                rows = folderTasks[i]!.Result;
            }
            catch (AggregateException aex) when (aex.InnerException is not null)
            {
                string target = folder.GetPSPath();
                WriteError(new ErrorRecord(new OrchException(target, aex.InnerException), "GetBucketLinkError",
                    ErrorCategory.InvalidOperation, target));
                continue;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                string target = folder.GetPSPath();
                WriteError(new ErrorRecord(new OrchException(target, ex), "GetBucketLinkError",
                    ErrorCategory.InvalidOperation, target));
                continue;
            }

            Int64 srcId = folder.Id ?? 0;
            string sourcePath = folder.GetPSPath();

            foreach (var row in rows)
            {
                token.ThrowIfCancellationRequested();
                if (row.Accessible?.AccessibleFolders is not { Length: > 1 }) continue;

                Int64 bucketId = row.Bucket.Id ?? 0;

                foreach (var linkFolder in row.Accessible.AccessibleFolders.OrderBy(f => f.FullyQualifiedName))
                {
                    token.ThrowIfCancellationRequested();
                    Int64 linkId = linkFolder.Id ?? 0;
                    if (linkId == srcId) continue;
                    if (!emitted.Add((srcId, bucketId, linkId))) continue;

                    WriteObject(new BucketLink
                    {
                        Path = sourcePath,
                        Name = row.Bucket.Name,
                        Link = LinkFolderPSPath(drive, linkFolder),
                        BucketId = bucketId,
                        FolderId = srcId,
                        LinkFolderId = linkId,
                    });
                }
            }
        }
    }
}
