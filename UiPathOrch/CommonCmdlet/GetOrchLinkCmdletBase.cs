using System.Management.Automation;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Generic base for the Get-Orch{Asset,Bucket,Queue}Link trio. The streaming
// algorithm — semaphore-capped per-folder chains (Phase 1 entity list,
// Phase 2 fan-out per-entity GetFoldersFor* calls) with a fixed lookahead
// window so consumer-side drain overlaps producer-side work — was previously
// duplicated across three files. Concrete cmdlets supply the per-entity
// primitives and the link-DTO constructor; the base owns the algorithm.
//
// Cancellation: Ctrl+C is signalled into ConsoleCancelHandler.Token. We
// Wait(token) on each folder task instead of GetAwaiter().GetResult() so
// the main thread bails promptly. In-flight workers may still complete
// their current synchronous API call (no token plumbed through OrchAPISession),
// but new sem.WaitAsync(token) calls bail — and per-key cache atomicity in
// OrchDriveInfo (the link cache is set only after a full successful response)
// keeps the cache clean either way.
//
// Derived classes must:
//   - Decorate the class with [Cmdlet(VerbsCommon.Get, "OrchXxxLink")] and
//     [OutputType(typeof(XxxLink))]
//   - Override Name to attach [ArgumentCompleter(typeof(LinkedXxxNameCompleter))] —
//     the [Parameter]/[SupportsWildcards] attributes must be re-declared on
//     the override (matches the RemoveDriveEntityCmdletBase convention).
//   - Implement the abstract members below.
public abstract class GetOrchLinkCmdletBase<TEntity, TLink> : OrchestratorPSCmdlet
    where TEntity : class
    where TLink : class
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public virtual string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    private const int MaxDegreeOfParallelism = 4;
    private const int FolderLookaheadWindow = 4;

    // Stable identifier emitted on WriteError, e.g. "GetAssetLinkError".
    protected abstract string ErrorId { get; }

    protected abstract ICollection<TEntity> GetEntities(OrchDriveInfo drive, Folder folder);
    protected abstract string? GetEntityName(TEntity? entity);
    protected abstract long GetEntityId(TEntity entity);
    protected abstract AccessibleFoldersDto? GetFoldersForEntity(OrchDriveInfo drive, Folder srcFolder, TEntity entity);
    protected abstract TLink BuildLink(string srcPath, TEntity entity, string linkFolderPath, long srcFolderId, long linkFolderId);

    private record FolderRow(TEntity Entity, AccessibleFoldersDto? Accessible);

    // SimpleFolder has no GetPSPath() — it's a value type returned by the
    // GetFoldersFor* family. Build the PSPath as drive prefix + FullyQualifiedName,
    // mirroring Folder.GetPSPath()'s output.
    private static string LinkFolderPSPath(OrchDriveInfo drive, SimpleFolder linkFolder)
    {
        var fqn = (linkFolder.FullyQualifiedName ?? "").Replace('/', System.IO.Path.DirectorySeparatorChar);
        return drive.NameColonSeparator + fqn;
    }

    protected sealed override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth).ToList();
        var wpName = Name.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        var token = cancelHandler.Token;

        // Single semaphore caps every API round-trip (entity list and
        // GetFoldersFor* alike) at MaxDegreeOfParallelism — same as a
        // typical OrchThreadPool inner cap.
        using var sem = new SemaphoreSlim(MaxDegreeOfParallelism);

        var folderTasks = new Task<FolderRow[]>?[drivesFolders.Count];

        // Per-folder chain: Phase 1 (entity list) → fan-out Phase 2
        // (GetFoldersFor* per entity). Each await on the semaphore takes
        // one slot, so a folder with N matching entities uses 1+N slot
        // acquisitions total, all subject to the global cap.
        Func<int, Task<FolderRow[]>> startFolder = idx => Task.Run(async () =>
        {
            var (drive, folder) = drivesFolders[idx];

            await sem.WaitAsync(token);
            List<TEntity> entities;
            try
            {
                entities = GetEntities(drive, folder)
                    .FilterByWildcards(e => GetEntityName(e), wpName)
                    .OrderBy(e => GetEntityName(e))
                    .ToList();
            }
            finally { sem.Release(); }

            return await Task.WhenAll(entities.Select(entity => Task.Run<FolderRow>(async () =>
            {
                await sem.WaitAsync(token);
                try
                {
                    return new FolderRow(entity, GetFoldersForEntity(drive, folder, entity));
                }
                finally { sem.Release(); }
            })));
        });

        // Prefill the initial lookahead window so the first folders are
        // already racing while the main thread sets up the consumer loop.
        for (int i = 0; i < Math.Min(FolderLookaheadWindow, folderTasks.Length); i++)
        {
            folderTasks[i] = startFolder(i);
        }

        var emitted = new HashSet<(long srcFolderId, long entityId, long linkFolderId)>();

        for (int i = 0; i < drivesFolders.Count; i++)
        {
            // Throw rather than swallow the cancel — OperationCanceledException
            // propagates to PowerShell which surfaces the standard "Operation
            // was canceled" message, matching how GetBucket and friends behave.
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
                folderTasks[i]!.Wait(token);
                rows = folderTasks[i]!.Result;
            }
            catch (AggregateException aex) when (aex.InnerException is not null)
            {
                string target = folder.GetPSPath();
                WriteError(new ErrorRecord(new OrchException(target, aex.InnerException), ErrorId,
                    ErrorCategory.InvalidOperation, target));
                continue;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                string target = folder.GetPSPath();
                WriteError(new ErrorRecord(new OrchException(target, ex), ErrorId,
                    ErrorCategory.InvalidOperation, target));
                continue;
            }

            long srcId = folder.Id ?? 0;
            string sourcePath = folder.GetPSPath();

            foreach (var row in rows)
            {
                token.ThrowIfCancellationRequested();
                if (row.Accessible?.AccessibleFolders is not { Length: > 1 }) continue;

                long entityId = GetEntityId(row.Entity);

                foreach (var linkFolder in row.Accessible.AccessibleFolders.OrderBy(f => f.FullyQualifiedName))
                {
                    token.ThrowIfCancellationRequested();
                    long linkId = linkFolder.Id ?? 0;
                    if (linkId == srcId) continue;
                    if (!emitted.Add((srcId, entityId, linkId))) continue;

                    WriteObject(BuildLink(sourcePath, row.Entity, LinkFolderPSPath(drive, linkFolder), srcId, linkId));
                }
            }
        }
    }
}
