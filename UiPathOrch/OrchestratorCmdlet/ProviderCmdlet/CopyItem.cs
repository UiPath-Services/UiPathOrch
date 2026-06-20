using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using UiPath.PowerShell.Positional;
using System.Management.Automation;
using System.Management.Automation.Provider;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Core;

// =============================================================================
// CopyItem.cs -- Copy-Item provider implementation for UiPathOrch drives
// =============================================================================
//
// The Copy-Item cmdlet on an Orch drive (or DU / Tm shadow drive) delegates
// here when both source and destination resolve to folders or sub-paths of
// folders. The implementation walks the folder tree and, for each folder,
// copies every kind of entity it can find: folder users / machines, buckets,
// packages, processes, assets, queues, triggers, API triggers, test sets,
// test schedules, test data queues, action catalogs. Within a folder the
// stages are sequential because they have dependencies (buckets before
// processes, packages before triggers, etc.); subfolders are recursed when
// -Recurse is passed.
//
// Per-entity-kind Copy methods are intentionally long. Each one has its own
// destination-lookup rules (by name / by id / per-folder vs per-tenant), its
// own "if a matching entity already exists at the destination, link instead
// of duplicating" path (Link* methods), and its own field-massaging
// (zero out server-managed ids and timestamps, replace masked secret values
// with "!!!PLEASE UPDATE!!!" placeholders, remap role / user / machine ids
// via the FindDst* lookup family).
//
// Errors are funnelled through IWritableHost.WriteError; the cmdlet does
// NOT throw to the pipeline, so a single bad entity doesn't abort the whole
// Copy-Item operation. The wide `catch (Exception ex)` pattern is therefore
// intentional throughout this file -- each catch logs and continues.
//
// -----------------------------------------------------------------------------
// Method directory (search by name to navigate -- line numbers shift)
// -----------------------------------------------------------------------------
//
// Cross-cutting helpers
//   GetRelativeDstFolder       resolve dst folder mirroring src-relative path
//   CreatePmGroup              thin wrapper that catches + reports group-create errors
//   GetDirectChildFolders      filter direct children into a materialised List<>
//                              (safe to mutate during enumeration)
//
// FindDst* (destination-entity lookup; return null with WriteWarning if not found)
//   FindDstBucket, FindDstPmGroups, FindDstCredentialStore, FindDstUser,
//   FindDstRobot, FindDstRobotByUnattendedAccount, FindDstMachine,
//   FindDstSession, FindDstQueue, FindDstRelease, FindDstCalendar,
//   FindDstFolders, FindDstTestCase, FindDstTestSet
//
// Folder copy
//   CopyFolder                 create or reuse dst folder (appends
//                              " - Copy" / " - Copy (N)" on same-parent copy)
//
// Folder-scoped role / user / machine
//   FindDstRoles               name-lookup roles in dst tenant; filters out
//                              "Inherited" / "Tenant" types
//   CopyFolderUsers            per-folder user assignment + role migration
//                              + optional UserMappingCsv name resolution
//   AssignMyselfToFolder       ensure operator has Folder Administrator role on dst
//   UnassignMyselfAtNewFolder  inverse (currently disabled at call site -- see
//                              comment in CopyItemRecurse)
//   CopyFolderMachines         per-folder machine assignment (Unattended / Robot)
//
// Package / Process
//   CopyPackages               per-folder + tenant packages, version-aware upload
//   CopyProcesses              Releases. Per-process id redirection: EntryPointId is
//                              remapped across feeds via PackageEntryPoints (matched by
//                              entry-point Path), and RetentionBucketId via FindDstBucket.
//                              InputArguments (plain workflow input values) are copied
//                              verbatim; Arguments metadata is reset (the server re-derives
//                              it from the package); ResourceOverwrites is reset (it is a
//                              job-execution override, never persisted on a Release).
//
// Asset
//   LinkAsset                  short-circuit: link to an existing dst asset
//                              if a matching one is reachable via shared folders
//   CopyAssets                 create the asset; handle Credential / Secret
//                              value placeholders ("!!!PLEASE UPDATE!!!");
//                              migrate UserValues with FindDstUser / FindDstMachine
//
// Queue
//   LinkQueue                  short-circuit (counterpart of LinkAsset)
//   CopyQueueItem              (currently unused stub)
//   CopyQueues                 queue defs with retention settings
//
// Robot id migration helpers
//   MigrateExecutorRobots      remap RobotIds in a process's RobotExecutor[]
//   MigrateMachineRobots       remap RobotIds in a machine's MachineRobotSession[]
//
// Trigger
//   CopyTriggers               ProcessSchedule (cron / Queue trigger),
//                              retargeted to dst Release
//   CopyApiTriggers            ApiTrigger (HTTP-invoked process)
//
// Bucket
//   LinkBucket                 short-circuit (counterpart of LinkAsset / LinkQueue)
//   CopyBuckets                bucket defs only; bucket contents are managed
//                              by the underlying storage account and are NOT copied
//
// Test Manager
//   CopyTestSets               test sets, retargeted to dst Releases / TestCases
//   CopyTestDataQueueItems     per-queue items
//   CopyTestSetSchedules
//   CopyTestDataQueues
//
// Actions
//   CopyActionCatalogs         action catalog definitions
//
// Link-path FQN math (LinkAsset / Queue / Bucket support)
//   ComputeDstFqn              map src link target FQN to dst, anchored on
//                              src / dst root mismatch
//   WalkUp                     string surgery: ascend N path levels by
//                              trimming "/x" suffixes
//
// Cmdlet entry / orchestration
//   CopyItemRecurse            main recursive driver (13 stages per folder)
//   CopyItemDynamicParameters  attaches -ExcludeEntities / -UserMappingCsv
//   ShouldCopyTenantEntities   ShouldProcess gate for the root -> root
//                              tenant-entity copy at the start of CopyItem
//   CopyItem                   PSDriveProvider CopyItem entry point
//
// -----------------------------------------------------------------------------
// Audit notes
// -----------------------------------------------------------------------------
//
// * `catch (Exception ex)` blocks (~66 in this file) are intentional --
//   they convert exceptions into WriteError + continue so the broader copy
//   doesn't abort on one bad entity. Add a more specific catch only when
//   you have a behavioural reason; widening / removing one of these will
//   change failure semantics for the user.
//
// * TODOs in this file (grep "TODO:") flag spots where the author was
//   unsure -- mostly version-gating (`// TODO: Is this version number
//   correct?`) and resolver completeness (`// TODO: Is this implementation
//   incomplete?`). They are diligence flags, not known defects.
//
// =============================================================================

