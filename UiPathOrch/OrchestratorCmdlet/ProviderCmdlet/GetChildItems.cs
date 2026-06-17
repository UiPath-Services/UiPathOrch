using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using UiPath.OrchAPI;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Positional;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Entities.JsonConverter;

namespace UiPath.PowerShell.Core;

// ContainerCmdletProvider: child enumeration (GetChildItems/Names, HasChildItems) + CSV export.
public partial class OrchProvider
{
    protected override void GetChildItems(string path, bool recurse)
    {
        GetChildItems(path, recurse, 0);
    }

    // Returns the depth of the folder (number of slashes + 1)
    // Depth of "": 0
    // Depth of "folder": 1
    // Depth of "folder/sub": 2
    public static uint FolderDepth(string orchestratorPath)
    {
        if (string.IsNullOrEmpty(orchestratorPath))
        {
            return 0;
        }
        return (uint)orchestratorPath.Count(c => c == '/') + 1;
    }

    private static readonly string DefaultCsvName = "ExportedFolders.csv";

    // The Path column holds the PARENT path so Import-Csv | New-Item recreates each folder
    // under its parent (New-Item -Path <parent> -Name <leaf>). folder.FullName is the folder's
    // own path now, so derive the parent from FullyQualifiedName here.
    private static string FolderParentPsPath(OrchDriveInfo drive, Folder folder)
    {
        int idx = folder.FullyQualifiedName!.LastIndexOf('/');
        return idx != -1
            ? drive.NameColon + OrchDriveInfo.OrchProviderPathToPSPath(folder.FullyQualifiedName.Substring(0, idx))
            : drive.NameColonSeparator;
    }

    private string? ExportCsvFile(OrchDriveInfo drive, string exportCsv, Encoding? csvEncoding, IEnumerable<Folder> output)
    {
        Encoding encoding = csvEncoding ?? Encoding.UTF8;
        string[] headers = ["Path", "Name", "Description", "FeedType"];

        var (physicalCsvPath, providerCsvPath) = OrchestratorPSCmdlet.GenerateCsvFilePath(exportCsv, SessionState, DefaultCsvName);
        using var writer = OrchestratorPSCmdlet.WriteCsvHeader(physicalCsvPath, encoding, headers);
        if (writer is null) return null;

        // Write a data row for each folder
        foreach (var folder in output.Where(f => f.FolderType != "Personal"))
        {
            string[] line = [
                OrchestratorPSCmdlet.EscapeCsvValue(FolderParentPsPath(drive, folder)),
                OrchestratorPSCmdlet.EscapeCsvValue(folder.DisplayName!),
                OrchestratorPSCmdlet.EscapeCsvValue(folder.Description),
                OrchestratorPSCmdlet.EscapeCsvValue(folder.FeedType)
            ];
            writer.WriteCsvLine(line);
        }

        return providerCsvPath;
    }

