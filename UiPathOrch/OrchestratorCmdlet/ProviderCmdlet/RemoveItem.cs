using System.Management.Automation;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Core;

public partial class OrchProvider
{
    protected override void RemoveItem(string path, bool recurse)
    {
        var drive = GetOrchDriveInfo(path);
        if (drive is null)
        {
            return;
        }

        int index = path.LastIndexOf(System.IO.Path.DirectorySeparatorChar);
        if (index != -1)
        {
            string parentPart = path.Substring(0, index);
            string childPart = path.Substring(index);
            path = parentPart + UnescapeWildcard(childPart);
        }

        string ocPath = OrchDriveInfo.PSPathToOrchPath(path);
        Folder folder = drive.GetFolder(ocPath);
        if (folder is null)
        {
            drive.ClearAllCache();
            folder = drive.GetFolder(ocPath);
        }
        if (folder is null) return;

        if (ShouldProcess(path, "Remove Folder"))
        {
            // Content-aware confirmation, like the Orchestrator web delete dialog ("this folder
            // contains N assets, M processes... really delete?"). Skipped for -Force / -Recurse
            // (explicit opt-out) and for empty folders (no prompt at all). Counting only runs on
            // this interactive path, so scripts using -Recurse / -Force incur no extra API calls.
            // A folder that has subfolders already triggered PowerShell's generic "...has children
            // and the Recurse parameter was not specified" prompt (HasChildItems is true for it),
            // so don't ask again here. A folder with no subfolders leaves the engine silent, so warn
            // about the resources it contains (like the Orchestrator web delete dialog) before they
            // are removed without notice. -Force / -Recurse opt out.
            if (!Force && !recurse && !HasSubfolders(drive.GetFolders(), folder.FullyQualifiedName ?? string.Empty))
            {
                string? summary = DescribeFolderContents(drive, folder);
                if (summary is not null &&
                    !ShouldContinue(summary + " Are you sure you want to continue?", "Confirm folder deletion"))
                {
                    return;
                }
            }

            try
            {
                // For personal workspace folders, disable the owner's workspace first
                // Otherwise, the deleted workspace folder will be automatically recreated immediately
                if (folder.FolderType == "Personal")
                {
                    var personalWorkspaces = drive.PersonalWorkspaces.Get();
                    var targetPersonalWorkspace = personalWorkspaces.FirstOrDefault(p => p.Id == folder.Id);
                    if (targetPersonalWorkspace is not null)
                    {
                        drive.DisablePersonalWorkspace(targetPersonalWorkspace.OwnerId);
                    }
                }

                drive.OrchAPISession.RemoveFolder(folder.Id ?? 0);

                if (folder.FolderType == "Personal")
                {
                    drive.PersonalWorkspaces.ClearCache();
                }

                // The folder (and, on a cascading delete, its descendants) is gone — drop it
                // from the catalog directly. Targeted: no full clear + refetch.
                drive.RemoveFolderFromCache(folder);

                drive.ClearFolderCache(folder);
            }
            catch (Exception ex)
            {
                var errorRecord = new ErrorRecord(new OrchException(path, ex), "RemoveFolderError", ErrorCategory.InvalidOperation, folder);
                WriteError(errorRecord);
            }
        }
    }

    // Best-effort summary of a folder's contained RESOURCES for the deletion confirmation, in the
    // spirit of the Orchestrator web delete dialog. Subfolders are intentionally not counted here:
    // this is only consulted for folders with no subfolders (folders that have subfolders take
    // PowerShell's own "...has children..." prompt instead), so subfolders would always be zero.
    // Returns null when the folder holds none of the counted resources, so the caller skips the
    // prompt. Counts are best-effort: a resource type that can't be read (permissions, unsupported
    // API version) is skipped rather than blocking deletion.
    private string? DescribeFolderContents(OrchDriveInfo drive, Folder folder)
    {
        int SafeCount(Func<int> counter)
        {
            try { return counter(); }
            catch { return 0; /* best-effort: a resource type we can't read shouldn't block deletion */ }
        }

        int processes = SafeCount(() => drive.Releases.Get(folder).Count);
        int triggers = SafeCount(() => drive.Triggers.Get(folder).Count);
        int assets = SafeCount(() => drive.Assets.Get(folder).Count);
        int buckets = SafeCount(() => drive.Buckets.Get(folder).Count);
        int queues = SafeCount(() => drive.Queues.Get(folder).Count);
        int actionCatalogs = SafeCount(() => drive.ActionCatalogs.Get(folder).Count);

        if (processes + triggers + assets + buckets + queues + actionCatalogs == 0)
            return null;

        // Fixed inventory in the requested order: processes, triggers, assets, buckets, queues,
        // action catalogs. Every count is shown, including zeros.
        string counts =
            $"Processes: {processes}, Triggers: {triggers}, Assets: {assets}, " +
            $"Buckets: {buckets}, Queues: {queues}, Action Catalogs: {actionCatalogs}";
        return $"The folder '{folder.GetPSPath()}' is not empty ({counts}). " +
               "Deleting it permanently removes the folder and all of its contents.";
    }
}
