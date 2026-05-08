using System.Management.Automation;
using UiPath.OrchAPI;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Generic base for the Add-Orch{Asset,Bucket,Queue}Link trio. The algorithm —
// parallel prefetch of the per-folder cache, same-drive target filtering,
// batched Share*ToFolders call (toAdd populated, toRemove empty), targeted
// cache invalidation — was independently mirrored across 3 cmdlet files
// before this base was extracted. RemoveOrchLinkCmdletBase is a deliberate
// near-clone of this class with toAdd/toRemove swapped and the action label
// using "✗" instead of "→"; the two are kept separate so each is readable
// straight through without a verb-flag branch.
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

    protected sealed override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth).ToList();
        var drivesLinks = SessionState.EnumFolders(Link).ToList();
        var wpName = Name.ConvertToWildcardPatternList();

        // Parallel prefetch warms the per-folder entity cache so the foreach
        // below hits the cache instead of issuing serial round-trips.
        Parallel.ForEach(drivesFolders, df =>
        {
            try { GetEntities(df.drive, df.folder); }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Add{LinkNoun} prefetch failed for '{df.folder.GetPSPath()}': {ex.Message}");
            }
        });

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
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

            // Cross-drive linking is not supported by the API; only same-drive targets apply.
            var sameDriveLinks = drivesLinks.Where(dl => dl.drive == drive).ToList();
            if (sameDriveLinks.Count == 0) continue;

            foreach (var entity in entities
                .FilterByWildcards(e => GetEntityName(e), wpName)
                .OrderBy(e => GetEntityName(e)).WithCancellation(cancelHandler.Token))
            {
                // Batch all targets for this (folder, entity) into a single API call.
                // Share*ToFolders accepts arrays for both the entity list and the
                // ToAdd folder list, so 1 entity × N targets is one round-trip.
                var toAddIds = sameDriveLinks
                    .Select(dl => dl.folder.Id ?? 0)
                    .Where(id => id != 0 && id != folder.Id)   // can't link to self
                    .Distinct()
                    .ToList();
                if (toAddIds.Count == 0) continue;

                string source = folder.GetPSPath();
                string target = System.IO.Path.Combine(source, GetEntityName(entity)!);
                string action = $"Add {LinkNoun} → {string.Join(", ", sameDriveLinks.Select(dl => dl.folder.GetPSPath()))}";

                if (!ShouldProcess(target, action)) continue;

                try
                {
                    long entityId = GetEntityId(entity);
                    Share(drive.OrchAPISession, folder.Id ?? 0, new List<long> { entityId }, toAddIds, new List<long>());

                    // Invalidate just what changed: this entity's link set, and the
                    // per-folder entity list for each newly-targeted folder (which
                    // now exposes this entity where it didn't before).
                    ClearLinkCache(drive, entityId);
                    foreach (var dl in sameDriveLinks.Where(dl => toAddIds.Contains(dl.folder.Id ?? 0)))
                    {
                        ClearPerFolderCache(drive, dl.folder);
                    }
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(target, ex), ErrorId,
                        ErrorCategory.InvalidOperation, target));
                }
            }
        }
    }
}
