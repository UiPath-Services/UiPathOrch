using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Runtime.InteropServices;
using UiPath.OrchAPI;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Entities.JsonConverter;
using UiPath.PowerShell.Positional;
using Job = UiPath.PowerShell.Entities.Job;
using License = UiPath.PowerShell.Entities.License;
using Path = System.IO.Path;
using Session = UiPath.PowerShell.Entities.Session;
using User = UiPath.PowerShell.Entities.User;

namespace UiPath.PowerShell.Core;

// OrchDriveInfo, OrchDuDriveInfo, OrchTmDriveInfo に共通のベースクラスを作成したいが
// ちょっと大変。。いったん先延ばし。
// 
//public class OrchDriveInfoBase : PSDriveInfo
//{
//}

public partial class OrchDriveInfo : PSDriveInfo
{
    internal readonly PSDrive _psDrive;

    private string? _NameColon = null;
    private string? _NameColonSeparator = null;

    internal string NameColon
    {
        get
        {
            _NameColon ??= Name + ':';
            return _NameColon;
        }
    }
    internal string NameColonSeparator
    {
        get
        {
            _NameColonSeparator ??= Name + ':' + Path.DirectorySeparatorChar;
            return _NameColonSeparator;
        }
    }

    private OrchAPISession? _orchAPISession;
    private readonly object _orchAPISessionLock = new();
    internal OrchAPISession OrchAPISession
    {
        get
        {
            if (_orchAPISession is null)
            {
                lock (_orchAPISessionLock)
                {
                    _orchAPISession ??= new OrchAPISession(this);
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
        int colonIndex = path.IndexOf(':');
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
        int colonIndex = path.IndexOf(':');
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
        return Path.DirectorySeparatorChar +
            (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                path.Replace('/', Path.DirectorySeparatorChar) : path);
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
        if (path is null || !path.Any() || path.All(p => p is null))
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
    public static List<OrchDriveInfo> EnumPmDrives(IEnumerable<string?>? path = null)
    {
        static void AddOrchDrive(HashSet<OrchDriveInfo> drives, PSDriveInfo drive)
        {
            if (drive is OrchDriveInfo orchDrive)
            {
                drives.Add(orchDrive);
            }
            else if (SessionState!.Path.CurrentLocation.Drive is OrchDuDriveInfo orchDuDrive)
            {
                drives.Add(orchDuDrive.ParentDrive);
            }
            else if (SessionState!.Path.CurrentLocation.Drive is OrchTmDriveInfo orchTmDrive)
            {
                drives.Add(orchTmDrive.ParentDrive);
            }
        }

        var drives = new HashSet<OrchDriveInfo>();
        if (path is null || !path.Any() || path.All(p => p is null))
        {
            if (SessionState is not null)
            {
                AddOrchDrive(drives, SessionState.Path.CurrentLocation.Drive);
            }
        }
        else
        {
            var psPaths = path.Select(p => SessionState!.Path.GetResolvedPSPathFromPSPath(p)).SelectMany(p => p);
            foreach (var p in psPaths)
            {
                AddOrchDrive(drives, p.Drive);
            }
        }
        return drives.OrderBy(d => d.Name).ToList();
    }

    // paths を指定しない場合、カレントドライブのみを返す
    public static List<OrchDuDriveInfo> EnumDuDrives(IEnumerable<string?>? path = null)
    {
        return EnumOrchDrivesImpl<OrchDuDriveInfo>(path);
    }

    // すこし実装が重複しているような。。きれいにできたい
    public static OrchDriveInfo GetPmDrive(string? path = null)
    {
        var srcDrives = EnumPmDrives([path]);
        if (srcDrives.Count > 1)
        {
            throw new Exception($"'{path}' resolved to multiple containers.");
        }
        if (srcDrives.Count == 0)
        {
            // たぶん先に EnumPmDrives() が例外を投げているはずなので、ここは実行されないと思う。
            throw new Exception($"Cannot find path '{path}' because it does not exist.");
        }
        return srcDrives[0];
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
        if (paths is null || !paths.Any() || paths.All(p => p is null))
        {
            PathInfo pathInfo = SessionState!.Path.CurrentLocation;
            if (pathInfo.Drive is OrchDriveInfo)
            {
                yield return SessionState!.Path.CurrentLocation;
            }
        }
        else
        {
            var psPaths = paths.Where(p => p is not null).Select(p => SessionState!.Path.GetResolvedPSPathFromPSPath(p)).SelectMany(p => p);
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
            if (drive is null) continue;

            drive.GetFolders(); // sorted by OrchDirectory and DisplayName
            var dicFolders = drive._dicFoldersForEnumFolders;
            if (dicFolders is null) continue;

            Folder folder = drive.GetFolder(OrchDriveInfo.PSPathToOrchPath(WildcardPattern.Unescape(p.ProviderPath)));
            if (folder is null) continue;

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

        if (ret is null || ret.Count == 0)
        {
            throw new Exception("Use Set-Location cmdlet (alias: cd) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters.");
        }
        return ret;
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

    public static IEnumerable<(string FullPath, string RelativePath)> ExpandLocalPath(SessionState sessionState, string[]? localPaths, string wildcard, bool recurse = false, int depth = 0)
    {
        localPaths = localPaths?.Select(p => sessionState.Path.GetUnresolvedProviderPathFromPSPath(p)).ToArray();

        HashSet<string> uniquePath = [];

        if (localPaths is null)
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
                EnumerationOptions option = new()
                {
                    RecurseSubdirectories = recurse,
                    MaxRecursionDepth = depth
                };
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
            if (drive is null) continue;

            Folder folder = drive?.GetFolder(OrchDriveInfo.PSPathToOrchPath(WildcardPattern.Unescape(p.ProviderPath)));
            if (folder is null) continue;

            string feedFolderPath = folder.GetPackageFeedFolder();
            Folder feedFolder = drive!.GetFolder(feedFolderPath);
            if (feedFolder is null) continue;

            if (!feedFolders.Add(feedFolder.GetPSPath()))
            {
                continue;
            }

            ret.Add((drive!, feedFolder));

            // folder がルートディレクトリで、かつ recurse の場合に限り子フォルダーを列挙する
            if ((folder.ParentId is null && folder.FolderType != "Personal") && recurse)
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

        if (ret is null || ret.Count == 0)
        {
            throw new Exception("Use Set-Location (cd) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters."); // 🤖 LLM: Run 'Get-OrchHelp' for guidance!");
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

    public static Dictionary<string, string>? LoadUserMappingCsv(IWritableHost _this, OrchDriveInfo srcDrive, OrchDriveInfo dstDrive, string? path)
    {
        if (path is null) return null;

        if (srcDrive == dstDrive)
        {
            _this.WriteWarning("The specified SourceTenant and DestinationTenant drives are the same. Ignoring -UserMappingCsv parameter.");
            return null;
        }

        if (srcDrive.GetPartitionGlobalId() == dstDrive.GetPartitionGlobalId())
        {
            _this.WriteWarning("The specified SourceTenant and DestinationTenant belong to the same organization. Ignoring -UserMappingCsv parameter.");
            return null;
        }

        string physicalPath = SessionState?.Path.GetUnresolvedProviderPathFromPSPath(path);
        if (string.IsNullOrEmpty(physicalPath)) return null;

        var userMapping = new Dictionary<string, string>();

        try
        {
            using var enumerator = File.ReadLines(physicalPath).GetEnumerator();

            // Read the first line to determine column positions
            if (!enumerator.MoveNext())
                throw new InvalidOperationException("The CSV file is empty.");

            var headerLine = enumerator.Current;
            var headers = headerLine.Split(',');

            int sourceIndex = Array.FindIndex(headers, h => h.Trim().Equals("SourceUserName", StringComparison.OrdinalIgnoreCase));
            int destinationIndex = Array.FindIndex(headers, h => h.Trim().Equals("DestinationUserName", StringComparison.OrdinalIgnoreCase));

            if (sourceIndex == -1 || destinationIndex == -1)
                throw new InvalidOperationException("The CSV file does not contain the required columns: 'SourceUserName' and 'DestinationUserName'.");

            // Process remaining lines
            while (enumerator.MoveNext())
            {
                var line = enumerator.Current;

                // Skip empty lines and comments
                if (string.IsNullOrWhiteSpace(line)) continue;

                // Split the line into columns
                var columns = line.Split(',');

                // Ensure the line has enough columns
                if (columns.Length <= Math.Max(sourceIndex, destinationIndex))
                    continue;

                string sourceUserName = columns[sourceIndex].Trim();
                string destinationUserName = columns[destinationIndex].Trim();

                // Add to dictionary if the source username is not empty
                if (!string.IsNullOrEmpty(sourceUserName))
                {
                    userMapping[sourceUserName] = destinationUserName;
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error reading the CSV file at {physicalPath}", ex);
        }

        return userMapping;
    }

    protected internal Folder? RootFolder;

    // TODO: created folder cache sorted by FullyQualifiedName for GetFolder()
    // public HashSet<Folder> _folderSetCache;

    public void ClearAllCache()
    {
        if (!string.IsNullOrEmpty(_dicPartitionGlobalId))
        {
            PmUsers.ClearCache(_dicPartitionGlobalId);
            PmGroups.ClearCache(_dicPartitionGlobalId);
            PmRobotAccounts.ClearCache(_dicPartitionGlobalId);
            PmExternalClients.ClearCache(_dicPartitionGlobalId);
            PmExternalApiResources.ClearCache(_dicPartitionGlobalId);
        }

        foreach (var cache in _allTenantCache)
        {
            cache.ClearCache();
        }

        foreach (var cache in _allFolderCache)
        {
            cache.ClearCache();
        }

        if (_orchAPISession is not null)
        {
            _orchAPISession.PmApiDeprecated = true;
        }

        #region Orchestrator API cache
        _dicFolders = null;
        _dicFoldersForEnumFolders = null;

        _dicAssetLinks = null;
        _dicAssetLinks_Exception.ClearCache();

        _dicAuditLogs = null;
        _dicAuditLogs_Exceptions.ClearCache();

        _dicBucketLinks = null;
        _dicBucketLinks_Exceptions?.ClearCache();

        _dicCalendars = null;
        _dicCalendars_Exceptions?.ClearCache();

        _dicCurrentUser = null;
        _dicCurrentUser_Exception?.ClearCache();

        _dicEntitiesSummary = null;
        _dicEntitiesSummary_Exceptions?.ClearCache();

        _dicJobs = null;

        _dicJobsHavingExecutionMedia = null;

        _dicLibraryVersions = null; // 例外が発生するとしたら、_dicLibraries 取得時に発生しているはずだから、まあいいか。。
        _dicLibraryVersionsInHostFeed = null;

        _dicLicenseNamedUser = null; // TODO: 例外キャッシュを追加

        _dicLicenseRuntime = null; // TODO: 例外キャッシュを追加

        _dicMachineClientSecrets = null;
        _dicMachineClientSecrets_Exception.ClearCache();

        _dicPackages = null;
        _dicPackages_Exceptions?.ClearCache();

        _dicPackageVersions = null; // 例外が発生するとしたら、_dicPackages 取得時に発生しているはずだから、まあいいか。。

        _dicPackageEntryPoint = null;
        _dicPackageEntryPoint_Exception?.ClearCache();

        //_dicPartitionGlobalId = null; // これは変わらないから、クリアしなくていいだろう。。

        _dicTriggers = null;
        _dicTriggers_Exceptions?.ClearCache();

        _dicTriggersDetailed = null;
        _dicTriggersDetailed_Exceptions.ClearCache();

        _dicQueueLinks = null; // TODO: 例外キャッシュを追加
        _dicQueueItems = null;

        //_dicReleaseList = null;
        //_dicReleaseList_Exceptions?.ClearCache();

        _dicReleases = null;
        _dicReleases_Exceptions?.ClearCache();
                
        _dicReleasesDetailed = null;
        _dicReleasesDetailed_Exceptions.ClearCache();

        _dicRobotLogs = null;

        _dicSearchDirectory = null;
        _dicSearchDirectory_Exception.ClearCache();

        _dicTenantId = null;
        _dicTenantKey = null;

        _dicTestSetExecutions = null;
        _dicTestSetExecutions_Exceptions?.ClearCache();

        _dicUsers = null;
        _dicUsers_Exception?.ClearCache();

        _dicUsersDetailed = null;
        #endregion

        #region Platform Management cache
        _dicPmAuditLogs = null;
        _dicPmAuditLogs_Exception.ClearCache();

        _dicPmAvailableUserBundles = null;
        _dicPmAvailableUserBundles_Exceptions.ClearCache();

        _dicPmBulkResolveByName = null;
        _dicPmBulkResolveByName_Exception.ClearCache();

        _dicSearchPmDirectory = null;
        _dicSearchPmDirectory_Exception.ClearCache();

        //_dicPmGroups = null;
        //_dicPmGroups_Exception?.ClearCache();

        _dicPmUserLicenseGroupAllocations = null;
        _dicPmUserLicenseGroupAllocations_Exceptions.ClearCache();

        _dicPmAuthenticationSetting = null;
        _dicPmAuthenticationSetting_Exception.ClearCache();

        #endregion
    }

    // TODO: このメソッドの実装は不完全のはず。。
    public void ClearFolderCache(Folder? folder)
    {
        if (folder is null || folder.Id is null || folder.Id.Value == 0) return;
        Int64 folderId = folder.Id.Value;

        foreach (var cache in _allFolderCache)
        {
            cache.ClearCache(folder);
        }

        //_dicAssetLinks = null; // TODO: もっとかしこく必要な部分だけクリアしたいが、、面倒くさい
        _dicJobs?.TryRemove(folderId, out _);
        _dicJobsHavingExecutionMedia?.TryRemove(folderId, out _);
        _dicTriggers?.TryRemove(folderId, out _);
        _dicQueueLinks = null;
        _dicReleases?.TryRemove(folderId, out _);
        //_dicReleaseList?.TryRemove(folderId, out _);
        _dicTestSetExecutions?.TryRemove(folderId, out _);

        _dicEntitiesSummary_Exceptions?.ClearCache();
        _dicPackages_Exceptions?.ClearCache();
        _dicTriggers_Exceptions?.ClearCache();
        //_dicReleaseList_Exceptions?.ClearCache();
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

        if (_dicAuditLogs is null)
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
            if (log.Entities is not null && log.Entities.Length > 0)
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
        if (log.Id is null || log.Details is not null) return false;
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
    internal ConcurrentDictionary<Int64, ConcurrentDictionary<Int64, Job>>? _dicJobs = null;
    public ReadOnlyCollection<Job> GetJobs(Folder folder, string? query = null, ulong skip = 0, ulong first = ulong.MaxValue, string? orderBy = null, bool orderAscending = false)
    {
        if (_dicJobs is null)
        {
            lock (this)
            {
                _dicJobs ??= new();
            }
        }
        if (!_dicJobs.TryGetValue(folder.Id ?? 0, out var folderJobs))
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
        _dicJobs ??= new();
        if (!_dicJobs.TryGetValue(folder.Id ?? 0, out var folderJobs))
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
        _dicJobs ??= new();

        var job = OrchAPISession.GetJob(folder.Id ?? 0, jobId);
        if (job is not null)
        {
            job.Path = folder.GetPSPath();
            if (!_dicJobs.TryGetValue(folder.Id ?? 0, out var folderJobs))
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
        if (_dicRobotLogs is null)
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
        if (_dicJobsHavingExecutionMedia is null)
        {
            lock (this)
            {
                _dicJobsHavingExecutionMedia ??= [];
            }
        }

        var result = OrchAPISession.GetExecutionMedia(folder.Id ?? 0, skip, first).ToList();
        _dicJobsHavingExecutionMedia[folder.Id ?? 0] = result;

        #region  この JobId がキャッシュされていない場合に限り、_dicJobs に入れておく
        _dicJobs ??= new();

        if (!_dicJobs.TryGetValue(folder.Id ?? 0, out var jobs))
        {
            jobs = new();
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
    //internal ConcurrentDictionary<Int64, List<Release>>? _dicReleaseList;
    //internal ExceptionsCachePer<Int64> _dicReleaseList_Exceptions = new();
    //public ReadOnlyCollection<Release> ListReleases(Folder folder)
    //{
    //    _dicReleaseList_Exceptions.ThrowCachedExceptionIfAny(folder.Id ?? 0);

    //    if (_dicReleaseList is null)
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

        if (_dicUsers is null)
        {
            lock (_dicUsers_Exception)
            {
                if (_dicUsers is null)
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
        if (_dicUsersDetailed is null)
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
        if (detailedUser is not null)
        {
            detailedUser.Path = NameColonSeparator;
            if (_dicUsers is not null)
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

        if (_dicCurrentUser is null)
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
                if (exUser is not null && exUser.PersonalWorkspace is not null)
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
            if (_dicCurrentUser is not null)
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
        if (_dicPartitionGlobalId is not null) return _dicPartitionGlobalId;

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
                if (_dicPartitionGlobalId is not null) break;
            }
        }
        return _dicPartitionGlobalId;
    }

    internal int? _dicTenantId = null;
    internal string? _dicTenantKey = null; // これは Guid なのか？ オンプレと AC で違うのか？
    internal object _dicTenantIdLock = new();
    internal (int? id, string? key) GetTenantId()
    {
        if (_dicTenantId is not null) return (_dicTenantId, _dicTenantKey);

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
                    if (detailedUser?.TenantId is not null) break;
                }
            }
            catch (Exception ex)
            {
                throw new OrchException(NameColonSeparator, ex);
            }
        }
        return (_dicTenantId, _dicTenantKey);
    }

    #region PersonalWorkspace cache
    public bool DisablePersonalWorkspace(long? userId)
    {
        if (userId is null) return false;

        try
        {
            var users = GetUsers();

            var owner = users.FirstOrDefault(o => o.Id == userId);
            if (owner is not null)
            {
                // ここはキャッシュをピンポイントで削除できるので、消しておくか。
                // 頻繁に実行する処理ではないし、この方が安全だよね。。
                _dicUsersDetailed?.TryRemove(owner.Id!.Value, out _);

                var detailedOwner = GetUser(owner);

                // もし GetUser() に失敗したら、このエンティティを安全に更新できないため、更新しない
                if (detailedOwner is null) return false;

                if (detailedOwner.MayHavePersonalWorkspace.GetValueOrDefault())
                {
                    var postingUser = OrchCollectionExtensions.DeepCopy(detailedOwner);
                    if (postingUser.UnattendedRobot is not null)
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

        if (_dicEntitiesSummary is null)
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
                if (summary is not null)
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
        if (_dicLicenseNamedUser is null)
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
        if (_dicLicenseRuntime is null)
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

    #region OrchTrigger cache
    // key: foldlerId
    internal ConcurrentDictionary<Int64, ConcurrentDictionary<Int64, ProcessSchedule>>? _dicTriggers = null;
    internal ExceptionsCachePer<Int64> _dicTriggers_Exceptions = new();
    public ICollection<ProcessSchedule> GetTriggers(Folder folder)
    {
        // ApiVersion == 11.1 のとき、個人用ワークスペースにはトリガーがないことを確認済み
        if (OrchAPISession.ApiVersion < 12 && folder.FolderType == "Personal") return [];

        _dicTriggers_Exceptions.ThrowCachedExceptionIfAny(folder.Id ?? 0);

        if (_dicTriggers is null)
        {
            lock (_dicTriggers_Exceptions)
            {
                _dicTriggers ??= [];
            }
        }
        if (!_dicTriggers.TryGetValue(folder.Id ?? 0, out var triggersPerFolder))
        {
            triggersPerFolder = [];
            _dicTriggers[folder.Id ?? 0] = triggersPerFolder;
            try
            {
                var triggers = OrchAPISession.GetProcessSchedules(folder.Id ?? 0).ToList();
                string folderPath = folder.GetPSPath();
                foreach (var trigger in triggers)
                {
                    trigger.Path = folderPath;
                    triggersPerFolder[trigger.Id!.Value] = trigger;
                }
            }
            catch (HttpResponseException ex)
            {
                _dicTriggers_Exceptions.CacheException(folder.Id ?? 0, ex);
                throw;
            }
        }
        return triggersPerFolder.Values;
    }
    #endregion

    internal ConcurrentDictionary<Int64, ConcurrentDictionary<Int64, ProcessSchedule>>? _dicTriggersDetailed = null;
    internal ExceptionsCachePer<(Int64, Int64)> _dicTriggersDetailed_Exceptions = new();
    public ProcessSchedule? GetTrigger(Folder folder, ProcessSchedule schedule)
    {
        _dicTriggersDetailed_Exceptions.ThrowCachedExceptionIfAny((folder.Id ?? 0, schedule.Id ?? 0));

        if (_dicTriggersDetailed is null)
        {
            lock (_dicTriggersDetailed_Exceptions)
            {
                _dicTriggersDetailed ??= [];
            }
        }

        if (!_dicTriggersDetailed.TryGetValue(folder.Id ?? 0, out var detailedTriggersPerFolder))
        {
            lock (folder)
            {
                if (!_dicTriggersDetailed.TryGetValue(folder.Id ?? 0, out detailedTriggersPerFolder))
                {
                    detailedTriggersPerFolder = [];
                    _dicTriggersDetailed[folder.Id!.Value] = detailedTriggersPerFolder;
                }
            }
        }

        if (!detailedTriggersPerFolder.TryGetValue(schedule.Id!.Value, out var detailedTrigger))
        {
            try
            {
                detailedTrigger = OrchAPISession.GetProcessSchedule(folder.Id ?? 0, schedule.Id!.Value);
                if (detailedTrigger is not null)
                {
                    detailedTrigger.Path = folder.GetPSPath();
                    detailedTriggersPerFolder[schedule.Id!.Value] = detailedTrigger;
                    detailedTrigger.ExecutorRobots = OrchAPISession.GetRobotIdsForSchedule(folder.Id ?? 0, schedule.Id!.Value)?
                        .Select(id => new RobotExecutor()
                        {
                            Id = id
                        }).ToArray();

                    if (_dicTriggers is not null && _dicTriggers.TryGetValue(folder.Id!.Value, out var triggersPerFolder))
                    {
                        triggersPerFolder[schedule.Id!.Value] = detailedTrigger;
                    }
                }
            }
            catch (HttpResponseException ex)
            {
                _dicTriggersDetailed_Exceptions.CacheException((folder.Id ?? 0, schedule.Id ?? 0), ex);
                throw;
            }
        }
        return detailedTrigger;
    }

    #region OrchAsset cache

    // key: (folderId, assetId)
    // assetId だけで一意になりそうだけど、念のため folderId もキーに含めておく。
    internal ConcurrentDictionary<(Int64 folderId, Int64 assetId), AccessibleFoldersDto?>? _dicAssetLinks = null;
    internal readonly ExceptionsCachePer<(Int64, Int64)> _dicAssetLinks_Exception = new();
    public AccessibleFoldersDto? GetFoldersForAsset(Folder folder, Asset asset)
    {
        _dicAssetLinks_Exception.ThrowCachedExceptionIfAny((folder.Id ?? 0, asset.Id ?? 0));

        if (_dicAssetLinks is null)
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

                if (folderShare is not null && folderShare.AccessibleFolders is not null)
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

    #region OrchProcess cache
    // Key: folderId, release.Id
    internal ConcurrentDictionary<Int64, ConcurrentDictionary<Int64, Release>>? _dicReleases = null;
    internal ExceptionsCachePer<Int64> _dicReleases_Exceptions = new();
    public ICollection<Release> GetReleases(Folder folder)
    {
        _dicReleases_Exceptions.ThrowCachedExceptionIfAny(folder.Id ?? 0);

        if (_dicReleases is null)
        {
            lock (_dicReleases_Exceptions)
            {
                _dicReleases ??= [];
            }
        }
        if (!_dicReleases.TryGetValue(folder.Id!.Value, out var releasesPerFolder))
        {
            // TODO: このへん、何か最適化されていないようだ？
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

    internal ConcurrentDictionary<Int64, ConcurrentDictionary<Int64, Release>>? _dicReleasesDetailed = null;
    internal ExceptionsCachePer<(Int64, Int64)> _dicReleasesDetailed_Exceptions = new();
    public Release? GetReleaseById(Folder folder, Int64 releaseId)
    {
        _dicReleasesDetailed_Exceptions.ThrowCachedExceptionIfAny((folder.Id!.Value, releaseId));

        if (_dicReleasesDetailed is null)
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
            if (release is not null)
            {
                release.Path = folder.GetPSPath();
                releasesDetailedPerFolder[release.Id!.Value] = release;
                if (_dicReleases is not null && _dicReleases.TryGetValue(folder.Id!.Value, out var releasesPerFolder))
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

    #region OrchLibraryVersion cache
    // key: Id
    internal ConcurrentDictionary<string, List<LibraryVersion>>? _dicLibraryVersions = null;
    public ReadOnlyCollection<LibraryVersion> GetLibraryVersions(string libraryId)
    {
        if (_dicLibraryVersions is null)
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
            _dicLibraryVersions[libraryId] = libraryVersions;
        }
        return libraryVersions.AsReadOnly();
    }
    #endregion

    #region OrchLibraryVersion cache
    // key: Id
    internal ConcurrentDictionary<string, List<LibraryVersion>>? _dicLibraryVersionsInHostFeed = null;
    public ReadOnlyCollection<LibraryVersion> GetLibraryVersionsInHostFeed(string libraryId)
    {
        if (_dicLibraryVersionsInHostFeed is null)
        {
            lock (this)
            {
                _dicLibraryVersionsInHostFeed ??= new();
            }
        }

        if (!_dicLibraryVersionsInHostFeed.TryGetValue(libraryId, out List<LibraryVersion> libraryVersions))
        {
            libraryVersions = OrchAPISession.GetLibraryVersions(libraryId, LibraryHostFeedId)
                .OrderBy(v => v.Version!, VersionComparer.Instance)
                .ToList();
            string path = NameColonSeparator;
            foreach (var library in libraryVersions)
            {
                //library.Name = $"{library.Id}.{library.Version}.nupkg";
                library.Path = path;
            }
            _dicLibraryVersionsInHostFeed[libraryId] = libraryVersions;
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
        string feedId = FolderFeedId.Get(folder) ?? "";
        _dicPackages_Exceptions.ThrowCachedExceptionIfAny(feedId);

        if (_dicPackages is null)
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
        string feedId = FolderFeedId.Get(folder) ?? "";
        _dicPackages_Exceptions.ThrowCachedExceptionIfAny(feedId);

        if (_dicPackageVersions is null)
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

        if (_dicPackageEntryPoint is null)
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

    #region OrchMachineClientSecret cache
    internal Dictionary<string, MachineClientSecretResponse[]?>? _dicMachineClientSecrets = null;
    internal readonly ExceptionsCachePer<string> _dicMachineClientSecrets_Exception = new();
    public MachineClientSecretResponse[]? GetMachineClientSecret(string licenseKey)
    {
        _dicMachineClientSecrets_Exception.ThrowCachedExceptionIfAny(licenseKey);

        if (_dicMachineClientSecrets is null || !_dicMachineClientSecrets.TryGetValue(licenseKey, out var secrets))
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

    // key: (folderId, assetId)
    // bucketId だけで一意になりそうだけど、念のため folderId もキーに含めておく。
    internal ConcurrentDictionary<(Int64 folderId, Int64 bucketId), AccessibleFoldersDto?>? _dicQueueLinks = null;
    public AccessibleFoldersDto? GetFoldersForQueue(Folder folder, QueueDefinition queue)
    {
        if (_dicQueueLinks is null)
        {
            lock (this)
            {
                _dicQueueLinks ??= new ConcurrentDictionary<(Int64 folderId, Int64 queueId), AccessibleFoldersDto?>();
            }
        }

        if (!_dicQueueLinks.TryGetValue((folder.Id ?? 0, queue.Id ?? 0), out var folderShare))
        {
            folderShare = OrchAPISession.GetFoldersForQueue(folder.Id ?? 0, queue.Id ?? 0);

            if (folderShare is not null && folderShare.AccessibleFolders is not null)
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

    #region OrchQueueItem cache
    // key: folderId, <queueName, <queueItemId>>
    internal ConcurrentDictionary<Int64, Dictionary<string, Dictionary<Int64, QueueItem>>>? _dicQueueItems = null;
    public List<QueueItem> GetQueueItems(Folder folder, QueueDefinition queue, string filter, ulong skip, ulong first, string? orderBy = null, bool orderAscending = false)
    {
        if (_dicQueueItems is null)
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
            item.PathName = queue.GetPSPath();
            item.Name = queue.Name;
            if (item.Id.HasValue)
            {
                queueItemsPerQueue[item.Id.Value] = item;
            }
        }

        return items;
    }

    public QueueItem? GetQueueItemById(Folder folder, QueueDefinition queue, Int64 id)
    {
        var item = OrchAPISession.GetQueueItemById(folder.Id!.Value, id);
        if (item is not null)
        {
            item.Path = folder.GetPSPath();
            item.PathName = queue.GetPSPath();
            item.Name = queue.Name;

            if (_dicQueueItems is null)
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

            queueItemsPerQueue[item.Id!.Value] = item;
        }
        return item;
    }
    #endregion

    #region OrchTestSetExecution cache
    // Key: <folderId, <TestSetExecutionId, TestSetExecution>>
    internal ConcurrentDictionary<Int64, ConcurrentDictionary<Int64, TestSetExecution>>? _dicTestSetExecutions = null;
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

        if (_dicTestSetExecutions is null)
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

    #region OrchBucket cache

    // key: (folderId, bucketId)
    // bucketId だけで一意になりそうだけど、念のため folderId もキーに含めておく。
    internal ConcurrentDictionary<(Int64 folderId, Int64 bucketId), AccessibleFoldersDto?>? _dicBucketLinks = null;
    internal ExceptionsCachePer<(Int64, Int64)> _dicBucketLinks_Exceptions = new();
    public AccessibleFoldersDto? GetFoldersForBucket(Folder folder, Bucket bucket)
    {
        _dicBucketLinks_Exceptions.ThrowCachedExceptionIfAny((folder.Id ?? 0, bucket.Id ?? 0));

        if (_dicBucketLinks is null)
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

                if (folderShare is not null && folderShare.AccessibleFolders is not null)
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

    #endregion

    #region OrchCalendar cache

    // Key: calenderId
    internal ConcurrentDictionary<long, ExtendedCalendar>? _dicCalendars = null;
    internal readonly ExceptionCachePerTenant _dicCalendars_Exceptions = new();
    public ICollection<ExtendedCalendar>? GetCalendars()
    {
        _dicCalendars_Exceptions.ThrowCachedExceptionIfAny();

        if (_dicCalendars is null)
        {
            lock (_dicCalendars_Exceptions)
            {
                _dicCalendars ??= [];
                try
                {
                    var calendars = OrchAPISession.GetCalendars()?.ToList();
                    if (calendars is not null)
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
        if (_dicCalendars is null)
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
        if (calendar is null)
        {
            //_dicCalendars[calendarId] = null; // null をコレクションに入れると後が面倒なので、キャッシュはしないでおく。。
            return null;
        }
        calendar.Path = NameColonSeparator;
        _dicCalendars[calendar.Id!.Value] = calendar;
        return calendar;
    }

    #endregion

    #region OrchExecutionSettings Cache
    // key: scope
    internal Dictionary<int, ExecutionSettingDefinition[]?>? _dicExecutionSettings = null;
    internal readonly ExceptionsCachePer<int> _dicExecutionSettingsException = new();
    public ExecutionSettingDefinition[]? GetExecutionSettings(int scope, string strScope)
    {
        _dicExecutionSettingsException.ThrowCachedExceptionIfAny(scope);

        if (_dicExecutionSettings is null)
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
                if (executionConf is not null)
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

    internal HashSet<PmAuditLog>? _dicPmAuditLogs = null;
    internal readonly ExceptionCachePerTenant _dicPmAuditLogs_Exception = new();
    public ReadOnlyCollection<PmAuditLog> GetPmAuditLog(string? query, ulong skip, ulong first)
    {
        // こいつはマルチスレッドを考慮する必要はないはずだが、念のため。。
        if (_dicPmAuditLogs is null)
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
    internal ConcurrentDictionary<string, PmDirectoryEntityInfo[]?>? _dicSearchPmDirectory = null;
    internal readonly ExceptionsCachePer<string> _dicSearchPmDirectory_Exception = new();
    public PmDirectoryEntityInfo[]? SearchPmDirectory(string key)
    {
        key = key.ToLower();
        _dicSearchPmDirectory_Exception.ThrowCachedExceptionIfAny(key);

        if (_dicSearchPmDirectory is null)
        {
            lock (_dicSearchPmDirectory_Exception)
            {
                _dicSearchPmDirectory ??= [];
            }
        }
        if (!_dicSearchPmDirectory.TryGetValue(key, out PmDirectoryEntityInfo[] ret))
        {
            try
            {
                var partitionGlobalId = GetPartitionGlobalId();
                ret = OrchAPISession.SearchPmDirectory(partitionGlobalId!, key);
                foreach (var user in ret ?? [])
                {
                    user.Path = NameColonSeparator;
                }
                _dicSearchPmDirectory[key] = ret;
            }
            catch (HttpResponseException ex)
            {
                _dicSearchPmDirectory_Exception.CacheException(key, ex);
                throw;
            }
        }
        return ret;
    }

    // key: (name, kind)
    // kind: "user", "group", or "application" ロボットは検索できないようだ。
    // unresolvedList は出力パラメータ。解決できなかった名前を、元の T のリストで返す。
    internal ConcurrentDictionary<(string, string), PmGroupMember?>? _dicPmBulkResolveByName = null;
    internal readonly ExceptionCachePerTenant _dicPmBulkResolveByName_Exception = new();
    public Dictionary<string, PmGroupMember?> PmBulkResolveByName<T>(
        string kind, IEnumerable<T> users, 
        Func<T, string> getSearchKeyFunc,
        List<T>? unresolvedList = null)
    {
        if (!users.Any())
        {
            return [];
        }

        _dicPmBulkResolveByName_Exception.ThrowCachedExceptionIfAny();

        if (_dicPmBulkResolveByName is null)
        {
            lock (_dicPmBulkResolveByName_Exception)
            {
                _dicPmBulkResolveByName ??= [];
            }
        }

        // まだ問い合わせていない names or emails の一覧を作成
        List<T> needQueryUsers = users
            .Where(user => !string.IsNullOrEmpty(getSearchKeyFunc(user)))
            .Where(user => !_dicPmBulkResolveByName.ContainsKey((kind, getSearchKeyFunc(user))))
            .ToList();

        if (needQueryUsers.Count != 0)
        {
            try
            {
                var partitionGlobalId = GetPartitionGlobalId();
                foreach (var chunk in needQueryUsers.Chunk(20))
                {
                    var result = OrchAPISession.PmBulkResolveByName(partitionGlobalId!, kind,
                        chunk.Select(u => getSearchKeyFunc(u)));

                    foreach (var kvp in result ?? [])
                    {
                        if (kvp.Value is not null)
                        {
                            kvp.Value.Path = NameColonSeparator;
                        }
                        _dicPmBulkResolveByName.AddOrUpdate((kind, kvp.Key), kvp.Value, (key, oldValue) => kvp.Value);
                    }
                }
            }
            catch (HttpResponseException ex)
            {
                _dicPmBulkResolveByName_Exception.CacheException(ex);
                throw;
            }
        }

        // キャッシュをそのまま返すのではなく、今回の問い合わせの対象（users）の結果だけを返す
        Dictionary<string, T> dicList = [];

        // unresolvedList が渡されている場合には、このリストを作成する。
        // users を高速に検索できるように、辞書にしておく。
        if (unresolvedList is not null)
        {
            dicList = users.ToDictionary(u => getSearchKeyFunc(u), u => u, StringComparer.OrdinalIgnoreCase);

            // 検索キーが null のものは、未解決リストに追加する
            foreach (var user in users)
            {
                if (string.IsNullOrEmpty(getSearchKeyFunc(user)))
                {
                    unresolvedList.Add(user);
                }
            }
        }

        Dictionary<string, PmGroupMember?> ret = [];
        foreach (var user in users)
        {
            string key = getSearchKeyFunc(user);
            if (string.IsNullOrEmpty(key))
            {
                continue;
            }
            // すべて辞書に入っているはず。。見つからなかった名前には null が入っている。
            if (_dicPmBulkResolveByName.TryGetValue((kind, key), out PmGroupMember value))
            {
                ret[key] = value;
                if (value is null && dicList is not null)
                {
                    // これも必ず辞書に入っているはず。。
                    if (dicList.TryGetValue(key, out var unresolvedEntry))
                    {
                        unresolvedList!.Add(unresolvedEntry);
                    }
                }
            }
        }

        return ret;
    }

    // key: name
    internal ConcurrentDictionary<string, DirectoryObject[]?>? _dicSearchDirectory = null;
    internal readonly ExceptionsCachePer<string>_dicSearchDirectory_Exception = new();
    public IEnumerable<DirectoryObject> SearchDirectory(string name)
    {
        // この API は、'+' を含むユーザー名を検索できないようだ。
        // 念のため、+ のほか '-' と '_' についても検索ワードから除いて処理する
        int index = name.IndexOfAny(['+', '-', '_']);
        string searchWord = index >= 0 ? name.Substring(0, index) : name;

        _dicSearchDirectory_Exception.ThrowCachedExceptionIfAny(searchWord);

        if (_dicSearchDirectory is null)
        {
            lock (_dicSearchDirectory_Exception)
            {
                _dicSearchDirectory ??= [];
            }
        }
        if (!_dicSearchDirectory.TryGetValue(searchWord, out var value))
        {
            try
            {
                value = OrchAPISession.SearchDirectory(searchWord);
                foreach (var v in value ?? [])
                {
                    v.Path = NameColonSeparator;
                }
                _dicSearchDirectory[searchWord] = value;
            }
            catch (HttpResponseException ex)
            {
                _dicSearchDirectory_Exception.CacheException(searchWord, ex);
                throw;
            }
        }

        return value?.Where(obj => obj.identityName?.StartsWith(name, StringComparison.OrdinalIgnoreCase) ?? false) ?? [];
    }

    #region PmGroup Cache

    internal BulkCreateResponse? CreatePmUserBulk(CreateUsersCommand cmd)
    {
        var ret = OrchAPISession.CreatePmUserBulk(cmd);

        PmUsers.ClearCache();

        foreach (var groupId in cmd.groupIDs ?? [])
        {
            PmGroups.ClearCache(groupId);
        }

        _dicSearchDirectory = null;
        _dicSearchDirectory_Exception.ClearCache();
        _dicSearchPmDirectory = null;
        _dicSearchPmDirectory_Exception.ClearCache();

        return ret;
    }

    internal PmGroup? CreatePmGroup(string? name, IEnumerable<string>? memberIds = null)
    {
        CreateGroupCommand createGroupCommand = new()
        {
            partitionGlobalId = GetPartitionGlobalId(),
            id = Guid.NewGuid().ToString(),
            name = name,
            directoryUserMemberIDs = memberIds?.ToArray()
        };

        var newGroup = OrchAPISession.CreatePmGroup(createGroupCommand);
        if (newGroup is not null)
        {
            newGroup.Path = NameColonSeparator; // キャッシュに入れる必要はないが、このメソッドが返す PmGroup に設定するのは必要だ。
            _dicSearchDirectory = null;
            _dicSearchDirectory_Exception.ClearCache();
            _dicSearchPmDirectory = null;
            _dicSearchPmDirectory_Exception.ClearCache();

            // PmGroup のキャッシュを削除すると、直後に PmGroups.Get() を呼び出したとき、この戻り値に
            // 作成したばかりの PmGroup が含まれない場合があるようだ。
            // そのため、ここではキャッシュを削除せず、キャッシュを更新するようにしておく。
            // CreateXxx() が正しい結果を返さないことがあったので、一貫したキャッシュを構築するため
            // CreateXxx() を呼び出した後は、キャッシュをクリアするようにしていたのだけど、ちと困るな。。
            PmGroups.Set(newGroup);
        }
        return newGroup;
    }

    internal PmRobotAccount? CreatePmRobot(CreateRobotAccountCommand cmd)
    {
        var ret = OrchAPISession.CreatePmRobot(cmd);
        if (ret is not null)
        {
            ret.Path = NameColonSeparator;
        }
        PmRobotAccounts.ClearCache();
        foreach (var groupId in cmd.groupIDsToAdd ?? [])
        {
            PmGroups.ClearCache(groupId);
        }

        _dicSearchDirectory = null;
        _dicSearchDirectory_Exception.ClearCache();
        _dicSearchPmDirectory = null;
        _dicSearchPmDirectory_Exception.ClearCache();
        return ret;
    }

    internal PmGroup? AddMemberToPmGroup(string? groupId, string? groupName, IEnumerable<string?>? memberIds)
    {
        if (groupId is null) return null;
        UpdateGroupCommand updateGroupCommand = new()
        {
            partitionGlobalId = GetPartitionGlobalId(),
            name = groupName,
            directoryUserIDsToAdd = memberIds?
                .Where(id => !string.IsNullOrEmpty(id))
                .Select(id => id!)
                .ToList() ?? [],
            directoryUserIDsToRemove = []
        };
        var ret = OrchAPISession.PutPmGroup(groupId, updateGroupCommand);
        if (ret is not null)
        {
            ret.Path = NameColonSeparator;
        }
        PmGroups.ClearCache(groupId);

        PmUsers.ClearCache(); // 内部にグループIDを含む
        PmRobotAccounts.ClearCache(); // 内部にグループIDを含む
        // PmExternalClients.ClearCache(); // この中にグループIDはない

        return ret;
    }

    internal PmGroup? RemoveMemberFromPmGroup(string? groupId, string? groupName, IEnumerable<string?>? memberIds)
    {
        if (groupId is null) return null;
        UpdateGroupCommand updateGroupCommand = new()
        {
            partitionGlobalId = GetPartitionGlobalId(),
            name = groupName,
            directoryUserIDsToAdd = [],
            directoryUserIDsToRemove = memberIds?
                .Where(id => !string.IsNullOrEmpty(id))
                .Select(id => id!)
                .ToList() ?? []
        };
        var ret = OrchAPISession.PutPmGroup(groupId, updateGroupCommand);
        if (ret is not null)
        {
            ret.Path = NameColonSeparator;
        }
        PmGroups.ClearCache(ret?.id);

        PmUsers.ClearCache(); // 内部にグループIDを含む
        PmRobotAccounts.ClearCache(); // 内部にグループIDを含む
        // PmExternalClients.ClearCache(); // この中にグループIDはない

        return ret;
    }

    #endregion

    internal ConcurrentDictionary<string, AvailableUserBundles>? _dicPmAvailableUserBundles = null;
    internal readonly ExceptionsCachePer<string> _dicPmAvailableUserBundles_Exceptions = new();
    //public AvailableUserBundles? GetPmUserLicenseGroupsAvailableLicenses(NuLicensedGroup? group)
    public AvailableUserBundles? GetPmUserLicenseGroupsAvailableLicenses(string? groupId, string groupName)
    {
        if (groupId is null) return null;

        _dicPmAvailableUserBundles_Exceptions.ThrowCachedExceptionIfAny(groupId);

        if (_dicPmAvailableUserBundles is null)
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
                if (ret is not null)
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

        if (_dicPmUserLicenseGroupAllocations is null)
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

    #region PmAuthenticationSetting cache
    internal PmAuthenticationRoot? _dicPmAuthenticationSetting = null;
    internal readonly ExceptionCachePerTenant _dicPmAuthenticationSetting_Exception = new();
    public PmAuthenticationRoot? GetPmAuthenticationSettings()
    {
        _dicPmAuthenticationSetting_Exception.ThrowCachedExceptionIfAny();

        if (_dicPmAuthenticationSetting is null)
        {
            lock (_dicPmAuthenticationSetting_Exception)
            {
                if (_dicPmAuthenticationSetting is null)
                {
                    var partitionGlobalId = GetPartitionGlobalId();

                    try
                    {
                        _dicPmAuthenticationSetting = OrchAPISession.GetPmAuthenticationSettings(partitionGlobalId!);
                        if (_dicPmAuthenticationSetting is not null)
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

    private string? _libraryHostFeedId;
    internal string? LibraryHostFeedId
    {
        get
        {
            if (string.IsNullOrEmpty(_libraryHostFeedId))
            {
                _libraryHostFeedId = LibraryFeeds.Get()?.FirstOrDefault(lf => lf.isShared)?.id;
            }
            return _libraryHostFeedId;
        }
    }

    // 組織のリストエンティティ
    public readonly ListCachePerOrganization<PmUser> PmUsers;
    public readonly ListCachePerOrganization<PmGroup> PmGroups;
    public readonly ListCachePerOrganization<PmRobotAccount> PmRobotAccounts;
    public readonly ListCachePerOrganization<ExternalClient> PmExternalClients;
    public readonly ListCachePerOrganization<ExternalResource> PmExternalApiResources;

    // これらはドライブごとに保持する必要があるため、 Cache クラスの static メンバにはできない
    internal readonly List<ITenantCacheClearable> _allTenantCache = [];
    internal readonly List<IFolderCacheClearable> _allFolderCache = [];

    // インデックスなしのテナントエンティティ
    public readonly SingleCachePerTenant<ActivitySettings> ActivitySettings;
    public readonly SingleCachePerTenant<string[]> AvailableVersions;
    public readonly SingleCachePerTenant<ODataValueOfString> ConnectionString;
    public readonly SingleCachePerTenant<UpdateSettings> UpdateSettings;
    public readonly SingleCachePerTenant<License> LicenseSettings;
    public readonly SingleCachePerTenant<LibraryFeed[]> LibraryFeeds;

    // インデックスつきテナントエンティティ
    public readonly IndexedCachePerTenant<User, UserPrivilege> UserPrivileges;

    // テナントのリストエンティティ
    public readonly ListCachePerTenant<ResponseDictionaryItem> AuthenticationSettings;
    public readonly ListCachePerTenant<CredentialStore> CredentialStores;
    public readonly ListCachePerTenant<Robot> Robots;
    public readonly ListCachePerTenant<Role> Roles;
    public readonly ListCachePerTenant<Library> LibrariesInTenant;
    public readonly ListCachePerTenant<Library> LibrariesInHost;
    public readonly ListCachePerTenant<ExtendedMachine> Machines;
    public readonly ListCachePerTenant<ExtendedRobot> AllRobotsAcrossFolders;
    public readonly ListCachePerTenant<MachineSessionRuntime> MachineSessionRuntimes;
    public readonly ListCachePerTenant<PersonalWorkspace> PersonalWorkspaces;
    public readonly ListCachePerTenant<Settings> Settings;
    public readonly ListCachePerTenant<Webhook> Webhooks;
    public readonly ListCachePerTenant<ResponseDictionaryItem> WebSettings;
    public readonly ListCachePerTenant<NuLicensedGroup> PmLicensedGroups;
    public readonly ListCachePerTenant<NuLicensedUser> PmLicensedUsers;

    // インデックスなしのフォルダーエンティティ
    public readonly SingleCachePerFolder<string> FolderFeedId;

    public readonly ListCachePerFolder<TaskCatalog> ActionCatalogs;
    public readonly ListCachePerFolder<HttpTrigger> ApiTriggers;
    public readonly ListCachePerFolder<Asset> Assets;
    public readonly ListCachePerFolder<Bucket> Buckets;
    public readonly ListCachePerFolder<Entities.Environment> Environments;
    public readonly ListCachePerFolder<MachineFolder> FolderMachines;
    public readonly ListCachePerFolder<MachineFolder> FolderMachinesAssigned;
    public readonly ListCachePerFolder<MachineFolder> FolderMachinesAssignable;
    public readonly ListCachePerFolder<UserRoles> FolderUsersWithNoInherited;
    public readonly ListCachePerFolder<UserRoles> FolderUsersWithInherited;
    public readonly ListCachePerFolder<MachineSessionRuntime> MachineSessionRuntimesByFolder;
    public readonly ListCachePerFolder<SimpleUser> Reviewers;
    public readonly ListCachePerFolder<QueueDefinition> Queues;
    public readonly ListCachePerFolder<RobotsFromFolderModel> RobotsFromFolder;
    public readonly ListCachePerFolder<Session> Sessions;
    public readonly ListCachePerFolder<TestCaseDefinition> TestCases;
    public readonly ListCachePerFolder<TestCaseExecution> TestCaseExecutions;
    public readonly ListCachePerFolder<TestDataQueue> TestDataQueues;
    //public readonly ListCachePerFolder<TestDataQueueItem> TestDataQueueItems;
    public readonly ListCachePerFolder<TestSet> TestSets;
    public readonly ListCachePerFolder<TestSetSchedule> TestSetSchedules;
    public readonly ListCachePerFolder<UserRobots> UserRobots;
    public readonly ListCachePerFolder<MachineRuntime> RuntimesForFolder;

    // インデックスつきフォルダーエンティティ
    public readonly IndexedListCachePerFolder<MachineFolder, ExtendedRobot> FolderRobots;
    public readonly IndexedListCachePerFolder<MachineFolder, RobotUser> MachinesRobots;
    public readonly IndexedListCachePerFolder<Bucket, BlobFile> BucketFiles;
    public readonly IndexedListCachePerFolder<TestDataQueue, TestDataQueueItem> TestDataQueueItems;

    //public readonly CachePerFolder<Release> Processes; // これはインデックスがついてた。。

    // このコンストラクタを実行するタイミングでは、NameColonSeparator は利用できない
    public OrchDriveInfo(ProviderInfo provider, PSDrive drive) :
        base(drive.Name, provider, drive.Name + ':' + Path.DirectorySeparatorChar, drive.Description, null, drive.Root)
    {
        _psDrive = drive;
        _psDrive.Root = _psDrive.Root?.TrimEnd('/');
        RootFolder = new Folder() { DisplayName = "", FullyQualifiedName = "", Path = NameColonSeparator };

        // キャッシュを初期化

        // 組織のリストエンティティ
        PmUsers                = new(this, OrchAPISession.GetPmUsers,                 e => e.Path = NameColonSeparator);
        PmGroups               = new(this, OrchAPISession.GetPmGroups,                e =>
            {
                e.Path = NameColonSeparator;
                foreach (var m in e.members ?? [])
                {
                    m.Path = NameColonSeparator;
                    m.PathGroupName = NameColonSeparator + e.name;
                }
            },
                                e => e.id, OrchAPISession.GetPmGroup);

        PmRobotAccounts        = new(this, OrchAPISession.GetPmRobotAccounts,         e => e.Path = NameColonSeparator);
        PmExternalClients      = new(this, OrchAPISession.GetPmExternalClients,       e => e.Path = NameColonSeparator);
        PmExternalApiResources = new(this, OrchAPISession.GetPmExternalApiResource,   e => e.Path = NameColonSeparator);

        // インデックスなしのテナントエンティティ
        ActivitySettings       = new(this, OrchAPISession.GetActivitySettings,        e => e.Path = NameColonSeparator);
        ConnectionString       = new(this, OrchAPISession.GetConnectionString,        e => e.Path = NameColonSeparator);
        LicenseSettings        = new(this, OrchAPISession.GetLicenseSettings,         e => e.Path = NameColonSeparator);
        MachineSessionRuntimes = new(this, OrchAPISession.GetMachineSessionRuntimes,  e => e.Path = NameColonSeparator);

        RuntimesForFolder      = new(this, OrchAPISession.GetRuntimesForFolder);
        AllRobotsAcrossFolders = new(this, OrchAPISession.FindAllRobotsAcrossFolders, e => e.Path = NameColonSeparator);
        PersonalWorkspaces     = new(this, OrchAPISession.GetPersonalWorkspaces,      e => e.Path = NameColonSeparator);
        Roles                  = new(this, OrchAPISession.GetRoles,                   e => e.Path = NameColonSeparator);

        LibrariesInTenant      = new(this, () => OrchAPISession.GetLibraries(null),   e => e.Path = NameColonSeparator);
        LibrariesInHost        = new(this, () => OrchAPISession.GetLibraries(LibraryHostFeedId), e => e.Path = NameColonSeparator);

        Settings               = new(this, OrchAPISession.GetSettings,                e => e.Path = NameColonSeparator);
        UpdateSettings         = new(this, OrchAPISession.GetUpdateSettings,          e => e.Path = NameColonSeparator);
        Webhooks               = new(this, OrchAPISession.GetWebhooks,                e => e.Path = NameColonSeparator);
        LibraryFeeds           = new(this, OrchAPISession.GetLibraryFeeds, null);

        Robots = new(this, () =>
            {
                if (OrchAPISession.ApiVersion >= 12)
                {
                    return OrchAPISession.GetRobots();
                }
                else
                {
                    // 11.1 では、ロボット一覧取得で API call が発生していなかった。。ユーザーからそれっぽいのを作ってみる。
                    // TODO: 12 以降ではどうか？
                    var users = GetUsers();
                    foreach (var user in users)
                    {
                        GetUser(user);
                    }
                    return users.Select(u => new Robot()
                    {
                        // TODO: ここ未完成。なんちゃって実装。
                        Path = NameColonSeparator,
                        Id = u.UnattendedRobot?.RobotId ?? u.RobotProvision?.RobotId,
                        Type = u.RobotProvision?.RobotType,
                        Username = u.UnattendedRobot?.UserName ?? u.RobotProvision?.UserName,
                        User = u
                    });
                }
            },
            e => e.Path = NameColonSeparator);

        AvailableVersions = new(this, () =>
            {
                var av = OrchAPISession.GetAvailableVersions();
                return av?.availableVersions;
            }
        );

        // 現行の実装では、必ず GetCredentialStore を呼び出している。
        // Get-OrchCredentialStore cmdlet に、-ExpandDetails parameter を実装して、この呼び出しは分離すべきかもしれない。
        CredentialStores = new(this, () =>
            {
                var stores = OrchAPISession.GetCredentialStores();
                var results = ParallelResults.ForEach(stores, store => OrchAPISession.GetCredentialStore(store.Id!.Value));
                List<CredentialStore> ret = [];
                foreach (var result in results)
                {
                    if (result.Result is not null) ret.Add(result.Result);
                }
                return ret;
            },
            store => store.Path = NameColonSeparator
        );

        AuthenticationSettings = new(this, () =>
            {
                var dic = OrchAPISession.GetAuthenticationSettings();
                var list = dic?.Keys?.Zip(dic.Values ?? [], (key, value) => new ResponseDictionaryItem()
                {
                    Path = NameColonSeparator,
                    Key = key,
                    Value = value
                });
                return list ?? [];
            }
        );

        // 15 の web interface で &$expand=UpdateInfo が付与されていることを確認済み
        Machines = new(this,
            OrchAPISession.ApiVersion >= 12
                ? () => OrchAPISession.GetMachines("&$expand=UpdateInfo")
                : () => OrchAPISession.GetMachines(null),
            machine =>
            {
                machine.Path = NameColonSeparator;
                string pathMachine = NameColonSeparator + machine.Name;
                foreach (var robotUser in machine.RobotUsers ?? [])
                {
                    robotUser.Path = machine.Path;
                    robotUser.Machine = machine.Name;
                    robotUser.PathMachine = pathMachine;
                }
            }
        );

        WebSettings = new(this, () =>
            {
                var dic = OrchAPISession.GetWebSettings();
                var list = dic?.Keys?.Zip(dic.Values ?? [], (key, value) => new ResponseDictionaryItem()
                {
                    Path = NameColonSeparator,
                    Key = key,
                    Value = value
                });
                return list ?? [];
            }
        );

        PmLicensedGroups = new(this,
            OrchAPISession.GetPmLicensedGroups,
            e =>
            {
                e.Path = NameColonSeparator;
                e.userBundleLicenseNames = e.userBundleLicenses?.Select(b => AvailableUserBundlesItems.Items[b]).ToArray();
            }
        );

        PmLicensedUsers = new(this,
            OrchAPISession.GetPmLicensedUsers,
            e =>
            {
                e.Path = NameColonSeparator;
                e.userBundleLicenseNames = e.userBundleLicenses?.Select(b => AvailableUserBundlesItems.Items[b]).ToArray();
            }
        );

        // インデックスつきのテナントエンティティ
        UserPrivileges = new(this, OrchAPISession.GetUserPrivilege, e => e.Id!.Value, e => e.UserName!,
            (e, userName) => 
            { 
                e.Path = NameColonSeparator;
                e.UserName = userName;
            }
        );

        // インデックスなしのフォルダエンティティ
        // 下記は 11.1 ではエラーになることを確認済み。TODO: feedId はどうやって取得するのか？ 12 以降ではどうか？
        FolderFeedId = new (this, OrchAPISession.GetFolderFeedId, null, 12);
        ActionCatalogs                 = new(this, OrchAPISession.GetTaskCatalogs,       (e, folderPath) => e.Path = folderPath, 16); // 16 でエラーが返らないことを確認済み
        ApiTriggers                    = new(this, OrchAPISession.GetHttpTriggers,       (e, folderPath) => e.Path = folderPath, 18); // 17 で web interface にないことを確認済み (17 で実行してもエラーは返らないようだが、)
        Buckets                        = new(this, OrchAPISession.GetBuckets,            (e, folderPath) => e.Path = folderPath);
        Environments                   = new(this, OrchAPISession.GetEnvironments,       (e, folderPath) => e.Path = folderPath);
        FolderUsersWithNoInherited     = new(this, fid => OrchAPISession.GetUsersForFolder(fid, false), (e, folderPath) => e.Path = folderPath);
        FolderUsersWithInherited       = new(this, fid => OrchAPISession.GetUsersForFolder(fid, true),  (e, folderPath) => e.Path = folderPath);
        MachineSessionRuntimesByFolder = new(this, fid => OrchAPISession.GetMachineSessionRuntimesByFolderId(fid), (e, folderPath) => e.Path = folderPath);
        Queues                         = new(this, OrchAPISession.GetQueues,             (e, folderPath) => e.Path = folderPath);
        Reviewers                      = new(this, OrchAPISession.GetReviewers);
        RobotsFromFolder               = new(this, OrchAPISession.GetRobotsFromFolder,   (e, folderPath) => e.Path = folderPath);
        Sessions                       = new(this, OrchAPISession.GetSessions,           (e, folderPath) => e.Path = folderPath);
        TestCases                      = new(this, OrchAPISession.GetTestCases,          (e, folderPath) => e.Path = folderPath, 18); // 17 で web interface にないことを確認済み
        TestCaseExecutions             = new(this, OrchAPISession.GetTestCaseExecutions, (e, folderPath) => e.Path = folderPath, 18); // 17 で web interface にないことを確認済み
        TestDataQueues                 = new(this, OrchAPISession.GetTestDataQueues,     (e, folderPath) => e.Path = folderPath, 18); // 17 で web interface にないことを確認済み
        TestSets                       = new(this, OrchAPISession.GetTestSets,           (e, folderPath) => e.Path = folderPath, 18); // 17 で web interface にないことを確認済み
        TestSetSchedules               = new(this, OrchAPISession.GetTestSetSchedules,   (e, folderPath) => e.Path = folderPath, 18); // 17 で web interface にないことを確認済み
        UserRobots                     = new(this, OrchAPISession.GetUserRobots);

        Assets = new(this, OrchAPISession.GetAssets, (e, folderPath) =>
        {
            e.Path = folderPath;
            if (e.UserValues is not null)
            {
                string assetPath = Path.Combine(folderPath, e.Name!);
                foreach (var userValue in e.UserValues)
                {
                    userValue.Name = e.Name;
                    userValue.Path = folderPath;
                    userValue.PathName = assetPath;
                }
            }
        });

        FolderMachines = new(this,
            fid => OrchAPISession.GetMachinesAssignedTo(fid),
            (e, folderPath) => e.Path = folderPath
        );

        // 15: ?$filter=((((IsAssignedToFolder eq true) or (IsInherited eq true))))
        FolderMachinesAssigned = new(this,
            OrchAPISession.ApiVersion >= 12
                ? fid => OrchAPISession.GetMachinesAssignedTo(fid, "&$filter=((IsAssignedToFolder eq true) or (IsInherited eq true))")
                : fid => OrchAPISession.GetMachinesAssignedTo(fid, "&$filter=(IsAssignedToFolder eq true)"),
            (e, folderPath) => e.Path = folderPath
        );

        FolderMachinesAssignable = new(this,
            fid => OrchAPISession.GetMachinesAssignedTo(fid, "&$filter=(IsAssignedToFolder eq false)"),
            (e, folderPath) => e.Path = folderPath
        );

        // Processes はインデックスがついているので、下記ではキャッシュを実装できない
        //Processes = new(this,
        //    OrchAPISession.ApiVersion >= 12
        //        ? fid => OrchAPISession.GetReleases(fid, "&$expand=Environment,CurrentVersion,ReleaseVersions,EntryPoint")
        //        : fid => OrchAPISession.GetReleases(fid, "&$expand=Environment,CurrentVersion,ReleaseVersions"),
        //    (e, folderPath) => e.Path = folderPath
        //);

        // インデックスつきのフォルダーエンティティ
        BucketFiles = new(this, OrchAPISession.GetBucketFiles,
            bucket => bucket.Id!.Value,
            bucket => bucket.Name!,
            (bucketItem, folderPath, bucketName, entityPath) =>
            {
                bucketItem.Path = folderPath;
                bucketItem.Bucket = bucketName;
                bucketItem.PathBucket = entityPath;
            }
        );

        FolderRobots = new(this, OrchAPISession.GetFolderRobots,
            machineFolder => machineFolder.Id!.Value,
            machineFolder => machineFolder.Name!,
            (folderRobot, folderPath, name, entityPath) =>
            {
                folderRobot.Path = folderPath;
                folderRobot.Machine = name;
            }
        );

        MachinesRobots = new(this, OrchAPISession.GetMachineRobots,
            machineFolder => machineFolder.Id!.Value,
            machineFolder => machineFolder.Name!,
            (machineRobot, folderPath, name, entityPath) =>
            {
                machineRobot.Path = folderPath;
                machineRobot.Machine = name;
                machineRobot.PathMachine = entityPath;
            }
        );

        TestDataQueueItems = new(this, OrchAPISession.GetTestDataQueueItems,
            testDataQueue => testDataQueue.Id!.Value,
            TestDataQueue => TestDataQueue.Name!,
            (queueItem, folderPath, name, entityPath) =>
            {
                queueItem.Path = folderPath;
                queueItem.PathTestDataQueue = entityPath;
            }
        );
    }

    internal List<Folder>? _dicFolders; // sorted by OrchDirectory and DisplayName
    internal List<Folder>? _dicFoldersForEnumFolders;
    public ReadOnlyCollection<Folder> GetFolders()
    {
        if (_dicFolders is null)
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
            List<PersonalWorkspace> personalWorkspaces = null;
            tasks.Add(Task.Run(() =>
            {
                // この例外は握りつぶす
                try { personalWorkspaces = PersonalWorkspaces.Get(); } catch { }
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
            if (user is not null && user.PersonalWorkspace is not null)
            {
                personalWorkspaceName = user.PersonalWorkspace.DisplayName;
                user.PersonalWorkspace.ParentId = null; // なぜか値が入っていることがあるので修正
                user.PersonalWorkspace.Path = NameColonSeparator;
                user.PersonalWorkspace.FolderType ??= "Personal"; // なぜか ApiVer == 11.1 では null になっているので修正
                user.PersonalWorkspace.FeedType = "FolderHierarchy"; // なぜか "Processes" が入っているので修正
                //user.PersonalWorkspace.FullName = NameColonSeparator + WildcardPattern.Escape(user.PersonalWorkspace.DisplayName);
                user.PersonalWorkspace.FullName = NameColonSeparator + user.PersonalWorkspace.DisplayName;
                _dicFolders = [user.PersonalWorkspace];
                _dicFoldersForEnumFolders = [user.PersonalWorkspace];
            }

            // personal workspaces that being explored の戻りを処理
            #region retriving Exploring Personal Workspace
            if (personalWorkspaces is not null)
            {
                foreach (var ws in personalWorkspaces
                    .Where(ws => ws.Name != personalWorkspaceName && // My Workspace は直前の current user の処理で追加済みなので除外
                        (user is not null && ws.ExploringUserIds is not null && ws.ExploringUserIds.Any(id => id == user?.Id)))
                    .OrderBy(ws => ws.Name)) 
                {
                    // 自分が exploring 中の、他人のワークスペースを追加する
                    // (explore していないワークスペースには、権限がなくアクセスできないため)
//                        if (user is not null && ws.ExploringUserIds is not null && ws.ExploringUserIds.Any(id => id == user?.Id))
                    {
                        var pwFolder = new Folder()
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
                        };
                        _dicFolders ??= [];
                        _dicFolders.Add(pwFolder);
                        _dicFoldersForEnumFolders ??= [];
                        _dicFoldersForEnumFolders.Add(pwFolder);
                    }
                }
            }
            #endregion

            // folders の戻りを処理
            foreach (var folder in folders ?? [])
            {
                // 追加済みのフォルダーをスキップ
                folder.FolderType ??= "Standard"; // なぜか ApiVer == 11.1 では null になっているので修正

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
            // _dicFolders は、GetChildItems 用だ。
            _dicFolders ??= [];
            _dicFoldersForEnumFolders ??= [];
            if (folders is not null)
            {
                #region _dicFolders を構築
                // 1. 最初に、ルート直下のフォルダをすべて追加
                // 1-1. personal workspace folder をすべて追加（これはすでに済み）
                // 1-2. それ以外のルート直下のフォルダを追加
                _dicFolders.AddRange(folders
                    .Where(f => !f.FullyQualifiedName!.Contains('/'))
                    .OrderBy(f => f.FullyQualifiedNameOrderable));

                // 2. personal workspace folder 配下にあるフォルダをすべて追加
                _dicFolders.AddRange(folders
                    .Where(f => f.FeedType == "PersonalWorkspace")
                    .Where(f => f.FullyQualifiedName!.Contains('/')) // これはなくても良い気がするが、
                    .OrderBy(f => f.FullyQualifiedNameOrderable));

                // 3. 残りのフォルダをすべて追加
                _dicFolders.AddRange(folders
                    .Where(f => f.FeedType != "PersonalWorkspace")
                    .Where(f => f.FullyQualifiedName!.Contains('/'))
                    .OrderBy(f => f.FullyQualifiedNameOrderable));
                #endregion

                #region _dicFoldersForEnumFolders を構築
                // _dicFoldersForEnumFolders は、OrchDriveInfo.EnumFolders() 用だ。微妙にソート順が違う。
                // 1. 最初に、personal workspace folder をすべて追加 (ルート直下のものだけ済みだ)
                // personal workspace folder 配下にあるフォルダを追加してから、FullyQualifiedName でソートし直すのが良かろう
                _dicFoldersForEnumFolders.AddRange(folders
                    .Where(f => f.FeedType == "PersonalWorkspace"));
                _dicFoldersForEnumFolders = _dicFoldersForEnumFolders
                    .OrderBy(f => f.FullyQualifiedNameOrderable)
                    .ToList();

                // 2. 残りのフォルダをすべて追加
                _dicFoldersForEnumFolders.AddRange(folders
                    .Where(f => f.FeedType != "PersonalWorkspace")
                    .OrderBy(f => f.FullyQualifiedNameOrderable));
                #endregion
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
        if (folder.ParentId is not null && folder.ParentId != 0)
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
        if (string.IsNullOrEmpty(CurrentLocation) || CurrentLocation == Path.DirectorySeparatorChar + "")
        {
            return null;
        }
        return GetFolder(OrchDriveInfo.PSPathToOrchPath(CurrentLocation))!;
    }
}
