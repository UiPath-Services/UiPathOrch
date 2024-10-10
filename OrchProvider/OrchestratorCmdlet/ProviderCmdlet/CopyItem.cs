using Microsoft.Management.Infrastructure.Options;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Text;
using System.Xml.Linq;
using UiPath.OrchAPI;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace UiPath.PowerShell.Core
{
    public interface IWritableHost
    {
        public void WriteError(ErrorRecord errorRecord);
        public void WriteWarning(string text);
        public void WriteProgress(ProgressRecord progressRecord);
        public bool ShouldProcess(string target, string action);
        //public void WriteObject(object sendToPipeline, bool enumerateCollection);
        //public void WriteObject(object sendToPipeline);
    }

    public class CopyItem_DynamicParameters
    {
        [Parameter]
        public SwitchParameter ExcludeEntities { get; set; }
    }

    // Copy-Item cmdlet
    // TODO: フォルダの Description を更新する手段として、Set-ItemProperty を実装したい。
    public partial class OrchProvider : NavigationCmdletProvider, IWritableHost  //, IPropertyCmdletProvider TODO
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
            if (targetFolder != null)
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
            if (newFolder != null)
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
            if (srcRoleIds == null || !srcRoleIds.Any()) return null;

            ReadOnlyCollection<Role> dstTenantRoles = null;
            try
            {
                dstTenantRoles = dstDrive.GetRoles();
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
                if (roleToAdded == null)
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
            OrchDriveInfo srcDrive, Folder srcFolder,  List<WildcardPattern>? wpUserName,
            OrchDriveInfo dstDrive, Folder newFolder, ProgressReporter reporter,
            CancellationToken cancelToken, bool shouldProcess)
        {
            if (newFolder.FolderType == "Personal") return;

            var srcFolderUsers = srcDrive.GetUsersForFolder(srcFolder, false)
                .FilterByWildcards(u => u?.UserEntity?.UserName, wpUserName).ToList();
            if (srcFolderUsers.Count == 0)
            {
                return;
            }

            var dstFolderUsers = dstDrive.GetUsersForFolder(newFolder, false)
                .FilterByWildcards(u => u?.UserEntity?.UserName, wpUserName).ToList();

            string targetFolder = newFolder.GetPSPath();

            reporter.TotalNum = srcFolderUsers.Count;
            int index = 0;
            foreach (var userRole in srcFolderUsers)
            {
                cancelToken.ThrowIfCancellationRequested();

                //reporter.WriteProgress(++index, $"{index:D}/{srcFolderUsers.Count} {userRole.UserEntity!.UserName}");
                reporter.WriteProgress(++index, $"{index:D}/{srcFolderUsers.Count}");

                if (shouldProcess || _this.ShouldProcess($"Item: '{userRole.GetPSPath()}' Destination: '{newFolder.GetPSPath()}'", "Copy Assigned User"))
                {
                    string userName = userRole.UserEntity?.UserName ?? "";
                    string msg = $"Assigning the {userRole.UserEntity?.Type} \"{userName}\"";

                    // assert(userRoles.Roles.Any())
                    List<Int64> newRoleIds = FindDstRoles(_this, srcDrive, userRole.Roles!, dstDrive, msg);

                    // フォルダロールがひとつもなければ、API call は失敗するので、エラーを出力しこのユーザーは追加しない
                    // と思ったけど、mix のロールが割り当て済みならエラーにならない気がするので、API call してみる。
                    //if (newRoleIds == null || !newRoleIds.Any())
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
                    if (existingSameNameUser != null)
                    {
                        newRolesPerFolder.First().RoleIds?.AddRange(existingSameNameUser.Roles!.Select(r => r.Id ?? 0));
                    }

                    try
                    {
                        DomainUserAssignment postingUser = null;
                        if (userRole.UserEntity?.Type == "DirectoryUser")
                        {
                            // ディレクトリを検索しなければ。。
                            var resolved = dstDrive.SearchForUsersAndGroups(userName)?
                                .Where(u => u.type == 0)
                                .Where(u => string.Compare(u.identityName, userName, true) == 0)
                                .FirstOrDefault();
                            if (resolved == null)
                            {
                                _this.WriteError(new ErrorRecord(new OrchException(targetFolder, $"{msg}: {dstDrive.Name}: does not have the DirectoryUser \"{userName}\"."), "AssignFolderUserError", ErrorCategory.InvalidOperation, targetFolder));
                                continue;
                            }

                            postingUser = new DomainUserAssignment
                            {
                                Domain = string.IsNullOrEmpty(resolved.domain) ? "autogen" : resolved.domain,
                                DirectoryIdentifier = resolved.identifier,
                                UserType = "DirectoryUser",
                                RolesPerFolder = newRolesPerFolder
                            };
                            dstDrive.OrchAPISession.AssignDirectoryUser(postingUser);

                            // 以下の実装では不十分だ。
                            //var dstTenantUsers = dstDrive.GetUsers();
                            //var user = dstTenantUsers.FirstOrDefault(u => string.Compare(u.UserName, userName, StringComparison.OrdinalIgnoreCase) == 0);
                            //if (user == null)
                            //{
                            //    _this.WriteError(new ErrorRecord(new OrchException(targetFolder, $"{msg}: {dstDrive.Name}: does not have the DirectoryUser \"{userName}\"."), "AssignFolderUserError", ErrorCategory.InvalidOperation, targetFolder));
                            //    continue;
                            //}

                            //postingUser = new DomainUserAssignment
                            //{
                            //    Domain = string.IsNullOrEmpty(user.Domain) ? "autogen" : user.Domain,
                            //    DirectoryIdentifier = user.Key,
                            //    UserType = user.Type,
                            //    RolesPerFolder = newRolesPerFolder
                            //};
                            //dstDrive.OrchAPISession.AssignDirectoryUser(postingUser);
                            dstDrive._dicUserRoles?.TryRemove((newFolder.Id ?? 0, false), out _);
                            dstDrive._dicUserRoles?.TryRemove((newFolder.Id ?? 0, true), out _);
                        }
                        else if (userRole.UserEntity?.Type == "DirectoryGroup")
                        {
                            var dstTenantGroups = dstDrive.GetPmGroups().Values;
                            var group = dstTenantGroups
                                .Where(g => g != null)
                                .FirstOrDefault(g => string.Compare(g!.displayName, userName, StringComparison.OrdinalIgnoreCase) == 0);
                            if (group == null)
                            {
                                _this.WriteError(new ErrorRecord(new OrchException(targetFolder, $"{msg}: {dstDrive.Name}: does not have the DirectoryGroup \"{userName}\"."), "AssignFolderUserError", ErrorCategory.InvalidOperation, targetFolder));
                                continue;
                            }

                            postingUser = new DomainUserAssignment
                            {
                                Domain = "autogen",
                                DirectoryIdentifier = group.id,
                                UserType = "DirectoryGroup",
                                RolesPerFolder = newRolesPerFolder
                            };
                            dstDrive.OrchAPISession.AssignDirectoryUser(postingUser);
                            dstDrive._dicUserRoles?.TryRemove((newFolder.Id ?? 0, false), out _);
                            dstDrive._dicUserRoles?.TryRemove((newFolder.Id ?? 0, true), out _);
                        }
                        else
                        {
                            // User, Robot, DirectoryRobot, DirectoryExternalApplication 
                            _this.WriteError(new ErrorRecord(new OrchException(targetFolder, $"{msg}: Type {userRole?.UserEntity?.Type} is not implemented."), "AssignFolderUserError", ErrorCategory.InvalidOperation, targetFolder));
                            continue;
                        }
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
            var folderAdministratorRole = drive.GetRoles().FirstOrDefault(r => r.DisplayName == "Folder Administrator");
            if (folderAdministratorRole == null)
                return false;

            if (drive.OrchAPISession.AuthManager.IsConfidentialApp)
            {
                // 機密アプリの場合には、この機密アプリをアサインする
            }
            else
            {
                // 非機密アプリの場合には、現在のユーザーをアサインする
                var currentUser = drive.GetCurrentUser();
                if (currentUser == null) return false;
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
            CancellationToken cancelToken, bool shouldProcess)
        {
            if (newFolder.FolderType == "Personal") return;

            IEnumerable<MachineFolder> srcMachines = null;
            try
            {
                srcMachines = srcDrive.GetMachinesAssignedToFolder(srcFolder)
                    .FilterByWildcards(m => m?.Name, wpNames);
            }
            catch (Exception ex)
            {
                _this.WriteError(new ErrorRecord(new OrchException(srcDrive.NameColonSeparator, ex), "GetFolderMachineError", ErrorCategory.InvalidOperation, srcDrive));
                return;
            }
            if (srcMachines == null || !srcMachines.Any())
            {
                return;
            }

            cancelToken.ThrowIfCancellationRequested();

            // バルクでまとめて追加するので、ユーザーに訊くのは一度だけ
            if (shouldProcess || _this.ShouldProcess($"Item: '{srcFolder.GetPSPath()}' Destination: '{newFolder.GetPSPath()}'", "Copy assigned machines"))
            {
                string targetFolder = newFolder.GetPSPath();
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
                {
                    var dstTenantMachines = dstDrive.GetMachinesAssignableToFolder(newFolder).ToDictionary(m => m.Name!);
                    var machinesToBeAdded = new List<MachineFolder>();

                    string msg1 = "Assigning machines";
                    foreach (var machine in srcMachines)
                    {
                        string msg2 = $"Assigning the machine \"{machine.Name}\"";
                        if (dstTenantMachines!.TryGetValue(machine.Name!, out var matchedMachine))
                        {
                            // dstから一致する要素を取り出し、処理
                            machinesToBeAdded.Add(matchedMachine);
                        }
                        else
                        {
                            _this.WriteError(new ErrorRecord(new OrchException(targetFolder, $"{msg2}: {dstDrive.Name}: does not have the machine \"{machine.Name}\"."), "AssignFolderMachineError", ErrorCategory.InvalidOperation, targetFolder));
                        }
                    }

                    if (machinesToBeAdded.Count == 0)
                    {
                        return;
                    }

                    reporter.TotalNum = machinesToBeAdded.Count;
                    reporter.WriteProgress(machinesToBeAdded.Count, $"{machinesToBeAdded.Count}/{machinesToBeAdded.Count}");
                    try
                    {
                        dstDrive.OrchAPISession.AddMachinesToFolder(newFolder.Id ?? 0, machinesToBeAdded.Select(m => m.Id ?? 0));
                    }
                    catch (Exception ex)
                    {
                        _this.WriteError(new ErrorRecord(new OrchException(targetFolder, msg1, ex), "AssignFolderMachineError", ErrorCategory.InvalidOperation, targetFolder));
                    }
                }
            }
        }

        internal static void CopyPackages(IWritableHost _this,
            OrchDriveInfo srcDrive, Folder srcFolder, 
            OrchDriveInfo dstDrive, Folder newFolder, ProgressReporter reporter, CancellationToken cancelToken)
        {
            // srcFolder と dstFolder の両方がルート直下のフィードつきフォルダーでなければ、何もしない
            if (srcFolder.FeedType != "FolderHierarchy" ||
                newFolder.FeedType != "FolderHierarchy" ||
                srcFolder.ParentId != null ||
                newFolder.ParentId != null)
            {
                return;
            }

            string msg = "Copying the packages";
            string srcFeedId;
            try
            {
                srcFeedId = srcDrive.GetFolderFeedId(srcFolder);
            }
            catch (Exception ex)
            {
                _this.WriteError(new ErrorRecord(new OrchException(srcFolder.GetPSPath(), msg, ex), "UploadPackageError", ErrorCategory.InvalidOperation, srcFolder));
                return;
            }

            string dstFeedId;
            try
            {
                dstFeedId = dstDrive.GetFolderFeedId(newFolder);
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

                    //reporter.WriteProgress(++index, $"{index:D}/{totalNum} {version.Id}:{version.Version}");
                    reporter.WriteProgress(++index, $"{index:D}/{totalNum}");

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
            if (srcBucketId == null || srcBucketId == 0) return null;

            var srcBuckets = srcDrive.GetBuckets(srcFolder);
            var srcBucket = srcBuckets.FirstOrDefault(b => b.Id == srcBucketId);
            if (srcBucket == null)
            {
                _this.WriteError(new ErrorRecord(
                    new OrchException(srcDrive.NameColonSeparator,
                    $"{msg}: {srcDrive.NameColonSeparator} does not have the bucket with Id = {srcBucketId}"), action, ErrorCategory.InvalidOperation, srcDrive));
                return null;
            }

            var dstBuckets = dstDrive.GetBuckets(newFolder);
            var dstBucket = dstBuckets.FirstOrDefault(b => b.Name == srcBucket.Name);
            if (dstBucket == null)
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
            CancellationToken cancelToken, bool shouldProcess)
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
            foreach (var process in processes)
            {
                cancelToken.ThrowIfCancellationRequested();
                
                string target = $"Item: '{process.GetPSPath()}' Destination: '{newFolder.GetPSPath()}'";
                if (shouldProcess || _this.ShouldProcess(target, "Copy process(es)"))
                {
                    msg = $"Copying process {process.GetPSPath()}";

                    // GetRelease と GetReleaseById で、返される内容がどの程度異なるか？
                    // 少なくとも、GetReleaseById でないと返されない内容があるのは確かなようだ。
                    //var releaseInCache = processes.FirstOrDefault(p => p.Id == process.Id);

                    //reporter.WriteProgress(++index, $"{index:D}/{processes.Count} {process.Name}");
                    reporter.WriteProgress(++index, $"{index:D}/{processes.Count}");

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

                    if (srcRelease == null)
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

                    string dstFeedId = dstDrive.GetFolderFeedId(newFolder);

                    #region エントリポイントの Id を移行
                    try
                    {
                        if (srcRelease.EntryPointId.HasValue)
                        {
                            string srcFeedId = srcDrive.GetFolderFeedId(srcFolder);
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
                            if (existingRelease != null)
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
                    // API ver が 15.0 の場合には、リテンションポリシーを読み取れなかった。この正しい数字は、もっと大きいかもしれない。
                    if (srcDrive.OrchAPISession.ApiVersion >= 16)
                    {
                        try
                        {
                            srcRetention = srcDrive.OrchAPISession.GetReleaseRetention(srcFolder.Id ?? 0, srcRelease.Id ?? 0);
                        }
                        catch (Exception ex)
                        {
                            string msg2 = $"Get retention info failed.";
                            _this.WriteError(new ErrorRecord(new OrchException(target, msg + ": " + msg2, ex), "GetRetentionSettingError", ErrorCategory.InvalidOperation, target));
                        }
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

                        if (srcRetention != null)
                        {
                            postingRelease.RetentionAction = srcRetention?.Action;
                            postingRelease.RetentionPeriod = srcRetention?.Period;
                            postingRelease.RetentionBucketId = FindDstBucket(_this,
                                srcDrive, srcFolder, srcRetention!.BucketId,
                                dstDrive, newFolder, "Copy Process", msg)?.Id;
                        }

                        if (dstDrive.OrchAPISession.ApiVersion >= 18)
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

                        if (postingRelease.SpecificPriorityValue != null)
                        {
                            postingRelease.JobPriority = null;
                        }

                        created = dstDrive.OrchAPISession.CreateRelease2(newFolder.Id ?? 0, postingRelease);

                        // 画面が乱れるから、この表示はしなくて良いか。。
                        //if (!shouldProcess && created != null)
                        //{
                        //    created.Path = newFolder.GetPSPath();
                        //    _this.WriteObject(created);
                        //}
                    }
                    catch (Exception ex)
                    {
                        _this.WriteError(new ErrorRecord(new OrchException(target, msg, ex), "CopyProcessError", ErrorCategory.InvalidOperation, target));
                    }

                    if (created == null)
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

                    //if (srcRetention == null)
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

        // TODO: List<IdGroup> を返すようにした方が、コード保守が安全になるような気がする
        internal static IEnumerable<PmGroup>? FindDstIdGroups(IWritableHost _this,
            OrchDriveInfo srcDrive, IEnumerable<string>? srcPmGroupIds,
            OrchDriveInfo dstDrive, string msg)
        {
            if (srcPmGroupIds == null) yield break;

            string target = srcDrive.NameColonSeparator;
            ICollection<PmGroup>? srcPmGroups = null;
            try
            {
                srcPmGroups = srcDrive.GetPmGroups()?.Values;
            }
            catch (Exception ex)
            {
                _this.WriteError(new ErrorRecord(new OrchException(target, msg, ex), "GetPmGroupError", ErrorCategory.InvalidOperation, dstDrive));
                yield break;
            }
            if (srcPmGroups == null)
            {
                _this.WriteError(new ErrorRecord(new OrchException(target, $"{msg}: Failed to retrieve PmGroup."), "GetPmGroupError", ErrorCategory.InvalidOperation, dstDrive));
                yield break;
            }

            target = dstDrive.NameColonSeparator;
            ICollection<PmGroup>? dstPmGroups = null;
            try
            {
                dstPmGroups = dstDrive.GetPmGroups()?.Values;
            }
            catch (Exception ex)
            {
                _this.WriteError(new ErrorRecord(new OrchException(target, msg, ex), "GetPmGroupError", ErrorCategory.InvalidOperation, dstDrive));
                yield break;
            }
            if (dstPmGroups == null)
            {
                _this.WriteError(new ErrorRecord(new OrchException(target, $"{msg}: Failed to retrieve PmGroup."), "GetPmGroupError", ErrorCategory.InvalidOperation, dstDrive));
                yield break;
            }

            foreach (var srcPmGroupId in srcPmGroupIds)
            {
                var srcPmGroup = srcPmGroups.FirstOrDefault(g => g?.id == srcPmGroupId);
                if (srcPmGroup == null)
                {
                    _this.WriteError(new ErrorRecord(new OrchException(srcDrive.NameColonSeparator, $"{msg}: {srcDrive.NameColon} does not have IdGroup with id = {srcPmGroupId}."), "GetGroupIdError", ErrorCategory.InvalidOperation, srcDrive));
                    continue;
                }

                var dstIdGroup = dstPmGroups.FirstOrDefault(g => string.Compare(g!.displayName, srcPmGroup.displayName, StringComparison.OrdinalIgnoreCase) == 0);
                if (dstIdGroup == null)
                {
                    _this.WriteError(new ErrorRecord(new OrchException(dstDrive.NameColonSeparator, $"{msg}: {dstDrive.NameColon} does not have IdGroup with name = {srcPmGroup.displayName}."), "GetGroupIdError", ErrorCategory.InvalidOperation, dstDrive));
                    continue;
                }
                yield return dstIdGroup;
            }
        }

        internal static CredentialStore? FindDstCredentialStore(IWritableHost _this,
            OrchDriveInfo srcDrive,
            OrchDriveInfo dstDrive, Folder newFolder, Int64? srcCredentialStoreId, string msg)
        {
            if (srcCredentialStoreId == null || srcCredentialStoreId.Value == 0) return null;

            try
            {
                CredentialStore srcCredentialStore = srcDrive.GetCredentialStores().FirstOrDefault(cs => (cs.Id ?? 0) == srcCredentialStoreId);
                if (srcCredentialStore == null)
                {
                    string target = $"{srcDrive.NameColonSeparator}";
                    _this.WriteError(new ErrorRecord(new OrchException(target, $"{msg}: {srcDrive.NameColon} does not have credential store with Id = {srcCredentialStoreId}."), "GetCredentialStoreError", ErrorCategory.InvalidOperation, target));
                    return null;
                }

                var dstCredentialStore = dstDrive.GetCredentialStores().FirstOrDefault(cs => string.Compare(cs.Name, srcCredentialStore.Name, StringComparison.OrdinalIgnoreCase) == 0);
                if (dstCredentialStore == null)
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

        internal static Entities.User? FindDstUser(IWritableHost _this,
            OrchDriveInfo srcDrive, 
            OrchDriveInfo dstDrive, Folder newFolder, Int64? srcUserId, string msg)
        {
            if (srcUserId == null || srcUserId == 0) return null;
            //string msg = $"Migrating the user id {Path.Combine(srcDrive.NameColon, srcUserId?.ToString() ?? "")}";
            try
            {
                var srcUser = srcDrive.GetUsers().FirstOrDefault(u => u.Id == srcUserId);
                if (srcUser == null)
                {
                    string target = srcDrive.NameColonSeparator;
                    _this.WriteError(new ErrorRecord(new OrchException(target, $"{msg}: {srcDrive.NameColon} does not have user with Id = {srcUserId}."), "FindUserError", ErrorCategory.InvalidOperation, target));
                    return null;
                }

                var dstUser = dstDrive.GetUsers().FirstOrDefault(u => string.Compare(u.UserName, srcUser.UserName, StringComparison.OrdinalIgnoreCase) == 0);
                if (dstUser == null)
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

        internal static Robot? FindDstRobot(IWritableHost _this,
            OrchDriveInfo srcDrive,
            OrchDriveInfo dstDrive, Folder dstFolder, Int64? srcRobotId, string msg)
        {
            if (srcRobotId == null || srcRobotId == 0) return null;
            try
            {
                var srcRobot = srcDrive.GetRobots()?.FirstOrDefault(r => r.Id == srcRobotId);
                if (srcRobot == null)
                {
                    _this.WriteError(new ErrorRecord(new OrchException(srcDrive.NameColonSeparator, $"{msg}: {srcDrive.NameColon} does not have robot with Id = {srcRobotId}."), "MigrateRobotIdError", ErrorCategory.InvalidOperation, srcDrive));
                    return null;
                }
                //msg = $"Migrating id of the robot {Path.Combine(srcDrive.NameColon, srcRobot.Name!)}";
                var dstRobots = dstDrive.GetRobots();
                var dstRobot = dstRobots?.FirstOrDefault(r => string.Compare(r.Name, srcRobot.Name, StringComparison.OrdinalIgnoreCase) == 0);
                if (dstRobot == null)
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

        internal static MachineFolder? FindDstMachine(IWritableHost _this,
            OrchDriveInfo srcDrive, Folder srcFolder,
            OrchDriveInfo dstDrive, Folder dstFolder, Int64? srcMachineId, string msg)
        {
            if (srcMachineId == null || srcMachineId == 0) return null;
            //string msg = $"Migrating the machine id {Path.Combine(srcDrive.NameColon, srcMachineId?.ToString() ?? "")}";
            try
            {
                var srcMachine = srcDrive.GetMachines().FirstOrDefault(m => m.Id == srcMachineId);
                if (srcMachine == null)
                {
                    string target = srcFolder.GetPSPath();
                    _this.WriteError(new ErrorRecord(new OrchException(target, $"{msg}: {srcDrive.NameColon} does not have machine with Id = {srcMachineId}."), "FindMachineError", ErrorCategory.InvalidOperation, target));
                    return null;
                }
                //msg = $"Migrating id of the machine {Path.Combine(srcDrive.NameColon, srcMachine.Name!)}";
                var dstMachineFolder = dstDrive.GetMachinesAssignedToFolder(dstFolder).FirstOrDefault(m => string.Compare(m.Name, srcMachine.Name, StringComparison.OrdinalIgnoreCase) == 0);
                if (dstMachineFolder == null)
                {
                    string target = dstFolder.GetPSPath();
                    _this.WriteError(new ErrorRecord(new OrchException(target, $"{msg}: {dstDrive.NameColon} does not have machine with Name = '{srcMachine.Name}'."), "MigrateMachineIdError", ErrorCategory.InvalidOperation, target));
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
            if (srcSessionId == null || srcSessionId.Value == 0) return null;

            //string msg = $"Finding the session id with robot id {dstRobotId} and machine id {dstMachineId}";
            MachineSessionRuntime srcSession = null;
            try
            {
                //string query = $"&$filter=((MachineType%20ne%20%27Template%27)%20or%20(MachineScope%20ne%20%27Cloud%27))%20and%20MachineId%20eq%20{dstMachineId}&runtimeType=Unattended&robotId={dstRobotId}";
                //string query = $"&robotId={dstRobot.Id.Value}&MachineId%20eq%20{dstMachineFolder.Id}";

                // TODO: これはキャッシュにかえた。ちゃんと動いているか？
                var srcSessions = srcDrive.GetMachineSessionRuntimesByFolderId(srcFolder).ToList();
                srcSession = srcSessions.FirstOrDefault(s => s.SessionId == srcSessionId);
                if (srcSession == null)
                {
                    _this.WriteError(new ErrorRecord(new OrchException(srcFolder.GetPSPath(), $"{msg}: The session not found with SessionId {srcSessionId}."), "MigrateSessionIdError", ErrorCategory.InvalidOperation, srcFolder));
                    return null;
                }
            }
            catch (Exception ex)
            {
                _this.WriteError(new ErrorRecord(new OrchException(srcFolder.GetPSPath(), msg, ex), "MigrateSessionIdError", ErrorCategory.InvalidOperation, srcFolder));
                return null;
            }

            var srcServiceUserName = srcSession.ServiceUserName;
            var srcMachineName = srcSession.MachineName;
            var srcHostMachineName = srcSession.HostMachineName;

            try
            {
                var dstSessions = dstDrive.OrchAPISession.GetMachineSessionRuntimesByFolderId(dstFolder.Id ?? 0);
                var dstSession = dstSessions.FirstOrDefault(s => 
                    (string.Compare(s.ServiceUserName, srcServiceUserName, true) == 0 && 
                    (string.Compare(s.MachineName, srcMachineName, true) == 0) &&
                    (string.Compare(s.HostMachineName, srcHostMachineName, true) == 0)));
                if (dstSession == null)
                {
                    _this.WriteError(new ErrorRecord(new OrchException(dstFolder.GetPSPath(), $"{msg}: The session not found with ServiceUserName = '{srcServiceUserName}', MachineName ='{srcMachineName}' and HostMachineName = '{srcHostMachineName}'."), "MigrateSessionIdError", ErrorCategory.InvalidOperation, dstFolder));
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
            if (srcQueueId == null || srcQueueId.Value == 0) return null;

            // スクリプトで連続して cmdlet を実行することを考えると、
            // いちいちキャッシュをクリアするべきじゃなかった。。
            // 下記はコメントアウトしておく。

            // 最新の状態をコピーできるように、このフォルダーのキャッシュを削除する
            //srcDrive._dicQueueDefinitions?.TryRemove(srcFolder.Id ?? 0, out var _);

            QueueDefinition srcQueue = null;
            try
            {
                srcQueue = srcDrive.GetQueues(srcFolder)?.FirstOrDefault(q => q.Id == srcQueueId);
            }
            catch (Exception ex)
            {
                string target = srcFolder.GetPSPath();
                _this.WriteError(new ErrorRecord(new OrchException(target, msg, ex), "MigrateQueueIdError", ErrorCategory.InvalidOperation, target));
                return null;
            }
            if (srcQueue == null)
            {
                string target = srcFolder.GetPSPath();
                _this.WriteError(new ErrorRecord(new OrchException(target, $"{msg}: {srcFolder.GetPSPath()} does not have queue with Id = {srcQueueId}."), "MigrateQueueIdError", ErrorCategory.InvalidOperation, target));
                return null;
            }

            QueueDefinition dstQueue = null;
            try
            {
                dstQueue = dstDrive.GetQueues(dstFolder)?.FirstOrDefault(q => string.Compare(q.Name, srcQueue.Name, StringComparison.OrdinalIgnoreCase) == 0);
            }
            catch (Exception ex)
            {
                string target = dstFolder.GetPSPath();
                _this.WriteError(new ErrorRecord(new OrchException(target, msg, ex), "MigrateQueueIdError", ErrorCategory.InvalidOperation, target));
                return null;
            }
            if (dstQueue == null)
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
        //    if (srcQueueId == null || srcQueueId.Value == 0) return null;
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
        //    if (srcQueue == null)
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
        //    if (dstQueue == null)
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
            if (srcReleaseId != null && srcReleaseId == 0) return null;

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
            if (srcRelease == null)
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
                _this.WriteError(new ErrorRecord(new OrchException(target, msg, ex), "MigrateProcessIdError", ErrorCategory.InvalidOperation, target));
                return null;
            }
            if (dstRelease == null)
            {
                _this.WriteError(new ErrorRecord(new OrchException(target, $"{msg}: {dstFolder.GetPSPath()} does not have process with Name = '{srcRelease.Name}'."), "MigrateMachineIdError", ErrorCategory.InvalidOperation, target));
                return null;
            }
            return dstRelease;
        }

        internal static ExtendedCalendar? FindDstCalendar(IWritableHost _this,
            OrchDriveInfo srcDrive, OrchDriveInfo dstDrive, Int64? srcCalendarId, string msg)
        {
            if (srcCalendarId == null || srcCalendarId == 0) return null;

            string target = srcDrive.NameColonSeparator;

            var srcCalendars = srcDrive.GetCalendars();
            var srcCalendar = srcCalendars?.FirstOrDefault(c => c.Id == srcCalendarId);
            if (srcCalendar == null)
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
            if (dstCalendar == null)
            {
                _this.WriteError(new ErrorRecord(new OrchException(dstDrive.NameColonSeparator, $"{msg}: {dstDrive.NameColonSeparator} doesn't have calendar with Name = {srcCalendar.Name}."), "MigrateMachineIdError", ErrorCategory.InvalidOperation, dstDrive));
                return null;
            }
            return dstCalendar;
        }

        internal static IEnumerable<Folder>? FindDstFolders(
            List<Int64>? folderIds, IEnumerable<Folder> srcFolders, IEnumerable<Folder> dstFolders)
        {
            if (folderIds == null)
                return null;

            var selectedSrcFolders = srcFolders.Where(src => folderIds.Contains(src.Id ?? 0)).ToList();
            return dstFolders.Where(dst => selectedSrcFolders.Any(src => string.Compare(src.FullyQualifiedName, dst.FullyQualifiedName, StringComparison.OrdinalIgnoreCase) == 0));
        }

        internal static bool LinkAsset(IWritableHost _this,
            OrchDriveInfo srcDrive, Folder srcFolder, 
            OrchDriveInfo dstDrive, Folder newFolder, Asset asset, string msg)
        {
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
                if (srcLinkFolderIds == null || !srcLinkFolderIds.Any())
                {
                    return false;
                }

                dstLinkFolders = FindDstFolders(
                    srcLinkFolderIds,
                    srcDrive.GetFolders(),
                    dstDrive.GetFolders());

                if (dstLinkFolders == null || !dstLinkFolders.Any())
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
                    var assets = dstDrive.GetAssets(dstLinkFolder);
                    var dstAsset = assets.FirstOrDefault(a => string.Compare(a.Name, asset.Name, StringComparison.OrdinalIgnoreCase) == 0);
                    if (dstAsset == null)
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
            CancellationToken cancelToken, bool shouldProcess)
        {
            dstDrive._dicMachinesAssigned?.TryRemove(newFolder.Id ?? 0, out _);

            string target = srcFolder.GetPSPath();
            string msg = "Copying assets";
            List<Asset> srcAssets;
            try
            {
                srcAssets = srcDrive.GetAssets(srcFolder).FilterByWildcards(a => a?.Name, wpName).ToList();
            }
            catch (Exception ex)
            {
                _this.WriteError(new ErrorRecord(new OrchException(target, msg, ex), "GetAssetError", ErrorCategory.InvalidOperation, srcFolder));
                return;
            }

            reporter.TotalNum = srcAssets.Count;

            int index = 0;
            foreach (var asset in srcAssets)
            {
                cancelToken.ThrowIfCancellationRequested();

                if (shouldProcess || _this.ShouldProcess($"Item: '{asset.GetPSPath()}' Destination: '{newFolder.GetPSPath()}'", "Copy Asset"))
                {
                    msg = $"Copying asset {asset.GetPSPath()}";
                    //reporter.WriteProgress(++index, $"{index:D}/{srcAssets.Count} {asset.Name}");
                    reporter.WriteProgress(++index, $"{index:D}/{srcAssets.Count}");

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

                        if (postingAsset.UserValues != null && postingAsset.UserValues.Count == 0)
                        {
                            postingAsset.UserValues = null;
                            postingAsset.ValueScope = "Global"; // ISSUE: UserValues がないアセットなのに、"PerRobot" となっている場合があった
                        }
                        if (postingAsset.UserValues != null)
                        {
                            List<AssetUserValue>? migratedUserValues = null;
                            foreach (var userValue in postingAsset.UserValues)
                            {
                                userValue.UserId = FindDstUser(_this, srcDrive, dstDrive, newFolder, userValue.UserId, msg)?.Id;
                                if (userValue.UserId == null || userValue.UserId == 0)
                                {
                                    continue;
                                }

                                if (userValue.MachineId != null && userValue.MachineId != 0)
                                {
                                    userValue.MachineId = FindDstMachine(_this,
                                        srcDrive, srcFolder,
                                        dstDrive, newFolder, userValue.MachineId, msg)?.Id;
                                    if (userValue.MachineId == null || userValue.MachineId == 0)
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
                            if (migratedUserValues == null)
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
                        //if (!shouldProcess && created != null)
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
                if (srcLinkFolderIds == null || !srcLinkFolderIds.Any())
                {
                    return false;
                }

                dstLinkFolders = FindDstFolders(
                    srcLinkFolderIds,
                    srcDrive.GetFolders(),
                    dstDrive.GetFolders());

                if (dstLinkFolders == null || !dstLinkFolders.Any())
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
                    var queues = dstDrive.GetQueues(dstLinkFolder);
                    var dstQueue = queues.FirstOrDefault(a => string.Compare(a.Name, queue.Name, StringComparison.OrdinalIgnoreCase) == 0);
                    if (dstQueue == null)
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
            CancellationToken cancelToken, bool shouldProcess)
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
                srcQueues = srcDrive.GetQueues(srcFolder).FilterByWildcards(q => q?.Name, wpName).ToList();
            }
            catch (Exception ex)
            {
                _this.WriteError(new ErrorRecord(new OrchException(target, msg, ex), "GetQueueError", ErrorCategory.InvalidOperation, target));
            }

            if (srcQueues == null || !srcQueues.Any())
            {
                return;
            }

            reporter.TotalNum = srcQueues.Count;

            int index = 0;
            foreach (var queue in srcQueues)
            {
                cancelToken.ThrowIfCancellationRequested();

                if (shouldProcess || _this.ShouldProcess($"Item: '{queue.GetPSPath()}' Destination: '{newFolder.GetPSPath()}'", "Copy Queue"))
                {
                    target = srcFolder.GetPSPath();
                    msg = $"Copying queue {queue.GetPSPath()}";
                    //reporter.WriteProgress(++index, $"{index:D}/{srcQueues.Count} {queue.Name}");
                    reporter.WriteProgress(++index, $"{index:D}/{srcQueues.Count}");

                    QueueDefinitionPosting postingQueue = null;

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
                    if (srcQueue == null)
                    {
                        _this.WriteError(new ErrorRecord(new OrchException(target, $"{msg}: Failed to get queue info."), "GetQueueError", ErrorCategory.InvalidOperation, target));
                        continue;
                    }

                    QueueRetentionSetting srcRetention = null;
                    try
                    {
                        if (srcDrive.OrchAPISession.ApiVersion >= 16)
                        {
                            srcRetention = srcDrive.OrchAPISession.GetQueueRetention(srcFolder.Id ?? 0, queue.Id ?? 0);
                        }
                    }
                    catch (Exception ex)
                    {
                        _this.WriteError(new ErrorRecord(new OrchException(target, $"{msg}: Failed to get queue retention settings", ex), "GetQueueError", ErrorCategory.InvalidOperation, target));
                    }

                    Int64? releaseId = null;
                    if (srcQueue.ReleaseId != null && srcQueue.ReleaseId != 0)
                    {
                        releaseId = FindDstRelease(_this,
                            srcDrive, srcFolder,
                            dstDrive, newFolder,
                            srcQueue.ReleaseId, msg)?.Id;
                    }

                    // TODO: ProcessScheduleId がコピーできていない気がする？
                    // どこかから移行しないといけなそうな値だ。
                    // Get-OrchQueue -Recurse | select name,ProcessScheduleId で確認
                    postingQueue = new QueueDefinitionPosting()
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
                        RetentionAction = srcRetention?.Action ?? "Delete", // TODO: OR バージョン依存。CreateQueue() 側で行うべきかも
                        RetentionPeriod = srcRetention?.Period ?? 30, // TODO: OR バージョン依存。CreateQueue() 側で行うべきかも
                        Tags = srcQueue.Tags
                    };

                    try
                    {
                        var created = dstDrive.OrchAPISession.CreateQueue(newFolder.Id ?? 0, postingQueue!);

                        // 画面が乱れるから、この表示はしなくて良いか。。
                        //if (!shouldProcess && created != null)
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

        internal static void CopyTriggers(IWritableHost _this,
            OrchDriveInfo srcDrive, Folder srcFolder, List<WildcardPattern>? wpName,
            OrchDriveInfo dstDrive, Folder newFolder, ProgressReporter reporter,
            CancellationToken cancelToken, bool shouldProcess)
        {
            string target = null;
            string msg = "Copying triggers";
            List<ProcessSchedule> srcTriggers = null;
            try
            {
                srcTriggers = srcDrive.GetProcessSchedules(srcFolder).FilterByWildcards(t => t?.Name, wpName).ToList();
            }
            catch (Exception ex)
            {
                _this.WriteError(new ErrorRecord(new OrchException(srcFolder.GetPSPath(), msg, ex), "GetTriggerError", ErrorCategory.InvalidOperation, target));
                return;
            }

            reporter.TotalNum = srcTriggers.Count;

            int index = 0;
            foreach (var srcTrigger in srcTriggers)
            {
                cancelToken.ThrowIfCancellationRequested();

                if (shouldProcess || _this.ShouldProcess($"Item: '{srcTrigger.GetPSPath()}' Destination: '{newFolder.GetPSPath()}'", "Copy Trigger"))
                {
                    target = newFolder.GetPSPath();
                    msg = $"Copying trigger {srcTrigger.GetPSPath()}";

                    //reporter.WriteProgress(++index, $"{index:D}/{srcTriggers.Count} {srcTrigger.Name}");
                    reporter.WriteProgress(++index, $"{index:D}/{srcTriggers.Count}");

                    var postingTrigger = OrchCollectionExtensions.DeepCopy(srcTrigger);
                    postingTrigger.Id = null;
                    postingTrigger.StartProcessNextOccurrence = null;
                    postingTrigger.Key = null;
                    postingTrigger.ReleaseKey = null;
                    // postingTrigger.Path = null; // JsonIgnore 属性がついているので不要
                    //postingTrigger.TimeZoneIana = null;
                    postingTrigger.ExternalJobKeyScheduler = null;
                    postingTrigger.StartProcessCronSummary = null;
                    postingTrigger.PackageName = null;

                    // キューIDを移行
                    // TODO: この条件式不要、中身だけあればいい気がする
                    if (srcTrigger.QueueDefinitionId != null && srcTrigger.QueueDefinitionId.Value != 0)
                    {
                        postingTrigger.QueueDefinitionId = FindDstQueue(_this,
                            srcDrive, srcFolder,
                            dstDrive, newFolder, srcTrigger.QueueDefinitionId, msg)?.Id;
                        // 見つからなくても処理を続行
                    }

                    // プロセスIDを移行
                    postingTrigger.ReleaseId = FindDstRelease(_this,
                        srcDrive, srcFolder,
                        dstDrive, newFolder, srcTrigger.ReleaseId, msg)?.Id;
                    if (postingTrigger.ReleaseId == null)
                    {
                        // ReleaseId は埋まっていないと API がエラーを返すため、処理を続行できない
                        // エラーは FindDstRelease() が出力済み
                        continue;
                    }

                    // マシンIDを移行
                    Int64? robotId = null;
                    if (postingTrigger.MachineRobots != null)
                    {
                        foreach (var machineRobot in postingTrigger.MachineRobots)
                        {
                            // RobotId を移行
                            var robot = FindDstRobot(_this,
                                srcDrive,
                                dstDrive, newFolder, machineRobot.RobotId, msg);
                            // 見つからず、robot == null でもコピー処理を続行

                            machineRobot.RobotId = robot?.Id;
                            //machineRobot.RobotUserName = robot?.Username;
                            machineRobot.RobotUserName = null;
                            robotId = robot?.Id;

                            // MachineId を移行
                            MachineFolder dstMachineFolder = null;
                            if (machineRobot.MachineId != null && machineRobot.MachineId != 0)
                            {
                                dstMachineFolder = FindDstMachine(_this,
                                    srcDrive, srcFolder,
                                    dstDrive, newFolder, machineRobot.MachineId, msg);
                                machineRobot.MachineId = dstMachineFolder?.Id;

                                // 見つからず、machineRobot.MachineId == null でもコピー処理を続行
                            }

                            //machineRobot.MachineName = machine?.Name;
                            machineRobot.MachineName = null;

                            if (machineRobot.SessionId != null && machineRobot.SessionId != 0)
                            {
                                machineRobot.SessionId = FindDstSession(_this,
                                    srcDrive, srcFolder,
                                    dstDrive, newFolder, machineRobot.SessionId, msg)?.SessionId;
                            }
                        }
                    }
                    if (robotId.HasValue)
                    {
                        postingTrigger.ExecutorRobots = new RobotExecutor[1];
                        postingTrigger.ExecutorRobots[0] = new RobotExecutor()
                        {
                            Id = robotId
                        };
                    }

                    // カレンダー Id を移行
                    postingTrigger.CalendarId = FindDstCalendar(_this, srcDrive, dstDrive,
                        postingTrigger.CalendarId, msg)?.Id;
                    postingTrigger.CalendarKey = null;

                    if (newFolder.ProvisionType != "Manual")
                    {
                        postingTrigger.EnvironmentId = null;
                        postingTrigger.StartStrategy = 1;// StartStrategy って何だろう。。
                    }

                    try
                    {
                        var created = dstDrive.OrchAPISession.PostProcessSchedule(newFolder.Id ?? 0, postingTrigger);

                        // 画面が乱れるから、この表示はしなくて良いか。。
                        //if (!shouldProcess && created != null)
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
            CancellationToken cancelToken, bool shouldProcess)
        {
            if (srcDrive.OrchAPISession.ApiVersion < 14) return;

            string target = srcFolder.GetPSPath();
            string msg = $"Copying API triggers";

            List<HttpTrigger> srcTriggers = null;
            try
            {
                srcTriggers = srcDrive.GetHttpTriggers(srcFolder).FilterByWildcards(t => t?.Name, wpName).ToList();
            }
            catch (Exception ex)
            {
                _this.WriteError(new ErrorRecord(new OrchException(target, msg, ex), "GetApiTriggerError", ErrorCategory.InvalidOperation, target));
                return;
            }

            reporter.TotalNum = srcTriggers.Count;
            target = newFolder.GetPSPath();

            int index = 0;
            foreach (var trigger in srcTriggers)
            {
                cancelToken.ThrowIfCancellationRequested();

                if (shouldProcess || _this.ShouldProcess($"Item: '{trigger.GetPSPath()}' Destination: '{newFolder.GetPSPath()}'", "Copy API Trigger"))
                {
                    msg = $"Copying API trigger {trigger.GetPSPath()}";
                    //reporter.WriteProgress(++index, $"{index:D}/{srcTriggers.Count} {trigger.Name}");
                    reporter.WriteProgress(++index, $"{index:D}/{srcTriggers.Count}");

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
                    if (postingTrigger!.ReleaseKey == null) continue;

                    if (postingTrigger.MachineRobots != null)
                    {
                        foreach (var machineRobot in postingTrigger.MachineRobots)
                        {
                            // RobotId を移行
                            Robot dstRobot = null;
                            if (machineRobot.RobotId != null && machineRobot.RobotId != 0)
                            {
                                dstRobot = FindDstRobot(_this,
                                    srcDrive,
                                    dstDrive, newFolder, machineRobot.RobotId, msg);
                                machineRobot.RobotId = dstRobot?.Id;
                            }

                            machineRobot.RobotUserName = null;

                            // MachineId を移行
                            MachineFolder dstMachineFolder = null;
                            if (machineRobot.MachineId != null && machineRobot.MachineId != 0)
                            {
                                dstMachineFolder = FindDstMachine(_this,
                                    srcDrive, srcFolder,
                                    dstDrive, newFolder, machineRobot.MachineId, msg);
                                machineRobot.MachineId = dstMachineFolder?.Id;
                            }

                            machineRobot.MachineName = null;

                            if (machineRobot.SessionId != null && machineRobot.SessionId != 0)
                            {
                                machineRobot.SessionId = FindDstSession(_this,
                                    srcDrive, srcFolder,
                                    dstDrive, newFolder, machineRobot.SessionId, msg)?.SessionId;
                            }
                        }
                    }

                    try
                    {
                        var created = dstDrive.OrchAPISession.CreateHttpTrigger(newFolder.Id ?? 0, postingTrigger);

                        // 画面が乱れるから、この表示はしなくて良いか。。
                        //if (!shouldProcess && created != null)
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
                if (srcLinkFolderIds == null || !srcLinkFolderIds.Any())
                {
                    return false;
                }

                dstLinkFolders = FindDstFolders(
                    srcLinkFolderIds,
                    srcDrive.GetFolders(),
                    dstDrive.GetFolders());

                if (dstLinkFolders == null || !dstLinkFolders.Any())
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
                    var buckets = dstDrive.GetBuckets(dstLinkFolder);
                    var dstBucket = buckets.FirstOrDefault(a => string.Compare(a.Name, bucket.Name, StringComparison.OrdinalIgnoreCase) == 0);
                    if (dstBucket == null)
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
            CancellationToken cancelToken, bool shouldProcess)
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
                srcBuckets = srcDrive.GetBuckets(srcFolder).FilterByWildcards(b => b?.Name, wpName).ToList();
            }
            catch (Exception ex)
            {
                _this.WriteError(new ErrorRecord(new OrchException(srcFolder.GetPSPath(), msg, ex), "GetBucketError", ErrorCategory.InvalidOperation, target));
                return;
            }

            reporter.TotalNum = srcBuckets.Count;
            target = newFolder.GetPSPath();

            int index = 0;
            foreach (var bucket in srcBuckets)
            {
                cancelToken.ThrowIfCancellationRequested();

                if (shouldProcess || _this.ShouldProcess(bucket.GetPSPath(), "Copy Bucket"))
                {
                    msg = $"Copying bucket {System.IO.Path.Combine(bucket.GetPSPath())}";
                    //reporter.WriteProgress(++index, $"{index:D}/{srcBuckets.Count} {bucket.Name}");
                    reporter.WriteProgress(++index, $"{index:D}/{srcBuckets.Count}");

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
                    postingBucket.Identifier = Guid.NewGuid();

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
                        //if (!shouldProcess && created != null)
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
            var srcTestCases = srcDrive.GetTestCases(srcFolder);
            var srcTestCase = srcTestCases.FirstOrDefault(ts => ts.Id == srcDefinitionId);
            if (srcTestCase == null)
            {
                _this.WriteError(new ErrorRecord(
                    new OrchException(srcDrive.NameColonSeparator,
                    $"{msg}: {srcFolder.GetPSPath()} does not have test case with Id = {srcDefinitionId}."), "CopyTestCaseError", ErrorCategory.InvalidOperation, dstDrive));
                return null;
            }

            var dstTestCases = dstDrive.GetTestCases(newFolder);
            var dstTestCase = dstTestCases.FirstOrDefault(tc => (tc.PackageIdentifier == srcTestCase.PackageIdentifier && tc.Name == srcTestCase.Name));
            if (dstTestCase == null)
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
            CancellationToken cancelToken, bool shouldProcess)
        {
            if (newFolder.FolderType == "Personal") return;
            if (srcDrive.OrchAPISession.ApiVersion < 14) return;

            // スクリプトで連続して cmdlet を実行することを考えると、
            // いちいちキャッシュをクリアするべきじゃなかった。。
            // 下記はコメントアウトしておく。

            // 最新の状態をコピーできるように、このフォルダーのキャッシュを削除する
            //srcDrive._dicTestSets?.TryRemove(srcFolder.Id ?? 0, out _);

            string msg = $"Copying test sets";

            try
            {
                var srcTestSets = srcDrive.GetTestSets(srcFolder).FilterByWildcards(b => b?.Name, wpName).ToList();
                reporter.TotalNum = srcTestSets.Count;

                int index = 0;
                foreach (var ts in srcTestSets)
                {
                    cancelToken.ThrowIfCancellationRequested();

                    string target = $"Item: '{ts.GetPSPath()}' Destination: '{newFolder.GetPSPath()}'";
                    if (shouldProcess || _this.ShouldProcess(target, "Copy TestSet"))
                    {
                        msg = $"Copying test set {ts.GetPSPath()}";
                        //reporter.WriteProgress(++index, $"{index:D}/{srcTestSets.Count} {testSetSchedule.Name}");
                        reporter.WriteProgress(++index, $"{index:D}/{srcTestSets.Count}");
                        try
                        {
                            var postingTestSet = srcDrive.OrchAPISession.GetTestSetForEdit(srcFolder.Id ?? 0, ts.Id ?? 0);

                            if (postingTestSet != null)
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
            if (srcDrive.OrchAPISession.ApiVersion < 14) return;

            ICollection<TestDataQueueItem> items;
            try
            {
                items = srcDrive.GetTestDataQueueItems(srcFolder, srcTestDataQueue);
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
            var srcTestSets = srcDrive.GetTestSets(srcFolder);
            var srcTestSet = srcTestSets.FirstOrDefault(ts => ts.Id == srcTestSetId);
            if (srcTestSet == null)
            {
                _this.WriteError(new ErrorRecord(
                    new OrchException(srcDrive.NameColonSeparator,
                    $"{msg}: {srcFolder.GetPSPath()} does not have test set with Id = {srcTestSetId}."), "CopyTestSetError", ErrorCategory.InvalidOperation, dstDrive));
                return null;
            }

            var dstTestSets = dstDrive.GetTestSets(newFolder);
            var dstTestSet = dstTestSets.FirstOrDefault(ts => (ts.Name == srcTestSet.Name));
            if (dstTestSet == null)
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
            CancellationToken cancelToken, bool shouldProcess)
        {
            if (newFolder.FolderType == "Personal") return;
            if (srcDrive.OrchAPISession.ApiVersion < 14) return;

            // スクリプトで連続して cmdlet を実行することを考えると、
            // いちいちキャッシュをクリアするべきじゃなかった。。
            // 下記はコメントアウトしておく。

            // 最新の状態をコピーできるように、このフォルダーのキャッシュを削除する
            //srcDrive._dicTestSetSchedules?.TryRemove(srcFolder.Id ?? 0, out _);

            string msg = $"Copying test schedules";

            List<TestSetSchedule> srcTestSetSchedules;
            try
            {
                srcTestSetSchedules = srcDrive.GetTestSetSchedules(srcFolder).FilterByWildcards(b => b?.Name, wpName).ToList();
            }
            catch (Exception ex)
            {
                _this.WriteError(new ErrorRecord(new OrchException(srcFolder.GetPSPath(), msg, ex), "GetTestSetScheduleError", ErrorCategory.InvalidOperation, srcFolder));
                return;
            }

            reporter.TotalNum = srcTestSetSchedules.Count;

            int index = 0;
            foreach (var testSetSchedule in srcTestSetSchedules)
            {
                cancelToken.ThrowIfCancellationRequested();

                string target = $"Item: '{testSetSchedule.GetPSPath()}' Destination: '{newFolder.GetPSPath()}'";
                if (shouldProcess || _this.ShouldProcess(target, "Copy TestSchedule"))
                {
                    msg = $"Copying test schedule {System.IO.Path.Combine(srcFolder.GetPSPath(), testSetSchedule.Name!)}";
                    //reporter.WriteProgress(++index, $"{index:D}/{srcTestSetSchedules.Count} {testSetSchedule.Name}");
                    reporter.WriteProgress(++index, $"{index:D}/{srcTestSetSchedules.Count}");

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
            CancellationToken cancelToken, bool shouldProcess)
        {
            if (newFolder.FolderType == "Personal") return;
            if (srcDrive.OrchAPISession.ApiVersion < 14) return;

            // スクリプトで連続して cmdlet を実行することを考えると、
            // いちいちキャッシュをクリアするべきじゃなかった。。
            // 下記はコメントアウトしておく。

            // 最新の状態をコピーできるように、このフォルダーのキャッシュを削除する
            //srcDrive._dicTestDataQueues?.TryRemove(srcFolder.Id ?? 0, out _);

            string msg = $"Copying test data queues";

            List<TestDataQueue> srcTestDataQueues;
            try
            {
                srcTestDataQueues = srcDrive.GetTestDataQueues(srcFolder).FilterByWildcards(b => b?.Name, wpName).ToList();
            }
            catch (Exception ex)
            {
                _this.WriteError(new ErrorRecord(new OrchException(srcFolder.GetPSPath(), msg, ex), "GetTestDataQueueError", ErrorCategory.InvalidOperation, srcFolder));
                return;
            }

            reporter.TotalNum = srcTestDataQueues.Count;

            int index = 0;
            foreach (var testDataQueue in srcTestDataQueues.Where(e => !e.IsDeleted.GetValueOrDefault()))
            {
                cancelToken.ThrowIfCancellationRequested();

                string target = $"Item: '{testDataQueue.GetPSPath()}' Destination: '{newFolder.GetPSPath()}'";
                if (shouldProcess || _this.ShouldProcess(target, "Copy TestDataQueue"))
                {
                    msg = $"Copying test data queue {System.IO.Path.Combine(srcFolder.GetPSPath(), testDataQueue.Name!)}";
                    //reporter.WriteProgress(++index, $"{index:D}/{srcTestDataQueues.Count} {testDataQueue.Name}");
                    reporter.WriteProgress(++index, $"{index:D}/{srcTestDataQueues.Count}");

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
            CancellationToken cancelToken, bool shouldProcess)
        {
            //if (srcDrive.OrchAPISession.ApiVersion < 14) return;

            string msg = $"Copying action catalogs";

            List<TaskCatalog> srcTaskCatalogs;
            try
            {
                srcTaskCatalogs = srcDrive.GetTaskCatalogs(srcFolder).FilterByWildcards(b => b?.Name, wpName).ToList();
            }
            catch (Exception ex)
            {
                _this.WriteError(new ErrorRecord(new OrchException(srcFolder.GetPSPath(), msg, ex), "GetActionCatalogError", ErrorCategory.InvalidOperation, srcFolder));
                return;
            }

            reporter.TotalNum = srcTaskCatalogs.Count;

            int index = 0;
            foreach (var srcTaskCatalog in srcTaskCatalogs)
            {
                cancelToken.ThrowIfCancellationRequested();

                string target = $"Item: '{srcTaskCatalog.GetPSPath()}' Destination: '{newFolder.GetPSPath()}'";
                if (shouldProcess || _this.ShouldProcess(target, "Copy ActionCatalog"))
                {
                    msg = $"Copying action catalog {System.IO.Path.Combine(srcFolder.GetPSPath(), srcTaskCatalog.Name!)}";
                    //reporter.WriteProgress(++index, $"{index:D}/{srcTestDataQueues.Count} {testDataQueue.Name}");
                    reporter.WriteProgress(++index, $"{index:D}/{srcTaskCatalogs.Count}");

                    var postingTaskCatalog = OrchCollectionExtensions.DeepCopy(srcTaskCatalog);
                    postingTaskCatalog.Id = null;
                    // postingTaskCatalog.Path = null; // JsonIgnore 属性がついているので不要
                    postingTaskCatalog.Key = null;
                    postingTaskCatalog.CreationTime = null;
                    postingTaskCatalog.FoldersCount = null;

                    if (postingTaskCatalog.RetentionBucketId != null)
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
                        dstDrive._dicTaskCatalog?.TryRemove(newFolder.Id ?? 0, out var _);
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
            if (dstCurrentUser == null) return;

            var srcFolderUsers = srcDrive.GetUsersForFolder(srcFolder, false);
            var srcMyself = srcFolderUsers?.FirstOrDefault(u => string.Compare(u.UserEntity!.UserName, dstCurrentUser.UserName, StringComparison.OrdinalIgnoreCase) == 0);
            if (srcMyself == null)
            {
                // コピー元フォルダーに自身は割り当てられていないので、コピー先から自身を剥がす
                try
                {
                    dstDrive.OrchAPISession.UnassignUserFromFolder(newFolder.Id ?? 0, dstCurrentUser.Id ?? 0);
                    dstDrive._dicUserRoles?.TryRemove((newFolder.Id ?? 0, true), out _);
                    dstDrive._dicUserRoles?.TryRemove((newFolder.Id ?? 0, false), out _);
                }
                catch { }
                return;
            }

            // コピー元フォルダーの自身に Folder Administrator ロールが割り当てられていなければ
            // コピー先フォルダーの自身から Folder Administrator ロールを剥がす
            bool srcIhaveFolderAdministratorRole = srcMyself.Roles?.Any(r => string.Compare(r.Name, "Folder Administrator", StringComparison.OrdinalIgnoreCase) == 0) ?? false;
            if (!srcIhaveFolderAdministratorRole)
            {
                var dstFolderUsers = dstDrive.GetUsersForFolder(newFolder, false);
                var dstMyself = dstFolderUsers?.FirstOrDefault(u => string.Compare(u.UserEntity!.UserName, dstCurrentUser.UserName, StringComparison.OrdinalIgnoreCase) == 0);
                if (dstMyself == null || dstMyself.Roles == null) return;

                var folderAdministratorRole = dstMyself.Roles?.FirstOrDefault(r => string.Compare(r.Name, "Folder Administrator", StringComparison.OrdinalIgnoreCase) == 0);
                if (folderAdministratorRole == null) return;

                dstMyself.Roles!.Remove(folderAdministratorRole);
                dstDrive.OrchAPISession.AssignUser(newFolder.Id ?? 0, dstMyself.Id ?? 0, dstMyself.Roles.Select(r => r.Id ?? 0)); ;
            }
        }

        private bool CopyItemRecurse(
            OrchDriveInfo srcDrive,
            Folder srcFolder,
            OrchDriveInfo dstDrive,
            Folder dstFolder,
            bool recurse)
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
                if (destinationWorkspace == null)
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
                // ここは複数形ではない
                string msg = $"Copying folder";

                // totalNum: folder itself, users, machines, packages, processes, assets, 
                // queues, triggers, API triggers, buckets, testsets, testschedules, testdataqueues
                int totalStageNum = 12;
                if (srcFolder.FolderType == "Perasonal") totalStageNum = 9;
                // Apps はコピーできるんだっけ？

                try
                {
                    // srcFolder がルート直下ではなく、かつ dstFolder がルートでないときは
                    // フィードを外してコピーする
                    string feedType;
                    if (srcFolder.ParentId != null && dstFolder != dstDrive.RootFolder)
                    {
                        feedType = "Processes";
                    }
                    else
                    {
                        feedType = srcFolder.FeedType;
                    }

                    Folder newFolder;
                    using ProgressReporter reporter = new(this, 1, totalStageNum, msg, msg);
                    using var cancelHandler = new ConsoleCancelHandler();
                    // ↑この親 reporter は、なるべくチカチカしない方が良いので、下↓のスコープには入れない。
                    // 次から始まるスコープ↓は、子供 reporter がタイムリーに消えるように導入したもの。
                    {
                        reporter.WriteProgress(0, $"\"{srcFolder.GetPSPath()}\" to \"{dstFolder.GetPSPath()}\"");

                        // #0 フォルダー自身をコピー
                        reporter.WriteProgress(0);
                        newFolder = CopyFolder(srcDrive, srcFolder, dstDrive, dstFolder, feedType!, cancelHandler.Token);
                        if (newFolder == null) return false;

                        srcDrive._dicReleases?.TryRemove(srcFolder.Id ?? 0, out _);
                        dstDrive._dicReleases?.TryRemove(dstFolder.Id ?? 0, out _);
                        dstDrive._dicMachinesAssigned?.TryRemove(dstFolder.Id ?? 0, out _);

                        if (!ExcludeEntities)
                        {
                            // #1 フォルダーユーザーをコピー
                            msg = "Copying assigned users...    ";
                            reporter.WriteProgress(1);
                            srcDrive!._dicUserRoles?.TryRemove((srcFolder.Id ?? 0, true), out var _);
                            srcDrive!._dicUserRoles?.TryRemove((srcFolder.Id ?? 0, false), out var _);
                            using var reporterFolderUsers = new ProgressReporter(this, 100, Int32.MaxValue, msg, msg);
                            CopyFolderUsers(this, srcDrive, srcFolder, null, dstDrive, newFolder, reporterFolderUsers, cancelHandler.Token, true);

                            // #2 フォルダーマシンをコピー
                            msg = "Copying assigned machines... ";
                            reporter.WriteProgress(2);
                            srcDrive._dicMachinesAssigned?.TryRemove(srcFolder.Id ?? 0, out _);
                            using var reporterFolderMachines = new ProgressReporter(this, 200, Int32.MaxValue, msg, msg);
                            CopyFolderMachines(this, srcDrive, srcFolder, null, dstDrive, newFolder, reporterFolderMachines, cancelHandler.Token, true);

                            // #3 バケットをコピー
                            // プロセスをコピーする前に、先にバケットをコピーしておく必要がある
                            msg = "Copying buckets...           ";
                            reporter.WriteProgress(3);
                            using var reporterBuckets = new ProgressReporter(this, 300, Int32.MaxValue, msg, msg);
                            CopyBuckets(this, srcDrive, srcFolder, null, dstDrive, newFolder, reporterBuckets, cancelHandler.Token, true);

                            // #4 フォルダーパッケージをコピー
                            msg = "Copying packages...          ";
                            reporter.WriteProgress(4);
                            using var reporterPackages = new ProgressReporter(this, 400, Int32.MaxValue, msg, msg);
                            CopyPackages(this, srcDrive, srcFolder, dstDrive, newFolder, reporterPackages, cancelHandler.Token);

                            // #5 プロセスをコピー
                            msg = "Copying processes...         ";
                            reporter.WriteProgress(5);
                            using var reporterProcesses = new ProgressReporter(this, 500, Int32.MaxValue, msg, msg);
                            CopyProcesses(this, srcDrive, srcFolder, null, dstDrive, newFolder, reporterProcesses, cancelHandler.Token, true);

                            // #6 アセットをコピー
                            msg = "Copying assets...            ";
                            reporter.WriteProgress(6);
                            srcDrive._dicAssets?.TryRemove(srcFolder.Id ?? 0, out _);
                            using var reporterAssets = new ProgressReporter(this, 600, Int32.MaxValue, msg, msg);
                            CopyAssets(this, srcDrive, srcFolder, null, dstDrive, newFolder, reporterAssets, cancelHandler.Token, true);

                            // #7 キューをコピー
                            msg = "Copying queues...            ";
                            reporter.WriteProgress(7);
                            using var reporterQueues = new ProgressReporter(this, 700, Int32.MaxValue, msg, msg);
                            CopyQueues(this, srcDrive, srcFolder, null, dstDrive, newFolder, reporterQueues, cancelHandler.Token, true);

                            // #8 トリガーをコピー
                            msg = "Copying triggers...          ";
                            reporter.WriteProgress(8);
                            srcDrive._dicProcessSchedules?.TryRemove(srcFolder.Id ?? 0, out _);
                            using var reporterTriggers = new ProgressReporter(this, 800, Int32.MaxValue, msg, msg);
                            CopyTriggers(this, srcDrive, srcFolder, null, dstDrive, newFolder, reporterTriggers, cancelHandler.Token, true);

                            // #8 APIトリガーをコピー
                            msg = "Copying API triggers...      ";
                            reporter.WriteProgress(9);
                            srcDrive._dicHttpTriggers?.TryRemove(srcFolder.Id ?? 0, out _);
                            using var reporterApiTriggers = new ProgressReporter(this, 900, Int32.MaxValue, msg, msg);
                            CopyApiTriggers(this, srcDrive, srcFolder, null, dstDrive, newFolder, reporterApiTriggers, cancelHandler.Token, true);

                            // #xx テストケースはコピーする必要がない。
                            // パッケージとプロセスをコピーすれば、自動で出てくる。
                            //msg = "Copying test cases...      ";
                            //reporter.WriteProgress();
                            //using var reporterTestCases = new ProgressReporter(this, 1100, Int32.MaxValue, msg, msg);
                            //CopyTestCases(this, srcDrive, srcFolder, dstDrive, newFolder, reporterTestCases);

                            // #10 テストセットをコピー
                            msg = "Copying test sets...         ";
                            reporter.WriteProgress(10);
                            using var reporterTestSets = new ProgressReporter(this, 1000, Int32.MaxValue, msg, msg);
                            CopyTestSets(this, srcDrive, srcFolder, null, dstDrive, newFolder, reporterTestSets, cancelHandler.Token, true);

                            // #11 テストセットスケジュールをコピー
                            msg = "Copying test schedules...    ";
                            reporter.WriteProgress(11);
                            using var reporterTestSchedules = new ProgressReporter(this, 1100, Int32.MaxValue, msg, msg);
                            CopyTestSetSchedules(this, srcDrive, srcFolder, null, dstDrive, newFolder, reporterTestSchedules, cancelHandler.Token, true);

                            // #12 テストデータキューをコピー
                            msg = "Copying test data queues...  ";
                            reporter.WriteProgress(12);
                            using var reporterTestDataQueues = new ProgressReporter(this, 1200, Int32.MaxValue, msg, msg);
                            CopyTestDataQueues(this, srcDrive, srcFolder, null, dstDrive, newFolder, reporterTestDataQueues, cancelHandler.Token, true);

                            // #13 アクションカタログをコピー
                            msg = "Copying action catalogs...  ";
                            reporter.WriteProgress(12);
                            using var reporterActionCatalogs = new ProgressReporter(this, 1300, Int32.MaxValue, msg, msg);
                            srcDrive._dicTaskCatalog?.TryRemove(srcFolder.Id ?? 0, out _);
                            CopyActionCatalogs(this, srcDrive, srcFolder, null, dstDrive, newFolder, reporterTestDataQueues, cancelHandler.Token, true);
                        }
                    }

                    if (recurse)
                    {
                        var subfolders = GetDirectChildFolders(srcDrive.GetFolders(), srcFolder);
                        foreach (var subfolder in subfolders)
                        {
                            CopyItemRecurse(srcDrive, subfolder, dstDrive, newFolder, true);
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

        protected override void CopyItem(string path, string copyPath, bool recurse)
        {
            var dynamicParameters = DynamicParameters as CopyItem_DynamicParameters;
            if (dynamicParameters != null && dynamicParameters.ExcludeEntities.IsPresent)
            {
                ExcludeEntities = true;
            }

            OrchDriveInfo srcDrive = ExtractOrchDriveInfo(path);
            OrchDriveInfo dstDrive = ExtractOrchDriveInfo(copyPath);

            srcDrive!.OrchAPISession.EnsureAuthenticated();
            dstDrive!.OrchAPISession.EnsureAuthenticated();

            if (srcDrive == null || dstDrive == null)
            {
                return;
            }

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
            if (srcFolder == null)
            {
                WriteError(new ErrorRecord(new OrchException(copyPath, $"{srcDrive.NameColon} does not have folder '{path}'."), "CopyFolderError", ErrorCategory.InvalidOperation, copyPath));
                return;
            }

            var dstFolder = dstDrive.GetFolder(OrchDriveInfo.PSPathToOrchPath(copyPath));
            if (dstFolder == null) // コピー先に指定されていたのは、存在しないフォルダー名
            {
                WriteError(new ErrorRecord(new OrchException(path, $"{dstDrive.NameColon} does not have folder '{copyPath}'."), "CopyFolderError", ErrorCategory.InvalidOperation, path));
                return;
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
                    var foldersToBeCopied = srcDrive.GetFolders().Where((f => f.ParentId == null && f != srcDrive.RootFolder));
                    foreach (var folderToBeCopied in foldersToBeCopied)
                    {
                        isDirty = CopyItemRecurse(srcDrive, folderToBeCopied, dstDrive, dstFolder ?? dstDrive.RootFolder!, true);
                    }
                }
                if (isDirty)
                {
                    dstDrive._dicFolders = null;
                }
                return;
            }

            bool bDirty = false;
            try
            {
                bDirty = CopyItemRecurse(srcDrive, srcFolder, dstDrive, dstFolder ?? dstDrive.RootFolder!, recurse);
            }
            catch (Exception)
            {
                // 例外が漏れた場合は、フォルダーが作成されたかされていないか分からない。。
                // ので、フォルダキャッシュをクリアしちゃう。
                dstDrive._dicFolders = null;
                throw;
            }
            finally
            {
                if (bDirty)
                {
                    dstDrive._dicFolders = null;
                }
            }
        }
    }
}
