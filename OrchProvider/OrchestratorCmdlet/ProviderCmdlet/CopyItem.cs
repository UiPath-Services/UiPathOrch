using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Provider;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Core;

// Cmdlet class のインスタンスと、CmdletProvider class のインスタンスを統一的に扱えるようにするためのインターフェイス。
// Cmdlet と CmdletProvider のサブクラスで実装しておくと便利だ。
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

// 画面にエラーを出力する処理は、このクラスに集約する。
public static class IWritableHostExtensions
{
    // この実装の一部は Folder の拡張メソッドにした方がいいような気がするが、
    internal static Folder? GetRelativeDstFolder(this IWritableHost _this, Folder srcRootFolder, Folder srcFolder, OrchDriveInfo dstDrive, Folder dstRootFolder, bool includeRoot = false)
    {
        var strDstRootFolder = dstRootFolder.FullyQualifiedName;
        //if (strDstRootFolder != "") strDstRootFolder += '/';

        // srcFolder の、srcRootFolder からの相対パスを取得
        string relativePath = srcFolder.FullyQualifiedName![srcRootFolder.FullyQualifiedName!.Length..];
        relativePath = relativePath.TrimStart('/').TrimEnd('/');

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

    // 例外処理とコンソールへのエラーメッセージ出力が不要の場合には、drive.CreatePmGroup() を直接呼び出してほしい。
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
}

// Copy-Item cmdlet
// TODO: フォルダの Description を更新する手段として、Set-ItemProperty を実装したい。
public partial class OrchProvider : NavigationCmdletProvider, IWritableHost
{
    private bool ExcludeEntities = false;

    // テナントパッケージのキャッシュは、一度だけクリアしたいがどうやって実装できるか、、
    //private ReadOnlyCollection<Package> tenantPackagesCache = null;

    // このメソッドは IEnumerable<Folder> を返すと問題が出るので、List<Folder> を返す必要がある
    // (enumeration 中にフォルダを作成すると、enumeration を継続できなくなる)
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

