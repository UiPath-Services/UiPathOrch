using System.Management.Automation;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Core;

// Share/link helpers behind Copy-Item / Copy-Orch* (CopyAssets/Queues/Buckets).
public partial class OrchProvider
{
    // Shared implementation behind LinkAsset / LinkQueue / LinkBucket.
    //
    // Cross-tenant: when the source entity is shared into folders other than srcFolder,
    // reproduce that link graph at the destination: for every dst folder that mirrors a
    // src link folder AND already holds a same-named entity, share that dst entity into
    // newFolder. Returns true if at least one link was established (so the caller skips
    // creating a duplicate). The same shared entity has one Id across all its link
    // folders, so seenIds dedups redundant Share*ToFolders calls; per-iteration try/catch
    // lets one folder's failure not block the others.
    //
    // Same tenant: deliberately does NOT link. Copying an entity into another folder of
    // the same tenant produces an independent entity; sharing an existing one with more
    // folders is Add-Orch{Asset,Queue,Bucket}Link's job. The operator is warned so the
    // choice is visible rather than silent.
    //
    // The three thin wrappers below supply only what differs: the entity name/id
    // accessors, the src link-folder lookup, the dst entity lookup, the concrete
    // Share*ToFolders call, and the two error ids. Link* are file-internal (the
    // Copy-Orch* cmdlets enter through CopyAssets/CopyQueues/CopyBuckets), so this
    // refactor doesn't touch any externally-visible signature.
    private static bool LinkSharedEntity<T>(
        IWritableHost _this,
        OrchDriveInfo srcDrive, Folder srcFolder,
        OrchDriveInfo dstDrive, Folder newFolder,
        string? entityName, string msg,
        string getLinkErrorId, string linkErrorId,
        string kind, string linkCmdlet, string srcEntityPSPath, Int64 srcEntityId,
        LinkCopyReport? report,
        Func<List<Int64>?> getSrcLinkFolderIds,
        Func<Folder, IEnumerable<T>> getDstEntities,
        Func<T, string?> nameOf,
        Func<T, Int64> idOf,
        Action<Int64, Int64, Int64> share,
        Action<Folder, Int64> invalidateDst)
        where T : class
    {
        // Measured: GetFoldersForAsset(id=) is 404 on 11.1 (20.10.16), so linked-entity copying
        // cannot work below 12; the link APIs are exercised routinely on Cloud (v20) by the
        // cross-tenant suites. API v12 has no obtainable on-prem build — the gate stays at 12,
        // bracketed by those measurements. Below the gate we can't even tell whether the entity
        // is shared, so there is nothing to warn about either.
        if (srcDrive.OrchAPISession.ApiVersion < 12) return false;
        if (dstDrive.OrchAPISession.ApiVersion < 12) return false;

        IEnumerable<Folder>? dstLinkFolders;
        List<Int64> srcLinkFolderIds;
        try
        {
            var ids = getSrcLinkFolderIds();
            if (ids is null || ids.Count == 0)
            {
                return false; // not shared with any other folder: an ordinary copy
            }
            srcLinkFolderIds = ids;

            if (IsSameTenant(srcDrive, dstDrive))
            {
                report?.NoteSameTenantCopy(_this, SameTenantCopyMessage(
                    srcDrive, newFolder, kind, linkCmdlet, srcEntityPSPath, srcLinkFolderIds));
                return false;
            }

            dstLinkFolders = FindDstFolders(
                srcLinkFolderIds,
                srcDrive.GetFolders(),
                dstDrive.GetFolders(),
                srcFolder,
                newFolder);
        }
        catch (Exception ex)
        {
            string target = srcFolder.GetPSPath();
            _this.WriteError(new ErrorRecord(new OrchException(target, msg, ex), getLinkErrorId, ErrorCategory.InvalidOperation, target));
            return false;
        }

        bool linked = false;
        var seenIds = new HashSet<Int64>();
        foreach (var dstLinkFolder in dstLinkFolders ?? [])
        {
            try
            {
                var dstEntity = getDstEntities(dstLinkFolder)
                    .FirstOrDefault(e => string.Compare(nameOf(e), entityName, StringComparison.OrdinalIgnoreCase) == 0);
                if (dstEntity is null) continue;
                Int64 dstEntityId = idOf(dstEntity);
                if (!seenIds.Add(dstEntityId)) continue;

                share(dstLinkFolder.Id ?? 0, dstEntityId, newFolder.Id ?? 0);
                // The share just made the entity visible in newFolder and changed its link
                // set, so both destination caches are now stale. Neither the Copy-Orch*
                // cmdlets nor Copy-Item's folder loop cover this: they clear the folder they
                // copied INTO, never the folder an entity was shared into. It matters because
                // newFolder's entity list is routinely cached BEFORE the entity lands in it —
                // an earlier folder's pass reads it right here, via getDstEntities, looking
                // for its own counterpart. Left stale, the folder stayed short one entity for
                // the rest of the session, and FindDstQueue — which resolves a queue trigger's
                // queue by name later in the same run — reported the queue as missing and
                // silently skipped the trigger.
                invalidateDst(newFolder, dstEntityId);
                linked = true;
            }
            catch (Exception ex)
            {
                string target = $"{dstLinkFolder.GetPSPath()} → {newFolder.GetPSPath()}";
                _this.WriteError(new ErrorRecord(new OrchException(target, msg, ex), linkErrorId, ErrorCategory.InvalidOperation, target));
                // continue — one folder's failure shouldn't block the others
            }
        }

        if (linked)
        {
            report?.NoteLinked(kind, srcEntityId, entityName);
        }
        else
        {
            // Not necessarily a loss yet: the counterpart folder may still be created — and
            // filled — by a later folder's pass in this same run, which then links this very
            // entity. LinkCopyReport re-judges at the end of the run and drops this record if
            // that happens. See its class comment.
            report?.NoteUnlinked(kind, srcEntityId, entityName,
                $"'{srcEntityPSPath}' is shared with {DescribeFolders(srcDrive, srcLinkFolderIds)}, " +
                $"but {dstDrive.NameColon} has no matching {kind} in the corresponding folder(s), so it was copied as an independent {kind}. " +
                $"Copy those folders too, or re-create the sharing afterwards with {linkCmdlet}.");
        }
        return linked;
    }