    protected override void GetChildItems(string path, bool recurse, uint depth)
    {
        var drive = GetOrchDriveInfo(path);
        if (drive is null)
        {
            return;
        }

        drive.OrchAPISession.EnsureAuthenticated();

        // Check Entra ID warning (once per drive session).
        // The warning is set to PendingWarning (not displayed here) because GetChildItems
        // is also called during tab completion, where WriteWarning output would be silently lost.
        // PendingWarning is flushed by OrchCmdlets.BeginProcessing, which also sets EntraIdWarningChecked.
        if (!drive.OrchAPISession.EntraIdWarningChecked && drive.OrchAPISession.PendingWarning is null)
        {
            try
            {
                if (drive.OrchAPISession.AuthManager.IsNonEntraIdUser())
                {
                    var prtId = drive.GetPartitionGlobalId();
                    if (prtId is not null)
                    {
                        var authSetting = drive.PmAuthenticationSetting.Get();
                        if (authSetting?.authenticationSettingType == "aad")
                        {
                            drive.OrchAPISession.PendingWarning = $"[{drive.NameColon}] You are not signed in to the organization via Entra ID. Some operations may require organization-level access. Use Switch-OrchCurrentUser to sign in with a different account.";
                        }
                        else
                        {
                            drive.OrchAPISession.EntraIdWarningChecked = true;
                        }
                    }
                }
                else
                {
                    drive.OrchAPISession.EntraIdWarningChecked = true;
                }
            }
            catch { } // Swallow - don't block navigation for a warning
        }

        var parameters = DynamicParameters as GetChildItems_Parameters;
        if (parameters is not null && parameters.Reload.IsPresent)
        {
            drive._dicFolders = null;
            drive._dicFoldersForEnumFolders = null;
            drive.PersonalWorkspaces.ClearCache();
        }

        if (!recurse)
        {
            depth = 0;
        }

        //string orchPath = OrchDriveInfo.PSPathToOrchPath(path).ToLower();
        string orchPath = OrchDriveInfo.PSPathToOrchPath(path);
        uint currentDepth = FolderDepth(orchPath);

        HashSet<string> dupCheck = [];

        List<Folder> csvOutput = null;

        // Returns the parent portion of a FullyQualifiedName ("A/B/C" -> "A/B", "X" -> "").
        // Used to group siblings together so Format-Table's GroupBy keeps each Directory
        // section contiguous under -Recurse, instead of interleaving grandchildren between
        // sibling folders the way a flat alphabetical sort does.
        static string ParentOf(string fqn)
        {
            int idx = fqn.LastIndexOf('/');
            return idx < 0 ? "" : fqn[..idx];
        }

        try
        {
            // Collect matching folders first, then re-emit grouped by parent. Sort is stable
            // and only on parent path — sibling order within each parent comes from the
            // original _dicFolders sequence, which intentionally puts personal workspaces
            // before regular folders to match the Orchestrator web UI.
            var matched = new List<Folder>();
            string? orchPathStart = orchPath == "" ? null : orchPath + "/";

            foreach (var folder in drive.GetFolders())
            {
                if (Stopping) return;
                if (orchPathStart is not null &&
                    !folder.FullyQualifiedName!.StartsWith(orchPathStart, StringComparison.OrdinalIgnoreCase))
                    continue;

                uint folderDepth = FolderDepth(folder.FullyQualifiedName!);
                if (folderDepth - (currentDepth + 1) <= depth)
                {
                    matched.Add(folder);
                }
            }

            foreach (var folder in matched.OrderBy(
                f => ParentOf(f.FullyQualifiedName!), StringComparer.OrdinalIgnoreCase))
            {
                if (Stopping) return;

                string psPath = OrchDriveInfo.OrchProviderPathToPSPath(folder.FullyQualifiedName!);
                string psPathEscaped = drive.NameColon + PathTools.EscapePSText2(psPath);

                // A folder with the same name as a personal workspace may exist; suppress
                // duplicate names on BOTH paths so `dir` output and an exported CSV stay
                // consistent (duplicate CSV rows would also collide on Import-Csv | New-Item).
                if (!dupCheck.Add(psPathEscaped))
                {
                    WriteWarning($"The folder name '{folder.GetPSPath()}' (Id = {folder.Id}) is duplicated. This folder won't be listed.");
                    continue;
                }

                if (string.IsNullOrEmpty(parameters?.ExportCsv))
                {
                    WriteItemObject(folder, psPathEscaped, true);
                }
                else
                {
                    csvOutput ??= [];
                    csvOutput.Add(folder);
                }
            }
        }
        catch (PipelineStoppedException)
        {
            // `dir | Select -First N`, Ctrl+C, etc. PowerShell stops the upstream
            // by throwing this; surfacing it as an ErrorRecord would emit a stray
            // "pipeline has been stopped" message after the data the caller wanted.
            throw;
        }
        catch (Exception ex)
        {
            var errorRecord = new ErrorRecord(new OrchException(path, ex), "GetChildItemsError", ErrorCategory.InvalidOperation, path);
            WriteError(errorRecord);
        }

        if (!string.IsNullOrEmpty(parameters?.ExportCsv))
        {
            // csvOutput stays null when nothing matched (e.g. an empty folder). Export a
            // header-only CSV in that case instead of NRE-ing inside ExportCsvFile's Where().
            string? csvPath = ExportCsvFile(drive, parameters.ExportCsv, parameters.CsvEncoding,
                csvOutput ?? Enumerable.Empty<Folder>());
            if (csvPath is not null)
            {
                WriteWarning($"CSV has been exported as '{csvPath}'.");
            }
        }
    }

    protected override object GetChildItemsDynamicParameters(string path, bool recurse)
    {
        return new GetChildItems_Parameters();
    }

    //private static string EscapeWildcard(string path)
    //{
    //    return path
    //        .Replace("`", "``")
    //        .Replace("*", "`*")
    //        .Replace("?", "`?");
    //        //.Replace("[", "`[") // no need to escape [ and ]
    //        //.Replace("]", "`]");
    //}

    private static string UnescapeWildcard(string path)
    {
        return path
            //.Replace("``", "`")
            .Replace("`*", "*")
            .Replace("`?", "?");
        //.Replace("[", "`[") // no need to unescape [ and ]
        //.Replace("]", "`]");
    }

