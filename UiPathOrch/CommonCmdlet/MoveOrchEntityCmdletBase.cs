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
// -Recurse mirrors the source tree (robocopy /MOVE /E semantics, the same
// shape the FileSystem provider gives a recursive move): an entity in a source
// subfolder lands in the matching subfolder under -Destination, not flattened
// into the destination root. The destination subfolder is computed from the
// entity's source-folder path relative to the source root and is created if it
// doesn't exist yet (a plain modern folder with no package feed — "Processes" —
// matching New-Item and Copy-Item's sub-folder convention; feeds are a
// root-folder concept and Move only ever creates sub-folders under the
// existing destination root). Folder creation honors -WhatIf.
//
// Pipeline / batching: like the link cmdlets, input is buffered across
// ProcessRecord and coalesced in EndProcessing so multiple rows naming the same
// (source folder, entity) collapse into one Share call. Unlike Add-Orch*Link,
// the destination is a single folder per move (an entity has one home), so the
// destination is resolved to exactly one folder; multiple destinations are an
// error (an entity can't be moved to two homes at once).
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
        var (destDrive, destRootFolder) = destFolders[0];

        // Cache of destination subfolders already resolved/created this run,
        // keyed by their FullyQualifiedName, so a tree with many entities under
        // the same subfolder creates it once.
        var dstFolderByFqn = new Dictionary<string, Folder?>(StringComparer.OrdinalIgnoreCase);

        // Entities already moved this run, so a duplicate (same entity reached
        // via overlapping -Path arguments) isn't moved twice.
        var movedKeys = new HashSet<(string Drive, long Src, long Entity)>();

        // Process in a single pass so folder creation and entity moves interleave
        // in execution order: a destination subfolder is created right before the
        // first entity that moves into it (not all up front). EnumFolders returns
        // source folders shallow-first, so the root folder's entities move before
        // a subfolder is created for the subfolder's entities.
        foreach (var (names, _, paths) in _buffered.WithCancellation(cancelHandler.Token))
        {
            // The source root anchors the relative path that is mirrored under
            // -Destination. Resolve -Path to a single root (wildcards allowed,
            // but to one folder) the same way -Destination is resolved.
            OrchDriveInfo srcDrive;
            Folder srcRootFolder;
            try
            {
                (srcDrive, srcRootFolder) = SessionState.ResolveToSingleFolder(paths is { Length: > 0 } ? paths[0] : null);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(string.Join(", ", paths ?? []), ex),
                    ErrorId, ErrorCategory.InvalidArgument, paths));
                continue;
            }

            // Same-drive relocation only; ShareToFolders can't cross drives.
            if (srcDrive != destDrive)
            {
                string src = srcRootFolder.GetPSPath();
                WriteError(new ErrorRecord(
                    new OrchException(src,
                        $"Move-Orch{EntityNoun} only moves within the same tenant drive. " +
                        $"Destination '{destRootFolder.GetPSPath()}' is on a different drive than the source '{src}'. " +
                        $"Use Copy-Orch{EntityNoun} to copy across drives."),
                    ErrorId, ErrorCategory.InvalidArgument, src));
                continue;
            }

            var srcDrivesFolders = SessionState.EnumFolders(paths, Recurse.IsPresent, Depth).ToList();
            var wpName = names.ConvertToWildcardPatternList();

            foreach (var (drive, folder) in srcDrivesFolders.WithCancellation(cancelHandler.Token))
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

                var matched = entities.FilterByWildcards(e => GetEntityName(e), wpName)
                    .OrderBy(e => GetEntityName(e)).ToList();
                if (matched.Count == 0) continue;

                long srcId = folder.Id ?? 0;
                string source = folder.GetPSPath();

                foreach (var entity in matched.WithCancellation(cancelHandler.Token))
                {
                    long entityId = GetEntityId(entity);
                    if (!movedKeys.Add((drive.NameColonSeparator, srcId, entityId))) continue;

                    string entityName = GetEntityName(entity)!;
                    string target = System.IO.Path.Combine(source, entityName);

                    // Resolve (creating as needed, just before the move) the
                    // destination subfolder mirroring this source folder. null
                    // means it couldn't be ensured — error already written, or
                    // -WhatIf returned a placeholder declined deeper up.
                    Folder? dstFolder = ResolveMirroredDestination(drive, srcRootFolder, folder, destRootFolder, dstFolderByFqn);
                    if (dstFolder is null) continue;

                    long dstId = dstFolder.Id ?? 0;
                    if (srcId == dstId) continue; // already there — no-op
                    string dst = dstFolder.GetPSPath();

                    if (!ShouldProcess(target, $"Move {EntityNoun} to {dst}")) continue;

                    try
                    {
                        // Atomic relocate: add the destination link and remove the
                        // source link in a single request. Operate from the source
                        // folder so srcFolderId is the entity's current context.
                        Share(drive.OrchAPISession, srcId,
                            new List<long> { entityId },
                            new List<long> { dstId },
                            new List<long> { srcId });

                        // Both folders' entity lists and this entity's link set changed.
                        ClearLinkCache(drive, entityId);
                        ClearPerFolderCache(drive, folder);
                        ClearPerFolderCache(drive, dstFolder);
                    }
                    catch (Exception ex)
                    {
                        // The relocate is atomic, so on failure nothing moved.
                        // Common cause: no create permission in the destination.
                        var wrapped = new OrchException(target,
                            $"'{entityName}' was not moved to '{dst}'. The relocation is atomic and was " +
                            $"rejected by the server (the entity remains in '{source}'). Server: {ex.Message}", ex);
                        WriteError(new ErrorRecord(wrapped, ErrorId,
                            ErrorCategory.InvalidOperation, target));
                    }
                }
            }
        }
    }

    // Returns the destination folder that mirrors srcFolder's path relative to
    // srcRootFolder, under destRootFolder — creating any missing intermediate
    // folders as plain modern folders (no package feed). Under -WhatIf it emits
    // the "New Folder" preview but creates nothing, returning a placeholder
    // folder so the caller can still preview the entity moves into it (the
    // placeholder's negative Id is never used because the move is also gated on
    // ShouldProcess and won't execute under -WhatIf). Returns null only on a
    // real folder-create error. Resolved folders are memoized by FQN.
    private Folder? ResolveMirroredDestination(
        OrchDriveInfo drive, Folder srcRootFolder, Folder srcFolder,
        Folder destRootFolder, Dictionary<string, Folder?> cache)
    {
        // relativePath is "" for the root itself, else e.g. "sub" or "a/b".
        string relativePath = srcFolder.GetRelativePath(srcRootFolder);
        if (string.IsNullOrEmpty(relativePath)) return destRootFolder;

        Folder current = destRootFolder;
        long placeholderId = -1;
        foreach (var segment in relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            string childFqn = string.IsNullOrEmpty(current.FullyQualifiedName)
                ? segment
                : $"{current.FullyQualifiedName}/{segment}";

            if (cache.TryGetValue(childFqn, out var cached))
            {
                if (cached is null) return null; // a prior attempt errored
                current = cached;
                continue;
            }

            // Existing child with this name under the current folder?
            long parentId = current.Id ?? 0;
            Folder? child = drive.GetFolders()
                .FirstOrDefault(f => f.ParentId == parentId
                    && string.Compare(f.DisplayName, segment, StringComparison.OrdinalIgnoreCase) == 0);

            if (child is null)
            {
                if (string.IsNullOrEmpty(current.FullyQualifiedName))
                {
                    // A folder directly under the tenant root is a top-level
                    // folder whose package-feed setting is significant and can't
                    // be inferred from the source — don't auto-create it. Surface
                    // an error so the user creates it explicitly with the feed
                    // they want.
                    WriteError(new ErrorRecord(
                        new OrchException(drive.NameColonSeparator,
                            $"{drive.NameColonSeparator}{childFqn} does not exist. Create top-level folders explicitly — their package-feed setting can't be inferred."),
                        ErrorId, ErrorCategory.InvalidOperation, drive));
                    cache[childFqn] = null;
                    return null;
                }

                // Mirror the source tree: create the missing subfolder. Folder
                // creation is a state change, so gate it on ShouldProcess — which
                // also emits the "New Folder" -WhatIf line. When ShouldProcess
                // returns false (-WhatIf, or declined -Confirm) don't create it;
                // instead carry a placeholder so the entity moves into this
                // folder still preview. Cache the placeholder so deeper segments
                // and sibling entities reuse it without re-prompting.
                string newPath = System.IO.Path.Combine(current.GetPSPath(), segment);
                if (!ShouldProcess(newPath, "New Folder"))
                {
                    child = new Folder
                    {
                        Id = placeholderId--,
                        DisplayName = segment,
                        ParentId = parentId,
                        FullyQualifiedName = childFqn,
                        Path = current.GetPSPath(),
                    };
                }
                else
                {
                    try
                    {
                        child = drive.OrchAPISession.CreateFolder(segment, null, "Processes", parentId);
                        if (child is not null)
                        {
                            child.Path = current.GetPSPath();
                            drive.AppendFolderToCache(child);
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(newPath, ex),
                            ErrorId, ErrorCategory.InvalidOperation, newPath));
                        child = null;
                    }
                    if (child is null)
                    {
                        cache[childFqn] = null;
                        return null;
                    }
                }
            }

            cache[childFqn] = child;
            current = child;
        }

        return current;
    }
}
