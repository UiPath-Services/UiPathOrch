using System.Management.Automation;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Core;

public partial class OrchProvider
{
    protected override void RenameItem(string path, string newName)
    {
        //path = UnescapeWildcard(path);
        var drive = GetOrchDriveInfo(path);
        if (drive is null)
        {
            return;
        }

        // -NewName must be a leaf, not a path (Rename-Item renames in place, it does not move).
        // Reduce ".\Shared2" -> "Shared2"; reject names that point elsewhere (e.g. "..\Shared2").
        string? leaf = PathTools.RenameLeaf(path, newName);
        if (leaf is null)
        {
            WriteError(new ErrorRecord(new OrchException(path, $"'{newName}' is not a valid new folder name. Supply a leaf name, not a path (Rename-Item renames in place; use Move-Item to move). Example: Rename-Item .\\Shared Shared2."), "RenameFolderError", ErrorCategory.InvalidArgument, path));
            return;
        }
        newName = leaf;

        string target = $"Item: {path} Destination: {System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path) ?? "", newName)}";
        if (ShouldProcess(target, "Rename Folder"))
        {
            try
            {
                string orchPath = OrchDriveInfo.PSPathToOrchPath(path);
                Folder? folder = drive.GetFolder(orchPath);
                if (folder is null)
                    return;
                drive.OrchAPISession.EditFolder(folder, newName!);
                drive._dicFolders = null;
                drive._dicFoldersForEnumFolders = null;

                //if (DynamicParameters is RuntimeDefinedParameterDictionary parameters)
                //{
                //    string orchPath = OrchDriveInfo.PSPathToOrchPath(path);
                //    Folder? folder = drive.GetFolder(orchPath);
                //    if (folder is null)
                //        return;

                //    string description = parameters["Description"].Value as string;
                //    if (folder.DisplayName == newName && folder.Description == description)
                //        return;

                //    newName ??= folder.DisplayName;
                //    description ??= folder.Description;

                //    drive.OrchAPISession.EditFolder(folder, newName!, description!);
                //    drive._dicFolders = null;
                //    drive._dicFoldersForEnumFolders = null;
                //}
            }
            catch (Exception ex)
            {
                var errorRecord = new ErrorRecord(new OrchException(path, ex), "RenameFolderError", ErrorCategory.InvalidOperation, path);
                WriteError(errorRecord);
            }
        }
    }
}
