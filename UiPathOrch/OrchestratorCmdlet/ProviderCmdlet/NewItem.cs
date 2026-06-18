using System.Management.Automation;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Positional;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Core;

public partial class OrchProvider
{
    public class NewItem_DynamicParameters
    {
        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(StaticTextsCompleter<Processes_FolderHierarchy>))]
        public string? FeedType { get; set; }

        [Parameter(Position = 2, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(StaticTextsCompleter<DescriptionHere>))]
        public string? Description { get; set; }
    }

    protected override void NewItem(string path, string itemTypeName, object newItemValue)
    {
        //path = UnescapeWildcard(path);

        var dynamicParameters = DynamicParameters as NewItem_DynamicParameters ?? new NewItem_DynamicParameters();

        // Provider dynamic parameters (FeedType/Description) do NOT bind from the pipeline by
        // property name, so when a row is piped in (Import-Csv | New-Item) they arrive on the
        // whole pipeline object that lands on -Value instead. Pull them from there as a fallback
        // -- this is what makes the `dir -ExportCsv | ... | Import-Csv | New-Item` round-trip
        // preserve folder type and description (the same reason -Name is read from it below).
        var psValue = newItemValue as PSObject;
        if (psValue is not null)
        {
            if (string.IsNullOrEmpty(dynamicParameters.FeedType))
            {
                dynamicParameters.FeedType = psValue.Properties["FeedType"]?.Value as string;
            }
            if (string.IsNullOrEmpty(dynamicParameters.Description))
            {
                dynamicParameters.Description = psValue.Properties["Description"]?.Value as string;
            }
        }

        if (string.IsNullOrEmpty(dynamicParameters.FeedType))
        {
            dynamicParameters.FeedType = "Processes";
        }

        if (psValue is not null)
        {
            string name = psValue.Properties["Name"]?.Value as string;
            if (name is not null)
            {
                // TODO: Is Unescape() needed??
                //path = System.IO.Path.Combine(path, WildcardPattern.Unescape(name));
                path = System.IO.Path.Combine(path, name);
            }
        }

        if (ShouldProcess(path, "New Folder"))
        {
            var drive = GetOrchDriveInfo(path);

            if (drive is null)
            {
                return;
            }

            string parentPath = GetParentPath(path, "");
            if (!ItemExists(parentPath))
            {
                WriteError(new ErrorRecord(new OrchException(path, $"{parentPath} does not exist."), "NewItem", ErrorCategory.InvalidOperation, drive));
                return;
            }

            try
            {
                string displayName = LeafFromParent(path, parentPath);

                Int64? parentPathId;
                if (parentPath == System.IO.Path.DirectorySeparatorChar.ToString())
                {
                    parentPathId = null;
                }
                else
                {
                    string orchParentPath = OrchDriveInfo.PSPathToOrchPath(parentPath);
                    parentPathId = drive.GetFolder(orchParentPath)?.Id;
                }

                if (!string.IsNullOrEmpty(dynamicParameters?.FeedType) && (dynamicParameters.FeedType != "Processes" && dynamicParameters.FeedType != "FolderHierarchy"))
                {
                    WriteError(new ErrorRecord(new OrchException($"{path} ({dynamicParameters.FeedType})", "FeedType must be 'Processes' or 'FolderHierarchy'."), "NewFolderError", ErrorCategory.InvalidArgument, path));
                    return; // don't fall through to CreateFolder with an invalid FeedType (was a confusing double error)
                }

                Folder f = drive.OrchAPISession.CreateFolder(displayName,
                    dynamicParameters?.Description,
                    (parentPathId is null || parentPathId == 0) ? dynamicParameters?.FeedType : "Processes",
                    parentPathId);
                if (f is not null)
                {
                    f.FullName = path;
                    WriteItemObject(f, path, true);
                }
                // Clear rather than hand-insert the new folder: the next GetFolders re-fetches
                // the authoritative entry (server fields + correct sort) on demand.
                drive.ClearFolders();
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(path, ex), "NewFolderError", ErrorCategory.InvalidOperation, path));
            }
        }
    }

    // The new folder's name = the path minus its parent. The parent may or may not carry a
    // trailing separator: a NESTED parent resolves to "Orch1:\Foo" (no trailing sep), but the
    // DRIVE ROOT resolves to "Orch1:\" (WITH one, since the 1.9.x GetParentPath re-rooting fix).
    // So strip a single boundary separator only when it is actually present, instead of always
    // skipping one char. The old `Substring(parentPath.Length + 1)` assumed no trailing separator
    // and therefore dropped the FIRST character of every top-level folder name —
    // `New-Item Orch1:\TestFixture_Base` created "estFixture_Base". Pure + unit-tested.
    internal static string LeafFromParent(string path, string parentPath)
    {
        string rest = path.Substring(parentPath.Length);
        if (rest.Length > 0 && rest[0] == System.IO.Path.DirectorySeparatorChar)
        {
            rest = rest.Substring(1);
        }
        return rest;
    }

    protected override object NewItemDynamicParameters(string path, string itemTypeName, object newItemValue)
    {
        return new NewItem_DynamicParameters();
    }
}
