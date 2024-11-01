using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Text.Json.Nodes;
using System.Threading.Tasks.Dataflow;
using UiPath.OrchAPI;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Entities.JsonConverter;
using UiPath.PowerShell.Positional;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;
using Job = UiPath.PowerShell.Entities.Job;
using License = UiPath.PowerShell.Entities.License;
using Path = System.IO.Path;
using Session = UiPath.PowerShell.Entities.Session;
using User = UiPath.PowerShell.Entities.User;

namespace UiPath.PowerShell.Core
{
    // OrchDriveInfo, OrchDuDriveInfo, OrchTmDriveInfo に共通のベースクラスを作成したいが
    // ちょっと大変。。いったん先延ばし。
    // 
    //public class OrchDriveInfoBase : PSDriveInfo
    //{
    //}

    public partial class OrchDriveInfo : PSDriveInfo
    {
        internal readonly ProxySettings? _globalProxy;
        internal readonly PSDrive _psDrive;

        private string? _NameColon = null;
        private string? _NameColonSeparator = null;

        internal string NameColon
        {
            get
            {
                _NameColon ??= Name + Path.VolumeSeparatorChar;
                return _NameColon;
            }
        }
        internal string NameColonSeparator
        {
            get
            {
                _NameColonSeparator ??= Name + Path.VolumeSeparatorChar + Path.DirectorySeparatorChar;
                return _NameColonSeparator;
            }
        }

        private OrchAPISession? _orchAPISession;
        private readonly object _orchAPISessionLock = new();
        internal OrchAPISession OrchAPISession
        {
            get
            {
                if (_orchAPISession == null)
                {
                    lock (_orchAPISessionLock)
                    {
                        _orchAPISession ??= new OrchAPISession(_psDrive, _globalProxy);
                    }
                }
                return _orchAPISession;
            }
        }

        // OrchFolderProvider の Start で初期化する
        internal static SessionState? SessionState;

        public static string GetTopParentPath(string orchPath)
        {
            int index = orchPath.IndexOf('/');
            if (index == -1)
            {
                return orchPath; // パスにスラッシュが含まれていない場合はそのまま返す
            }

            // スラッシュまでの部分文字列を取得
            return orchPath[..index];
        }

        public static string ExtractDriveName(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return "";
            }
            int colonIndex = path.IndexOf(Path.VolumeSeparatorChar);
            if (colonIndex != -1)
            {
                return path[..colonIndex];
            }
            else
            {
                return "";
            }
        }

        public static string PSPathToOrchPath(string path)
        {
            int colonIndex = path.IndexOf(Path.VolumeSeparatorChar);
            if (colonIndex != -1)
            {
                path = path[(colonIndex + 1)..];
            }
            //            string ret = WildcardPattern.Unescape(path.TrimStart('\\').TrimEnd('\\').Replace('\\', '/'));
            string ret = path
                .TrimStart(Path.DirectorySeparatorChar)
                .TrimEnd(Path.DirectorySeparatorChar)
                .Replace(Path.DirectorySeparatorChar, '/');
            return ret;
        }

        public static string OrchProviderPathToPSPath(string path)
        {
            return "\\" + path.Replace('/', '\\');//★★★
            //return "\\" + WildcardPattern.Escape(path.Replace('/', '\\'));//★★★
            //return path.Replace('/', '\\');
            //            return WildcardPattern.Escape("\\" + path.Replace('/', '\\'));
        }

        public static string MakeValidFolderName(string originalString)
        {
            string invalidChars = new string(Path.GetInvalidFileNameChars())
                                + new string(Path.GetInvalidPathChars());

            // 無効な文字を '_' に置換
            string validString = new(originalString
              .Select(ch => invalidChars.Contains(ch) ? '_' : ch)
              .ToArray());

            return validString;
        }

        public static IEnumerable<OrchDriveInfo> EnumAllOrchDrives()
        {
            return SessionState!.Drive.GetAllForProvider("UiPathOrch")
                .Cast<OrchDriveInfo>()
                .OrderBy(d => d.Name);
        }

        // paths を指定しない場合、カレントドライブのみを返す
        // T には OrchDriveInfo, OrchDuDriveInfo などを指定できる
        public static List<T> EnumOrchDrivesImpl<T>(IEnumerable<string?>? path = null) where T : PSDriveInfo
        {
            var drives = new List<T>();
            if (path == null || !path.Any() || path.All(p => p == null))
            {
                if (SessionState!.Path.CurrentLocation.Drive is T orchDrive)
                    drives.Add(orchDrive);
            }
            else
            {
                var psPaths = path.Select(p => SessionState!.Path.GetResolvedPSPathFromPSPath(p)).SelectMany(p => p);
                foreach (var p in psPaths)
                {
                    if (p.Drive is T orchDrive)
                        drives.Add(orchDrive);
                }
            }
            return drives.Distinct().ToList();
        }

        // paths を指定しない場合、カレントドライブのみを返す
        public static List<OrchDriveInfo> EnumOrchDrives(IEnumerable<string?>? path = null)
        {
            return EnumOrchDrivesImpl<OrchDriveInfo>(path);
        }

        // paths を指定しない場合、カレントドライブのみを返す
        public static List<OrchDuDriveInfo> EnumDuDrives(IEnumerable<string?>? path = null)
        {
            return EnumOrchDrivesImpl<OrchDuDriveInfo>(path);
        }

        public static OrchDriveInfo GetOrchDrive(string? path = null)
        {
            var srcDrives = EnumOrchDrives([path]);
            if (srcDrives.Count > 1)
            {
                throw new Exception($"'{path}' resolved to multiple containers.");
            }
            if (srcDrives.Count == 0)
            {
                // たぶん先に EnumOrchDrives() が例外を投げているはずなので、ここは実行されないと思う。
                throw new Exception($"Cannot find path '{path}' because it does not exist.");
            }
            return srcDrives[0];
        }

        // EnumOrchDrive と似ているけど、こちらはカレントドライブを考慮しない。
        // Destination を解決するのに使う。
        public static List<OrchDriveInfo> EnumDestinationDrives(IEnumerable<string> paths)
        {
            return paths
                .Select(p => SessionState!.Path.GetResolvedPSPathFromPSPath(p))
                .SelectMany(p => p)
                .Where(p => p.Drive is OrchDriveInfo orchDrive)
                .Select(p => p.Drive)
                .DistinctBy(p => p.Name)
                .Cast<OrchDriveInfo>()
                .ToList();
        }

        public static IEnumerable<PathInfo> ResolveOrchDrivePaths(IEnumerable<string?>? paths = null)
        {
            if (paths == null || !paths.Any() || paths.All(p => p == null))
            {
                PathInfo pathInfo = SessionState!.Path.CurrentLocation;
                if (pathInfo.Drive is OrchDriveInfo)
                {
                    yield return SessionState!.Path.CurrentLocation;
                }
            }
            else
            {
                var psPaths = paths.Where(p => p != null).Select(p => SessionState!.Path.GetResolvedPSPathFromPSPath(p)).SelectMany(p => p);
                foreach (var pathInfo in psPaths.Where(p => p.Provider.Name == "UiPathOrch"))
                {
                    yield return pathInfo;
                }
            }
        }

