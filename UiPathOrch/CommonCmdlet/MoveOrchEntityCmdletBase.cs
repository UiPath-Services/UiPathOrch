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

    // The destination folder the entity is moved INTO. A single folder, because
    // an entity has one home — so this is a scalar, not an array: a comma-
    // separated list is rejected at bind time rather than at run time. A
    // wildcard is accepted, but it must expand to exactly one folder on the
    // entity's drive; expanding to more than one is an error.
    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public virtual string? Destination { get; set; }

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

    private readonly List<(string[]? Name, string? Destination, string[]? Path)> _buffered = new();

    // One entity in one source folder.
    private sealed class MoveGroup
    {
        public required OrchDriveInfo Drive { get; init; }
        public required Folder SrcFolder { get; init; }
        public required long EntityId { get; init; }
        public required string EntityName { get; init; }
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

        // -Destination is a single command-line value (a scalar that may use a
        // wildcard); on a pipeline every row carries the same one. Resolve it
        // once, up front. It must expand to exactly one folder: zero folders or
        // more than one is a hard error for the whole invocation (an entity has
        // one home, so there's nothing sensible to do with 0 or 2+ targets).
        var destination = _buffered.Select(b => b.Destination).FirstOrDefault(d => !string.IsNullOrEmpty(d));
        var destFolders = SessionState.EnumFolders(destination is null ? null : new[] { destination }).ToList();
        if (destFolders.Count == 0)
        {
            WriteError(new ErrorRecord(
                new ItemNotFoundException($"-Destination '{destination}' did not resolve to any folder."),
                ErrorId, ErrorCategory.ObjectNotFound, destination));
            return;
        }
        if (destFolders.Count > 1)
        {
            WriteError(new ErrorRecord(
                new OrchException(destination ?? "",
                    $"-Destination '{destination}' resolved to {destFolders.Count} folders " +
                    $"[{string.Join(", ", destFolders.Select(d => d.folder.GetPSPath()))}]. " +
                    $"An {EntityNoun.ToLowerInvariant()} has a single home folder — specify a destination that matches exactly one."),
                ErrorId, ErrorCategory.InvalidArgument, destination));
            return;
        }
        var (destDrive, destFolder) = destFolders[0];

        // Resolve each buffered row into (drive, src folder, entity) groups.
        // Keyed by drive + src + entity so the same entity across rows merges.
        var groups = new Dictionary<(string Drive, long Src, long Entity), MoveGroup>();

        foreach (var (names, _, paths) in _buffered.WithCancellation(cancelHandler.Token))
        {
            var srcDrivesFolders = SessionState.EnumFolders(paths, Recurse.IsPresent, Depth).ToList();
            var wpName = names.ConvertToWildcardPatternList();

            foreach (var (drive, folder) in srcDrivesFolders)
            {
                // The move is a same-drive relocation; ShareToFolders can't reach
                // a destination on a different drive than the source.
                if (drive != destDrive)
                {
                    string src = folder.GetPSPath();
                    WriteError(new ErrorRecord(
                        new OrchException(src,
                            $"Move-Orch{EntityNoun} only moves within the same tenant drive. " +
                            $"Destination '{destFolder.GetPSPath()}' is on a different drive than the source '{src}'. " +
                            $"Use Copy-Orch{EntityNoun} to copy across drives."),
                        ErrorId, ErrorCategory.InvalidArgument, src));
                    continue;
                }

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

                foreach (var entity in entities
                    .FilterByWildcards(e => GetEntityName(e), wpName)
                    .OrderBy(e => GetEntityName(e)).WithCancellation(cancelHandler.Token))
                {
                    long entityId = GetEntityId(entity);
                    var key = (drive.NameColonSeparator, folder.Id ?? 0, entityId);
                    if (!groups.ContainsKey(key))
                    {
                        groups[key] = new MoveGroup
                        {
                            Drive = drive,
                            SrcFolder = folder,
                            EntityId = entityId,
                            EntityName = GetEntityName(entity)!,
                        };
                    }
                }
            }
        }

        long dstId = destFolder.Id ?? 0;
        string dst = destFolder.GetPSPath();

        // One Share call per group: toAdd=[dst], toRemove=[src], atomic.
        foreach (var group in groups.Values.WithCancellation(cancelHandler.Token))
        {
            string source = group.SrcFolder.GetPSPath();
            string target = System.IO.Path.Combine(source, group.EntityName);

            if (group.SrcFolder.Id == dstId) continue; // already there — no-op

            long srcId = group.SrcFolder.Id ?? 0;
            string action = $"Move {EntityNoun} to {dst}";

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
                ClearPerFolderCache(group.Drive, destFolder);
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
