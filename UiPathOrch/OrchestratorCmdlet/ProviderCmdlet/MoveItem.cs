using System.Management.Automation;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Core;

public partial class OrchProvider
{
    protected override void MoveItem(string path, string destination)
    {
        var drive = GetOrchDriveInfo(path);
        if (drive is null)
        {
            return;
        }

        // Move-Item is a single same-drive operation (MoveFolder is one API call) — unlike
        // Copy-Item, which supports cross-drive transfers. If the destination explicitly names a
        // different drive, reject it: otherwise PSPathToOrchPath strips the drive qualifier and the
        // destination is silently reinterpreted on the source drive — a no-such-folder error at
        // best, a move into a same-named folder on the wrong drive at worst.
        var dstDrive = ExtractOrchDriveInfo(destination);
        if (dstDrive is not null && IsCrossDriveMovePure(drive.Name, dstDrive.Name))
        {
            WriteError(new ErrorRecord(new OrchException(destination, $"Cannot move across drives: '{path}' is on {drive.NameColon} but destination '{destination}' is on {dstDrive.NameColon}. Move-Item works within a single drive; use Copy-Item for cross-drive transfers."), "MoveItemError", ErrorCategory.InvalidArgument, destination));
            return;
        }

        if (ShouldProcess(path, "Move Folder"))
        {
            Folder srcFolder = null;
            try
            {
                string ocPath = OrchDriveInfo.PSPathToOrchPath(path);
                srcFolder = drive.GetFolder(ocPath);
                if (srcFolder is null)
                {
                    WriteError(new ErrorRecord(new OrchException(path, $"{drive.NameColon} does not have folder '{path}'."), "MoveItemError", ErrorCategory.ObjectNotFound, path));
                    return;
                }

                // The destination is the folder that becomes the new parent. It must already exist
                // (Move-Item does not create it). Resolve it on the source drive — cross-drive moves
                // aren't a single API call, so a different-drive destination won't resolve here and
                // surfaces as this clear error rather than an NRE.
                string ocDestination = OrchDriveInfo.PSPathToOrchPath(destination);
                Folder dstFolder = drive.GetFolder(ocDestination);
                if (dstFolder is null)
                {
                    WriteError(new ErrorRecord(new OrchException(destination, $"{drive.NameColon} does not have destination folder '{destination}'."), "MoveItemError", ErrorCategory.ObjectNotFound, destination));
                    return;
                }

                // Reject moving a folder into itself or one of its own descendants — either would
                // create a cycle (the moved subtree would become its own ancestor).
                bool intoSelfOrDescendant = srcFolder == dstFolder
                    || IsMoveIntoSelfOrDescendantPure(srcFolder.FullyQualifiedName, dstFolder.FullyQualifiedName);
                if (intoSelfOrDescendant)
                {
                    WriteError(new ErrorRecord(new OrchException(destination, $"Cannot move folder '{path}' into itself or one of its descendants."), "MoveItemError", ErrorCategory.InvalidOperation, destination));
                    return;
                }

                drive.OrchAPISession.MoveFolder(srcFolder.Id ?? 0, dstFolder.Id);
                // The moved subtree's paths all change — clear and let GetFolders re-fetch.
                drive.ClearFolders();
                drive.ClearFolderCache(srcFolder);
            }
            catch (Exception ex)
            {
                //int index = path.LastIndexOf('\\');
                //string pathName;
                //if (index != -1)
                //    pathName = path.Substring(index);
                //else
                //    pathName = path;
                WriteError(new ErrorRecord(new OrchException(path, ex), "MoveItemError", ErrorCategory.InvalidOperation, srcFolder));
            }
        }
    }

    // Pure cross-drive test for MoveItem (extracted so it is unit-testable without a live drive).
    // Move-Item is single-drive, unlike Copy-Item: a move is cross-drive only when the destination
    // explicitly names a drive different from the source. A null/empty destination drive name
    // (unqualified path / unknown drive) is treated as same-drive and resolved on the source drive.
    internal static bool IsCrossDriveMovePure(string srcDriveName, string? dstDriveName)
        => !string.IsNullOrEmpty(dstDriveName)
            && !string.Equals(dstDriveName, srcDriveName, StringComparison.OrdinalIgnoreCase);

    // Pure self/descendant test for MoveItem (extracted so it is unit-testable). True when the
    // destination folder is the source folder itself or one of its descendants, compared by
    // fully-qualified name with a '/' boundary so a sibling like "Foo2" is not mistaken for a
    // descendant of "Foo". A null/empty source FQN (e.g. root) is never a match here.
    internal static bool IsMoveIntoSelfOrDescendantPure(string? srcFqn, string? dstFqn)
        => !string.IsNullOrEmpty(srcFqn) && dstFqn is not null
            && (string.Equals(dstFqn, srcFqn, StringComparison.OrdinalIgnoreCase)
                || dstFqn.StartsWith(srcFqn + "/", StringComparison.OrdinalIgnoreCase));
}
