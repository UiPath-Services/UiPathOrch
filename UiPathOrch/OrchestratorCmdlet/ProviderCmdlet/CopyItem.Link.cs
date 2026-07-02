using System.Management.Automation;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Core;

// Share/link helpers behind Copy-Item / Copy-Orch* (CopyAssets/Queues/Buckets).
public partial class OrchProvider
{
    // Shared implementation behind LinkAsset / LinkQueue / LinkBucket. When the
    // source entity is shared into folders other than srcFolder, reproduce that
    // link graph at the destination: for every dst folder that mirrors a src link
    // folder AND already holds a same-named entity, share that dst entity into
    // newFolder. Returns true if at least one link was established (so the caller
    // skips creating a duplicate). The same shared entity has one Id across all
    // its link folders, so seenIds dedups redundant Share*ToFolders calls;
    // per-iteration try/catch lets one folder's failure not block the others.
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
        Func<List<Int64>?> getSrcLinkFolderIds,
        Func<Folder, IEnumerable<T>> getDstEntities,
        Func<T, string?> nameOf,
        Func<T, Int64> idOf,
        Action<Int64, Int64, Int64> share)
        where T : class
    {
        // Measured: GetFoldersForAsset(id=) is 404 on 11.1 (20.10.16), so linked-entity copying
        // cannot work below 12; the link APIs are exercised routinely on Cloud (v20) by the
        // cross-tenant suites. API v12 has no obtainable on-prem build — the gate stays at 12,
        // bracketed by those measurements.
        if (srcDrive.OrchAPISession.ApiVersion < 12) return false;
        if (dstDrive.OrchAPISession.ApiVersion < 12) return false;

        IEnumerable<Folder> dstLinkFolders = null;
        try
        {
            var srcLinkFolderIds = getSrcLinkFolderIds();
            if (srcLinkFolderIds is null || !srcLinkFolderIds.Any())
            {
                return false;
            }

            dstLinkFolders = FindDstFolders(
                srcLinkFolderIds,
                srcDrive.GetFolders(),
                dstDrive.GetFolders(),
                srcFolder,
                newFolder);

            if (dstLinkFolders is null || !dstLinkFolders.Any())
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            string target = srcFolder.GetPSPath();
            _this.WriteError(new ErrorRecord(new OrchException(target, msg, ex), getLinkErrorId, ErrorCategory.InvalidOperation, target));
            return false;
        }

        bool linked = false;
        var seenIds = new HashSet<Int64>();
        foreach (var dstLinkFolder in dstLinkFolders)
        {
            try
            {
                var dstEntity = getDstEntities(dstLinkFolder)
                    .FirstOrDefault(e => string.Compare(nameOf(e), entityName, StringComparison.OrdinalIgnoreCase) == 0);
                if (dstEntity is null) continue;
                Int64 dstEntityId = idOf(dstEntity);
                if (!seenIds.Add(dstEntityId)) continue;

                share(dstLinkFolder.Id ?? 0, dstEntityId, newFolder.Id ?? 0);
                linked = true;
            }
            catch (Exception ex)
            {
                string target = $"{dstLinkFolder.GetPSPath()} → {newFolder.GetPSPath()}";
                _this.WriteError(new ErrorRecord(new OrchException(target, msg, ex), linkErrorId, ErrorCategory.InvalidOperation, target));
                // continue — one folder's failure shouldn't block the others
            }
        }
        return linked;
    }

    internal static bool LinkAsset(IWritableHost _this,
        OrchDriveInfo srcDrive, Folder srcFolder,
        OrchDriveInfo dstDrive, Folder newFolder, Asset asset, string msg)
    {
        return LinkSharedEntity<Asset>(_this, srcDrive, srcFolder, dstDrive, newFolder,
            asset.Name, msg, "GetAssetLinkError", "LinkAssetError",
            () => srcDrive.GetFoldersForAsset(srcFolder, asset)?.AccessibleFolders?
                .Select(af => af.Id ?? 0).Where(id => id != srcFolder.Id).ToList(),
            f => dstDrive.Assets.Get(f),
            a => a.Name,
            a => a.Id ?? 0,
            (linkFolderId, entityId, newFolderId) => dstDrive.OrchAPISession.ShareAssetsToFolders(
                linkFolderId, new List<Int64> { entityId }, new List<Int64> { newFolderId }, new List<Int64>()));
    }

    internal static bool LinkQueue(IWritableHost _this,
        OrchDriveInfo srcDrive, Folder srcFolder,
        OrchDriveInfo dstDrive, Folder newFolder, QueueDefinition queue)
    {
        string msg = $"Sharing queue {queue.GetPSPath()}";
        return LinkSharedEntity<QueueDefinition>(_this, srcDrive, srcFolder, dstDrive, newFolder,
            queue.Name, msg, "GetQueueLinkError", "LinkQueueError",
            () => srcDrive.GetFoldersForQueue(srcFolder, queue)?.AccessibleFolders?
                .Select(af => af.Id ?? 0).Where(id => id != srcFolder.Id).ToList(),
            f => dstDrive.Queues.Get(f),
            q => q.Name,
            q => q.Id ?? 0,
            (linkFolderId, entityId, newFolderId) => dstDrive.OrchAPISession.ShareQueuesToFolders(
                linkFolderId, new List<Int64> { entityId }, new List<Int64> { newFolderId }, new List<Int64>()));
    }

    internal static bool LinkBucket(IWritableHost _this,
        OrchDriveInfo srcDrive, Folder srcFolder,
        OrchDriveInfo dstDrive, Folder newFolder, Bucket bucket)
    {
        string msg = $"Sharing bucket {bucket.GetPSPath()}";
        return LinkSharedEntity<Bucket>(_this, srcDrive, srcFolder, dstDrive, newFolder,
            bucket.Name, msg, "GetBucketLinkError", "LinkBucketError",
            () => srcDrive.GetFoldersForBucket(srcFolder, bucket)?.AccessibleFolders?
                .Select(af => af.Id ?? 0).Where(id => id != srcFolder.Id).ToList(),
            f => dstDrive.Buckets.Get(f),
            b => b.Name,
            b => b.Id ?? 0,
            (linkFolderId, entityId, newFolderId) => dstDrive.OrchAPISession.ShareBucketsToFolders(
                linkFolderId, new List<Int64> { entityId }, new List<Int64> { newFolderId }, new List<Int64>()));
    }

}