        // TODO: 引数に IWritableHost を追加して、パスをひとつずつ解釈するようにしたい。
        public static List<(OrchDriveInfo drive, Folder folder)> EnumFolders(IEnumerable<string?>? path, bool recurse = false, uint depth = 0, bool includeRoot = false)
        {
            var paths = ResolveOrchDrivePaths(path);

            List<(OrchDriveInfo drive, Folder folder)> ret = [];

            if (recurse && depth == 0)
                depth = uint.MaxValue;

            foreach (var p in paths)
            {
                OrchDriveInfo drive = p.Drive as OrchDriveInfo;
                if (drive == null) continue;

                var dicFolders = drive!.GetFolders(); // sorted by OrchDirectory and DisplayName
                if (dicFolders == null) continue;

                Folder folder = drive?.GetFolder(OrchDriveInfo.PSPathToOrchPath(WildcardPattern.Unescape(p.ProviderPath)));
                if (folder == null) continue;

                string orchPathStart;
                if (folder.FullyQualifiedName == "")
                    orchPathStart = "";
                else
                    orchPathStart = folder.FullyQualifiedName + "/";

                uint currentDepth = OrchProvider.FolderDepth(folder.FullyQualifiedName!);

                HashSet<string> visited = [];

                // dicFolders にはルートフォルダーが含まれないため、ルートだけ先にここで探して追加する
                if (includeRoot && currentDepth == 0)
                {
                    ret.Add((drive!, drive!.RootFolder!));
                }

                foreach (var p2 in dicFolders)
                {
                    if (!(p2.FullyQualifiedName! + "/").StartsWith(orchPathStart, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (!visited.Add(p2.GetPSPath())) continue;

                    uint folderDepth = OrchProvider.FolderDepth(p2.FullyQualifiedName!);
                    if (folderDepth - currentDepth <= depth)
                    {
                        ret.Add((drive!, p2));
                    }
                }
            }

            if (ret == null || ret.Count == 0)
            {
                throw new Exception("Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters.");
            }
            return ret.OrderBy(df => df.folder.FullyQualifiedNameOrderable).ToList();
        }

        // planned to be obsoleted
        // ResolveToSingleFolder() を使って書き直すべきだ。
        public static List<(OrchDriveInfo drive, Folder folder)> EnumFolders(string? path, bool recurse = false, uint depth = 0, bool includeRoot = false)
        {
            return EnumFolders([path], recurse, depth, includeRoot);
        }

        // TODO: これを一般化しなければ。★★★★
        public static (OrchDriveInfo drive, Folder folder) ResolveToSingleFolder(string? path)
        {
            var ret = EnumFolders(path, false, 0, true);
            switch (ret.Count)
            {
                case 0: throw new Exception($"Cannot find path '{path}'.");
                case 1: return ret[0];
                default: throw new Exception($"Path '{path}' resolved to multiple folders.");
            }
        }

        public static List<(OrchDriveInfo drive, Folder folder)> EnumFoldersWithoutPersonalWorkspace(IEnumerable<string>? path, bool recurse = false, uint depth = 0, bool includeRoot = false)
        {
            return EnumFolders(path, recurse, depth, includeRoot)
                .Where(df => df.folder.FolderType != "Personal").ToList();
        }

        public static IEnumerable<(string FullPath, string RelativePath)> ExpandLocalPath(string[]? localPaths, string wildcard, bool recurse = false, int depth = 0)
        {
            HashSet<string> uniquePath = [];

            if (localPaths == null)
            {
                EnumerationOptions option = new()
                {
                    RecurseSubdirectories = recurse,
                    MaxRecursionDepth = depth
                };
                string root = OrchDriveInfo.SessionState!.Path.CurrentFileSystemLocation.Path;
                foreach (var pathExpanded in Directory.EnumerateFiles(root, wildcard, option))
                {
                    if (uniquePath.Add(pathExpanded))
                    {
                        string relativePath = Path.GetRelativePath(root, System.IO.Path.GetDirectoryName(pathExpanded)!);
                        yield return (pathExpanded, relativePath);
                    }
                }
                yield break;
            }

            List<string> localPathExpanded1 = [];
            foreach (var localPath in localPaths)
            {
                Collection<PathInfo> resolvedPaths = OrchDriveInfo.SessionState!.Path.GetResolvedPSPathFromPSPath(localPath);
                localPathExpanded1.AddRange(resolvedPaths.Select(p => p.Path));
            }

            foreach (var localPath in localPathExpanded1)
            {
                if (Directory.Exists(localPath))
                {
                    EnumerationOptions option = new();
                    option.RecurseSubdirectories = recurse;
                    option.MaxRecursionDepth = depth;
                    string root = localPath;
                    foreach (var pathExpanded in Directory.EnumerateFiles(root, wildcard, option))
                    {
                        if (uniquePath.Add(pathExpanded))
                        {
                            string relativePath = Path.GetRelativePath(root, System.IO.Path.GetDirectoryName(pathExpanded)!);
                            yield return (pathExpanded, relativePath);
                        }
                    }
                }
                else
                {
                    if (uniquePath.Add(localPath))
                    {
                        yield return (localPath, "");
                    }
                }
            }
        }

        // TODO: このメソッドを参照する completer はすべてきれいに書き直せる
        public static List<(OrchDriveInfo drive, Folder folder)> EnumPackageFeedFolders(IEnumerable<string?>? path, bool recurse = false)
        {
            var paths = ResolveOrchDrivePaths(path);

            List<(OrchDriveInfo drive, Folder folder)> ret = [];

            HashSet<string> feedFolders = [];
            foreach (var p in paths)
            {
                OrchDriveInfo drive = p.Drive as OrchDriveInfo;
                if (drive == null) continue;

                Folder folder = drive?.GetFolder(OrchDriveInfo.PSPathToOrchPath(WildcardPattern.Unescape(p.ProviderPath)));
                if (folder == null) continue;

                string feedFolderPath = folder.GetPackageFeedFolder();
                Folder feedFolder = drive!.GetFolder(feedFolderPath);
                if (feedFolder == null) continue;

                if (!feedFolders.Add(feedFolder.GetPSPath()))
                {
                    continue;
                }

                ret.Add((drive!, feedFolder));

                // folder がルートディレクトリで、かつ recurse の場合に限り子フォルダーを列挙する
                if ((folder.ParentId == null && folder.FolderType != "Personal") && recurse)
                {
                    var folders = drive!.GetFolders(); // sorted by OrchDirectory and DisplayName
                    foreach (var subFolder in folders
                        .Where(f => f.ParentId == folder.Id)
                        .Where(f => f.FeedType == "FolderHierarchy"))
                    {
                        if (!feedFolders.Add(subFolder.GetPSPath()))
                        {
                            continue;
                        }
                        ret.Add((drive, subFolder));
                    }
                }
            }

            if (ret == null || ret.Count == 0)
            {
                throw new Exception("Use Set-Location (cd) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters.");
            }
            return ret;
        }

        //public static List<(OrchDriveInfo drive, Folder folder)> ResolveToSingleFeedFolder(string? path)
        public static (OrchDriveInfo drive, Folder folder) ResolveToSingleFeedFolder(string? path)
        {
            // まず、単一のフォルダに解決する
            var ret = EnumPackageFeedFolders([path], false);
            if (ret.Count == 0)
            {
                throw new Exception($"Cannot find path '{path}'.");
            }
            else if (ret.Count > 1)
            {
                throw new Exception($"Path '{path}' resolved to multiple folders.");
            }

            // recurse で、かつ解決したフォルダがルートフォルダであれば、サブフォルダも取得
            //if (recurse)
            //{
            //    var (drive, folder) = ret[0];
            //    if (folder == drive.RootFolder)
            //    {
            //        ret = EnumPackageFeedFolders([folder.GetPSPath()], true);
            //    }
            //}
            return ret.First();
        }

        protected internal Folder? RootFolder;

        // TODO: created folder cache sorted by FullyQualifiedName for GetFolder()
        // public HashSet<Folder> _folderSetCache;

        public void ClearAllCache()
        {
            #region Orchestrator API cache
            _dicFolders = null;

            _dicActivitySettings = null;
            _dicActivitySettings_Exception.ClearCache();

            _dicAssets = null;
            _dicAssets_Exceptions?.ClearCache();

            _dicAssetLinks = null;
            _dicAssetLinks_Exception.ClearCache();

            _dicAssignedMachines = null;
            _dicAssignedMachines_Exceptions.ClearCache();

            _dicAuditLogs = null;
            _dicAuditLogs_Exceptions.ClearCache();

            _dicAuthenticationSettings = null;
            _dicAuthenticationSettings_Exception.ClearCache();

            _dicAvailableVersions = null;
            _dicAvailableVersions_Exception.ClearCache();

            _dicBlobFiles = null;
            _dicBlobFiles_Exceptions?.ClearCache();

            _dicBuckets = null;
            _dicBuckets_Exceptions?.ClearCache();

            _dicBucketLinks = null;
            _dicBucketLinks_Exceptions?.ClearCache();

            _dicCalendars = null;
            _dicCalendars_Exceptions?.ClearCache();

            _dicConnectionString = null;
            _dicConnectionString_Exception.ClearCache();

            _dicCredentialStores = null;
            _dicCredentialStores_Exceptions?.ClearCache();

            _dicCurrentUser = null;
            _dicCurrentUser_Exception?.ClearCache();

            _dicEntitiesSummary = null;
            _dicEntitiesSummary_Exceptions?.ClearCache();

            _dicEnvironments = null;
            _dicEnvironments_Exceptions.ClearCache();

            _dicExtendedMachines = null;
            _dicExtendedMachines_Exception?.ClearCache();

            _dicFolderFeedIds = null; // これはクリアする必要ないけど、いちおうクリアしちゃうか。。

            _dicHttpTriggers = null;
            _dicHttpTriggers_Exceptions?.ClearCache();

            _dicJobs = null;

            _dicJobsHavingExecutionMedia = null;

            _dicLibraries = null;
            _dicLibraries_Exceptions?.ClearCache();

            _dicLibraryVersions = null; // 例外が発生するとしたら、_dicLibraries 取得時に発生しているはずだから、まあいいか。。

            _dicLicense = null;
            _dicLicense_Exception?.ClearCache();

            _dicLicenseNamedUser = null; // TODO: 例外キャッシュを追加

            _dicLicenseRuntime = null; // TODO: 例外キャッシュを追加

            _dicMachinesAssignable = null;
            _dicMachinesAssignable_Exceptions?.ClearCache();

            _dicMachinesAssigned = null;
            _dicMachinesAssigned_Exceptions?.ClearCache();

            _dicMachineClientSecrets = null;
            _dicMachineClientSecrets_Exception.ClearCache();

            _dicMachineSessionRuntimes = null;
            _dicMachineSessionRuntimes_Exception?.ClearCache();

            _dicMachineSessionRuntimesByFolder = null;
            _dicMachineSessionRuntimesByFolder_Exceptions?.ClearCache();

            _dicPackages = null;
            _dicPackages_Exceptions?.ClearCache();

            _dicPackageVersions = null; // 例外が発生するとしたら、_dicPackages 取得時に発生しているはずだから、まあいいか。。

            _dicPackageEntryPoint = null;
            _dicPackageEntryPoint_Exception?.ClearCache();

            _dicPartitionGlobalId = null;

            _dicPersonalWorkspaces = null;
            _dicPersonalWorkspaces_Exception?.ClearCache();

            _dicProcessSchedules = null;
            _dicProcessSchedules_Exceptions?.ClearCache();

            _dicProcessScheduleDetailed = null;
            _dicProcessScheduleDetailed_Exceptions.ClearCache();

            _dicQueueDefinitions = null;
            _dicQueueDefinitions_Exceptions?.ClearCache();

            _dicQueueLinks = null; // TODO: 例外キャッシュを追加
            _dicQueueItems = null;

            _dicReleaseList = null;
            _dicReleaseList_Exceptions?.ClearCache();

            _dicReleases = null;
            _dicReleases_Exceptions?.ClearCache();
                    
            _dicReleasesDetailed = null;
            _dicReleasesDetailed_Exceptions.ClearCache();

            _dicReviewer = null;
            _dicReviewer_Exceptions?.ClearCache();

            _dicRobots = null;
            _dicRobots_Exception?.ClearCache();

            _dicRobotsFromFolder = null;
            _dicRobotsFromFolder_Exceptions.ClearCache();

            _dicRobotLogs = null;

            _dicRoles = null;
            _dicRoles_Exception?.ClearCache();

            _dicSearchForUsersAndGroups = null;
            _dicSearchForUsersAndGroups_Exception.ClearCache();

            _dicSessions = null;
            _dicSessions_Exceptions.ClearCache();

            _dicSettings = null;
            _dicSettings_Exception?.ClearCache();

            _dicTaskCatalog = null;
            _dicTaskCatalog_Exceptions?.ClearCache();

            _dicTenantId = null;
            _dicTenantKey = null;

            _dicTestCases = null;
            _dicTestCases_Exceptions?.ClearCache();

            _dicTestCaseExecutions = null;
            _dicTestCaseExecutions_Exceptions.ClearCache();

            _dicTestDataQueues = null;
            _dicTestDataQueues_Exceptions?.ClearCache();

            _dicTestDataQueueItems = null;

            _dicTestSets = null;
            _dicTestSets_Exceptions?.ClearCache();

            _dicTestSetExecutions = null;
            _dicTestSetExecutions_Exceptions?.ClearCache();

            _dicTestSetSchedules = null;
            _dicTestSetSchedules_Exceptions?.ClearCache();

            _dicUpdateSettings = null;
            _dicUpdateSettings_Exception.ClearCache();

            _dicUserRoles = null;
            _dicUserRoles_Exceptions?.ClearCache();

            _dicUsers = null;
            _dicUsers_Exception?.ClearCache();

            _dicUsersDetailed = null;

            _dicWebhooks = null;
            _dicWebhooks_Exceptions?.ClearCache();

            _dicWebSettings = null;
            _dicWebSettings_Exception.ClearCache();
            #endregion

            #region Platform Management cache
            _dicPmAuditLogs = null;
            _dicPmAuditLogs_Exception.ClearCache();

            _dicPmAvailableUserBundles = null;
            _dicPmAvailableUserBundles_Exceptions.ClearCache();

            _dicPmBulkResolveByName = null;
            _dicPmBulkResolveByName_Exception.ClearCache();

            _dicPmDirectoryUsers = null;
            _dicPmDirectoryUsersException.ClearCache();

            _dicPmExternalApiResource = null;
            _dicPmExternalApiResource_Exception?.ClearCache();

            _dicPmExternalClient = null;
            _dicPmExternalClient_Exception?.ClearCache();

            _dicPmGroups = null;
            _dicPmGroups_Exception?.ClearCache();

            _dicPmRobotAccounts = null;
            _dicPmRobotAccounts_Exceptions?.ClearCache();

            _dicPmUsers = null;
            _dicPmUsers_Exception?.ClearCache();

            _dicPmLicensedUsers = null;
            _dicPmLicensedUsers_Exceptions.ClearCache();

            _dicPmLicensedGroups = null;
            _dicPmLicensedGroups_Exceptions.ClearCache();

            _dicPmUserLicenseGroupAllocations = null;
            _dicPmUserLicenseGroupAllocations_Exceptions.ClearCache();

            _dicPmAuthenticationSetting = null;
            _dicPmAuthenticationSetting_Exception.ClearCache();

            #endregion
        }

        // TODO: このメソッドの実装は不完全のはず。。
        public void ClearFolderCache(Folder? folder)
        {
            if (folder == null || folder.Id == null || folder.Id.Value == 0) return;
            Int64 folderId = folder.Id.Value;

            _dicAssets?.TryRemove(folderId, out _);
            _dicAssetLinks = null; // TODO: もっとかしこく必要な部分だけクリアしたいが、、面倒くさい
            _dicFolderFeedIds?.TryRemove(folderId, out _);
            _dicHttpTriggers?.TryRemove(folderId, out _);
            _dicJobs?.TryRemove(folderId, out _);
            _dicJobsHavingExecutionMedia?.TryRemove(folderId, out _);
            _dicMachinesAssignable?.TryRemove(folderId, out _);
            _dicMachinesAssigned?.TryRemove(folderId, out _);
            _dicProcessSchedules?.TryRemove(folderId, out _);
            _dicQueueDefinitions?.TryRemove(folderId, out _);
            _dicQueueLinks = null;
            _dicReleases?.TryRemove(folderId, out _);
            _dicReleaseList?.TryRemove(folderId, out _);
            _dicTestCases?.TryRemove(folderId, out _);
            _dicTestSets?.TryRemove(folderId, out _);
            _dicTestSetExecutions?.TryRemove(folderId, out _);
            _dicUserRoles?.TryRemove((folderId, false), out _);
            _dicUserRoles?.TryRemove((folderId, true), out _);

            _dicAssets_Exceptions?.ClearCache();
            _dicBuckets_Exceptions?.ClearCache();
            _dicEntitiesSummary_Exceptions?.ClearCache();
            _dicHttpTriggers_Exceptions?.ClearCache();
            _dicMachinesAssignable_Exceptions?.ClearCache();
            _dicMachinesAssigned_Exceptions?.ClearCache();
            _dicPackages_Exceptions?.ClearCache();
            _dicProcessSchedules_Exceptions?.ClearCache();
            _dicQueueDefinitions_Exceptions?.ClearCache();
            _dicReleaseList_Exceptions?.ClearCache();
            _dicReleases_Exceptions?.ClearCache();
            _dicTestCases_Exceptions?.ClearCache();
            _dicTestDataQueues_Exceptions?.ClearCache();
            _dicTestSetExecutions_Exceptions?.ClearCache();
            _dicTestSetSchedules_Exceptions?.ClearCache();
            _dicTestSets_Exceptions?.ClearCache();
            _dicUserRoles_Exceptions?.ClearCache();
        }

        //public void ClearFolderCacheRecurse(Folder folder)
        //{
        //    Int64? folderId = folder.Id;

        //    var subfolders = GetFolders().Where(f => f.ParentId == folderId);
        //    foreach (var subfolder in subfolders)
        //    {
        //        ClearFolderCacheRecurse(subfolder);
        //    }
        //    ClearFolderCache(folder);
        //}

        internal ConcurrentDictionary<Int64, AuditLog>? _dicAuditLogs = null;
        internal ExceptionCachePerTenant _dicAuditLogs_Exceptions = new();
        public ReadOnlyCollection<AuditLog> GetAuditLogs(string? query, ulong skip, ulong first)
        {
            _dicAuditLogs_Exceptions.ThrowCachedExceptionIfAny();

            if (_dicAuditLogs == null)
            {
                // うまく動いていたけど、concurrent な方が安全かな？
                // ConcurrentDictionary で書き直してしまえ。
                //_dicAuditLogs = new HashSet<AuditLog>(new EntityEqualityComparer<AuditLog, Int64>(log => log.Id ?? 0));
                lock (_dicAuditLogs_Exceptions)
                {
                    _dicAuditLogs ??= new();
                }
            }
            List<AuditLog> queriedLogs;
            try
            {
                queriedLogs = OrchAPISession.GetAuditLogs(query, skip, first).ToList();
            }
            catch (HttpResponseException ex)
            {
                _dicAuditLogs_Exceptions.CacheException(ex);
                throw;
            }
            foreach (var log in queriedLogs)
            {
                log.Path = NameColonSeparator;
                if (log.Entities != null && log.Entities.Length > 0)
                {
                    //string auditLogInfo = $"{Path.Combine(Name, log.Id?.ToString() ?? "")} {log.ExecutionTime?.ToLocalTime():yyyy/MM/dd HH:mm:ss} {log.OperationText}";
                    string pathId = log.GetPSPath();
                    foreach (var entity in log.Entities)
                    {
                        entity.Path = NameColonSeparator;
                        entity.PathId = pathId;
                        entity.CustomDataExpanded = JsonTools.JsonToDictionary(entity.CustomData);
                    }
                }

                // Details をキャッシュ取得済みであれば、付け替える
                if (_dicAuditLogs.TryGetValue(log.Id!.Value, out var cached))
                {
                    log.Details = cached?.Details;
                }

                _dicAuditLogs[log.Id!.Value] = log;
            }

            // 今回の問い合わせ結果のみを返す
            return queriedLogs.AsReadOnly();
        }

        // API call が発生したら true を返す
        public bool GetAuditLogDetails(AuditLog log)
        {
            if (log.Id == null || log.Details != null) return false;
            log.Details = OrchAPISession.GetAuditLogDetails(log.Id.Value)?.ToArray();
            foreach (var d in log.Details ?? [])
            {
                d.Path = NameColonSeparator;
                d.PathId  = NameColonSeparator + log.Id.ToString();
                d.CustomDataExpanded = JsonTools.JsonToDictionary(d.CustomData);
            }
            return true;
        }

        // Job は、一度の OrchAPI.GetJobs() 呼び出しですべてを取得できないため
        // 呼び出すたびに結果を追加する必要がある。そのため Dictionary<JobId, Job> を使う
        // Key: <folderId, <JobId, Job>>
        internal ConcurrentDictionary<Int64, Dictionary<Int64, Job>>? _dicJobs = null;
        public ReadOnlyCollection<Job> GetJobs(Folder folder, string? query = null, ulong skip = 0, ulong first = ulong.MaxValue, string? orderBy = null, bool orderAscending = false)
        {
            if (_dicJobs == null)
            {
                lock (this)
                {
                    _dicJobs ??= new ConcurrentDictionary<Int64, Dictionary<Int64, Job>>();
                }
            }
            if (!_dicJobs.TryGetValue(folder.Id ?? 0, out Dictionary<Int64, Job> folderJobs))
            {
                folderJobs = [];
                _dicJobs[folder.Id ?? 0] = folderJobs;
            }

            var jobs = OrchAPISession.GetJobs(folder.Id ?? 0, query, skip, first, orderBy, orderAscending).ToList();
            string folderPath = folder.GetPSPath();
            foreach (var job in jobs)
            {
                job.Path = folderPath;
                if (job.Id.HasValue)
                {
                    folderJobs![job.Id.Value] = job;
                }
            }
            return jobs.AsReadOnly();
        }

        public ReadOnlyCollection<Job> StartJobs(Folder folder, string processKey, string? runtimeType, int? jobsCount, string? inputArguments = null)
        {
            _dicJobs ??= new ConcurrentDictionary<Int64, Dictionary<Int64, Job>>();
            if (!_dicJobs.TryGetValue(folder.Id ?? 0, out Dictionary<Int64, Job> folderJobs))
            {
                folderJobs = [];
                _dicJobs[folder.Id ?? 0] = folderJobs;
            }

            var jobs = OrchAPISession.StartJobs(folder.Id ?? 0, processKey, runtimeType, jobsCount, inputArguments).ToList();
            foreach (var job in jobs)
            {
                job.Path = folder.GetPSPath();
                folderJobs![job.Id ?? 0] = job;
            }
            return jobs.AsReadOnly();
        }

        public Job? GetJob(Folder folder, Int64 jobId)
        {
            _dicJobs ??= new ConcurrentDictionary<Int64, Dictionary<Int64, Job>>();

            var job = OrchAPISession.GetJob(folder.Id ?? 0, jobId);
            if (job != null)
            {
                job.Path = folder.GetPSPath();
                if (!_dicJobs.TryGetValue(folder.Id ?? 0, out Dictionary<Int64, Job> folderJobs))
                {
                    folderJobs = [];
                    _dicJobs[folder.Id ?? 0] = folderJobs;
                }
                folderJobs[job.Id ?? 0] = job;
            }
            return job;
        }

        // key: folderId, hash
        // Log の Id は必ずゼロが返ってしまうので、うまくキャッシュすることができない
        // 仕方がないので、ハッシュ値で辞書を作成する。
        internal ConcurrentDictionary<Int64, HashSet<Log>>? _dicRobotLogs = null;
        public ReadOnlyCollection<Log> GetRobotLogs(Folder folder, string? query, ulong skip, ulong first, string? orderBy = null, bool orderAscending = false)
        {
            if (_dicRobotLogs == null)
            {
                lock(this)
                {
                    _dicRobotLogs = [];
                }
            }
            if (!_dicRobotLogs.TryGetValue(folder.Id ?? 0, out var folderLogs))
            {
                folderLogs = [];
                _dicRobotLogs[folder.Id ?? 0] = folderLogs;
            }

            // 必ず問い合わせる
            var logs = OrchAPISession.GetRobotLogs(folder.Id ?? 0, query, skip, first, orderBy, orderAscending).ToList();
            string folderPath = folder.GetPSPath();
            foreach (var log in logs)
            {
                log.Path = folderPath;
                folderLogs.Add(log);
                //if (log.Id.HasValue)
                //{
                //    //folderLogs[log.Id.Value] = log;
                //}
            }

            return logs.AsReadOnly();
        }

        // Key: <folderId, <JobId, ExecutionMedia>>
        internal ConcurrentDictionary<Int64, List<ExecutionMedia>>? _dicJobsHavingExecutionMedia = null;
        public ReadOnlyCollection<ExecutionMedia> GetExecutionMedia(Folder folder, ulong skip = 0, ulong first = ulong.MaxValue)
        {
            if (_dicJobsHavingExecutionMedia == null)
            {
                lock (this)
                {
                    _dicJobsHavingExecutionMedia ??= [];
                }
            }

            var result = OrchAPISession.GetExecutionMedia(folder.Id ?? 0, skip, first).ToList();
            _dicJobsHavingExecutionMedia[folder.Id ?? 0] = result;

            #region  この JobId がキャッシュされていない場合に限り、_dicJobs に入れておく
            _dicJobs ??= new ConcurrentDictionary<long, Dictionary<long, Job>>();

            if (!_dicJobs.TryGetValue(folder.Id ?? 0, out var jobs))
            {
                jobs = new Dictionary<long, Job>();
                _dicJobs[folder.Id ?? 0] = jobs;
            }

            foreach (var media in result)
            {
                if (media.JobId.HasValue && !jobs.ContainsKey(media.JobId.Value!))
                {
                    jobs[media.JobId.Value] = new Job
                    {
                        Id = media.JobId,
                        ReleaseName = media.ReleaseName,
                        HasMediaRecorded = true,
                    };
                }
            }
            #endregion

            string path = folder.GetPSPath();
            foreach (var media in result)
            {
                media.Path = path;
                //jobsHavingExecutionMedia[jobRecording.JobId ?? 0] = jobRecording;
            }

            return result.AsReadOnly();
        }

        #region OrchReleaseList cache
        // この API は非公開であり、古いバージョンの Orchestrator では動かない場合があるようだ。
        // そのため、しばらく封印する。
        // Key: folderId
        internal ConcurrentDictionary<Int64, List<Release>>? _dicReleaseList;
        internal ExceptionsCachePer<Int64> _dicReleaseList_Exceptions = new();
        //public ReadOnlyCollection<Release> ListReleases(Folder folder)
        //{
        //    _dicReleaseList_Exceptions.ThrowCachedExceptionIfAny(folder.Id ?? 0);

        //    if (_dicReleaseList == null)
        //    {
        //        lock (_dicReleaseList_Exceptions)
        //        {
        //            _dicReleaseList ??= new ConcurrentDictionary<Int64, List<Release>>();
        //        }
        //    }
        //    if (!_dicReleaseList.TryGetValue(folder.Id ?? 0, out List<Release> releases))
        //    {
        //        try
        //        {
        //            releases = OrchAPISession.ListReleases(folder.Id ?? 0).ToList();
        //            string path = folder.GetPSPath();
        //            foreach (var release in releases)
        //            {
        //                release.Path = path;
        //            }
        //            _dicReleaseList[folder.Id ?? 0] = releases;
        //        }
        //        catch (HttpResponseException ex)
        //        {
        //            _dicReleaseList_Exceptions.CacheException(folder.Id ?? 0, ex);
        //            throw;
        //        }
        //    }
        //    return releases.AsReadOnly();
        //}
        #endregion

        #region OrchUser cache
        // Key: userId
        internal ConcurrentDictionary<long, User>? _dicUsers = null;
        internal readonly ExceptionCachePerTenant _dicUsers_Exception = new();
        public ICollection<User> GetUsers()
        {
            _dicUsers_Exception.ThrowCachedExceptionIfAny();

            if (_dicUsers == null)
            {
                lock (_dicUsers_Exception)
                {
                    if (_dicUsers == null)
                    {
                        try
                        {
                            _dicUsers = new ConcurrentDictionary<long, User>(OrchAPISession.GetUsers().ToDictionary(u => u.Id ?? 0, u => u));

                            string driveName = NameColonSeparator;
                            foreach (var user in _dicUsers.Values)
                            {
                                user.Path = driveName;
                            }
                        }
                        catch (HttpResponseException ex)
                        {
                            _dicUsers_Exception.CacheException(ex);
                            throw;
                        }
                    }
                }
            }
            return _dicUsers.Values;
        }

        internal ConcurrentDictionary<long, User>? _dicUsersDetailed = null;
        public User? GetUser(User user)
        {
            if (_dicUsersDetailed == null)
            {
                lock (this)
                {
                    _dicUsersDetailed = [];
                }
            }

            if (_dicUsersDetailed.TryGetValue(user.Id!.Value, out var detailedUser))
            {
                return detailedUser;
            }
            detailedUser = OrchAPISession.GetUser(user.Id!.Value);
            if (detailedUser != null)
            {
                detailedUser.Path = NameColonSeparator;
                if (_dicUsers != null)
                {
                    _dicUsers[detailedUser.Id!.Value] = detailedUser;
                }
                _dicUsersDetailed[detailedUser.Id!.Value] = detailedUser;
            }
            return detailedUser;
        }
        #endregion

        #region OrchCurrentUser cache
        internal User? _dicCurrentUser = null;
        internal readonly ExceptionCachePerTenant _dicCurrentUser_Exception = new();
        public User? GetCurrentUser()
        {
            if (OrchAPISession.AuthManager.IsConfidentialApp)
            {
                throw new Exception("This operation is not supported in a Confidential app. Please switch to a Non-Confidential setting to connect your tenant with `Edit-OrchConfig` cmdlet.");
            }

            _dicCurrentUser_Exception.ThrowCachedExceptionIfAny();

            if (_dicCurrentUser == null)
            {
                try
                {
                    ExtendedUser exUser = OrchAPISession.GetCurrentUserExtended();
                    if (!string.IsNullOrEmpty(exUser?.AccountId))
                        _dicPartitionGlobalId = exUser?.AccountId; // 恐らく AC ではこっち。
                    else
                        _dicPartitionGlobalId = exUser?.TenantKey; // 恐らくオンプレではこっち。
                    _dicTenantId = exUser?.TenantId;
                    _dicTenantKey = exUser?.TenantKey;
                    if (exUser != null && exUser.PersonalWorkspace != null)
                    {
                        exUser.PersonalWorkspace.Path = NameColon;
                    }
                    _dicCurrentUser = exUser;
                }
                catch (Exception)
                {
                    try
                    {
                        _dicCurrentUser = OrchAPISession.GetCurrentUser();
                    }
                    catch (HttpResponseException ex)
                    {
                        _dicCurrentUser_Exception.CacheException(ex);
                        throw;
                    }
                }
                if (_dicCurrentUser != null)
                {
                    _dicCurrentUser.Path = NameColonSeparator;
                }
            }
            return _dicCurrentUser;
        }
        #endregion

        internal string? _dicPartitionGlobalId = null;
        internal object _dicPartitionGlobalIdLock = new();
        internal string? GetPartitionGlobalId()
        {
            if (_dicPartitionGlobalId != null) return _dicPartitionGlobalId;

            lock (_dicPartitionGlobalIdLock)
            {
                // ここを実行するときは、必ず機密アプリの場合になっている
                // 非機密アプリでは、ログイン時に GetCurrentUser() を実行しているはず。
                // ただし、OrchProvider より先に OrchDmProvider か OrchTmProvider でログインした場合には
                // GetCurrentUser() を実行していないので、非機密アプリになっている可能性もある。
                var users = GetUsers();
                foreach (var user in users)
                {
                    var detailedUser = OrchAPISession.GetUser(user.Id ?? 0);
                    _dicPartitionGlobalId = detailedUser?.AccountId ?? detailedUser?.TenantKey;
                    _dicTenantId = detailedUser?.TenantId;
                    _dicTenantKey = detailedUser?.TenantKey;
                    if (_dicPartitionGlobalId != null) break;
                }
            }
            return _dicPartitionGlobalId;
        }

        internal int? _dicTenantId = null;
        internal string? _dicTenantKey = null; // これは Guid なのか？ オンプレと AC で違うのか？
        internal object _dicTenantIdLock = new();
        internal (int? id, string? key) GetTenantId()
        {
            if (_dicTenantId != null) return (_dicTenantId, _dicTenantKey);

            lock (_dicPartitionGlobalIdLock)
            {
                // ここを実行するときは、必ず機密アプリの場合になっている
                // 非機密アプリでは、ログイン時に GetCurrentUser() を実行しているはず。
                try
                {
                    var users = GetUsers();
                    foreach (var user in users)
                    {
                        var detailedUser = OrchAPISession.GetUser(user.Id ?? 0);
                        _dicPartitionGlobalId = detailedUser?.AccountId ?? "";
                        _dicTenantId = detailedUser?.TenantId;
                        _dicTenantKey = detailedUser?.TenantKey;
                        if (detailedUser?.TenantId != null) break;
                    }
                }
                catch (Exception ex)
                {
                    throw new OrchException(NameColonSeparator, ex);
                }
            }
            return (_dicTenantId, _dicTenantKey);
        }

        #region GetAvailableVersions cache
        internal string[]? _dicAvailableVersions = null;
        internal readonly ExceptionCachePerTenant _dicAvailableVersions_Exception = new();
        public string[]? GetAvailableVersions()
        {
            _dicAvailableVersions_Exception.ThrowCachedExceptionIfAny();

            if (_dicAvailableVersions == null)
            {
                try
                {
                    var av = OrchAPISession.GetAvailableVersions();
                    _dicAvailableVersions = av?.availableVersions;
                }
                catch (HttpResponseException ex)
                {
                    _dicAvailableVersions_Exception.CacheException(ex);
                    throw;
                }
            }
            return _dicAvailableVersions;
        }
        #endregion

        #region PersonalWorkspace cache
        // no need to be multi-threaded
        internal List<PersonalWorkspace>? _dicPersonalWorkspaces = null;
        internal readonly ExceptionCachePerTenant _dicPersonalWorkspaces_Exception = new();
        public ReadOnlyCollection<PersonalWorkspace> GetPersonalWorkspaces()
        {
            _dicPersonalWorkspaces_Exception.ThrowCachedExceptionIfAny();

            if (_dicPersonalWorkspaces == null)
            {
                try
                {
                    _dicPersonalWorkspaces = OrchAPISession.GetPersonalWorkspaces().ToList();
                    foreach (var pw in _dicPersonalWorkspaces)
                    {
                        pw.Path = NameColonSeparator;
                    }
                }
                catch (HttpResponseException ex)
                {
                    _dicPersonalWorkspaces_Exception.CacheException(ex);
                    throw;
                }
            }
            return _dicPersonalWorkspaces.AsReadOnly();
        }

        internal List<PersonalWorkspace>? _dicPersonalWorkspacesExploringAvailable = null;

        public bool DisablePersonalWorkspace(long? userId)
        {
            if (userId == null) return false;

            try
            {
                var users = GetUsers();

                var owner = users.FirstOrDefault(o => o.Id == userId);
                if (owner != null)
                {
                    // ここはキャッシュをピンポイントで削除できるので、消しておくか。
                    // 頻繁に実行する処理ではないし、この方が安全だよね。。
                    _dicUsersDetailed?.TryRemove(owner.Id!.Value, out _);

                    var detailedOwner = GetUser(owner);

                    // もし GetUser() に失敗したら、このエンティティを安全に更新できないため、更新しない
                    if (detailedOwner == null) return false;

                    if (detailedOwner.MayHavePersonalWorkspace.GetValueOrDefault())
                    {
                        var postingUser = OrchCollectionExtensions.DeepCopy(detailedOwner);
                        if (postingUser.UnattendedRobot != null)
                        {
                            // サーバーから返る Password には "*****" が入っていたりする。
                            // 間違ってパスワードを "*****" で更新したりしないように、null を入れておく。
                            postingUser.UnattendedRobot.Password = null;
                        }
                        postingUser.MayHavePersonalWorkspace = false;
                        OrchAPISession.PutUser(postingUser);
                        _dicUsers = null;
                        _dicUsersDetailed = null;
                    }
                }
            }
            catch
            {
                return false; // この例外は握りつぶす
            }
            return true;
        }

        #endregion

        #region OrchEntitiesSummary cache
        // key: foldlerId
        internal ConcurrentDictionary<Int64, EntitiesSummary?>? _dicEntitiesSummary = null;
        internal ExceptionsCachePer<Int64> _dicEntitiesSummary_Exceptions = new();
        public EntitiesSummary? GetEntitiesSummary(Int64 folderId, string path)
        {
            _dicEntitiesSummary_Exceptions.ThrowCachedExceptionIfAny(folderId);

            if (_dicEntitiesSummary == null)
            {
                lock (_dicEntitiesSummary_Exceptions)
                {
                    _dicEntitiesSummary ??= new ConcurrentDictionary<Int64, EntitiesSummary?>();
                }
            }
            if (!_dicEntitiesSummary.TryGetValue(folderId, out EntitiesSummary summary))
            {
                try
                {
                    summary = OrchAPISession.GetEntitiesSummary(folderId);
                    if (summary != null)
                    {
                        summary.Path = path;
                    }
                    _dicEntitiesSummary[folderId] = summary;
                }
                catch (HttpResponseException ex)
                {
                    _dicEntitiesSummary_Exceptions.CacheException(folderId, ex);
                    throw;
                }
            }
            return summary;
        }

        #endregion

        #region OrchNamedUserLicense cache
        // key: RobotType
        internal ConcurrentDictionary<string, List<LicenseNamedUser>>? _dicLicenseNamedUser = null;
        public ReadOnlyCollection<LicenseNamedUser> GetLicenseNamedUser(string robotType)
        {
            if (_dicLicenseNamedUser == null)
            {
                lock (this)
                {
                    _dicLicenseNamedUser ??= new ConcurrentDictionary<string, List<LicenseNamedUser>>(StringComparer.OrdinalIgnoreCase);
                }
            }
            if (!_dicLicenseNamedUser.TryGetValue(robotType, out var ret))
            {
                ret = OrchAPISession.GetLicensesNamedUser(robotType).ToList();
                string pathRobotType = NameColonSeparator + robotType;
                foreach (var license in ret)
                {
                    license.RobotType = robotType;
                    license.Path = NameColonSeparator;
                    license.PathRobotType = pathRobotType;
                }
                _dicLicenseNamedUser[robotType] = ret;
            }
            return ret.AsReadOnly();
        }
        #endregion

        #region OrchRuntimeLicense cache
        // key: RobotType
        internal ConcurrentDictionary<string, List<LicenseRuntime>>? _dicLicenseRuntime = null;
        public ReadOnlyCollection<LicenseRuntime> GetLicenseRuntime(string robotType)
        {
            if (_dicLicenseRuntime == null)
            {
                lock (this)
                {
                    _dicLicenseRuntime ??= new ConcurrentDictionary<string, List<LicenseRuntime>>(StringComparer.OrdinalIgnoreCase);
                }
            }
            // LicenseRuntime はキャッシュせず、都度問い合わせる
//            if (!_dicRuntimeLicense.TryGetValue(robotType, out var ret))
//            {
                List<LicenseRuntime> ret = OrchAPISession.GetLicensesRuntime(robotType).ToList();
                foreach (var license in ret)
                {
                    license.RobotType = robotType;
                    license.Path = NameColonSeparator + robotType;
                }
                _dicLicenseRuntime[robotType] = ret;
 //           }
            return ret.AsReadOnly();
        }
        #endregion

        #region OrchProcessSchedule cache
        // key: foldlerId
        internal ConcurrentDictionary<Int64, ConcurrentDictionary<Int64, ProcessSchedule>>? _dicProcessSchedules = null;
        internal ExceptionsCachePer<Int64> _dicProcessSchedules_Exceptions = new();
        public ICollection<ProcessSchedule> GetProcessSchedules(Folder folder)
        {
            _dicProcessSchedules_Exceptions.ThrowCachedExceptionIfAny(folder.Id ?? 0);

            if (_dicProcessSchedules == null)
            {
                lock (_dicProcessSchedules_Exceptions)
                {
                    _dicProcessSchedules ??= [];
                }
            }
            if (!_dicProcessSchedules.TryGetValue(folder.Id ?? 0, out var triggersPerFolder))
            {
                triggersPerFolder = [];
                _dicProcessSchedules[folder.Id ?? 0] = triggersPerFolder;
                try
                {
                    var triggers = OrchAPISession.GetProcessSchedule(folder.Id ?? 0).ToList();
                    string folderPath = folder.GetPSPath();
                    foreach (var trigger in triggers)
                    {
                        trigger.Path = folderPath;
                        triggersPerFolder[trigger.Id!.Value] = trigger;
                    }
                }
                catch (HttpResponseException ex)
                {
                    _dicProcessSchedules_Exceptions.CacheException(folder.Id ?? 0, ex);
                    throw;
                }
            }
            return triggersPerFolder.Values;
        }
        #endregion

        internal ConcurrentDictionary<Int64, ConcurrentDictionary<Int64, ProcessSchedule>>? _dicProcessScheduleDetailed = null;
        internal ExceptionsCachePer<(Int64, Int64)> _dicProcessScheduleDetailed_Exceptions = new();
        public ProcessSchedule? GetProcessSchedule(Folder folder, ProcessSchedule schedule)
        {
            _dicProcessScheduleDetailed_Exceptions.ThrowCachedExceptionIfAny((folder.Id ?? 0, schedule.Id ?? 0));

            if (_dicProcessScheduleDetailed == null)
            {
                lock (_dicProcessScheduleDetailed_Exceptions)
                {
                    _dicProcessScheduleDetailed ??= [];
                }
            }

            if (!_dicProcessScheduleDetailed.TryGetValue(folder.Id ?? 0, out var detailedTriggersPerFolder))
            {
                lock (folder)
                {
                    if (!_dicProcessScheduleDetailed.TryGetValue(folder.Id ?? 0, out detailedTriggersPerFolder))
                    {
                        detailedTriggersPerFolder = [];
                        _dicProcessScheduleDetailed[folder.Id!.Value] = detailedTriggersPerFolder;
                    }
                }
            }

            if (!detailedTriggersPerFolder.TryGetValue(schedule.Id!.Value, out var detailedTrigger))
            {
                try
                {
                    detailedTrigger = OrchAPISession.GetProcessSchedule(folder.Id ?? 0, schedule.Id!.Value);
                    if (detailedTrigger != null)
                    {
                        detailedTrigger.Path = folder.GetPSPath();
                        detailedTriggersPerFolder[schedule.Id!.Value] = detailedTrigger;

                        if (_dicProcessSchedules != null && _dicProcessSchedules.TryGetValue(folder.Id!.Value, out var triggersPerFolder))
                        {
                            triggersPerFolder[schedule.Id!.Value] = detailedTrigger;
                        }
                    }
                }
                catch (HttpResponseException ex)
                {
                    _dicProcessScheduleDetailed_Exceptions.CacheException((folder.Id ?? 0, schedule.Id ?? 0), ex);
                    throw;
                }
            }
            return detailedTrigger;
        }

        #region OrchHttpTrigger cache
        internal ConcurrentDictionary<Int64, List<HttpTrigger>>? _dicHttpTriggers = null;
        internal ExceptionsCachePer<Int64> _dicHttpTriggers_Exceptions = new();
        private ReadOnlyCollection<HttpTrigger>? _dicHttpTriggersEmpty = null;
        public ReadOnlyCollection<HttpTrigger> GetHttpTriggers(Folder folder)
        {
            // TODO: 16 未満の数字は正しいか？ 15.0 では、取得がエラーになることは確認済みだが、
            if (OrchAPISession.ApiVersion < 16)
            {
                _dicHttpTriggersEmpty ??= new List<HttpTrigger>().AsReadOnly();
                return _dicHttpTriggersEmpty;
            }

            _dicHttpTriggers_Exceptions.ThrowCachedExceptionIfAny(folder.Id ?? 0);

            if (_dicHttpTriggers == null)
            {
                lock (_dicHttpTriggers_Exceptions)
                {
                    _dicHttpTriggers ??= new ConcurrentDictionary<Int64, List<HttpTrigger>>();
                }
            }
            if (!_dicHttpTriggers.TryGetValue(folder.Id ?? 0, out List<HttpTrigger> triggers))
            {
                try
                {
                    triggers = OrchAPISession.GetHttpTriggers(folder.Id ?? 0).ToList();
                    string folderPath = folder.GetPSPath();
                    foreach (var trigger in triggers)
                    {
                        trigger.Path = folderPath;
                    }
                    _dicHttpTriggers[folder.Id ?? 0] = triggers;
                }
                catch (HttpResponseException ex)
                {
                    _dicHttpTriggers_Exceptions.CacheException(folder.Id ?? 0, ex);
                    throw;
                }
            }
            return triggers.AsReadOnly();
        }

        #endregion

        #region OrchFolderUser cache
        // Key: folderId, includeInherited
        internal ConcurrentDictionary<(Int64, bool), List<UserRoles>>? _dicUserRoles = null;
        internal ExceptionsCachePer<Int64> _dicUserRoles_Exceptions = new();
        public ReadOnlyCollection<UserRoles> GetUsersForFolder(Folder folder, bool includeInherited)
        {
            _dicUserRoles_Exceptions.ThrowCachedExceptionIfAny(folder.Id ?? 0);

            if (_dicUserRoles == null)
            {
                lock (_dicUserRoles_Exceptions)
                {
                    _dicUserRoles ??= new ConcurrentDictionary<(Int64, bool), List<UserRoles>>();
                }
            }
            if (!_dicUserRoles.TryGetValue((folder.Id ?? 0, includeInherited), out List<UserRoles> userRoles))
            {
                try
                {
                    userRoles = OrchAPISession.GetUsersForFolder(folder.Id ?? 0, includeInherited).ToList();
                    string folderPath = folder.GetPSPath();
                    foreach (var userRole in userRoles)
                    {
                        userRole.Path = folderPath;
                    }
                    _dicUserRoles[(folder.Id ?? 0, includeInherited)] = userRoles;
                }
                catch (HttpResponseException ex)
                {
                    _dicUserRoles_Exceptions.CacheException(folder.Id ?? 0, ex);
                    throw;
                }
            }

            return userRoles.AsReadOnly();
        }
        #endregion

        #region OrchFolderMachine cache
        // Key: folderId
        internal ConcurrentDictionary<Int64, List<MachineFolder>>? _dicMachinesAssigned = null;
        internal ExceptionsCachePer<Int64> _dicMachinesAssigned_Exceptions = new();
        public ReadOnlyCollection<MachineFolder> GetMachinesAssignedToFolder(Folder folder)
        {
            _dicMachinesAssigned_Exceptions.ThrowCachedExceptionIfAny(folder.Id ?? 0);

            if (_dicMachinesAssigned == null)
            {
                lock (_dicMachinesAssigned_Exceptions)
                {
                    _dicMachinesAssigned ??= new ConcurrentDictionary<Int64, List<MachineFolder>>();
                }
            }
            if (!_dicMachinesAssigned.TryGetValue(folder.Id ?? 0, out List<MachineFolder> machines))
            {
                try
                {
                    string query = null;
                    if (OrchAPISession.ApiVersion >= 12)
                    {
                        query = "&$filter=((IsAssignedToFolder eq true) or (IsInherited eq true))";
                    }
                    else
                    {
                        query = "&$filter=(IsAssignedToFolder eq true)";
                    }
                    machines = OrchAPISession.GetMachinesAssignedTo(folder.Id ?? 0, query).ToList();
                    string folderPath = folder.GetPSPath();
                    foreach (var machine in machines)
                    {
                        machine.Path = folderPath;
                    }
                    _dicMachinesAssigned[folder.Id ?? 0] = machines;
                }
                catch (HttpResponseException ex)
                {
                    _dicMachinesAssigned_Exceptions.CacheException(folder.Id ?? 0, ex);
                    throw;
                }
            }
            return machines.AsReadOnly();
        }

        // Key: folderId
        internal ConcurrentDictionary<Int64, List<MachineFolder>>? _dicMachinesAssignable = null;
        internal ExceptionsCachePer<Int64> _dicMachinesAssignable_Exceptions = new();
        public ReadOnlyCollection<MachineFolder> GetMachinesAssignableToFolder(Folder folder)
        {
            _dicMachinesAssignable_Exceptions.ThrowCachedExceptionIfAny(folder.Id ?? 0);

            if (_dicMachinesAssignable == null)
            {
                lock (_dicMachinesAssignable_Exceptions)
                {
                    _dicMachinesAssignable ??= new ConcurrentDictionary<Int64, List<MachineFolder>>();
                }
            }
            if (!_dicMachinesAssignable.TryGetValue(folder.Id ?? 0, out List<MachineFolder> machines))
            {
                try
                {
                    machines = OrchAPISession.GetMachinesAssignableTo(folder.Id ?? 0).ToList();
                    string folderPath = folder.GetPSPath();
                    foreach (var machine in machines)
                    {
                        machine.Path = folderPath;
                    }
                    _dicMachinesAssignable[folder.Id ?? 0] = machines;
                }
                catch (HttpResponseException ex)
                {
                    _dicMachinesAssignable_Exceptions.CacheException(folder.Id ?? 0, ex);
                    throw;
                }
            }
            return machines.AsReadOnly();
        }
        #endregion

        internal ConcurrentDictionary<Int64, List<MachineFolder>>? _dicAssignedMachines = null;
        internal ExceptionsCachePer<Int64> _dicAssignedMachines_Exceptions = new();
        public ReadOnlyCollection<MachineFolder> GetAssignedMachines(Folder folder)
        {
            _dicAssignedMachines_Exceptions.ThrowCachedExceptionIfAny(folder.Id ?? 0);

            if (_dicAssignedMachines == null)
            {
                lock (_dicAssignedMachines_Exceptions)
                {
                    _dicAssignedMachines ??= new ConcurrentDictionary<Int64, List<MachineFolder>>();
                }
            }
            if (!_dicAssignedMachines.TryGetValue(folder.Id ?? 0, out List<MachineFolder> machines))
            {
                try
                {
                    machines = OrchAPISession.GetAssignedMachines(folder.Id ?? 0).ToList();
                    string folderPath = folder.GetPSPath();
                    foreach (var machine in machines)
                    {
                        machine.Path = folderPath;
                    }
                    _dicAssignedMachines[folder.Id ?? 0] = machines;
                }
                catch (HttpResponseException ex)
                {
                    _dicAssignedMachines_Exceptions.CacheException(folder.Id ?? 0, ex);
                    throw;
                }
            }
            return machines.AsReadOnly();
        }

        internal ConcurrentDictionary<Int64, List<UserRobots>>? _dicUserRobots = null;
        internal ExceptionsCachePer<Int64> _dicUserRobots_Exceptions = new();
        public ReadOnlyCollection<UserRobots> GetUserRobots(Folder folder)
        {
            _dicUserRobots_Exceptions.ThrowCachedExceptionIfAny(folder.Id ?? 0);

            if (_dicUserRobots == null)
            {
                lock (_dicUserRobots_Exceptions)
                {
                    _dicUserRobots ??= [];
                }
            }
            if (!_dicUserRobots.TryGetValue(folder.Id ?? 0, out var userRobots))
            {
                try
                {
                    userRobots = OrchAPISession.GetUserRobots(folder.Id ?? 0).ToList();
                    //string folderPath = folder.GetPSPath();
                    //foreach (var machine in machines)
                    //{
                    //    machine.Path = folderPath;
                    //}
                    _dicUserRobots[folder.Id ?? 0] = userRobots;
                }
                catch (HttpResponseException ex)
                {
                    _dicUserRobots_Exceptions.CacheException(folder.Id ?? 0, ex);
                    throw;
                }
            }
            return userRobots.AsReadOnly();
        }

        #region OrchAsset cache
        // Key: folderId
        internal ConcurrentDictionary<Int64, List<Asset>>? _dicAssets;
        internal ExceptionsCachePer<Int64> _dicAssets_Exceptions = new();
        public ReadOnlyCollection<Asset> GetAssets(Folder folder)
        {
            _dicAssets_Exceptions.ThrowCachedExceptionIfAny(folder.Id ?? 0);

            if (_dicAssets == null)
            {
                lock (_dicAssets_Exceptions)
                {
                    _dicAssets ??= new ConcurrentDictionary<Int64, List<Asset>>();
                }
            }

            if (!_dicAssets.TryGetValue(folder.Id ?? 0, out List<Asset> assets))
            {
                try
                {
                    assets = OrchAPISession.GetAssets(folder.Id ?? 0).ToList();
                    string folderPath = folder.GetPSPath();
                    foreach (var asset in assets)
                    {
                        asset.Path = folderPath;
                        if (asset.UserValues != null)
                        {
                            string assetPath = Path.Combine(folderPath, asset.Name!);
                            foreach (var userValue in asset.UserValues)
                            {
                                userValue.Name = asset.Name;
                                userValue.Path = folderPath;
                                userValue.PathName = assetPath;
                            }
                        }
                    }
                    _dicAssets[folder.Id ?? 0] = assets;
                }
                catch (HttpResponseException ex)
                {
                    _dicAssets_Exceptions.CacheException(folder.Id ?? 0, ex);
                    throw;
                }
            }
            return assets.AsReadOnly();
        }

        // key: (folderId, assetId)
        // assetId だけで一意になりそうだけど、念のため folderId もキーに含めておく。
        internal ConcurrentDictionary<(Int64 folderId, Int64 assetId), AccessibleFoldersDto?>? _dicAssetLinks = null;
        internal readonly ExceptionsCachePer<(Int64, Int64)> _dicAssetLinks_Exception = new();
        public AccessibleFoldersDto? GetFoldersForAsset(Folder folder, Asset asset)
        {
            _dicAssetLinks_Exception.ThrowCachedExceptionIfAny((folder.Id ?? 0, asset.Id ?? 0));

            if (_dicAssetLinks == null)
            {
                lock (_dicAssetLinks_Exception)
                {
                    _dicAssetLinks ??= new ConcurrentDictionary<(Int64 folderId, Int64 assetId), AccessibleFoldersDto?>();
                }
            }

            if (!_dicAssetLinks.TryGetValue((folder.Id ?? 0, asset.Id ?? 0), out var folderShare))
            {
                try
                {
                    folderShare = OrchAPISession.GetFoldersForAsset(folder.Id ?? 0, asset.Id ?? 0);

                    if (folderShare != null && folderShare.AccessibleFolders != null)
                    {
                        string folderPath = folder.GetPSPath();
                        foreach (var accessibleFolder in folderShare.AccessibleFolders)
                        {
                            accessibleFolder.Path = folderPath;
                            accessibleFolder.PathName = asset.GetPSPath();
                        }
                    }
                    _dicAssetLinks[(folder.Id ?? 0, asset.Id ?? 0)] = folderShare;
                }
                catch (HttpResponseException ex)
                {
                    _dicAssetLinks_Exception.CacheException((folder.Id ?? 0, asset.Id ?? 0), ex);
                    throw;
                }
            }
            return folderShare;
        }

        #endregion

        #region OrchCredentialStore cashe
        // no need to be multi-threaded
        internal List<CredentialStore>? _dicCredentialStores = null;
        internal readonly ExceptionCachePerTenant _dicCredentialStores_Exceptions = new();
        public ReadOnlyCollection<CredentialStore> GetCredentialStores()
        {
            _dicCredentialStores_Exceptions.ThrowCachedExceptionIfAny();

            if (_dicCredentialStores == null)
            {
                _dicCredentialStores = [];
                try
                {
                    var stores = OrchAPISession.GetCredentialStores();
                    Parallel.ForEach(stores, store =>
                    {
                        var credentialStore = OrchAPISession.GetCredentialStore(store.Id ?? 0);
                        if (credentialStore != null)
                        {
                            credentialStore.Path = NameColonSeparator;
                            _dicCredentialStores.Add(credentialStore);
                        }
                    });
                }
                catch (HttpResponseException ex)
                {
                    _dicCredentialStores_Exceptions.CacheException(ex);
                    throw;
                }
            }
            return _dicCredentialStores.AsReadOnly();
        }
        #endregion

        #region OrchWebhook cache
        // no need to be multi-threaded
        internal List<Webhook>? _dicWebhooks = null;
        internal readonly ExceptionCachePerTenant _dicWebhooks_Exceptions = new();
        public ReadOnlyCollection<Webhook> GetWebhooks()
        {
            _dicWebhooks_Exceptions.ThrowCachedExceptionIfAny();

            if (_dicWebhooks == null)
            {
                try
                {
                    _dicWebhooks = OrchAPISession.GetWebhooks().ToList();
                    foreach (var webhook in _dicWebhooks)
                    {
                        webhook.Path = NameColonSeparator;
                    }
                }
                catch (HttpResponseException ex)
                {
                    _dicWebhooks_Exceptions.CacheException(ex);
                    throw;
                }
            }
            return _dicWebhooks.AsReadOnly();
        }

        #endregion

        #region OrchProcess cache
        // Key: folderId, release.Id
        internal ConcurrentDictionary<Int64, Dictionary<Int64, Release>>? _dicReleases = null;
        internal ExceptionsCachePer<Int64> _dicReleases_Exceptions = new();
        public ICollection<Release> GetReleases(Folder folder)
        {
            _dicReleases_Exceptions.ThrowCachedExceptionIfAny(folder.Id ?? 0);

            if (_dicReleases == null)
            {
                lock (_dicReleases_Exceptions)
                {
                    _dicReleases ??= [];
                }
            }
            if (!_dicReleases.TryGetValue(folder.Id!.Value, out var releasesPerFolder))
            {
                lock (_dicReleases)
                {
                    releasesPerFolder ??= [];
                    _dicReleases[folder.Id.Value] = releasesPerFolder;
                }

                try
                {
                    string query = null;
                    if (OrchAPISession.ApiVersion >= 12)
                    {
                        query = "&$expand=Environment,CurrentVersion,ReleaseVersions,EntryPoint";
                    }
                    else
                    {
                        query = "&$expand=Environment,CurrentVersion,ReleaseVersions";
                    }
                    var releases = OrchAPISession.GetReleases(folder.Id.Value, query).ToList();
                    string folderPath = folder.GetPSPath();
                    foreach (var release in releases)
                    {
                        release.Path = folderPath;
                        releasesPerFolder[release.Id!.Value] = release;
                    }
                    return releases;
                }
                catch (HttpResponseException ex)
                {
                    _dicReleases_Exceptions.CacheException(folder.Id ?? 0, ex);
                    throw;
                }
            }
            return releasesPerFolder.Values;
        }

        internal ConcurrentDictionary<Int64, Dictionary<Int64, Release>>? _dicReleasesDetailed = null;
        internal ExceptionsCachePer<(Int64, Int64)> _dicReleasesDetailed_Exceptions = new();
        public Release? GetReleaseById(Folder folder, Int64 releaseId)
        {
            _dicReleasesDetailed_Exceptions.ThrowCachedExceptionIfAny((folder.Id!.Value, releaseId));

            if (_dicReleasesDetailed == null)
            {
                lock (_dicReleasesDetailed_Exceptions)
                {
                    _dicReleasesDetailed ??= [];
                }
            }
            if (!_dicReleasesDetailed.TryGetValue(folder.Id!.Value, out var releasesDetailedPerFolder))
            {
                lock (_dicReleasesDetailed)
                {
                    releasesDetailedPerFolder ??= [];
                    _dicReleasesDetailed[folder.Id!.Value] = releasesDetailedPerFolder;
                }
            }
            try
            {
                //var release = OrchAPISession.GetReleaseById(folder.Id ?? 0, releaseId, "?$expand=Environment,CurrentVersion,ReleaseVersions,EntryPoint");
                var release = OrchAPISession.GetReleaseById(folder.Id ?? 0, releaseId, "?$expand=ReleaseVersions,EntryPoint");
                if (release != null)
                {
                    release.Path = folder.GetPSPath();
                    releasesDetailedPerFolder[release.Id!.Value] = release;
                    if (_dicReleases != null && _dicReleases.TryGetValue(folder.Id!.Value, out var releasesPerFolder))
                    {
                        releasesPerFolder[release.Id.Value] = release;
                    }
                }
                return release;
            }
            catch (HttpResponseException ex)
            {
                _dicReleasesDetailed_Exceptions.CacheException((folder.Id.Value, releaseId), ex);
                throw;
            }
        }
        #endregion

        #region OrchLibrary cache
        // no need to be multi-threaded
        // Key: Id
        internal List<Library>? _dicLibraries = null;
        internal readonly ExceptionCachePerTenant _dicLibraries_Exceptions = new();
        public ReadOnlyCollection<Library> GetLibraries()
        {
            _dicRoles_Exception.ThrowCachedExceptionIfAny();

            if (_dicLibraries == null)
            {
                try
                {
                    _dicLibraries = OrchAPISession.GetLibraries().ToList();
                    string driveName = NameColonSeparator;
                    foreach (var library in _dicLibraries)
                    {
                        library.Path = driveName;
                    }
                }
                catch (HttpResponseException ex)
                {
                    _dicLibraries_Exceptions.CacheException(ex);
                    throw;
                }
            }
            return _dicLibraries.AsReadOnly();
        }

        // key: Id
        internal ConcurrentDictionary<string, List<LibraryVersion>>? _dicLibraryVersions = null;
        public ReadOnlyCollection<LibraryVersion> GetLibraryVersions(string libraryId)
        {
            if (_dicLibraryVersions == null)
            {
                lock (this)
                {
                    _dicLibraryVersions ??= new();
                }
            }

            if (!_dicLibraryVersions.TryGetValue(libraryId, out List<LibraryVersion> libraryVersions))
            {
                libraryVersions = OrchAPISession.GetLibraryVersions(libraryId)
                    .OrderBy(v => v.Version!, VersionComparer.Instance)
                    .ToList();
                string path = NameColonSeparator;
                foreach (var library in libraryVersions)
                {
                    //library.Name = $"{library.Id}.{library.Version}.nupkg";
                    library.Path = path;
                }
                _dicLibraryVersions ??= new();
                _dicLibraryVersions[libraryId] = libraryVersions;
            }
            return libraryVersions.AsReadOnly();
        }
        #endregion

        #region OrchPackage cache
        // Key: FeedId
        internal ConcurrentDictionary<string, List<Package>>? _dicPackages = null;
        internal ExceptionsCachePer<string> _dicPackages_Exceptions = new();
        public ReadOnlyCollection<Package> GetPackages(Folder folder)
        {
            string feedId = GetFolderFeedId(folder) ?? "";
            _dicPackages_Exceptions.ThrowCachedExceptionIfAny(feedId);

            if (_dicPackages == null)
            {
                lock (_dicPackages_Exceptions)
                {
                    _dicPackages ??= new ConcurrentDictionary<string, List<Package>>();
                }
            }

            string feedFolder = System.IO.Path.Combine(NameColonSeparator, folder.GetPackageFeedFolder());
            if (!_dicPackages.TryGetValue(feedId, out List<Package> packages))
            {
                try
                {
                    packages = OrchAPISession.GetPackages(feedId).ToList();
                    foreach (var package in packages)
                    {
                        package.Path = feedFolder;
                    }
                    _dicPackages[feedId] = packages;
                }
                catch (HttpResponseException ex)
                {
                    _dicPackages_Exceptions.CacheException(feedId, ex);
                    throw;
                }
            }
            return packages.AsReadOnly();
        }
        #endregion

        #region OrchPackageVersion cache
        // <Key: FeedId, <Key: Id, Package>>
        internal ConcurrentDictionary<string, ConcurrentDictionary<string, List<Package>>>? _dicPackageVersions = null;
        // processId に "id:version" を渡さないでね。それをするとキャッシュが壊れちゃう。
        public ReadOnlyCollection<Package> GetPackageVersions(Folder folder, string processId)
        {
            string feedId = GetFolderFeedId(folder) ?? "";
            _dicPackages_Exceptions.ThrowCachedExceptionIfAny(feedId);

            if (_dicPackageVersions == null)
            {
                lock (this)
                {
                    _dicPackageVersions ??= new ConcurrentDictionary<string, ConcurrentDictionary<string, List<Package>>>();
                }
            }
            if (!_dicPackageVersions.TryGetValue(feedId, out ConcurrentDictionary<string, List<Package>> packageVersionsByProcId))
            {
                packageVersionsByProcId = new ConcurrentDictionary<string, List<Package>>();
                _dicPackageVersions[feedId] = packageVersionsByProcId;
            }

            if (!packageVersionsByProcId.TryGetValue(processId, out List<Package> packageVersions))
            {
                try
                {
                    packageVersions = OrchAPISession.GetPackageVersions(feedId, processId)
                        .OrderBy(p => p.Version!, VersionComparer.Instance)
                        .ToList();
                    string folderPath = folder.GetPSPath();
                    foreach (var package in packageVersions)
                    {
                        package.Path = folderPath;
                    }
                    packageVersionsByProcId[processId] = packageVersions;
                }
                catch (HttpResponseException ex)
                {
                    _dicPackages_Exceptions.CacheException(feedId, ex);
                    throw;
                }
            }
            return packageVersions.AsReadOnly();
        }
        #endregion

        #region PackageEntryPoint cache
        // key: (feedId, packageId, version)
        internal ConcurrentDictionary<(string, string, string), List<PackageEntryPoint>>? _dicPackageEntryPoint = null;
        internal readonly ExceptionsCachePer<(string, string, string)> _dicPackageEntryPoint_Exception = new();
        public ReadOnlyCollection<PackageEntryPoint> GetPackageEntryPoints(string? feedId, string packageId, string version)
        {
            feedId ??= "";
            _dicPackageEntryPoint_Exception.ThrowCachedExceptionIfAny((feedId, packageId, version));

            if (_dicPackageEntryPoint == null)
            {
                lock (_dicPackageEntryPoint_Exception)
                {
                    _dicPackageEntryPoint ??= new();
                }
            }

            if (!_dicPackageEntryPoint.TryGetValue((feedId, packageId, version), out var entrypoints))
            {
                try
                {
                    entrypoints = OrchAPISession.GetPackageEntryPoints(feedId, packageId, version).ToList();
                    _dicPackageEntryPoint[(feedId, packageId, version)] = entrypoints;
                }
                catch (HttpResponseException ex)
                {
                    _dicPackageEntryPoint_Exception.CacheException((feedId, packageId, version), ex);
                    throw;
                }
            }
            return entrypoints.AsReadOnly();
        }
        #endregion


        #region OrchRole cache
        // no need to be multi-threaded
        internal List<Role>? _dicRoles = null;
        internal readonly ExceptionCachePerTenant _dicRoles_Exception = new();
        public ReadOnlyCollection<Role> GetRoles()
        {
            _dicRoles_Exception.ThrowCachedExceptionIfAny();

            if (_dicRoles == null)
            {
                try
                {
                    _dicRoles = OrchAPISession.GetRoles().ToList();
                    foreach (var role in _dicRoles)
                    {
                        role.Path = NameColonSeparator;
                    }
                }
                catch (HttpResponseException ex)
                {
                    _dicRoles_Exception.CacheException(ex);
                    throw;
                }
            }
            return _dicRoles.AsReadOnly();
        }
        #endregion

        #region OrchRobot cache
        // no need to be multi-threaded
        internal List<Robot>? _dicRobots = null;
        internal readonly ExceptionCachePerTenant _dicRobots_Exception = new();
        public ReadOnlyCollection<Robot> GetRobots()
        {
            _dicRobots_Exception.ThrowCachedExceptionIfAny();

            if (_dicRobots == null)
            {
                try
                {
                    _dicRobots = OrchAPISession.GetRobots().ToList();
                    foreach (var robot in _dicRobots)
                    {
                        robot.Path = NameColonSeparator;
                    }
                }
                catch (HttpResponseException ex)
                {
                    _dicRobots_Exception.CacheException(ex);
                    throw;
                }
            }
            return _dicRobots.AsReadOnly();
        }
        #endregion

        #region OrchMachine cashe
        // no need to be multi-threaded
        // Key: Machine.Name
        internal List<ExtendedMachine>? _dicExtendedMachines = null;
        internal readonly ExceptionCachePerTenant _dicExtendedMachines_Exception = new();
        public ReadOnlyCollection<ExtendedMachine> GetMachines()
        {
            _dicExtendedMachines_Exception.ThrowCachedExceptionIfAny();

            if (_dicExtendedMachines == null)
            {
                try
                {
                    string query = null;
                    if (OrchAPISession.ApiVersion >= 12)
                    {
                        query = "&$expand=UpdateInfo";
                    }
                    else
                    {
                        query = null;
                    }
                    _dicExtendedMachines = OrchAPISession.GetMachines(query).ToList();

                    foreach (var machine in _dicExtendedMachines)
                    {
                        machine.Path = NameColonSeparator;
                        if (machine.RobotUsers != null)
                        {
                            string pathMachine = machine.Path + machine.Name;
                            foreach (var robotUser in machine.RobotUsers)
                            {
                                robotUser.Path = machine.Path;
                                robotUser.Machine = machine.Name;
                                robotUser.PathMachine = pathMachine;
                            }
                        }
                    }
                }
                catch (HttpResponseException ex)
                {
                    _dicExtendedMachines_Exception.CacheException(ex);
                    throw;
                }
            }
            return _dicExtendedMachines.AsReadOnly();
        }
        #endregion

        #region OrchMachineClientSecret cache
        internal Dictionary<Guid, MachineClientSecretResponse[]?>? _dicMachineClientSecrets = null;
        internal readonly ExceptionsCachePer<Guid> _dicMachineClientSecrets_Exception = new();
        public MachineClientSecretResponse[]? GetMachineClientSecret(Guid licenseKey)
        {
            _dicMachineClientSecrets_Exception.ThrowCachedExceptionIfAny(licenseKey);

            if (_dicMachineClientSecrets == null || !_dicMachineClientSecrets.TryGetValue(licenseKey, out var secrets))
            {
                lock (_dicMachineClientSecrets_Exception)
                {
                    try
                    {
                        var secretValues = OrchAPISession.GetMachineClientSecret(licenseKey);
                        _dicMachineClientSecrets ??= [];
                        _dicMachineClientSecrets[licenseKey] = secretValues;
                        return secretValues;
                    }
                    catch (HttpResponseException ex)
                    {
                        _dicMachineClientSecrets_Exception.CacheException(licenseKey, ex);
                        throw;
                    }
                }
            }
            return secrets;
        }
        #endregion

        #region OrchQueue cache
        internal ConcurrentDictionary<Int64, List<QueueDefinition>>? _dicQueueDefinitions  = null;
        internal ExceptionsCachePer<Int64> _dicQueueDefinitions_Exceptions = new();
        public ReadOnlyCollection<QueueDefinition> GetQueues(Folder folder)
        {
            _dicQueueDefinitions_Exceptions.ThrowCachedExceptionIfAny(folder.Id ?? 0);

            if (_dicQueueDefinitions == null)
            {
                lock (_dicQueueDefinitions_Exceptions)
                {
                    _dicQueueDefinitions ??= new ConcurrentDictionary<Int64, List<QueueDefinition>>();
                }
            }
            if (!_dicQueueDefinitions.TryGetValue(folder.Id ?? 0, out List<QueueDefinition> queues))
            {
                try
                {
                    queues = OrchAPISession.GetQueues(folder.Id ?? 0).ToList();
                    string folderPath = folder.GetPSPath();
                    foreach (var queue in queues)
                    {
                        queue.Path = folderPath;
                    }
                    _dicQueueDefinitions[folder.Id ?? 0] = queues;
                }
                catch (HttpResponseException ex)
                {
                    _dicQueueDefinitions_Exceptions.CacheException(folder.Id ?? 0, ex);
                    throw;
                }
            }
            return queues.AsReadOnly();
        }

        // key: (folderId, assetId)
        // bucketId だけで一意になりそうだけど、念のため folderId もキーに含めておく。
        internal ConcurrentDictionary<(Int64 folderId, Int64 bucketId), AccessibleFoldersDto?>? _dicQueueLinks = null;
        public AccessibleFoldersDto? GetFoldersForQueue(Folder folder, QueueDefinition queue)
        {
            if (_dicQueueLinks == null)
            {
                lock (this)
                {
                    _dicQueueLinks ??= new ConcurrentDictionary<(Int64 folderId, Int64 queueId), AccessibleFoldersDto?>();
                }
            }

            if (!_dicQueueLinks.TryGetValue((folder.Id ?? 0, queue.Id ?? 0), out var folderShare))
            {
                folderShare = OrchAPISession.GetFoldersForQueue(folder.Id ?? 0, queue.Id ?? 0);

                if (folderShare != null && folderShare.AccessibleFolders != null)
                {
                    string folderPath = folder.GetPSPath();
                    foreach (var accessibleFolder in folderShare.AccessibleFolders)
                    {
                        accessibleFolder.Path = folderPath;
                        accessibleFolder.PathName = queue.GetPSPath();
                    }
                }
                _dicQueueLinks[(folder.Id ?? 0, queue.Id ?? 0)] = folderShare;
            }
            return folderShare;
        }

        #endregion

        #region RobotsFromFolder cache
        // key: folderId
        internal ConcurrentDictionary<Int64, List<RobotsFromFolderModel>>? _dicRobotsFromFolder = null;
        internal ExceptionsCachePer<Int64> _dicRobotsFromFolder_Exceptions = new();
        public ReadOnlyCollection<RobotsFromFolderModel> GetRobotsFromFolder(Folder folder)
        {
            _dicRobotsFromFolder_Exceptions.ThrowCachedExceptionIfAny(folder.Id ?? 0);

            if (_dicRobotsFromFolder == null)
            {
                lock (_dicRobotsFromFolder_Exceptions)
                {
                    _dicRobotsFromFolder ??= [];
                }
            }
            if (!_dicRobotsFromFolder.TryGetValue(folder.Id ?? 0, out var robots))
            {
                try
                {
                    robots = OrchAPISession.GetRobotsFromFolder(folder.Id!.Value).ToList();
                    if (robots != null)
                    {
                        string folderPath = folder.GetPSPath();
                        foreach (var robot in robots)
                        {
                            robot.Path = folderPath;
                        }
                        _dicRobotsFromFolder[folder.Id ?? 0] = robots;
                    }
                }
                catch (HttpResponseException ex)
                {
                    _dicRobotsFromFolder_Exceptions.CacheException(folder.Id ?? 0, ex);
                    throw;
                }
            }
            return robots?.AsReadOnly() ?? new List<RobotsFromFolderModel>().AsReadOnly();
        }
        #endregion

        #region Reviewer cache
        // key: folderId
        internal ConcurrentDictionary<Int64, List<SimpleUser>>? _dicReviewer = null;
        internal ExceptionsCachePer<Int64> _dicReviewer_Exceptions = new();
        public ReadOnlyCollection<SimpleUser> GetReviewers(Folder folder)
        {
            _dicReviewer_Exceptions.ThrowCachedExceptionIfAny(folder.Id ?? 0);

            if (_dicReviewer == null)
            {
                lock (_dicReviewer_Exceptions)
                {
                    _dicReviewer ??= [];
                }
            }
            if (!_dicReviewer.TryGetValue(folder.Id ?? 0, out var reviewers))
            {
                try
                {
                    reviewers = OrchAPISession.GetReviewers(folder.Id!.Value).ToList();
                    if (reviewers != null)
                    {
                        //string folderPath = folder.GetPSPath();
                        //foreach (var robot in reviewers)
                        //{
                        //    robot.Path = folderPath;
                        //}
                        _dicReviewer[folder.Id ?? 0] = reviewers;
                    }
                }
                catch (HttpResponseException ex)
                {
                    _dicReviewer_Exceptions.CacheException(folder.Id ?? 0, ex);
                    throw;
                }
            }
            return reviewers?.AsReadOnly() ?? new List<SimpleUser>().AsReadOnly();
        }
        #endregion

        #region OrchQueueItem cache
        // key: folderId, <queueName, <queueItemId>>
        internal ConcurrentDictionary<Int64, Dictionary<string, Dictionary<Int64, QueueItem>>>? _dicQueueItems = null;
        public ReadOnlyCollection<QueueItem> GetQueueItems(Folder folder, QueueDefinition queue, string filter, ulong skip, ulong first, string? orderBy = null, bool orderAscending = false)
        {
            if (_dicQueueItems == null)
            {
                lock (this)
                {
                    _dicQueueItems ??= [];
                }
            }
            if (!_dicQueueItems.TryGetValue(folder.Id!.Value, out var queueItemsPerFolder))
            {
                queueItemsPerFolder = [];
                _dicQueueItems[folder.Id!.Value] = queueItemsPerFolder;
            }

            if (!queueItemsPerFolder.TryGetValue(queue.Name!, out var queueItemsPerQueue))
            {
                queueItemsPerQueue = [];
                queueItemsPerFolder[queue.Name!] = queueItemsPerQueue;
            }

            var items = OrchAPISession.GetQueueItems(folder.Id!.Value, filter, skip, first, orderBy, orderAscending).ToList();
            foreach (var item in items)
            {
                item.Path = folder.GetPSPath();
                item.PathQueue = queue.GetPSPath();
                item.Queue = queue.Name;
                if (item.Id.HasValue)
                {
                    queueItemsPerQueue[item.Id.Value] = item;
                }
            }

            return items.AsReadOnly();
        }
        #endregion

        #region OrchTestSet cache
        // Key: folderId
        internal ConcurrentDictionary<Int64, List<TestSet>>? _dicTestSets = null;
        internal ExceptionsCachePer<Int64> _dicTestSets_Exceptions = new();
        private ReadOnlyCollection<TestSet>? _dicTestSetsEmpty = null;
        public ReadOnlyCollection<TestSet> GetTestSets(Folder folder)
        {
            // TODO: 16 未満の数字は正しいか？ 15.0 では、取得がエラーになることは確認済みだが、
            if (OrchAPISession.ApiVersion < 16)
            {
                _dicTestSetsEmpty ??= new List<TestSet>().AsReadOnly();
                return _dicTestSetsEmpty;
            }

            _dicTestSets_Exceptions.ThrowCachedExceptionIfAny(folder.Id ?? 0);

            if (_dicTestSets == null)
            {
                lock (_dicTestSets_Exceptions)
                {
                    _dicTestSets ??= new ConcurrentDictionary<Int64, List<TestSet>>();
                }
            }

            if (!_dicTestSets.TryGetValue(folder.Id ?? 0, out List<TestSet> testSets))
            {
                try
                {
                    testSets = OrchAPISession.GetTestSets(folder.Id ?? 0).ToList();
                    string folderPath = folder.GetPSPath();
                    foreach (var testSet in testSets)
                    {
                        testSet.Path = folderPath;
                    }
                    _dicTestSets[folder.Id ?? 0] = testSets;
                }
                catch (HttpResponseException ex)
                {
                    _dicTestSets_Exceptions.CacheException(folder.Id ?? 0, ex);
                    throw;
                }
            }
            return testSets.AsReadOnly();
        }

        #endregion

        #region OrchTestCase cache

        // Key: folderId
        internal ConcurrentDictionary<Int64, List<TestCaseDefinition>>? _dicTestCases = null;
        internal ExceptionsCachePer<Int64> _dicTestCases_Exceptions = new();
        private ReadOnlyCollection<TestCaseDefinition>? _dicTestCasesEmpty = null;
        public ReadOnlyCollection<TestCaseDefinition> GetTestCases(Folder folder)
        {
            // TODO: 16 未満の数字は正しいか？ 15.0 では、取得がエラーになることは確認済みだが、
            if (OrchAPISession.ApiVersion < 16)
            {
                _dicTestCasesEmpty ??= new List<TestCaseDefinition>().AsReadOnly();
                return _dicTestCasesEmpty;
            }

            _dicTestCases_Exceptions.ThrowCachedExceptionIfAny(folder.Id ?? 0);

            if (_dicTestCases == null)
            {
                lock (_dicTestCases_Exceptions)
                {
                    _dicTestCases ??= new ConcurrentDictionary<Int64, List<TestCaseDefinition>>();
                }
            }

            if (!_dicTestCases.TryGetValue(folder.Id ?? 0, out List<TestCaseDefinition> testCases))
            {
                try
                {
                    testCases = OrchAPISession.GetTestCases(folder.Id ?? 0).ToList();
                    string folderPath = folder.GetPSPath();
                    foreach (var testCase in testCases)
                    {
                        testCase.Path = folderPath;
                    }
                    _dicTestCases[folder.Id ?? 0] = testCases;
                }
                catch (HttpResponseException ex)
                {
                    _dicTestCases_Exceptions.CacheException(folder.Id ?? 0, ex);
                    throw;
                }
            }
            return testCases.AsReadOnly();
        }

        #endregion

        #region OrchTestCaseExecution cache

        // Key: folderId
        internal ConcurrentDictionary<Int64, List<TestCaseExecution>>? _dicTestCaseExecutions = null;
        internal ExceptionsCachePer<Int64> _dicTestCaseExecutions_Exceptions = new();
        private ReadOnlyCollection<TestCaseExecution>? _dicTestCaseExecutionsEmpty = null;
        public ReadOnlyCollection<TestCaseExecution> GetTestCaseExecutions(Folder folder)
        {
            // TODO: 16 未満の数字は正しいか？ 15.0 では、取得がエラーになることは確認済みだが、
            if (OrchAPISession.ApiVersion < 16)
            {
                _dicTestCaseExecutionsEmpty ??= new List<TestCaseExecution>().AsReadOnly();
                return _dicTestCaseExecutionsEmpty;
            }

            _dicTestCaseExecutions_Exceptions.ThrowCachedExceptionIfAny(folder.Id ?? 0);

            if (_dicTestCaseExecutions == null)
            {
                lock (_dicTestCaseExecutions_Exceptions)
                {
                    _dicTestCaseExecutions ??= new();
                }
            }

            if (!_dicTestCaseExecutions.TryGetValue(folder.Id ?? 0, out var executions))
            {
                try
                {
                    executions = OrchAPISession.GetTestCaseExecutions(folder.Id ?? 0).ToList();
                    string folderPath = folder.GetPSPath();
                    foreach (var execution in executions)
                    {
                        execution.Path = folderPath;
                    }
                    _dicTestCaseExecutions[folder.Id ?? 0] = executions;
                }
                catch (HttpResponseException ex)
                {
                    _dicTestCaseExecutions_Exceptions.CacheException(folder.Id ?? 0, ex);
                    throw;
                }
            }
            return executions.AsReadOnly();
        }

        #endregion

        #region OrchTestSetExecution cache
        // Key: <folderId, <TestSetExecutionId, TestSetExecution>>
        internal ConcurrentDictionary<Int64, Dictionary<Int64, TestSetExecution>>? _dicTestSetExecutions = null;
        internal ExceptionsCachePer<Int64> _dicTestSetExecutions_Exceptions = new();
        private ReadOnlyCollection<TestSetExecution>? _dicTestSetExecutionsEmpty = null;
        public ReadOnlyCollection<TestSetExecution> GetTestSetExecutions(Folder folder, string? query = null, ulong skip = 0, ulong first = ulong.MaxValue)
        {
            // TODO: 16 未満の数字は正しいか？ 15.0 では、取得がエラーになることは確認済みだが、
            if (OrchAPISession.ApiVersion < 16)
            {
                _dicTestSetExecutionsEmpty ??= new List<TestSetExecution>().AsReadOnly();
                return _dicTestSetExecutionsEmpty;
            }

            _dicTestSetExecutions_Exceptions.ThrowCachedExceptionIfAny(folder.Id ?? 0);

            if (_dicTestSetExecutions == null)
            {
                lock (_dicTestSetExecutions_Exceptions)
                {
                    _dicTestSetExecutions ??= new();
                }
            }

            if (!_dicTestSetExecutions.TryGetValue(folder.Id ?? 0, out var folderTestSetExecutions))
            {
                folderTestSetExecutions = [];
                _dicTestSetExecutions[folder.Id ?? 0] = folderTestSetExecutions;
            }
            try
            {
                var testSetExecutions = OrchAPISession.GetTestSetExecutions(folder.Id ?? 0, query, skip, first).ToList();
                string folderPath = folder.GetPSPath();
                foreach (var testSetExecution in testSetExecutions)
                {
                    testSetExecution.Path = folderPath;
                    folderTestSetExecutions![testSetExecution.Id ?? 0] = testSetExecution;
                }
                return testSetExecutions.AsReadOnly();
            }
            catch (HttpResponseException ex)
            {
                _dicTestSetExecutions_Exceptions.CacheException(folder.Id ?? 0, ex);
                throw;
            }
        }

        #endregion

        #region OrchTestSetSchedule cache

        // Key: folderId
        internal ConcurrentDictionary<Int64, List<TestSetSchedule>>? _dicTestSetSchedules = null;
        internal ExceptionsCachePer<Int64> _dicTestSetSchedules_Exceptions = new();
        private ReadOnlyCollection<TestSetSchedule>? _dicTestSetSchedulesEmpty = null;
        public ReadOnlyCollection<TestSetSchedule> GetTestSetSchedules(Folder folder)
        {
            // TODO: 16 未満の数字は正しいか？ 15.0 では、API トリガー取得がエラーになることは確認済みだが、
            if (OrchAPISession.ApiVersion < 16)
            {
                _dicTestSetSchedulesEmpty ??= new List<TestSetSchedule>().AsReadOnly();
                return _dicTestSetSchedulesEmpty;
            }

            _dicTestSetSchedules_Exceptions.ThrowCachedExceptionIfAny(folder.Id ?? 0);

            if (_dicTestSetSchedules == null)
            {
                lock (_dicTestSetSchedules_Exceptions)
                {
                    _dicTestSetSchedules ??= new();
                }
            }

            if (!_dicTestSetSchedules.TryGetValue(folder.Id ?? 0, out var testSetSchedules))
            {
                try
                {
                    testSetSchedules = OrchAPISession.GetTestSetSchedules(folder.Id ?? 0).ToList();
                    string folderPath = folder.GetPSPath();
                    foreach (var testSetSchedule in testSetSchedules)
                    {
                        testSetSchedule.Path = folderPath;
                    }
                    _dicTestSetSchedules[folder.Id ?? 0] = testSetSchedules;
                }
                catch (HttpResponseException ex)
                {
                    _dicTestSetSchedules_Exceptions.CacheException(folder.Id ?? 0, ex);
                    throw;
                }
            }
            return testSetSchedules.AsReadOnly();
        }

        #endregion

        #region OrchTestDataQueue cache
        // Key: folderId
        internal ConcurrentDictionary<Int64, List<TestDataQueue>>? _dicTestDataQueues = null;
        internal ExceptionsCachePer<Int64> _dicTestDataQueues_Exceptions = new();
        private ReadOnlyCollection<TestDataQueue>? _dicTestDataQueuesEmpty = null;
        public ReadOnlyCollection<TestDataQueue> GetTestDataQueues(Folder folder)
        {
            // TODO: 16 未満の数字は正しいか？ 15.0 では、API トリガー取得がエラーになることは確認済みだが、
            if (OrchAPISession.ApiVersion < 16)
            {
                _dicTestDataQueuesEmpty ??= new List<TestDataQueue>().AsReadOnly();
                return _dicTestDataQueuesEmpty;
            }

            _dicTestDataQueues_Exceptions.ThrowCachedExceptionIfAny(folder.Id ?? 0);

            if (_dicTestDataQueues == null)
            {
                lock (_dicTestDataQueues_Exceptions)
                {
                    _dicTestDataQueues ??= new();
                }
            }

            if (!_dicTestDataQueues.TryGetValue(folder.Id ?? 0, out var testQueues))
            {
                try
                {
                    testQueues = OrchAPISession.GetTestDataQueues(folder.Id ?? 0).ToList();
                    string folderPath = folder.GetPSPath();
                    foreach (var testDataQueue in testQueues)
                    {
                        testDataQueue.Path = folderPath;
                    }
                    _dicTestDataQueues[folder.Id ?? 0] = testQueues;
                }
                catch (HttpResponseException ex)
                {
                    _dicTestDataQueues_Exceptions.CacheException(folder.Id ?? 0, ex);
                    throw;
                }
            }
            return testQueues.AsReadOnly();
        }
        #endregion

        #region TestDataQueueItem
        // Key: folderId, testDataQueueId
        internal ConcurrentDictionary<Int64, Dictionary<Int64, List<TestDataQueueItem>>>? _dicTestDataQueueItems = null;
        public ReadOnlyCollection<TestDataQueueItem> GetTestDataQueueItems(Folder folder, TestDataQueue testDataQueue)
        {
            if (_dicTestDataQueueItems == null)
            {
                lock (this)
                {
                    _dicTestDataQueueItems ??= new();
                }
            }

            if (!_dicTestDataQueueItems.TryGetValue(folder.Id ?? 0, out var dicTestQueueItemsPerFolder))
            {
                dicTestQueueItemsPerFolder = [];
                _dicTestDataQueueItems[folder.Id ?? 0] = dicTestQueueItemsPerFolder;
            }

            if (!dicTestQueueItemsPerFolder.TryGetValue(testDataQueue.Id ?? 0, out var lstTestDataQueueItems))
            {
                lstTestDataQueueItems = OrchAPISession.GetTestDataQueueItems(folder.Id ?? 0, testDataQueue.Id ?? 0).ToList();
                foreach (var item in lstTestDataQueueItems)
                {
                    item.Path = NameColonSeparator;
                    item.PathTestDataQueue = testDataQueue.GetPSPath();
                }
                dicTestQueueItemsPerFolder[testDataQueue.Id ?? 0] = lstTestDataQueueItems;
            }

            return lstTestDataQueueItems.AsReadOnly();
        }
        #endregion

        #region OrchBucket cache

        // Key: folderId
        internal ConcurrentDictionary<Int64, List<Bucket>>? _dicBuckets = null;
        internal ExceptionsCachePer<Int64> _dicBuckets_Exceptions = new();
        public ReadOnlyCollection<Bucket> GetBuckets(Folder folder)
        {
            _dicBuckets_Exceptions.ThrowCachedExceptionIfAny(folder.Id ?? 0);

            if (_dicBuckets == null)
            {
                lock (_dicBuckets_Exceptions)
                {
                    _dicBuckets ??= new ConcurrentDictionary<Int64, List<Bucket>>();
                }
            }

            if (!_dicBuckets.TryGetValue(folder.Id ?? 0, out List<Bucket> buckets))
            {
                try
                {
                    buckets = OrchAPISession.GetBuckets(folder.Id ?? 0).ToList();
                    string folderPath = folder.GetPSPath();
                    foreach (var bucket in buckets)
                    {
                        bucket.Path = folderPath;
                    }
                    _dicBuckets[folder.Id ?? 0] = buckets;
                }
                catch (HttpResponseException ex)
                {
                    _dicBuckets_Exceptions.CacheException(folder.Id ?? 0, ex);
                    throw;
                }
            }
            return buckets.AsReadOnly();
        }

        // key: (folderId, bucketId)
        // bucketId だけで一意になりそうだけど、念のため folderId もキーに含めておく。
        internal ConcurrentDictionary<(Int64 folderId, Int64 bucketId), AccessibleFoldersDto?>? _dicBucketLinks = null;
        internal ExceptionsCachePer<(Int64, Int64)> _dicBucketLinks_Exceptions = new();
        public AccessibleFoldersDto? GetFoldersForBucket(Folder folder, Bucket bucket)
        {
            _dicBucketLinks_Exceptions.ThrowCachedExceptionIfAny((folder.Id ?? 0, bucket.Id ?? 0));

            if (_dicBucketLinks == null)
            {
                lock (_dicBucketLinks_Exceptions)
                {
                    _dicBucketLinks ??= new ConcurrentDictionary<(Int64 folderId, Int64 assetId), AccessibleFoldersDto?>();
                }
            }

            if (!_dicBucketLinks.TryGetValue((folder.Id ?? 0, bucket.Id ?? 0), out var folderShare))
            {
                try
                {
                    folderShare = OrchAPISession.GetFoldersForBucket(folder.Id ?? 0, bucket.Id ?? 0);

                    if (folderShare != null && folderShare.AccessibleFolders != null)
                    {
                        string folderPath = folder.GetPSPath();
                        foreach (var accessibleFolder in folderShare.AccessibleFolders)
                        {
                            accessibleFolder.Path = folderPath;
                            accessibleFolder.PathName = bucket.GetPSPath();
                        }
                    }
                    _dicBucketLinks[(folder.Id ?? 0, bucket.Id ?? 0)] = folderShare;
                }
                catch (HttpResponseException ex)
                {
                    _dicBucketLinks_Exceptions.CacheException((folder.Id ?? 0, bucket.Id ?? 0), ex);
                    throw;
                }
            }
            return folderShare;
        }

        // BlobFile は、ディレクトリもしくはフォルダを表す
        // key: folderId, <bucketId, BlobFiles>
        internal ConcurrentDictionary<Int64, ConcurrentDictionary<Int64, List<BlobFile>>>? _dicBlobFiles = null;
        // key: (folderId, bucketId)
        internal ExceptionsCachePer<(Int64, Int64)> _dicBlobFiles_Exceptions = new();
        public ReadOnlyCollection<BlobFile> GetBucketFiles(Folder folder, Bucket bucket)
        {
            _dicBlobFiles_Exceptions.ThrowCachedExceptionIfAny((folder.Id ?? 0, bucket.Id ?? 0));

            if (_dicBlobFiles == null)
            {
                lock (_dicBlobFiles_Exceptions)
                {
                    _dicBlobFiles ??= new();
                }
            }

            if (!_dicBlobFiles.TryGetValue(folder.Id ?? 0, out var blobFilesPerFolder))
            {
                blobFilesPerFolder = [];
                _dicBlobFiles[folder.Id ?? 0] = blobFilesPerFolder;
            }

            if (!blobFilesPerFolder.TryGetValue(bucket.Id ?? 0, out var blobItems))
            {
                try
                {
                    blobItems = OrchAPISession.GetBucketFiles(folder.Id ?? 0, bucket.Id ?? 0).ToList();
                    string folderPath = folder.GetPSPath();
                    string pathBucket = Path.Combine(folderPath, bucket.Name!);
                    foreach (var item in blobItems)
                    {
                        item.Path = folderPath;
                        item.Bucket = bucket.Name!;
                        item.PathBucket = pathBucket;
                    }

                    #region このフォルダを仮想的に追加
                    //blobItems.Insert(0, new BlobFile()
                    //{
                    //    Path = NameColonSeparator + folderPath,
                    //    IsDirectory = true
                    //});
                    #endregion

                    blobFilesPerFolder[bucket.Id ?? 0] = blobItems;
                }
                catch (HttpResponseException ex)
                {
                    _dicBlobFiles_Exceptions.CacheException((folder.Id ?? 0, bucket.Id ?? 0), ex);
                    throw;
                }
            }
            return blobItems.AsReadOnly();
        }

        #endregion

        #region OrchCalendar cache

        // Key: calenderId
        internal ConcurrentDictionary<long, ExtendedCalendar>? _dicCalendars = null;
        internal readonly ExceptionCachePerTenant _dicCalendars_Exceptions = new();
        public ICollection<ExtendedCalendar>? GetCalendars()
        {
            _dicCalendars_Exceptions.ThrowCachedExceptionIfAny();

            if (_dicCalendars == null)
            {
                lock (_dicCalendars_Exceptions)
                {
                    _dicCalendars ??= [];
                    try
                    {
                        var calendars = OrchAPISession.GetCalendars()?.ToList();
                        if (calendars != null)
                        {
                            foreach (var calendar in calendars)
                            {
                                calendar.Path = NameColonSeparator;
                                if (calendar.Id.HasValue)
                                {
                                    _dicCalendars[calendar.Id.Value] = calendar;
                                }
                            }
                        }
                    }
                    catch (HttpResponseException ex)
                    {
                        _dicCalendars_Exceptions.CacheException(ex);
                        throw;
                    }
                }
            }
            return _dicCalendars?.Values;
        }

        // ここでは例外をキャッシュしなくても十分な気がする
        //internal readonly ExceptionsCachePer<long> _dicCalendar_Exceptions = new();
        public ExtendedCalendar? GetCalendar(ExtendedCalendar calendar)
        {
            if (_dicCalendars == null)
            {
                lock (_dicCalendars_Exceptions) // dead lock を避けるため、同じオブジェクトでロックしないと。
                {
                    _dicCalendars ??= [];
                }
            }

            if (_dicCalendars.TryGetValue(calendar.Id!.Value, out var extendedCalendar))
            {
                if (calendar.ExcludedDates?.Length != 0) // もう取得済み・キャッシュ済みなのでこのまま返す
                    return calendar;
            }

            calendar = OrchAPISession.GetCalendar(calendar.Id.Value);
            if (calendar == null)
            {
                //_dicCalendars[calendarId] = null; // null をコレクションに入れると後が面倒なので、キャッシュはしないでおく。。
                return null;
            }
            calendar.Path = NameColonSeparator;
            _dicCalendars[calendar.Id!.Value] = calendar;
            return calendar;
        }

        #endregion

        // Key: folderId
        internal ConcurrentDictionary<Int64, List<Entities.Environment>>? _dicEnvironments = null;
        internal ExceptionsCachePer<Int64> _dicEnvironments_Exceptions = new();
        public ReadOnlyCollection<Entities.Environment> GetEnvironments(Folder folder)
        {
            _dicEnvironments_Exceptions.ThrowCachedExceptionIfAny(folder.Id ?? 0);

            if (_dicEnvironments == null)
            {
                lock (_dicEnvironments_Exceptions)
                {
                    _dicEnvironments ??= new();
                }
            }

            if (!_dicEnvironments.TryGetValue(folder.Id ?? 0, out var environments))
            {
                try
                {
                    environments = OrchAPISession.GetEnvironments(folder.Id ?? 0).ToList();
                    string folderPath = folder.GetPSPath();
                    foreach (var env in environments)
                    {
                        env.Path = folderPath;
                    }
                    _dicEnvironments[folder.Id ?? 0] = environments;
                }
                catch (HttpResponseException ex)
                {
                    _dicEnvironments_Exceptions.CacheException(folder.Id ?? 0, ex);
                    throw;
                }
            }
            return environments.AsReadOnly();
        }

        #region OrchSession cache
        // Key: folderId
        internal ConcurrentDictionary<Int64, List<Session>>? _dicSessions = null;
        internal readonly ExceptionsCachePer<Int64>_dicSessions_Exceptions = new();
        public ReadOnlyCollection<Session> GetSessions(Folder folder)
        {
            _dicSessions_Exceptions.ThrowCachedExceptionIfAny(folder.Id ?? 0);

            if (_dicSessions == null)
            {
                lock (_dicSessions_Exceptions)
                {
                    _dicSessions ??= new();
                }
            }
            if (!_dicSessions.TryGetValue(folder.Id ?? 0, out List<Session> sessions))
            {
                try
                {
                    sessions = OrchAPISession.GetSessions(folder.Id ?? 0).ToList();
                    string folderPath = folder.GetPSPath();
                    foreach (var session in sessions)
                    {
                        session.Path = folderPath;
                    }
                    _dicSessions[folder.Id ?? 0] = sessions;
                }
                catch (HttpResponseException ex)
                {
                    _dicSessions_Exceptions.CacheException(folder.Id ?? 0, ex);
                    throw;
                }
            }
            return sessions.AsReadOnly();
        }
        #endregion

        #region MachineSessionRuntimes cache
        // Key: folderId, SessionId
        internal List<MachineSessionRuntime>? _dicMachineSessionRuntimes = null;
        internal readonly ExceptionCachePerTenant _dicMachineSessionRuntimes_Exception = new();
        //public ReadOnlyCollection<MachineSessionRuntime> GetMachineSessionRuntimes(string? query = null, ulong skip = 0, ulong first = ulong.MaxValue)
        public ReadOnlyCollection<MachineSessionRuntime> GetMachineSessionRuntimes()
        {
            _dicMachineSessionRuntimes_Exception.ThrowCachedExceptionIfAny();

            if (_dicMachineSessionRuntimes == null)
            {
                try
                {
                    _dicMachineSessionRuntimes = OrchAPISession.GetMachineSessionRuntimes().ToList();
                    foreach (var session in _dicMachineSessionRuntimes)
                    {
                        session.Path = NameColonSeparator;
                    }
                }
                catch (HttpResponseException ex)
                {
                    _dicMachineSessionRuntimes_Exception.CacheException(ex);
                    throw;
                }
            }
            return _dicMachineSessionRuntimes.AsReadOnly();
        }
        #endregion

        #region MachineSessionRuntimeByFolder cache
        // Key: folderId, SessionId
        internal ConcurrentDictionary<Int64, List<MachineSessionRuntime>>? _dicMachineSessionRuntimesByFolder = null;
        internal readonly ExceptionsCachePer<Int64> _dicMachineSessionRuntimesByFolder_Exceptions = new();
        public ReadOnlyCollection<MachineSessionRuntime> GetMachineSessionRuntimesByFolderId(Folder folder, string? query = null, ulong skip = 0, ulong first = ulong.MaxValue)
        {
            _dicMachineSessionRuntimesByFolder_Exceptions.ThrowCachedExceptionIfAny(folder.Id ?? 0);

            if (_dicMachineSessionRuntimesByFolder == null)
            {
                lock (_dicMachineSessionRuntimesByFolder_Exceptions)
                {
                    _dicMachineSessionRuntimesByFolder ??= new();
                }
            }
            if (!_dicMachineSessionRuntimesByFolder.TryGetValue(folder.Id ?? 0, out var sessions))
            {
                try
                {
                    sessions = OrchAPISession.GetMachineSessionRuntimesByFolderId(folder.Id ?? 0, query, skip, first).ToList();
                    string folderPath = folder.GetPSPath();
                    foreach (var session in sessions)
                    {
                        session.Path = folderPath;
                    }
                    _dicMachineSessionRuntimesByFolder[folder.Id ?? 0] = sessions;
                }
                catch (HttpResponseException ex)
                {
                    _dicMachineSessionRuntimesByFolder_Exceptions.CacheException(folder.Id ?? 0, ex);
                    throw;
                }
            }
            return sessions.AsReadOnly();
        }
        #endregion

        #region OrchSetting Cache
        internal List<Settings>? _dicSettings = null;
        internal readonly ExceptionCachePerTenant _dicSettings_Exception = new();
        public List<Settings>? GetSettings()
        {
            _dicSettings_Exception.ThrowCachedExceptionIfAny();

            if (_dicSettings == null)
            {
                lock (_dicSettings_Exception)
                {
                    if (_dicSettings == null)
                    {
                        try
                        {
                            _dicSettings = OrchAPISession.GetSettings().ToList();
                            foreach (var setting in _dicSettings)
                            {
                                setting.Path = NameColonSeparator;
                            }
                        }
                        catch (HttpResponseException ex)
                        {
                            _dicSettings_Exception.CacheException(ex);
                            throw;
                        }
                    }
                }
            }
            return _dicSettings;
        }
        #endregion

        #region OrchWebSetting Cache
        internal List<ResponseDictionaryItem>? _dicWebSettings = null;
        internal readonly ExceptionCachePerTenant _dicWebSettings_Exception = new();
        public List<ResponseDictionaryItem>? GetWebSettings()
        {
            _dicWebSettings_Exception.ThrowCachedExceptionIfAny();

            if (_dicWebSettings == null)
            {
                lock (_dicWebSettings_Exception)
                {
                    if (_dicWebSettings == null)
                    {
                        try
                        {
                            var dic = OrchAPISession.GetWebSettings();
                            _dicWebSettings = dic?.Keys?.Zip(dic.Values ?? [], (key, value) => new ResponseDictionaryItem()
                            {
                                Path = NameColonSeparator,
                                Key = key,
                                Value = value
                            }).ToList();
                        }
                        catch (HttpResponseException ex)
                        {
                            _dicWebSettings_Exception.CacheException(ex);
                            throw;
                        }
                    }
                }
            }
            return _dicWebSettings;
        }
        #endregion

        #region OrchUpdateSettings Cache
        internal UpdateSettings? _dicUpdateSettings = null;
        internal readonly ExceptionCachePerTenant _dicUpdateSettings_Exception = new();
        public UpdateSettings? GetUpdateSettings()
        {
            _dicUpdateSettings_Exception.ThrowCachedExceptionIfAny();

            if (_dicUpdateSettings == null)
            {
                lock (_dicUpdateSettings_Exception)
                {
                    if (_dicUpdateSettings == null)
                    {
                        try
                        {
                            _dicUpdateSettings = OrchAPISession.GetUpdateSettings();
                            if (_dicUpdateSettings != null)
                            {
                                _dicUpdateSettings.Path = NameColonSeparator;
                            }
                        }
                        catch (HttpResponseException ex)
                        {
                            _dicUpdateSettings_Exception.CacheException(ex);
                            throw;
                        }
                    }
                }
            }
            return _dicUpdateSettings;
        }
        #endregion

        #region OrchExecutionSettings Cache
        // key: scope
        internal Dictionary<int, ExecutionSettingDefinition[]?>? _dicExecutionSettings = null;
        internal readonly ExceptionsCachePer<int> _dicExecutionSettingsException = new();
        public ExecutionSettingDefinition[]? GetExecutionSettings(int scope, string strScope)
        {
            _dicExecutionSettingsException.ThrowCachedExceptionIfAny(scope);

            if (_dicExecutionSettings == null)
            {
                lock (_dicExecutionSettingsException)
                {
                    _dicExecutionSettings ??= [];
                }
            }

            if (!_dicExecutionSettings.TryGetValue(scope, out var ret))
            {
                try
                {
                    var executionConf = OrchAPISession.GetExecutionSettings(scope);
                    if (executionConf != null)
                    {
                        foreach (var setting in executionConf.Configuration ?? [])
                        {
                            setting.Path = NameColonSeparator;
                            setting.PathScope = Path.Combine(NameColonSeparator, strScope);
                            setting.Scope = strScope;
                        }
                        ret = executionConf.Configuration;
                        _dicExecutionSettings[scope] = ret;
                    }
                }
                catch (HttpResponseException ex)
                {
                    _dicExecutionSettingsException.CacheException(scope, ex);
                    throw;
                }
            }
            return ret;
        }
        #endregion

        #region OrchActivitySetting Cache
        internal ActivitySettings? _dicActivitySettings = null;
        internal readonly ExceptionCachePerTenant _dicActivitySettings_Exception = new();
        public ActivitySettings? GetActivitySettings()
        {
            _dicActivitySettings_Exception.ThrowCachedExceptionIfAny();

            if (_dicActivitySettings == null)
            {
                lock (_dicActivitySettings_Exception)
                {
                    if (_dicActivitySettings == null)
                    {
                        try
                        {
                            _dicActivitySettings = OrchAPISession.GetActivitySettings();
                            if (_dicActivitySettings != null)
                            {
                                _dicActivitySettings.Path = NameColonSeparator;
                            }
                        }
                        catch (HttpResponseException ex)
                        {
                            _dicActivitySettings_Exception.CacheException(ex);
                            throw;
                        }
                    }
                }
            }
            return _dicActivitySettings;
        }
        #endregion

        #region OrchAuthenticationSetting Cache
        internal List<ResponseDictionaryItem>? _dicAuthenticationSettings = null;
        internal readonly ExceptionCachePerTenant _dicAuthenticationSettings_Exception = new();
        public List<ResponseDictionaryItem>? GetAuthenticationSettings()
        {
            _dicAuthenticationSettings_Exception.ThrowCachedExceptionIfAny();

            if (_dicAuthenticationSettings == null)
            {
                lock (_dicAuthenticationSettings_Exception)
                {
                    if (_dicAuthenticationSettings == null)
                    {
                        try
                        {
                            var dic = OrchAPISession.GetAuthenticationSettings();
                            _dicAuthenticationSettings = dic?.Keys?.Zip(dic.Values ?? [], (key, value) => new ResponseDictionaryItem()
                            {
                                Path = NameColonSeparator,
                                Key = key,
                                Value = value
                            }).ToList();
                        }
                        catch (HttpResponseException ex)
                        {
                            _dicAuthenticationSettings_Exception.CacheException(ex);
                            throw;
                        }
                    }
                }
            }
            return _dicAuthenticationSettings;
        }
        #endregion

        #region OrchConnectionSetting Cache
        internal ODataValueOfString? _dicConnectionString = null;
        internal readonly ExceptionCachePerTenant _dicConnectionString_Exception = new();
        public ODataValueOfString? GetConnectionString()
        {
            _dicConnectionString_Exception.ThrowCachedExceptionIfAny();

            if (_dicConnectionString == null)
            {
                lock (_dicConnectionString_Exception)
                {
                    if (_dicConnectionString == null)
                    {
                        try
                        {
                            _dicConnectionString = OrchAPISession.GetConnectionString();
                            if (_dicConnectionString != null)
                            {
                                _dicConnectionString.Path = NameColonSeparator;
                            }
                        }
                        catch (HttpResponseException ex)
                        {
                            _dicConnectionString_Exception.CacheException(ex);
                            throw;
                        }
                    }
                }
            }
            return _dicConnectionString;
        }
        #endregion

        #region OrchLicense Cache
        internal License? _dicLicense = null;
        internal readonly ExceptionCachePerTenant _dicLicense_Exception = new();
        public License? GetLicenseSettings()
        {
            _dicLicense_Exception.ThrowCachedExceptionIfAny();

            if (_dicLicense == null)
            {
                lock (_dicLicense_Exception)
                {
                    if (_dicLicense == null)
                    {
                        try
                        {
                            _dicLicense = OrchAPISession.GetLicenseSettings();
                            if (_dicLicense != null)
                            {
                                _dicLicense.Path = NameColonSeparator;
                            }
                        }
                        catch (HttpResponseException ex)
                        {
                            _dicLicense_Exception.CacheException(ex);
                            throw;
                        }
                    }
                }
            }
            return _dicLicense;
        }
        #endregion

        #region OrchActionCatalog Cache
        internal ConcurrentDictionary<Int64, List<TaskCatalog>>? _dicTaskCatalog = null;
        internal readonly ExceptionsCachePer<Int64> _dicTaskCatalog_Exceptions = new();
        public ReadOnlyCollection<TaskCatalog> GetTaskCatalogs(Folder folder)
        {
            _dicTaskCatalog_Exceptions.ThrowCachedExceptionIfAny(folder.Id ?? 0);

            if (_dicTaskCatalog == null)
            {
                lock (_dicTaskCatalog_Exceptions)
                {
                    _dicTaskCatalog ??= [];
                }
            }

            if (!_dicTaskCatalog.TryGetValue(folder.Id ?? 0, out var actionCatalogs))
            {
                try
                {
                    actionCatalogs = OrchAPISession.GetTaskCatalogs(folder.Id ?? 0).ToList();
                    string folderPath = folder.GetPSPath();
                    foreach (var actionCatalog in actionCatalogs)
                    {
                        actionCatalog.Path = folderPath;
                    }
                    _dicTaskCatalog[folder.Id ?? 0] = actionCatalogs;
                }
                catch (HttpResponseException ex)
                {
                    _dicTaskCatalog_Exceptions.CacheException(folder.Id ?? 0, ex);
                    throw;
                }
            }
            return actionCatalogs.AsReadOnly();
        }

        #endregion

        #region PmUser Cache
        // key: userId
        internal ConcurrentDictionary<string, PmUser>? _dicPmUsers = null;
        internal readonly ExceptionCachePerTenant _dicPmUsers_Exception = new();
        public ConcurrentDictionary<string, PmUser> GetPmUsers()
        {
            _dicPmUsers_Exception.ThrowCachedExceptionIfAny();

            if (_dicPmUsers == null)
            {
                lock (_dicPmUsers_Exception)
                {
                    if (_dicPmUsers == null)
                    {
                        try
                        {
                            _dicPmUsers = new ConcurrentDictionary<string, PmUser>();
                            var partitionGlobalId = GetPartitionGlobalId();
                            var users = OrchAPISession.GetPmUsers(partitionGlobalId!);
                            foreach (var user in users)
                            {
                                user.Path = NameColonSeparator;
                                _dicPmUsers[user.id ?? ""] = user;
                            }
                        }
                        catch (HttpResponseException ex)
                        {
                            _dicPmUsers_Exception.CacheException(ex);
                            throw;
                        }
                    }
                }
            }
            return _dicPmUsers;
        }
        #endregion

        internal HashSet<PmAuditLog>? _dicPmAuditLogs = null;
        internal readonly ExceptionCachePerTenant _dicPmAuditLogs_Exception = new();
        public ReadOnlyCollection<PmAuditLog> GetPmAuditLog(string? query, ulong skip, ulong first)
        {
            // こいつはマルチスレッドを考慮する必要はないはずだが、念のため。。
            if (_dicPmAuditLogs == null)
            {
                lock (_dicPmAuditLogs_Exception)
                {
                    _dicPmAuditLogs = [];
                }
            }

            // 必ず問い合わせる
            var partitionGlobalId = GetPartitionGlobalId();
            var logs = OrchAPISession.GetPmAuditLog(partitionGlobalId, query, skip, first).ToList();
            foreach (var log in logs)
            {
                log.Path = NameColonSeparator;
                log.auditLogDetailsExpanded = JsonTools.JsonToDictionary(log.auditLogDetails);
                _dicPmAuditLogs.Add(log);
            }

            return logs.AsReadOnly();
        }

        // key: 検索テキスト
        internal ConcurrentDictionary<string, PmDirectoryEntityInfo[]?>? _dicPmDirectoryUsers = null;
        internal readonly ExceptionsCachePer<string> _dicPmDirectoryUsersException = new();
        public PmDirectoryEntityInfo[]? SearchPmDirectoryUsers(string key)
        {
            _dicPmDirectoryUsersException.ThrowCachedExceptionIfAny(key);

            if (_dicPmDirectoryUsers == null)
            {
                lock (_dicPmDirectoryUsersException)
                {
                    _dicPmDirectoryUsers ??= [];
                }
            }
            PmDirectoryEntityInfo[] ret;
            if (!_dicPmDirectoryUsers.TryGetValue(key, out ret))
            {
                try
                {
                    var partitionGlobalId = GetPartitionGlobalId();
                    ret = OrchAPISession.SearchPmDirectoryUsers(partitionGlobalId!, key);
                    foreach (var user in ret ?? [])
                    {
                        user.Path = NameColonSeparator;
                    }
                    _dicPmDirectoryUsers[key] = ret;
                }
                catch (HttpResponseException ex)
                {
                    _dicPmDirectoryUsersException.CacheException(key, ex);
                    throw;
                }
            }
            return ret;
        }

        // key: (name, kind)
        // kind: "user", "group", or "application"
        internal ConcurrentDictionary<(string, string), Member>? _dicPmBulkResolveByName = null;
        internal readonly ExceptionCachePerTenant _dicPmBulkResolveByName_Exception = new();
        public ReadOnlyCollection<Member> PmBulkResolveByName(string kind, IEnumerable<string> names)
        {
            _dicPmBulkResolveByName_Exception.ThrowCachedExceptionIfAny();

            if (_dicPmBulkResolveByName == null)
            {
                lock (_dicPmBulkResolveByName_Exception)
                {
                    _dicPmBulkResolveByName ??= [];
                }
            }

            // まだ問い合わせていない names の一覧を作成
            string[] needQueryNames = names.Where(name => !_dicPmBulkResolveByName.ContainsKey((kind, name))).ToArray();

            if (needQueryNames.Length != 0)
            {
                try
                {
                    var partitionGlobalId = GetPartitionGlobalId();
                    var result = OrchAPISession.PmBulkResolveByName(partitionGlobalId!, kind, needQueryNames);
                    foreach (var kvp in result ?? [])
                    {
                        _dicPmBulkResolveByName.AddOrUpdate((kind, kvp.Key), kvp.Value, (key, oldValue) => kvp.Value);
                    }
                }
                catch (HttpResponseException ex)
                {
                    _dicPmBulkResolveByName_Exception.CacheException(ex);
                    throw;
                }
            }

            // 指定されたキーを持つ要素を抽出してリストを作成
            var ret = names.Select(name => {
                    bool found = _dicPmBulkResolveByName.TryGetValue((kind, name), out Member value);
                    return (found, value);
                })
                .Where(result => result.found && result.value != null)
                .Select(result => result.value!)
                .ToList();
            return ret.AsReadOnly();
        }

        // key: name
        internal ConcurrentDictionary<string, DirectoryObject[]?>? _dicSearchForUsersAndGroups = null;
        internal readonly ExceptionsCachePer<string>_dicSearchForUsersAndGroups_Exception = new();
        public IEnumerable<DirectoryObject> SearchForUsersAndGroups(string name)
        {
            // この API は、'+' を含むユーザー名を検索できないようだ。
            // 念のため、+ のほか '-' と '_' についても検索ワードから除いて処理する
            int index = name.IndexOfAny(['+', '-', '_']);
            string searchWord = index >= 0 ? name.Substring(0, index) : name;

            _dicSearchForUsersAndGroups_Exception.ThrowCachedExceptionIfAny(searchWord);

            if (_dicSearchForUsersAndGroups == null)
            {
                lock (_dicSearchForUsersAndGroups_Exception)
                {
                    _dicSearchForUsersAndGroups ??= [];
                }
            }
            if (!_dicSearchForUsersAndGroups.TryGetValue(searchWord, out var value))
            {
                try
                {
                    value = OrchAPISession.SearchForUsersAndGroups(searchWord);
                    foreach (var v in value ?? [])
                    {
                        v.Path = NameColonSeparator;
                    }
                    _dicSearchForUsersAndGroups[searchWord] = value;
                }
                catch (HttpResponseException ex)
                {
                    _dicSearchForUsersAndGroups_Exception.CacheException(searchWord, ex);
                    throw;
                }
            }

            return value?.Where(obj => obj.identityName?.StartsWith(name, StringComparison.OrdinalIgnoreCase) ?? false) ?? [];
        }

        #region PmRobot Cache
        // key: robotId
        internal ConcurrentDictionary<string, PmRobotAccount>? _dicPmRobotAccounts = null;
        internal readonly ExceptionCachePerTenant _dicPmRobotAccounts_Exceptions = new();
        public ConcurrentDictionary<string, PmRobotAccount> GetPmRobotAccounts()
        {
            _dicPmRobotAccounts_Exceptions.ThrowCachedExceptionIfAny();

            if (_dicPmRobotAccounts == null)
            {
                lock (_dicPmRobotAccounts_Exceptions)
                {
                    if (_dicPmRobotAccounts == null)
                    {
                        var partitionGlobalId = GetPartitionGlobalId();
                        _dicPmRobotAccounts = new ConcurrentDictionary<string, PmRobotAccount>();
                        try
                        {
                            var robots = OrchAPISession.GetPmRobotAccounts(partitionGlobalId!);
                            if (robots == null) return _dicPmRobotAccounts;
                            foreach (var robot in robots)
                            {
                                robot.Path = NameColonSeparator;
                                _dicPmRobotAccounts[robot.id ?? ""] = robot;
                            }
                        }
                        catch (HttpResponseException ex)
                        {
                            _dicPmRobotAccounts_Exceptions.CacheException(ex);
                            throw;
                        }
                    }
                }
            }
            return _dicPmRobotAccounts;
        }
        #endregion

        #region PmGroup Cache
        // key: groupId
        // 機密アプリの場合、繰り返し API call することを避けるためにキャッシュに null を入れた方が良いか？
        // TODO: ConcurrentDictionary ではなく PmGroup[] で良いのではないか？
        internal ConcurrentDictionary<string, PmGroup>? _dicPmGroups = null;
        internal readonly ExceptionCachePerTenant _dicPmGroups_Exception = new();
        public ConcurrentDictionary<string, PmGroup> GetPmGroups()
        {
            _dicPmGroups_Exception.ThrowCachedExceptionIfAny();

            if (_dicPmGroups == null)
            {
                lock (_dicPmGroups_Exception)
                {
                    if (_dicPmGroups == null)
                    {
                        _dicPmGroups = new();
                        var partitionGlobalId = GetPartitionGlobalId();

                        try
                        {
                            var groups = OrchAPISession.GetPmGroups(partitionGlobalId!);

                            #region ここで各グループの詳細を取得するならこっち。
                            // でもグループの数が多い場合、API call の数が多くなるので、ここでは取得しない方が良いか。。
                            //Parallel.ForEach(groups!, group =>
                            //{
                            //    var detailedGroup = OrchAPISession.GetIdentityGroup(partitionGlobalId!, group.id!);
                            //    if (detailedGroup != null)
                            //    {
                            //        detailedGroup.Path = NameColonSeparator;
                            //        if (detailedGroup.members != null)
                            //        {
                            //            foreach (var member in detailedGroup.members)
                            //            {
                            //                member.setGroupName(group.name);
                            //                member.setPath(Path.Combine(NameColon, group.name!));
                            //            }
                            //        }
                            //        _dicIdGroups[detailedGroup.id ?? ""] = detailedGroup;
                            //    }
                            //});
                            #endregion

                            #region 各グループの詳細は、Get-OrchIdGroup -ExpandMember を指定したときに
                            // 指定されたグループだけについて詳細を取得するならこっち。
                            foreach (var group in groups ?? [])
                            {
                                group.Path = NameColonSeparator;
                                _dicPmGroups[group.id!] = group;
                                if (group.members != null && group.members.Length == 0)
                                {
                                    group.members = null;
                                }
                            }
                            #endregion
                        }
                        catch (HttpResponseException ex)
                        {
                            _dicPmGroups_Exception.CacheException(ex);
                            throw;
                        }
                    }
                }
            }
            return _dicPmGroups;
        }

        public PmGroup? GetPmGroup(string? groupId)
        {
            if (groupId == null) return null;

            if (_dicPmGroups == null)
            {
                lock (this)
                {
                    _dicPmGroups ??= new();
                }
            }

            if (_dicPmGroups.TryGetValue(groupId, out var group))
            {
                if (group == null) return null;
                // members があれば、すでにキャッシュ済みなのでそのまま返す
                //if (group.members != null && group.members.Length != 0) return group;
                if (group.members != null) return group;
            }

            #region Get partitionGlobalId
            var partitionGlobalId = GetPartitionGlobalId();
            if (string.IsNullOrEmpty(partitionGlobalId))
            {
                return group;
            }
            #endregion

            var group2 = OrchAPISession.GetPmGroup(partitionGlobalId!, groupId);
            if (group2 != null)
            {
                group2.Path = NameColonSeparator;
                if (group2.members != null)
                {
                    foreach (var member in group2.members)
                    {
                        member.Path = NameColonSeparator;
                        member.PathGroupName = Path.Combine(NameColonSeparator, group?.name ?? "");
                        member.groupName = group?.name;
                    }
                }

                if (group != null)
                {
                    group.name = group2.name;
                    group.displayName = group2.displayName;
                    group.type = group2.type;
                    group.creationTime = group2.creationTime;
                    group.lastModificationTime = group2.lastModificationTime;
                    group.members = group2.members;
                    group.mappingRole = group2.mappingRole;
                    group.scope = group2.scope;
                    return group;
                }
                else
                {
                    _dicPmGroups[groupId] = group2;
                    return group2;
                }
            }
            return null;
        }

        #endregion

        #region PmUserLicensedUsers Cache
        internal List<NuLicensedUser>? _dicPmLicensedUsers = null;
        internal readonly ExceptionCachePerTenant _dicPmLicensedUsers_Exceptions = new();
        public ReadOnlyCollection<NuLicensedUser> GetPmLicensedUsers()
        {
            _dicPmLicensedUsers_Exceptions.ThrowCachedExceptionIfAny();

            if (_dicPmLicensedUsers == null)
            {
                lock (_dicPmLicensedUsers_Exceptions)
                {
                    try
                    {
                        _dicPmLicensedUsers = OrchAPISession.GetPmLicensedUsers().ToList();

                        foreach (var user in _dicPmLicensedUsers)
                        {
                            user.Path = NameColonSeparator;
                            user.userBundleLicenseNames = user.userBundleLicenses?.Select(b => AvailableUserBundlesItems.Items[b]).ToArray();
                        }
                    }
                    catch (HttpResponseException ex)
                    {
                        _dicPmLicensedUsers_Exceptions.CacheException(ex);
                        throw;
                    }
                }
            }
            return _dicPmLicensedUsers.AsReadOnly();
        }
        #endregion

        #region PmUserLicenseGroups Cache
        internal List<NuLicensedGroup>? _dicPmLicensedGroups = null;
        internal readonly ExceptionCachePerTenant _dicPmLicensedGroups_Exceptions = new();
        public ReadOnlyCollection<NuLicensedGroup> GetPmLicensedGroups()
        {
            _dicPmLicensedGroups_Exceptions.ThrowCachedExceptionIfAny();

            if (_dicPmLicensedGroups == null)
            {
                lock (_dicPmLicensedGroups_Exceptions)
                {
                    try
                    {
                        _dicPmLicensedGroups = OrchAPISession.GetPmLicensedGroups().ToList();

                        foreach (var group in _dicPmLicensedGroups)
                        {
                            group.Path = NameColonSeparator;
                            group.userBundleLicenseNames = group.userBundleLicenses?.Select(b => AvailableUserBundlesItems.Items[b]).ToArray();
                        }
                    }
                    catch (HttpResponseException ex)
                    {
                        _dicPmLicensedGroups_Exceptions.CacheException(ex);
                        throw;
                    }
                }
            }
            return _dicPmLicensedGroups.AsReadOnly();
        }
        #endregion

        internal ConcurrentDictionary<string, AvailableUserBundles>? _dicPmAvailableUserBundles = null;
        internal readonly ExceptionsCachePer<string> _dicPmAvailableUserBundles_Exceptions = new();
        //public AvailableUserBundles? GetPmUserLicenseGroupsAvailableLicenses(NuLicensedGroup? group)
        public AvailableUserBundles? GetPmUserLicenseGroupsAvailableLicenses(string? groupId, string groupName)
        {
            if (groupId == null) return null;

            _dicPmAvailableUserBundles_Exceptions.ThrowCachedExceptionIfAny(groupId);

            if (_dicPmAvailableUserBundles == null)
            {
                lock (_dicPmAvailableUserBundles_Exceptions)
                {
                    _dicPmAvailableUserBundles ??= [];
                }
            }

            if (!_dicPmAvailableUserBundles.TryGetValue(groupId, out var ret))
            {
                try
                {
                    ret = OrchAPISession.GetPmLicensedGroupsAvailableLicenses(groupId);
                    if (ret != null)
                    {
                        ret.Path = NameColonSeparator;
                        ret.GroupName = groupName;
                        ret.PathGroupName = System.IO.Path.Combine(NameColonSeparator, groupName);
                        foreach (var bundle in ret.availableUserBundles ?? [])
                        {
                            if (AvailableUserBundlesItems.Items.TryGetValue(bundle.code ?? "", out var name))
                            {
                                bundle.name = name;
                            }
                        }
                        _dicPmAvailableUserBundles[groupId] = ret;
                    }
                }
                catch (HttpResponseException ex)
                {
                    _dicPmAvailableUserBundles_Exceptions.CacheException(groupId, ex);
                    throw;
                }
            }
            return ret;
        }

        #region GetPmUserLicenseGroupAllocations Cache
        internal ConcurrentDictionary<string, List<NuLicensedGroupMember>>? _dicPmUserLicenseGroupAllocations = null;
        internal readonly ExceptionsCachePer<string> _dicPmUserLicenseGroupAllocations_Exceptions = new();
        public ReadOnlyCollection<NuLicensedGroupMember> GetPmLicensedGroupAllocations(NuLicensedGroup group)
        {
            _dicPmUserLicenseGroupAllocations_Exceptions.ThrowCachedExceptionIfAny(group.id!);

            if (_dicPmUserLicenseGroupAllocations == null)
            {
                lock (_dicPmUserLicenseGroupAllocations_Exceptions)
                {
                    _dicPmUserLicenseGroupAllocations ??= [];
                }
            }

            if (!_dicPmUserLicenseGroupAllocations.TryGetValue(group.id!, out var ret))
            {
                try
                {
                    ret = OrchAPISession.GetPmLicenseGroupAllocations(group.id).ToList();
                    foreach (var user in ret)
                    {
                        user.Path = NameColonSeparator;
                        user.GroupName = group?.name;
                        user.PathGroupName = System.IO.Path.Combine(NameColonSeparator, group?.name ?? "");
                    }
                    _dicPmUserLicenseGroupAllocations[group!.id!] = ret;
                }
                catch (HttpResponseException ex)
                {
                    _dicPmUserLicenseGroupAllocations_Exceptions.CacheException(group.id!, ex);
                    throw;
                }
            }
            return ret.AsReadOnly();
        }
        #endregion

        #region PmExternalApiResource cache
        internal ExternalResource[]? _dicPmExternalApiResource = null;
        internal readonly ExceptionCachePerTenant _dicPmExternalApiResource_Exception = new();
        public ExternalResource[]? GetIdentityExternalApiResource()
        {
            _dicPmExternalApiResource_Exception.ThrowCachedExceptionIfAny();

            if (_dicPmExternalApiResource == null)
            {
                lock (_dicPmExternalApiResource_Exception)
                {
                    if (_dicPmExternalApiResource == null)
                    {
                        try
                        {
                            _dicPmExternalApiResource = OrchAPISession.GetPmExternalApiResource();
                            foreach (var res in _dicPmExternalApiResource ?? [])
                            {
                                res.Path = NameColonSeparator;
                            }
                        }
                        catch (HttpResponseException ex)
                        {
                            _dicPmExternalApiResource_Exception.CacheException(ex);
                            throw;
                        }
                    }
                }
            }
            return _dicPmExternalApiResource;
        }
        #endregion

        #region PmExternalClient cache
        internal ConcurrentDictionary<string, ExternalClient>? _dicPmExternalClient = null;
        internal readonly ExceptionCachePerTenant _dicPmExternalClient_Exception = new();
        public ConcurrentDictionary<string, ExternalClient> GetIdentityExternalClient()
        {
            _dicPmExternalClient_Exception.ThrowCachedExceptionIfAny();

            if (_dicPmExternalClient == null)
            {
                lock (_dicPmExternalClient_Exception)
                {
                    if (_dicPmExternalClient == null)
                    {
                        var partitionGlobalId = GetPartitionGlobalId();

                        try
                        {
                            var apps = OrchAPISession.GetPmExternalClient(partitionGlobalId!);
                            _dicPmExternalClient = new ConcurrentDictionary<string, ExternalClient>();
                            if (apps == null) return _dicPmExternalClient;
                            foreach (var app in apps)
                            {
                                app.Path = NameColonSeparator;
                                _dicPmExternalClient[app.id ?? ""] = app;
                            }
                        }
                        catch (HttpResponseException ex)
                        {
                            _dicPmExternalClient_Exception.CacheException(ex);
                            throw;
                        }
                    }
                }
            }
            return _dicPmExternalClient;
        }
        #endregion

        #region PmAuthenticationSetting cache
        internal PmAuthenticationRoot? _dicPmAuthenticationSetting = null;
        internal readonly ExceptionCachePerTenant _dicPmAuthenticationSetting_Exception = new();
        public PmAuthenticationRoot? GetPmAuthenticationSettings()
        {
            _dicPmAuthenticationSetting_Exception.ThrowCachedExceptionIfAny();

            if (_dicPmAuthenticationSetting == null)
            {
                lock (_dicPmAuthenticationSetting_Exception)
                {
                    if (_dicPmAuthenticationSetting == null)
                    {
                        var partitionGlobalId = GetPartitionGlobalId();

                        try
                        {
                            _dicPmAuthenticationSetting = OrchAPISession.GetPmAuthenticationSettings(partitionGlobalId!);
                            if (_dicPmAuthenticationSetting != null)
                            {
                                _dicPmAuthenticationSetting.Path = NameColonSeparator;
                            }
                        }
                        catch (HttpResponseException ex)
                        {
                            _dicPmAuthenticationSetting_Exception.CacheException(ex);
                            throw;
                        }
                    }
                }
            }
            return _dicPmAuthenticationSetting;
        }
        #endregion

        #region FeedId cache

        // Key: folderId, Value: feedId
        // このメソッドは、"" を返すべきではない
        // このメソッドを呼ぶ側が、必要に応じて null を "" に変換する必要がある
        // (Dictionary のキーとして使う場合など)
        internal ConcurrentDictionary<Int64, string>? _dicFolderFeedIds = null;
        public string? GetFolderFeedId(Folder folder)
        {
            if (_dicFolderFeedIds == null)
            {
                lock (this)
                {
                    _dicFolderFeedIds ??= new ConcurrentDictionary<long, string>();
                }
            }
            Int64 folderId = folder.Id ?? 0;
            if (!_dicFolderFeedIds.TryGetValue(folderId, out string feedId))
            {
                feedId = OrchAPISession.GetFolderFeedId(folderId);
                _dicFolderFeedIds[folderId] = feedId!;
            }
            return feedId;
        }

        #endregion

        // このコンストラクタを実行するタイミングでは、NameColonSeparator は利用できない
        public OrchDriveInfo(ProviderInfo provider, PSDrive drive, ProxySettings? globalProxy) :
            base(drive.Name, provider, drive.Name + ":\\", drive.Description, null, drive.Root)
        {
            _globalProxy = globalProxy;
            _psDrive = drive;
            _psDrive.Root = _psDrive.Root?.TrimEnd('/');
            RootFolder = new Folder() { DisplayName = "", FullyQualifiedName = "", Path = NameColonSeparator };
        }

        internal List<Folder>? _dicFolders; // sorted by OrchDirectory and DisplayName
        public ReadOnlyCollection<Folder> GetFolders()
        {
            if (_dicFolders == null)
            {
                OrchAPISession.EnsureAuthenticated();

                var tasks = new List<Task>();

                // get current user to get my own personal workspace
                ExtendedUser user = null;
                if (!OrchAPISession.AuthManager.IsConfidentialApp)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        // この例外は握りつぶす
                        try { user = GetCurrentUser() as ExtendedUser; } catch { }
                    }));
                }