// Interface to handle Cmdlet class instances and CmdletProvider class instances uniformly.
// Convenient to implement in subclasses of Cmdlet and CmdletProvider.
public interface IWritableHost
{
    public void WriteError(ErrorRecord errorRecord);
    public void WriteWarning(string text);
    public void WriteProgress(ProgressRecord progressRecord);
    public bool ShouldProcess(string target, string action);

    // Variant that also reports WHY ShouldProcess returned false (notably -WhatIf vs a declined
    // -Confirm). Default returns no reason; OrchestratorPSCmdlet overrides it with the real reason
    // so callers can preview read-only side effects (e.g. dropped per-user UserValues) under -WhatIf.
    public bool ShouldProcess(string target, string action, out ShouldProcessReason reason)
    {
        reason = ShouldProcessReason.None;
        return ShouldProcess(target, action);
    }

    // True when the host's Write-Progress renderer handles East Asian Wide characters
    // correctly (PowerShell #21293 / PR #26185). Default is conservative -- assume it does
    // NOT, so ProgressReporter drops wide StatusDescription text. OrchestratorPSCmdlet and
    // OrchProvider override this using the live host version.
    public bool RendersWideProgress => false;
    //public void WriteObject(object sendToPipeline, bool enumerateCollection);
    //public void WriteObject(object sendToPipeline);
    //public void ThrowTerminatingError(ErrorRecord errorRecord);
}

