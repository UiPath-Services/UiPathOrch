using System.Collections.ObjectModel;
using System.Management.Automation;
using UiPath.PowerShell.Commands;

namespace UiPath.PowerShell.Core;

// IPropertyCmdletProvider: folder Description via Get/Set/Clear-ItemProperty.
public partial class OrchProvider
{
    // Folders expose two text fields: DisplayName (read-only here — change it with Rename-Item)
    // and Description. Only Description is settable, because the Orchestrator folder PUT accepts
    // changes to DisplayName and Description only and DisplayName already has a dedicated verb.

    public void GetProperty(string path, Collection<string>? providerSpecificPickList)
    {
        var drive = GetOrchDriveInfo(path);
        if (drive is null) return;

        var folder = drive.GetFolder(OrchDriveInfo.PSPathToOrchPath(path));
        if (folder is null)
        {
            WriteError(new ErrorRecord(new OrchException(path, $"{drive.NameColon} does not have folder '{path}'."), "ItemDoesNotExist", ErrorCategory.ObjectNotFound, path));
            return;
        }

        (string Name, object? Value)[] available =
        [
            ("Description", folder.Description),
            ("DisplayName", folder.DisplayName),
        ];

        var result = new PSObject();
        bool any = false;
        if (providerSpecificPickList is null || providerSpecificPickList.Count == 0)
        {
            foreach (var (name, value) in available) { result.Properties.Add(new PSNoteProperty(name, value)); any = true; }
        }
        else
        {
            foreach (var requested in providerSpecificPickList)
            {
                if (string.IsNullOrEmpty(requested)) continue;
                var match = available.FirstOrDefault(p => string.Equals(p.Name, requested, StringComparison.OrdinalIgnoreCase));
                if (match.Name is not null)
                {
                    result.Properties.Add(new PSNoteProperty(match.Name, match.Value));
                    any = true;
                }
                else
                {
                    WriteError(new ErrorRecord(new OrchException(path, $"A folder has no '{requested}' property. Available: Description, DisplayName."), "PropertyNotFound", ErrorCategory.InvalidArgument, requested));
                }
            }
        }

        if (any) WritePropertyObject(result, path);
    }

    public void SetProperty(string path, PSObject propertyValue)
    {
        if (propertyValue is null) return;

        var drive = GetOrchDriveInfo(path);
        if (drive is null) return;

        var folder = drive.GetFolder(OrchDriveInfo.PSPathToOrchPath(path));
        if (folder is null)
        {
            WriteError(new ErrorRecord(new OrchException(path, $"{drive.NameColon} does not have folder '{path}'."), "ItemDoesNotExist", ErrorCategory.ObjectNotFound, path));
            return;
        }

        string? newDescription = null;
        bool found = false;
        foreach (PSMemberInfo property in propertyValue.Properties)
        {
            if (string.Equals(property.Name, "Description", StringComparison.OrdinalIgnoreCase))
            {
                newDescription = property.Value as string ?? property.Value?.ToString() ?? string.Empty;
                found = true;
            }
            else if (string.Equals(property.Name, "DisplayName", StringComparison.OrdinalIgnoreCase))
            {
                WriteError(new ErrorRecord(new OrchException(path, "A folder's DisplayName is changed with Rename-Item, not Set-ItemProperty."), "PropertyNotSettable", ErrorCategory.InvalidArgument, property.Name));
            }
            else
            {
                WriteError(new ErrorRecord(new OrchException(path, $"A folder's '{property.Name}' property cannot be set. Only Description is settable."), "PropertyNotSettable", ErrorCategory.InvalidArgument, property.Name));
            }
        }

        if (!found) return;

        if (string.Equals(folder.FolderType, "Personal", StringComparison.OrdinalIgnoreCase))
        {
            WriteError(new ErrorRecord(
                new OrchException(path, "A personal workspace folder's Description cannot be set — Orchestrator does not allow editing a personal workspace through the folder API."),
                "PersonalWorkspaceNotEditable", ErrorCategory.InvalidOperation, path));
            return;
        }

        if (ShouldProcess(path, "Set Description"))
        {
            try
            {
                drive.OrchAPISession.EditFolder(folder, folder.DisplayName!, newDescription ?? string.Empty);
                drive.ClearFolders();

                var result = new PSObject();
                result.Properties.Add(new PSNoteProperty("Description", newDescription));
                WritePropertyObject(result, path);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(path, ex), "SetPropertyError", ErrorCategory.InvalidOperation, path));
            }
        }
    }

    public void ClearProperty(string path, Collection<string> propertyToClear)
    {
        var drive = GetOrchDriveInfo(path);
        if (drive is null) return;

        var folder = drive.GetFolder(OrchDriveInfo.PSPathToOrchPath(path));
        if (folder is null)
        {
            WriteError(new ErrorRecord(new OrchException(path, $"{drive.NameColon} does not have folder '{path}'."), "ItemDoesNotExist", ErrorCategory.ObjectNotFound, path));
            return;
        }

        // Default (no pick list) clears Description. Any other named property is rejected.
        bool clearDescription = providerSpecificPickListIsEmptyOrHasDescription(propertyToClear);
        if (propertyToClear is not null)
        {
            foreach (var p in propertyToClear)
            {
                if (!string.IsNullOrEmpty(p) && !string.Equals(p, "Description", StringComparison.OrdinalIgnoreCase))
                {
                    WriteError(new ErrorRecord(new OrchException(path, $"A folder's '{p}' property cannot be cleared. Only Description is clearable."), "PropertyNotClearable", ErrorCategory.InvalidArgument, p));
                }
            }
        }

        if (!clearDescription) return;

        if (string.Equals(folder.FolderType, "Personal", StringComparison.OrdinalIgnoreCase))
        {
            WriteError(new ErrorRecord(
                new OrchException(path, "A personal workspace folder's Description cannot be cleared — Orchestrator does not allow editing a personal workspace through the folder API."),
                "PersonalWorkspaceNotEditable", ErrorCategory.InvalidOperation, path));
            return;
        }

        if (ShouldProcess(path, "Clear Description"))
        {
            try
            {
                drive.OrchAPISession.EditFolder(folder, folder.DisplayName!, string.Empty);
                drive.ClearFolders();

                var result = new PSObject();
                result.Properties.Add(new PSNoteProperty("Description", string.Empty));
                WritePropertyObject(result, path);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(path, ex), "ClearPropertyError", ErrorCategory.InvalidOperation, path));
            }
        }

        static bool providerSpecificPickListIsEmptyOrHasDescription(Collection<string>? list) =>
            list is null || list.Count == 0 ||
            list.Any(p => string.Equals(p, "Description", StringComparison.OrdinalIgnoreCase));
    }

    public object? GetPropertyDynamicParameters(string path, Collection<string>? providerSpecificPickList) => null;

    public object? SetPropertyDynamicParameters(string path, PSObject propertyValue) => null;

    public object? ClearPropertyDynamicParameters(string path, Collection<string> propertyToClear) => null;
}
