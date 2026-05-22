using System.Management.Automation;
using UiPath.OrchAPI;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Generic base for the Add-Orch{Asset,Bucket,Queue}Link trio.
//
// Batching model (v1.5.3): pipeline input is buffered across ProcessRecord
// and the actual Share*ToFolders calls are issued once in EndProcessing,
// grouped by (drive, source folder, entity). All target folders for one
// entity collapse into a single API call with a deduplicated toAdd list.
// This turns the common "one asset shared into N folders" CSV
// (`Get-OrchAssetLink -ExportCsv` emits one row per target) from N
// round-trips into 1.
//
//   Import-Csv links.csv | Add-OrchAssetLink
//     row (Dept#2, DatabaseConnection, Development)
//     row (Dept#2, DatabaseConnection, Finance)      -> 1 ShareAssetsToFolders
//     row (Dept#2, DatabaseConnection, Production)       call, toAdd = {Development,
//     row (Dept#2, DatabaseConnection, fuga)             Finance, Production, fuga}
//
// Direct invocation still works and still batches within the call:
//   Add-OrchAssetLink -Name X -Link A,B,C   ->   one buffered tuple, one Share.
//
// Idempotency: Share*ToFolders is an incremental (toAdd / toRemove) op, so
// re-adding an already-shared folder is a server-side no-op — exact-
// duplicate rows are deduped here AND harmless server-side either way.
//
// Error isolation is per (source folder, entity) group rather than per row:
// a group whose Share call fails is reported via WriteError and the rest
// continue. A malformed source folder (GetEntities throws) is reported once
// and skipped.
//
// Derived classes must:
//   - Decorate the class with [Cmdlet(VerbsCommon.Add, "OrchXxxLink", SupportsShouldProcess = true)]
//   - Override Name to attach [ArgumentCompleter(typeof(XxxNameCompleter))] —
//     the [Parameter]/[SupportsWildcards] attributes must be re-declared on
//     the override (matches the RemoveDriveEntityCmdletBase convention).
//   - Implement the abstract members below.
public abstract class AddOrchLinkCmdletBase<TEntity> : OrchestratorPSCmdlet
    where TEntity : class
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public virtual string[]? Name { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public virtual string[]? Link { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    // Stable identifier emitted on WriteError, e.g. "AddAssetLinkError".
    protected abstract string ErrorId { get; }

    // "AssetLink" / "BucketLink" / "QueueLink" — appears in the action label
    // and the prefetch debug log.
    protected abstract string LinkNoun { get; }

    protected abstract ICollection<TEntity> GetEntities(OrchDriveInfo drive, Folder folder);
    protected abstract string? GetEntityName(TEntity? entity);
    protected abstract long GetEntityId(TEntity entity);
    protected abstract void Share(OrchAPISession api, long srcFolderId, List<long> entityIds, List<long> toAdd, List<long> toRemove);
    protected abstract void ClearLinkCache(OrchDriveInfo drive, long entityId);
    protected abstract void ClearPerFolderCache(OrchDriveInfo drive, Folder folder);

    // One buffered invocation's worth of parameter values. In pipeline mode
    // each input object produces one entry (Name/Link/Path are single-valued
    // per row); in direct mode there's a single entry with multi-valued
    // arrays. Both shapes are resolved identically in EndProcessing.
    private readonly List<(string[]? Name, string[]? Link, string[]? Path)> _buffered = new();

    // Accumulator group: one entity in one source folder, with the union of
    // every target folder requested for it across all buffered rows.
    private sealed class LinkGroup
    {
        public required OrchDriveInfo Drive { get; init; }
        public required Folder SrcFolder { get; init; }
        public required TEntity Entity { get; init; }
        public required long EntityId { get; init; }
        public required string EntityName { get; init; }
        public HashSet<long> TargetIds { get; } = new();
        public List<Folder> TargetFolders { get; } = new();
    }

    protected sealed override void ProcessRecord()
    {
        // Buffer only — all the work happens in EndProcessing once the whole
        // pipeline has been seen, so same-entity rows can be coalesced.
        _buffered.Add((Name, Link, Path));
    }

    protected sealed override void EndProcessing()
    {
        if (_buffered.Count == 0) return;

        using var cancelHandler = new ConsoleCancelHandler();

        // Phase 1: resolve every buffered row into (drive, src folder, entity)
        // groups, unioning target folder ids. Keyed by drive name + src folder
        // id + entity id so the same asset across multiple rows merges.
        var groups = new Dictionary<(string Drive, long Src, long Entity), LinkGroup>();
        var prefetchTargets = new HashSet<(OrchDriveInfo drive, Folder folder)>();

        foreach (var (names, links, paths) in _buffered)
        {
            var srcDrivesFolders = SessionState.EnumFolders(paths, Recurse.IsPresent, Depth).ToList();
            foreach (var df in srcDrivesFolders) prefetchTargets.Add(df);
        }

        // Warm the per-folder entity caches in parallel before the serial
        // grouping loop (mirrors the pre-batch behaviour).
        Parallel.ForEach(prefetchTargets, df =>
        {
            try { GetEntities(df.drive, df.folder); }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Add{LinkNoun} prefetch failed for '{df.folder.GetPSPath()}': {ex.Message}");
            }
        });

        foreach (var (names, links, paths) in _buffered.WithCancellation(cancelHandler.Token))
        {
            var srcDrivesFolders = SessionState.EnumFolders(paths, Recurse.IsPresent, Depth).ToList();
            var drivesLinks = SessionState.EnumFolders(links).ToList();
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

                // Cross-drive linking is not supported by the API; same-drive only.
                var sameDriveLinks = drivesLinks.Where(dl => dl.drive == drive).ToList();
                if (sameDriveLinks.Count == 0) continue;

                foreach (var entity in entities
                    .FilterByWildcards(e => GetEntityName(e), wpName)
                    .OrderBy(e => GetEntityName(e)).WithCancellation(cancelHandler.Token))
                {
                    long entityId = GetEntityId(entity);
                    var key = (drive.NameColonSeparator, folder.Id ?? 0, entityId);
                    if (!groups.TryGetValue(key, out var group))
                    {
                        group = new LinkGroup
                        {
                            Drive = drive,
                            SrcFolder = folder,
                            Entity = entity,
                            EntityId = entityId,
                            EntityName = GetEntityName(entity)!,
                        };
                        groups[key] = group;
                    }

                    foreach (var dl in sameDriveLinks)
                    {
                        long fid = dl.folder.Id ?? 0;
                        if (fid == 0 || fid == folder.Id) continue;   // can't link to self
                        if (group.TargetIds.Add(fid))
                        {
                            group.TargetFolders.Add(dl.folder);
                        }
                    }
                }
            }
        }

        // Phase 2: one Share call per group, with the deduped toAdd list.
        foreach (var group in groups.Values.WithCancellation(cancelHandler.Token))
        {
            if (group.TargetIds.Count == 0) continue;

            string source = group.SrcFolder.GetPSPath();
            string target = System.IO.Path.Combine(source, group.EntityName);
            string action = $"Add {LinkNoun} → {string.Join(", ", group.TargetFolders.Select(f => f.GetPSPath()))}";

            if (!ShouldProcess(target, action)) continue;

            try
            {
                Share(group.Drive.OrchAPISession, group.SrcFolder.Id ?? 0,
                    new List<long> { group.EntityId }, group.TargetIds.ToList(), new List<long>());

                // Invalidate just what changed: this entity's link set, and the
                // per-folder entity list for each newly-targeted folder.
                ClearLinkCache(group.Drive, group.EntityId);
                foreach (var tf in group.TargetFolders)
                {
                    ClearPerFolderCache(group.Drive, tf);
                }
            }
            catch (Exception ex)
            {
                // The Share call is atomic: if any target folder is rejected
                // (e.g. the caller lacks Assets.Create permission there), the
                // WHOLE batch fails and NONE of this entity's links are added.
                // Make that explicit and list the folders involved so the user
                // can drop the offending row(s) from the CSV and re-run. The
                // server message (which names the rejected folder) is preserved.
                string targets = string.Join(", ", group.TargetFolders.Select(f => f.GetPSPath()));
                var wrapped = new OrchException(target,
                    $"No {LinkNoun} added for '{group.EntityName}'. The batched share to [{targets}] " +
                    $"is all-or-nothing and was rejected by the server (see below); fix or remove the " +
                    $"offending target and re-run. Server: {ex.Message}", ex);
                WriteError(new ErrorRecord(wrapped, ErrorId,
                    ErrorCategory.InvalidOperation, target));
            }
        }
    }
}