// Error output to the console is consolidated in this class.
public static class IWritableHostExtensions
{
    // Some of this implementation should probably be refactored as Folder extension methods, but
    // When createIfMissing is true, a destination folder that doesn't exist yet
    // is created on demand (mirroring the source tree) instead of erroring —
    // a plain modern folder with no package feed ("Processes"), matching
    // Move-Orch*'s -Recurse mirror. createCache dedups the create/ShouldProcess
    // across the per-folder calls of a single Copy invocation (pass one dict for
    // the whole run). Only the Copy-Orch* cmdlets opt in; the Copy-Item provider
    // has its own folder-creation path and does not call this with createIfMissing.
    internal static Folder? GetRelativeDstFolder(this IWritableHost _this, Folder srcRootFolder, Folder srcFolder, OrchDriveInfo dstDrive, Folder dstRootFolder, bool includeRoot = false, bool createIfMissing = false, Dictionary<string, Folder?>? createCache = null)
    {
        var strDstRootFolder = dstRootFolder.FullyQualifiedName;
        //if (strDstRootFolder != "") strDstRootFolder += '/';

        // Get the relative path of srcFolder from srcRootFolder
        string relativePath = srcFolder.GetRelativePath(srcRootFolder);

        string strDstFolder = null;
        if (strDstRootFolder == "")
        {
            if (!includeRoot && relativePath == "")
            {
                _this.WriteError(new ErrorRecord(
                    new OrchException(dstDrive.NameColonSeparator, $"Folder entities cannot be copied to {dstDrive.NameColonSeparator}."),
                    "CopyFolderEntityToRootFolderError",
                    ErrorCategory.InvalidOperation,
                    dstDrive));
                return null;
            }
            strDstFolder = relativePath;
        }
        else
        {
            strDstFolder = (strDstRootFolder + '/' + relativePath).Trim('/');
        }

        if (string.IsNullOrEmpty(strDstFolder))
        {
            return dstDrive.RootFolder;
        }

        var dstFolder = dstDrive.GetFolders().FirstOrDefault(f => string.Compare(f.FullyQualifiedName, strDstFolder, StringComparison.OrdinalIgnoreCase) == 0);
        if (dstFolder is not null)
        {
            return dstFolder;
        }

        if (createIfMissing)
        {
            // Mirror the source tree: create the missing folder (and any missing
            // ancestors) instead of erroring.
            return _this.CreateMirroredDstFolder(dstDrive, dstRootFolder, relativePath, createCache ?? new Dictionary<string, Folder?>());
        }

        if ('/' != System.IO.Path.DirectorySeparatorChar)
        {
            strDstFolder = strDstFolder.Replace('/', System.IO.Path.DirectorySeparatorChar);
        }
        _this.WriteError(new ErrorRecord(
            new OrchException(srcFolder.GetPSPath(), $"{dstDrive.NameColonSeparator}{strDstFolder} does not exist."),
            "NoCorrespondingDstFolderError",
            ErrorCategory.InvalidOperation,
            dstDrive));
        return null;
    }

    // Resolve-or-create the destination folder mirroring relativePath under
    // dstRootFolder, creating each missing segment as a plain modern folder
    // (feed type "Processes" = no package feed). Folder creation is a state
    // change, so it's gated on ShouldProcess — which also emits the "New Folder"
    // -WhatIf line; when ShouldProcess returns false (-WhatIf / declined -Confirm)
    // a negative-Id placeholder is carried so the entity copy into the folder
    // still previews. cache dedups creates and prompts across the per-folder
    // calls of one Copy run. Mirror of MoveOrchEntityCmdletBase.ResolveMirroredDestination.
    private static Folder? CreateMirroredDstFolder(this IWritableHost _this, OrchDriveInfo dstDrive, Folder dstRootFolder, string relativePath, Dictionary<string, Folder?> cache)
    {
        if (string.IsNullOrEmpty(relativePath)) return dstRootFolder;

        Folder current = dstRootFolder;
        long placeholderId = -1;
        foreach (var segment in relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            string childFqn = string.IsNullOrEmpty(current.FullyQualifiedName)
                ? segment
                : $"{current.FullyQualifiedName}/{segment}";

            if (cache.TryGetValue(childFqn, out var cached))
            {
                if (cached is null) return null; // a prior attempt errored
                current = cached;
                continue;
            }

            long parentId = current.Id ?? 0;
            Folder? child = dstDrive.GetFolders()
                .FirstOrDefault(f => f.ParentId == parentId
                    && string.Compare(f.DisplayName, segment, StringComparison.OrdinalIgnoreCase) == 0);

            if (child is null)
            {
                if (string.IsNullOrEmpty(current.FullyQualifiedName))
                {
                    // A folder directly under the tenant root is a top-level
                    // folder whose package-feed setting is significant and can't
                    // be inferred from the source — don't auto-create it. Surface
                    // the same "does not exist" error so the user creates it
                    // explicitly with the feed they want.
                    _this.WriteError(new ErrorRecord(
                        new OrchException(dstDrive.NameColonSeparator,
                            $"{dstDrive.NameColonSeparator}{childFqn} does not exist. Create top-level folders explicitly — their package-feed setting can't be inferred."),
                        "NoCorrespondingDstFolderError", ErrorCategory.InvalidOperation, dstDrive));
                    cache[childFqn] = null;
                    return null;
                }

                string newPath = System.IO.Path.Combine(current.GetPSPath(), segment);
                if (!_this.ShouldProcess(newPath, "New Folder"))
                {
                    child = new Folder
                    {
                        Id = placeholderId--,
                        DisplayName = segment,
                        ParentId = parentId,
                        FullyQualifiedName = childFqn,
                        FullName = System.IO.Path.Combine(current.GetPSPath(), segment),
                    };
                }
                else
                {
                    try
                    {
                        child = dstDrive.OrchAPISession.CreateFolder(segment, null, "Processes", parentId);
                        if (child is not null)
                        {
                            child.FullName = System.IO.Path.Combine(current.GetPSPath(), segment);
                            dstDrive.AppendFolderToCache(child);
                        }
                    }
                    catch (Exception ex)
                    {
                        _this.WriteError(new ErrorRecord(new OrchException(newPath, ex),
                            "CreateDstFolderError", ErrorCategory.InvalidOperation, newPath));
                        child = null;
                    }
                    if (child is null)
                    {
                        cache[childFqn] = null;
                        return null;
                    }
                }
            }

            cache[childFqn] = child;
            current = child;
        }

        return current;
    }

