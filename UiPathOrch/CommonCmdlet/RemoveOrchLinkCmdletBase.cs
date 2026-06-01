using System.Management.Automation;
using UiPath.OrchAPI;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Generic base for the Remove-Orch{Asset,Bucket,Queue}Link trio. Mirror of
// AddOrchLinkCmdletBase with one change: Share*ToFolders is called with toAdd
// empty and toRemove populated (the ShouldProcess action reads "Remove X from
// ..." vs "Add X to ..."). The two bases are kept separate (rather than fused
// on a verb-flag) so each reads straight through without dispatch.
//
// Derived classes must:
//   - Decorate the class with
//     [Cmdlet(VerbsCommon.Remove, "OrchXxxLink", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium)]
//   - Override Name and Link to attach [ArgumentCompleter(typeof(...))] —
//     the [Parameter]/[SupportsWildcards] attributes must be re-declared on
//     the override (matches the RemoveDriveEntityCmdletBase convention).
//   - Implement the abstract members below.
public abstract class RemoveOrchLinkCmdletBase<TEntity> : OrchestratorPSCmdlet
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

    // Stable identifier emitted on WriteError, e.g. "RemoveAssetLinkError".
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
                    $"Remove{LinkNoun} prefetch failed for '{df.folder.GetPSPath()}': {ex.Message}");
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

            // Cross-drive operations are not supported by the API; only same-drive targets apply.
            var sameDriveLinks = drivesLinks.Where(dl => dl.drive == drive).ToList();
            if (sameDriveLinks.Count == 0) continue;

            foreach (var entity in entities
                .FilterByWildcards(e => GetEntityName(e), wpName)
                .OrderBy(e => GetEntityName(e)).WithCancellation(cancelHandler.Token))
            {
                // Batch all targets for this (folder, entity) into a single API call.
                // The Share*ToFolders endpoint accepts both ToAdd and ToRemove arrays;
                // we pass empty ToAdd and the targets as ToRemove.
                var toRemoveIds = sameDriveLinks
                    .Select(dl => dl.folder.Id ?? 0)
                    .Where(id => id != 0 && id != folder.Id)
                    .Distinct()
                    .ToList();
                if (toRemoveIds.Count == 0) continue;

                string source = folder.GetPSPath();
                string target = System.IO.Path.Combine(source, GetEntityName(entity)!);
                string action = $"Remove {LinkNoun} from {string.Join(", ", sameDriveLinks.Select(dl => dl.folder.GetPSPath()))}";

                if (!ShouldProcess(target, action)) continue;

                try
                {
                    long entityId = GetEntityId(entity);
                    Share(drive.OrchAPISession, folder.Id ?? 0, new List<long> { entityId }, new List<long>(), toRemoveIds);

                    // Invalidate just what changed: this entity's link set, and the
                    // per-folder entity list for each unlinked folder (which no
                    // longer exposes this entity).
                    ClearLinkCache(drive, entityId);
                    foreach (var dl in sameDriveLinks.Where(dl => toRemoveIds.Contains(dl.folder.Id ?? 0)))
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