    // GetChildNames backs `Get-ChildItem -Name` and wildcard resolution (`cd t*`, `rmdir *`).
    // Per the provider contract it writes ONLY the child's name string as the item (not the
    // Folder object); the second WriteItemObject argument is the path the engine uses to build
    // the result's PSPath note-property.
    //
    // That path is emitted RAW (unescaped), mirroring the authoritative FileSystem provider,
    // whose GetChildNames does `WriteItemObject(fsinfo.Name, fsinfo.FullName, ...)` with the
    // unescaped FullName (PowerShell's FileSystemProvider.cs). Do NOT EscapePSText2 /
    // WildcardPattern.Escape it here: the PSPath built from this path binds to `-LiteralPath`
    // (`[Alias("PSPath")]`), and `EffectivePath` re-applies WildcardPattern.Escape on bind — so
    // a pre-escaped path would be escaped twice (e.g. a folder named `Fin*ce`) and fail to
    // resolve literally. Left raw, it round-trips: `dir | <cmdlet> -LiteralPath` and
    // `Get-Item -LiteralPath $f.PSPath` both resolve correctly. (EscapePSText2 in GetChildItems /
    // GetItem only escapes `* ?` and predates this; matching IT here would be the wrong target.)
    protected override void GetChildNames(string path, ReturnContainers returnContainers)
    {
        OrchDriveInfo? drive = GetOrchDriveInfo(path);
        if (drive is null)
        {
            return;
        }

        string ocPath = OrchDriveInfo.PSPathToOrchPath(path);

        if (ocPath == "")
        {
            // Direct children of the drive root = folders at depth 1, filtered the same way as
            // GetChildItems (by depth) so the two enumeration methods stay consistent. The
            // equivalent "!ParentId.HasValue" test also works here (GetFolders() masks every
            // top-level folder's ParentId to null), but matching GetChildItems is clearer.
            foreach (var folder in drive.GetFolders().Where(f =>
                f.FullyQualifiedName is not null && FolderDepth(f.FullyQualifiedName) == 1))
            {
                if (Stopping) return;
                string fullPath = drive.NameColon + OrchDriveInfo.OrchProviderPathToPSPath(folder.FullyQualifiedName!);
                WriteItemObject(folder.DisplayName!, fullPath, true);
            }
        }
        else
        {
            Folder parentFolder = drive.GetFolder(ocPath);
            Int64 parentFolderId = parentFolder?.Id ?? 0;

            foreach (var folder in drive.GetFolders().Where(f => f.ParentId == parentFolderId))
            {
                if (Stopping) return;
                string fullPath = drive.NameColon + OrchDriveInfo.OrchProviderPathToPSPath(folder.FullyQualifiedName!);
                WriteItemObject(folder.DisplayName!, fullPath, true);
            }
        }
    }

    // Always returning true seems to reduce accidental rmdir operations due to user error.
    protected override bool HasChildItems(string path)
    {
        // Report whether the folder actually has SUBFOLDERS. This must be accurate, not a constant:
        //  * PowerShell's wildcard path globber (Resolve-Path Orch1:\Shar*, Get-ChildItem Orch1:\*,
        //    and -Path <wildcard> which resolves through it) only enumerates a container's children
        //    when HasChildItems(container) is true — returning false here breaks all wildcard
        //    resolution.
        //  * Remove-Item's generic "...has children and the Recurse parameter was not specified"
        //    prompt is also driven by this; returning true unconditionally (the old behavior) made
        //    that prompt fire for empty folders too. With an accurate value, empty folders delete
        //    without that prompt, and RemoveItem adds its own content-aware confirmation.
        var drive = GetOrchDriveInfo(path);
        if (drive is null)
            return false;

        return HasSubfolders(drive, OrchDriveInfo.PSPathToOrchPath(path));
    }

    // True if the folder at the given fully-qualified Orchestrator path has any direct subfolder.
    // "" is the drive root, whose direct children are the depth-1 folders.
    private static bool HasSubfolders(OrchDriveInfo drive, string fqn)
    {
        uint childDepth = FolderDepth(fqn) + 1;
        string start = fqn + "/";
        return drive.GetFolders().Any(f =>
            f.FullyQualifiedName is not null &&
            FolderDepth(f.FullyQualifiedName) == childDepth &&
            (fqn.Length == 0 || (f.FullyQualifiedName + "/").StartsWith(start, StringComparison.OrdinalIgnoreCase)));
    }
}
