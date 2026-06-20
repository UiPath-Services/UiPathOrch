using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using UiPath.PowerShell.Positional;
using System.Management.Automation;
using System.Management.Automation.Provider;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Core;

// Per-entity copy logic for Copy-Item / Copy-Orch*: users, machines, packages,
// processes, assets, queues, triggers, buckets, test entities, action catalogs.
public partial class OrchProvider
{
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

        // One budget shared across every asset in this folder so that copying many assets that
        // all reference the same unassigned users / machines collapses into a few warnings plus a
        // single bulk-remediation hint instead of flooding the warning stream.
        var dropBudget = new DropWarningBudget(_this, srcFolder.GetPSPath(), newFolder.GetPSPath(), DropWarningThreshold);

        int index = 0;
        foreach (var asset in srcAssets.OrderBy(a => a.Name))
        {
            cancelToken.ThrowIfCancellationRequested();

            // shouldProcess == true means an ancestor (e.g. a folder copy) already confirmed.
            // Otherwise ask: a declined -Confirm just skips, while -WhatIf still previews which
            // per-user values would be dropped (read-only) before returning without copying.
            ShouldProcessReason spReason = ShouldProcessReason.None;
            if (!shouldProcess &&
                !_this.ShouldProcess($"Item: '{asset.GetPSPath()}' Destination: '{newFolder.GetPSPath()}'", "Copy Asset", out spReason))
            {
                if (spReason == ShouldProcessReason.WhatIf)
                {
                    PreviewDroppedUserValues(_this, srcDrive, srcFolder, dstDrive, newFolder, asset, userMapping, dropBudget);
                }
                continue;
            }

            //reporter.WriteProgress(++index, asset.Name);
            reporter.WriteProgress(++index);
            CopyOneAsset(_this, srcDrive, srcFolder, dstDrive, newFolder, asset, userMapping, dropBudget);
        }
    }

    // Maximum number of per-value "owner not assigned, value dropped" warnings emitted in full
    // per CopyAssets call before they collapse into a single bulk-remediation hint. See DropWarningBudget.
    private const int DropWarningThreshold = 5;

    // Routes a drop warning through the budget when one is supplied (asset copy), otherwise writes
    // it directly (other FindDstMachine callers, e.g. session / robot migration, pass no budget).
    private static void WarnDrop(IWritableHost host, DropWarningBudget? budget, string message)
    {
        if (budget is not null)
        {
            budget.Warn(message);
        }
        else
        {
            host.WriteWarning(message);
        }
    }

    // PowerShell single-quoted literal for a value that may itself contain quotes, so the
    // "e.g.: ..." command examples in the drop warnings stay copy-paste safe.
    private static string PsLiteral(string? value) => "'" + (value ?? string.Empty).Replace("'", "''") + "'";

    // Resolves a per-user asset value's destination user and machine, emitting the same
    // "not assigned in '<dst>'" warnings FindDstUser / FindDstMachine produce (throttled via
    // budget). Returns false when the user or machine can't be mapped to the destination folder
    // (the value would be dropped). Resolved ids come back via out params; the input AssetUserValue
    // is never mutated, so this is safe to call against a cached source asset during a -WhatIf preview.
    private static bool TryMapUserValueOwner(IWritableHost _this,
        OrchDriveInfo srcDrive, Folder srcFolder, OrchDriveInfo dstDrive, Folder newFolder,
        AssetUserValue userValue, string msg, Dictionary<string, string>? userMapping,
        DropWarningBudget? budget, out long? dstUserId, out long? dstMachineId)
    {
        dstUserId = FindDstUser(_this, srcDrive, srcFolder, dstDrive, newFolder, userValue.UserId, msg, budget, userMapping)?.Id;
        dstMachineId = userValue.MachineId;
        if (dstUserId is null || dstUserId == 0)
        {
            return false;
        }

        if (userValue.MachineId is not null && userValue.MachineId != 0)
        {
            // FindDstMachine emits the WriteWarning when the machine isn't assigned to the
            // destination folder; returning false here drops just this value (no extra warning).
            dstMachineId = FindDstMachine(_this, srcDrive, srcFolder, dstDrive, newFolder, userValue.MachineId, msg, budget)?.Id;
            if (dstMachineId is null || dstMachineId == 0)
            {
                return false;
            }
        }
        return true;
    }

    // -WhatIf preview: surface which per-user values would be dropped (their user / machine
    // isn't assigned to the destination folder) without copying anything.
    private static void PreviewDroppedUserValues(IWritableHost _this,
        OrchDriveInfo srcDrive, Folder srcFolder, OrchDriveInfo dstDrive, Folder newFolder,
        Asset asset, Dictionary<string, string>? userMapping, DropWarningBudget? budget)
    {
        if (asset.UserValues is null)
        {
            return;
        }
        string msg = $"Copying asset {asset.GetPSPath()}";
        foreach (var userValue in asset.UserValues)
        {
            TryMapUserValueOwner(_this, srcDrive, srcFolder, dstDrive, newFolder, userValue, msg, userMapping, budget, out _, out _);
        }
    }

    // Copies one asset to the destination folder: links to an existing same-named asset if one
    // is present, otherwise POSTs a sanitized copy. Credential / Secret values get placeholders;
    // per-user values are re-homed to the destination's users / machines or dropped with a warning.
    private static void CopyOneAsset(IWritableHost _this,
        OrchDriveInfo srcDrive, Folder srcFolder, OrchDriveInfo dstDrive, Folder newFolder,
        Asset asset, Dictionary<string, string>? userMapping, DropWarningBudget? budget)
    {
        string msg = $"Copying asset {asset.GetPSPath()}";

        // Get links, and if an entity with the same name exists in the linked folder of the target drive,
        // just create a link to it instead
        if (LinkAsset(_this, srcDrive, srcFolder, dstDrive, newFolder, asset, msg))
        {
            return;
        }

        string target = newFolder.GetPSPath();

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
                    if (!TryMapUserValueOwner(_this, srcDrive, srcFolder, dstDrive, newFolder, userValue, msg, userMapping, budget, out long? dstUserId, out long? dstMachineId))
                    {
                        continue;
                    }
                    userValue.UserId = dstUserId;
                    userValue.MachineId = dstMachineId;

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
                        return;
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
                    RetentionAction = srcQueue.RetentionAction,
                    RetentionPeriod = srcQueue.RetentionPeriod,
                    RetentionBucketId = FindDstBucket(_this,
                            srcDrive, srcFolder, srcQueue.RetentionBucketId,
                            dstDrive, newFolder, "Copy Process", msg)?.Id,
                    StaleRetentionAction = srcQueue.StaleRetentionAction,
                    StaleRetentionPeriod = srcQueue.StaleRetentionPeriod,
                    StaleRetentionBucketId = FindDstBucket(_this,
                            srcDrive, srcFolder, srcQueue.StaleRetentionBucketId,
                            dstDrive, newFolder, "Copy Process", msg)?.Id,
                    Tags = srcQueue.Tags
                };

                // Retention defaults (Delete/30 final, Delete/180 stale, None->Delete at
                // >= v19) are applied centrally in OrchAPISession.CreateQueue.

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

                var detailedSrcTrigger = srcDrive.TriggersDetailed.Get(srcFolder, srcTrigger.Id!.Value);

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

    // Throttles the per-value "owner not assigned, value dropped" warnings emitted while copying an
    // asset's per-user values. The first DropWarningThreshold drops warn in full (each naming the
    // user / machine and the per-value Copy-OrchFolderUser / Copy-OrchFolderMachine fix); once
    // exceeded, a single summary points at the bulk Copy-OrchFolderUser * / Copy-OrchFolderMachine *
    // fix and the rest are suppressed, so copying an asset — or a folder of assets — with many
    // unmapped owners doesn't flood the warning stream. One instance is shared per CopyAssets call.
    internal sealed class DropWarningBudget
    {
        private readonly IWritableHost _host;
        private readonly string _srcPath;
        private readonly string _dstPath;
        private readonly int _threshold;
        private int _count;
        private bool _summarized;

        public DropWarningBudget(IWritableHost host, string srcPath, string dstPath, int threshold)
        {
            _host = host;
            _srcPath = srcPath;
            _dstPath = dstPath;
            _threshold = threshold;
        }

        public void Warn(string detailedMessage)
        {
            _count++;
            if (_count <= _threshold)
            {
                _host.WriteWarning(detailedMessage);
                return;
            }
            if (!_summarized)
            {
                _summarized = true;
                _host.WriteWarning($"More than {_threshold} per-user values were dropped because their user / machine is not assigned in '{_dstPath}'; further per-value warnings are suppressed. To assign all of the source folder's users / machines at once, e.g.: Copy-OrchFolderUser -Path {PsLiteral(_srcPath)} * -Destination {PsLiteral(_dstPath)}  (and Copy-OrchFolderMachine * for machines).");
            }
        }
    }
}
