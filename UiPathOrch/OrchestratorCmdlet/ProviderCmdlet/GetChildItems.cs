using System.Data;
using System.Management.Automation;
using System.Text;
using UiPath.OrchAPI;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;

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

    // The Entra-ID local-user advisory, worded to match the banner the Orchestrator
    // web UI shows for the same condition. AuthManager.BaseUrl is the org-scoped
    // sign-in URL (cloud: "https://cloud.uipath.com/{org}"); the "[drive:]" prefix
    // disambiguates which drive the notice is about when several are mounted.
    private static string BuildEntraIdSignInWarning(OrchDriveInfo drive)
    {
        string orgUrl = drive.OrchAPISession.AuthManager.BaseUrl;
        return $"[{drive.NameColon}] You are signed in with a local user account. "
            + "This organization supports Entra ID directory integration and single sign on. "
            + "To take advantage of all directory capabilities, like directory search and directory groups "
            + $"please sign out and sign in through the organization-specific URL: {orgUrl} in your browser "
            + "— then run 'Import-OrchConfig' here to sign in again with that account.";
    }

    protected override void GetChildItems(string path, bool recurse, uint depth)
    {
        var drive = GetOrchDriveInfo(path);
        if (drive is null)
        {
            return;
        }

        drive.OrchAPISession.EnsureAuthenticated();

        // Entra-ID local-user advisory (once per drive session). It is QUEUED into
        // PendingWarning, never WriteWarning'd here, because GetChildItems also runs
        // during `dir <path>` tab completion where a direct warning is lost; the
        // next OrchestratorPSCmdlet's BeginProcessing drains it.
        //
        // EntraIdWarningChecked latches the probe, but ONLY on a conclusive outcome.
        // A probe taken before the token / partition id / auth setting are available
        // is left un-latched so a later enumeration retries it — previously any of
        // those transient gaps (or an unrelated pending warning) permanently
        // suppressed the advisory.
        if (!drive.OrchAPISession.EntraIdWarningChecked)
        {
            try
            {
                switch (drive.OrchAPISession.AuthManager.GetEntraUserKind())
                {
                    case OrchestratorAuthManager.EntraUserKind.LocalUser:
                        if (drive.GetPartitionGlobalId() is not null)
                        {
                            var authSetting = drive.PmAuthenticationSetting.Get();
                            if (authSetting is not null) // only conclusive once fetched
                            {
                                drive.OrchAPISession.EntraIdWarningChecked = true;
                                if (authSetting.authenticationSettingType == "aad")
                                {
                                    drive.OrchAPISession.AppendPendingWarning(BuildEntraIdSignInWarning(drive));
                                }
                            }
                            // authSetting null (transient / missing scope): retry on a later enumeration.
                        }
                        // partition id not yet known: retry on a later enumeration.
                        break;

                    case OrchestratorAuthManager.EntraUserKind.EntraOrNotApplicable:
                        // Conclusive: signed in via Entra ID, or a principal (Confidential
                        // App / robot) the advisory doesn't apply to.
                        drive.OrchAPISession.EntraIdWarningChecked = true;
                        break;

                    // Unknown: no parseable token yet — leave un-latched and retry.
                }
            }
            catch { } // Swallow - don't block navigation for a warning
        }

        var parameters = DynamicParameters as GetChildItems_Parameters;
        if (parameters is not null && parameters.Reload.IsPresent)
        {
            drive.ClearFolders();
            drive.PersonalWorkspaces.ClearCache();
        }

        if (!recurse)
        {
            depth = 0;
        }

        //string orchPath = OrchDriveInfo.PSPathToOrchPath(path).ToLower();
        string orchPath = OrchDriveInfo.PSPathToOrchPath(path);

        HashSet<string> dupCheck = [];

        List<Folder> csvOutput = null;

        try
        {
            foreach (var folder in SelectChildItems(drive.GetFolders(), orchPath, depth))
            {
                if (Stopping) return;

                // Emit the PSPath RAW (drive-qualified, but NOT wildcard-escaped), identical to
                // GetChildNames and GetItem. The PSPath binds to -LiteralPath ([Alias("PSPath")]),
                // whose EffectivePath re-applies WildcardPattern.Escape on bind; pre-escaping here
                // would escape twice, so a folder named e.g. "Fin*ce" would fail to resolve back to
                // itself via `dir | <cmdlet> -LiteralPath` or `Get-Item -LiteralPath $f.PSPath`.
                string psPathRaw = drive.NameColon + OrchDriveInfo.OrchProviderPathToPSPath(folder.FullyQualifiedName!);

                // A folder with the same name as a personal workspace may exist; suppress
                // duplicate names on BOTH paths so `dir` output and an exported CSV stay
                // consistent (duplicate CSV rows would also collide on Import-Csv | New-Item).
                if (!dupCheck.Add(psPathRaw))
                {
                    WriteWarning($"The folder name '{folder.GetPSPath()}' (Id = {folder.Id}) is duplicated. This folder won't be listed.");
                    continue;
                }

                if (string.IsNullOrEmpty(parameters?.ExportCsv))
                {
                    WriteItemObject(folder, psPathRaw, true);
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

    // Returns the parent portion of a FullyQualifiedName ("A/B/C" -> "A/B", "X" -> "").
    // Used to group siblings together so Format-Table's GroupBy keeps each Directory section
    // contiguous under -Recurse, instead of interleaving grandchildren between sibling folders
    // the way a flat alphabetical sort does.
    internal static string FqnParent(string fqn)
    {
        int idx = fqn.LastIndexOf('/');
        return idx < 0 ? "" : fqn[..idx];
    }

    // The depth-filter + parent-grouped ordering behind GetChildItems / dir, extracted pure over
    // the catalog so it is unit-testable without a live drive. Keeps only folders under orchPath
    // (the drive root when ""), within `depth` extra levels (0 = direct children), then re-emits
    // grouped by parent: the sort is STABLE and ONLY on parent path, so sibling order within each
    // parent comes from the catalog sequence (which intentionally puts personal workspaces before
    // regular folders to match the Orchestrator web UI). Per-item Stopping is not checked here —
    // this is an in-memory pass over an already-fetched list; the cancellable work (WriteItemObject)
    // stays in the caller's output loop, which keeps its Stopping check.
    internal static List<Folder> SelectChildItems(IEnumerable<Folder> folders, string orchPath, uint depth)
    {
        uint currentDepth = FolderDepth(orchPath);
        string? orchPathStart = orchPath == "" ? null : orchPath + "/";

        var matched = new List<Folder>();
        foreach (var folder in folders)
        {
            if (orchPathStart is not null &&
                !folder.FullyQualifiedName!.StartsWith(orchPathStart, StringComparison.OrdinalIgnoreCase))
                continue;

            uint folderDepth = FolderDepth(folder.FullyQualifiedName!);
            if (folderDepth - (currentDepth + 1) <= depth)
            {
                matched.Add(folder);
            }
        }

        return matched
            .OrderBy(f => FqnParent(f.FullyQualifiedName!), StringComparer.OrdinalIgnoreCase)
            .ToList();
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
    // unescaped FullName (PowerShell's FileSystemProvider.cs). Do NOT WildcardPattern.Escape it
    // here: the PSPath built from this path binds to `-LiteralPath`
    // (`[Alias("PSPath")]`), and `EffectivePath` re-applies WildcardPattern.Escape on bind — so
    // a pre-escaped path would be escaped twice (e.g. a folder named `Fin*ce`) and fail to
    // resolve literally. Left raw, it round-trips: `dir | <cmdlet> -LiteralPath` and
    // `Get-Item -LiteralPath $f.PSPath` both resolve correctly. GetChildItems and GetItem now emit
    // the PSPath the same RAW way (they previously wildcard-escaped the PSPath, which broke exactly
    // this round-trip for `* ?` names) — all four emit paths agree.
    protected override void GetChildNames(string path, ReturnContainers returnContainers)
    {
        OrchDriveInfo? drive = GetOrchDriveInfo(path);
        if (drive is null)
        {
            return;
        }

        string ocPath = OrchDriveInfo.PSPathToOrchPath(path);

        // The parent-id lookup only matters for the non-root branch; skip it at the root so
        // GetFolder (a catalog probe) is not invoked there — matching the former code exactly.
        Int64 parentFolderId = ocPath == "" ? 0 : (drive.GetFolder(ocPath)?.Id ?? 0);

        foreach (var folder in SelectChildNames(drive.GetFolders(), ocPath, parentFolderId))
        {
            if (Stopping) return;
            string fullPath = drive.NameColon + OrchDriveInfo.OrchProviderPathToPSPath(folder.FullyQualifiedName!);
            WriteItemObject(folder.DisplayName!, fullPath, true);
        }
    }

    // Selects the direct children of a folder for GetChildNames (-Name / wildcard resolution).
    // Two branches, both pure over the catalog:
    //  * root (ocPath == ""): folders at depth 1. The equivalent "!ParentId.HasValue" test also
    //    works here (GetFolders() masks every top-level folder's ParentId to null), but matching
    //    GetChildItems' depth filter keeps the two enumeration methods consistent.
    //  * non-root: folders whose ParentId equals the resolved parent folder's id.
    // Source order is preserved (Where is stable), so siblings keep the catalog's web-UI order.
    internal static IEnumerable<Folder> SelectChildNames(IEnumerable<Folder> folders, string ocPath, Int64 parentFolderId)
    {
        if (ocPath == "")
        {
            return folders.Where(f =>
                f.FullyQualifiedName is not null && FolderDepth(f.FullyQualifiedName) == 1);
        }
        return folders.Where(f => f.ParentId == parentFolderId);
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

        return HasSubfolders(drive.GetFolders(), OrchDriveInfo.PSPathToOrchPath(path));
    }

    // True if the folder at the given fully-qualified Orchestrator path has any direct subfolder.
    // "" is the drive root, whose direct children are the depth-1 folders. Pure over the folder
    // list (not the drive) so HasChildItems' accuracy — which the wildcard path globber and
    // Remove-Item's recurse prompt both depend on — is unit-testable without a live drive.
    internal static bool HasSubfolders(IEnumerable<Folder> folders, string fqn)
    {
        uint childDepth = FolderDepth(fqn) + 1;
        string start = fqn + "/";
        return folders.Any(f =>
            f.FullyQualifiedName is not null &&
            FolderDepth(f.FullyQualifiedName) == childDepth &&
            (fqn.Length == 0 || (f.FullyQualifiedName + "/").StartsWith(start, StringComparison.OrdinalIgnoreCase)));
    }
}