    private static string SameTenantCopyMessage(OrchDriveInfo srcDrive, Folder newFolder,
        string kind, string linkCmdlet, string srcEntityPSPath, List<Int64> srcLinkFolderIds)
        => $"'{srcEntityPSPath}' is shared with {DescribeFolders(srcDrive, srcLinkFolderIds)}. " +
           $"The copy to '{newFolder.GetPSPath()}' does not reproduce those links — within one tenant it creates an independent {kind}. " +
           $"To share the existing {kind} with that folder instead of copying it, use {linkCmdlet}.";

    // -WhatIf preview of the link decision.
    //
    // Only the SAME-TENANT verdict is previewable: Copy never reproduces links within a
    // tenant, so it is final at preview time and worth showing before committing. The
    // cross-tenant verdict is deliberately NOT previewed — a cross-tenant link depends on
    // entities this run has not created yet (folder A's pass links nothing; folder B's pass
    // links A's freshly created entity), so predicting it under -WhatIf would report losses
    // that the real run does not have. Cross-tenant misses are reported by LinkCopyReport at
    // the end of the real run instead.
    private static void PreviewSameTenantLinkLoss(IWritableHost _this,
        OrchDriveInfo srcDrive, OrchDriveInfo dstDrive, Folder newFolder,
        string kind, string linkCmdlet, string srcEntityPSPath,
        Func<List<Int64>?> getSrcLinkFolderIds, LinkCopyReport? report)
    {
        if (report is null) return;
        if (!IsSameTenant(srcDrive, dstDrive)) return;
        if (srcDrive.OrchAPISession.ApiVersion < 12 || dstDrive.OrchAPISession.ApiVersion < 12) return;

        List<Int64>? ids;
        try
        {
            ids = getSrcLinkFolderIds();
        }
        catch
        {
            return; // a read-only preview must never fail the run over a warning
        }
        if (ids is null || ids.Count == 0) return;

        report.NoteSameTenantCopy(_this, SameTenantCopyMessage(
            srcDrive, newFolder, kind, linkCmdlet, srcEntityPSPath, ids));
    }

    internal static void PreviewAssetLinkLoss(IWritableHost _this,
        OrchDriveInfo srcDrive, Folder srcFolder,
        OrchDriveInfo dstDrive, Folder newFolder, Asset asset, LinkCopyReport? report)
        => PreviewSameTenantLinkLoss(_this, srcDrive, dstDrive, newFolder,
            "asset", "Add-OrchAssetLink", asset.GetPSPath(),
            () => srcDrive.GetFoldersForAsset(srcFolder, asset)?.AccessibleFolders?
                .Select(af => af.Id ?? 0).Where(id => id != srcFolder.Id).ToList(), report);

    internal static void PreviewQueueLinkLoss(IWritableHost _this,
        OrchDriveInfo srcDrive, Folder srcFolder,
        OrchDriveInfo dstDrive, Folder newFolder, QueueDefinition queue, LinkCopyReport? report)
        => PreviewSameTenantLinkLoss(_this, srcDrive, dstDrive, newFolder,
            "queue", "Add-OrchQueueLink", queue.GetPSPath(),
            () => srcDrive.GetFoldersForQueue(srcFolder, queue)?.AccessibleFolders?
                .Select(af => af.Id ?? 0).Where(id => id != srcFolder.Id).ToList(), report);