                // get personal workspaces that being explored
                ReadOnlyCollection<PersonalWorkspace> personalWorkspaces = null;
                tasks.Add(Task.Run(() =>
                {
                    // この例外は握りつぶす
                    try { personalWorkspaces = GetPersonalWorkspaces(); } catch { }
                }));

                // get folders
                List<Folder> folders = null;
                tasks.Add(Task.Run(() =>
                {
                    // もし例外が発生したら、そのまま漏らす
                    folders = OrchAPISession.GetFolders().ToList();
                }));

                Task.WaitAll([..tasks]);

                // current user の戻りを処理
                string personalWorkspaceName = "";
                if (user != null && user.PersonalWorkspace != null)
                {
                    personalWorkspaceName = user.PersonalWorkspace.DisplayName;
                    user.PersonalWorkspace.ParentId = null; // なぜか値が入っていることがあるので修正
                    user.PersonalWorkspace.Path = NameColonSeparator;
                    user.PersonalWorkspace.FeedType = "FolderHierarchy"; // なぜか "Processes" が入っているので修正
                    //user.PersonalWorkspace.FullName = NameColonSeparator + WildcardPattern.Escape(user.PersonalWorkspace.DisplayName);
                    user.PersonalWorkspace.FullName = NameColonSeparator + user.PersonalWorkspace.DisplayName;
                    _dicFolders = [user.PersonalWorkspace];
                }

