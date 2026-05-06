using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchAssetLink")]
[OutputType(typeof(AssetLink))]
public class GetAssetLinkCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(AssetNameCompleter))]
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

    private record FolderRow(Asset Asset, AccessibleFoldersDto? Accessible);

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

        // Single semaphore caps every API round-trip (Assets.Get and
        // GetFoldersForAsset alike) at MaxDegreeOfParallelism — same as a
        // typical OrchThreadPool inner cap. Folder lookahead launches the
        // next window's chains as the consumer advances, so by the time
        // main thread awaits folder[i], folders [i+1..i+window-1] are
        // already partway through their work in background.
        using var sem = new SemaphoreSlim(MaxDegreeOfParallelism);

        var folderTasks = new Task<FolderRow[]>?[drivesFolders.Count];

        // Per-folder chain: Phase 1 (Assets.Get) → fan-out Phase 2
        // (GetFoldersForAsset per asset). Each await on the semaphore
        // takes one slot, so a folder with N matching assets uses 1+N
        // slot-acquisitions total, all subject to the global cap.
        Func<int, Task<FolderRow[]>> startFolder = idx => Task.Run(async () =>
        {
            var (drive, folder) = drivesFolders[idx];

            await sem.WaitAsync(token);
            List<Asset> assets;
            try
            {
                assets = drive.Assets.Get(folder)
                    .FilterByWildcards(a => a?.Name, wpName)
                    .OrderBy(a => a.Name)
                    .ToList();
            }
            finally { sem.Release(); }

            return await Task.WhenAll(assets.Select(asset => Task.Run<FolderRow>(async () =>
            {
                await sem.WaitAsync(token);
                try
                {
                    return new FolderRow(asset, drive.GetFoldersForAsset(folder, asset));
                }
                finally { sem.Release(); }
            })));
        });

        // Prefill the initial lookahead window so the first folders are
        // already racing while main thread sets up the consumer loop.
        for (int i = 0; i < Math.Min(FolderLookaheadWindow, folderTasks.Length); i++)
        {
            folderTasks[i] = startFolder(i);
        }

        var emitted = new HashSet<(Int64 srcFolderId, Int64 assetId, Int64 linkFolderId)>();

        for (int i = 0; i < drivesFolders.Count; i++)
        {
            // Throw rather than swallow the cancel — OperationCanceledException
            // propagates to PowerShell which surfaces the standard "Operation
            // was canceled" message, matching how GetBucket and friends behave.
            // Silently returning would leave the user wondering whether the
            // cmdlet actually ran or just stopped early.
            token.ThrowIfCancellationRequested();

            // Slide the lookahead window forward: as we begin draining
            // folder[i], queue up folder[i + window] so it can start
            // racing in the background while we emit folder[i]'s rows.
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
                // (signaled into ConsoleCancelHandler.Token) bails out of the
                // wait promptly with OperationCanceledException — propagated
                // to PowerShell uncaught. In-flight workers may still be
                // inside a synchronous API call — those run to completion
                // since Assets.Get / GetFoldersForAsset don't accept a token —
                // but the main thread exits without queueing more work.
                folderTasks[i]!.Wait(token);
                rows = folderTasks[i]!.Result;
            }
            catch (AggregateException aex) when (aex.InnerException is not null)
            {
                string target = folder.GetPSPath();
                WriteError(new ErrorRecord(new OrchException(target, aex.InnerException), "GetAssetLinkError",
                    ErrorCategory.InvalidOperation, target));
                continue;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                string target = folder.GetPSPath();
                WriteError(new ErrorRecord(new OrchException(target, ex), "GetAssetLinkError",
                    ErrorCategory.InvalidOperation, target));
                continue;
            }

            Int64 srcId = folder.Id ?? 0;
            string sourcePath = folder.GetPSPath();

            foreach (var row in rows)
            {
                token.ThrowIfCancellationRequested();
                if (row.Accessible?.AccessibleFolders is not { Length: > 1 }) continue;

                Int64 assetId = row.Asset.Id ?? 0;

                foreach (var linkFolder in row.Accessible.AccessibleFolders.OrderBy(f => f.FullyQualifiedName))
                {
                    token.ThrowIfCancellationRequested();
                    Int64 linkId = linkFolder.Id ?? 0;
                    if (linkId == srcId) continue;
                    if (!emitted.Add((srcId, assetId, linkId))) continue;

                    WriteObject(new AssetLink
                    {
                        Path = sourcePath,
                        Name = row.Asset.Name,
                        Link = LinkFolderPSPath(drive, linkFolder),
                        AssetId = assetId,
                        FolderId = srcId,
                        LinkFolderId = linkId,
                    });
                }
            }
        }
    }
}