    internal static void PreviewBucketLinkLoss(IWritableHost _this,
        OrchDriveInfo srcDrive, Folder srcFolder,
        OrchDriveInfo dstDrive, Folder newFolder, Bucket bucket, LinkCopyReport? report)
        => PreviewSameTenantLinkLoss(_this, srcDrive, dstDrive, newFolder,
            "bucket", "Add-OrchBucketLink", bucket.GetPSPath(),
            () => srcDrive.GetFoldersForBucket(srcFolder, bucket)?.AccessibleFolders?
                .Select(af => af.Id ?? 0).Where(id => id != srcFolder.Id).ToList(), report);

    // Two PSDrives can be mounted on the SAME tenant (e.g. Orch1: and Orch1c: both on
    // .../yotsuda/svc1), and a copy between them is still a same-tenant copy — so identity
    // is the drive's root URL, not the drive object.
    private static bool IsSameTenant(OrchDriveInfo a, OrchDriveInfo b)
        => ReferenceEquals(a, b)
        || string.Equals(a.OrchAPISession._base_url.TrimEnd('/'),
                         b.OrchAPISession._base_url.TrimEnd('/'),
                         StringComparison.OrdinalIgnoreCase);

    // Folder ids -> a short, readable list of PSPaths for the warnings above.
    private static string DescribeFolders(OrchDriveInfo drive, List<Int64> folderIds)
    {
        const int MaxNamed = 3;
        List<string> paths;
        try
        {
            paths = drive.GetFolders()
                .Where(f => folderIds.Contains(f.Id ?? 0))
                .Select(f => f.GetPSPath())
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch
        {
            // The folder catalog is cached and already loaded by this point; if it somehow
            // isn't, a warning is not worth failing the copy over.
            paths = [];
        }

        if (paths.Count == 0) return $"{folderIds.Count} other folder(s)";
        return paths.Count <= MaxNamed
            ? string.Join(", ", paths)
            : $"{string.Join(", ", paths.Take(MaxNamed))} (+{paths.Count - MaxNamed} more)";
    }

    internal static bool LinkAsset(IWritableHost _this,
        OrchDriveInfo srcDrive, Folder srcFolder,
        OrchDriveInfo dstDrive, Folder newFolder, Asset asset, string msg,
        LinkCopyReport? report = null)
    {
        return LinkSharedEntity<Asset>(_this, srcDrive, srcFolder, dstDrive, newFolder,
            asset.Name, msg, "GetAssetLinkError", "LinkAssetError",
            "asset", "Add-OrchAssetLink", asset.GetPSPath(), asset.Id ?? 0, report,
            () => srcDrive.GetFoldersForAsset(srcFolder, asset)?.AccessibleFolders?
                .Select(af => af.Id ?? 0).Where(id => id != srcFolder.Id).ToList(),
            f => dstDrive.Assets.Get(f),
            a => a.Name,
            a => a.Id ?? 0,
            (linkFolderId, entityId, newFolderId) => dstDrive.OrchAPISession.ShareAssetsToFolders(
                linkFolderId, new List<Int64> { entityId }, new List<Int64> { newFolderId }, new List<Int64>()),
            (folder, entityId) => { dstDrive.Assets.ClearCache(folder); dstDrive.ClearAssetLinkCache(entityId); });
    }

    internal static bool LinkQueue(IWritableHost _this,
        OrchDriveInfo srcDrive, Folder srcFolder,
        OrchDriveInfo dstDrive, Folder newFolder, QueueDefinition queue,
        LinkCopyReport? report = null)
    {
        string msg = $"Sharing queue {queue.GetPSPath()}";
        return LinkSharedEntity<QueueDefinition>(_this, srcDrive, srcFolder, dstDrive, newFolder,
            queue.Name, msg, "GetQueueLinkError", "LinkQueueError",
            "queue", "Add-OrchQueueLink", queue.GetPSPath(), queue.Id ?? 0, report,
            () => srcDrive.GetFoldersForQueue(srcFolder, queue)?.AccessibleFolders?
                .Select(af => af.Id ?? 0).Where(id => id != srcFolder.Id).ToList(),
            f => dstDrive.Queues.Get(f),
            q => q.Name,
            q => q.Id ?? 0,
            (linkFolderId, entityId, newFolderId) => dstDrive.OrchAPISession.ShareQueuesToFolders(
                linkFolderId, new List<Int64> { entityId }, new List<Int64> { newFolderId }, new List<Int64>()),
            (folder, entityId) => { dstDrive.Queues.ClearCache(folder); dstDrive.ClearQueueLinkCache(entityId); });
    }

    internal static bool LinkBucket(IWritableHost _this,
        OrchDriveInfo srcDrive, Folder srcFolder,
        OrchDriveInfo dstDrive, Folder newFolder, Bucket bucket,
        LinkCopyReport? report = null)
    {
        string msg = $"Sharing bucket {bucket.GetPSPath()}";
        return LinkSharedEntity<Bucket>(_this, srcDrive, srcFolder, dstDrive, newFolder,
            bucket.Name, msg, "GetBucketLinkError", "LinkBucketError",
            "bucket", "Add-OrchBucketLink", bucket.GetPSPath(), bucket.Id ?? 0, report,
            () => srcDrive.GetFoldersForBucket(srcFolder, bucket)?.AccessibleFolders?
                .Select(af => af.Id ?? 0).Where(id => id != srcFolder.Id).ToList(),
            f => dstDrive.Buckets.Get(f),
            b => b.Name,
            b => b.Id ?? 0,
            (linkFolderId, entityId, newFolderId) => dstDrive.OrchAPISession.ShareBucketsToFolders(
                linkFolderId, new List<Int64> { entityId }, new List<Int64> { newFolderId }, new List<Int64>()),
            (folder, entityId) => { dstDrive.Buckets.ClearCache(folder); dstDrive.ClearBucketLinkCache(entityId); });
    }

    // Tracks what happened to shared entities' folder links across ONE copy run (one
    // Copy-Orch* ProcessRecord, or one Copy-Item), so a shared entity that landed as an
    // independent copy is reported instead of silently diverging.
    //
    // The two cases differ in when they can be judged:
    //
    //   * Same tenant — Copy never reproduces links by design, so the verdict is final at
    //     the moment of the copy and the warning is emitted immediately.
    //
    //   * Cross tenant — a link can only be made once the counterpart folder exists at the
    //     destination AND already holds the entity, which in a folder-tree copy is often
    //     only true on a LATER folder's pass of the same run (folder A's pass finds nothing
    //     in dst B yet; folder B's pass then links A's freshly created entity). Warning at
    //     the moment of the miss would cry wolf on the normal migration, so misses are
    //     recorded and re-judged in Flush: a later link of the SAME source entity cancels
    //     the pending warning. The key is the source entity id — a shared entity has one id
    //     across all of its link folders — with the name as a tiebreaker for the id-less case.
    //
    // Both kinds are throttled the same way as DropWarningBudget: the first few in full,
    // then one summary line, so a large migration doesn't flood the warning stream.
    internal sealed class LinkCopyReport
    {
        private const int Threshold = 5;

        private readonly Dictionary<(string kind, Int64 id, string? name), string> _unlinked = new();
        private readonly HashSet<(string kind, Int64 id, string? name)> _linked = new();
        private int _sameTenantCount;

        public void NoteSameTenantCopy(IWritableHost host, string message)
        {
            _sameTenantCount++;
            if (_sameTenantCount <= Threshold)
            {
                host.WriteWarning(message);
            }
        }

        public void NoteLinked(string kind, Int64 srcEntityId, string? name)
        {
            var key = (kind, srcEntityId, name);
            _linked.Add(key);
            _unlinked.Remove(key);
        }

        public void NoteUnlinked(string kind, Int64 srcEntityId, string? name, string message)
        {
            var key = (kind, srcEntityId, name);
            if (_linked.Contains(key)) return; // an earlier pass already linked it
            _unlinked[key] = message;
        }

        public void Flush(IWritableHost host)
        {
            if (_sameTenantCount > Threshold)
            {
                host.WriteWarning($"{_sameTenantCount} shared entities were copied without their folder links (same tenant); {_sameTenantCount - Threshold} further warning(s) were suppressed. Use Add-Orch{{Asset,Queue,Bucket}}Link to share an existing entity with another folder instead of copying it.");
            }

            int shown = 0;
            foreach (var message in _unlinked.Values)
            {
                if (++shown > Threshold) break;
                host.WriteWarning(message);
            }
            if (_unlinked.Count > Threshold)
            {
                host.WriteWarning($"{_unlinked.Count} shared entities were copied as independent entities because the destination had no matching entity in the corresponding folder(s); {_unlinked.Count - Threshold} further warning(s) were suppressed. Get-Orch{{Asset,Queue,Bucket}}Link -ExportCsv on the source and Add-Orch{{Asset,Queue,Bucket}}Link on the destination re-create the sharing.");
            }
        }
    }
}
