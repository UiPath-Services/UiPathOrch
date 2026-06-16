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

    // Per-src-role resolution outcome from ResolveDstRolesPure.
    internal enum FindDstRoleResult
    {
        Resolved,                       // matched a dst folder-scope role; Id appended to result list
        SkippedAsInherited,             // src role marked Origin = "Inherited" -> caller skips silently
        SkippedAsTenantRole,            // matched but dst role's Type == "Tenant" -> excluded from folder roles
        NotFoundInDstTenant,            // no dst role with matching Id (same-drive) or Name (cross-drive)
    }

    internal sealed record FindDstRoleEntry(SimpleRole SrcRole, Role? DstRole, FindDstRoleResult Result);

    // Pure version of FindDstRoles. Caller supplies the dst tenant role list
    // (fetched and possibly null-checked outside) so the matching policy is
    // exercisable without standing up a real OrchDriveInfo.
    //
    // Policy (preserved verbatim):
    //   - srcRoles with Origin == "Inherited" are silently skipped.
    //   - When isSameDrive, match by Id (safer for renamed roles).
    //   - When cross-drive, match by Name (case-insensitive).
    //   - A matched dst role with Type == "Tenant" is intentionally NOT
    //     added to the returned folder-role-id list -- tenant roles are
    //     surfaced in classic-folder user payloads but cannot legally be
    //     assigned as folder-scope roles.
    internal static List<FindDstRoleEntry> ResolveDstRolesPure(
        IEnumerable<SimpleRole> srcRoles,
        IEnumerable<Role> dstTenantRoles,
        bool isSameDrive)
    {
        var entries = new List<FindDstRoleEntry>();
        var dstList = dstTenantRoles.ToList();

        foreach (var sr in srcRoles)
        {
            if (sr.Origin == "Inherited")
            {
                entries.Add(new FindDstRoleEntry(sr, null, FindDstRoleResult.SkippedAsInherited));
                continue;
            }

            Role? matched = isSameDrive
                ? dstList.FirstOrDefault(r => r.Id == sr.Id)
                : dstList.FirstOrDefault(r => string.Equals(r.Name, sr.Name, StringComparison.OrdinalIgnoreCase));

            if (matched is null)
            {
                entries.Add(new FindDstRoleEntry(sr, null, FindDstRoleResult.NotFoundInDstTenant));
                continue;
            }

            if (matched.Type == "Tenant")
            {
                entries.Add(new FindDstRoleEntry(sr, matched, FindDstRoleResult.SkippedAsTenantRole));
                continue;
            }

            entries.Add(new FindDstRoleEntry(sr, matched, FindDstRoleResult.Resolved));
        }
        return entries;
    }

    internal static List<Int64>? FindDstRoles(IWritableHost _this,
        OrchDriveInfo srcDrive, IEnumerable<SimpleRole> srcRoleIds,
        OrchDriveInfo dstDrive, string msg)
    {
        if (srcRoleIds is null || !srcRoleIds.Any()) return null;

        ICollection<Role> dstTenantRoles = null;
        try
        {
            dstTenantRoles = dstDrive.Roles.Get();
        }
        catch (Exception ex)
        {
            _this.WriteError(new ErrorRecord(new OrchException(dstDrive.NameColonSeparator, msg, ex), "MigrateRoleIdError", ErrorCategory.InvalidOperation, dstDrive));
            return null;
        }

        var entries = ResolveDstRolesPure(srcRoleIds, dstTenantRoles, isSameDrive: srcDrive == dstDrive);

        var retRoles = new List<Int64>();
        foreach (var entry in entries)
        {
            switch (entry.Result)
            {
                case FindDstRoleResult.NotFoundInDstTenant:
                    _this.WriteWarning($"{msg}: {dstDrive.NameColon} does not have role with Name = '{entry.SrcRole.Name}'.");
                    break;
                case FindDstRoleResult.Resolved:
                    retRoles.Add(entry.DstRole!.Id ?? 0);
                    break;
                    // SkippedAsInherited / SkippedAsTenantRole: silent (preserved from original).
            }
        }
        return retRoles;
    }

    // Per-name outcome from ResolveDstDirectoryUserPure.
    internal enum FindDstDirectoryUserResult
    {
        Resolved,    // exactly one matching directory object; resolved is non-null
        NotFound,    // no matching directory object -> caller falls back to email search
        Duplicated,  // more than one match -> ambiguous; caller emits an error and skips
    }

    // Pure destination-directory resolution extracted from CopyFolderUsers so the
    // matching policy is unit-testable without a live OrchAPISession. The IO
    // wrapper (CopyFolderUsers) still performs the SearchDirectory call, the
    // "Duplicated" WriteError, the email fallback on NotFound, and the final
    // folder assignment.
    //
    // searchResults is what dstDrive.SearchDirectory(searchName) returned. NOTE:
    // SearchDirectory hits /api/DirectoryService/SearchForUsersAndGroups?prefix=,
    // which is a PREFIX search -- the server returns every user/group whose name
    // STARTS WITH searchName, not just exact hits. searchName is passed so the
    // post-filter can narrow that prefix set down to the intended object.
    internal static (DirectoryObject? resolved, FindDstDirectoryUserResult result) ResolveDstDirectoryUserPure(
        IEnumerable<DirectoryObject>? searchResults,
        string searchName,
        int type)
    {
        var matches = (searchResults ?? Enumerable.Empty<DirectoryObject>())
            .Where(u => u is not null && u.type == type)
            // SearchForUsersAndGroups is a PREFIX search; the email-fallback branch
            // and TestUserMappingCsv both narrow back to an exact identityName hit,
            // so do the same here -- otherwise a lone prefix sibling resolves to the
            // wrong principal, or several siblings raise a spurious "Duplicated".
            .Where(u => string.Compare(u.identityName, searchName, StringComparison.OrdinalIgnoreCase) == 0)
            .ToList();

        if (matches.Count == 1) return (matches[0], FindDstDirectoryUserResult.Resolved);
        if (matches.Count > 1) return (matches[0], FindDstDirectoryUserResult.Duplicated);
        return (null, FindDstDirectoryUserResult.NotFound);
    }

    internal static void CopyFolderUsers(IWritableHost _this,
        OrchDriveInfo srcDrive, Folder srcFolder, List<WildcardPattern>? wpUserName, List<WildcardPattern>? wpType,
        OrchDriveInfo dstDrive, Folder newFolder, ProgressReporter reporter,
        bool shouldProcess, CancellationToken cancelToken,
        Dictionary<string, string>? userMapping = null)
    {
        if (newFolder.FolderType == "Personal") return;

        var srcFolderUsers = srcDrive.FolderUsersWithNoInherited.Get(srcFolder)
            // -UserName matches tenant UserName OR EmailAddress (B2B).
            .FilterFolderUsersByUserName(srcDrive, wpUserName).ToList();
        if (srcFolderUsers.Count == 0)
        {
            return;
        }

        // Get already-assigned users
        var dstFolderUsers = dstDrive.FolderUsersWithNoInherited.Get(newFolder)
            .FilterFolderUsersByUserName(dstDrive, wpUserName)
            .FilterByWildcards(u => u?.UserEntity?.Type, wpType).ToList();

        string targetFolder = newFolder.GetPSPath();

        reporter.TotalNum = srcFolderUsers.Count;
        int index = 0;
        foreach (var userRole in srcFolderUsers.OrderBy(u => u.UserEntity?.UserName))
        {
            cancelToken.ThrowIfCancellationRequested();

            //reporter.WriteProgress(++index, $"{index:D}/{srcFolderUsers.Count} {userRole.UserEntity!.UserName}");
            reporter.WriteProgress(++index);

            if (shouldProcess || _this.ShouldProcess($"Item: '{userRole.GetPSPath()}' Destination: '{newFolder.GetPSPath()}'", "Copy FolderUser"))
            {
                string userName = userRole.UserEntity?.UserName ?? "";
                string msg = $"Assigning the {userRole.UserEntity?.Type} \"{userName}\"";

                // assert(userRoles.Roles.Any())
                List<Int64> newRoleIds = FindDstRoles(_this, srcDrive, userRole.Roles!, dstDrive, msg);

                // If there are no folder roles, the API call will fail, so output an error and skip this user
                // ...or so I thought, but if mixed roles are already assigned, it may not error, so try the API call.
                //if (newRoleIds is null || !newRoleIds.Any())
                //{
                //    _this.WriteError(new ErrorRecord(new OrchException(targetFolder, $"{msg}: No roles matched."), "CopyFolderError", ErrorCategory.InvalidOperation, newFolder));
                //    continue;
                //}

                List<FolderRoles> newRolesPerFolder = [
                    new FolderRoles
                    {
                        FolderId = newFolder.Id ?? 0,
                        RoleIds = newRoleIds
                    }
                ];

                // If the same user is already assigned to this folder,
                // preserve the existing roles
                var existingSameNameUser = dstFolderUsers.FirstOrDefault(u => string.Compare(u.UserEntity?.UserName, userRole.UserEntity?.UserName, StringComparison.OrdinalIgnoreCase) == 0);
                if (existingSameNameUser is not null)
                {
                    newRolesPerFolder.First().RoleIds?.AddRange(existingSameNameUser.Roles!.Select(r => r.Id ?? 0));
                }

                try
                {
                    DomainUserAssignment postingUser = null;
                    if (!DirectoryTypeItems.Items.TryGetValue(userRole.UserEntity?.Type ?? "", out var type))
                    {
                        _this.WriteError(new ErrorRecord(new OrchException(userRole.GetPSPath(), $"Invalid Type: '{userRole.UserEntity?.Type}'."), "AssignFolderUserError", ErrorCategory.InvalidOperation, targetFolder));
                        continue;
                    }

                    // Name resolution via UserMappingCsv
                    string resolvedUserName = userName;
                    if (userMapping is not null && userMapping.TryGetValue(userName, out var mappedName)
                        && !string.IsNullOrEmpty(mappedName))
                    {
                        resolvedUserName = mappedName;
                    }

                    // Need to search the directory..
                    var (resolvedPrimary, dirResult) = ResolveDstDirectoryUserPure(
                        dstDrive.SearchDirectory(resolvedUserName), resolvedUserName, type);

                    DirectoryObject resolved = null;

                    if (dirResult == FindDstDirectoryUserResult.Resolved)
                    {
                        resolved = resolvedPrimary;
                    }

                    else if (dirResult == FindDstDirectoryUserResult.Duplicated)
                    {
                        _this.WriteError(new ErrorRecord(new OrchException(dstDrive.NameColonSeparator, $"Duplicated {type} found for '{userName}'."), "SearchForUsersAndGroupsError", ErrorCategory.InvalidOperation, dstDrive));
                    }

                    else // FindDstDirectoryUserResult.NotFound
                    {
                        // A user with the same name as the srcDrive user was not found in the dstDrive directory!
                        // So also search dstDrive using this user's email address.

                        // First, check the srcUser's email.
                        var srcUserEmail = srcDrive.Users.Get().FirstOrDefault(u => u.Id == userRole.UserEntity?.Id)?.EmailAddress;

                        // TODO: If not found among local users, need to search the directory.
                        //if (string.IsNullOrEmpty(srcUserEmail))
                        //{
                        //    // If not found among tenant users, search the directory.
                        //    var srcDirectoryUser = srcDrive.SearchPmDirectoryCache.Get(userName.ToLower())?
                        //        .Where(u => u.type == type)
                        //        .Where(u => string.Compare(u.identityName, userName, StringComparison.OrdinalIgnoreCase) == 0)
                        //        .FirstOrDefault();
                        //    srcUserEmail = srcDirectoryUser?.email;
                        //}

                        if (!string.IsNullOrEmpty(srcUserEmail) && srcUserEmail != userName)
                        {
                            resolved = dstDrive.SearchDirectory(srcUserEmail)?
                                .Where(u => u.type == type)
                                .Where(u => string.Compare(u.identityName, srcUserEmail, StringComparison.OrdinalIgnoreCase) == 0)
                                .FirstOrDefault();
                        }
                    }
                    if (resolved is null)
                    {
                        _this.WriteError(new ErrorRecord(new OrchException(targetFolder, $"{msg}: {dstDrive.Name}: does not have the DirectoryUser \"{userName}\"."), "AssignFolderUserError", ErrorCategory.InvalidOperation, targetFolder));
                        continue;
                    }

                    postingUser = new DomainUserAssignment
                    {
                        Domain = string.IsNullOrEmpty(resolved.domain) ? "autogen" : resolved.domain,
                        DirectoryIdentifier = resolved.identifier,
                        UserType = userRole.UserEntity?.Type,
                        RolesPerFolder = newRolesPerFolder
                    };
                    dstDrive.OrchAPISession.AssignFolderUser(postingUser);

                    dstDrive.FolderUsersWithInherited.ClearCache(newFolder);
                    dstDrive.FolderUsersWithNoInherited.ClearCache(newFolder);
                }
                catch (Exception ex)
                {
                    _this.WriteError(new ErrorRecord(new OrchException(targetFolder, msg, ex), "CopyFolderUserError", ErrorCategory.InvalidOperation, targetFolder));
                }
            }
        }
    }

    internal static bool AssignMyselfToFolder(IWritableHost _this, OrchDriveInfo drive, Folder folder)
    {
        var folderAdministratorRole = drive.Roles.Get().FirstOrDefault(r => r.DisplayName == "Folder Administrator");
        if (folderAdministratorRole is null)
            return false;

        if (drive.OrchAPISession.AuthManager.IsConfidentialApp)
        {
            // For confidential apps, assign this confidential app
        }
        else
        {
            // For non-confidential apps, assign the current user
            var currentUser = drive.CurrentUser.Get();
            if (currentUser is null) return false;
            DomainUserAssignment duser = new()
            {
                Domain = string.IsNullOrEmpty(currentUser.Domain) ? "autogen" : currentUser.Domain,
                UserName = currentUser.UserName,
                DirectoryIdentifier = currentUser.Key,
                UserType = currentUser.Type,
                RolesPerFolder = [new FolderRoles()
                {
                    FolderId = folder.Id ?? 0,
                    RoleIds = [folderAdministratorRole.Id ?? 0]
                }]
            };
            try
            {
                drive.OrchAPISession.AssignFolderUser(duser);
            }
            catch (Exception ex)
            {
                _this.WriteWarning($"Failed to assign {currentUser.UserName} to folder {folder.GetPSPath()}: {ex.Message}.");
                return false;
            }
        }

        return true;
    }

    internal static void CopyFolderMachines(
        IWritableHost _this,
        OrchDriveInfo srcDrive, Folder srcFolder, List<WildcardPattern>? wpNames,
        OrchDriveInfo dstDrive, Folder newFolder, ProgressReporter reporter,
        bool shouldProcess, CancellationToken cancelToken)
    {
        if (newFolder.FolderType == "Personal") return;

        IEnumerable<MachineFolder> srcMachines = null;
        try
        {
            srcMachines = srcDrive.FolderMachinesAssigned.Get(srcFolder)
                .Where(e => e.IsAssignedToFolder.GetValueOrDefault())
                .FilterByWildcards(m => m?.Name, wpNames);
        }
        catch (Exception ex)
        {
            _this.WriteError(new ErrorRecord(new OrchException(srcDrive.NameColonSeparator, ex), "GetFolderMachineError", ErrorCategory.InvalidOperation, srcDrive));
            return;
        }
        if (srcMachines is null || !srcMachines.Any())
        {
            return;
        }

        cancelToken.ThrowIfCancellationRequested();

        string targetFolder = newFolder.GetPSPath();

        var machinesToBeAdded = new List<MachineFolder>();
        var dstMachinesAssignable = dstDrive
            .FolderMachinesAssignable.Get(newFolder)
            .ToDictionary(m => m.Name!, StringComparer.OrdinalIgnoreCase);

        var dstMachinesAssigned = dstDrive
            .FolderMachinesAssigned.Get(newFolder)
            .ToDictionary(m => m.Name!, StringComparer.OrdinalIgnoreCase);

        // Even if the destination is the same drive, it might be better to look up Id by name..
        //if (srcDrive == dstDrive)
        //{
        //    reporter.TotalNum = srcMachines.Count;
        //    reporter.WriteProgress(srcMachines.Count, $"{srcMachines.Count}/{srcMachines.Count}");

        //    //string machineNames = string.Join(", ", srcMachines.Select(m => m.Name));
        //    try
        //    {
        //        dstDrive.OrchAPISession.AddMachinesToFolder(newFolder.Id ?? 0, srcMachines.Select(m => m.Id ?? 0));
        //    }
        //    catch (Exception ex)
        //    {
        //        WriteError(new ErrorRecord(new OrchException(target, ex), "CopyFolderMachineError", ErrorCategory.InvalidOperation, target));
        //    }
        //}
        //else

        foreach (var srcMachine in srcMachines.OrderBy(m => m.Name))
        {
            if (dstMachinesAssignable.TryGetValue(srcMachine.Name!, out var dstMachine))
            {
                if (shouldProcess || _this.ShouldProcess($"Item: '{srcMachine.GetPSPath()}' Destination: '{newFolder.GetPSPath()}'", "Copy FolderMachine"))
                {
                    machinesToBeAdded.Add(dstMachine);
                }
            }
            else if (dstMachinesAssigned.TryGetValue(srcMachine.Name!, out dstMachine))
            {
                _this.WriteWarning($"The folder '{newFolder.GetPSPath()}' already has the machine '{srcMachine.Name}' assigned.");
            }
            else
            {
                string msg = $"Copying folder machine \"{srcMachine.Name}\"";
                _this.WriteError(new ErrorRecord(new OrchException(targetFolder, $"{msg}: {dstDrive.Name}: does not have the machine named \"{srcMachine.Name}\"."), "AssignFolderMachineError", ErrorCategory.InvalidOperation, targetFolder));
            }
        }

        if (machinesToBeAdded.Count == 0) return;

        reporter.TotalNum = machinesToBeAdded.Count;
        reporter.WriteProgress(machinesToBeAdded.Count);
        try
        {
            dstDrive.OrchAPISession.AddMachinesToFolder(newFolder.Id ?? 0, machinesToBeAdded.Select(m => m.Id ?? 0));
        }
        catch (Exception ex)
        {
            _this.WriteError(new ErrorRecord(new OrchException(targetFolder, "Assigning machines failed.", ex), "AssignFolderMachineError", ErrorCategory.InvalidOperation, targetFolder));
        }

        #region If srcMachine's PropagateToSubFolders is true, set it to true on dstMachine as well
        foreach (var dstMachine in machinesToBeAdded)
        {
            var srcMachine = srcMachines.FirstOrDefault(m => string.Compare(m.Name, dstMachine.Name, StringComparison.OrdinalIgnoreCase) == 0);
            if (srcMachine is null) continue; // Should never be null, but just in case

            if (srcMachine.PropagateToSubFolders.GetValueOrDefault())
            {
                try
                {
                    //if (shouldProcess || _this.ShouldProcess(dstMachine.GetPSPath(), "Enable FolderMachineInherit"))
                    {
                        dstDrive.OrchAPISession.SetFolderMachineInherit(newFolder.Id!.Value, dstMachine.Id!.Value, true);
                    }
                }
                catch (Exception ex)
                {
                    _this.WriteError(new ErrorRecord(new OrchException(targetFolder, "Enable FolderMachineInherited failed.", ex), "EnableFolderMachineInheritedError", ErrorCategory.InvalidOperation, targetFolder));
                }
            }
        }
        #endregion
    }

    internal static void CopyPackages(IWritableHost _this,
        OrchDriveInfo srcDrive, Folder srcFolder,
        OrchDriveInfo dstDrive, Folder newFolder, ProgressReporter reporter, CancellationToken cancelToken)
    {
        // Do nothing unless both srcFolder and dstFolder are root-level folders with feeds
        if (srcFolder.FeedType != "FolderHierarchy" ||
            newFolder.FeedType != "FolderHierarchy" ||
            srcFolder.ParentId is not null ||
            newFolder.ParentId is not null)
        {
            return;
        }

        string msg = "Copying packages";
        string srcFeedId;
        try
        {
            srcFeedId = srcDrive.FolderFeedId.Get(srcFolder);
        }
        catch (Exception ex)
        {
            _this.WriteError(new ErrorRecord(new OrchException(srcFolder.GetPSPath(), msg, ex), "UploadPackageError", ErrorCategory.InvalidOperation, srcFolder));
            return;
        }

        string dstFeedId;
        try
        {
            dstFeedId = dstDrive.FolderFeedId.Get(newFolder);
        }
        catch (Exception ex)
        {
            _this.WriteError(new ErrorRecord(new OrchException(newFolder.GetPSPath(), msg, ex), "UploadPackageError", ErrorCategory.InvalidOperation, newFolder));
            return;
        }

        // Considering that cmdlets may be executed consecutively in a script,
        // we shouldn't have been clearing the cache each time..
        // Comment out the below.

        // Clear this folder's cache so we can copy the latest state
        // For tenant packages, there is no appropriate time to clear the cache,
        // but due to the preceding conditions, this code path is never reached, so no concern
        //if (!string.IsNullOrEmpty(srcFeedId))
        //{
        //    srcDrive!._dicPackages?.TryRemove(srcFeedId, out _);
        //}

        var packages = srcDrive.GetPackages(srcFolder);
        int totalNum = 0;
        Parallel.ForEach(packages, package =>
        {
            var versions = srcDrive.GetPackageVersions(srcFolder, package.Id!);
            Interlocked.Add(ref totalNum, versions.Count);
        });

        reporter.TotalNum = totalNum;

        string srcFeedFolder = System.IO.Path.Combine(srcDrive.NameColon, srcFolder.GetPackageFeedFolder());
        string dstFeedFolder = System.IO.Path.Combine(dstDrive.NameColon, newFolder.GetPackageFeedFolder());

        int index = 0;
        foreach (var package in packages.OrderBy(p => p.Id!.ToLower()))
        {
            msg = $"Copying the package {System.IO.Path.Combine(srcFeedFolder, $"{package.Id!}.{package.Version}.nupkg")}";

            var versions = srcDrive.GetPackageVersions(srcFolder, package.Id!);
            foreach (var version in versions)
            {
                cancelToken.ThrowIfCancellationRequested();

                //reporter.WriteProgress(++index, $"{version.Id}:{version.Version}");
                reporter.WriteProgress(++index);

                string fileName;
                byte[] fileContent;
                try // download package
                {
                    (fileName, fileContent) = srcDrive.OrchAPISession.DownloadPackage(srcFeedId!, version.Id!, version.Version!);
                }
                catch (Exception ex)
                {
                    _this.WriteError(new ErrorRecord(new OrchException(srcFeedFolder, msg, ex), "DownloadPackageError", ErrorCategory.InvalidOperation, srcFeedFolder));
                    continue;
                }

                try // upload package
                {
                    dstDrive.OrchAPISession.UploadPackage(dstFeedId, fileName!, fileContent!);
                }
                catch (Exception ex)
                {
                    _this.WriteError(new ErrorRecord(new OrchException(dstFeedFolder, msg, ex), "UploadPackageError", ErrorCategory.InvalidOperation, dstFeedFolder));
                }
            }
        }

        // Adding a package to a personal workspace automatically creates a process.
        // Clear the process cache so the subsequent CopyProcesses() sees the correct state.
        if (newFolder.FolderType == "Personal")
        {
            dstDrive.Releases.ClearCache(newFolder);
            dstDrive.ReleasesDetailed.ClearCache(newFolder);
        }
    }


    // action should be like "Copy Process"
    internal static Bucket? FindDstBucket(IWritableHost _this,
        OrchDriveInfo srcDrive, Folder srcFolder, Int64? srcBucketId,
        OrchDriveInfo dstDrive, Folder newFolder, string action, string msg)
    {
        if (srcBucketId is null || srcBucketId == 0) return null;

        var srcBuckets = srcDrive.Buckets.Get(srcFolder);
        var srcBucket = srcBuckets.FirstOrDefault(b => b.Id == srcBucketId);
        if (srcBucket is null)
        {
            _this.WriteWarning($"{msg}: {srcDrive.NameColonSeparator} does not have the bucket with Id = {srcBucketId}.");
            return null;
        }

        var dstBuckets = dstDrive.Buckets.Get(newFolder);
        // Case-insensitive name match. The Orchestrator server rejects
        // bucket creation with "The name <X> is already used" when a same-
        // name bucket exists in any case, so name uniqueness IS case-
        // insensitive at the API level. The original '==' comparison was
        // case-sensitive, would miss a dst bucket whose name differed
        // only in case, and would then trigger that server-side rejection
        // -- the latent symptom was "Copy-Item fails with already-used".
        var dstBucket = ResolveDstByName(dstBuckets, srcBucket.Name, b => b.Name);
        if (dstBucket is null)
        {
            _this.WriteWarning($"{msg}: {newFolder.GetPSPath()} does not have the bucket with Name = '{srcBucket.Name}'.");
            return null;
        }
        return dstBucket;
    }

    internal static void CopyProcesses(IWritableHost _this,
        OrchDriveInfo srcDrive, Folder srcFolder, List<WildcardPattern>? wpName,
        OrchDriveInfo dstDrive, Folder newFolder, ProgressReporter reporter,
        bool shouldProcess, CancellationToken cancelToken)
    {
        // Considering that cmdlets may be executed consecutively in a script,
        // we shouldn't have been clearing the cache each time..
        // Comment out the below.

        // Clear this folder's cache so we can copy the latest state
        //srcDrive._dicReleases?.TryRemove(srcFolder.Id ?? 0, out _);

        // Also clear the dstDrive cache since we need to get the latest process list later
        //dstDrive._dicReleases?.TryRemove(newFolder.Id ?? 0, out _);

        string msg = "Copying the process(es)";
        List<Release> processes;
        try
        {
            // call ToList() to create shallow copy
            processes = srcDrive.Releases.Get(srcFolder)
                .FilterByWildcards(r => r?.Name, wpName)
                .ToList();
        }
        catch (Exception ex)
        {
            _this.WriteError(new ErrorRecord(new OrchException(srcFolder.GetPSPath(), msg, ex), "GetProcessError", ErrorCategory.InvalidOperation, srcFolder));
            return;
        }

        reporter.TotalNum = processes.Count;

        int index = 0;
        bool isNewFolderProcessCacheDirty = false;
        foreach (var process in processes.OrderBy(p => p.Name))
        {
            cancelToken.ThrowIfCancellationRequested();

            string target = $"Item: '{process.GetPSPath()}' Destination: '{newFolder.GetPSPath()}'";
            if (shouldProcess || _this.ShouldProcess(target, "Copy Process"))
            {
                msg = $"Copying process {process.GetPSPath()}";

                // How much do the contents returned by GetRelease and GetReleaseById differ?
                // At the very least, there is content that is only returned by GetReleaseById.
                //var releaseInCache = processes.FirstOrDefault(p => p.Id == process.Id);

                //reporter.WriteProgress(++index, $"{index:D}/{processes.Count} {process.Name}");
                reporter.WriteProgress(++index);

                #region Get the source release information
                Release srcRelease = null;
                try
                {
                    srcRelease = srcDrive.ReleasesDetailed.Get(srcFolder, process.Id ?? 0);
                }
                catch (Exception ex)
                {
                    _this.WriteError(new ErrorRecord(new OrchException(target, msg, ex), "GetProcessError", ErrorCategory.InvalidOperation, target));
                    continue;
                }

                if (srcRelease is null)
                {
                    continue;
                }


                //if (srcRelease.ProcessType == "TestAutomationProcess")
                //{
                //    _this.WriteWarning($"Copy {target}: TestAutomationProcess is not supported.");
                //    continue;
                //}
                #endregion

                #region Get the entry point Id of the srcRelease
                #endregion

                string dstFeedId = dstDrive.FolderFeedId.Get(newFolder);

                #region Migrate the entry point Id
                try
                {
                    if (srcRelease.EntryPointId.HasValue)
                    {
                        string srcFeedId = srcDrive.FolderFeedId.Get(srcFolder);
                        var srcEntryPoints = srcDrive.PackageEntryPoints.Get((srcFeedId ?? "", srcRelease.ProcessKey!, srcRelease.ProcessVersion!)).ToList();
                        var dstEntryPoints = dstDrive.PackageEntryPoints.Get((dstFeedId ?? "", srcRelease.ProcessKey!, srcRelease.ProcessVersion!)).ToList();

                        var srcEntryPoint = srcEntryPoints.FirstOrDefault(e => e.Id == srcRelease.EntryPointId);
                        if (srcEntryPoint is null)
                        {
                            // The release references an entry point that isn't in the package's
                            // entry-point list (e.g. removed in a repackage). We can't remap it,
                            // so null it — posting the src id into the dst tenant would be wrong —
                            // and warn instead of letting the old 'srcEntryPoint!' throw an NRE.
                            _this.WriteWarning($"{msg}: source entry point id {srcRelease.EntryPointId} not found in package '{srcRelease.ProcessKey} {srcRelease.ProcessVersion}'; copying the process without an entry point.");
                            srcRelease.EntryPointId = null;
                        }
                        else
                        {
                            var dstEntryPoint = ResolveDstEntryPointByPath(dstEntryPoints, srcEntryPoint.Path);
                            srcRelease.EntryPointId = dstEntryPoint?.Id;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // The error handling here is a bit rough.. Would like to rewrite this more carefully later. Move part of this to the region above.
                    string msg2 = "Migrating entry point id {srcRelease.EntryPointId} failed.";
                    _this.WriteError(new ErrorRecord(new OrchException(target, $"{msg}: {msg2}", ex), "GetProcessError", ErrorCategory.InvalidOperation, target));
                    // Better not to continue here.
                }
                #endregion

                #region Delete existing process with the same name at the destination
                // Once we've successfully prepared to copy the process up to this point,
                // only when copying to a personal workspace, overwrite if an existing process with the same name exists.
                // In other words, if an existing process with the same name exists, delete it first.
                if (newFolder.FolderType == "Personal")
                {
                    Release existingRelease = null;
                    try
                    {
                        var dstReleases = dstDrive.Releases.Get(newFolder);
                        existingRelease = dstReleases.FirstOrDefault(r => r.Name == srcRelease.Name);
                        if (existingRelease is not null)
                        {
                            dstDrive.OrchAPISession.RemoveRelease(newFolder.Id ?? 0, existingRelease.Id ?? 0);
                            isNewFolderProcessCacheDirty = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        _this.WriteWarning($"Remove existing process {existingRelease?.GetPSPath()} failed. Copying process {srcRelease.GetPSPath()} may fail. ${ex.Message}");
                    }
                }
                #endregion

                #region Get the ReleaseRetention of the srcRelease
                ReleaseRetentionSetting srcRetention = null;
                try
                {
                    srcRetention = srcDrive.OrchAPISession.GetReleaseRetention(srcFolder.Id ?? 0, srcRelease.Id ?? 0);
                }
                catch (Exception ex)
                {
                    string msg2 = $"Get release retention failed.";
                    _this.WriteError(new ErrorRecord(new OrchException(target, msg + ": " + msg2, ex), "GetReleaseRetentionError", ErrorCategory.InvalidOperation, target));
                }
                #endregion

                #region Create the Release in dstFolder
                Release created = null;
                try
                {
                    Release postingRelease = OrchCollectionExtensions.DeepCopy(srcRelease);
                    // postingRelease.Path = null;// Not needed since it has the JsonIgnore attribute
                    postingRelease.CreationTime = null;
                    postingRelease.CreatorUserId = null;
                    postingRelease.Id = null;
                    postingRelease.FeedId = dstFeedId;
                    postingRelease.Key = null;
                    postingRelease.IsLatestVersion = null;
                    postingRelease.IsProcessDeleted = null;
                    postingRelease.ProcessType = null;
                    postingRelease.EnvironmentName = null;
                    postingRelease.SupportsMultipleEntryPoints = null;
                    postingRelease.RequiresUserInteraction = null;
                    postingRelease.IsAttended = null;
                    postingRelease.IsCompiled = null;
                    postingRelease.OrganizationUnitId = null;
                    postingRelease.TargetFramework = null;
                    postingRelease.Arguments = null;
                    // AutoUpdate (package auto-update policy) is a real settable field, not a
                    // folder/server-derived one -- preserve the source value instead of wiping it.
                    postingRelease.ResourceOverwrites = [];

                    if (srcRetention is not null)
                    {
                        postingRelease.RetentionAction = srcRetention.Action;
                        postingRelease.RetentionPeriod = srcRetention.Period;
                        postingRelease.RetentionBucketId = FindDstBucket(_this,
                            srcDrive, srcFolder, srcRetention.BucketId,
                            dstDrive, newFolder, "Copy Process", msg)?.Id;
                    }

                    if (dstDrive.OrchAPISession.ApiVersion >= 17)
                    {
                        postingRelease.RetentionAction ??= "Delete";
                        postingRelease.RetentionPeriod ??= 30;
                    }

                    // EnvironmentId must be set to null for modern folders.
                    // Even for classic folders (ProvisionType == "Manual"), it won't work unless replaced with the correct Id.
                    // But I don't think we need to create something like Get-OrchEnvironment..
                    // If the source and destination folders are the same, keep the EnvironmentId as-is;
                    // otherwise set it to null.
                    //if (newFolder.ProvisionType != "Manual")
                    if (srcDrive != dstDrive || srcFolder != newFolder)
                    {
                        postingRelease.EnvironmentId = null;
                    }

                    if (postingRelease.SpecificPriorityValue is not null)
                    {
                        postingRelease.JobPriority = null;
                    }

                    created = dstDrive.OrchAPISession.PostRelease(newFolder.Id ?? 0, postingRelease);

                    // This output clutters the screen, so maybe we don't need it..
                    //if (!shouldProcess && created is not null)
                    //{
                    //    created.Path = newFolder.GetPSPath();
                    //    _this.WriteObject(created);
                    //}
                }
                catch (Exception ex)
                {
                    _this.WriteError(new ErrorRecord(new OrchException(target, msg, ex), "CopyProcessError", ErrorCategory.InvalidOperation, target));
                }

                if (created is null)
                {
                    continue;
                }
                #endregion

                // Is the below needed for older API versions?
                //#region Get the ReleaseRetention of the srcRelease
                //ReleaseRetentionSetting srcRetention;
                //try
                //{
                //    srcRetention = srcDrive.OrchAPISession.GetReleaseRetention(srcFolder.Id ?? 0, srcRelease.Id ?? 0);
                //}
                //catch (Exception ex)
                //{
                //    string msg2 = $"Get retention info failed.";
                //    _this.WriteError(new ErrorRecord(new OrchException(target, msg + ": " + msg2, ex), "GetRetentionSettingError", ErrorCategory.InvalidOperation, target));
                //    continue;
                //}
                //#endregion

                //if (srcRetention is null)
                //{
                //    continue;
                //}

                //#region Copy ReleaseRetention to createdRelease
                //try
                //{
                //    srcRetention.ReleaseId = created.Id;
                //    dstDrive.OrchAPISession.PutReleaseRetention(newFolder.Id ?? 0, created.Id ?? 0, srcRetention);
                //}
                //catch (Exception ex)
                //{
                //    string msg2 = "Put retention info failed.";
                //    _this.WriteError(new ErrorRecord(new OrchException(target, msg + ": " + msg2, ex), "PutRetentionSettingError", ErrorCategory.InvalidOperation, target));
                //}
                //#endregion
            }
        }

        if (isNewFolderProcessCacheDirty)
        {
            dstDrive.Releases.ClearCache(newFolder);
        }
    }

    // If the group does not exist at the destination, create a group with the same name
    internal static List<PmGroup>? FindDstPmGroups(IWritableHost _this,
        OrchDriveInfo srcDrive, IEnumerable<string>? srcPmGroupIds,
        OrchDriveInfo dstDrive, string msg)
    {
        if (srcPmGroupIds is null) return null;

        string target = srcDrive.NameColonSeparator;
        IEnumerable<PmGroup>? srcPmGroups = null;
        try
        {
            srcPmGroups = srcDrive.PmGroups.Get();
        }
        catch (Exception ex)
        {
            _this.WriteError(new ErrorRecord(new OrchException(target, msg, ex), "GetPmGroupError", ErrorCategory.InvalidOperation, dstDrive));
            return null;
        }
        if (srcPmGroups is null)
        {
            _this.WriteError(new ErrorRecord(new OrchException(target, $"{msg}: Failed to retrieve PmGroup."), "GetPmGroupError", ErrorCategory.InvalidOperation, dstDrive));
            return null;
        }

        target = dstDrive.NameColonSeparator;
        IEnumerable<PmGroup>? dstPmGroups = null;
        try
        {
            dstPmGroups = dstDrive.PmGroups.Get();
        }
        catch (Exception ex)
        {
            _this.WriteError(new ErrorRecord(new OrchException(target, msg, ex), "GetPmGroupError", ErrorCategory.InvalidOperation, dstDrive));
            return null;
        }
        if (dstPmGroups is null)
        {
            _this.WriteError(new ErrorRecord(new OrchException(target, $"{msg}: Failed to retrieve PmGroup."), "GetPmGroupError", ErrorCategory.InvalidOperation, dstDrive));
            return null;
        }

        List<PmGroup> ret = [];
        foreach (var srcPmGroupId in srcPmGroupIds)
        {
            var srcPmGroup = srcPmGroups.FirstOrDefault(g => g?.id == srcPmGroupId);
            if (srcPmGroup is null)
            {
                _this.WriteWarning($"{msg}: {srcDrive.NameColon} does not have PmGroup with id = {srcPmGroupId}. Ignoring this id.");
                continue;
            }

            var dstPmGroup = dstPmGroups.FirstOrDefault(g => string.Compare(g!.displayName, srcPmGroup.displayName, StringComparison.OrdinalIgnoreCase) == 0);
            if (dstPmGroup is null)
            {
                dstPmGroup = _this.CreatePmGroup(dstDrive, srcPmGroup.name);
                if (dstPmGroup is null) continue;
            }
            ret.Add(dstPmGroup);
        }
        return ret;
    }

    internal static CredentialStore? FindDstCredentialStore(IWritableHost _this,
        OrchDriveInfo srcDrive,
        OrchDriveInfo dstDrive, Folder newFolder, Int64? srcCredentialStoreId, string msg)
    {
        if (srcCredentialStoreId is null || srcCredentialStoreId.Value == 0) return null;

        try
        {
            CredentialStore srcCredentialStore = srcDrive.CredentialStores.Get().FirstOrDefault(cs => (cs.Id ?? 0) == srcCredentialStoreId);
            if (srcCredentialStore is null)
            {
                _this.WriteWarning($"{msg}: {srcDrive.NameColon} does not have credential store with Id = {srcCredentialStoreId}.");
                return null;
            }

            var dstCredentialStore = ResolveDstByName(dstDrive.CredentialStores.Get(), srcCredentialStore.Name, cs => cs.Name);
            if (dstCredentialStore is null)
            {
                _this.WriteWarning($"{msg}: {dstDrive.NameColon} does not have credential store with Name = '{srcCredentialStore.Name}'.");
            }
            return dstCredentialStore;
        }
        catch (Exception ex)
        {
            string target = newFolder.GetPSPath();
            _this.WriteError(new ErrorRecord(new OrchException(target, msg, ex), "MigrateCredentialStoreIdError", ErrorCategory.InvalidOperation, target));
            return null;
        }
    }

    // Generic name-match resolver used by the simple FindDst* methods
    // (FindDstRobot / FindDstMachine / FindDstQueue / FindDstRelease /
    // FindDstCalendar / FindDstCredentialStore / FindDstBucket /
    // FindDstTestSet).
    //
    // All current callers use the default StringComparison.OrdinalIgnoreCase
    // -- every entity kind the Orchestrator UI treats by name does so
    // case-insensitively, and same-name uniqueness on the server side is
    // case-insensitive too (creation with a differently-cased name fails
    // with "name already used"). The Ordinal overload is retained for any
    // future caller that has a legitimate case-sensitive need.
    internal static T? ResolveDstByName<T>(
        IEnumerable<T>? candidates,
        string? srcName,
        Func<T, string?> nameOf,
        StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        where T : class
    {
        if (candidates is null || string.IsNullOrEmpty(srcName)) return null;
        return candidates.FirstOrDefault(c => c is not null && string.Equals(nameOf(c), srcName, comparison));
    }

    // Resolve the destination package entry point whose Path matches srcPath. Named
    // wrapper over ResolveDstByName so the entry-point match shares the family's
    // OrdinalIgnoreCase policy. CopyProcesses uses this to remap a Release's
    // EntryPointId across feeds; the original inline '==' was case-sensitive and
    // silently dropped the EntryPointId when a dst feed exposed the same entry point
    // under a different case (e.g. "Main.xaml" vs "main.xaml").
    internal static PackageEntryPoint? ResolveDstEntryPointByPath(
        IEnumerable<PackageEntryPoint>? dstEntryPoints, string? srcPath)
        => ResolveDstByName(dstEntryPoints, srcPath, e => e.Path);

    // Result of FindDstUser's pure-logic resolver (see ResolveDstUserPure).
    // Lets the IO wrapper emit the right warning per case without re-checking.
    internal enum FindDstUserResult
    {
        Found,                  // dstUser is non-null AND assigned to the target folder
        NotFound,               // no matching user in dstUsers (by name or email)
        NotAssignedToFolder,    // matched a user but they're not on newFolder's assigned-users list
    }

    // Pure name-resolution + folder-assignment check. Extracted from FindDstUser
    // so the matching policy is unit-testable without standing up a real
    // OrchDriveInfo / OrchAPISession / AuthManager. The IO wrapper below does
    // the cache fetch / cache-clear retry / warning emission.
    //
    // Matching order (preserved from the original implementation):
    //   1. UserMappingCsv lookup -- if userMapping[srcUser.UserName] exists
    //      and is non-empty, use the mapped name as searchName; otherwise
    //      searchName = srcUser.UserName.
    //   2. Case-insensitive UserName match against dstUsers.
    //   3. Case-insensitive UserName-equals-srcUser.EmailAddress match
    //      against dstUsers (only when allowEmailFallback is true).
    //   4. If a user is found, verify their Id is in assignedFolderUserIds;
    //      if not, return (user, NotAssignedToFolder) -- the caller should
    //      treat the user as unusable on this folder.
    internal static (Entities.User? user, FindDstUserResult result) ResolveDstUserPure(
        Entities.User srcUser,
        IEnumerable<Entities.User> dstUsers,
        Dictionary<string, string>? userMapping,
        HashSet<long> assignedFolderUserIds,
        bool allowEmailFallback = true)
    {
        string searchName = srcUser.UserName ?? "";
        if (userMapping is not null
            && userMapping.TryGetValue(searchName, out var mapped)
            && !string.IsNullOrEmpty(mapped))
        {
            searchName = mapped;
        }

        var dstUser = dstUsers.FirstOrDefault(u =>
            string.Equals(u.UserName, searchName, StringComparison.OrdinalIgnoreCase));

        if (dstUser is null && allowEmailFallback && !string.IsNullOrEmpty(srcUser.EmailAddress))
        {
            dstUser = dstUsers.FirstOrDefault(u =>
                string.Equals(u.UserName, srcUser.EmailAddress, StringComparison.OrdinalIgnoreCase));
        }

        if (dstUser is null) return (null, FindDstUserResult.NotFound);

        if (dstUser.Id is null || !assignedFolderUserIds.Contains(dstUser.Id.Value))
        {
            return (dstUser, FindDstUserResult.NotAssignedToFolder);
        }

        return (dstUser, FindDstUserResult.Found);
    }

    // TODO: Is this implementation incomplete? Need to search the directory for users.
    // The current implementation only searches local users.
    internal static Entities.User? FindDstUser(IWritableHost _this,
        OrchDriveInfo srcDrive,
        OrchDriveInfo dstDrive, Folder newFolder, Int64? srcUserId, string msg,
        Dictionary<string, string>? userMapping = null)
    {
        if (srcUserId is null || srcUserId == 0) return null;
        try
        {
            var srcUser = srcDrive.Users.Get().FirstOrDefault(u => u.Id == srcUserId);
            if (srcUser is null)
            {
                _this.WriteWarning($"{msg}: {srcDrive.NameColon} does not have user with Id = {srcUserId}.");
                return null;
            }

            // Folder assignment fetch is auth-protected and the same on both
            // attempts, so fetch once outside the retry loop.
            var assignedFolderUserIds = dstDrive.FolderUsersWithInherited.Get(newFolder)
                .Where(ur => ur?.UserEntity?.Id is not null)
                .Select(ur => ur.UserEntity!.Id!.Value)
                .ToHashSet();

            // First attempt: try name match, then email fallback against the
            // currently-cached dstUsers list.
            var (dstUser, result) = ResolveDstUserPure(
                srcUser, dstDrive.Users.Get(), userMapping, assignedFolderUserIds,
                allowEmailFallback: true);

            // Retry once after clearing the tenant user cache. This handles the
            // case where AssignDirectoryUser was just called in CopyFolderUsers
            // and the cached user list is stale. Email fallback is enabled
            // on the retry too -- the first pass having tried email is no
            // reason to skip it after a fresh fetch (a B2B user whose
            // UserName != EmailAddress that wasn't in the stale cache is
            // exactly what the retry exists for).
            if (result == FindDstUserResult.NotFound)
            {
                dstDrive.Users.ClearCache();
                (dstUser, result) = ResolveDstUserPure(
                    srcUser, dstDrive.Users.Get(), userMapping, assignedFolderUserIds,
                    allowEmailFallback: true);
            }

            // Re-compute the search name (cheap) for the warning messages.
            string searchName = srcUser.UserName ?? "";
            if (userMapping is not null
                && userMapping.TryGetValue(searchName, out var mappedName)
                && !string.IsNullOrEmpty(mappedName))
            {
                searchName = mappedName;
            }

            switch (result)
            {
                case FindDstUserResult.NotFound:
                    _this.WriteWarning($"{msg}: {dstDrive.NameColon} does not have user with Name = '{searchName}'.");
                    return null;

                case FindDstUserResult.NotAssignedToFolder:
                    // Without this check, the cmdlet would PUT a per-Robot UserValue
                    // with a UserId that the destination folder doesn't own; the
                    // server returns 200 but silently drops the UserValue (and can
                    // wipe the asset's Global Value as a side effect).
                    _this.WriteWarning($"{msg}: A user with the name '{searchName}' is not assigned in '{newFolder.GetPSPath()}'.");
                    return null;

                default:
                    return dstUser;
            }
        }
        catch (Exception ex)
        {
            string target = newFolder.GetPSPath();
            _this.WriteError(new ErrorRecord(new OrchException(target, msg, ex), "MigrateUserIdError", ErrorCategory.InvalidOperation, target));
            return null;
        }
    }

    internal static RobotsFromFolderModel? FindDstRobot(IWritableHost _this,
        OrchDriveInfo srcDrive,
        OrchDriveInfo dstDrive, Folder dstFolder, Int64? srcRobotId, string msg)
    {
        if (srcRobotId is null || srcRobotId == 0) return null;
        try
        {
            var srcRobot = srcDrive.Robots.Get()?.FirstOrDefault(r => r.Id == srcRobotId);
            if (srcRobot is null)
            {
                _this.WriteWarning($"{msg}: {srcDrive.NameColon} does not have robot with Id = {srcRobotId}.");
                return null;
            }
            //msg = $"Migrating id of the robot {Path.Combine(srcDrive.NameColon, srcRobot.Name!)}";

            var dstRobots = dstDrive.RobotsFromFolder.Get(dstFolder);
            var dstRobot = ResolveDstByName(dstRobots, srcRobot.Name, r => r.Name);
            if (dstRobot is null)
            {
                _this.WriteWarning($"{msg}: {dstDrive.NameColon} does not have robot with Name = '{srcRobot.Name}' ({srcRobot.Username}) in '{dstFolder.GetPSPath()}'.");
                return null;
            }
            return dstRobot;
        }
        catch (Exception ex)
        {
            string target = dstFolder.GetPSPath();
            _this.WriteError(new ErrorRecord(new OrchException(target, msg, ex), "MigrateRobotIdError", ErrorCategory.InvalidOperation, target));
            return null;
        }
    }

    internal static RobotsFromFolderModel? FindDstRobotByUnattendedAccount(IWritableHost _this,
        OrchDriveInfo srcDrive, Folder srcFolder,
        OrchDriveInfo dstDrive, Folder dstFolder, Int64? srcRobotId, string msg)
    {
        if (srcRobotId is null || srcRobotId == 0) return null;
        try
        {
            string? srcRobot_Type = null;
            string? srcRobot_Username = null;
            if (srcFolder.ProvisionType == "Manual")
            {
                // For classic folders, search for classic robots via GET /odata/Sessions
                var sessions = srcDrive.Sessions.Get(srcFolder);
                var srcRobot = sessions.FirstOrDefault(s => s.Robot?.Id == srcRobotId);
                if (srcRobot is null)
                {
                    _this.WriteWarning($"{msg}: {srcDrive.NameColon} does not have robot with Id = {srcRobotId}.");
                    return null;
                }
                srcRobot_Type = srcRobot.Robot?.Type;
                srcRobot_Username = srcRobot.Robot?.Username;
            }
            else
            {
                var srcRobot = srcDrive.Robots.Get()?.FirstOrDefault(r => r.Id == srcRobotId);
                if (srcRobot is null)
                {
                    _this.WriteWarning($"{msg}: {srcDrive.NameColon} does not have robot with Id = {srcRobotId}.");
                    return null;
                }
                srcRobot_Type = srcRobot.Type;
                srcRobot_Username = srcRobot.Username;
            }

            //msg = $"Migrating id of the robot {Path.Combine(srcDrive.NameColon, srcRobot.Name!)}";

            // The current implementation searches for robots by the UR's Windows account name (an ID like domain\user)
            // Would it be better to search by the robot's own name? (Is that possible?)
            // For classic robots, no matching robot name can be found

            var dstRobots = dstDrive.RobotsFromFolder.Get(dstFolder);
            // Both fields case-insensitive. Type is server-stable ("Unattended"
            // / "Development" / etc.) so case variation is unlikely in
            // practice, but the comparison costs nothing extra and any
            // future server-side casing change would silently not match
            // under the original case-sensitive '=='.
            var dstRobot = dstRobots?.FirstOrDefault(r =>
                string.Equals(r.Type, srcRobot_Type, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(r.Username, srcRobot_Username, StringComparison.OrdinalIgnoreCase));
            if (dstRobot is null)
            {
                string target = dstFolder.GetPSPath();
                //_this.WriteError(new ErrorRecord(new OrchException(target, $"{msg}: A Robot with the user name '{srcRobot.Username}' is not configured in {dstFolder.GetPSPath()}."), "MigrateRobotIdError", ErrorCategory.InvalidOperation, target));
                _this.WriteWarning($"{msg}: An unattended robot with the user name '{srcRobot_Username}' ({srcRobot_Username}) is not configured in {dstFolder.GetPSPath()}.");
                return null;
            }
            return dstRobot;
        }
        catch (Exception ex)
        {
            string target = dstFolder.GetPSPath();
            _this.WriteError(new ErrorRecord(new OrchException(target, msg, ex), "MigrateRobotIdError", ErrorCategory.InvalidOperation, target));
            return null;
        }
    }

    internal static MachineFolder? FindDstMachine(IWritableHost _this,
        OrchDriveInfo srcDrive, Folder srcFolder,
        OrchDriveInfo dstDrive, Folder dstFolder, Int64? srcMachineId, string msg)
    {
        if (srcMachineId is null || srcMachineId == 0) return null;
        //string msg = $"Migrating the machine id {Path.Combine(srcDrive.NameColon, srcMachineId?.ToString() ?? "")}";
        try
        {
            var srcMachine = srcDrive.Machines.Get().FirstOrDefault(m => m.Id == srcMachineId);
            if (srcMachine is null)
            {
                _this.WriteWarning($"{msg}: {srcDrive.NameColon} does not have machine with Id = {srcMachineId}.");
                return null;
            }
            //msg = $"Migrating id of the machine {Path.Combine(srcDrive.NameColon, srcMachine.Name!)}";
            var dstMachineFolder = ResolveDstByName(dstDrive.FolderMachinesAssigned.Get(dstFolder), srcMachine.Name, m => m.Name);
            if (dstMachineFolder is null)
            {
                string target = dstFolder.GetPSPath();
                //_this.WriteError(new ErrorRecord(new OrchException(target, $"{msg}: A machine with the name '{srcMachine.Name}' is not assigned in '{dstFolder.GetPSPath()}'."),
                //    "MigrateMachineIdError",
                //    ErrorCategory.InvalidOperation,
                //    target));
                _this.WriteWarning($"{msg}: A machine with the name '{srcMachine.Name}' is not assigned in '{dstFolder.GetPSPath()}'.");
                return null;
            }
            return dstMachineFolder;
        }
        catch (Exception ex)
        {
            string target = dstFolder.GetPSPath();
            _this.WriteError(new ErrorRecord(new OrchException(target, msg, ex), "MigrateMachineIdError", ErrorCategory.InvalidOperation, target));
            return null;
        }
    }

    // Pure 3-tier session matching extracted from FindDstSession so the
    // fallback chain is unit-testable. Original behaviour preserved verbatim,
    // including the questionable bits (see asymmetries below).
    //
    // Match tiers, applied in order:
    //   Tier 1 (primary): MachineName + HostMachineName + ServiceUserName
    //     all match case-insensitively. Null fields are coerced to "".
    //   Tier 2 (fallback A): MachineName + HostMachineName match
    //     case-insensitively AND the dst's ServiceUserName is null/empty.
    //     Useful when the dst session hasn't been assigned a service user yet.
    //   Tier 3 (fallback B): MachineName + HostMachineName match only.
    //     Loosest -- ignores ServiceUserName entirely.
    //
    // Preserved asymmetries (candidates for BugDiscovery):
    //   * Tier 1 coerces null fields to "" before comparing
    //     (string.Compare(s.X ?? "", srcX, ...)). Tiers 2 and 3 do NOT
    //     coerce on the dst side (string.Compare(s.X, srcX, ...)). When a
    //     dst session has a null field, Tier 1 sees it as ""-equals-srcX
    //     but Tiers 2/3 see null-vs-srcX which Compare treats with null
    //     ordering rules (null is less than any string). Different match
    //     outcomes are possible across tiers for the same row.
    //   * The wrapper (FindDstSession below) writes the "not found"
    //     warning BEFORE attempting Tier 2/3 fallbacks. A successful
    //     fallback resolution still leaves a misleading warning in the
    //     user's output stream.
    internal static MachineSessionRuntime? ResolveDstSessionPure(
        IEnumerable<MachineSessionRuntime> dstSessions,
        string srcMachineName,
        string srcHostMachineName,
        string srcServiceUserName)
    {
        // All tiers null-coerce dst fields to "" before compare so a dst
        // session row with null fields is visible to every tier. The
        // original implementation only coerced on Tier 1, which left
        // null-field rows unreachable for the looser tiers -- inconsistent
        // and fixed during this round of audits.

        // Tier 1: full triple match.
        var dstSession = dstSessions.FirstOrDefault(s =>
            string.Equals(s.MachineName ?? "", srcMachineName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(s.HostMachineName ?? "", srcHostMachineName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(s.ServiceUserName ?? "", srcServiceUserName, StringComparison.OrdinalIgnoreCase));
        if (dstSession is not null) return dstSession;

        // Tier 2: machine + host match, dst service user empty.
        dstSession = dstSessions.FirstOrDefault(s =>
            string.Equals(s.MachineName ?? "", srcMachineName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(s.HostMachineName ?? "", srcHostMachineName, StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrEmpty(s.ServiceUserName));
        if (dstSession is not null) return dstSession;

        // Tier 3: machine + host match only. Loosest.
        return dstSessions.FirstOrDefault(s =>
            string.Equals(s.MachineName ?? "", srcMachineName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(s.HostMachineName ?? "", srcHostMachineName, StringComparison.OrdinalIgnoreCase));
    }

    internal static MachineSessionRuntime? FindDstSession(IWritableHost _this,
        OrchDriveInfo srcDrive, Folder srcFolder,
        OrchDriveInfo dstDrive, Folder dstFolder, Int64? srcSessionId, string msg)
    {
        if (srcSessionId is null || srcSessionId.Value == 0) return null;

        MachineSessionRuntime srcSession = null;
        try
        {
            // TODO: Changed this to use cache. Is it working correctly?
            var srcSessions = srcDrive.MachineSessionRuntimesByFolder.Fetch(srcFolder).ToList();
            srcSession = srcSessions.FirstOrDefault(s => s.SessionId == srcSessionId);
            if (srcSession is null)
            {
                _this.WriteWarning($"\"{srcFolder.GetPSPath()}\": {msg}: The session not found with SessionId {srcSessionId}.");
                return null;
            }
        }
        catch (Exception ex)
        {
            _this.WriteError(new ErrorRecord(new OrchException(srcFolder.GetPSPath(), msg, ex), "MigrateSessionIdError", ErrorCategory.InvalidOperation, srcFolder));
            return null;
        }

        var srcMachineName = srcSession.MachineName ?? "";
        var srcHostMachineName = srcSession.HostMachineName ?? "";
        var srcServiceUserName = srcSession.ServiceUserName ?? "";

        try
        {
            var dstSessions = dstDrive.MachineSessionRuntimesByFolder.Fetch(dstFolder);

            // Warn only when ALL three tiers miss. The original wrapper
            // warned after Tier 1 failed and BEFORE Tiers 2/3 were tried,
            // so a successful fallback resolution still left a misleading
            // "not found" message in the user's output stream. Fixed
            // during this round of audits.
            var resolved = ResolveDstSessionPure(dstSessions, srcMachineName, srcHostMachineName, srcServiceUserName);
            if (resolved is null)
            {
                _this.WriteWarning($"\"{dstFolder.GetPSPath()}\": {msg}: The session not found with MachineName ='{srcMachineName}', HostMachineName = '{srcHostMachineName}' and ServiceUserName = '{srcServiceUserName}'.");
            }
            return resolved;
        }
        catch (Exception ex)
        {
            _this.WriteError(new ErrorRecord(new OrchException(dstFolder.GetPSPath(), msg, ex), "MigrateSessionIdError", ErrorCategory.InvalidOperation, dstFolder));
            return null;
        }
    }

    internal static QueueDefinition? FindDstQueue(IWritableHost _this,
        OrchDriveInfo srcDrive, Folder srcFolder,
        OrchDriveInfo dstDrive, Folder dstFolder, Int64? srcQueueId, string msg)
    {
        if (srcQueueId is null || srcQueueId.Value == 0) return null;

        // Considering that cmdlets may be executed consecutively in a script,
        // we shouldn't have been clearing the cache each time..
        // Comment out the below.

        // Clear this folder's cache so we can copy the latest state
        //srcDrive._dicQueueDefinitions?.TryRemove(srcFolder.Id ?? 0, out var _);

        QueueDefinition srcQueue = null;
        try
        {
            srcQueue = srcDrive.Queues.Get(srcFolder)?.FirstOrDefault(q => q.Id == srcQueueId);
        }
        catch (Exception ex)
        {
            string target = srcFolder.GetPSPath();
            _this.WriteError(new ErrorRecord(new OrchException(target, msg, ex), "MigrateQueueIdError", ErrorCategory.InvalidOperation, target));
            return null;
        }
        if (srcQueue is null)
        {
            string target = srcFolder.GetPSPath();
            _this.WriteError(new ErrorRecord(new OrchException(target, $"{msg}: {srcFolder.GetPSPath()} does not have queue with Id = {srcQueueId}."), "MigrateQueueIdError", ErrorCategory.InvalidOperation, target));
            return null;
        }

        QueueDefinition dstQueue = null;
        try
        {
            dstQueue = ResolveDstByName(dstDrive.Queues.Get(dstFolder), srcQueue.Name, q => q.Name);
        }
        catch (Exception ex)
        {
            string target = dstFolder.GetPSPath();
            _this.WriteError(new ErrorRecord(new OrchException(target, msg, ex), "MigrateQueueIdError", ErrorCategory.InvalidOperation, target));
            return null;
        }
        if (dstQueue is null)
        {
            _this.WriteWarning($"{msg}: {dstFolder.GetPSPath()} does not have queue with Name = '{srcQueue.Name}'.");
            return null;
        }
        return dstQueue;
    }

    internal static Release? FindDstRelease(IWritableHost _this,
        OrchDriveInfo srcDrive, Folder srcFolder,
        OrchDriveInfo dstDrive, Folder dstFolder, Int64? srcReleaseId, string msg)
    {
        if (srcReleaseId is null || srcReleaseId == 0) return null;

        string target = srcFolder.GetPSPath();
        //string msg = $"Migrating process id {Path.Combine(srcFolder.GetPSPath(), srcReleaseId?.ToString() ?? "")}";
        Release srcRelease = null;
        try
        {
            srcRelease = srcDrive.Releases.Get(srcFolder)?.FirstOrDefault(r => r.Id == srcReleaseId);
        }
        catch (Exception ex)
        {
            _this.WriteError(new ErrorRecord(new OrchException(target, msg, ex), "MigrateProcessIdError", ErrorCategory.InvalidOperation, target));
            return null;
        }
        if (srcRelease is null)
        {
            _this.WriteWarning($"{msg}: The process id {srcReleaseId} not found in '{srcFolder.GetPSPath()}'.");
            return null;
        }

        //msg = $"Migrating id of process {Path.Combine(srcFolder.GetPSPath(), srcRelease.Name!)}";

        Release dstRelease = null;
        target = dstFolder.GetPSPath();
        try
        {
            dstRelease = ResolveDstByName(dstDrive.Releases.Get(dstFolder), srcRelease.Name, r => r.Name);
        }
        catch (Exception ex)
        {
            _this.WriteError(new ErrorRecord(
                new OrchException(target, $"{msg}: Failed to get processes from {dstFolder.GetPSPath()}", ex),
                "MigrateProcessIdError", ErrorCategory.InvalidOperation, target));
            return null;
        }
        if (dstRelease is null)
        {
            _this.WriteWarning($"{msg}: {dstFolder.GetPSPath()} does not have process with Name = '{srcRelease.Name}'.");
            return null;
        }
        return dstRelease;
    }

    internal static ExtendedCalendar? FindDstCalendar(IWritableHost _this,
        OrchDriveInfo srcDrive, OrchDriveInfo dstDrive, Int64? srcCalendarId, string msg)
    {
        if (srcCalendarId is null || srcCalendarId == 0) return null;

        var srcCalendars = srcDrive.Calendars.Get();
        var srcCalendar = srcCalendars?.FirstOrDefault(c => c.Id == srcCalendarId);
        if (srcCalendar is null)
        {
            _this.WriteWarning($"{msg}: {srcDrive.NameColonSeparator} doesn't have calendar with Id = {srcCalendarId}.");
            return null;
        }

        //msg = $"Migrating id of the calendar {Path.Combine(srcDrive.NameColon, srcCalendar.Name!)}";
        ExtendedCalendar dstCalendar = null;
        try
        {
            dstCalendar = ResolveDstByName(dstDrive.Calendars.Get(), srcCalendar.Name, c => c.Name);
        }
        catch (Exception ex)
        {
            _this.WriteError(new ErrorRecord(new OrchException(dstDrive.NameColonSeparator, msg, ex), "MigrateCalendarIdError", ErrorCategory.InvalidOperation, dstDrive));
            return null;
        }
        if (dstCalendar is null)
        {
            //_this.WriteError(new ErrorRecord(new OrchException(dstDrive.NameColonSeparator, $"{msg}: {dstDrive.NameColonSeparator} doesn't have calendar with Name = {srcCalendar.Name}."), "MigrateMachineIdError", ErrorCategory.InvalidOperation, dstDrive));
            _this.WriteWarning($"{msg}: Calendar with name '{srcCalendar.Name}' does not exist in '{dstDrive.NameColonSeparator}'.");
            return null;
        }
        return dstCalendar;
    }

    /// <summary>
    /// Finds, for each src link folder identified by <paramref name="folderIds"/>, the
    /// corresponding dst folder by rebasing src's relative position around
    /// <paramref name="srcAnchor"/> onto dst's tree around <paramref name="dstAnchor"/>.
    /// Replaces the older FQN-equality match, which was correct for cross-drive copies
    /// (where src and dst trees share FQN shape) but broken for same-drive copies
    /// (src and dst FQNs differ, so the equality only ever matched src against itself,
    /// leaving Link*/Bucket/Queue sharing the SOURCE entity into dst folders).
    /// </summary>
    internal static IEnumerable<Folder>? FindDstFolders(
        List<Int64>? folderIds,
        IEnumerable<Folder> srcFolders,
        IEnumerable<Folder> dstFolders,
        Folder srcAnchor,
        Folder dstAnchor)
    {
        if (folderIds is null) return null;

        var selectedSrcFolders = srcFolders.Where(src => folderIds.Contains(src.Id ?? 0)).ToList();
        if (selectedSrcFolders.Count == 0) return Enumerable.Empty<Folder>();

        var dstByFqn = new Dictionary<string, Folder>(StringComparer.OrdinalIgnoreCase);
        foreach (var d in dstFolders)
        {
            if (d.FullyQualifiedName is not null) dstByFqn[d.FullyQualifiedName] = d;
        }

        string srcAnchorFqn = srcAnchor.FullyQualifiedName ?? "";
        string dstAnchorFqn = dstAnchor.FullyQualifiedName ?? "";

        var result = new List<Folder>();
        foreach (var srcLink in selectedSrcFolders)
        {
            string? candidateDstFqn = ComputeDstFqn(srcLink.FullyQualifiedName ?? "", srcAnchorFqn, dstAnchorFqn);
            if (candidateDstFqn is null) continue;
            if (!dstByFqn.TryGetValue(candidateDstFqn, out var dstMatch)) continue;
            // Defensive: when src and dst share the folder pool (same drive), refuse to
            // return a dst folder that is itself one of the src link folders. Without
            // this, same-drive copies whose dst tree happens to alias an src link folder
            // would re-introduce the "share src into dst" foot-gun.
            if (folderIds.Contains(dstMatch.Id ?? 0)) continue;
            result.Add(dstMatch);
        }
        return result;
    }

    /// <summary>
    /// Returns the FQN of the dst folder corresponding to a src link folder, by rebasing
    /// the src→link relationship around the (srcAnchor, dstAnchor) pair. Handles three
    /// shapes: srcLink is a descendant of srcAnchor, srcLink is an ancestor of srcAnchor,
    /// or srcLink is reachable via a common ancestor higher up the src tree (sibling or
    /// cousin). Returns null when the relationship has no expressible dst equivalent
    /// (e.g., disjoint top-level subtrees).
    /// </summary>
    internal static string? ComputeDstFqn(string srcLinkFqn, string srcAnchorFqn, string dstAnchorFqn)
    {
        // Identical → dst equivalent is dstAnchor itself
        if (string.Equals(srcLinkFqn, srcAnchorFqn, StringComparison.OrdinalIgnoreCase))
        {
            return dstAnchorFqn;
        }

        // Descendant of srcAnchor: replace srcAnchor prefix with dstAnchor
        if (srcLinkFqn.StartsWith(srcAnchorFqn + "/", StringComparison.OrdinalIgnoreCase))
        {
            return dstAnchorFqn + srcLinkFqn.Substring(srcAnchorFqn.Length);
        }

        // Ancestor of srcAnchor: walk up dstAnchor by the same number of segments
        if (srcAnchorFqn.StartsWith(srcLinkFqn + "/", StringComparison.OrdinalIgnoreCase))
        {
            int upSteps = srcAnchorFqn.Substring(srcLinkFqn.Length).Count(c => c == '/');
            return WalkUp(dstAnchorFqn, upSteps);
        }

        // Sibling / cousin: find longest common prefix that ends at a "/" boundary,
        // walk up dstAnchor by the number of segments below that prefix in srcAnchor,
        // then descend into the dst tree by srcLink's tail under the common prefix.
        int lastBoundary = -1;
        int minLen = Math.Min(srcAnchorFqn.Length, srcLinkFqn.Length);
        for (int i = 0; i < minLen; i++)
        {
            if (char.ToLowerInvariant(srcAnchorFqn[i]) != char.ToLowerInvariant(srcLinkFqn[i])) break;
            if (srcAnchorFqn[i] == '/') lastBoundary = i;
        }
        if (lastBoundary < 0) return null; // no shared ancestor

        string srcCommon = srcAnchorFqn.Substring(0, lastBoundary);
        string srcSuffixToLink = srcLinkFqn.Substring(lastBoundary + 1);
        int upStepsFromAnchor = srcAnchorFqn.Substring(srcCommon.Length).Count(c => c == '/');

        string? dstCommon = WalkUp(dstAnchorFqn, upStepsFromAnchor);
        if (dstCommon is null) return null;

        return dstCommon.Length == 0 ? srcSuffixToLink : dstCommon + "/" + srcSuffixToLink;
    }

    /// <summary>Strips <paramref name="upSteps"/> trailing "/segment" pieces from
    /// <paramref name="fqn"/>. Returns null if the path can't go that high.</summary>
    internal static string? WalkUp(string fqn, int upSteps)
    {
        for (int i = 0; i < upSteps; i++)
        {
            int lastSlash = fqn.LastIndexOf('/');
            if (lastSlash < 0) return null;
            fqn = fqn.Substring(0, lastSlash);
        }
        return fqn;
    }

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
        // TODO: Is this version number correct? There likely aren't any Orchestrators older than v12 anymore.
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

    internal static void CopyAssets(IWritableHost _this,
        OrchDriveInfo srcDrive, Folder srcFolder, List<WildcardPattern>? wpName,
        OrchDriveInfo dstDrive, Folder newFolder, ProgressReporter reporter,
        bool shouldProcess, CancellationToken cancelToken,
        Dictionary<string, string>? userMapping = null)
    {
        dstDrive.FolderMachinesAssigned.ClearCache(newFolder);

        string target = srcFolder.GetPSPath();
        string msg = "Copying assets";
        List<Asset> srcAssets;
        try
        {
            srcAssets = srcDrive.Assets.Get(srcFolder).FilterByWildcards(a => a?.Name, wpName).ToList();
        }
        catch (Exception ex)
        {
            _this.WriteError(new ErrorRecord(new OrchException(target, msg, ex), "GetAssetError", ErrorCategory.InvalidOperation, srcFolder));
            return;
        }

        reporter.TotalNum = srcAssets.Count;

        int index = 0;
        foreach (var asset in srcAssets.OrderBy(a => a.Name))
        {
            cancelToken.ThrowIfCancellationRequested();

            if (shouldProcess || _this.ShouldProcess($"Item: '{asset.GetPSPath()}' Destination: '{newFolder.GetPSPath()}'", "Copy Asset"))
            {
                msg = $"Copying asset {asset.GetPSPath()}";
                //reporter.WriteProgress(++index, asset.Name);
                reporter.WriteProgress(++index);

                // Get links, and if an entity with the same name exists in the linked folder of the target drive,
                // just create a link to it instead
                if (LinkAsset(_this, srcDrive, srcFolder, dstDrive, newFolder, asset, msg))
                {
                    continue;
                }

                target = newFolder.GetPSPath();

                bool bCredentialWarningNeeded = false;
                bool bSecretWarningNeeded = false;
                try
                {
                    Asset postingAsset = OrchCollectionExtensions.DeepCopy(asset);
                    postingAsset.Id = null;
                    postingAsset.Key = null;
                    postingAsset.Value = null;
                    postingAsset.CredentialStoreId = FindDstCredentialStore(_this,
                        srcDrive, dstDrive, newFolder, postingAsset.CredentialStoreId, msg)?.Id;
                    postingAsset.CreationTime = null;
                    postingAsset.CreatorUserId = null;
                    postingAsset.LastModificationTime = null;
                    postingAsset.LastModifierUserId = null;
                    postingAsset.FoldersCount = null;
                    // postingAsset.Path = null; // Not needed since it has the JsonIgnore attribute

                    if (postingAsset.ValueType == "Credential")
                    {
                        postingAsset.IntValue = null;
                        postingAsset.BoolValue = null;
                        postingAsset.StringValue = null;
                        // ExternalName (vault reference) takes priority — don't clobber it with a placeholder.
                        if (string.IsNullOrEmpty(postingAsset.ExternalName))
                        {
                            postingAsset.CredentialPassword = "!!!PLEASE UPDATE!!!";
                            bCredentialWarningNeeded = true;
                        }
                    }
                    else if (postingAsset.ValueType == "Secret")
                    {
                        // The server masks SecretValue on GET (always ""), so copying would POST an empty
                        // secret which the server rejects with "asset secret value cannot be null".
                        // Use a placeholder and warn so the operator rotates it — unless ExternalName
                        // (vault reference) is set, in which case preserve it and no placeholder is needed.
                        postingAsset.IntValue = null;
                        postingAsset.BoolValue = null;
                        postingAsset.StringValue = null;
                        if (string.IsNullOrEmpty(postingAsset.ExternalName))
                        {
                            postingAsset.SecretValue = "!!!PLEASE UPDATE!!!";
                            bSecretWarningNeeded = true;
                        }
                    }

                    if (postingAsset.UserValues is not null && postingAsset.UserValues.Count == 0)
                    {
                        postingAsset.UserValues = null;
                        postingAsset.ValueScope = "Global"; // ISSUE: Some assets had "PerRobot" despite having no UserValues
                    }
                    if (postingAsset.UserValues is not null)
                    {
                        List<AssetUserValue>? migratedUserValues = null;
                        foreach (var userValue in postingAsset.UserValues)
                        {
                            userValue.UserId = FindDstUser(_this, srcDrive, dstDrive, newFolder, userValue.UserId, msg, userMapping)?.Id;
                            if (userValue.UserId is null || userValue.UserId == 0)
                            {
                                continue;
                            }

                            if (userValue.MachineId is not null && userValue.MachineId != 0)
                            {
                                userValue.MachineId = FindDstMachine(_this,
                                    srcDrive, srcFolder,
                                    dstDrive, newFolder, userValue.MachineId, msg)?.Id;
                                // FindDstMachine already emits a WriteWarning when the machine is not
                                // assigned to the destination folder. Skip this UserValue silently here
                                // to avoid a duplicate warning+error pair.
                                if (userValue.MachineId is null || userValue.MachineId == 0)
                                {
                                    continue;
                                }
                            }

                            userValue.Id = null;
                            userValue.Value = null;
                            // userValue.Path = null; // Not needed since it has the JsonIgnore attribute
                            // userValue.Name = null; // Not needed since it has the JsonIgnore attribute
                            // userValue.PathName = null; // Not needed since it has the JsonIgnore attribute
                            userValue.CredentialStoreId = FindDstCredentialStore(_this,
                                srcDrive, dstDrive, newFolder, userValue.CredentialStoreId, msg)?.Id;

                            if (userValue.ValueType == "Credential")
                            {
                                userValue.IntValue = null;
                                userValue.BoolValue = null;
                                userValue.StringValue = null;
                                if (string.IsNullOrEmpty(userValue.ExternalName))
                                {
                                    userValue.CredentialPassword = "!!!PLEASE UPDATE!!!";
                                    bCredentialWarningNeeded = true;
                                }
                            }
                            else if (userValue.ValueType == "Secret")
                            {
                                userValue.IntValue = null;
                                userValue.BoolValue = null;
                                userValue.StringValue = null;
                                if (string.IsNullOrEmpty(userValue.ExternalName))
                                {
                                    userValue.SecretValue = "!!!PLEASE UPDATE!!!";
                                    bSecretWarningNeeded = true;
                                }
                            }
                            migratedUserValues ??= [];
                            migratedUserValues.Add(userValue);
                        }
                        if (migratedUserValues is null)
                        {
                            postingAsset.ValueScope = "Global";
                            postingAsset.UserValues = null;
                            if (!postingAsset.HasDefaultValue.GetValueOrDefault())
                            {
                                _this.WriteWarning($"{msg}: No applicable per-user values for the destination folder and the asset has no global default value. Skipping.");
                                continue;
                            }
                        }
                        postingAsset.UserValues = migratedUserValues;
                    }

                    var created = dstDrive.OrchAPISession.AddAsset(newFolder.Id ?? 0, postingAsset);

                    // This output clutters the screen, so maybe we don't need it..
                    //if (!shouldProcess && created is not null)
                    //{
                    //    created.Path = newFolder.GetPSPath();
                    //    _this.WriteObject(created);
                    //}

                    if (bCredentialWarningNeeded)
                    {
                        target = System.IO.Path.Combine(newFolder.GetPSPath(), created?.Name ?? "");
                        _this.WriteWarning($"'{target}': Please update credential asset passwords with Set-OrchCredentialAsset cmdlet.");
                    }

                    if (bSecretWarningNeeded)
                    {
                        target = System.IO.Path.Combine(newFolder.GetPSPath(), created?.Name ?? "");
                        _this.WriteWarning($"'{target}': Please update Secret asset values with Set-OrchSecretAsset cmdlet.");
                    }

                    // Decided to clear the cache in each Copy-OrchXxx. Not doing it here.
                    // When copying folders, the destination new folder's cache is empty anyway.
                    //dstDrive._dicAssets?.TryRemove(newFolder.Id.Value!, out _);
                }
                catch (Exception ex)
                {
                    _this.WriteError(new ErrorRecord(new OrchException(target, msg, ex), "CopyAssetError", ErrorCategory.InvalidOperation, target));
                }
            }
        }
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

    internal static void CopyQueueItem(IWritableHost _this,
        OrchDriveInfo srcDrive, Folder srcFolder, QueueDefinition srcQueue,
        OrchDriveInfo dstDrive, Folder newFolder, QueueDefinition dstQueue, ProgressReporter reporter)
    {
        // to be implemented
    }

    internal static void CopyQueues(IWritableHost _this,
        OrchDriveInfo srcDrive, Folder srcFolder, List<WildcardPattern>? wpName,
        OrchDriveInfo dstDrive, Folder newFolder, ProgressReporter reporter,
        bool shouldProcess, CancellationToken cancelToken)
    {
        // Considering that cmdlets may be executed consecutively in a script,
        // we shouldn't have been clearing the cache each time..
        // Comment out the below.

        // Clear this folder's cache so we can copy the latest state
        //srcDrive._dicQueueDefinitions?.TryRemove(srcFolder.Id ?? 0, out _);

        string target = srcFolder.GetPSPath();
        string msg = $"Copying queues";
        List<QueueDefinition> srcQueues = null;
        try
        {
            srcQueues = srcDrive.Queues.Get(srcFolder).FilterByWildcards(q => q?.Name, wpName).ToList();
        }
        catch (Exception ex)
        {
            _this.WriteError(new ErrorRecord(new OrchException(target, msg, ex), "GetQueueError", ErrorCategory.InvalidOperation, target));
        }

        if (srcQueues is null || !srcQueues.Any())
        {
            return;
        }

        reporter.TotalNum = srcQueues.Count;

        int index = 0;
        foreach (var queue in srcQueues.OrderBy(q => q.Name))
        {
            cancelToken.ThrowIfCancellationRequested();

            if (shouldProcess || _this.ShouldProcess($"Item: '{queue.GetPSPath()}' Destination: '{newFolder.GetPSPath()}'", "Copy Queue"))
            {
                target = srcFolder.GetPSPath();
                msg = $"Copying queue {queue.GetPSPath()}";
                //reporter.WriteProgress(++index, queue.Name);
                reporter.WriteProgress(++index);

                QueueDefinition postingQueue = null;

                // Get links, and if an entity with the same name exists in the linked folder of the target drive,
                // just create a link to it instead
                if (LinkQueue(_this, srcDrive, srcFolder, dstDrive, newFolder, queue))
                {
                    continue;
                }

                QueueDefinition srcQueue = null;
                try
                {
                    srcQueue = srcDrive.OrchAPISession.GetQueue(srcFolder.Id ?? 0, queue.Id ?? 0);
                }
                catch (Exception ex)
                {
                    _this.WriteError(new ErrorRecord(new OrchException(target, $"{msg}: Failed to get queue info", ex), "GetQueueError", ErrorCategory.InvalidOperation, target));
                    continue;
                }
                if (srcQueue is null)
                {
                    _this.WriteError(new ErrorRecord(new OrchException(target, $"{msg}: Failed to get queue info."), "GetQueueError", ErrorCategory.InvalidOperation, target));
                    continue;
                }

                Int64? releaseId = null;
                if (srcQueue.ReleaseId is not null && srcQueue.ReleaseId != 0)
                {
                    releaseId = FindDstRelease(_this,
                        srcDrive, srcFolder,
                        dstDrive, newFolder,
                        srcQueue.ReleaseId, msg)?.Id;
                }

                // ProcessScheduleId is the queue's server-managed back-reference to its
                // queue trigger. CreateQueue ignores a client-supplied value: verified by
                // POSTing /odata/QueueDefinitions with ProcessScheduleId set to a
                // non-existent id and to an id already bound to another queue — the created
                // queue came back with it null both times (there is no "free, assignable"
                // schedule id to test with, since a ProcessScheduleId only ever exists as a
                // trigger's reference to its own queue). The server sets it when a queue
                // trigger targeting this queue is created. So copying the src value here is
                // a harmless no-op, order-independent: the dst queue gets its real
                // ProcessScheduleId when the queue trigger is copied — by CopyTriggers in
                // the Copy-Item flow, or by a later standalone Copy-OrchTrigger. Copying a
                // queue trigger before its queue is handled gracefully (FindDstQueue skips
                // it with a warning), so neither order corrupts the relationship.
                postingQueue = new QueueDefinition()
                {
                    Name = srcQueue.Name,
                    Description = srcQueue.Description,
                    MaxNumberOfRetries = srcQueue.MaxNumberOfRetries,
                    AcceptAutomaticallyRetry = srcQueue.AcceptAutomaticallyRetry,
                    RetryAbandonedItems = srcQueue.RetryAbandonedItems,
                    EnforceUniqueReference = srcQueue.EnforceUniqueReference,
                    Encrypted = srcQueue.Encrypted,
                    ProcessScheduleId = srcQueue.ProcessScheduleId,
                    ReleaseId = releaseId,
                    SpecificDataJsonSchema = srcQueue.SpecificDataJsonSchema,
                    OutputDataJsonSchema = srcQueue.OutputDataJsonSchema,
                    AnalyticsDataJsonSchema = srcQueue.AnalyticsDataJsonSchema,
                    SlaInMinutes = srcQueue.SlaInMinutes,
                    RiskSlaInMinutes = srcQueue.RiskSlaInMinutes,
                    RetentionAction = srcQueue.RetentionAction ?? "Delete", // TODO: OR version dependent. Should probably be done in CreateQueue()
                    RetentionPeriod = srcQueue.RetentionPeriod ?? 30, // TODO: OR version dependent. Should probably be done in CreateQueue()
                    RetentionBucketId = FindDstBucket(_this,
                            srcDrive, srcFolder, srcQueue.RetentionBucketId,
                            dstDrive, newFolder, "Copy Process", msg)?.Id,
                    StaleRetentionAction = srcQueue.StaleRetentionAction ?? "Delete",
                    StaleRetentionPeriod = srcQueue.StaleRetentionPeriod ?? 180,
                    StaleRetentionBucketId = FindDstBucket(_this,
                            srcDrive, srcFolder, srcQueue.StaleRetentionBucketId,
                            dstDrive, newFolder, "Copy Process", msg)?.Id,
                    Tags = srcQueue.Tags
                };

                if (dstDrive.OrchAPISession.ApiVersion >= 19)
                {
                    // Newer versions require SLA or Trigger when ReleaseId is set.
                    // Set a default SLA to preserve the process association.
                    if (postingQueue.ReleaseId is not null
                        && (postingQueue.SlaInMinutes is null || postingQueue.SlaInMinutes == 0)
                        && (postingQueue.ProcessScheduleId is null || postingQueue.ProcessScheduleId == 0))
                    {
                        postingQueue.SlaInMinutes = 1440; // 24 hours
                    }

                    // "None" means "Keep". In Automation Cloud, "None" cannot be used.
                    if (string.IsNullOrEmpty(postingQueue.RetentionAction) || postingQueue.RetentionAction == "None")
                    {
                        postingQueue.RetentionAction = "Delete";
                    }
                    if (postingQueue.RetentionPeriod is null || postingQueue.RetentionPeriod == 0)
                    {
                        postingQueue.RetentionPeriod = 30;
                    }

                    if (string.IsNullOrEmpty(postingQueue.StaleRetentionAction) || postingQueue.StaleRetentionAction == "None")
                    {
                        postingQueue.StaleRetentionAction = "Delete";
                    }
                    if (postingQueue.StaleRetentionPeriod is null || postingQueue.StaleRetentionPeriod == 0)
                    {
                        postingQueue.StaleRetentionPeriod = 180;
                    }
                }

                try
                {
                    var created = dstDrive.OrchAPISession.CreateQueue(newFolder.Id ?? 0, postingQueue!);

                    // This output clutters the screen, so maybe we don't need it..
                    //if (!shouldProcess && created is not null)
                    //{
                    //    created.Path = newFolder.GetPSPath();
                    //    _this.WriteObject(created);
                    //}
                }
                catch (Exception ex)
                {
                    target = newFolder.GetPSPath();
                    _this.WriteError(new ErrorRecord(new OrchException(target, msg, ex), "CreateQueueError", ErrorCategory.InvalidOperation, target));
                }
            }
        }
    }

    private static RobotExecutor[]? MigrateExecutorRobots(IWritableHost _this, string msg,
        OrchDriveInfo srcDrive, Folder srcFolder,
        OrchDriveInfo dstDrive, Folder newFolder,
        RobotExecutor[]? executorRobots)
    {
        if (executorRobots is null) return null;

        List<RobotsFromFolderModel?> dstRobots = [];
        foreach (var executorRobot in executorRobots.Where(er => er is not null))
        {
            var dstExecutorRobot = FindDstRobotByUnattendedAccount(_this, srcDrive, srcFolder, dstDrive, newFolder, executorRobot.Id, msg);
            dstRobots.Add(dstExecutorRobot);
        }

        return dstRobots
            .Where(r => r?.Id is not null)
            .DistinctBy(r => r!.Id)
            .Select(r => new RobotExecutor() { Id = r!.Id })
            .ToArray();
    }

    private static MachineRobotSession[]? MigrateMachineRobots(IWritableHost _this, string msg,
        OrchDriveInfo srcDrive, Folder srcFolder,
        OrchDriveInfo dstDrive, Folder newFolder,
        MachineRobotSession[]? machineRobots,
        RobotExecutor[]? executorRobots = null)
    {
        if (machineRobots is null) return null;

        List<MachineRobotSession> dstSessions = [];

        if (srcFolder.ProvisionType == "Manual")
        {
            var srcSessions = srcDrive.Sessions.Get(srcFolder);
            foreach (var executorRobot in executorRobots ?? [])
            {
                var dstRobotId = FindDstRobotByUnattendedAccount(_this, srcDrive, srcFolder, dstDrive, newFolder, executorRobot?.Id, msg)?.Id;

                var srcSession = srcSessions.FirstOrDefault(s => s.Robot?.Id == executorRobot?.Id);
                var dstMachineId = FindDstMachine(_this, srcDrive, srcFolder, dstDrive, newFolder, srcSession?.MachineId, msg)?.Id;

                dstSessions.Add(new MachineRobotSession()
                {
                    RobotId = dstRobotId,
                    MachineId = dstMachineId
                });
            }
        }
        else
        {
            foreach (var machineRobot in machineRobots.Where(mr => mr is not null))
            {
                var robotId = FindDstRobotByUnattendedAccount(_this, srcDrive, srcFolder, dstDrive, newFolder, machineRobot.RobotId, msg)?.Id;
                var machineId = FindDstMachine(_this, srcDrive, srcFolder, dstDrive, newFolder, machineRobot.MachineId, msg)?.Id;

                if (robotId is not null || machineId is not null)
                {
                    dstSessions.Add(new MachineRobotSession()
                    {
                        // Migrate RobotId
                        RobotId = robotId,
                        MachineId = machineId,
                        SessionId = (machineId is null) ? null : FindDstSession(_this, srcDrive, srcFolder, dstDrive, newFolder, machineRobot.SessionId, msg)?.SessionId
                    });
                }
            }
        }

        dstSessions = dstSessions.DistinctBy(s => (s.RobotId, s.MachineId, s.SessionId)).ToList();
        if (dstSessions.Count != 0)
        {
            return dstSessions.ToArray();
        }
        return null;
    }

    internal static void CopyTriggers(IWritableHost _this,
        OrchDriveInfo srcDrive, Folder srcFolder, List<WildcardPattern>? wpName,
        OrchDriveInfo dstDrive, Folder newFolder, ProgressReporter reporter,
        bool shouldProcess, CancellationToken cancelToken)
    {
        string target = null;
        string msg = "Copying triggers";
        List<ProcessSchedule> srcTriggers = null;
        try
        {
            srcTriggers = srcDrive.GetTriggers(srcFolder).FilterByWildcards(t => t?.Name, wpName).ToList();
        }
        catch (Exception ex)
        {
            _this.WriteError(new ErrorRecord(new OrchException(srcFolder.GetPSPath(), msg, ex), "GetTriggerError", ErrorCategory.InvalidOperation, target));
            return;
        }

        reporter.TotalNum = srcTriggers.Count;

        int index = 0;
        foreach (var srcTrigger in srcTriggers.OrderBy(t => t.Name))
        {
            cancelToken.ThrowIfCancellationRequested();

            if (shouldProcess || _this.ShouldProcess($"Item: '{srcTrigger.GetPSPath()}' Destination: '{newFolder.GetPSPath()}'", "Copy Trigger"))
            {
                target = newFolder.GetPSPath();
                msg = $"Copying trigger {srcTrigger.GetPSPath()}";

                //reporter.WriteProgress(++index, srcTrigger.Name);
                reporter.WriteProgress(++index);

                var detailedSrcTrigger = srcDrive.GetTrigger(srcFolder, srcTrigger);

                var postingTrigger = OrchCollectionExtensions.DeepCopy(detailedSrcTrigger);
                if (postingTrigger is null) continue;

                postingTrigger.Id = null;
                postingTrigger.StartProcessNextOccurrence = null;
                postingTrigger.Key = null;
                postingTrigger.ReleaseKey = null;
                // postingTrigger.Path = null; // Not needed since it has the JsonIgnore attribute
                postingTrigger.TimeZoneIana = null;
                postingTrigger.ExternalJobKeyScheduler = null;
                postingTrigger.StartProcessCronSummary = null;
                postingTrigger.PackageName = null;
                if (postingTrigger.SpecificPriorityValue.HasValue) postingTrigger.JobPriority = null;

                // The Enabled property of triggers retrieved via API never seems to be null.
                // Also, if Enabled is set to null when POSTing a trigger, it becomes true.
                if (postingTrigger.Enabled.GetValueOrDefault())
                {
                    // Only warn when the source trigger's Enabled is true
                    _this.WriteWarning($"'{newFolder.GetPSPath()}\\{srcTrigger.Name}': This trigger will be disabled. Please enable it if necessary.");
                }
                // In any case, it is safer to set it to false.
                postingTrigger.Enabled = false; // Disable copied entities

                // Migrate queue ID
                // TODO: This condition might be unnecessary, just the body should suffice
                if (srcTrigger.QueueDefinitionId.GetValueOrDefault() != 0)
                {
                    postingTrigger.QueueDefinitionId = FindDstQueue(_this,
                        srcDrive, srcFolder,
                        dstDrive, newFolder, srcTrigger.QueueDefinitionId, msg)?.Id;
                    // If this is a queue trigger but the queue was not found, we probably don't need to copy it.
                    if (postingTrigger.QueueDefinitionId is null) continue;
                }

                // Migrate process ID
                postingTrigger.ReleaseId = FindDstRelease(_this,
                    srcDrive, srcFolder,
                    dstDrive, newFolder, srcTrigger.ReleaseId, msg)?.Id;
                if (postingTrigger.ReleaseId is null)
                {
                    // The API returns an error if ReleaseId is not populated, so we cannot continue
                    // The error has already been output by FindDstRelease()
                    continue;
                }

                // Migrate MachineRobots
                postingTrigger.MachineRobots = MigrateMachineRobots(_this, msg,
                    srcDrive, srcFolder,
                    dstDrive, newFolder,
                    postingTrigger.MachineRobots,
                    postingTrigger.ExecutorRobots);

                // Migrate ExecutorRobots
                postingTrigger.ExecutorRobots = MigrateExecutorRobots(_this, msg,
                    srcDrive, srcFolder,
                    dstDrive, newFolder,
                    postingTrigger.ExecutorRobots);

                // Migrate calendar Id
                postingTrigger.CalendarId = FindDstCalendar(_this, srcDrive, dstDrive,
                    postingTrigger.CalendarId, msg)?.Id;
                postingTrigger.CalendarKey = null;

                if (newFolder.ProvisionType != "Manual")
                {
                    postingTrigger.EnvironmentId = null;
                    postingTrigger.StartStrategy = 1;// What is StartStrategy exactly..
                }

                if (postingTrigger.StopProcessDate < DateTime.Now)
                {
                    _this.WriteWarning($"{msg}: The StopProcessDate is in the past ({postingTrigger.StopProcessDate.Value.ToLocalTime()}). Remove it before copying.");
                    postingTrigger.StopProcessDate = null;
                    postingTrigger.Enabled = false;
                }

                try
                {
                    var created = dstDrive.OrchAPISession.PostProcessSchedule(newFolder.Id ?? 0, postingTrigger);

                    // This output clutters the screen, so maybe we don't need it..
                    //if (!shouldProcess && created is not null)
                    //{
                    //    created.Path = newFolder.GetPSPath();
                    //    _this.WriteObject(created);
                    //}
                }
                catch (Exception ex)
                {
                    _this.WriteError(new ErrorRecord(new OrchException(target, msg, ex), "CreateTriggerError", ErrorCategory.InvalidOperation, target));
                }
            }
        }
    }

    internal static void CopyApiTriggers(IWritableHost _this,
        OrchDriveInfo srcDrive, Folder srcFolder, List<WildcardPattern>? wpName,
        OrchDriveInfo dstDrive, Folder newFolder, ProgressReporter reporter,
        bool shouldProcess, CancellationToken cancelToken)
    {
        // TODO: Does this succeed in v14?
        // TODO: Does this succeed in v15?
        if (srcDrive.OrchAPISession.ApiVersion < 14) return;

        string target = srcFolder.GetPSPath();
        string msg = $"Copying API triggers";

        List<HttpTrigger> srcTriggers = null;
        try
        {
            srcTriggers = srcDrive.ApiTriggers.Get(srcFolder).FilterByWildcards(t => t?.Name, wpName).ToList();
        }
        catch (Exception ex)
        {
            _this.WriteError(new ErrorRecord(new OrchException(target, msg, ex), "GetApiTriggerError", ErrorCategory.InvalidOperation, target));
            return;
        }

        reporter.TotalNum = srcTriggers.Count;
        target = newFolder.GetPSPath();

        int index = 0;
        foreach (var trigger in srcTriggers.OrderBy(t => t.Name))
        {
            cancelToken.ThrowIfCancellationRequested();

            if (shouldProcess || _this.ShouldProcess($"Item: '{trigger.GetPSPath()}' Destination: '{newFolder.GetPSPath()}'", "Copy ApiTrigger"))
            {
                msg = $"Copying API trigger {trigger.GetPSPath()}";
                //reporter.WriteProgress(++index, trigger.Name);
                reporter.WriteProgress(++index);

                //var detailedTrigger = srcDrive.OrchAPISession.GetHttpTrigger(srcFolder.Id ?? 0, trigger.Id!);
                //detailedTrigger ??= trigger;

                var postingTrigger = OrchCollectionExtensions.DeepCopy(trigger);
                postingTrigger.Id = null;
                postingTrigger.OrganizationUnitId = null;
                // CallbackMode is read-only: the create/edit endpoint never accepts
                // it (the web UI doesn't post it either) and the server rejects any
                // value other than its own default — echoing the source value back
                // would, on a stricter tenant/version, fail the whole POST with
                // "httpTrigger must not be null". Drop it like Id/OrganizationUnitId
                // and let the server assign the default. Mirrors New-OrchApiTrigger,
                // which no longer exposes -CallbackMode.
                postingTrigger.CallbackMode = null;
                // postingTrigger.Path = null; // Not needed since it has the JsonIgnore attribute

                // Migrate ReleaseKey
                var dstRelease = FindDstRelease(_this,
                        srcDrive, srcFolder,
                        dstDrive, newFolder, trigger.Release?.Id, msg);
                postingTrigger!.ReleaseKey = dstRelease?.Key;
                if (postingTrigger!.ReleaseKey is null) continue;

                // Migrate MachineRobots
                postingTrigger.MachineRobots = MigrateMachineRobots(_this, msg,
                    srcDrive, srcFolder,
                    dstDrive, newFolder,
                    postingTrigger.MachineRobots);

                try
                {
                    var created = dstDrive.OrchAPISession.CreateHttpTrigger(newFolder.Id ?? 0, postingTrigger);

                    // This output clutters the screen, so maybe we don't need it..
                    //if (!shouldProcess && created is not null)
                    //{
                    //    created.Path = newFolder.GetPSPath();
                    //    _this.WriteObject(created);
                    //}
                }
                catch (Exception ex)
                {
                    _this.WriteError(new ErrorRecord(new OrchException(target, msg, ex), "CopyApiTriggerError", ErrorCategory.InvalidOperation, target));
                }
            }
        }
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

    internal static void CopyBuckets(IWritableHost _this,
        OrchDriveInfo srcDrive, Folder srcFolder, List<WildcardPattern>? wpName,
        OrchDriveInfo dstDrive, Folder newFolder, ProgressReporter reporter,
        bool shouldProcess, CancellationToken cancelToken)
    {
        // Considering that cmdlets may be executed consecutively in a script,
        // we shouldn't have been clearing the cache each time..
        // Comment out the below.

        // Clear this folder's cache so we can copy the latest state
        //srcDrive._dicBuckets?.TryRemove(srcFolder.Id ?? 0, out _);

        string target = srcFolder.GetPSPath();
        string msg = $"Copying buckets";

        List<Bucket> srcBuckets;
        try
        {
            srcBuckets = srcDrive.Buckets.Get(srcFolder).FilterByWildcards(b => b?.Name, wpName).ToList();
        }
        catch (Exception ex)
        {
            _this.WriteError(new ErrorRecord(new OrchException(srcFolder.GetPSPath(), msg, ex), "GetBucketError", ErrorCategory.InvalidOperation, target));
            return;
        }

        reporter.TotalNum = srcBuckets.Count;
        target = newFolder.GetPSPath();

        int index = 0;
        foreach (var bucket in srcBuckets.OrderBy(b => b.Name))
        {
            cancelToken.ThrowIfCancellationRequested();

            if (shouldProcess || _this.ShouldProcess($"Item: '{bucket.GetPSPath()}' Destination: '{newFolder.GetPSPath()}'", "Copy Bucket"))
            {
                msg = $"Copying bucket {System.IO.Path.Combine(bucket.GetPSPath())}";
                //reporter.WriteProgress(++index, bucket.Name);
                reporter.WriteProgress(++index);

                // Get links, and if an entity with the same name exists in the linked folder of the target drive,
                // just create a link to it instead
                if (LinkBucket(_this, srcDrive, srcFolder, dstDrive, newFolder, bucket))
                {
                    continue;
                }

                var postingBucket = OrchCollectionExtensions.DeepCopy(bucket);
                postingBucket.Id = null;
                // postingBucket.Path = null; // Not needed since it has the JsonIgnore attribute
                postingBucket.FoldersCount = null;
                postingBucket.Identifier = Guid.NewGuid().ToString();

                bool bPasswordExists = !string.IsNullOrEmpty(postingBucket.Password);
                if (bPasswordExists)
                {
                    postingBucket.Password = "!!!PLEASE UPDATE!!!";
                }
                postingBucket.CredentialStoreId = FindDstCredentialStore(_this,
                    srcDrive, dstDrive, newFolder, bucket.CredentialStoreId, msg)?.Id;

                try
                {
                    var created = dstDrive.OrchAPISession.PostBucket(newFolder.Id ?? 0, postingBucket);

                    // This output clutters the screen, so maybe we don't need it..
                    //if (!shouldProcess && created is not null)
                    //{
                    //    created.Path = newFolder.GetPSPath();
                    //    _this.WriteObject(created);
                    //}

                    if (bPasswordExists)
                    {
                        _this.WriteWarning($"'{System.IO.Path.Combine(newFolder.GetPSPath(), bucket.Name!)}': Please update the storage bucket Password with Update-OrchBucket cmdlet.");
                    }
                }
                catch (Exception ex)
                {
                    _this.WriteError(new ErrorRecord(new OrchException(target, msg, ex), "CopyBucketError", ErrorCategory.InvalidOperation, target));
                }
            }
        }
    }

    // There is no need to copy TestCases.
    // TestCases are created by copying the test process package.

    internal static TestCaseDefinition? FindDstTestCase(IWritableHost _this,
        OrchDriveInfo srcDrive, Folder srcFolder, Int64? srcDefinitionId,
        OrchDriveInfo dstDrive, Folder newFolder, string msg)
    {
        var srcTestCases = srcDrive.TestCases.Get(srcFolder);
        var srcTestCase = srcTestCases.FirstOrDefault(ts => ts.Id == srcDefinitionId);
        if (srcTestCase is null)
        {
            _this.WriteWarning($"{msg}: {srcFolder.GetPSPath()} does not have test case with Id = {srcDefinitionId}.");
            return null;
        }

        var dstTestCases = dstDrive.TestCases.Get(newFolder);
        // Case-insensitive compound match. Test entity names follow the
        // same Orchestrator name-uniqueness rule as Buckets (rejected on
        // create when a same-name entity differs only in case), so a
        // case-sensitive '==' lookup here would miss a dst test case that
        // exists under a different case and cause a spurious "not found"
        // warning. PackageIdentifier is intrinsically case-stable but
        // matched the same way for symmetry.
        var dstTestCase = dstTestCases.FirstOrDefault(tc =>
            string.Equals(tc.PackageIdentifier, srcTestCase.PackageIdentifier, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(tc.Name, srcTestCase.Name, StringComparison.OrdinalIgnoreCase));
        if (dstTestCase is null)
        {
            _this.WriteWarning($"{msg}: {newFolder.GetPSPath()} does not have test case with PackageIdentifier = '{srcTestCase.PackageIdentifier}' and Name = '{srcTestCase.Name}'.");
        }
        return dstTestCase;
    }

    internal static void CopyTestSets(IWritableHost _this,
        OrchDriveInfo srcDrive, Folder srcFolder, List<WildcardPattern>? wpName,
        OrchDriveInfo dstDrive, Folder newFolder, ProgressReporter reporter,
        bool shouldProcess, CancellationToken cancelToken)
    {
        if (newFolder.FolderType == "Personal") return;

        // Test entities do not exist in v16 and earlier; v15 (e.g. Orch 22.10)
        // returns HTTP 500 from /odata/TestSets rather than 404, so without the
        // gate users on older Orch see "An internal error occurred during your
        // request!" noise in every Copy-Item recursion. v17+ has stable APIs.
        if (srcDrive.OrchAPISession.ApiVersion < 17) return;
        if (dstDrive.OrchAPISession.ApiVersion < 17) return;

        string msg = $"Copying test sets";

        try
        {
            var srcTestSets = srcDrive.TestSets.Get(srcFolder).FilterByWildcards(b => b?.Name, wpName).ToList();
            reporter.TotalNum = srcTestSets.Count;

            int index = 0;
            foreach (var ts in srcTestSets.OrderBy(t => t.Name))
            {
                cancelToken.ThrowIfCancellationRequested();

                string target = $"Item: '{ts.GetPSPath()}' Destination: '{newFolder.GetPSPath()}'";
                if (shouldProcess || _this.ShouldProcess(target, "Copy TestSet"))
                {
                    msg = $"Copying test set {ts.GetPSPath()}";
                    //reporter.WriteProgress(++index, testSetSchedule.Name);
                    reporter.WriteProgress(++index);
                    try
                    {
                        var postingTestSet = srcDrive.OrchAPISession.GetTestSetForEdit(srcFolder.Id ?? 0, ts.Id ?? 0);

                        if (postingTestSet is not null)
                        {
                            postingTestSet.Id = null;
                            postingTestSet.CreationTime = null;
                            foreach (var p in postingTestSet.Packages ?? [])
                            {
                                p.Id = null;
                                p.TestSetId = null;
                                p.TestSet = null;
                                p.LastModificationTime = null;
                                p.LastModifierUserId = null;
                                p.CreationTime = null;
                                p.CreatorUserId = null;
                            }

                            foreach (var tc in postingTestSet.TestCases ?? [])
                            {
                                tc.Id = null;
                                tc.TestSetId = null;
                                tc.Definition = null;
                                tc.LastModificationTime = null;
                                tc.LastModifierUserId = null;
                                tc.CreationTime = null;
                                tc.CreatorUserId = null;

                                tc.DefinitionId = FindDstTestCase(_this, srcDrive, srcFolder, tc.DefinitionId, dstDrive, newFolder, msg)?.Id;

                                tc.ReleaseId = FindDstRelease(_this,
                                    srcDrive, srcFolder,
                                    dstDrive, newFolder, tc.ReleaseId, msg)?.Id;
                            }

                            postingTestSet.RobotId = FindDstRobot(_this,
                                srcDrive, dstDrive, newFolder, postingTestSet.RobotId, "Migrating test set robot")?.Id;

                            dstDrive.OrchAPISession.CreateTestSet(newFolder.Id ?? 0, postingTestSet);
                        }
                    }
                    catch (Exception ex)
                    {
                        _this.WriteError(new ErrorRecord(new OrchException(srcFolder.GetPSPath(), msg, ex), "CreateTestSetError", ErrorCategory.InvalidOperation, srcFolder));
                        return;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _this.WriteError(new ErrorRecord(new OrchException(srcFolder.GetPSPath(), msg, ex), "GetTestSetError", ErrorCategory.InvalidOperation, srcFolder));
            return;
        }
    }

    // Returns the ContentJsonSchema with its top-level "required" array removed, plus a
    // flag for whether anything changed. Used to temporarily relax a Test Data Queue's
    // schema during migration so items missing now-required fields are still accepted;
    // the original schema is restored afterwards. Returns the input unchanged
    // (changed == false) for empty input, a schema with no "required", or unparseable JSON.
    private static (string? schema, bool changed) RelaxRequiredSchema(string? contentJsonSchema)
    {
        if (string.IsNullOrWhiteSpace(contentJsonSchema)) return (contentJsonSchema, false);
        try
        {
            if (JsonNode.Parse(contentJsonSchema) is JsonObject obj && obj.Remove("required"))
            {
                return (obj.ToJsonString(), true);
            }
        }
        catch (JsonException)
        {
            // Malformed schema -> leave it as-is; create will surface any real problem.
        }
        return (contentJsonSchema, false);
    }

    internal static void CopyTestDataQueueItems(IWritableHost _this,
        OrchDriveInfo srcDrive, Folder srcFolder, TestDataQueue srcTestDataQueue,
        OrchDriveInfo dstDrive, Folder newFolder, string dstTestDataQueueName, bool shouldProcess)
    {
        if (newFolder.FolderType == "Personal") return;

        // TestDataQueue endpoints stabilised in v18; older Orch returns HTTP 500.
        if (srcDrive.OrchAPISession.ApiVersion < 18) return;
        if (dstDrive.OrchAPISession.ApiVersion < 18) return;

        ICollection<TestDataQueueItem> items;
        try
        {
            items = srcDrive.TestDataQueueItems.Get(srcFolder, srcTestDataQueue);
        }
        catch (Exception ex)
        {
            _this.WriteError(new ErrorRecord(new OrchException(srcTestDataQueue.GetPSPath(), $"CopyTestDataQueueItem: {ex.Message}"), "CopyFolderError", ErrorCategory.InvalidOperation, srcTestDataQueue));
            return;
        }

        var contents = items
            .Select(i => i?.ContentJson)
            .Where(c => !string.IsNullOrEmpty(c))
            .Select(c => c!)
            .ToList();
        if (contents.Count == 0) return;

        if (!(shouldProcess || _this.ShouldProcess($"Items: '{srcTestDataQueue.GetPSPath()}' Destination: '{newFolder.GetPSPath()}'", "Copy TestDataQueueItem")))
            return;

        // Upload in batches. A batch rejected for a content-schema violation (409)
        // means one or more rows are individually invalid, so fall back to one item
        // at a time: the good rows still land and only the bad rows are reported. Any
        // other failure (auth / not-found / transient) would reject every remaining
        // row the same way, so report it once and stop instead of flooding the caller.
        // Same strategy as Import-OrchTestDataQueueItem.
        const int batchSize = 100;
        long dstFolderId = newFolder.Id ?? 0;
        foreach (var batch in contents.Chunk(batchSize))
        {
            try
            {
                dstDrive.OrchAPISession.AddTestDataQueueItems(dstFolderId, dstTestDataQueueName, "[" + string.Join(",", batch) + "]");
            }
            catch (Exception exBatch) when (TestDataQueueUploadPolicy.IsPerRowDataError(exBatch))
            {
                foreach (var content in batch)
                {
                    try
                    {
                        dstDrive.OrchAPISession.AddTestDataQueueItem(dstFolderId, dstTestDataQueueName, content);
                    }
                    catch (Exception exItem)
                    {
                        _this.WriteError(new ErrorRecord(new OrchException(newFolder.GetPSPath(), $"CopyTestDataQueueItem ('{dstTestDataQueueName}'): an item was rejected: {exItem.Message}"), "CopyTestDataQueueItemError", ErrorCategory.InvalidOperation, newFolder));
                    }
                }
            }
            catch (Exception exBatch)
            {
                _this.WriteError(new ErrorRecord(new OrchException(newFolder.GetPSPath(), $"CopyTestDataQueueItem ('{dstTestDataQueueName}'): upload failed; the remaining items were not copied: {exBatch.Message}"), "CopyTestDataQueueItemError", ErrorCategory.InvalidOperation, newFolder));
                break;
            }
        }
    }

    internal static TestSet? FindDstTestSet(IWritableHost _this,
            OrchDriveInfo srcDrive, Folder srcFolder, Int64? srcTestSetId,
            OrchDriveInfo dstDrive, Folder newFolder, string msg)
    {
        var srcTestSets = srcDrive.TestSets.Get(srcFolder);
        var srcTestSet = srcTestSets.FirstOrDefault(ts => ts.Id == srcTestSetId);
        if (srcTestSet is null)
        {
            _this.WriteWarning($"{msg}: {srcFolder.GetPSPath()} does not have test set with Id = {srcTestSetId}.");
            return null;
        }

        var dstTestSets = dstDrive.TestSets.Get(newFolder);
        // Case-insensitive name match (same rationale as FindDstBucket /
        // FindDstTestCase). Original '==' was case-sensitive; missed dst
        // test sets that existed under a different case.
        var dstTestSet = ResolveDstByName(dstTestSets, srcTestSet.Name, ts => ts.Name);
        if (dstTestSet is null)
        {
            _this.WriteWarning($"{msg}: {newFolder.GetPSPath()} does not have test set with Name = '{srcTestSet.Name}'.");
        }
        return dstTestSet;
    }

    internal static void CopyTestSetSchedules(IWritableHost _this,
        OrchDriveInfo srcDrive, Folder srcFolder, List<WildcardPattern>? wpName,
        OrchDriveInfo dstDrive, Folder newFolder, ProgressReporter reporter,
        bool shouldProcess, CancellationToken cancelToken)
    {
        if (newFolder.FolderType == "Personal") return;

        // TestSetSchedule endpoints stabilised in v18; older Orch returns HTTP 500.
        if (srcDrive.OrchAPISession.ApiVersion < 18) return;
        if (dstDrive.OrchAPISession.ApiVersion < 18) return;

        string msg = $"Copying test schedules";

        List<TestSetSchedule> srcTestSetSchedules;
        try
        {
            srcTestSetSchedules = srcDrive.TestSetSchedules.Get(srcFolder).FilterByWildcards(b => b?.Name, wpName).ToList();
        }
        catch (Exception ex)
        {
            _this.WriteError(new ErrorRecord(new OrchException(srcFolder.GetPSPath(), msg, ex), "GetTestSetScheduleError", ErrorCategory.InvalidOperation, srcFolder));
            return;
        }

        reporter.TotalNum = srcTestSetSchedules.Count;

        int index = 0;
        foreach (var testSetSchedule in srcTestSetSchedules.OrderBy(t => t.Name))
        {
            cancelToken.ThrowIfCancellationRequested();

            string target = $"Item: '{testSetSchedule.GetPSPath()}' Destination: '{newFolder.GetPSPath()}'";
            if (shouldProcess || _this.ShouldProcess(target, "Copy TestSetSchedule"))
            {
                msg = $"Copying test schedule {System.IO.Path.Combine(srcFolder.GetPSPath(), testSetSchedule.Name!)}";
                //reporter.WriteProgress(++index, testSetSchedule.Name);
                reporter.WriteProgress(++index);

                var postingTestSetSchedule = OrchCollectionExtensions.DeepCopy(testSetSchedule);
                postingTestSetSchedule.Id = null;
                // postingTestSetSchedule.Path = null; // Not needed since it has the JsonIgnore attribute
                postingTestSetSchedule.TestSetId = FindDstTestSet(_this,
                    srcDrive, srcFolder, postingTestSetSchedule.TestSetId,
                    dstDrive, newFolder, msg)?.Id;

                postingTestSetSchedule.CalendarId = FindDstCalendar(_this,
                    srcDrive, dstDrive, postingTestSetSchedule.CalendarId, msg)?.Id;

                try
                {
                    dstDrive.OrchAPISession.CreateTestSetSchedule(newFolder.Id ?? 0, postingTestSetSchedule);
                }
                catch (Exception ex)
                {
                    _this.WriteError(new ErrorRecord(new OrchException(newFolder.GetPSPath(), msg, ex), "CopyApiTriggerError", ErrorCategory.InvalidOperation, target));
                }
            }
        }
    }

    internal static void CopyTestDataQueues(IWritableHost _this,
        OrchDriveInfo srcDrive, Folder srcFolder, List<WildcardPattern>? wpName,
        OrchDriveInfo dstDrive, Folder newFolder, ProgressReporter reporter,
        bool shouldProcess, CancellationToken cancelToken)
    {
        if (newFolder.FolderType == "Personal") return;

        // TestDataQueue endpoints stabilised in v18; older Orch returns HTTP 500.
        // (Pre-fix: the second guard accidentally checked srcDrive twice; fixed below
        // to guard dstDrive as the symmetric src/dst pair the other helpers use.)
        if (srcDrive.OrchAPISession.ApiVersion < 18) return;
        if (dstDrive.OrchAPISession.ApiVersion < 18) return;

        string msg = $"Copying test data queues";

        List<TestDataQueue> srcTestDataQueues;
        try
        {
            srcTestDataQueues = srcDrive.TestDataQueues.Get(srcFolder).FilterByWildcards(b => b?.Name, wpName).ToList();
        }
        catch (Exception ex)
        {
            _this.WriteError(new ErrorRecord(new OrchException(srcFolder.GetPSPath(), msg, ex), "GetTestDataQueueError", ErrorCategory.InvalidOperation, srcFolder));
            return;
        }

        reporter.TotalNum = srcTestDataQueues.Count;

        int index = 0;
        foreach (var testDataQueue in srcTestDataQueues
            .Where(e => !e.IsDeleted.GetValueOrDefault())
            .OrderBy(q => q.Name))
        {
            cancelToken.ThrowIfCancellationRequested();

            string target = $"Item: '{testDataQueue.GetPSPath()}' Destination: '{newFolder.GetPSPath()}'";
            if (shouldProcess || _this.ShouldProcess(target, "Copy TestDataQueue"))
            {
                msg = $"Copying test data queue {System.IO.Path.Combine(srcFolder.GetPSPath(), testDataQueue.Name!)}";
                //reporter.WriteProgress(++index, testDataQueue.Name);
                reporter.WriteProgress(++index);

                var postingTestDataQueue = OrchCollectionExtensions.DeepCopy(testDataQueue);
                postingTestDataQueue.Id = null;
                // postingTestDataQueue.Path = null; // Not needed since it has the JsonIgnore attribute
                postingTestDataQueue.ItemsCount = null;
                postingTestDataQueue.ConsumedItemsCount = null;
                postingTestDataQueue.LastModificationTime = null;
                postingTestDataQueue.LastModifierUserId = null;
                postingTestDataQueue.CreationTime = null;
                postingTestDataQueue.CreatorUserId = null;

                // Create the queue with the schema's top-level `required` list removed
                // so items that predate a schema change (and so omit a now-required
                // field) are still accepted, then restore the original schema after the
                // items are uploaded. The destination queue ends up identical to the
                // source — required is enforced for new items, legacy items are intact.
                string? originalSchema = postingTestDataQueue.ContentJsonSchema;
                var (relaxedSchema, relaxed) = RelaxRequiredSchema(originalSchema);
                if (relaxed) postingTestDataQueue.ContentJsonSchema = relaxedSchema;

                try
                {
                    var created = dstDrive.OrchAPISession.CreateTestDataQueue(newFolder.Id ?? 0, postingTestDataQueue);
                    CopyTestDataQueueItems(_this,
                        srcDrive, srcFolder, testDataQueue,
                        dstDrive, newFolder, testDataQueue.Name!, true);

                    if (relaxed && created?.Id is long createdId)
                    {
                        try
                        {
                            postingTestDataQueue.ContentJsonSchema = originalSchema;
                            dstDrive.OrchAPISession.UpdateTestDataQueue(newFolder.Id ?? 0, createdId, postingTestDataQueue);
                        }
                        catch (Exception exRestore)
                        {
                            _this.WriteWarning($"{msg}: items copied, but restoring the original schema (required fields) failed: {exRestore.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _this.WriteError(new ErrorRecord(new OrchException(newFolder.GetPSPath(), msg, ex), "CopyTestDataQueueError", ErrorCategory.InvalidOperation, target));
                }
            }
        }
    }

    internal static void CopyActionCatalogs(IWritableHost _this,
        OrchDriveInfo srcDrive, Folder srcFolder, List<WildcardPattern>? wpName,
        OrchDriveInfo dstDrive, Folder newFolder, ProgressReporter reporter,
        bool shouldProcess, CancellationToken cancelToken)
    {
        // TODO: Is this version number needed?
        //if (srcDrive.OrchAPISession.ApiVersion < 14) return;

        string msg = $"Copying action catalogs";

        List<TaskCatalog> srcTaskCatalogs;
        try
        {
            srcTaskCatalogs = srcDrive.ActionCatalogs.Get(srcFolder).FilterByWildcards(b => b?.Name, wpName).ToList();
        }
        catch (Exception ex)
        {
            _this.WriteError(new ErrorRecord(new OrchException(srcFolder.GetPSPath(), msg, ex), "GetActionCatalogError", ErrorCategory.InvalidOperation, srcFolder));
            return;
        }

        reporter.TotalNum = srcTaskCatalogs.Count;

        int index = 0;
        foreach (var srcTaskCatalog in srcTaskCatalogs.OrderBy(t => t.Name))
        {
            cancelToken.ThrowIfCancellationRequested();

            string target = $"Item: '{srcTaskCatalog.GetPSPath()}' Destination: '{newFolder.GetPSPath()}'";
            if (shouldProcess || _this.ShouldProcess(target, "Copy ActionCatalog"))
            {
                msg = $"Copying action catalog {System.IO.Path.Combine(srcFolder.GetPSPath(), srcTaskCatalog.Name!)}";
                //reporter.WriteProgress(++index, testDataQueue.Name);
                reporter.WriteProgress(++index);

                var postingTaskCatalog = OrchCollectionExtensions.DeepCopy(srcTaskCatalog);
                postingTaskCatalog.Id = null;
                // postingTaskCatalog.Path = null; // Not needed since it has the JsonIgnore attribute
                postingTaskCatalog.Key = null;
                postingTaskCatalog.CreationTime = null;
                postingTaskCatalog.FoldersCount = null;

                if (postingTaskCatalog.RetentionBucketId is not null)
                {
                    var destinationBucket = FindDstBucket(_this,
                        srcDrive, srcFolder, postingTaskCatalog.RetentionBucketId,
                        dstDrive, newFolder, "Copy ActionCatalog", msg);
                    postingTaskCatalog.RetentionBucketId = destinationBucket?.Id;
                    postingTaskCatalog.RetentionBucketName = destinationBucket?.Name;
                }

                try
                {
                    dstDrive.OrchAPISession.CreateTaskCatalog(newFolder.Id ?? 0, postingTaskCatalog);
                    dstDrive.ActionCatalogs.ClearCache(newFolder);
                }
                catch (Exception ex)
                {
                    _this.WriteError(new ErrorRecord(new OrchException(newFolder.GetPSPath(), msg, ex), "CopyApiTriggerError", ErrorCategory.InvalidOperation, target));
                }
            }
        }
    }

    // The newly created destination folder automatically has the current user assigned with the Folder Administrator Role.
    // If the current user is not assigned to the source folder, unassign them from this folder.
    // If the current user is assigned to the source folder but does not have the Folder Administrator Role,
    // remove the Folder Administrator Role from the current user.
    // This operation must be performed after all other copy operations are complete. (Otherwise, the other copy operations will fail.)
    internal static void UnassignMyselfAtNewFolder(
        OrchDriveInfo srcDrive, Folder srcFolder,
        OrchDriveInfo dstDrive, Folder newFolder)
    {
        var dstCurrentUser = dstDrive.CurrentUser.Get();
        if (dstCurrentUser is null) return;

        var srcFolderUsers = srcDrive.FolderUsersWithNoInherited.Get(srcFolder);
        var srcMyself = srcFolderUsers?.FirstOrDefault(u => string.Compare(u.UserEntity!.UserName, dstCurrentUser.UserName, StringComparison.OrdinalIgnoreCase) == 0);
        if (srcMyself is null)
        {
            // The current user is not assigned to the source folder, so unassign them from the destination
            try
            {
                dstDrive.OrchAPISession.UnassignUserFromFolder(newFolder.Id ?? 0, dstCurrentUser.Id ?? 0);
                dstDrive.FolderUsersWithInherited.ClearCache(newFolder);
                dstDrive.FolderUsersWithNoInherited.ClearCache(newFolder);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UnassignUserFromFolder failed for '{newFolder.GetPSPath()}': {ex.Message}");
            }
            return;
        }

        // If the current user does not have the Folder Administrator role in the source folder,
        // remove the Folder Administrator role from the current user in the destination folder
        bool srcIhaveFolderAdministratorRole = srcMyself.Roles?.Any(r => string.Compare(r.Name, "Folder Administrator", StringComparison.OrdinalIgnoreCase) == 0) ?? false;
        if (!srcIhaveFolderAdministratorRole)
        {
            var dstFolderUsers = dstDrive.FolderUsersWithNoInherited.Get(newFolder);
            var dstMyself = dstFolderUsers?.FirstOrDefault(u => string.Compare(u.UserEntity!.UserName, dstCurrentUser.UserName, StringComparison.OrdinalIgnoreCase) == 0);
            if (dstMyself is null || dstMyself.Roles is null) return;

            var folderAdministratorRole = dstMyself.Roles?.FirstOrDefault(r => string.Compare(r.Name, "Folder Administrator", StringComparison.OrdinalIgnoreCase) == 0);
            if (folderAdministratorRole is null) return;

            dstMyself.Roles!.Remove(folderAdministratorRole);
            dstDrive.OrchAPISession.AssignUser(newFolder.Id ?? 0, dstMyself.Id ?? 0, dstMyself.Roles.Select(r => r.Id ?? 0)); ;
        }
    }

    private bool CopyItemRecurse(
        OrchDriveInfo srcDrive,
        Folder srcFolder,
        OrchDriveInfo dstDrive,
        Folder dstFolder,
        bool recurse,
        CancellationToken cancelToken,
        Dictionary<string, string>? userMapping = null)
    {
        if (srcFolder == dstFolder)
        {
            WriteError(new ErrorRecord(new OrchException(srcFolder.GetPSPath(),
                "Cannot copy a folder to itself."),
                "CopyFolderError", ErrorCategory.InvalidOperation, srcFolder));
            return false;
        }

        Folder? destinationWorkspace = null;
        if (srcFolder.FolderType == "Personal")
        {
            if (ExcludeEntities) return false;
            if (dstFolder != dstDrive.RootFolder)
            {
                // If dst is directly specified, copy contents directly without creating a subfolder
                destinationWorkspace = dstFolder;
            }

            // If UserMappingCsv is provided, find the corresponding dst workspace based on OwnerId
            // (only if dst is not explicitly specified)
            if (destinationWorkspace is null && userMapping is not null)
            {
                // Determine the source user name from the src workspace's OwnerId
                var srcWorkspace = srcDrive.PersonalWorkspaces.Get().FirstOrDefault(pw => pw.Id == srcFolder.Id);
                string? srcOwnerUserName = null;
                if (srcWorkspace?.OwnerId is not null)
                {
                    srcOwnerUserName = srcDrive.Users.Get().FirstOrDefault(u => u.Id == srcWorkspace.OwnerId)?.UserName;
                }
                // If OwnerId is empty, infer from the folder name
                if (string.IsNullOrEmpty(srcOwnerUserName) && srcFolder.DisplayName?.EndsWith("'s workspace") == true)
                {
                    srcOwnerUserName = srcFolder.DisplayName[..^"'s workspace".Length];
                }

                if (!string.IsNullOrEmpty(srcOwnerUserName)
                    && userMapping.TryGetValue(srcOwnerUserName, out var dstUserName)
                    && !string.IsNullOrEmpty(dstUserName))
                {
                    // Get the dst user's Id and search PersonalWorkspaces by OwnerId
                    var dstUser = dstDrive.Users.Get().FirstOrDefault(u => string.Compare(u.UserName, dstUserName, StringComparison.OrdinalIgnoreCase) == 0);
                    if (dstUser?.Id is not null)
                    {
                        var dstWorkspace = dstDrive.PersonalWorkspaces.Get().FirstOrDefault(pw => pw.OwnerId == dstUser.Id);
                        if (dstWorkspace is not null)
                        {
                            destinationWorkspace = dstDrive.GetFolders().FirstOrDefault(f => f.Id == dstWorkspace.Id);
                        }
                    }
                }
            }

            // If not found via mapping, fall back to searching for a folder with the same name
            destinationWorkspace ??= dstDrive.GetFolder(srcFolder.DisplayName!);

            if (destinationWorkspace is null)
            {
                // A personal workspace folder can't be discovered through the API until
                // it has been opened ("start exploring") in the Orchestrator web UI — and
                // that has to happen on both tenants. This isn't a failure of the copy, so
                // surface it as guidance (not an error), then skip this workspace.
                WriteWarning(
                    $"To copy the personal workspace '{srcFolder.GetPSPath()}', the corresponding personal workspace folder must first be opened (start exploring) in the Orchestrator web UI on both the source ({srcDrive.NameColonSeparator}) and destination ({dstDrive.NameColonSeparator}) tenants. This is not supported through the API and must be done manually. Once both are explored, run `Clear-OrchCache` and copy again.");
                return false;
            }
        }
        // Even if src is a regular folder, if dst is a Personal workspace,
        // copy contents directly without creating a subfolder
        else if (dstFolder.FolderType == "Personal")
        {
            destinationWorkspace = dstFolder;
            WriteWarning($"Destination is a personal workspace. Contents of \"{srcFolder.GetPSPath()}\" will be copied directly into \"{dstFolder.GetPSPath()}\" without creating a subfolder.");
        }

        string target = $"Item: '{srcFolder.GetPSPath()}' Destination: '{dstFolder.GetPSPath()}'";

        // Use the reason-returning overload so that under -WhatIf we can still descend
        // into subfolders below (each emits its own "Copy Folder" line) while a declined
        // -Confirm (reason != WhatIf) stops here. The two strings reproduce the output of
        // ShouldProcess(target, "Copy Folder") verbatim — the -WhatIf line is identical.
        bool proceed = ShouldProcess(
            $"Performing the operation \"Copy Folder\" on target \"{target}\".",
            $"Are you sure you want to perform this action?\nPerforming the operation \"Copy Folder\" on target \"{target}\".",
            "Confirm",
            out ShouldProcessReason shouldProcessReason);

        if (proceed)
        {
            // totalNum: folder itself, users, machines, packages, processes, assets, 
            // queues, triggers, API triggers, buckets, testsets, testschedules, testdataqueues
            int totalStageNum = 13;
            if (srcFolder.FolderType == "Personal") totalStageNum = 9;
            // Can Apps be copied?

            try
            {
                // When srcFolder is not directly under root and dstFolder is not root,
                // copy without the feed
                string feedType;
                if (srcFolder.ParentId is not null && dstFolder != dstDrive.RootFolder)
                {
                    feedType = "Processes";
                }
                else
                {
                    feedType = srcFolder.FeedType;
                }

                Folder newFolder;
                using ProgressReporter reporter = new(this, 1, totalStageNum, "Copying folder");
                // The scope starting below was introduced so that child reporters are disposed of in a timely manner.
                {
                    // #0 Copy the folder itself (no folder creation needed for personal workspaces)
                    if (destinationWorkspace is not null)
                    {
                        newFolder = destinationWorkspace;
                        reporter.WriteProgress(0, $"\"{srcFolder.GetPSPath()}\" to \"{newFolder.GetPSPath()}\"");
                    }
                    else
                    {
                        reporter.WriteProgress(0, $"\"{srcFolder.GetPSPath()}\" to \"{dstFolder.GetPSPath()}\"");
                        reporter.WriteProgress(0);
                        newFolder = CopyFolder(srcDrive, srcFolder, dstDrive, dstFolder, feedType!, cancelToken);
                        if (newFolder is null) return false;
                    }

                    srcDrive.Releases.ClearCache(srcFolder);
                    dstDrive.Releases.ClearCache(dstFolder);
                    dstDrive.FolderMachinesAssigned.ClearCache(dstFolder);

                    if (!ExcludeEntities)
                    {
                        int rootIndex = 0;

                        // Each stage bumps the parent progress bar, optionally clears a
                        // src-side cache, then runs its per-entity copy under a child
                        // ProgressReporter. Ordering is significant — buckets before
                        // processes, packages before triggers — so keep this list in
                        // dependency order. Base numbers (100..1300) match the original
                        // per-stage reporter offsets. (Test cases are intentionally absent:
                        // they are created automatically when packages/processes copy.)
                        var stages = new (string Label, int Base, Action? PreStep, Action<ProgressReporter> Run)[]
                        {
                            ("Copying folder users...      ", 100,
                                () => { srcDrive.FolderUsersWithInherited.ClearCache(srcFolder); srcDrive.FolderUsersWithNoInherited.ClearCache(srcFolder); },
                                r => CopyFolderUsers(this, srcDrive, srcFolder, null, null, dstDrive, newFolder, r, true, cancelToken, userMapping)),
                            ("Copying folder machines...   ", 200,
                                () => srcDrive.FolderMachinesAssigned.ClearCache(srcFolder),
                                r => CopyFolderMachines(this, srcDrive, srcFolder, null, dstDrive, newFolder, r, true, cancelToken)),
                            ("Copying buckets...           ", 300, null,
                                r => CopyBuckets(this, srcDrive, srcFolder, null, dstDrive, newFolder, r, true, cancelToken)),
                            ("Copying packages...          ", 400, null,
                                r => CopyPackages(this, srcDrive, srcFolder, dstDrive, newFolder, r, cancelToken)),
                            ("Copying processes...         ", 500, null,
                                r => CopyProcesses(this, srcDrive, srcFolder, null, dstDrive, newFolder, r, true, cancelToken)),
                            ("Copying assets...            ", 600,
                                () => srcDrive.Assets.ClearCache(srcFolder),
                                r => CopyAssets(this, srcDrive, srcFolder, null, dstDrive, newFolder, r, true, cancelToken, userMapping)),
                            ("Copying queues...            ", 700, null,
                                r => CopyQueues(this, srcDrive, srcFolder, null, dstDrive, newFolder, r, true, cancelToken)),
                            ("Copying triggers...          ", 800,
                                () => srcDrive.Triggers.ClearCache(srcFolder),
                                r => CopyTriggers(this, srcDrive, srcFolder, null, dstDrive, newFolder, r, true, cancelToken)),
                            ("Copying API triggers...      ", 900,
                                () => srcDrive.ApiTriggers.ClearCache(srcFolder),
                                r => CopyApiTriggers(this, srcDrive, srcFolder, null, dstDrive, newFolder, r, true, cancelToken)),
                            ("Copying test sets...         ", 1000, null,
                                r => CopyTestSets(this, srcDrive, srcFolder, null, dstDrive, newFolder, r, true, cancelToken)),
                            ("Copying test schedules...    ", 1100, null,
                                r => CopyTestSetSchedules(this, srcDrive, srcFolder, null, dstDrive, newFolder, r, true, cancelToken)),
                            ("Copying test data queues...  ", 1200, null,
                                r => CopyTestDataQueues(this, srcDrive, srcFolder, null, dstDrive, newFolder, r, true, cancelToken)),
                            ("Copying action catalogs...   ", 1300, null,
                                r => CopyActionCatalogs(this, srcDrive, srcFolder, null, dstDrive, newFolder, r, true, cancelToken)),
                        };

                        // Child reporters are kept alive for the whole sequence and disposed
                        // together at the end (reverse order, like the original stacked
                        // `using var` declarations) so the progress display timing is unchanged.
                        var childReporters = new List<ProgressReporter>(stages.Length);
                        try
                        {
                            foreach (var stage in stages)
                            {
                                reporter.WriteProgress(++rootIndex);
                                stage.PreStep?.Invoke();
                                var childReporter = new ProgressReporter(this, stage.Base, Int32.MaxValue, stage.Label);
                                childReporters.Add(childReporter);
                                stage.Run(childReporter);
                                cancelToken.ThrowIfCancellationRequested();
                            }
                        }
                        finally
                        {
                            for (int i = childReporters.Count - 1; i >= 0; i--)
                            {
                                childReporters[i].Dispose();
                            }
                        }
                    }
                }

                if (recurse)
                {
                    var subfolders = GetDirectChildFolders(srcDrive.GetFolders(), srcFolder);
                    if (newFolder.FolderType == "Personal" && subfolders.Count > 0)
                    {
                        WriteWarning($"Subfolders of \"{srcFolder.GetPSPath()}\" cannot be copied into a personal workspace. Skipping {subfolders.Count} subfolder(s).");
                    }
                    else
                    {
                        foreach (var subfolder in subfolders)
                        {
                            CopyItemRecurse(srcDrive, subfolder, dstDrive, newFolder, true, cancelToken, userMapping);
                            cancelToken.ThrowIfCancellationRequested();
                        }
                    }
                }

                // If the current user is not assigned to the source folder,
                // unassign them from the destination folder
                // But if we unassign, links cannot be copied..
                // UnassignMyselfAtNewFolder(srcDrive, srcFolder, dstDrive, newFolder);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(target, ex), "CopyFolderError", ErrorCategory.InvalidOperation, srcFolder));
            }

            return true;
        }

        // -WhatIf only: the destination folder is not actually created, so we recurse
        // with a "would-be" folder whose PSPath is "<dstFolder>\<srcName>" — that's where
        // this folder's copy would land — so every subfolder still emits its own
        // "Copy Folder" -WhatIf line. A declined -Confirm (reason != WhatIf) stops here.
        if (shouldProcessReason == ShouldProcessReason.WhatIf && recurse)
        {
            Folder wouldBeNewFolder = destinationWorkspace ?? new Folder
            {
                FullName = System.IO.Path.Combine(dstFolder.GetPSPath(), srcFolder.DisplayName ?? ""),
                DisplayName = srcFolder.DisplayName,
                FolderType = srcFolder.FolderType,
                ParentId = dstFolder.Id,
            };
            if (wouldBeNewFolder.FolderType != "Personal")
            {
                foreach (var subfolder in GetDirectChildFolders(srcDrive.GetFolders(), srcFolder))
                {
                    CopyItemRecurse(srcDrive, subfolder, dstDrive, wouldBeNewFolder, true, cancelToken, userMapping);
                    cancelToken.ThrowIfCancellationRequested();
                }
            }
        }
        return false;
    }

    protected override object CopyItemDynamicParameters(string path, string destination, bool recurse)
    {
        return new CopyItem_DynamicParameters();
    }

    private bool ShouldCopyTenantEntities<T>(string kind, OrchDriveInfo srcDrive, IEnumerable<T>? srcEntities, OrchDriveInfo dstDrive)
    {
        // Include the source count so the -WhatIf / -Confirm line shows how many of each
        // kind would be copied (e.g. "Item: 'Orch1:\* (5)'") without enumerating every
        // name — magnitude at a glance while keeping the per-type overview to one line.
        int count = srcEntities?.Count() ?? 0;
        if (count > 0)
        {
            return ShouldProcess($"Item: '{srcDrive.NameColonSeparator}* ({count})' Destination: '{dstDrive.NameColonSeparator}'", $"Copy {kind}");
        }
        return false;
    }

    protected override void CopyItem(string path, string copyPath, bool recurse)
    {
        var dynamicParameters = DynamicParameters as CopyItem_DynamicParameters;
        // Assign unconditionally (don't only set it to true) so the flag resets every
        // call. The field carries -ExcludeEntities down through CopyItemRecurse; relying
        // on a fresh provider instance per Copy-Item to clear it would silently leak the
        // flag into a later un-flagged copy if instances were ever pooled/reused.
        ExcludeEntities = dynamicParameters?.ExcludeEntities.IsPresent ?? false;

        OrchDriveInfo srcDrive = ExtractOrchDriveInfo(path);
        OrchDriveInfo dstDrive = ExtractOrchDriveInfo(copyPath);

        if (srcDrive is null || dstDrive is null)
        {
            return;
        }

        var userMapping = SessionState?.LoadUserMappingCsv(this, srcDrive, dstDrive, dynamicParameters?.UserMappingCsv);

        // This parent reporter should avoid flickering, so place it in a wide scope.
        using var cancelHandler = new ConsoleCancelHandler();

        srcDrive.OrchAPISession.EnsureAuthenticated();
        dstDrive.OrchAPISession.EnsureAuthenticated();

        // cache the folders
        Parallel.ForEach(Enumerable.Range(0, 2), index =>
        {
            switch (index)
            {
                case 0: srcDrive.GetFolders(); break;
                case 1: dstDrive.GetFolders(); break;
            }
        });

        var srcFolder = srcDrive.GetFolder(OrchDriveInfo.PSPathToOrchPath(path));
        if (srcFolder is null)
        {
            WriteError(new ErrorRecord(new OrchException(copyPath, $"{srcDrive.NameColon} does not have folder '{path}'."), "CopyFolderError", ErrorCategory.InvalidOperation, copyPath));
            return;
        }

        var dstFolder = dstDrive.GetFolder(OrchDriveInfo.PSPathToOrchPath(copyPath));
        if (dstFolder is null) // The destination specified was a non-existent folder name
        {
            WriteError(new ErrorRecord(new OrchException(path, $"{dstDrive.NameColon} does not have folder '{copyPath}'."), "CopyFolderError", ErrorCategory.InvalidOperation, path));
            return;
        }

        // First, when copying from root to root, copy all tenant entities.
        if (!ExcludeEntities && srcFolder == srcDrive.RootFolder && dstFolder == dstDrive.RootFolder)
        {
            if (ShouldCopyTenantEntities("Library", srcDrive, srcDrive.LibrariesInTenant.Get(), dstDrive))
            {
                CopyLibraryCmdlet.CopyLibraries(this, [srcDrive], null, null, [dstDrive], true, cancelHandler.Token);
            }

            if (ShouldCopyTenantEntities("Package", srcDrive, srcDrive.GetPackages(srcDrive.RootFolder), dstDrive))
            {
                CopyPackageCmdlet.CopyPackages(this, [(srcDrive, srcDrive.RootFolder)], srcDrive.RootFolder, null, null, [(dstDrive, dstDrive.RootFolder)], true, cancelHandler.Token);
            }

            if (ShouldCopyTenantEntities("CredentialStore", srcDrive, srcDrive.CredentialStores.Get(), dstDrive))
            {
                CopyCredentialStoreCmdlet.CopyCredentialStores(this, srcDrive, null, [dstDrive], true, cancelHandler.Token);
            }

            if (ShouldCopyTenantEntities("Role", srcDrive, srcDrive.Roles.Get(), dstDrive))
            {
                CopyRoleCmdlet.CopyRoles(this, srcDrive, null, [dstDrive], true, cancelHandler.Token);
            }

            if (ShouldCopyTenantEntities("User", srcDrive, srcDrive.Users.Get(), dstDrive))
            {
                CopyUserCmdlet.CopyUsers(this, srcDrive, null, null, null, [dstDrive], true, cancelHandler.Token, userMapping);
            }

            if (ShouldCopyTenantEntities("Machine", srcDrive, srcDrive.Machines.Get(), dstDrive))
            {
                CopyMachineCmdlet.CopyMachines(this, srcDrive, null, [dstDrive], true, cancelHandler.Token);
            }

            if (ShouldCopyTenantEntities("Calendar", srcDrive, srcDrive.Calendars.Get(), dstDrive))
            {
                CopyCalendarCmdlet.CopyCalendars(this, srcDrive, null, [dstDrive], true, cancelHandler.Token);
            }

            if (ShouldCopyTenantEntities("Webhook", srcDrive, srcDrive.Webhooks.Get(), dstDrive))
            {
                CopyWebhookCmdlet.CopyWebhooks(this, srcDrive, null, [dstDrive], true, cancelHandler.Token);
            }
        }

        // We don't want to call ShouldProcess("/") on the root folder,
        // so handle recursive copy of the root folder as a special case
        if (srcFolder == srcDrive.RootFolder)
        {
            bool isDirty = false;
            if (recurse)
            {
                // Enumerate all personal workspaces and root-level folders.
                // Personal workspace folders sometimes have a ParentId for some reason, but GetFolders() masks this.
                var foldersToBeCopied = srcDrive.GetFolders().Where((f => f.ParentId is null && f != srcDrive.RootFolder));
                foreach (var folderToBeCopied in foldersToBeCopied)
                {
                    // Accumulate: if ANY top-level folder was actually copied the dst
                    // folder cache must be invalidated below. Plain '=' would keep only
                    // the last folder's result, skipping the reset when the final folder
                    // returns false (e.g. a personal workspace) despite earlier copies.
                    isDirty |= CopyItemRecurse(srcDrive, folderToBeCopied, dstDrive, dstFolder ?? dstDrive.RootFolder!, true, cancelHandler.Token, userMapping);
                }
            }
            else if (!ExcludeEntities)
            {
                // A root-to-root copy without -Recurse copies the tenant-level entities
                // above but no folders. Warn (in both real and -WhatIf runs) so the
                // missing folders aren't mistaken for an empty tenant — -Recurse would
                // also copy every folder and its entities. Personal workspaces are
                // excluded from the count since they are never copied by -Recurse anyway.
                int skipped = srcDrive.GetFolders().Count(f => f != srcDrive.RootFolder && f.FolderType != "Personal");
                if (skipped > 0)
                {
                    WriteWarning($"Copying tenant-level entities only. {skipped} folder(s) and their entities are not copied without -Recurse.");
                }
            }
            if (isDirty)
            {
                dstDrive._dicFolders = null;
                dstDrive._dicFoldersForEnumFolders = null;
            }
            return;
        }

        bool bDirty = false;
        try
        {
            bDirty = CopyItemRecurse(srcDrive, srcFolder, dstDrive, dstFolder ?? dstDrive.RootFolder!, recurse, cancelHandler.Token, userMapping);
        }
        catch (Exception)
        {
            // If an exception leaked, we don't know whether the folder was created or not..
            // So clear the folder cache.
            dstDrive._dicFolders = null;
            dstDrive._dicFoldersForEnumFolders = null;
            throw;
        }
        finally
        {
            if (bDirty)
            {
                dstDrive._dicFolders = null;
                dstDrive._dicFoldersForEnumFolders = null;
            }
        }
    }
}
