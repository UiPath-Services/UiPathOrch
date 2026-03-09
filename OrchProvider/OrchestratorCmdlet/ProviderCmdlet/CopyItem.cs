using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Provider;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Core;

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
    internal static Folder? GetRelativeDstFolder(this IWritableHost _this, Folder srcRootFolder, Folder srcFolder, OrchDriveInfo dstDrive, Folder dstRootFolder, bool includeRoot = false)
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
        if (dstFolder is null)
        {
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

        return dstFolder;
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
            newFolder.Path = dstFolder.GetPSPath();
            WriteItemObject(newFolder, newFolder.GetPSPath(), true);
            dstDrive._dicFolders!.Add(newFolder); // Add for now without worrying about sort order
        }
        return newFolder;
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

        List<Int64> retRoles = [];
        foreach (var ur in srcRoleIds.Where(r => r.Origin != "Inherited"))
        {
            Role roleToAdded;
            if (srcDrive == dstDrive)
            {
                // Searching by name should work, but searching by Id is probably safer..
                roleToAdded = dstTenantRoles.FirstOrDefault(r => r.Id == ur.Id);
            }
            else
            {
                roleToAdded = dstTenantRoles.FirstOrDefault(r => string.Compare(r.Name, ur.Name, StringComparison.OrdinalIgnoreCase) == 0);
            }

            // When copying folders between different tenants, a role with the same name may not exist,
            // so display an error and continue processing
            if (roleToAdded is null)
            {
                _this.WriteError(new ErrorRecord(
                    new OrchException(dstDrive.NameColonSeparator,
                    $"{msg}: {dstDrive.NameColon} does not have role with Name ='{ur.Name}'."), "CopyFolderError", ErrorCategory.InvalidOperation, dstDrive));
                continue;
            }

            // Folder users retrieved from classic folders include tenant roles,
            // so those must be excluded
            if (roleToAdded.Type != "Tenant")
            {
                retRoles.Add(roleToAdded.Id ?? 0);
            }
        }
        return retRoles;
    }

    internal static void CopyFolderUsers(IWritableHost _this,
        OrchDriveInfo srcDrive, Folder srcFolder,  List<WildcardPattern>? wpUserName, List<WildcardPattern>? wpType,
        OrchDriveInfo dstDrive, Folder newFolder, ProgressReporter reporter,
        bool shouldProcess, CancellationToken cancelToken,
        Dictionary<string, string>? userMapping = null)
    {
        if (newFolder.FolderType == "Personal") return;

        var srcFolderUsers = srcDrive.FolderUsersWithNoInherited.Get(srcFolder)
            .FilterByWildcards(u => u?.UserEntity?.UserName, wpUserName).ToList();
        if (srcFolderUsers.Count == 0)
        {
            return;
        }

        // Get already-assigned users
        var dstFolderUsers = dstDrive.FolderUsersWithNoInherited.Get(newFolder)
            .FilterByWildcards(u => u?.UserEntity?.UserName, wpUserName)
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
                    var resolvedUsers = dstDrive.SearchDirectory(resolvedUserName)?
                        .Where(u => u.type == type).ToList();

                    DirectoryObject resolved = null;

                    if (resolvedUsers?.Count == 1)
                    {
                        resolved = resolvedUsers.First();
                    }

                    else if (resolvedUsers?.Count > 1)
                    {
                        _this.WriteError(new ErrorRecord(new OrchException(dstDrive.NameColonSeparator, $"Duplicated {type} found for '{userName}'."), "SearchForUsersAndGroupsError", ErrorCategory.InvalidOperation, dstDrive));
                    }

                    else if (resolvedUsers is null || resolvedUsers.Count == 0)
                    {
                        // A user with the same name as the srcDrive user was not found in the dstDrive directory!
                        // So also search dstDrive using this user's email address.

                        // First, check the srcUser's email.
                        var srcUserEmail = srcDrive.GetUsers().FirstOrDefault(u => u.Id == userRole.UserEntity?.Id)?.EmailAddress;

                        // TODO: If not found among local users, need to search the directory.
                        //if (string.IsNullOrEmpty(srcUserEmail))
                        //{
                        //    // If not found among tenant users, search the directory.
                        //    var srcDirectoryUser = srcDrive.SearchPmDirectory(userName)?
                        //        .Where(u => u.type == type)
                        //        .Where(u => string.Compare(u.identityName, userName, true) == 0)
                        //        .FirstOrDefault();
                        //    srcUserEmail = srcDirectoryUser?.email;
                        //}

                        if (!string.IsNullOrEmpty(srcUserEmail) && srcUserEmail!= userName)
                        {
                            resolved = dstDrive.SearchDirectory(srcUserEmail)?
                                .Where(u => u.type == type)
                                .Where(u => string.Compare(u.identityName, srcUserEmail, true) == 0)
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
                    dstDrive.OrchAPISession.AssignDirectoryUser(postingUser);

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
            var currentUser = drive.GetCurrentUser();
            if (currentUser is null) return false;
            DomainUserAssignment duser = new()
            {
                Domain = string.IsNullOrEmpty(currentUser.Domain) ? "autogen" : currentUser.Domain,
                UserName = currentUser.UserName,
                DirectoryIdentifier = currentUser.Key,
                UserType = currentUser.Type,
                RolesPerFolder = [new FolderRoles() {
                    FolderId = folder.Id ?? 0,
                    RoleIds = [folderAdministratorRole.Id ?? 0]
                }]
            };
            try
            {
                drive.OrchAPISession.AssignDirectoryUser(duser);
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
            var srcMachine = srcMachines.FirstOrDefault(m => string.Compare(m.Name, dstMachine.Name, true) == 0);
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
            dstDrive._dicReleases?.TryRemove(newFolder.Id ?? 0, out var _);
            dstDrive._dicReleasesDetailed?.TryRemove(newFolder.Id ?? 0, out var _);
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
            _this.WriteError(new ErrorRecord(
                new OrchException(srcDrive.NameColonSeparator,
                $"{msg}: {srcDrive.NameColonSeparator} does not have the bucket with Id = {srcBucketId}"), action, ErrorCategory.InvalidOperation, srcDrive));
            return null;
        }

        var dstBuckets = dstDrive.Buckets.Get(newFolder);
        var dstBucket = dstBuckets.FirstOrDefault(b => b.Name == srcBucket.Name);
        if (dstBucket is null)
        {
            _this.WriteError(new ErrorRecord(
                new OrchException(newFolder.GetPSPath(),
                $"{msg}: {newFolder.GetPSPath()} does not have the bucket with Name = '{srcBucket.Name}'."), action, ErrorCategory.InvalidOperation, newFolder));
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
            processes = srcDrive.GetReleases(srcFolder)
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
                    srcRelease = srcDrive.GetReleaseById(srcFolder, process.Id ?? 0);
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
                        var srcEntryPoints = srcDrive.GetPackageEntryPoints(srcFeedId, srcRelease.ProcessKey!, srcRelease.ProcessVersion!).ToList();
                        var dstEntryPoints = dstDrive.GetPackageEntryPoints(dstFeedId, srcRelease.ProcessKey!, srcRelease.ProcessVersion!).ToList();

                        var srcEntryPoint = srcEntryPoints.FirstOrDefault(e => e.Id == srcRelease.EntryPointId);
                        var dstEntryPoint = dstEntryPoints.FirstOrDefault(e => e.Path == srcEntryPoint!.Path);

                        srcRelease.EntryPointId = dstEntryPoint?.Id;
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
                        var dstReleases = dstDrive.GetReleases(newFolder);
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
                    postingRelease.AutoUpdate = null;
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
            dstDrive._dicReleases?.TryRemove(newFolder.Id ?? 0, out _);
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
                _this.WriteError(new ErrorRecord(new OrchException(srcDrive.NameColonSeparator, $"{msg}: {srcDrive.NameColon} does not have PmGroup with id = {srcPmGroupId}. Ignoring this id."), "GetGroupIdError", ErrorCategory.InvalidOperation, srcDrive));
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
                string target = $"{srcDrive.NameColonSeparator}";
                _this.WriteError(new ErrorRecord(new OrchException(target, $"{msg}: {srcDrive.NameColon} does not have credential store with Id = {srcCredentialStoreId}."), "GetCredentialStoreError", ErrorCategory.InvalidOperation, target));
                return null;
            }

            var dstCredentialStore = dstDrive.CredentialStores.Get().FirstOrDefault(cs => string.Compare(cs.Name, srcCredentialStore.Name, StringComparison.OrdinalIgnoreCase) == 0);
            if (dstCredentialStore is null)
            {
                string target = dstDrive.NameColonSeparator;
                _this.WriteError(new ErrorRecord(new OrchException(target, $"{msg}: {dstDrive.NameColon} does not have credential store with Name = {srcCredentialStore.Name}."), "GetCredentialStoreError", ErrorCategory.InvalidOperation, target));
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

    // TODO: Is this implementation incomplete? Need to search the directory for users.
    // The current implementation only searches local users.
    internal static Entities.User? FindDstUser(IWritableHost _this,
        OrchDriveInfo srcDrive,
        OrchDriveInfo dstDrive, Folder newFolder, Int64? srcUserId, string msg,
        Dictionary<string, string>? userMapping = null)
    {
        if (srcUserId is null || srcUserId == 0) return null;
        //string msg = $"Migrating the user id {Path.Combine(srcDrive.NameColon, srcUserId?.ToString() ?? "")}";
        try
        {
            var srcUser = srcDrive.GetUsers().FirstOrDefault(u => u.Id == srcUserId);
            if (srcUser is null)
            {
                string target = srcDrive.NameColonSeparator;
                _this.WriteError(new ErrorRecord(new OrchException(target, $"{msg}: {srcDrive.NameColon} does not have user with Id = {srcUserId}."), "FindUserError", ErrorCategory.InvalidOperation, target));
                return null;
            }

            // Name resolution via UserMappingCsv
            string searchName = srcUser.UserName!;
            if (userMapping is not null && userMapping.TryGetValue(srcUser.UserName!, out var mappedName)
                && !string.IsNullOrEmpty(mappedName))
            {
                searchName = mappedName;
            }

            var dstUsers = dstDrive.GetUsers();
            var dstUser = dstUsers.FirstOrDefault(u => string.Compare(u.UserName, searchName, StringComparison.OrdinalIgnoreCase) == 0);
            if (dstUser is null)
            {
                // User not found! Try searching by email as well.
                dstUser = dstUsers.FirstOrDefault(u => string.Compare(u.UserName, srcUser.EmailAddress, StringComparison.OrdinalIgnoreCase) == 0);
            }

            if (dstUser is null)
            {
                // If AssignDirectoryUser was already executed in CopyFolderUsers,
                // the tenant user cache may be stale, so clear it and retry
                dstDrive._dicUsers = null;
                dstUsers = dstDrive.GetUsers();
                dstUser = dstUsers.FirstOrDefault(u => string.Compare(u.UserName, searchName, StringComparison.OrdinalIgnoreCase) == 0);
            }

            if (dstUser is null)
            {
                string target = newFolder.GetPSPath();
                _this.WriteError(new ErrorRecord(new OrchException(target, $"{msg}: {dstDrive.NameColon} does not have user with Name = '{searchName}'."), "FindUserError", ErrorCategory.InvalidOperation, target));
            }
            return dstUser;
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
                _this.WriteError(new ErrorRecord(new OrchException(srcDrive.NameColonSeparator, $"{msg}: {srcDrive.NameColon} does not have robot with Id = {srcRobotId}."), "MigrateRobotIdError", ErrorCategory.InvalidOperation, srcDrive));
                return null;
            }
            //msg = $"Migrating id of the robot {Path.Combine(srcDrive.NameColon, srcRobot.Name!)}";

            var dstRobots = dstDrive.RobotsFromFolder.Get(dstFolder);
            var dstRobot = dstRobots?.FirstOrDefault(r => string.Compare(r.Name, srcRobot.Name, StringComparison.OrdinalIgnoreCase) == 0);
            if (dstRobot is null)
            {
                string target = dstFolder.GetPSPath();
                _this.WriteError(new ErrorRecord(new OrchException(target, $"{msg}: {dstDrive.NameColon} does not have robot with Name = '{srcRobot.Name}' ({srcRobot.Username})."), "MigrateRobotIdError", ErrorCategory.InvalidOperation, target));
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
                    _this.WriteError(new ErrorRecord(new OrchException(srcDrive.NameColonSeparator, $"{msg}: {srcDrive.NameColon} does not have robot with Id = {srcRobotId}."), "MigrateRobotIdError", ErrorCategory.InvalidOperation, srcDrive));
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
                    _this.WriteError(new ErrorRecord(new OrchException(srcDrive.NameColonSeparator, $"{msg}: {srcDrive.NameColon} does not have robot with Id = {srcRobotId}."), "MigrateRobotIdError", ErrorCategory.InvalidOperation, srcDrive));
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
            var dstRobot = dstRobots?.FirstOrDefault(r => 
                r.Type == srcRobot_Type && // This srcRobot.Type should always be "Unattended"..
                string.Compare(r.Username, srcRobot_Username, StringComparison.OrdinalIgnoreCase) == 0);
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
                string target = srcFolder.GetPSPath();
                _this.WriteError(new ErrorRecord(new OrchException(target, $"{msg}: {srcDrive.NameColon} does not have machine with Id = {srcMachineId}."), "FindMachineError", ErrorCategory.InvalidOperation, target));
                return null;
            }
            //msg = $"Migrating id of the machine {Path.Combine(srcDrive.NameColon, srcMachine.Name!)}";
            var dstMachineFolder = dstDrive.FolderMachinesAssigned.Get(dstFolder).FirstOrDefault(m => string.Compare(m.Name, srcMachine.Name, StringComparison.OrdinalIgnoreCase) == 0);
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

    internal static MachineSessionRuntime? FindDstSession(IWritableHost _this,
        OrchDriveInfo srcDrive, Folder srcFolder,
        OrchDriveInfo dstDrive, Folder dstFolder, Int64? srcSessionId, string msg)
    {
        if (srcSessionId is null || srcSessionId.Value == 0) return null;

        //string msg = $"Finding the session id with robot id {dstRobotId} and machine id {dstMachineId}";
        MachineSessionRuntime srcSession = null;
        try
        {
            //string query = $"&$filter=((MachineType%20ne%20%27Template%27)%20or%20(MachineScope%20ne%20%27Cloud%27))%20and%20MachineId%20eq%20{dstMachineId}&runtimeType=Unattended&robotId={dstRobotId}";
            //string query = $"&robotId={dstRobot.Id.Value}&MachineId%20eq%20{dstMachineFolder.Id}";

            // TODO: Changed this to use cache. Is it working correctly?
            var srcSessions = srcDrive.MachineSessionRuntimesByFolder.Get(srcFolder).ToList();
            srcSession = srcSessions.FirstOrDefault(s => s.SessionId == srcSessionId);
            if (srcSession is null)
            {
                //_this.WriteWarning($"{srcFolder.GetPSPath()}: {msg}: The session not found with SessionId {srcSessionId}.");
                _this.WriteError(new ErrorRecord(new OrchException(srcFolder.GetPSPath(), $"{msg}: The session not found with SessionId {srcSessionId}."), "MigrateSessionIdError", ErrorCategory.InvalidOperation, srcFolder));
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
            var dstSessions = dstDrive.MachineSessionRuntimesByFolder.Get(dstFolder);
            var dstSession = dstSessions.FirstOrDefault(s =>
                string.Compare(s.MachineName ?? "", srcMachineName, true) == 0 &&
                string.Compare(s.HostMachineName ?? "", srcHostMachineName, true) == 0 &&
                string.Compare(s.ServiceUserName ?? "", srcServiceUserName, true) == 0);

            if (dstSession is null)
            {
                //_this.WriteError(new ErrorRecord(new OrchException(dstFolder.GetPSPath(),
                //    $"{msg}: The session not found with MachineName ='{srcMachineName}', HostMachineName = '{srcHostMachineName}' and ServiceUserName = '{srcServiceUserName}'."), "MigrateSessionIdError", ErrorCategory.InvalidOperation, dstFolder));
                _this.WriteWarning($"\"{dstFolder.GetPSPath()}\": {msg}: The session not found with MachineName ='{srcMachineName}', HostMachineName = '{srcHostMachineName}' and ServiceUserName = '{srcServiceUserName}'.");

                dstSession = dstSessions.FirstOrDefault(s =>
                    string.Compare(s.MachineName, srcMachineName, true) == 0 &&
                    string.Compare(s.HostMachineName, srcHostMachineName, true) == 0 &&
                    string.IsNullOrEmpty(s.ServiceUserName));

                dstSession ??= dstSessions.FirstOrDefault(s =>
                        (string.Compare(s.MachineName, srcMachineName, true) == 0 &&
                        (string.Compare(s.HostMachineName, srcHostMachineName, true) == 0)));
            }
            return dstSession;
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
            dstQueue = dstDrive.Queues.Get(dstFolder)?.FirstOrDefault(q => string.Compare(q.Name, srcQueue.Name, true) == 0);
        }
        catch (Exception ex)
        {
            string target = dstFolder.GetPSPath();
            _this.WriteError(new ErrorRecord(new OrchException(target, msg, ex), "MigrateQueueIdError", ErrorCategory.InvalidOperation, target));
            return null;
        }
        if (dstQueue is null)
        {
            string target = dstFolder.GetPSPath();
            _this.WriteError(new ErrorRecord(new OrchException(target, $"{msg}: {dstFolder.GetPSPath()} does not have queue with Name = '{srcQueue.Name}'."), "MigrateQueueIdError", ErrorCategory.InvalidOperation, target));
            return null;
        }
        return dstQueue;
    }

    //private QueueDefinition? FindDstTrigger(
    //    OrchDriveInfo srcDrive, Folder srcFolder,
    //    OrchDriveInfo dstDrive, Folder dstFolder,
    //    Int64? srcQueueId)
    //{
    //    if (srcQueueId is null || srcQueueId.Value == 0) return null;
    //    srcDrive._dicQueueDefinitions?.TryRemove(srcFolder.Id ?? 0, out var _);

    //    string msg = $"Migrating the queue id {Path.Combine(srcFolder.GetPSPath(), srcQueueId?.ToString() ?? "")}";
    //    QueueDefinition srcQueue = null;
    //    try
    //    {
    //        srcQueue = srcDrive.GetQueues(srcFolder)?.FirstOrDefault(q => q.Id == srcQueueId);
    //    }
    //    catch (Exception ex)
    //    {
    //        string target = srcFolder.GetPSPath();
    //        WriteError(new ErrorRecord(new OrchException(target, msg, ex), "MigrateQueueIdError", ErrorCategory.InvalidOperation, target));
    //        return null;
    //    }
    //    if (srcQueue is null)
    //    {
    //        string target = srcFolder.GetPSPath();
    //        WriteError(new ErrorRecord(new OrchException(target, $"{msg}: The queue not found."), "MigrateQueueIdError", ErrorCategory.InvalidOperation, target));
    //        return null;
    //    }

    //    msg = $"Migrating id of the queue {Path.Combine(srcFolder.GetPSPath(), srcQueue.Name!)}";
    //    QueueDefinition dstQueue = null;
    //    try
    //    {
    //        dstQueue = dstDrive.GetQueues(dstFolder)?.FirstOrDefault(q => string.Compare(q.Name, srcQueue.Name, StringComparison.OrdinalIgnoreCase) == 0);
    //    }
    //    catch (Exception ex)
    //    {
    //        string target = dstFolder.GetPSPath();
    //        WriteError(new ErrorRecord(new OrchException(target, msg, ex), "MigrateQueueIdError", ErrorCategory.InvalidOperation, target));
    //        return null;
    //    }
    //    if (dstQueue is null)
    //    {
    //        string target = dstFolder.GetPSPath();
    //        WriteError(new ErrorRecord(new OrchException(target, $"{msg}: The queue does not exist."), "MigrateQueueIdError", ErrorCategory.InvalidOperation, target));
    //        return null;
    //    }
    //    return dstQueue;
    //}

    internal static Release? FindDstRelease(IWritableHost _this,
        OrchDriveInfo srcDrive, Folder srcFolder,
        OrchDriveInfo dstDrive, Folder dstFolder, Int64? srcReleaseId, string msg)
    {
        if (srcReleaseId is not null && srcReleaseId == 0) return null;

        string target = srcFolder.GetPSPath();
        //string msg = $"Migrating process id {Path.Combine(srcFolder.GetPSPath(), srcReleaseId?.ToString() ?? "")}";
        Release srcRelease = null;
        try
        {
            srcRelease = srcDrive.GetReleases(srcFolder)?.FirstOrDefault(r => r.Id == srcReleaseId);
        }
        catch (Exception ex)
        {
            _this.WriteError(new ErrorRecord(new OrchException(target, msg, ex), "MigrateProcessIdError", ErrorCategory.InvalidOperation, target));
            return null;
        }
        if (srcRelease is null)
        {
            _this.WriteError(new ErrorRecord(new OrchException(target, $"The process id {srcReleaseId} not found."), "MigrateProcessIdError", ErrorCategory.InvalidOperation, target));
            return null;
        }

        //msg = $"Migrating id of process {Path.Combine(srcFolder.GetPSPath(), srcRelease.Name!)}";

        Release dstRelease = null;
        target = dstFolder.GetPSPath();
        try
        {
            dstRelease = dstDrive.GetReleases(dstFolder)?.FirstOrDefault(q => string.Compare(q.Name, srcRelease.Name, StringComparison.OrdinalIgnoreCase) == 0);
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
            _this.WriteError(new ErrorRecord(new OrchException(target, $"{msg}: {dstFolder.GetPSPath()} does not have process with Name = '{srcRelease.Name}'."), "MigrateMachineIdError", ErrorCategory.InvalidOperation, target));
            return null;
        }
        return dstRelease;
    }

    internal static ExtendedCalendar? FindDstCalendar(IWritableHost _this,
        OrchDriveInfo srcDrive, OrchDriveInfo dstDrive, Int64? srcCalendarId, string msg)
    {
        if (srcCalendarId is null || srcCalendarId == 0) return null;

        string target = srcDrive.NameColonSeparator;

        var srcCalendars = srcDrive.GetCalendars();
        var srcCalendar = srcCalendars?.FirstOrDefault(c => c.Id == srcCalendarId);
        if (srcCalendar is null)
        {
            _this.WriteError(new ErrorRecord(new OrchException(target, $"{msg}: {srcDrive.NameColonSeparator} doesn't have calendar with Id = {srcCalendarId}."), "MigrateCalendarIdError", ErrorCategory.InvalidOperation, target));
            return null;
        }

        //msg = $"Migrating id of the calendar {Path.Combine(srcDrive.NameColon, srcCalendar.Name!)}";
        ExtendedCalendar dstCalendar = null;
        try
        {
            dstCalendar = dstDrive.OrchAPISession.GetCalendars()?.FirstOrDefault(r => string.Compare(r.Name, srcCalendar.Name, StringComparison.OrdinalIgnoreCase) == 0);
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

    internal static IEnumerable<Folder>? FindDstFolders(
        List<Int64>? folderIds, IEnumerable<Folder> srcFolders, IEnumerable<Folder> dstFolders)
    {
        if (folderIds is null)
            return null;

        var selectedSrcFolders = srcFolders.Where(src => folderIds.Contains(src.Id ?? 0)).ToList();
        return dstFolders.Where(dst => selectedSrcFolders.Any(src => string.Compare(src.FullyQualifiedName, dst.FullyQualifiedName, StringComparison.OrdinalIgnoreCase) == 0));
    }

    internal static bool LinkAsset(IWritableHost _this,
        OrchDriveInfo srcDrive, Folder srcFolder, 
        OrchDriveInfo dstDrive, Folder newFolder, Asset asset, string msg)
    {
        // TODO: Is this version number correct? There likely aren't any Orchestrators older than v12 anymore.
        if (srcDrive.OrchAPISession.ApiVersion < 12) return false;
        if (dstDrive.OrchAPISession.ApiVersion < 12) return false;

        //string msg = $"Sharing asset {Path.Combine(srcFolder.GetPSPath(), asset.Name!)}";
        IEnumerable<Folder> dstLinkFolders = null;
        try
        {
            var srcLinks = srcDrive.GetFoldersForAsset(srcFolder, asset);
            var srcLinkFolderIds = srcLinks?.AccessibleFolders?
                .Select(af => af.Id ?? 0)
                .Where(id => id != srcFolder.Id)
                .ToList();
            if (srcLinkFolderIds is null || !srcLinkFolderIds.Any())
            {
                return false;
            }

            dstLinkFolders = FindDstFolders(
                srcLinkFolderIds,
                srcDrive.GetFolders(),
                dstDrive.GetFolders());

            if (dstLinkFolders is null || !dstLinkFolders.Any())
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            string target = srcFolder.GetPSPath();
            _this.WriteError(new ErrorRecord(new OrchException(target, msg, ex), "GetAssetLinkError", ErrorCategory.InvalidOperation, target));
            return false;
        }

        try
        {
            foreach (var dstLinkFolder in dstLinkFolders)
            {
                var assets = dstDrive.Assets.Get(dstLinkFolder);
                var dstAsset = assets.FirstOrDefault(a => string.Compare(a.Name, asset.Name, StringComparison.OrdinalIgnoreCase) == 0);
                if (dstAsset is null)
                {
                    continue;
                }
                //_this.WriteWarning($"{msg}: The same link found in the target drive: {dstLinkFolder.GetPSPath()}. The contents of this asset won't be copied.");
                dstDrive.OrchAPISession.ShareAssetsToFolders(dstLinkFolder.Id ?? 0,
                                new List<Int64> { dstAsset.Id ?? 0 },
                                new List<Int64> { newFolder.Id ?? 0 },
                                new List<Int64>());
                return true;
            }
        }
        catch (Exception ex)
        {
            string target = newFolder.GetPSPath();
            _this.WriteError(new ErrorRecord(new OrchException(target, msg, ex), "LinkAssetError", ErrorCategory.InvalidOperation, target));
            return false;
        }
        return false;
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
                bool bCredentialWarningDone = false;
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
                        postingAsset.CredentialPassword = "!!!PLEASE UPDATE!!!";
                        bCredentialWarningNeeded = true;
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
                                if (userValue.MachineId is null || userValue.MachineId == 0)
                                {
                                    _this.WriteError(new ErrorRecord(new OrchException(target, $"{msg}: The machine {userValue.MachineName} is not assigned to the folder."), "CopyAssetError", ErrorCategory.InvalidOperation, target));
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
                                userValue.CredentialPassword = "!!!PLEASE UPDATE!!!";
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
                                _this.WriteError(new ErrorRecord(new OrchException(target, $"{msg}: No applicable values. Skipping."), "CopyAssetError", ErrorCategory.InvalidOperation, target));
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

                    if (bCredentialWarningNeeded && !bCredentialWarningDone)
                    {
                        target = System.IO.Path.Combine(newFolder.GetPSPath(), created?.Name ?? "");
                        _this.WriteWarning($"'{target}': Please update credential asset passwords with Set-OrchCredentialAsset cmdlet.");
                        bCredentialWarningDone = true;
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
        // TODO: Is this version number correct? There likely aren't any Orchestrators older than v12 anymore.
        if (srcDrive.OrchAPISession.ApiVersion < 12) return false;
        if (dstDrive.OrchAPISession.ApiVersion < 12) return false;

        string msg = $"Sharing queue {queue.GetPSPath()}";
        IEnumerable<Folder> dstLinkFolders = null;
        try
        {
            var srcLinks = srcDrive.GetFoldersForQueue(srcFolder, queue);
            var srcLinkFolderIds = srcLinks?.AccessibleFolders?
                .Select(af => af.Id ?? 0)
                .Where(id => id != srcFolder.Id)
                .ToList();
            if (srcLinkFolderIds is null || !srcLinkFolderIds.Any())
            {
                return false;
            }

            dstLinkFolders = FindDstFolders(
                srcLinkFolderIds,
                srcDrive.GetFolders(),
                dstDrive.GetFolders());

            if (dstLinkFolders is null || !dstLinkFolders.Any())
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            string target = srcFolder.GetPSPath();
            _this.WriteError(new ErrorRecord(new OrchException(target, msg, ex), "GetQueueLinkError", ErrorCategory.InvalidOperation, target));
            return false;
        }

        try
        {
            foreach (var dstLinkFolder in dstLinkFolders)
            {
                var queues = dstDrive.Queues.Get(dstLinkFolder);
                var dstQueue = queues.FirstOrDefault(a => string.Compare(a.Name, queue.Name, StringComparison.OrdinalIgnoreCase) == 0);
                if (dstQueue is null)
                {
                    continue;
                }
                //_this.WriteWarning($"{msg}: The same link found in the target drive: {dstLinkFolder.GetPSPath()}. The contents of this queue won't be copied.");
                dstDrive.OrchAPISession.ShareQueuesToFolders(dstLinkFolder.Id ?? 0,
                                [dstQueue.Id ?? 0],
                                [newFolder.Id ?? 0],
                                []);
                return true;
            }
        }
        catch (Exception ex)
        {
            string target = newFolder.GetPSPath();
            _this.WriteError(new ErrorRecord(new OrchException(target, msg, ex), "LinkQueueError", ErrorCategory.InvalidOperation, target));
            return false;
        }
        return false;
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

                // TODO: I don't think ProcessScheduleId is being copied properly?
                // It seems like a value that needs to be migrated from somewhere.
                // Verify with Get-OrchQueue -Recurse | select name,ProcessScheduleId
                postingQueue = new QueueDefinition()
                {
                    Name = srcQueue.Name,
                    Description = srcQueue.Description,
                    MaxNumberOfRetries = srcQueue.MaxNumberOfRetries,
                    AcceptAutomaticallyRetry = srcQueue.AcceptAutomaticallyRetry,
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
                    StaleRetentionAction = srcQueue.RetentionAction ?? "Delete",
                    StaleRetentionPeriod = srcQueue.StaleRetentionPeriod ?? 180,
                    StaleRetentionBucketId = FindDstBucket(_this,
                            srcDrive, srcFolder, srcQueue.StaleRetentionBucketId,
                            dstDrive, newFolder, "Copy Process", msg)?.Id,
                    Tags = srcQueue.Tags
                };

                if (dstDrive.OrchAPISession.ApiVersion >= 19)
                {
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
                    _this.WriteWarning($"{msg}: The StopProcessDate is in the past ({postingTrigger.StopProcessDate.Value.ToLocalTime}). Remove it before copying.");
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
        if (srcDrive.OrchAPISession.ApiVersion < 12) return false;
        if (dstDrive.OrchAPISession.ApiVersion < 12) return false;

        string msg = $"Sharing bucket {bucket.GetPSPath()}";

        IEnumerable<Folder> dstLinkFolders = null;
        try
        {
            var srcLinks = srcDrive.GetFoldersForBucket(srcFolder, bucket);
            var srcLinkFolderIds = srcLinks?.AccessibleFolders?
                .Select(af => af.Id ?? 0)
                .Where(id => id != srcFolder.Id)
                .ToList();
            if (srcLinkFolderIds is null || !srcLinkFolderIds.Any())
            {
                return false;
            }

            dstLinkFolders = FindDstFolders(
                srcLinkFolderIds,
                srcDrive.GetFolders(),
                dstDrive.GetFolders());

            if (dstLinkFolders is null || !dstLinkFolders.Any())
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            string target = srcFolder.GetPSPath();
            _this.WriteError(new ErrorRecord(new OrchException(target, msg, ex), "GetBucketLinkError", ErrorCategory.InvalidOperation, target));
            return false;
        }

        try
        {
            foreach (var dstLinkFolder in dstLinkFolders)
            {
                var buckets = dstDrive.Buckets.Get(dstLinkFolder);
                var dstBucket = buckets.FirstOrDefault(a => string.Compare(a.Name, bucket.Name, StringComparison.OrdinalIgnoreCase) == 0);
                if (dstBucket is null)
                {
                    continue;
                }
                dstDrive.OrchAPISession.ShareBucketsToFolders(dstLinkFolder.Id ?? 0,
                                new List<Int64> { dstBucket.Id ?? 0 },
                                new List<Int64> { newFolder.Id ?? 0 },
                                new List<Int64>());
                return true;
            }
        }
        catch (Exception ex)
        {
            string target = newFolder.GetPSPath();
            _this.WriteError(new ErrorRecord(new OrchException(target, msg, ex), "LinkBucketError", ErrorCategory.InvalidOperation, target));
            return false;
        }
        return false;
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

            if (shouldProcess || _this.ShouldProcess(bucket.GetPSPath(), "Copy Bucket"))
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
                        _this.WriteWarning($"Please manually update the password for the storage bucket \"{System.IO.Path.Combine(newFolder.GetPSPath(), bucket.Name!)}\".");
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
            _this.WriteError(new ErrorRecord(
                new OrchException(srcDrive.NameColonSeparator,
                $"{msg}: {srcFolder.GetPSPath()} does not have test case with Id = {srcDefinitionId}."), "CopyTestCaseError", ErrorCategory.InvalidOperation, dstDrive));
            return null;
        }

        var dstTestCases = dstDrive.TestCases.Get(newFolder);
        var dstTestCase = dstTestCases.FirstOrDefault(tc => (tc.PackageIdentifier == srcTestCase.PackageIdentifier && tc.Name == srcTestCase.Name));
        if (dstTestCase is null)
        {
            _this.WriteError(new ErrorRecord(
                new OrchException(srcDrive.NameColonSeparator,
                $"{msg}: {newFolder.GetPSPath()} does not have test case with PackageIdentifier = '{srcTestCase.PackageIdentifier}' and Name = '{srcTestCase.Name}'."), "CopyTestCaseError", ErrorCategory.InvalidOperation, dstDrive));
        }
        return dstTestCase;
    }

    internal static void CopyTestSets(IWritableHost _this,
        OrchDriveInfo srcDrive, Folder srcFolder, List<WildcardPattern>? wpName,
        OrchDriveInfo dstDrive, Folder newFolder, ProgressReporter reporter,
        bool shouldProcess, CancellationToken cancelToken)
    {
        if (newFolder.FolderType == "Personal") return;

        // TODO: Is this version number correct?
        // Confirmed that test entities do not exist in v16
        //if (srcDrive.OrchAPISession.ApiVersion < 17) return;
        //if (dstDrive.OrchAPISession.ApiVersion < 17) return;

        // Considering that cmdlets may be executed consecutively in a script,
        // we shouldn't have been clearing the cache each time..
        // Comment out the below.

        // Clear this folder's cache so we can copy the latest state
        //srcDrive._dicTestSets?.TryRemove(srcFolder.Id ?? 0, out _);

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

                            // TODO: Replace placeholder with an appropriate message
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

    internal static void CopyTestDataQueueItems(IWritableHost _this,
        OrchDriveInfo srcDrive, Folder srcFolder, TestDataQueue srcTestDataQueue,
        OrchDriveInfo dstDrive, Folder newFolder, string dstTestDataQueueName, bool shouldProcess)
    {
        if (newFolder.FolderType == "Personal") return;

        // Confirmed that test entities do not exist in v17
        //if (srcDrive.OrchAPISession.ApiVersion < 18) return;
        //if (dstDrive.OrchAPISession.ApiVersion < 18) return;

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

        if (items.Count == 0) return;

        string strItems = "[" + string.Join(",", items.Select(i => i.ContentJson)) + "]";

        try
        {
            if (shouldProcess || _this.ShouldProcess($"Items: {srcTestDataQueue.GetPSPath()} Destination: {newFolder.GetPSPath()}", "Copy TestDataQueueItem"))
            {
                dstDrive.OrchAPISession.AddTestDataQueueItems(newFolder.Id ?? 0, dstTestDataQueueName, strItems);
            }
        }
        catch (Exception ex)
        {
            _this.WriteError(new ErrorRecord(new OrchException(newFolder.GetPSPath(), $"CopyTestDataQueueItem: {ex.Message}"), "CopyFolderError", ErrorCategory.InvalidOperation, newFolder));
            return;
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
            _this.WriteError(new ErrorRecord(
                new OrchException(srcDrive.NameColonSeparator,
                $"{msg}: {srcFolder.GetPSPath()} does not have test set with Id = {srcTestSetId}."), "CopyTestSetError", ErrorCategory.InvalidOperation, dstDrive));
            return null;
        }

        var dstTestSets = dstDrive.TestSets.Get(newFolder);
        var dstTestSet = dstTestSets.FirstOrDefault(ts => (ts.Name == srcTestSet.Name));
        if (dstTestSet is null)
        {
            _this.WriteError(new ErrorRecord(
                new OrchException(srcDrive.NameColonSeparator,
                $"{msg}: {newFolder.GetPSPath()} does not have test set with Name = '{srcTestSet.Name}'."), "CopyTestSetError", ErrorCategory.InvalidOperation, dstDrive));
        }
        return dstTestSet;
    }

    internal static void CopyTestSetSchedules(IWritableHost _this,
        OrchDriveInfo srcDrive, Folder srcFolder, List<WildcardPattern>? wpName,
        OrchDriveInfo dstDrive, Folder newFolder, ProgressReporter reporter,
        bool shouldProcess, CancellationToken cancelToken)
    {
        if (newFolder.FolderType == "Personal") return;

        // Confirmed that test entities do not exist in v17
        //if (srcDrive.OrchAPISession.ApiVersion < 18) return;
        //if (dstDrive.OrchAPISession.ApiVersion < 18) return;

        // Considering that cmdlets may be executed consecutively in a script,
        // we shouldn't have been clearing the cache each time..
        // Comment out the below.

        // Clear this folder's cache so we can copy the latest state
        //srcDrive._dicTestSetSchedules?.TryRemove(srcFolder.Id ?? 0, out _);

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

        // Confirmed that test entities do not exist in v17
        //if (srcDrive.OrchAPISession.ApiVersion < 18) return;
        //if (srcDrive.OrchAPISession.ApiVersion < 18) return;

        // Considering that cmdlets may be executed consecutively in a script,
        // we shouldn't have been clearing the cache each time..
        // Comment out the below.

        // Clear this folder's cache so we can copy the latest state
        //srcDrive._dicTestDataQueues?.TryRemove(srcFolder.Id ?? 0, out _);

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

                try
                {
                    dstDrive.OrchAPISession.CreateTestDataQueue(newFolder.Id ?? 0, postingTestDataQueue);
                    CopyTestDataQueueItems(_this,
                        srcDrive, srcFolder, testDataQueue,
                        dstDrive, newFolder, testDataQueue.Name!, true);
                }
                catch (Exception ex)
                {
                    _this.WriteError(new ErrorRecord(new OrchException(newFolder.GetPSPath(), msg, ex), "CopyApiTriggerError", ErrorCategory.InvalidOperation, target));
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
        var dstCurrentUser = dstDrive.GetCurrentUser();
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
            catch { }
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
                    srcOwnerUserName = srcDrive.GetUsers().FirstOrDefault(u => u.Id == srcWorkspace.OwnerId)?.UserName;
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
                    var dstUser = dstDrive.GetUsers().FirstOrDefault(u => string.Compare(u.UserName, dstUserName, StringComparison.OrdinalIgnoreCase) == 0);
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
                WriteError(new ErrorRecord(new OrchException(srcFolder.GetPSPath(),
                    $"No corresponding personal workspace exists in {dstDrive.NameColonSeparator}. " +
                    $"You may want to start exploring the destination personal workspace and reflect it in this PS console by running `Clear-OrchCache {dstDrive.NameColon}'."),
                    "CopyFolderError", ErrorCategory.InvalidOperation, srcFolder));
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

        if (ShouldProcess(target, $"Copy Folder"))
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

                    srcDrive._dicReleases?.TryRemove(srcFolder.Id ?? 0, out _);
                    dstDrive._dicReleases?.TryRemove(dstFolder.Id ?? 0, out _);
                    dstDrive.FolderMachinesAssigned.ClearCache(dstFolder);

                    if (!ExcludeEntities)
                    {
                        int rootIndex = 0;

                        // #1 Copy folder users
                        string msg;
                        msg = "Copying folder users...      ";
                        reporter.WriteProgress(++rootIndex);
                        srcDrive.FolderUsersWithInherited.ClearCache(srcFolder);
                        srcDrive.FolderUsersWithNoInherited.ClearCache(srcFolder);
                        using var reporterFolderUsers = new ProgressReporter(this, 100, Int32.MaxValue, msg);
                        CopyFolderUsers(this, srcDrive, srcFolder, null, null, dstDrive, newFolder, reporterFolderUsers, true, cancelToken, userMapping);

                        cancelToken.ThrowIfCancellationRequested();

                        // #2 Copy folder machines
                        msg = "Copying folder machines...   ";
                        reporter.WriteProgress(++rootIndex);
                        srcDrive.FolderMachinesAssigned.ClearCache(srcFolder);
                        using var reporterFolderMachines = new ProgressReporter(this, 200, Int32.MaxValue, msg);
                        CopyFolderMachines(this, srcDrive, srcFolder, null, dstDrive, newFolder, reporterFolderMachines, true, cancelToken);

                        cancelToken.ThrowIfCancellationRequested();

                        // #3 Copy buckets
                        // Buckets must be copied before copying processes
                        msg = "Copying buckets...           ";
                        reporter.WriteProgress(++rootIndex);
                        using var reporterBuckets = new ProgressReporter(this, 300, Int32.MaxValue, msg);
                        CopyBuckets(this, srcDrive, srcFolder, null, dstDrive, newFolder, reporterBuckets, true, cancelToken);

                        cancelToken.ThrowIfCancellationRequested();

                        // #4 Copy folder packages
                        msg = "Copying packages...          ";
                        reporter.WriteProgress(++rootIndex);
                        using var reporterPackages = new ProgressReporter(this, 400, Int32.MaxValue, msg);
                        CopyPackages(this, srcDrive, srcFolder, dstDrive, newFolder, reporterPackages, cancelToken);

                        cancelToken.ThrowIfCancellationRequested();

                        // #5 Copy processes
                        msg = "Copying processes...         ";
                        reporter.WriteProgress(++rootIndex);
                        using var reporterProcesses = new ProgressReporter(this, 500, Int32.MaxValue, msg);
                        CopyProcesses(this, srcDrive, srcFolder, null, dstDrive, newFolder, reporterProcesses, true, cancelToken);

                        cancelToken.ThrowIfCancellationRequested();

                        // #6 Copy assets
                        msg = "Copying assets...            ";
                        reporter.WriteProgress(++rootIndex);
                        srcDrive.Assets.ClearCache(srcFolder);
                        using var reporterAssets = new ProgressReporter(this, 600, Int32.MaxValue, msg);
                        CopyAssets(this, srcDrive, srcFolder, null, dstDrive, newFolder, reporterAssets, true, cancelToken, userMapping);

                        cancelToken.ThrowIfCancellationRequested();

                        // #7 Copy queues
                        msg = "Copying queues...            ";
                        reporter.WriteProgress(++rootIndex);
                        using var reporterQueues = new ProgressReporter(this, 700, Int32.MaxValue, msg);
                        CopyQueues(this, srcDrive, srcFolder, null, dstDrive, newFolder, reporterQueues, true, cancelToken);

                        cancelToken.ThrowIfCancellationRequested();

                        // #8 Copy triggers
                        msg = "Copying triggers...          ";
                        reporter.WriteProgress(++rootIndex);
                        srcDrive._dicTriggers?.TryRemove(srcFolder.Id ?? 0, out _);
                        using var reporterTriggers = new ProgressReporter(this, 800, Int32.MaxValue, msg);
                        CopyTriggers(this, srcDrive, srcFolder, null, dstDrive, newFolder, reporterTriggers, true, cancelToken);

                        cancelToken.ThrowIfCancellationRequested();

                        // #8 Copy API triggers
                        msg = "Copying API triggers...      ";
                        reporter.WriteProgress(++rootIndex);
                        srcDrive.ApiTriggers.ClearCache(srcFolder);
                        using var reporterApiTriggers = new ProgressReporter(this, 900, Int32.MaxValue, msg);
                        CopyApiTriggers(this, srcDrive, srcFolder, null, dstDrive, newFolder, reporterApiTriggers, true, cancelToken);

                        cancelToken.ThrowIfCancellationRequested();

                        // #xx Test cases do not need to be copied.
                        // They are automatically created when packages and processes are copied.
                        //msg = "Copying test cases...        ";
                        //reporter.WriteProgress();
                        //using var reporterTestCases = new ProgressReporter(this, 1100, Int32.MaxValue, msg);
                        //CopyTestCases(this, srcDrive, srcFolder, dstDrive, newFolder, reporterTestCases);

                        // #10 Copy test sets
                        msg = "Copying test sets...         ";
                        reporter.WriteProgress(++rootIndex);
                        using var reporterTestSets = new ProgressReporter(this, 1000, Int32.MaxValue, msg);
                        CopyTestSets(this, srcDrive, srcFolder, null, dstDrive, newFolder, reporterTestSets, true, cancelToken);

                        cancelToken.ThrowIfCancellationRequested();

                        // #11 Copy test set schedules
                        msg = "Copying test schedules...    ";
                        reporter.WriteProgress(++rootIndex);
                        using var reporterTestSchedules = new ProgressReporter(this, 1100, Int32.MaxValue, msg);
                        CopyTestSetSchedules(this, srcDrive, srcFolder, null, dstDrive, newFolder, reporterTestSchedules, true, cancelToken);

                        cancelToken.ThrowIfCancellationRequested();

                        // #12 Copy test data queues
                        msg = "Copying test data queues...  ";
                        reporter.WriteProgress(++rootIndex);
                        using var reporterTestDataQueues = new ProgressReporter(this, 1200, Int32.MaxValue, msg);
                        CopyTestDataQueues(this, srcDrive, srcFolder, null, dstDrive, newFolder, reporterTestDataQueues, true, cancelToken);

                        cancelToken.ThrowIfCancellationRequested();

                        // #13 Copy action catalogs
                        msg = "Copying action catalogs...   ";
                        reporter.WriteProgress(++rootIndex);
                        using var reporterActionCatalogs = new ProgressReporter(this, 1300, Int32.MaxValue, msg);
                        //srcDrive.ActionCatalogs.ClearCache(srcFolder);
                        CopyActionCatalogs(this, srcDrive, srcFolder, null, dstDrive, newFolder, reporterTestDataQueues, true, cancelToken);

                        cancelToken.ThrowIfCancellationRequested();
                    }
                }

                if (recurse)
                {
                    var subfolders = GetDirectChildFolders(srcDrive.GetFolders(), srcFolder);
                    if (newFolder.FolderType == "Personal" && subfolders.Count > 0)
                    {
                        WriteWarning($"Subfolders of \"{srcFolder.GetPSPath()}\" cannot be copied into a personal workspace. Skipping {subfolders.Count} subfolder(s).");
                    }
                    else foreach (var subfolder in subfolders)
                    {
                        CopyItemRecurse(srcDrive, subfolder, dstDrive, newFolder, true, cancelToken, userMapping);
                        cancelToken.ThrowIfCancellationRequested();
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
        return false;
    }

    protected override object CopyItemDynamicParameters(string path, string destination, bool recurse)
    {
        return new CopyItem_DynamicParameters();
    }

    private bool ShouldCopyTenantEntities<T>(string kind, OrchDriveInfo srcDrive, IEnumerable<T>? srcEntities, OrchDriveInfo dstDrive)
    {
        if (srcEntities?.Any() ?? false)
        {
            return ShouldProcess($"Item: '{srcDrive.NameColonSeparator}*' Destination: '{dstDrive.NameColonSeparator}'", $"Copy {kind}");
        }
        return false;
    }

    protected override void CopyItem(string path, string copyPath, bool recurse)
    {
        var dynamicParameters = DynamicParameters as CopyItem_DynamicParameters;
        if (dynamicParameters is not null && dynamicParameters.ExcludeEntities.IsPresent)
        {
            ExcludeEntities = true;
        }

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
                CopyLibraryCommand.CopyLibraries(this, [srcDrive], null, null, [dstDrive], true, cancelHandler.Token);
            }

            if (ShouldCopyTenantEntities("Package", srcDrive, srcDrive.GetPackages(srcDrive.RootFolder), dstDrive))
            {
                CopyPackageCommand.CopyPackages(this, [(srcDrive, srcDrive.RootFolder)], srcDrive.RootFolder, null, null, [(dstDrive, dstDrive.RootFolder)], true, cancelHandler.Token);
            }

            if (ShouldCopyTenantEntities("CredentialStore", srcDrive, srcDrive.CredentialStores.Get(), dstDrive))
            {
                CopyCredentialStoreCommand.CopyCredentialStores(this, srcDrive, null, [dstDrive], true, cancelHandler.Token);
            }

            if (ShouldCopyTenantEntities("Role", srcDrive, srcDrive.Roles.Get(), dstDrive))
            {
                CopyRoleCommand.CopyRoles(this, srcDrive, null, [dstDrive], true, cancelHandler.Token);
            }

            if (ShouldCopyTenantEntities("User", srcDrive, srcDrive.GetUsers(), dstDrive))
            {
                CopyUserCommand.CopyUsers(this, srcDrive, null, null, null, [dstDrive], true, cancelHandler.Token, userMapping);
            }

            if (ShouldCopyTenantEntities("Machine", srcDrive, srcDrive.Machines.Get(), dstDrive))
            {
                CopyMachineCommand.CopyMachines(this, srcDrive, null, [dstDrive], true, cancelHandler.Token);
            }

            if (ShouldCopyTenantEntities("Calendar", srcDrive, srcDrive.GetCalendars(), dstDrive))
            {
                CopyCalendarCommand.CopyCalendars(this, srcDrive, null, [dstDrive], true, cancelHandler.Token);
            }

            if (ShouldCopyTenantEntities("Webhook", srcDrive, srcDrive.Webhooks.Get(), dstDrive))
            {
                CopyWebhookCommand.CopyWebhooks(this, srcDrive, null, [dstDrive], true, cancelHandler.Token);
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
                    isDirty = CopyItemRecurse(srcDrive, folderToBeCopied, dstDrive, dstFolder ?? dstDrive.RootFolder!, true, cancelHandler.Token, userMapping);
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
