using System.Management.Automation;
using UiPath.OrchAPI;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Generic base for the Move-Orch{Asset,Bucket,Queue} trio.
//
// Moves an entity from its source folder to a destination folder WITHIN THE
// SAME TENANT DRIVE. An Asset / Bucket / Queue is a single tenant-level entity
// surfaced into one or more folders via the Share*ToFolders endpoint (the same
// endpoint Add-/Remove-Orch*Link drive). Verified on a live tenant: a single
// ShareToFolders call carrying toAdd=[destination] AND toRemove=[source] in one
// request atomically relocates the entity — it disappears from the source
// folder, stays a first-class editable entity in the destination, keeps its Id,
// and carries its data with it (asset value, queue items, bucket Identifier /
// blobs). No copy is made; this is a true move of the one shared entity.
//
// Because the API takes toAdd and toRemove in one request there is no
// add-then-remove window: either the whole relocation applies or none of it
// does (the server validates create permission on the destination and the
// source unlink together). That removes the "added to dst but still in src"
// half-state a two-call approach would risk.
//
// Same-drive only: the Share endpoint cannot move an entity across tenant
// drives, so a -Destination on a different drive is reported as an error
// rather than silently doing nothing. A -Destination equal to the source
// folder is a no-op (the entity is already there).
//
// Pipeline / batching: like the link cmdlets, input is buffered across
// ProcessRecord and coalesced in EndProcessing so multiple rows naming the same
// (source folder, entity) collapse into one Share call. Unlike Add-Orch*Link,
// the destination is a single folder per move (an entity has one home), so the
// destination is resolved to exactly one folder; multiple destinations for the
// same entity are an error (an entity can't be moved to two homes at once).
//
// Derived classes must:
//   - Decorate the class with
//     [Cmdlet(VerbsCommon.Move, "OrchXxx", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium)]
//   - Override Name to attach [ArgumentCompleter(typeof(XxxNameCompleter))] —
//     the [Parameter]/[SupportsWildcards] attributes must be re-declared on the
//     override (matches the link-cmdlet convention).
//   - Implement the abstract members below.
public abstract class MoveOrchEntityCmdletBase<TEntity> : OrchestratorPSCmdlet
    where TEntity : class
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public virtual string[]? Name { get; set; }

    // The destination folder the entity is moved INTO. Single folder per entity
    // (an entity has one home). Wildcards are allowed but must resolve to one
    // folder on the entity's drive.
    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public virtual string[]? Destination { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    // Stable identifier emitted on WriteError, e.g. "MoveAssetError".
    protected abstract string ErrorId { get; }

    // "Asset" / "Bucket" / "Queue" — appears in the action label.
    protected abstract string EntityNoun { get; }

    protected abstract ICollection<TEntity> GetEntities(OrchDriveInfo drive, Folder folder);
    protected abstract string? GetEntityName(TEntity? entity);
    protected abstract long GetEntityId(TEntity entity);
    protected abstract void Share(OrchAPISession api, long srcFolderId, List<long> entityIds, List<long> toAdd, List<long> toRemove);
    protected abstract void ClearLinkCache(OrchDriveInfo drive, long entityId);
    protected abstract void ClearPerFolderCache(OrchDriveInfo drive, Folder folder);

    private readonly List<(string[]? Name, string[]? Destination, string[]? Path)> _buffered = new();

    // One entity in one source folder, with its resolved destination folder.
    private sealed class MoveGroup
    {
        public required OrchDriveInfo Drive { get; init; }
        public required Folder SrcFolder { get; init; }
        public required long EntityId { get; init; }
        public required string EntityName { get; init; }
        public Folder? DstFolder { get; set; }
        public bool Ambiguous { get; set; }
    }

    protected sealed override void ProcessRecord()
    {
        _buffered.Add((Name, Destination, Path));
    }

    protected sealed override void EndProcessing()
    {
        if (_buffered.Count == 0) return;

        using var cancelHandler = new ConsoleCancelHandler();

        // Warm the per-folder entity caches for every source folder in parallel.
        var prefetchTargets = new HashSet<(OrchDriveInfo drive, Folder folder)>();
        foreach (var (_, _, paths) in _buffered)
        {
            foreach (var df in SessionState.EnumFolders(paths, Recurse.IsPresent, Depth))
            {
                prefetchTargets.Add(df);
            }
        }
        Parallel.ForEach(prefetchTargets, df =>
        {
            try { GetEntities(df.drive, df.folder); }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Move{EntityNoun} prefetch failed for '{df.folder.GetPSPath()}': {ex.Message}");
            }
        });

        // Resolve each buffered row into (drive, src folder, entity) groups,
        // pinning the single destination folder. Keyed by drive + src + entity
        // so the same entity across rows merges (and conflicting destinations
        // are flagged ambiguous).
        var groups = new Dictionary<(string Drive, long Src, long Entity), MoveGroup>();

        foreach (var (names, destinations, paths) in _buffered.WithCancellation(cancelHandler.Token))
        {
            var srcDrivesFolders = SessionState.EnumFolders(paths, Recurse.IsPresent, Depth).ToList();
            var destDrivesFolders = SessionState.EnumFolders(destinations).ToList();
            var wpName = names.ConvertToWildcardPatternList();

            foreach (var (drive, folder) in srcDrivesFolders)
            {
                ICollection<TEntity> entities;
                try
                {
                    entities = GetEntities(drive, folder);
                }
                catch (Exception ex)
                {
                    string targetErr = folder.GetPSPath();
                    WriteError(new ErrorRecord(new OrchException(targetErr, ex), ErrorId,
                        ErrorCategory.InvalidOperation, targetErr));
                    continue;
                }

                // The move is a same-drive relocation; a destination on another
                // drive can't be reached by ShareToFolders. Surface that rather
                // than silently dropping it.
                var sameDriveDests = destDrivesFolders.Where(d => d.drive == drive).ToList();
                var crossDriveDests = destDrivesFolders.Where(d => d.drive != drive).ToList();
                if (crossDriveDests.Count > 0)
                {
                    string src = folder.GetPSPath();
                    WriteError(new ErrorRecord(
                        new OrchException(src,
                            $"Move-Orch{EntityNoun} only moves within the same tenant drive. " +
                            $"Destination(s) [{string.Join(", ", crossDriveDests.Select(d => d.folder.GetPSPath()))}] " +
                            $"are on a different drive than the source '{src}'. Use Copy-Orch{EntityNoun} to copy across drives."),
                        ErrorId, ErrorCategory.InvalidArgument, src));
                }

                foreach (var entity in entities
                    .FilterByWildcards(e => GetEntityName(e), wpName)
                    .OrderBy(e => GetEntityName(e)).WithCancellation(cancelHandler.Token))
                {
                    long entityId = GetEntityId(entity);
                    var key = (drive.NameColonSeparator, folder.Id ?? 0, entityId);
                    if (!groups.TryGetValue(key, out var group))
                    {
                        group = new MoveGroup
                        {
                            Drive = drive,
                            SrcFolder = folder,
                            EntityId = entityId,
                            EntityName = GetEntityName(entity)!,
                        };
                        groups[key] = group;
                    }

                    foreach (var d in sameDriveDests)
                    {
                        long did = d.folder.Id ?? 0;
                        if (did == 0) continue;
                        if (group.DstFolder is null)
                        {
                            group.DstFolder = d.folder;
                        }
                        else if (group.DstFolder.Id != did)
                        {
                            // Two different destinations for the same entity —
                            // an entity has one home, so this is ambiguous.
                            group.Ambiguous = true;
                        }
                    }
                }
            }
        }

        // One Share call per group: toAdd=[dst], toRemove=[src], atomic.
        foreach (var group in groups.Values.WithCancellation(cancelHandler.Token))
        {
            string source = group.SrcFolder.GetPSPath();
            string target = System.IO.Path.Combine(source, group.EntityName);

            if (group.Ambiguous)
            {
                WriteError(new ErrorRecord(
                    new OrchException(target,
                        $"Cannot move '{group.EntityName}': -Destination resolved to more than one folder. " +
                        $"An {EntityNoun.ToLowerInvariant()} has a single home folder — specify exactly one destination."),
                    ErrorId, ErrorCategory.InvalidArgument, target));
                continue;
            }

            if (group.DstFolder is null) continue;             // no destination resolved on this drive
            if (group.DstFolder.Id == group.SrcFolder.Id) continue; // already there — no-op

            long srcId = group.SrcFolder.Id ?? 0;
            long dstId = group.DstFolder.Id ?? 0;
            string dst = group.DstFolder.GetPSPath();
            string action = $"Move {EntityNoun} → {dst}";

            if (!ShouldProcess(target, action)) continue;

            try
            {
                // Atomic relocate: add the destination link and remove the
                // source link in a single request. Operate from the source
                // folder so srcFolderId is the entity's current context.
                Share(group.Drive.OrchAPISession, srcId,
                    new List<long> { group.EntityId },
                    new List<long> { dstId },
                    new List<long> { srcId });

                // Both folders' entity lists and this entity's link set changed.
                ClearLinkCache(group.Drive, group.EntityId);
                ClearPerFolderCache(group.Drive, group.SrcFolder);
                ClearPerFolderCache(group.Drive, group.DstFolder);
            }
            catch (Exception ex)
            {
                // The relocate is atomic, so on failure nothing moved. Common
                // cause: the caller lacks create permission in the destination.
                var wrapped = new OrchException(target,
                    $"'{group.EntityName}' was not moved to '{dst}'. The relocation is atomic and was " +
                    $"rejected by the server (the entity remains in '{source}'). Server: {ex.Message}", ex);
                WriteError(new ErrorRecord(wrapped, ErrorId,
                    ErrorCategory.InvalidOperation, target));
            }
        }
    }
}