                // personal workspaces that being explored の戻りを処理
                #region retriving Exploring Personal Workspace
                if (personalWorkspaces != null)
                {
                    foreach (var ws in personalWorkspaces
                        .Where(ws => ws.Name != personalWorkspaceName && // My Workspace は直前の current user の処理で追加済みなので除外
                            (user != null && ws.ExploringUserIds != null && ws.ExploringUserIds.Any(id => id == user?.Id)))
                        .OrderBy(ws => ws.Name)) 
                    {
                        // 自分が exploring 中の、他人のワークスペースを追加する
                        // (explore していないワークスペースには、権限がなくアクセスできないため)
//                        if (user != null && ws.ExploringUserIds != null && ws.ExploringUserIds.Any(id => id == user?.Id))
                        {
                            _dicFolders ??= [];
                            _dicFolders.Add(new Folder()
                            {
                                DisplayName = ws.Name,
                                FullyQualifiedName = ws.Name,
                                FullyQualifiedNameOrderable = ws.Name,
                                Id = ws.Id,
                                IsActive = ws.IsActive,
                                Key = ws.Key,
                                FolderType = "Personal",
                                FeedType = "FolderHierarchy",
                                ProvisionType = "Automatic",
                                PermissionModel = "FineGrained",
                                Path = NameColonSeparator,
                                FullName = NameColonSeparator + WildcardPattern.Escape(ws.Name)
                            });
                        }
                    }
                }
                #endregion