    // If exception handling and error message output to the console are not needed, call drive.CreatePmGroup() directly.
    internal static PmGroup? CreatePmGroup(this IWritableHost _this, OrchDriveInfo drive, string? groupName, IEnumerable<string>? memberIds = null)
    {
        PmGroup ret = null;
        try
        {
            ret = drive.CreatePmGroup(groupName, memberIds);
        }
        catch (Exception ex)
        {
            _this.WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, $"Failed to create PmGroup '{groupName}'", ex), "AddPmGroupError", ErrorCategory.InvalidOperation, drive));
        }
        return ret;
    }
}

public class CopyItem_DynamicParameters
{
    [Parameter]
    public SwitchParameter ExcludeEntities { get; set; }

    [Parameter]
    public string? UserMappingCsv { get; set; }
}

// Copy-Item cmdlet
// TODO: Implement Set-ItemProperty as a means to update folder Description.
public partial class OrchProvider : NavigationCmdletProvider, IWritableHost
{
    private bool ExcludeEntities = false;

    // IWritableHost: report whether this host renders wide Write-Progress text correctly
    // (PowerShell #21293 / PR #26185), based on the live host version. See ProgressRendering.
    public bool RendersWideProgress
    {
        get
        {
            try { return ProgressRendering.HostRendersWideProgress(Host?.Version); }
            catch { return false; }
        }
    }

    // We want to clear the tenant package cache only once, but how to implement that..
    //private ReadOnlyCollection<Package> tenantPackagesCache = null;

    // This method must return List<Folder> instead of IEnumerable<Folder> because
    // creating folders during enumeration would break the enumeration
    private static List<Folder> GetDirectChildFolders(ReadOnlyCollection<Folder> folders, Folder parentFolder)
    {
        List<Folder> ret = new();
        foreach (var folder in folders)
        {
            if (folder.ParentId == parentFolder.Id)
            {
                ret.Add(folder);
            }
        }
        return ret;
    }

    private Folder? CopyFolder(
        OrchDriveInfo srcDrive, Folder srcFolder,
        OrchDriveInfo dstDrive, Folder dstFolder, string feedType,
        CancellationToken cancelToken)
    {
        string newFolderDisplayName = srcFolder.DisplayName;

        // When copying within the same parent folder, append " - Copy" to the destination folder name
        Folder srcParentFolder = srcDrive.GetParentFolder(srcFolder);
        if (srcParentFolder == dstFolder)
        {
            int index = 1;
            List<Folder> siblingFolders = dstDrive.GetFolders().Where(f => f.ParentId == dstFolder.Id).ToList();
            while (true)
            {
                cancelToken.ThrowIfCancellationRequested();

                if (index == 1)
                {
                    newFolderDisplayName = $"{srcFolder.DisplayName} - Copy";
                }
                else
                {
                    newFolderDisplayName = $"{srcFolder.DisplayName} - Copy ({index})";
                }
                // Break if no folder with the same name as newFolderDisplayName exists
                if (!siblingFolders.Any(f => f.DisplayName == newFolderDisplayName))
                {
                    break;
                }
                ++index;
            }
        }

        // If an existing folder with the same name exists, return it without creating a new one
        Folder targetFolder = dstDrive.GetFolders()
            .Where(f => f.ParentId == dstFolder.Id)
            .FirstOrDefault(f => string.Compare(f.DisplayName, newFolderDisplayName, StringComparison.OrdinalIgnoreCase) == 0);
        if (targetFolder is not null)
        {
            // This warning might be too noisy, so maybe we don't need it..
            //string target = targetFolder.GetPSPath();
            //WriteWarning($"The target folder exists. Copying the contents from \"{srcFolder.GetPSPath()}\"...");
            return targetFolder;
        }

        if (srcFolder.ProvisionType == "Manual")
        {
            WriteWarning($"The classic folder {srcFolder.GetPSPath()} is converted to modern folder {System.IO.Path.Combine(dstFolder.GetPSPath(), srcFolder.DisplayName!)}.");
        }

        var newFolder = dstDrive.OrchAPISession.CreateFolder(newFolderDisplayName!, srcFolder.Description, feedType, dstFolder.Id);
        if (newFolder is not null)
        {
            newFolder.FullName = System.IO.Path.Combine(dstFolder.GetPSPath(), newFolderDisplayName!);
            WriteItemObject(newFolder, newFolder.GetPSPath(), true);
            dstDrive.AppendFolderToCache(newFolder);
        }
        return newFolder;
    }

}
