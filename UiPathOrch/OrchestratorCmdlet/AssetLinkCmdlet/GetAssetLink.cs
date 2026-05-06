using System.Collections.Concurrent;
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

    // Discriminated union for the producer/consumer queue: workers push either a
    // resolved AssetLink row or a per-folder failure; the main thread drains the
    // queue and dispatches each item to WriteObject / WriteError.
    private abstract record EmitItem;
    private sealed record EmitOk(AssetLink Link) : EmitItem;
    private sealed record EmitError(string Path, object Target, Exception Exception) : EmitItem;

    // SimpleFolder has no GetPSPath() — it's the value type returned by the
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

        // Streaming pipeline. The OrchThreadPool pattern used elsewhere yields
        // results in input order, which means a slow folder #1 gates emission of
        // the already-completed folders behind it. -Recurse over a deep tree
        // makes that very visible. Producer/consumer with BlockingCollection
        // emits each row as soon as a worker resolves it, so output trickles in
        // completion order — at the cost of non-deterministic ordering across
        // folders, which the user can re-impose with `| Sort-Object` if needed.
        using var sem = new SemaphoreSlim(4);
        using var queue = new BlockingCollection<EmitItem>();

        // Thread-safe dedup so two workers that encounter the same link from
        // different vantage points don't race to push the same row twice.
        var emitted = new ConcurrentDictionary<(Int64 srcFolderId, Int64 assetId, Int64 linkFolderId), byte>();

        var workers = drivesFolders.Select(df => Task.Run(() =>
        {
            try
            {
                sem.Wait(token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            try
            {
                ProcessFolder(df, wpName, emitted, queue, token);
            }
            catch (OperationCanceledException)
            {
                // graceful: the consumer side has already exited via cancel
            }
            catch (Exception ex)
            {
                // Per-folder failure routed to the main thread's WriteError.
                if (!queue.IsAddingCompleted)
                {
                    queue.Add(new EmitError(df.folder.GetPSPath(), df.folder, ex));
                }
            }
            finally
            {
                sem.Release();
            }
        })).ToArray();

        // Background completion signaller: once all workers have settled, mark
        // the queue done so GetConsumingEnumerable returns naturally.
        _ = Task.Run(() =>
        {
            try { Task.WaitAll(workers); }
            catch { /* per-worker exceptions are already in the queue */ }
            queue.CompleteAdding();
        });

        // Main thread: drain and emit. WriteObject and WriteError must be called
        // here, not from worker threads — the PowerShell pipeline is not thread-safe.
        try
        {
            foreach (var item in queue.GetConsumingEnumerable(token))
            {
                switch (item)
                {
                    case EmitOk ok:
                        WriteObject(ok.Link);
                        break;
                    case EmitError err:
                        WriteError(new ErrorRecord(
                            new OrchException(err.Path, err.Exception),
                            "GetAssetLinkError",
                            ErrorCategory.InvalidOperation,
                            err.Target));
                        break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Ctrl+C: workers still running in background will short-circuit on
            // the next token check. Nothing to emit further.
        }
    }

    private static void ProcessFolder(
        (OrchDriveInfo drive, Folder folder) df,
        IReadOnlyList<WildcardPattern>? wpName,
        ConcurrentDictionary<(Int64, Int64, Int64), byte> emitted,
        BlockingCollection<EmitItem> queue,
        CancellationToken token)
    {
        var (drive, folder) = df;
        var assets = drive.Assets.Get(folder);
        Int64 srcId = folder.Id ?? 0;
        string sourcePath = folder.GetPSPath();

        foreach (var asset in assets
            .FilterByWildcards(a => a?.Name, wpName)
            .OrderBy(a => a.Name))
        {
            token.ThrowIfCancellationRequested();

            var accessible = drive.GetFoldersForAsset(folder, asset);
            if (accessible?.AccessibleFolders is not { Length: > 1 }) continue;

            Int64 assetId = asset.Id ?? 0;

            foreach (var linkFolder in accessible.AccessibleFolders.OrderBy(f => f.FullyQualifiedName))
            {
                Int64 linkId = linkFolder.Id ?? 0;
                if (linkId == srcId) continue;
                if (!emitted.TryAdd((srcId, assetId, linkId), 0)) continue;

                if (queue.IsAddingCompleted) return;
                queue.Add(new EmitOk(new AssetLink
                {
                    Path = sourcePath,
                    Name = asset.Name,
                    Link = LinkFolderPSPath(drive, linkFolder),
                    AssetId = assetId,
                    FolderId = srcId,
                    LinkFolderId = linkId,
                }), token);
            }
        }
    }
}