                // folders の戻りを処理
                foreach (var folder in folders ?? [])
                {
                    // Path メンバに、親フォルダの名前を入れておく
                    int idx = folder.FullyQualifiedName!.LastIndexOf('/');
                    if (idx != -1)
                    {
                        string orchPath = OrchDriveInfo.OrchProviderPathToPSPath(folder.FullyQualifiedName.Substring(0, idx));
                        folder.Path = NameColon + orchPath;
                        folder.FullName = NameColon + Path.Combine(orchPath, folder.DisplayName ?? "");
                        //folder.FullName = NameColon + WildcardPattern.Escape(Path.Combine(orchPath, folder.DisplayName ?? ""));
                    }
                    else
                    {
                        string orchPath = OrchDriveInfo.OrchProviderPathToPSPath(folder.FullyQualifiedName);
                        folder.Path = NameColonSeparator;
                        folder.FullName = NameColon + orchPath;
                        //folder.FullName = NameColon + WildcardPattern.Escape(orchPath);
                    }
                }
                _dicFolders ??= [];
                if (folders != null)
                {
                    _dicFolders.AddRange(folders
                        .OrderBy(f => f.Path!) //, StringComparer.Ordinal)
                        .ThenBy(f => f.DisplayName)); // , StringComparer.Ordinal).ToList();
                }
            }

            return _dicFolders.AsReadOnly();
        }

        public Folder? GetFolder(string orchPath)
        {
            if (orchPath == "")
            {
                return RootFolder;
            }
            return GetFolders().FirstOrDefault(f => string.Equals(f.FullyQualifiedName, orchPath, StringComparison.OrdinalIgnoreCase));
        }

        public Folder GetParentFolder(Folder folder)
        {
            if (folder.ParentId != null && folder.ParentId != 0)
            {
                return GetFolders().FirstOrDefault(f => f!.Id == folder.ParentId, RootFolder)!;
            }
            return RootFolder!;
        }

        public Folder? GetParentFolder(string orchPath)
        {
            int slashIndex = orchPath.LastIndexOf('/');
            if (slashIndex == -1)
            {
                return RootFolder!;
            }

            string parentPath = orchPath.Substring(0, slashIndex);
            return GetFolders().FirstOrDefault(f => f.FullyQualifiedName == parentPath);
        }

        public Folder? GetCurrentFolder()
        {
            if (string.IsNullOrEmpty(CurrentLocation) || CurrentLocation == "\\")
            {
                return null;
            }
            return GetFolder(OrchDriveInfo.PSPathToOrchPath(CurrentLocation))!;
        }
    }
}
