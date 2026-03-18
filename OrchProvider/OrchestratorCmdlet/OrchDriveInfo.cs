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

// Would like to create a common base class for OrchDriveInfo, OrchDuDriveInfo, and OrchTmDriveInfo,
// but it's a bit of work.. postponing for now.
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

    //public bool _warningOutput = false;

    // Initialized in OrchFolderProvider's Start method
    internal static SessionState? SessionState;

    public static string GetTopParentPath(string orchPath)
    {
        int index = orchPath.IndexOf('/');
        if (index == -1)
        {
            return orchPath; // Return as-is if the path does not contain a slash
        }

        // Get the substring up to the slash
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

    protected internal Folder? RootFolder;

    // TODO: created folder cache sorted by FullyQualifiedName for GetFolder()
    // public HashSet<Folder> _folderSetCache;

    public void ClearAllCache()
    {
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

        _dicJobsHavingExecutionMedia = null;

        _dicLibraryVersions = null; // If an exception occurs, it should have already been thrown when getting _dicLibraries, so this should be fine..
        _dicLibraryVersionsInHostFeed = null;

        _dicLicenseNamedUser = null;

        _dicLicenseRuntime = null;

        _dicMachineClientSecrets = null;
        _dicMachineClientSecrets_Exception.ClearCache();

        _dicPackages = null;
        _dicPackages_Exceptions?.ClearCache();

        _dicPackageVersions = null; // If an exception occurs, it should have already been thrown when getting _dicPackages, so this should be fine..

        _dicPackageEntryPoint = null;
        _dicPackageEntryPoint_Exception?.ClearCache();

        //_dicPartitionGlobalId = null; // This doesn't change, so no need to clear it..

        _dicTestSetExecutions = null;
        _dicTestCaseExecutions = null;
        _dicTestCaseAssertions = null;
        _dicTestSetExecutions_Exceptions?.ClearCache();
        _dicTestCaseExecutions_Exceptions?.ClearCache();

        _dicTriggers = null;
        _dicTriggers_Exceptions?.ClearCache();

        _dicTriggersDetailed = null;
        _dicTriggersDetailed_Exceptions.ClearCache();

        _dicQueueLinks = null;
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


        #endregion
    }

    // TODO: This method's implementation is probably incomplete..
    public void ClearFolderCache(Folder? folder)
    {
        if (folder is null || folder.Id is null || folder.Id.Value == 0) return;
        Int64 folderId = folder.Id.Value;

        foreach (var cache in _allFolderCache)
        {
            cache.ClearCache(folder);
        }

        //_dicAssetLinks = null; // TODO: Would like to clear only the necessary parts more intelligently, but it's too much effort
        _dicJobsHavingExecutionMedia?.TryRemove(folderId, out _);
        _dicTriggers?.TryRemove(folderId, out _);
        _dicQueueLinks = null;
        _dicReleases?.TryRemove(folderId, out _);
        //_dicReleaseList?.TryRemove(folderId, out _);
        _dicTestSetExecutions?.TryRemove(folderId, out _);

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
            // It was working fine, but concurrent might be safer?
            // Let's just rewrite it with ConcurrentDictionary.
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

            // If Details were already fetched and cached, replace them
            if (_dicAuditLogs.TryGetValue(log.Id!.Value, out var cached))
            {
                log.Details = cached?.Details;
            }

            _dicAuditLogs[log.Id!.Value] = log;
        }

        // Return only the results from this query
        return queriedLogs.AsReadOnly();
    }

    // Returns true if an API call was made
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

    public ReadOnlyCollection<Job> StartJobs(Folder folder, string processKey, string? runtimeType, int? jobsCount, string? inputArguments = null)
    {
        var jobs = OrchAPISession.StartJobs(folder.Id ?? 0, processKey, runtimeType, jobsCount, inputArguments).ToList();
        foreach (var job in jobs)
        {
            job.Path = folder.GetPSPath();
            Jobs.AddToCache(folder, job);
        }
        return jobs.AsReadOnly();
    }

    // This method could be integrated into the IncrementalCachePerFolder class, but it gets a bit complex, so postponing. Reasons:
    // 1. The cache class constructor stays simple
    // 2. Single-item fetch APIs may differ by type, making it hard to generalize
    // 3. The AddToCache method is generic and can be used for other purposes
    public Job? GetJob(Folder folder, Int64 jobId)
    {
        var job = OrchAPISession.GetJob(folder.Id ?? 0, jobId);
        if (job is not null)
        {
            job.Path = folder.GetPSPath();
            Jobs.AddToCache(folder, job);
        }
        return job;
    }

    // key: folderId, hash
    // The Log's Id always returns zero, so it cannot be cached properly
    // As a workaround, create a dictionary using hash values.
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

        // Always query the API
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

        #region  Add to Jobs cache only if this JobId is not already cached
        var jobs = Jobs.GetCache(folder);
        foreach (var media in result)
        {
            if (media.JobId.HasValue && (jobs is null || !jobs.ContainsKey(media.JobId.Value!)))
            {
                Jobs.AddToCache(folder, new Job
                {
                    Id = media.JobId,
                    Path = folder.GetPSPath(),
                    ReleaseName = media.ReleaseName,
                    HasMediaRecorded = true,
                });
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
    // This API is undocumented, and it seems it may not work on older versions of Orchestrator.
    // Therefore, sealing it off for now.
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
                    _dicPartitionGlobalId = exUser?.AccountId; // Probably this one for Automation Cloud.
                else
                    _dicPartitionGlobalId = exUser?.TenantKey; // Probably this one for on-premises.
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

        // Try to get from JWT first (fast path)
        _dicPartitionGlobalId = OrchAPISession.AuthManager.GetPartitionGlobalIdFromJwt();
        if (_dicPartitionGlobalId is not null) return _dicPartitionGlobalId;

        lock (_dicPartitionGlobalIdLock)
        {
            // Fallback: query via API (confidential app, or JWT without prt_id)
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
    internal string? _dicTenantKey = null; // Is this a Guid? Does it differ between on-premises and AC?
    internal object _dicTenantIdLock = new();
    internal (int? id, string? key) GetTenantId()
    {
        if (_dicTenantId is not null) return (_dicTenantId, _dicTenantKey);

        lock (_dicPartitionGlobalIdLock)
        {
            // When reaching this code, it should always be a confidential app
            // For non-confidential apps, GetCurrentUser() should have been called during login.
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
                // We can pinpoint-delete the cache here, so let's do it.
                // This is not a frequently executed operation, and it's safer this way..
                _dicUsersDetailed?.TryRemove(owner.Id!.Value, out _);

                var detailedOwner = GetUser(owner);

                // If GetUser() fails, we cannot safely update this entity, so skip the update
                if (detailedOwner is null) return false;

                if (detailedOwner.MayHavePersonalWorkspace.GetValueOrDefault())
                {
                    var postingUser = OrchCollectionExtensions.DeepCopy(detailedOwner);
                    if (postingUser.UnattendedRobot is not null)
                    {
                        // The Password returned from the server may contain "*****".
                        // Set it to null to prevent accidentally updating the password with "*****".
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
            return false; // Swallow this exception
        }
        return true;
    }

    #endregion

    #region OrchEntitiesSummary cache
    // key: foldlerId

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
        // LicenseRuntime is not cached; query each time
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
        // Confirmed that personal workspaces have no triggers when ApiVersion == 11.1
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
    // assetId alone should be unique, but including folderId in the key just to be safe.
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
            // TODO: Something seems unoptimized around here?
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
    // Do not pass "id:version" as processId. Doing so will corrupt the cache.
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
    // bucketId alone should be unique, but include folderId in the key as well just in case.
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
    //private ReadOnlyCollection<TestSetExecution>? _dicTestSetExecutionsEmpty = null;
    public ReadOnlyCollection<TestSetExecution> GetTestSetExecutions(Folder folder, string? query = null, ulong skip = 0, ulong first = ulong.MaxValue)
    {
        // TODO: Is the threshold of < 16 correct? Confirmed that retrieval errors occur on 15.0,
        // but actually, the error seems to be independent of ApiVersion..
        //if (OrchAPISession.ApiVersion < 16)
        //{
        //    _dicTestSetExecutionsEmpty ??= new List<TestSetExecution>().AsReadOnly();
        //    return _dicTestSetExecutionsEmpty;
        //}

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

    /// <summary>
    /// Searches for a TestSetExecution by name from cache or API and returns its Id.
    /// Returns null if not found.
    /// </summary>
    public Int64? ResolveTestSetExecutionId(Folder folder, string name)
    {
        // First search the cache
        if (_dicTestSetExecutions?.TryGetValue(folder.Id ?? 0, out var cached) ?? false)
        {
            var found = cached.Values.FirstOrDefault(e =>
                string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase));
            if (found is not null)
            {
                return found.Id;
            }
        }

        // If not in cache, search by name via API
        var filter = $"&$filter=(Name%20eq%20%27{Uri.EscapeDataString(name)}%27)";
        var results = GetTestSetExecutions(folder, filter, 0, 1);
        var execution = results.FirstOrDefault();
        return execution?.Id;
    }

    #endregion

    #region OrchTestCaseExecution cache
    // Key: folderId
    internal ConcurrentDictionary<Int64, List<TestCaseExecution>>? _dicTestCaseExecutions = null;
    internal ExceptionsCachePer<Int64> _dicTestCaseExecutions_Exceptions = new();

    // TestCaseAssertion cache: Key1=folderId, Key2=testCaseExecutionId
    internal ConcurrentDictionary<Int64, ConcurrentDictionary<Int64, List<TestCaseAssertion>>>? _dicTestCaseAssertions = null;

    /// <summary>
    /// Retrieves TestCaseExecution from the API and sets Path and TestSetExecutionName.
    /// Returns from cache if available.
    /// </summary>
    public List<TestCaseExecution> GetTestCaseExecutions(Folder folder, string? filter = null, ulong skip = 0, ulong first = ulong.MaxValue)
    {
        // Do not use cache when filter/skip/first are specified
        bool useCache = (filter is null && skip == 0 && first == ulong.MaxValue);

        if (useCache)
        {
            _dicTestCaseExecutions_Exceptions.ThrowCachedExceptionIfAny(folder.Id ?? 0);

            if (_dicTestCaseExecutions is null)
            {
                lock (_dicTestCaseExecutions_Exceptions)
                {
                    _dicTestCaseExecutions ??= new();
                }
            }

            if (_dicTestCaseExecutions.TryGetValue(folder.Id ?? 0, out var cached))
            {
                return cached;
            }
        }

        try
        {
            var testCaseExecutions = OrchAPISession.GetTestCaseExecutions(folder.Id ?? 0, filter, skip, first).ToList();
            string folderPath = folder.GetPSPath();

            // Prepare to retrieve TestSetExecutionName from cache
            ConcurrentDictionary<Int64, TestSetExecution>? folderTestSetExecutions = null;
            _dicTestSetExecutions?.TryGetValue(folder.Id ?? 0, out folderTestSetExecutions);

            // Collect TestSetExecutionIds that are not in the cache
            var missingTestSetExecutionIds = testCaseExecutions
                .Where(t => t.TestSetExecutionId is not null)
                .Select(t => t.TestSetExecutionId!.Value)
                .Distinct()
                .Where(id => folderTestSetExecutions is null || !folderTestSetExecutions.ContainsKey(id))
                .ToList();

            // Fetch TestSetExecutions not in cache from the API (batched in groups of 20)
            if (missingTestSetExecutionIds.Count > 0)
            {
                const int batchSize = 20;
                foreach (var batch in missingTestSetExecutionIds.Chunk(batchSize))
                {
                    var orFilter = batch.CreateOrFilter(id => $"Id eq {id}");
                    if (orFilter is not null)
                    {
                        var query = $"&$filter={orFilter}";
                        GetTestSetExecutions(folder, query, 0, ulong.MaxValue);
                    }
                }
                // Re-fetch since the cache was updated
                _dicTestSetExecutions?.TryGetValue(folder.Id ?? 0, out folderTestSetExecutions);
            }

            foreach (var testCaseExecution in testCaseExecutions)
            {
                testCaseExecution.Path = folderPath;

                // Set TestSetExecutionName from cache
                if (testCaseExecution.TestSetExecutionId is not null && folderTestSetExecutions is not null)
                {
                    if (folderTestSetExecutions.TryGetValue(testCaseExecution.TestSetExecutionId.Value, out var testSetExecution))
                    {
                        testCaseExecution.TestSetExecutionName = testSetExecution.Name;
                        testCaseExecution.PathTestSetExecutionName = Path.Combine(folderPath, testSetExecution.Name!);
                    }
                }
            }

            // Save to or merge into cache
            if (useCache)
            {
                _dicTestCaseExecutions![folder.Id ?? 0] = testCaseExecutions;
            }
            else if (testCaseExecutions.Count > 0)
            {
                // Even with a filter, merge results into the cache
                if (_dicTestCaseExecutions is null)
                {
                    lock (_dicTestCaseExecutions_Exceptions)
                    {
                        _dicTestCaseExecutions ??= new();
                    }
                }
                var folderId = folder.Id ?? 0;
                _dicTestCaseExecutions.AddOrUpdate(
                    folderId,
                    testCaseExecutions,
                    (key, existing) =>
                    {
                        var existingIds = existing.Where(e => e.Id is not null).Select(e => e.Id!.Value).ToHashSet();
                        var newItems = testCaseExecutions.Where(e => e.Id is not null && !existingIds.Contains(e.Id.Value));
                        existing.AddRange(newItems);
                        return existing;
                    });
            }

            return testCaseExecutions;
        }
        catch (HttpResponseException ex)
        {
            if (useCache)
            {
                _dicTestCaseExecutions_Exceptions.CacheException(folder.Id ?? 0, ex);
            }
            throw;
        }
    }

    #endregion

    #region OrchBucket cache

    // key: (folderId, bucketId)
    // bucketId alone should be unique, but including folderId in the key just to be safe.
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

    // Caching exceptions here probably isn't necessary
    //internal readonly ExceptionsCachePer<long> _dicCalendar_Exceptions = new();
    public ExtendedCalendar? GetCalendar(ExtendedCalendar calendar)
    {
        if (_dicCalendars is null)
        {
            lock (_dicCalendars_Exceptions) // Must lock on the same object to avoid deadlocks.
            {
                _dicCalendars ??= [];
            }
        }

        if (_dicCalendars.TryGetValue(calendar.Id!.Value, out var extendedCalendar))
        {
            if (calendar.ExcludedDates?.Length != 0) // Already fetched and cached, return as-is
                return calendar;
        }

        calendar = OrchAPISession.GetCalendar(calendar.Id.Value);
        if (calendar is null)
        {
            //_dicCalendars[calendarId] = null; // Adding null to the collection causes trouble later, so skip caching..
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
        // This shouldn't need to be thread-safe, but just to be safe..
        if (_dicPmAuditLogs is null)
        {
            lock (_dicPmAuditLogs_Exception)
            {
                _dicPmAuditLogs = [];
            }
        }

        // Always query the API
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

    // key: search text
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
    // kind: "user", "group", or "application" - robots cannot be searched apparently.
    // unresolvedList is an output parameter. Returns unresolved names as the original T list.
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

        // Build a list of names or emails that haven't been queried yet
        List<T> needQueryUsers = users
            .Where(user => !string.IsNullOrEmpty(getSearchKeyFunc(user)))
            .Where(user => !_dicPmBulkResolveByName.ContainsKey((kind, getSearchKeyFunc(user))))
            .DistinctBy(user => getSearchKeyFunc(user))
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

        // Rather than returning the entire cache, return only the results for the queried users
        Dictionary<string, T> dicList = [];

        // If unresolvedList is provided, build this list.
        // Use a dictionary for fast user lookup.
        if (unresolvedList is not null)
        {
            dicList = users.ToDictionary(u => getSearchKeyFunc(u), u => u, StringComparer.OrdinalIgnoreCase);

            // Add entries with null search keys to the unresolved list
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
            // Everything should be in the dictionary.. Names not found will have null values.
            if (_dicPmBulkResolveByName.TryGetValue((kind, key), out PmGroupMember value))
            {
                ret[key] = value;
                if (value is null && dicList is not null)
                {
                    // This should also always be in the dictionary..
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
        // This API cannot search for user names containing '+'.
        // As a precaution, also exclude '-' and '_' from the search word
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

        // Search by name prefix match, return results if found
        var ret = value?.Where(obj => obj.identityName?.StartsWith(name, StringComparison.OrdinalIgnoreCase) ?? false) ?? [];
        if (ret.Any()) return ret;

        // If no prefix match, return all results. If there's only one entry, it should be the desired user.
        return value ?? [];
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
            newGroup.Path = NameColonSeparator; // Not needed for the cache, but necessary to set on the PmGroup returned by this method.
            _dicSearchDirectory = null;
            _dicSearchDirectory_Exception.ClearCache();
            _dicSearchPmDirectory = null;
            _dicSearchPmDirectory_Exception.ClearCache();

            // If we clear the PmGroup cache, the newly created PmGroup may not be included
            // in the return value when PmGroups.Get() is called immediately after.
            // Therefore, update the cache here instead of clearing it.
            // We used to clear the cache after calling CreateXxx() to build a consistent cache,
            // since CreateXxx() sometimes returned incorrect results, but this is problematic..
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

        PmUsers.ClearCache(); // Contains group IDs internally
        PmRobotAccounts.ClearCache(); // Contains group IDs internally
        // PmExternalClients.ClearCache(); // This does not contain group IDs

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

        PmUsers.ClearCache(); // Contains group IDs internally
        PmRobotAccounts.ClearCache(); // Contains group IDs internally
        // PmExternalClients.ClearCache(); // This does not contain group IDs

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

    // Organization list entities
    public readonly ListCachePerOrganization<PmUser> PmUsers;
    public readonly ListCachePerOrganization<PmGroup> PmGroups;
    public readonly ListCachePerOrganization<PmRobotAccount> PmRobotAccounts;
    public readonly ListCachePerOrganization<ExternalClient> PmExternalClients;
    public readonly ListCachePerOrganization<ExternalResource> PmExternalApiResources;
    public readonly ListCachePerOrganization<NuLicensedGroup> PmLicensedGroups;
    public readonly ListCachePerOrganization<NuLicensedUser> PmLicensedUsers;
    public readonly ListCachePerOrganization<AccessAllowedMember> PmAccessAllowedMember;

    // Non-indexed organization entities
    // These should not be fetched in multi-threaded contexts. Path assignment will break.
    public readonly SingleCachePerOrganization<PmAuthenticationRoot> PmAuthenticationSetting;

    // These must be kept per drive, so they cannot be static members of the Cache class
    internal readonly List<ITenantCacheClearable> _allTenantCache = [];
    internal readonly List<IFolderCacheClearable> _allFolderCache = [];

    // Non-indexed tenant entities
    public readonly SingleCachePerTenant<ActivitySettings> ActivitySettings;
    public readonly SingleCachePerTenant<string[]> AvailableVersions;
    public readonly SingleCachePerTenant<ODataValueOfString> ConnectionString;
    public readonly SingleCachePerTenant<UpdateSettings> UpdateSettings;
    public readonly SingleCachePerTenant<License> LicenseSettings;
    public readonly SingleCachePerTenant<LibraryFeed[]> LibraryFeeds;

    // Indexed tenant entities
    public readonly IndexedCachePerTenant<User, UserPrivilege> UserPrivileges;

    // Tenant list entities
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

    // Non-indexed folder entities
    public readonly SingleCachePerFolder<EntitiesSummary> EntitiesSummary;
    public readonly SingleCachePerFolder<string> FolderFeedId;

    public readonly ListCachePerFolder<TaskCatalog> ActionCatalogs;
    public readonly ListCachePerFolder<HttpTrigger> ApiTriggers;
    public readonly ListCachePerFolder<ApiTrigger> EventTriggers;
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
    public readonly ListCachePerFolder<TestDataQueue> TestDataQueues;
    //public readonly ListCachePerFolder<TestDataQueueItem> TestDataQueueItems;
    public readonly ListCachePerFolder<TestSet> TestSets;
    public readonly ListCachePerFolder<TestSetSchedule> TestSetSchedules;
    public readonly ListCachePerFolder<UserRobots> UserRobots;
    public readonly ListCachePerFolder<MachineRuntime> RuntimesForFolder;

    // Indexed folder entities
    public readonly IndexedListCachePerFolder<MachineFolder, ExtendedRobot> FolderRobots;
    public readonly IndexedListCachePerFolder<MachineFolder, RobotUser> MachinesRobots;
    public readonly IndexedListCachePerFolder<Bucket, BlobFile> BucketFiles;
    public readonly IndexedListCachePerFolder<Release, SubtypedPackageResource> ReleaseRequirements;
    public readonly IndexedListCachePerFolder<TestDataQueue, TestDataQueueItem> TestDataQueueItems;

    //public readonly CachePerFolder<Release> Processes; // This one was indexed..

    // Incremental cache for folder entities
    public readonly IncrementalCachePerFolder<Int64, Job> Jobs;

    // At the time this constructor runs, NameColonSeparator is not yet available
    public OrchDriveInfo(ProviderInfo provider, PSDrive drive) :
        base(drive.Name, provider, drive.Name + ':' + Path.DirectorySeparatorChar, drive.Description, null, drive.Root)
    {
        _psDrive = drive;
        _psDrive.Root = _psDrive.Root?.TrimEnd('/');
        RootFolder = new Folder() { DisplayName = "", FullyQualifiedName = "", Path = NameColonSeparator };

        // Initialize caches

        // Organization list entities
        PmUsers                = new(this, OrchAPISession.GetPmUsers,                 e => e.Path = NameColonSeparator);
        PmGroups               = new(this, OrchAPISession.GetPmGroups,                e =>
            {
                e.Path = NameColonSeparator;
                foreach (var m in e.members ?? [])
                {
                    m.Path = NameColonSeparator;
                    m.groupName = e.name;
                    m.PathGroupName = NameColonSeparator + e.name;
                }
            },
                                e => e.id, OrchAPISession.GetPmGroup);

        PmRobotAccounts        = new(this, OrchAPISession.GetPmRobotAccounts,         e => e.Path = NameColonSeparator);
        PmExternalClients      = new(this, OrchAPISession.GetPmExternalClients,       e => e.Path = NameColonSeparator);
        PmExternalApiResources = new(this, OrchAPISession.GetPmExternalApiResource,   e => e.Path = NameColonSeparator);

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

        PmAccessAllowedMember = new(this,
            OrchAPISession.GetPmPartitionAccessPolicy,
            e =>
            {
                e.Path = NameColonSeparator;
            }
        );

        // Non-indexed organization entities
        PmAuthenticationSetting = new(this,
            OrchAPISession.GetPmAuthenticationSetting,
            e =>
            {
                e.Path = NameColonSeparator;
                if (e.settingsExpanded is null && !string.IsNullOrEmpty(e.externalIdentityProviderDto?.settings))
                {
                    try
                    {
                        e.settingsExpanded = JsonTools.JsonToDictionary(e.externalIdentityProviderDto.settings);
                    }
                    catch
                    {
                        e.settingsExpanded = [];
                    }
                }
            }
        );

        // Non-indexed tenant entities
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
                    // In 11.1, the robot listing did not produce an API call.. Try building something similar from users.
                    // TODO: How does this work in version 12 and later?
                    var users = GetUsers();
                    foreach (var user in users)
                    {
                        GetUser(user);
                    }
                    return users.Select(u => new Robot()
                    {
                        // TODO: This is incomplete. Rough/placeholder implementation.
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

        // The current implementation always calls GetCredentialStore.
        // We should consider adding an -ExpandDetails parameter to the Get-OrchCredentialStore cmdlet and separating this call.
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

        // Confirmed that the v15 web interface includes &$expand=UpdateInfo
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

        // Indexed tenant entities
        UserPrivileges = new(this, OrchAPISession.GetUserPrivilege, e => e.Id!.Value, e => e.UserName!,
            (e, userName) => 
            { 
                e.Path = NameColonSeparator;
                e.UserName = userName;
            }
        );

        // Non-indexed folder entities
        // Confirmed that the below returns an error in 11.1. TODO: How do we get feedId? How does this work in version 12 and later?
        EntitiesSummary                = new(this, fid => OrchAPISession.GetEntitiesSummary(fid!.Value), (e, folderPath) => e.Path = folderPath);
        FolderFeedId                   = new(this, OrchAPISession.GetFolderFeedId, null, 12);
        ActionCatalogs                 = new(this, OrchAPISession.GetTaskCatalogs,       (e, folderPath) => e.Path = folderPath, 16); // Confirmed no error is returned in v16
        ApiTriggers                    = new(this, OrchAPISession.GetHttpTriggers,       (e, folderPath) => e.Path = folderPath, 18); // Confirmed not present in the v17 web interface (executing in v17 does not return an error, though)
        EventTriggers                  = new(this, OrchAPISession.GetEventTriggers,      (e, folderPath) => e.Path = folderPath, 18);
        Buckets                        = new(this, OrchAPISession.GetBuckets,            (e, folderPath) => e.Path = folderPath);
        Environments                   = new(this, OrchAPISession.GetEnvironments,       (e, folderPath) => e.Path = folderPath);
        FolderUsersWithNoInherited     = new(this, fid => OrchAPISession.GetUsersForFolder(fid, false), (e, folderPath) => e.Path = folderPath);
        FolderUsersWithInherited       = new(this, fid => OrchAPISession.GetUsersForFolder(fid, true),  (e, folderPath) => e.Path = folderPath);
        MachineSessionRuntimesByFolder = new(this, fid => OrchAPISession.GetMachineSessionRuntimesByFolderId(fid), (e, folderPath) => e.Path = folderPath);
        Queues                         = new(this, OrchAPISession.GetQueues,             (e, folderPath) => e.Path = folderPath);
        Reviewers                      = new(this, OrchAPISession.GetReviewers);
        RobotsFromFolder               = new(this, OrchAPISession.GetRobotsFromFolder,   (e, folderPath) => e.Path = folderPath);
        Sessions                       = new(this, OrchAPISession.GetSessions,           (e, folderPath) => e.Path = folderPath);
        TestCases                      = new(this, OrchAPISession.GetTestCases,          (e, folderPath) => e.Path = folderPath); // Confirmed not in v17 web interface, but apparently not dependent on API version
        TestDataQueues                 = new(this, OrchAPISession.GetTestDataQueues,     (e, folderPath) => e.Path = folderPath); // Confirmed not in v17 web interface, but apparently not dependent on API version
        TestSets                       = new(this, OrchAPISession.GetTestSets,           (e, folderPath) => e.Path = folderPath); // Confirmed not in v17 web interface, but apparently not dependent on API version
        TestSetSchedules               = new(this, OrchAPISession.GetTestSetSchedules,   (e, folderPath) => e.Path = folderPath); // Confirmed not in v17 web interface, but apparently not dependent on API version
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

        // v15: ?$filter=((((IsAssignedToFolder eq true) or (IsInherited eq true))))
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

        // Processes is indexed, so the cache cannot be implemented as shown below
        //Processes = new(this,
        //    OrchAPISession.ApiVersion >= 12
        //        ? fid => OrchAPISession.GetReleases(fid, "&$expand=Environment,CurrentVersion,ReleaseVersions,EntryPoint")
        //        : fid => OrchAPISession.GetReleases(fid, "&$expand=Environment,CurrentVersion,ReleaseVersions"),
        //    (e, folderPath) => e.Path = folderPath
        //);

        // Indexed folder entities
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

        ReleaseRequirements = new(this, OrchAPISession.GetReleaseRequirement,
            release => release.Id!.Value,
            release => release.Name!,
            (releaseRequirement, folderPath, name, entityPath) =>
            {
                releaseRequirement.Path = folderPath;
                releaseRequirement.Release = name;
            }
        );

        TestDataQueueItems = new(this, OrchAPISession.GetTestDataQueueItems,
            testDataQueue => testDataQueue.Id!.Value,
            testDataQueue => testDataQueue.Name!,
            (queueItem, folderPath, name, entityPath) =>
            {
                queueItem.Path = folderPath;
                queueItem.PathTestDataQueue = entityPath;
            }
        );

        // Incremental cache for folder entities
        Jobs = new IncrementalCachePerFolder<Int64, Job>(
            this,
            //(folderId, query, skip, first, orderBy, orderAsc) => OrchAPISession.GetJobs(folderId, query, skip, first, orderBy, orderAsc),
            OrchAPISession.GetJobs,
            job => job.Id!.Value,
            (job, folderPath) => job.Path = folderPath
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
            List<PersonalWorkspace> personalWorkspaces = null;
            if (!OrchAPISession.AuthManager.IsConfidentialApp)
            {
                tasks.Add(Task.Run(() =>
                {
                    // Swallow this exception
                    try { user = GetCurrentUser() as ExtendedUser; } catch { }
                }));

                // get personal workspaces that being explored
                tasks.Add(Task.Run(() =>
                {
                    // Swallow this exception
                    try { personalWorkspaces = PersonalWorkspaces.Get(); } catch { }
                }));
            }

            // get folders
            List<Folder> folders = null;
            tasks.Add(Task.Run(() =>
            {
                // If an exception occurs, let it propagate
                folders = OrchAPISession.GetFolders().ToList();
            }));

            Task.WaitAll([..tasks]);

            // Process the current user result
            string personalWorkspaceName = "";
            if (user is not null && user.PersonalWorkspace is not null)
            {
                personalWorkspaceName = user.PersonalWorkspace.DisplayName;
                user.PersonalWorkspace.ParentId = null; // Fix: this sometimes has a value for some reason
                user.PersonalWorkspace.Path = NameColonSeparator;
                user.PersonalWorkspace.FolderType ??= "Personal"; // Fix: this is null in ApiVer 11.1 for some reason
                user.PersonalWorkspace.FeedType = "FolderHierarchy"; // Fix: this contains "Processes" for some reason
                //user.PersonalWorkspace.FullName = NameColonSeparator + WildcardPattern.Escape(user.PersonalWorkspace.DisplayName);
                user.PersonalWorkspace.FullName = NameColonSeparator + user.PersonalWorkspace.DisplayName;
                _dicFolders = [user.PersonalWorkspace];
                _dicFoldersForEnumFolders = [user.PersonalWorkspace];
            }

            // Process the personal workspaces being explored result
            #region retriving Exploring Personal Workspace
            if (personalWorkspaces is not null)
            {
                foreach (var ws in personalWorkspaces
                    .Where(ws => ws.Name != personalWorkspaceName && // Exclude My Workspace since it was already added in the current user processing above
                        (user is not null && ws.ExploringUserIds is not null && ws.ExploringUserIds.Any(id => id == user?.Id)))
                    .OrderBy(ws => ws.Name)) 
                {
                    // Add other users' workspaces that we are currently exploring
                    // (Workspaces we are not exploring are inaccessible due to lack of permissions)
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

            // Process the folders result
            foreach (var folder in folders ?? [])
            {
                // Skip already-added folders
                folder.FolderType ??= "Standard"; // Fix: this is null in ApiVer 11.1 for some reason

                // Set the Path member to the parent folder name
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
            // _dicFolders is used by GetChildItems.
            _dicFolders ??= [];
            _dicFoldersForEnumFolders ??= [];
            if (folders is not null)
            {
                #region Build _dicFolders
                // 1. First, add all folders directly under the root
                // 1-1. Add all personal workspace folders (already done above)
                // 1-2. Add the remaining folders directly under the root
                _dicFolders.AddRange(folders
                    .Where(f => !f.FullyQualifiedName!.Contains('/'))
                    .OrderBy(f => f.FullyQualifiedNameOrderable));

                // 2. Add all folders under personal workspace folders
                _dicFolders.AddRange(folders
                    .Where(f => f.FeedType == "PersonalWorkspace")
                    .Where(f => f.FullyQualifiedName!.Contains('/')) // This filter may not be necessary, but keeping it
                    .OrderBy(f => f.FullyQualifiedNameOrderable));

                // 3. Add all remaining folders
                _dicFolders.AddRange(folders
                    .Where(f => f.FeedType != "PersonalWorkspace")
                    .Where(f => f.FullyQualifiedName!.Contains('/'))
                    .OrderBy(f => f.FullyQualifiedNameOrderable));
                #endregion

                #region Build _dicFoldersForEnumFolders
                // _dicFoldersForEnumFolders is used by OrchDriveInfo.EnumFolders(). The sort order differs slightly.
                // 1. First, add all personal workspace folders (only root-level ones are done so far)
                // Add folders under personal workspace folders, then re-sort by FullyQualifiedName
                _dicFoldersForEnumFolders.AddRange(folders
                    .Where(f => f.FeedType == "PersonalWorkspace"));
                _dicFoldersForEnumFolders = _dicFoldersForEnumFolders
                    .OrderBy(f => f.FullyQualifiedNameOrderable)
                    .ToList();

                // 2. Add all remaining folders
                _dicFoldersForEnumFolders.AddRange(folders
                    .Where(f => f.FeedType != "PersonalWorkspace")
                    .OrderBy(f => f.FullyQualifiedNameOrderable));
                #endregion
            }
        }

        return _dicFolders.AsReadOnly();
    }

    public Folder? GetFolder(string? orchPath)
    {
        if (string.IsNullOrEmpty(orchPath))
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