        // 同じ親フォルダーの中でコピーを指示した場合には、コピー先のフォルダー名に - Copy を付加する
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
                // newFolderDisplayName と同じ名前がなければ break
                if (!siblingFolders.Any(f => f.DisplayName == newFolderDisplayName))
                {
                    break;
                }
                ++index;
            }
        }

        // 同名の既存フォルダが存在すれば、フォルダを作成せずに既存フォルダを返す
        Folder targetFolder = dstDrive.GetFolders()
            .Where(f => f.ParentId == dstFolder.Id)
            .FirstOrDefault(f => string.Compare(f.DisplayName, newFolderDisplayName, StringComparison.OrdinalIgnoreCase) == 0);
        if (targetFolder is not null)
        {
            // この警告は、うるさいから出さなくてもいいか。。
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
            dstDrive._dicFolders!.Add(newFolder); // いったん、ソート順を気にせず追加しておく
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
                // 名前で探しても見つかるはずだけど、Id で探したほうが安全かな。。
                roleToAdded = dstTenantRoles.FirstOrDefault(r => r.Id == ur.Id);
            }
            else
            {
                roleToAdded = dstTenantRoles.FirstOrDefault(r => string.Compare(r.Name, ur.Name, StringComparison.OrdinalIgnoreCase) == 0);
            }

            // 別のテナント間でのフォルダコピーでは、同名のロールがない場合があるので
            // エラーを表示して、処理を継続する
            if (roleToAdded is null)
            {
                _this.WriteError(new ErrorRecord(
                    new OrchException(dstDrive.NameColonSeparator,
                    $"{msg}: {dstDrive.NameColon} does not have role with Name ='{ur.Name}'."), "CopyFolderError", ErrorCategory.InvalidOperation, dstDrive));
                continue;
            }

            // クラシックフォルダーから取得したフォルダーユーザーには
            // テナントロールが含まれているので、それを除いておかねばならない
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
        bool shouldProcess, CancellationToken cancelToken)
    {
        if (newFolder.FolderType == "Personal") return;

        var srcFolderUsers = srcDrive.FolderUsersWithNoInherited.Get(srcFolder)
            .FilterByWildcards(u => u?.UserEntity?.UserName, wpUserName).ToList();
        if (srcFolderUsers.Count == 0)
        {
            return;
        }

        // すでに割り当て済みのユーザーを取得する
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

                // フォルダロールがひとつもなければ、API call は失敗するので、エラーを出力しこのユーザーは追加しない
                // と思ったけど、mix のロールが割り当て済みならエラーにならない気がするので、API call してみる。
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

                // すでに同じユーザーがこのフォルダーにアサイン済みであれば
                // 既存のロールを保持するようにする
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

                    // ディレクトリを検索しなければ。。
                    var resolved = dstDrive.SearchDirectory(userName)?
                        .Where(u => u.type == type)
                        .Where(u => string.Compare(u.identityName, userName, true) == 0)
                        .FirstOrDefault();
                    if (resolved is null)
                    {
                        // srcDrive のユーザーと同名のユーザーが、dstDrive のディレクトリに見つからない！
                        // そこで、このユーザーの email でも dstDrive で検索する。

                        // まず srcUser の email を確認する。
                        var srcUserEmail = srcDrive.GetUsers().FirstOrDefault(u => u.Id == userRole.UserEntity?.Id)?.EmailAddress;

                        // TODO: ローカルユーザーにいなければ、ディレクトリを探さないと。
                        //if (string.IsNullOrEmpty(srcUserEmail))
                        //{
                        //    // もしテナントユーザーにいなければ、ディレクトリを検索する。
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
            // 機密アプリの場合には、この機密アプリをアサインする
        }
        else
        {
            // 非機密アプリの場合には、現在のユーザーをアサインする
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

        // 宛先が同じドライブでも、名前で Id を探した方がいいかな。。
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

        #region srcMachine の PropagateToSubFolders が true なら、dstMachine でも true にする
        foreach (var dstMachine in machinesToBeAdded)
        {
            var srcMachine = srcMachines.FirstOrDefault(m => string.Compare(m.Name, dstMachine.Name, true) == 0);
            if (srcMachine is null) continue; // null のはずはないが念のため

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
        // srcFolder と dstFolder の両方がルート直下のフィードつきフォルダーでなければ、何もしない
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

        // スクリプトで連続して cmdlet を実行することを考えると、
        // いちいちキャッシュをクリアするべきじゃなかった。。
        // 下記はコメントアウトしておく。

        // 最新の状態をコピーできるように、このフォルダーのキャッシュを削除する
        // テナントのパッケージについては、削除する適切なタイミングがないが
        // 前段の条件式により、ここには到達しないので、考慮する必要はない
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
        // スクリプトで連続して cmdlet を実行することを考えると、
        // いちいちキャッシュをクリアするべきじゃなかった。。
        // 下記はコメントアウトしておく。

        // 最新の状態をコピーできるように、このフォルダーのキャッシュを削除する
        //srcDrive._dicReleases?.TryRemove(srcFolder.Id ?? 0, out _);

        // あとで最新のプロセス一覧を取得する必要があるため、dstDrive のキャッシュも削除しておく
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

                // GetRelease と GetReleaseById で、返される内容がどの程度異なるか？
                // 少なくとも、GetReleaseById でないと返されない内容があるのは確かなようだ。
                //var releaseInCache = processes.FirstOrDefault(p => p.Id == process.Id);

                //reporter.WriteProgress(++index, $"{index:D}/{processes.Count} {process.Name}");
                reporter.WriteProgress(++index);

                #region src の release 情報を取得
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

                #region srcRelease のエントリポイントの Id を取得
                #endregion

                string dstFeedId = dstDrive.FolderFeedId.Get(newFolder);

                #region エントリポイントの Id を移行
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
                    // 少しエラー処理が適当かな。。あとで丁寧に書き直したい。この一部を↑の region に移す。
                    string msg2 = "Migrating entry point id {srcRelease.EntryPointId} failed.";
                    _this.WriteError(new ErrorRecord(new OrchException(target, $"{msg}: {msg2}", ex), "GetProcessError", ErrorCategory.InvalidOperation, target));
                    // ここでは continue しない方が良い。
                }
                #endregion

                #region コピー先にある、同名のプロセスを削除する
                // ここまで、問題なくプロセスをコピーする準備が整ったら、
                // 個人用ワークスペースにプロセスをコピーする場合に限り、同名の既存プロセスがあれば、上書きコピーする。
                // つまり、同名の既存のプロセスがあれば、削除しておく。
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

                #region srcRelease の ReleaseRetension を取得
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

                #region dstFolder に Release を作成
                Release created = null;
                try
                {
                    Release postingRelease = OrchCollectionExtensions.DeepCopy(srcRelease);
                    // postingRelease.Path = null;// JsonIgnore 属性がついているので不要
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

                    // EnvironmentId は、モダンフォルダーでは null にしておかないといけない。
                    // クラシックフォルダー（ProvisionType == "Manual"）でも、正しい Id に付け替えないと動かない。
                    // けど、Get-OrchEnvironment みたいなのは作らなくても良いかなと思う。。
                    // コピー元とコピー先のフォルダーが同じであれば、EnvironmentId はそのままにして
                    // そうでなければ null にしとけ。
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

                    // 画面が乱れるから、この表示はしなくて良いか。。
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

                // 古いバージョンの API だと、下記が必要になるのか？
                //#region srcRelease の ReleaseRetension を取得
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

                //#region createdRelease に ReleaseRetension をコピー
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

    // コピー先にグループがない場合には、同じ名前のグループを作成する
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

    // TODO: この実装が足りてないではないか。ディレクトリ検索でユーザーを探さないと。
    // 現在の実装はローカルユーザーしか探していない。
    internal static Entities.User? FindDstUser(IWritableHost _this,
        OrchDriveInfo srcDrive, 
        OrchDriveInfo dstDrive, Folder newFolder, Int64? srcUserId, string msg)
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

            var dstUsers = dstDrive.GetUsers();
            var dstUser = dstUsers.FirstOrDefault(u => string.Compare(u.UserName, srcUser.UserName, StringComparison.OrdinalIgnoreCase) == 0);
            if (dstUser is null)
            {
                // ユーザーが見つからない！ Email でも探してみる。
                dstUser = dstUsers.FirstOrDefault(u => string.Compare(u.UserName, srcUser.EmailAddress, StringComparison.OrdinalIgnoreCase) == 0);
            }

            if (dstUser is null)
            {
                string target = newFolder.GetPSPath();
                _this.WriteError(new ErrorRecord(new OrchException(target, $"{msg}: {dstDrive.NameColon} does not have user with Name = '{srcUser.UserName}'."), "GetCredentialStoreError", ErrorCategory.InvalidOperation, target));
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
                // クラシックフォルダの場合には、GET /odata/Sessions でクラシックロボットを探す
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

            // 現在の実装では、ロボットを UR の Windows アカウント名 (domain\user みたいな ID) で探している
            // ロボット自体の名前で探す方が良いのか？（できるのか？）
            // クラシックロボットでは、それっぽいロボット名が見つからない

            var dstRobots = dstDrive.RobotsFromFolder.Get(dstFolder);
            var dstRobot = dstRobots?.FirstOrDefault(r => 
                r.Type == srcRobot_Type && // たぶん、この srcRobot.Type は必ず "Unattended" になっているはず。。
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

            // TODO: これはキャッシュにかえた。ちゃんと動いているか？
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

        // スクリプトで連続して cmdlet を実行することを考えると、
        // いちいちキャッシュをクリアするべきじゃなかった。。
        // 下記はコメントアウトしておく。

        // 最新の状態をコピーできるように、このフォルダーのキャッシュを削除する
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
        // TODO: この数字は正しいか？ 12 より古い数字の Orchestrator はもうないような気がする。
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
        bool shouldProcess, CancellationToken cancelToken)
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

                // リンクを取得し、ターゲットドライブのリンク先フォルダに同名のエンティティがあれば
                // そこにリンクを張るだけにする
                if (LinkAsset(_this, srcDrive, srcFolder, dstDrive, newFolder, asset, msg))
                {
                    continue;
                }

                target = newFolder.GetPSPath();

                bool bCredentailWarningNeeded = false;
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
                    // postingAsset.Path = null; // JsonIgnore 属性がついているので不要

                    if (postingAsset.ValueType == "Credential")
                    {
                        postingAsset.IntValue = null;
                        postingAsset.BoolValue = null;
                        postingAsset.StringValue = null;
                        postingAsset.CredentialPassword = "!!!PLEASE UPDATE!!!";
                        bCredentailWarningNeeded = true;
                    }

                    if (postingAsset.UserValues is not null && postingAsset.UserValues.Count == 0)
                    {
                        postingAsset.UserValues = null;
                        postingAsset.ValueScope = "Global"; // ISSUE: UserValues がないアセットなのに、"PerRobot" となっている場合があった
                    }
                    if (postingAsset.UserValues is not null)
                    {
                        List<AssetUserValue>? migratedUserValues = null;
                        foreach (var userValue in postingAsset.UserValues)
                        {
                            userValue.UserId = FindDstUser(_this, srcDrive, dstDrive, newFolder, userValue.UserId, msg)?.Id;
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
                            // userValue.Path = null; // JsonIgnore 属性がついているので不要
                            // userValue.Name = null; // JsonIgnore 属性がついているので不要
                            // userValue.PathName = null; // JsonIgnore 属性がついているので不要
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

                    // 画面が乱れるから、この表示はしなくて良いか。。
                    //if (!shouldProcess && created is not null)
                    //{
                    //    created.Path = newFolder.GetPSPath();
                    //    _this.WriteObject(created);
                    //}

                    if (bCredentailWarningNeeded && !bCredentialWarningDone)
                    {
                        target = System.IO.Path.Combine(newFolder.GetPSPath(), created?.Name ?? "");
                        _this.WriteWarning($"'{target}': Please update credential asset passwords with Set-OrchCredentialAsset cmdlet.");
                        bCredentialWarningDone = true;
                    }

                    // キャッシュのクリアは、各 Copy-OrchXxx ですることに決めた。ここではしない。
                    // フォルダのコピー時には、コピー先の新規フォルダのキャッシュは空だからね。
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
        // TODO: この数字は正しいか？ 12 より古い数字の Orchestrator はもうないような気がする。
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
        // スクリプトで連続して cmdlet を実行することを考えると、
        // いちいちキャッシュをクリアするべきじゃなかった。。
        // 下記はコメントアウトしておく。

        // 最新の状態をコピーできるように、このフォルダーのキャッシュを削除する
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

                // リンクを取得し、ターゲットドライブのリンク先フォルダに同名のエンティティがあれば
                // そこにリンクを張るだけにする
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

                // TODO: ProcessScheduleId がコピーできていない気がする？
                // どこかから移行しないといけなそうな値だ。
                // Get-OrchQueue -Recurse | select name,ProcessScheduleId で確認
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
                    RetentionAction = srcQueue.RetentionAction ?? "Delete", // TODO: OR バージョン依存。CreateQueue() 側で行うべきかも
                    RetentionPeriod = srcQueue.RetentionPeriod ?? 30, // TODO: OR バージョン依存。CreateQueue() 側で行うべきかも
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
                    // None は Keep という意味。Automation Cloud では、None は使えない。
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

                    // 画面が乱れるから、この表示はしなくて良いか。。
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
                        // RobotId を移行
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
                // postingTrigger.Path = null; // JsonIgnore 属性がついているので不要
                postingTrigger.TimeZoneIana = null;
                postingTrigger.ExternalJobKeyScheduler = null;
                postingTrigger.StartProcessCronSummary = null;
                postingTrigger.PackageName = null;
                if (postingTrigger.SpecificPriorityValue.HasValue) postingTrigger.JobPriority = null;

                // API で取得したトリガーの Enabled は null になることはないようだ。
                // また、Enabled を null としてトリガーを POST すると、この Enabled は true になるようだ。
                if (postingTrigger.Enabled.GetValueOrDefault())
                {
                    // コピー元のトリガーが true である場合に限り警告
                    _this.WriteWarning($"'{newFolder.GetPSPath()}\\{srcTrigger.Name}': This trigger will be disabled. Please enable it if necessary.");
                }
                // どんな場合であれ、false を代入しておく方が安全だ。
                postingTrigger.Enabled = false; // コピーしたエンティティは無効にしておく

                // キューIDを移行
                // TODO: この条件式不要、中身だけあればいい気がする
                if (srcTrigger.QueueDefinitionId.GetValueOrDefault() != 0)
                {
                    postingTrigger.QueueDefinitionId = FindDstQueue(_this,
                        srcDrive, srcFolder,
                        dstDrive, newFolder, srcTrigger.QueueDefinitionId, msg)?.Id;
                    // キュートリガーなのにキューが見つからなかったら、これはコピーしなくて良いのでは。
                    if (postingTrigger.QueueDefinitionId is null) continue;
                }

                // プロセスIDを移行
                postingTrigger.ReleaseId = FindDstRelease(_this,
                    srcDrive, srcFolder,
                    dstDrive, newFolder, srcTrigger.ReleaseId, msg)?.Id;
                if (postingTrigger.ReleaseId is null)
                {
                    // ReleaseId は埋まっていないと API がエラーを返すため、処理を続行できない
                    // エラーは FindDstRelease() が出力済み
                    continue;
                }

                // MachineRobots を移行
                postingTrigger.MachineRobots = MigrateMachineRobots(_this, msg,
                    srcDrive, srcFolder,
                    dstDrive, newFolder,
                    postingTrigger.MachineRobots,
                    postingTrigger.ExecutorRobots);

                // ExecutorRobots を移行
                postingTrigger.ExecutorRobots = MigrateExecutorRobots(_this, msg,
                    srcDrive, srcFolder,
                    dstDrive, newFolder,
                    postingTrigger.ExecutorRobots);

                // カレンダー Id を移行
                postingTrigger.CalendarId = FindDstCalendar(_this, srcDrive, dstDrive,
                    postingTrigger.CalendarId, msg)?.Id;
                postingTrigger.CalendarKey = null;

                if (newFolder.ProvisionType != "Manual")
                {
                    postingTrigger.EnvironmentId = null;
                    postingTrigger.StartStrategy = 1;// StartStrategy って何だろう。。
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

                    // 画面が乱れるから、この表示はしなくて良いか。。
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
        // TODO: 14 で成功するか？
        // TODO: 15 で成功するか？
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
                // postingTrigger.Path = null; // JsonIgnore 属性がついているので不要

                // ReleaseKey を移行
                var dstRelease = FindDstRelease(_this,
                        srcDrive, srcFolder,
                        dstDrive, newFolder, trigger.Release?.Id, msg);
                postingTrigger!.ReleaseKey = dstRelease?.Key;
                if (postingTrigger!.ReleaseKey is null) continue;

                // MachineRobots を移行
                postingTrigger.MachineRobots = MigrateMachineRobots(_this, msg,
                    srcDrive, srcFolder,
                    dstDrive, newFolder,
                    postingTrigger.MachineRobots);

                try
                {
                    var created = dstDrive.OrchAPISession.CreateHttpTrigger(newFolder.Id ?? 0, postingTrigger);

                    // 画面が乱れるから、この表示はしなくて良いか。。
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
        // スクリプトで連続して cmdlet を実行することを考えると、
        // いちいちキャッシュをクリアするべきじゃなかった。。
        // 下記はコメントアウトしておく。

        // 最新の状態をコピーできるように、このフォルダーのキャッシュを削除する
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

                // リンクを取得し、ターゲットドライブのリンク先フォルダに同名のエンティティがあれば
                // そこにリンクを張るだけにする
                if (LinkBucket(_this, srcDrive, srcFolder, dstDrive, newFolder, bucket))
                {
                    continue;
                }

                var postingBucket = OrchCollectionExtensions.DeepCopy(bucket);
                postingBucket.Id = null;
                // postingBucket.Path = null; // JsonIgnore 属性がついているので不要
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

                    // 画面が乱れるから、この表示はしなくて良いか。。
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

    // TestCase をコピーする必要はない。
    // TestCase は、テストプロセスパッケージをコピーすることにより作成される。

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

        // TODO: この数字は正しいか？
        // 16 ではテストエンティティがないことは確認済み
        if (srcDrive.OrchAPISession.ApiVersion < 17) return;
        if (dstDrive.OrchAPISession.ApiVersion < 17) return;

        // スクリプトで連続して cmdlet を実行することを考えると、
        // いちいちキャッシュをクリアするべきじゃなかった。。
        // 下記はコメントアウトしておく。

        // 最新の状態をコピーできるように、このフォルダーのキャッシュを削除する
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

                            // TODO: ほえほえを適切なメッセージに直す
                            postingTestSet.RobotId = FindDstRobot(_this,
                                srcDrive, dstDrive, newFolder, postingTestSet.RobotId, "ほえほえ")?.Id;

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

        // 17 ではテストエンティティがないことは確認済み
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

        // 17 ではテストエンティティがないことは確認済み
        if (srcDrive.OrchAPISession.ApiVersion < 18) return;
        if (dstDrive.OrchAPISession.ApiVersion < 18) return;

        // スクリプトで連続して cmdlet を実行することを考えると、
        // いちいちキャッシュをクリアするべきじゃなかった。。
        // 下記はコメントアウトしておく。

        // 最新の状態をコピーできるように、このフォルダーのキャッシュを削除する
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
                // postingTestSetSchedule.Path = null; // JsonIgnore 属性がついているので不要
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

        // 17 ではテストエンティティがないことは確認済み
        if (srcDrive.OrchAPISession.ApiVersion < 18) return;
        if (srcDrive.OrchAPISession.ApiVersion < 18) return;

        // スクリプトで連続して cmdlet を実行することを考えると、
        // いちいちキャッシュをクリアするべきじゃなかった。。
        // 下記はコメントアウトしておく。

        // 最新の状態をコピーできるように、このフォルダーのキャッシュを削除する
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
                // postingTestDataQueue.Path = null; // JsonIgnore 属性がついているので不要
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
        // TODO: この数字は必要？
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
                // postingTaskCatalog.Path = null; // JsonIgnore 属性がついているので不要
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

    // コピー先（新規作成した）フォルダーには、自動で自身が Folder Administrator Role で割り当てられている。
    // コピー元のフォルダーに、自身が割り当てられていなければ、このフォルダーから自身を剥がす。
    // コピー元のフォルダーに、自身が割り当てられているが Folder Administrator Role をもたない場合は
    // 自身から Folder Administrator Role を剥がす。
    // この処理は、ほかのすべてのコピー処理が完了してから行う。（さもないと、ほかのコピー処理が失敗してしまう）
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
            // コピー元フォルダーに自身は割り当てられていないので、コピー先から自身を剥がす
            try
            {
                dstDrive.OrchAPISession.UnassignUserFromFolder(newFolder.Id ?? 0, dstCurrentUser.Id ?? 0);
                dstDrive.FolderUsersWithInherited.ClearCache(newFolder);
                dstDrive.FolderUsersWithNoInherited.ClearCache(newFolder);
            }
            catch { }
            return;
        }

        // コピー元フォルダーの自身に Folder Administrator ロールが割り当てられていなければ
        // コピー先フォルダーの自身から Folder Administrator ロールを剥がす
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
        CancellationToken cancelToken)
    {
        if (srcFolder.FolderType == "Personal")
        {
            if (ExcludeEntities) return false;

            if (srcDrive == dstDrive)
            {
                WriteError(new ErrorRecord(new OrchException(srcFolder.GetPSPath(),
                    "Copying a personal workspace to the same drive is not supported."),
                    "CopyFolderError", ErrorCategory.InvalidOperation, srcFolder));
                return false;
            }
            if (dstFolder != dstDrive.RootFolder)
            {
                WriteError(new ErrorRecord(new OrchException(srcFolder.GetPSPath(),
                    "Copying a personal workspace to anything other than a personal workspace with the same name is not supported."),
                    "CopyFolderError", ErrorCategory.InvalidOperation, srcFolder));
                return false;
            }
            // 同名の個人用ワークスペースが存在しなければリターン
            Folder destinationWorkspace = dstDrive.GetFolder(srcFolder.DisplayName!);
            if (destinationWorkspace is null)
            {
                WriteError(new ErrorRecord(new OrchException(srcFolder.GetPSPath(),
                    $"No personal workspace with the same name exists in {dstDrive.NameColonSeparator}. " +
                    $"You may want to start exploring the destination personal workspace and reflect it in this PS console by running `Clear-OrchCache {dstDrive.NameColon}'."),
                    "CopyFolderError", ErrorCategory.InvalidOperation, srcFolder));
                return false;
            }
        }

        string target = $"Item: '{srcFolder.GetPSPath()}' Destination: '{dstFolder.GetPSPath()}'";

        if (ShouldProcess(target, $"Copy Folder"))
        {
            // totalNum: folder itself, users, machines, packages, processes, assets, 
            // queues, triggers, API triggers, buckets, testsets, testschedules, testdataqueues
            int totalStageNum = 13;
            if (srcFolder.FolderType == "Personal") totalStageNum = 9;
            // Apps はコピーできるんだっけ？

            try
            {
                // srcFolder がルート直下ではなく、かつ dstFolder がルートでないときは
                // フィードを外してコピーする
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
                // 次から始まるスコープ↓は、子供 reporter がタイムリーに消えるように導入したもの。
                {
                    reporter.WriteProgress(0, $"\"{srcFolder.GetPSPath()}\" to \"{dstFolder.GetPSPath()}\"");

                    // #0 フォルダー自身をコピー
                    reporter.WriteProgress(0);
                    newFolder = CopyFolder(srcDrive, srcFolder, dstDrive, dstFolder, feedType!, cancelToken);
                    if (newFolder is null) return false;

                    srcDrive._dicReleases?.TryRemove(srcFolder.Id ?? 0, out _);
                    dstDrive._dicReleases?.TryRemove(dstFolder.Id ?? 0, out _);
                    dstDrive.FolderMachinesAssigned.ClearCache(dstFolder);

                    if (!ExcludeEntities)
                    {
                        int rootIndex = 0;

                        // #1 フォルダーユーザーをコピー
                        string msg;
                        msg = "Copying folder users...      ";
                        reporter.WriteProgress(++rootIndex);
                        srcDrive.FolderUsersWithInherited.ClearCache(srcFolder);
                        srcDrive.FolderUsersWithNoInherited.ClearCache(srcFolder);
                        using var reporterFolderUsers = new ProgressReporter(this, 100, Int32.MaxValue, msg);
                        CopyFolderUsers(this, srcDrive, srcFolder, null, null, dstDrive, newFolder, reporterFolderUsers, true, cancelToken);

                        cancelToken.ThrowIfCancellationRequested();

                        // #2 フォルダーマシンをコピー
                        msg = "Copying folder machines...   ";
                        reporter.WriteProgress(++rootIndex);
                        srcDrive.FolderMachinesAssigned.ClearCache(srcFolder);
                        using var reporterFolderMachines = new ProgressReporter(this, 200, Int32.MaxValue, msg);
                        CopyFolderMachines(this, srcDrive, srcFolder, null, dstDrive, newFolder, reporterFolderMachines, true, cancelToken);

                        cancelToken.ThrowIfCancellationRequested();

                        // #3 バケットをコピー
                        // プロセスをコピーする前に、先にバケットをコピーしておく必要がある
                        msg = "Copying buckets...           ";
                        reporter.WriteProgress(++rootIndex);
                        using var reporterBuckets = new ProgressReporter(this, 300, Int32.MaxValue, msg);
                        CopyBuckets(this, srcDrive, srcFolder, null, dstDrive, newFolder, reporterBuckets, true, cancelToken);

                        cancelToken.ThrowIfCancellationRequested();

                        // #4 フォルダーパッケージをコピー
                        msg = "Copying packages...          ";
                        reporter.WriteProgress(++rootIndex);
                        using var reporterPackages = new ProgressReporter(this, 400, Int32.MaxValue, msg);
                        CopyPackages(this, srcDrive, srcFolder, dstDrive, newFolder, reporterPackages, cancelToken);

                        cancelToken.ThrowIfCancellationRequested();

                        // #5 プロセスをコピー
                        msg = "Copying processes...         ";
                        reporter.WriteProgress(++rootIndex);
                        using var reporterProcesses = new ProgressReporter(this, 500, Int32.MaxValue, msg);
                        CopyProcesses(this, srcDrive, srcFolder, null, dstDrive, newFolder, reporterProcesses, true, cancelToken);

                        cancelToken.ThrowIfCancellationRequested();

                        // #6 アセットをコピー
                        msg = "Copying assets...            ";
                        reporter.WriteProgress(++rootIndex);
                        srcDrive.Assets.ClearCache(srcFolder);
                        using var reporterAssets = new ProgressReporter(this, 600, Int32.MaxValue, msg);
                        CopyAssets(this, srcDrive, srcFolder, null, dstDrive, newFolder, reporterAssets, true, cancelToken);

                        cancelToken.ThrowIfCancellationRequested();

                        // #7 キューをコピー
                        msg = "Copying queues...            ";
                        reporter.WriteProgress(++rootIndex);
                        using var reporterQueues = new ProgressReporter(this, 700, Int32.MaxValue, msg);
                        CopyQueues(this, srcDrive, srcFolder, null, dstDrive, newFolder, reporterQueues, true, cancelToken);

                        cancelToken.ThrowIfCancellationRequested();

                        // #8 トリガーをコピー
                        msg = "Copying triggers...          ";
                        reporter.WriteProgress(++rootIndex);
                        srcDrive._dicTriggers?.TryRemove(srcFolder.Id ?? 0, out _);
                        using var reporterTriggers = new ProgressReporter(this, 800, Int32.MaxValue, msg);
                        CopyTriggers(this, srcDrive, srcFolder, null, dstDrive, newFolder, reporterTriggers, true, cancelToken);

                        cancelToken.ThrowIfCancellationRequested();

                        // #8 APIトリガーをコピー
                        msg = "Copying API triggers...      ";
                        reporter.WriteProgress(++rootIndex);
                        srcDrive.ApiTriggers.ClearCache(srcFolder);
                        using var reporterApiTriggers = new ProgressReporter(this, 900, Int32.MaxValue, msg);
                        CopyApiTriggers(this, srcDrive, srcFolder, null, dstDrive, newFolder, reporterApiTriggers, true, cancelToken);

                        cancelToken.ThrowIfCancellationRequested();

                        // #xx テストケースはコピーする必要がない。
                        // パッケージとプロセスをコピーすれば、自動で出てくる。
                        //msg = "Copying test cases...        ";
                        //reporter.WriteProgress();
                        //using var reporterTestCases = new ProgressReporter(this, 1100, Int32.MaxValue, msg);
                        //CopyTestCases(this, srcDrive, srcFolder, dstDrive, newFolder, reporterTestCases);

                        // #10 テストセットをコピー
                        msg = "Copying test sets...         ";
                        reporter.WriteProgress(++rootIndex);
                        using var reporterTestSets = new ProgressReporter(this, 1000, Int32.MaxValue, msg);
                        CopyTestSets(this, srcDrive, srcFolder, null, dstDrive, newFolder, reporterTestSets, true, cancelToken);

                        cancelToken.ThrowIfCancellationRequested();

                        // #11 テストセットスケジュールをコピー
                        msg = "Copying test schedules...    ";
                        reporter.WriteProgress(++rootIndex);
                        using var reporterTestSchedules = new ProgressReporter(this, 1100, Int32.MaxValue, msg);
                        CopyTestSetSchedules(this, srcDrive, srcFolder, null, dstDrive, newFolder, reporterTestSchedules, true, cancelToken);

                        cancelToken.ThrowIfCancellationRequested();

                        // #12 テストデータキューをコピー
                        msg = "Copying test data queues...  ";
                        reporter.WriteProgress(++rootIndex);
                        using var reporterTestDataQueues = new ProgressReporter(this, 1200, Int32.MaxValue, msg);
                        CopyTestDataQueues(this, srcDrive, srcFolder, null, dstDrive, newFolder, reporterTestDataQueues, true, cancelToken);

                        cancelToken.ThrowIfCancellationRequested();

                        // #13 アクションカタログをコピー
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
                    foreach (var subfolder in subfolders)
                    {
                        CopyItemRecurse(srcDrive, subfolder, dstDrive, newFolder, true, cancelToken);
                        cancelToken.ThrowIfCancellationRequested();
                    }
                }

                // コピー元フォルダーに自身がアサインされていなければ
                // コピー先フォルダーから自身を剥がす
                // これ剥がしちゃうと、リンクがコピーできないな。。
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

        // この親 reporter は、なるべくチカチカしない方が良いので、広いスコープに置く。
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
        if (dstFolder is null) // コピー先に指定されていたのは、存在しないフォルダー名
        {
            WriteError(new ErrorRecord(new OrchException(path, $"{dstDrive.NameColon} does not have folder '{copyPath}'."), "CopyFolderError", ErrorCategory.InvalidOperation, path));
            return;
        }

        // まず、ルートからルートにコピーする場合には、すべてのテナントエンティティをコピーする。
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
                CopyUserCommand.CopyUsers(this, srcDrive, null, null, null, [dstDrive], true, cancelHandler.Token);
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

        // ルートフォルダに対して ShouldProcess("/") を呼び出したくないので
        // ルートフォルダーを recursive copy する場合には特別扱いする
        if (srcFolder == srcDrive.RootFolder)
        {
            bool isDirty = false;
            if (recurse)
            {
                // 個人用ワークスペースとルート直下のフォルダをすべて列挙する。
                // 個人用ワークスペースのフォルダーには、なぜか ParentId が入っていることがあるが、GetFolders() で詐称している。
                var foldersToBeCopied = srcDrive.GetFolders().Where((f => f.ParentId is null && f != srcDrive.RootFolder));
                foreach (var folderToBeCopied in foldersToBeCopied)
                {
                    isDirty = CopyItemRecurse(srcDrive, folderToBeCopied, dstDrive, dstFolder ?? dstDrive.RootFolder!, true, cancelHandler.Token);
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
            bDirty = CopyItemRecurse(srcDrive, srcFolder, dstDrive, dstFolder ?? dstDrive.RootFolder!, recurse, cancelHandler.Token);
        }
        catch (Exception)
        {
            // 例外が漏れた場合は、フォルダーが作成されたかされていないか分からない。。
            // ので、フォルダキャッシュをクリアしちゃう。
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
