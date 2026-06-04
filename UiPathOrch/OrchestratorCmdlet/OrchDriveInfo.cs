using System.Collections.Concurrent;
using UiPath.PowerShell.Positional;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Runtime.InteropServices;
using UiPath.OrchAPI;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Entities.JsonConverter;
using Job = UiPath.PowerShell.Entities.Job;
using License = UiPath.PowerShell.Entities.License;
using Path = System.IO.Path;
using Session = UiPath.PowerShell.Entities.Session;
using User = UiPath.PowerShell.Entities.User;

namespace UiPath.PowerShell.Core;

public partial class OrchDriveInfo : OrchDriveInfoBase
{
    internal readonly PSDrive _psDrive;

    private OrchAPISession? _orchAPISession;
    private readonly object _orchAPISessionLock = new();
    internal override OrchAPISession OrchAPISession
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

    // TODO: created folder cache sorted by FullyQualifiedName for GetFolder()
    // public HashSet<Folder> _folderSetCache;


    public override void ClearTenantCache()
    {
        // Registry-driven part (cache instances iterate themselves) lives on
        // OrchDriveInfoBase. The block below is the Orchestrator-specific
        // tenant-level extras that the shadow drives (Du / Tm) do not need.
        // These are all tenant-scoped (tenant identity, folder catalog,
        // PmApiDeprecated, org-shared SearchPmDirectoryCache) so they fire on
        // any clear that touches tenant scope (tenant-only or drive-level).
        base.ClearTenantCache();

        if (_orchAPISession is not null)
        {
            _orchAPISession.PmApiDeprecated = true;
        }

        #region Orchestrator API cache
        _dicFolders = null;
        _dicFoldersForEnumFolders = null;


        _tenantId = null;
        _tenantKey = null;

        #endregion

        #region Platform Management cache


        SearchPmDirectoryCache.ClearCache();

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

        // Drop AssetLinks entries for this folder only (the original code dropped
        // the entire dict; predicate-based clear is more precise).
        AssetLinks.ClearCache(k => k.folderId == folderId);
        // JobsHavingExecutionMedia auto-cleared per folder via _allFolderCache (IncrementalCachePerFolder).
        // Triggers / TriggersDetailed auto-cleared per folder via _allFolderCache.
        // Drop QueueLinks entries for this folder only (matching the AssetLinks pattern).
        QueueLinks.ClearCache(k => k.folderId == folderId);
        // Releases auto-cleared per folder via _allFolderCache (ListCachePerFolder).
        // TestSetExecutions auto-cleared per folder via _allFolderCache (IncrementalCachePerFolder).
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

    // Returns true if an API call was made
    public bool GetAuditLogDetails(AuditLog log)
    {
        if (log.Id is null || log.Details is not null) return false;
        log.Details = OrchAPISession.GetAuditLogDetails(log.Id.Value)?.ToArray();
        foreach (var d in log.Details ?? [])
        {
            d.Path = NameColonSeparator;
            d.PathId = NameColonSeparator + log.Id.ToString();
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

    internal string? _partitionGlobalId = null;
    internal object _partitionGlobalIdLock = new();

    // Passive accessor -- exposes only the cached field. Returns null until
    // GetPartitionGlobalId() has populated it; used by cache cleanup paths
    // (ClearCache, Import-OrchConfig teardown) that must not trigger auth on
    // drives the user hasn't touched.
    internal override string? PartitionGlobalId => _partitionGlobalId;

    internal override string? GetPartitionGlobalId()
    {
        if (_partitionGlobalId is not null) return _partitionGlobalId;

        // Try to get from JWT first (fast path)
        _partitionGlobalId = OrchAPISession.AuthManager.GetPartitionGlobalIdFromJwt();
        if (_partitionGlobalId is not null) return _partitionGlobalId;

        lock (_partitionGlobalIdLock)
        {
            // Fallback: query via API (confidential app, or JWT without prt_id)
            var users = Users.Get();
            foreach (var user in users)
            {
                var detailedUser = OrchAPISession.GetUser(user.Id ?? 0);
                _partitionGlobalId = detailedUser?.AccountId ?? detailedUser?.TenantKey;
                _tenantId = detailedUser?.TenantId;
                _tenantKey = detailedUser?.TenantKey;
                if (_partitionGlobalId is not null) break;
            }
        }
        return _partitionGlobalId;
    }

    internal int? _tenantId = null;
    internal string? _tenantKey = null; // Is this a Guid? Does it differ between on-premises and AC?
    internal (int? id, string? key) GetTenantId()
    {
        if (_tenantId is not null) return (_tenantId, _tenantKey);

        lock (_partitionGlobalIdLock)
        {
            // When reaching this code, it should always be a confidential app
            // For non-confidential apps, GetCurrentUser() should have been called during login.
            try
            {
                var users = Users.Get();
                foreach (var user in users)
                {
                    var detailedUser = OrchAPISession.GetUser(user.Id ?? 0);
                    _partitionGlobalId = detailedUser?.AccountId ?? "";
                    _tenantId = detailedUser?.TenantId;
                    _tenantKey = detailedUser?.TenantKey;
                    if (detailedUser?.TenantId is not null) break;
                }
            }
            catch (Exception ex)
            {
                throw new OrchException(NameColonSeparator, ex);
            }
        }
        return (_tenantId, _tenantKey);
    }

    #region PersonalWorkspace cache
    public bool DisablePersonalWorkspace(long? userId)
    {
        if (userId is null) return false;

        try
        {
            var users = Users.Get();

            var owner = users.FirstOrDefault(o => o.Id == userId);
            if (owner is not null)
            {
                // We can pinpoint-delete the cache here, so let's do it.
                // This is not a frequently executed operation, and it's safer this way..
                UsersDetailed.ClearCache(owner.Id!.Value);

                var detailedOwner = UsersDetailed.Get(owner.Id!.Value);

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
                    Users.ClearCache();
                    UsersDetailed.ClearCache();
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

    #region OrchTrigger cache
    // Backwards-compat shim: delegates to Triggers (ListCachePerFolder).
    // Pre-v12 Personal workspaces have no triggers — short-circuit so we don't
    // even try to fetch them; the API call itself isn't supported there.
    public ICollection<ProcessSchedule> GetTriggers(Folder folder)
    {
        if (OrchAPISession.ApiVersion < 12 && folder.FolderType == "Personal") return [];
        return Triggers.Get(folder);
    }
    #endregion

    // Backwards-compat shim: delegates to TriggersDetailed
    // (KeyedSingleCachePerFolder). The detail payload doesn't include the
    // schedule's executor-robot assignments — those come from a separate
    // endpoint — so we side-fetch them once per cache entry and stash them on
    // the cached trigger. Re-fetch only if ExecutorRobots is still null
    // (which means a previous side-fetch failed or hasn't run yet).
    public ProcessSchedule? GetTrigger(Folder folder, ProcessSchedule schedule)
    {
        var trigger = TriggersDetailed.Get(folder, schedule.Id!.Value);
        if (trigger is not null && trigger.ExecutorRobots is null)
        {
            trigger.ExecutorRobots = OrchAPISession.GetRobotIdsForSchedule(folder.Id ?? 0, schedule.Id!.Value)?
                .Select(id => new RobotExecutor() { Id = id })
                .ToArray() ?? [];
        }
        return trigger;
    }

    #region OrchAsset cache

    // Backwards-compat wrapper: AssetLinks (KeyedSingleCachePerTenant) is keyed
    // by (folderId, assetId). The accessibleFolder.Path / PathName fields depend
    // on the caller's Folder + Asset reference (not derivable from just the
    // ids), so they're set per-call in the wrapper rather than in the cache
    // initializer.
    public AccessibleFoldersDto? GetFoldersForAsset(Folder folder, Asset asset)
    {
        var folderShare = AssetLinks.Get((folder.Id ?? 0, asset.Id ?? 0));
        if (folderShare?.AccessibleFolders is not null)
        {
            string folderPath = folder.GetPSPath();
            string assetPath = asset.GetPSPath();
            foreach (var accessibleFolder in folderShare.AccessibleFolders)
            {
                accessibleFolder.Path = folderPath;
                accessibleFolder.PathName = assetPath;
            }
        }
        return folderShare;
    }

    /// <summary>
    /// Drop all cached AccessibleFoldersDto entries for a single asset, plus any
    /// matching exception entries. Call after Add/Remove-OrchAssetLink so the
    /// next GetFoldersForAsset call re-fetches just this asset's link set
    /// without flushing unrelated assets' caches.
    /// </summary>
    public void ClearAssetLinkCache(Int64 assetId) =>
        AssetLinks.ClearCache(k => k.assetId == assetId);

    #endregion

    #region OrchPackage cache
    // Backwards-compat shim: delegates to Packages (KeyedListCachePerTenant).
    // Path is set per-call in the wrapper rather than in the cache initializer
    // because feedFolder depends on the *calling* folder (multiple folders may
    // share the same feedId on FolderHierarchy feeds, but they do live in the
    // same hierarchy so the feedFolder string is the same — preserved here for
    // safety with the small per-call assignment cost).
    public ReadOnlyCollection<Package> GetPackages(Folder folder)
    {
        string feedId = FolderFeedId.Get(folder) ?? "";
        var packages = Packages.Get(feedId);
        string feedFolder = System.IO.Path.Combine(NameColonSeparator, folder.GetPackageFeedFolder());
        foreach (var package in packages)
        {
            package.Path = feedFolder;
        }
        return packages;
    }
    #endregion

    #region OrchPackageVersion cache
    // Backwards-compat wrapper: PackageVersions (KeyedListCachePerTenant) keyed
    // by (feedId, packageId). Path depends on caller's Folder reference (not
    // derivable from the id tuple), set per-call.
    // Do not pass "id:version" as processId. Doing so will corrupt the cache.
    public ReadOnlyCollection<Package> GetPackageVersions(Folder folder, string processId)
    {
        string feedId = FolderFeedId.Get(folder) ?? "";
        var versions = PackageVersions.Get((feedId, processId));
        if (versions.Count > 0)
        {
            string folderPath = folder.GetPSPath();
            foreach (var package in versions)
            {
                package.Path = folderPath;
            }
        }
        return versions;
    }
    #endregion

    // Backwards-compat wrapper: QueueLinks (KeyedSingleCachePerTenant) keyed by
    // (folderId, queueId). Path / PathName depend on caller's Folder + Queue
    // references; set per-call (same pattern as AssetLinks).
    public AccessibleFoldersDto? GetFoldersForQueue(Folder folder, QueueDefinition queue)
    {
        var folderShare = QueueLinks.Get((folder.Id ?? 0, queue.Id ?? 0));
        if (folderShare?.AccessibleFolders is not null)
        {
            string folderPath = folder.GetPSPath();
            string queuePath = queue.GetPSPath();
            foreach (var accessibleFolder in folderShare.AccessibleFolders)
            {
                accessibleFolder.Path = folderPath;
                accessibleFolder.PathName = queuePath;
            }
        }
        return folderShare;
    }

    /// <summary>
    /// Drop all cached AccessibleFoldersDto entries for a single queue, plus
    /// any matching exception entries. Call after Add/Remove-OrchQueueLink so
    /// the next GetFoldersForQueue call re-fetches just this queue's link
    /// set without flushing unrelated queues' caches.
    /// </summary>
    public void ClearQueueLinkCache(Int64 queueId) =>
        QueueLinks.ClearCache(k => k.queueId == queueId);

    #region OrchQueueItem cache
    // Backwards-compat wrappers: delegate to QueueItems (IncrementalCachePerFolder
    // keyed by folder + item.Id). The original 3-level structure (folderId →
    // queueName → itemId) is flattened — per-queue grouping is a display-time
    // concern (each QueueItem carries its queue.Name) and external readers
    // regroup via GroupBy when needed.
    public List<QueueItem> GetQueueItems(Folder folder, QueueDefinition queue, string filter, ulong skip, ulong first, string? orderBy = null, bool orderAscending = false)
    {
        var items = QueueItems.Fetch(folder, filter, skip, first, orderBy, orderAscending).ToList();
        string folderPath = folder.GetPSPath();
        string queuePath = queue.GetPSPath();
        foreach (var item in items)
        {
            item.Path = folderPath;
            item.PathName = queuePath;
            item.Name = queue.Name;
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
            QueueItems.AddToCache(folder, item);
        }
        return item;
    }
    #endregion

    #region OrchTestSetExecution cache
    /// <summary>
    /// Searches for a TestSetExecution by name from cache or API and returns its Id.
    /// Returns null if not found.
    /// </summary>
    public Int64? ResolveTestSetExecutionId(Folder folder, string name)
    {
        // First search the cache
        var cached = TestSetExecutions.GetCache(folder)?.Values;
        if (cached is not null)
        {
            var found = cached.FirstOrDefault(e =>
                string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase));
            if (found is not null)
            {
                return found.Id;
            }
        }

        // If not in cache, search by name via API
        var filter = $"&$filter=(Name%20eq%20%27{Uri.EscapeDataString(PathTools.EscapeODataLiteral(name))}%27)";
        var results = TestSetExecutions.Fetch(folder, filter, 0, 1);
        var execution = results.FirstOrDefault();
        return execution?.Id;
    }

    #endregion

    #region OrchTestCaseExecution cache

    // TestCaseAssertion cache: per-folder, per-TestCaseExecutionId list of
    // assertions. Initializer sets Path and TestCaseExecutionId; callers still
    // patch TestSetExecutionName / PathTestSetExecutionName per call because
    // those require cross-cache lookup into TestSetExecutions.
    public readonly KeyedListCachePerFolder<long, TestCaseAssertion> TestCaseAssertions;

    /// <summary>
    /// Backwards-compat wrapper: delegates to TestCaseExecutions
    /// (IncrementalCachePerFolder). Always fetches with filter/skip/first
    /// (replacing the original filter-bypass logic — all -First cmdlets now
    /// follow the always-fetch + accumulate pattern, matching AuditLog /
    /// QueueItem / TestSetExecution). After fetch, post-processes each result
    /// to set TestSetExecutionName / PathTestSetExecutionName from the
    /// TestSetExecutions cache (batch-fetching any missing TestSetExecutionIds).
    /// </summary>
    public List<TestCaseExecution> GetTestCaseExecutions(Folder folder, string? filter = null, ulong skip = 0, ulong first = ulong.MaxValue)
    {
        var testCaseExecutions = TestCaseExecutions.Fetch(folder, filter, skip, first).ToList();
        if (testCaseExecutions.Count == 0) return testCaseExecutions;

        string folderPath = folder.GetPSPath();

        // TestSetExecutionName / PathTestSetExecutionName depend on the
        // TestSetExecutions cache, which isn't visible to the cache class's
        // initializer (initializer only has folderPath, not Folder). Resolve
        // here after Fetch, batch-fetching any TestSetExecutionIds we're
        // missing.
        ConcurrentDictionary<Int64, TestSetExecution>? folderTestSetExecutions = null;
        folderTestSetExecutions = TestSetExecutions.GetCache(folder);

        var missingTestSetExecutionIds = testCaseExecutions
            .Where(t => t.TestSetExecutionId is not null)
            .Select(t => t.TestSetExecutionId!.Value)
            .Distinct()
            .Where(id => folderTestSetExecutions is null || !folderTestSetExecutions.ContainsKey(id))
            .ToList();

        if (missingTestSetExecutionIds.Count > 0)
        {
            const int batchSize = 20;
            foreach (var batch in missingTestSetExecutionIds.Chunk(batchSize))
            {
                var orFilter = batch.CreateOrFilter(id => $"Id eq {id}");
                if (orFilter is not null)
                {
                    var query = $"&$filter={orFilter}";
                    TestSetExecutions.Fetch(folder, query, 0, ulong.MaxValue);
                }
            }
            folderTestSetExecutions = TestSetExecutions.GetCache(folder);
        }

        foreach (var testCaseExecution in testCaseExecutions)
        {
            if (testCaseExecution.TestSetExecutionId is not null && folderTestSetExecutions is not null)
            {
                if (folderTestSetExecutions.TryGetValue(testCaseExecution.TestSetExecutionId.Value, out var testSetExecution))
                {
                    testCaseExecution.TestSetExecutionName = testSetExecution.Name;
                    testCaseExecution.PathTestSetExecutionName = Path.Combine(folderPath, testSetExecution.Name!);
                }
            }
        }

        return testCaseExecutions;
    }

    #endregion

    #region OrchBucket cache

    // Backwards-compat wrapper: BucketLinks (KeyedSingleCachePerTenant) keyed by
    // (folderId, bucketId). Path / PathName depend on caller's Folder + Bucket
    // references; set per-call (same pattern as AssetLinks / QueueLinks).
    public AccessibleFoldersDto? GetFoldersForBucket(Folder folder, Bucket bucket)
    {
        var folderShare = BucketLinks.Get((folder.Id ?? 0, bucket.Id ?? 0));
        if (folderShare?.AccessibleFolders is not null)
        {
            string folderPath = folder.GetPSPath();
            string bucketPath = bucket.GetPSPath();
            foreach (var accessibleFolder in folderShare.AccessibleFolders)
            {
                accessibleFolder.Path = folderPath;
                accessibleFolder.PathName = bucketPath;
            }
        }
        return folderShare;
    }

    /// <summary>
    /// Drop all cached AccessibleFoldersDto entries for a single bucket, plus
    /// any matching exception entries. Call after Add/Remove-OrchBucketLink so
    /// the next GetFoldersForBucket call re-fetches just this bucket's link
    /// set without flushing unrelated buckets' caches.
    /// </summary>
    public void ClearBucketLinkCache(Int64 bucketId) =>
        BucketLinks.ClearCache(k => k.bucketId == bucketId);

    #endregion

    #region OrchExecutionSettings Cache
    // Backwards-compat wrapper: ExecutionSettings (KeyedSingleCachePerTenant) is keyed
    // by `scope` only; PathScope / Scope depend on `strScope` and are set per-call so
    // the same cache entry stays correct across different strScope arguments.
    public ExecutionSettingDefinition[]? GetExecutionSettings(int scope, string strScope)
    {
        var arr = ExecutionSettings.Get(scope);
        if (arr is not null)
        {
            string pathScope = Path.Combine(NameColonSeparator, strScope);
            foreach (var setting in arr)
            {
                setting.PathScope = pathScope;
                setting.Scope = strScope;
            }
        }
        return arr;
    }
    #endregion

    // key: (name, kind)
    // kind: "user", "group", or "application" - robots cannot be searched apparently.
    // unresolvedList is an output parameter. Receives back the original T entries
    // whose search key was empty OR whose name the API confirmed unresolved.
    // Cache + bulk-fetch are owned by PmGroupMembers; this wrapper only adapts the
    // generic T input/output ergonomics that PowerShell cmdlets expect.
    public Dictionary<string, PmGroupMember?> PmBulkResolveByName<T>(
        string kind, IEnumerable<T> users,
        Func<T, string> getSearchKeyFunc,
        List<T>? unresolvedList = null)
    {
        // Materialize once — caller may pass a deferred enumerable.
        List<T> userList = users as List<T> ?? users.ToList();
        if (userList.Count == 0) return [];

        // OrdinalIgnoreCase: defensive, in case the caller's getSearchKeyFunc returns
        // case-variant strings for the same user (the API itself is case-sensitive, so
        // the cache key is case-sensitive — this dictionary is only for mapping a
        // resolved name back to the caller's T instance for unresolvedList).
        var searchKeyToUser = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
        var nonEmptyKeys = new List<string>();
        foreach (var user in userList)
        {
            var key = getSearchKeyFunc(user);
            if (string.IsNullOrEmpty(key))
            {
                unresolvedList?.Add(user);
                continue;
            }
            // First-writer-wins on case collision — matches the previous behavior.
            if (!searchKeyToUser.ContainsKey(key)) searchKeyToUser[key] = user;
            nonEmptyKeys.Add(key);
        }

        var resolved = PmGroupMembers.GetMany(kind, nonEmptyKeys);

        if (unresolvedList is not null)
        {
            foreach (var (name, value) in resolved)
            {
                if (value is null && searchKeyToUser.TryGetValue(name, out var unresolvedEntry))
                {
                    unresolvedList.Add(unresolvedEntry);
                }
            }
        }

        return new Dictionary<string, PmGroupMember?>(resolved);
    }

    // Backwards-compat shim: SearchDirectoryCache (KeyedSingleCachePerTenant) is keyed
    // by `searchWord` (`name` truncated at the first '+', '-', or '_'); the underlying
    // API can't handle those characters. Cached entries are then post-filtered by full
    // `name` prefix match.
    //
    // `domain` defaults to null (=> "autogen" in the API call). EntraID-federated
    // OnPrem tenants reject "autogen" with a generic 500 and need the tenant's real
    // domain (e.g. "frc"). The cache key is composed `searchWord|domain` so distinct
    // domains don't fight over the same slot.
    public IEnumerable<DirectoryObject> SearchDirectory(string name, string? domain = null)
    {
        int index = name.IndexOfAny(['+', '-', '_']);
        string searchWord = index >= 0 ? name.Substring(0, index) : name;
        string cacheKey = searchWord + "|" + (domain ?? "");

        var value = SearchDirectoryCache.Get(cacheKey);

        // Prefer prefix match against the full name; fall back to all results when
        // none match (a single entry in `value` is likely the desired user).
        var ret = value?.Where(obj => obj.identityName?.StartsWith(name, StringComparison.OrdinalIgnoreCase) ?? false) ?? [];
        return ret.Any() ? ret : (value ?? []);
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

        SearchDirectoryCache.ClearCache();
        SearchPmDirectoryCache.ClearCache();

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
            // Drive-local Path is set by the caller cmdlet on a per-emit ShallowClone copy.
            SearchDirectoryCache.ClearCache();
            SearchPmDirectoryCache.ClearCache();

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
        // Drive-local Path is set by the caller cmdlet on a per-emit ShallowClone copy.
        PmRobotAccounts.ClearCache();
        foreach (var groupId in cmd.groupIDsToAdd ?? [])
        {
            PmGroups.ClearCache(groupId);
        }

        SearchDirectoryCache.ClearCache();
        SearchPmDirectoryCache.ClearCache();
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
        // Drive-local Path is set by the caller cmdlet on a per-emit ShallowClone copy.
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
        // Drive-local Path is set by the caller cmdlet on a per-emit ShallowClone copy.
        PmGroups.ClearCache(ret?.id);

        PmUsers.ClearCache(); // Contains group IDs internally
        PmRobotAccounts.ClearCache(); // Contains group IDs internally
        // PmExternalClients.ClearCache(); // This does not contain group IDs

        return ret;
    }

    #endregion

    // Backwards-compat wrapper: PmAvailableUserBundles (KeyedSingleCachePerOrganization,
    // keyed by groupId). The groupName parameter used to drive a per-call mutation of
    // GroupName / PathGroupName on the shared cache entry — those fields have been
    // dropped (the singleton is org-shared, so the mutation was racy across drives).
    // Current callers don't read those fields anyway; if a future caller wants to
    // WriteObject this entity, give it [JsonIgnore] labels + ShallowClone and set
    // them on the per-emit copy (see NuLicensedGroupMember). The groupName parameter
    // is kept for backward compatibility but unused.
    public AvailableUserBundles? GetPmUserLicenseGroupsAvailableLicenses(string? groupId, string groupName)
    {
        if (groupId is null) return null;
        return PmAvailableUserBundles.Get(groupId);
    }

    #region GetPmUserLicenseGroupAllocations Cache
    // Backwards-compat shim: delegates to PmUserLicenseGroupAllocations
    // (KeyedListCachePerOrganization, keyed by group.id). Drive-local Path,
    // GroupName, and PathGroupName are set by the caller cmdlet on a per-emit
    // ShallowClone copy so the shared cache entries are not mutated per-call.
    public ReadOnlyCollection<NuLicensedGroupMember> GetPmLicensedGroupAllocations(NuLicensedGroup group) =>
        PmUserLicenseGroupAllocations.Get(group.id!);
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
    public readonly ListCachePerOrganization<AvailableUserBundle> PmLicenses;
    public readonly ListCachePerOrganization<TenantAllocation> PmLicenseAllocations;
    public readonly ListCachePerOrganization<NuLicensedGroup> PmLicensedGroups;
    public readonly ListCachePerOrganization<NuLicensedUser> PmLicensedUsers;
    public readonly ListCachePerOrganization<AccessAllowedMember> PmAccessAllowedMember;

    // Non-indexed organization entities.
    // Multi-threaded fetch is safe: SingleCachePerOrganization serializes
    // same-partition fetches via a per-partitionGlobalId lock, runs the
    // initializer once before publishing, and no longer mutates a shared
    // Path on cache hit (drive-local Path is set by the cmdlet on a per-emit
    // ShallowClone copy). Get-Pm* cmdlets fetch these via
    // OrchThreadPool.RunForEach.
    public readonly SingleCachePerOrganization<PmAuthenticationRoot> PmAuthenticationSetting;
    public readonly SingleCachePerOrganization<LicenseInventory> PmLicenseInventory;
    public readonly SingleCachePerOrganization<AccountLicense> PmLicenseContract;
    // Org-global: /api/Status/Version is identical across tenants in an org.
    public readonly SingleCachePerOrganization<OrchProductVersion> ProductVersion;

    // _allTenantCache / _allFolderCache live on OrchDriveInfoBase; cache
    // instances declared below register themselves via the inherited members.

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
    public readonly IncrementalCachePerTenant<long, MachineSessionRuntime> MachineSessionRuntimes;
    public readonly IncrementalCachePerTenant<long, Session> UserSessions;
    public readonly ListCachePerTenant<PersonalWorkspace> PersonalWorkspaces;
    public readonly ListCachePerTenant<Settings> Settings;
    public readonly ListCachePerTenant<Webhook> Webhooks;
    public readonly ListCachePerTenant<WebhookEventType> WebhookEventTypes;
    public readonly ListCachePerTenant<ResponseDictionaryItem> WebSettings;

    // Keyed list entities (tenant-scoped, per-key list)
    public readonly KeyedListCachePerTenant<string, LicenseNamedUser> LicenseNamedUsers;
    public readonly KeyedListCachePerTenant<string, LibraryVersion> LibraryVersions;
    public readonly KeyedListCachePerTenant<string, LibraryVersion> LibraryVersionsInHostFeed;
    public readonly KeyedListCachePerTenant<string, Package> Packages;
    public readonly KeyedListCachePerOrganization<string, NuLicensedGroupMember> PmUserLicenseGroupAllocations;
    public readonly KeyedListCachePerTenant<string, LicenseRuntime> LicenseRuntimes;

    // Single-keyed entities (one entity per key, exception cached per key)
    public readonly KeyedSingleCachePerTenant<string, MachineClientSecretResponse[]?> MachineClientSecrets;
    public readonly KeyedSingleCachePerTenant<int, ExecutionSettingDefinition[]?> ExecutionSettings;
    public readonly KeyedSingleCachePerOrganization<string, PmDirectoryEntityInfo[]?> SearchPmDirectoryCache;
    public readonly KeyedSingleCachePerTenant<string, DirectoryObject[]?> SearchDirectoryCache;
    public readonly KeyedSingleCachePerOrganization<string, AvailableUserBundles> PmAvailableUserBundles;
    // The connected user's own portal preference for one setting key, org-scoped
    // (self-only: one user per drive). Invalidated per key by Set-/Copy-PmUserPreference.
    public readonly KeyedSingleCachePerOrganization<string, PmUserSettingDto> PmUserPreferences;
    public readonly PmGroupMembersCache PmGroupMembers;
    public readonly RobotLogsCache RobotLogs;
    public readonly KeyedSingleCachePerTenant<long, User> UsersDetailed;
    public readonly ListCachePerTenant<User> Users;
    public readonly IncrementalCachePerTenant<long, AuditLog> AuditLogs;
    // PmAuditLog has no scalar id; identity is structural (9 fields via PmAuditLog.Equals).
    // Key = entity itself — ConcurrentDictionary<PmAuditLog, PmAuditLog> uses the same
    // GetHashCode/Equals contract HashSet<PmAuditLog> used to, so dedup semantics are preserved.
    public readonly IncrementalCachePerTenant<PmAuditLog, PmAuditLog> PmAuditLogs;
    public readonly IncrementalCachePerTenant<string, Alert> Alerts;
    public readonly SingleCachePerTenant<User> CurrentUser;
    public readonly ListCachePerTenant<ExtendedCalendar> Calendars;
    public readonly KeyedSingleCachePerTenant<long, ExtendedCalendar> CalendarsDetailed;
    public readonly KeyedSingleCachePerTenant<(Int64 folderId, Int64 assetId), AccessibleFoldersDto?> AssetLinks;
    public readonly KeyedSingleCachePerTenant<(Int64 folderId, Int64 queueId), AccessibleFoldersDto?> QueueLinks;
    public readonly KeyedSingleCachePerTenant<(Int64 folderId, Int64 bucketId), AccessibleFoldersDto?> BucketLinks;
    public readonly KeyedListCachePerTenant<(string feedId, string packageId), Package> PackageVersions;
    public readonly KeyedListCachePerTenant<(string feedId, string packageId, string version), PackageEntryPoint> PackageEntryPoints;
    public readonly IncrementalCachePerFolder<long, ExecutionMedia> JobsHavingExecutionMedia;
    public readonly IncrementalCachePerFolder<long, TestCaseExecution> TestCaseExecutions;
    public readonly IncrementalCachePerFolder<long, TestSetExecution> TestSetExecutions;
    public readonly IncrementalCachePerFolder<long, QueueItem> QueueItems;

    public readonly ListCachePerFolder<DfEntity> DfEntities;

    // Non-indexed folder entities
    public readonly SingleCachePerFolder<EntitiesSummary> EntitiesSummary;
    public readonly SingleCachePerFolder<string> FolderFeedId;

    public readonly ListCachePerFolder<TaskCatalog> ActionCatalogs;
    public readonly ListCachePerFolder<HttpTrigger> ApiTriggers;
    public readonly ListCachePerFolder<BusinessRule> BusinessRules;
    public readonly ListCachePerFolder<Connection> Connections;
    public readonly ListCachePerFolder<ApiTrigger> EventTriggers;
    public readonly ListCachePerFolder<Asset> Assets;
    public readonly ListCachePerFolder<Bucket> Buckets;
    public readonly ListCachePerFolder<Entities.Environment> Environments;
    public readonly ListCachePerFolder<MachineFolder> FolderMachines;
    public readonly ListCachePerFolder<MachineFolder> FolderMachinesAssigned;
    public readonly ListCachePerFolder<MachineFolder> FolderMachinesAssignable;
    public readonly ListCachePerFolder<UserRoles> FolderUsersWithNoInherited;
    public readonly ListCachePerFolder<UserRoles> FolderUsersWithInherited;
    public readonly IncrementalCachePerFolder<long, MachineSessionRuntime> MachineSessionRuntimesByFolder;
    public readonly ListCachePerFolder<SimpleUser> Reviewers;
    public readonly ListCachePerFolder<QueueDefinition> Queues;
    public readonly ListCachePerFolder<RobotsFromFolderModel> RobotsFromFolder;
    public readonly ListCachePerFolder<Session> Sessions;
    public readonly ListCachePerFolder<OrchTask> Tasks;
    public readonly ListCachePerFolder<TestCaseDefinition> TestCases;
    public readonly ListCachePerFolder<TestDataQueue> TestDataQueues;
    //public readonly ListCachePerFolder<TestDataQueueItem> TestDataQueueItems;
    public readonly ListCachePerFolder<TestSet> TestSets;
    public readonly ListCachePerFolder<Release> Releases;
    public readonly KeyedSingleCachePerFolder<long, Release> ReleasesDetailed;
    public readonly ListCachePerFolder<ProcessSchedule> Triggers;
    public readonly KeyedSingleCachePerFolder<long, ProcessSchedule> TriggersDetailed;
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

    // Completer-only state-scoped Job caches. Each holds only jobs in the named state(s),
    // so a Tab on Restart-OrchJob never collides with one on Resume-OrchJob.
    public readonly ListCachePerFolder<Job> FaultedJobs;
    public readonly ListCachePerFolder<Job> SuspendedJobs;
    public readonly ListCachePerFolder<Job> StoppableJobs;

    // Conservative invalidation: state transitions during Job mutations are non-trivial
    // (Restart can yield Pending→Running→Faulted, Stop can yield Stopping→Successful/Faulted),
    // so clear all completer Job caches together.
    public void ClearJobCompleterCaches(Folder folder)
    {
        FaultedJobs.ClearCache(folder);
        SuspendedJobs.ClearCache(folder);
        StoppableJobs.ClearCache(folder);
    }

    // At the time this constructor runs, NameColonSeparator is not yet available
    public OrchDriveInfo(ProviderInfo provider, PSDrive drive) :
        base(drive.Name!, provider, drive.Name + ':' + Path.DirectorySeparatorChar, drive.Description!, null, drive.Root!)
    {
        _psDrive = drive;
        _psDrive.Root = _psDrive.Root?.TrimEnd('/');
        RootFolder = new Folder() { DisplayName = "", FullyQualifiedName = "", Path = NameColonSeparator };

        // Initialize caches

        // Organization list entities
        // Drive-local Path is a plain [JsonIgnore] property set by the cmdlet
        // on a per-emit ShallowClone() copy (never on the shared singleton);
        // see LicenseInventory / PmAuthenticationRoot.
        PmUsers = new(this, OrchAPISession.GetPmUsers);
        PmGroups = new(this, OrchAPISession.GetPmGroups, e =>
            {
                // PmGroupMember.Path / PathGroupName are drive-local; the cmdlet
                // sets them on a per-emit ShallowClone copy. groupName is
                // parent-derived (org-scoped) and stays.
                foreach (var m in e.members ?? [])
                {
                    m.groupName = e.name;
                }
            },
                                e => e.id, OrchAPISession.GetPmGroup);

        // No initializer needed — Path is set on a per-emit ShallowClone copy.
        PmRobotAccounts = new(this, OrchAPISession.GetPmRobotAccounts);
        PmExternalClients = new(this, OrchAPISession.GetPmExternalClients);
        PmExternalApiResources = new(this, OrchAPISession.GetPmExternalApiResource);

        PmLicenses = new(this,
            OrchAPISession.GetPmLicenses,
            // Only entity-only derived state — display name lookup.
            e =>
            {
                if (e.code is not null && AvailableUserBundlesItems.Items.TryGetValue(e.code, out var name))
                    e.name = name;
            }
        );

        PmLicenseAllocations = new(this, OrchAPISession.GetPmLicenseAllocations);

        PmLicensedGroups = new(this,
            OrchAPISession.GetPmLicensedGroups,
            e =>
            {
                e.userBundleLicenseNames = e.userBundleLicenses?.Select(b => AvailableUserBundlesItems.Items[b]).ToArray();
            }
        );

        PmLicensedUsers = new(this,
            OrchAPISession.GetPmLicensedUsers,
            e =>
            {
                e.userBundleLicenseNames = e.userBundleLicenses?.Select(b => AvailableUserBundlesItems.Items[b]).ToArray();
            }
        );

        PmAccessAllowedMember = new(this, OrchAPISession.GetPmPartitionAccessPolicy);

        // Non-indexed organization entities
        PmAuthenticationSetting = new(this,
            OrchAPISession.GetPmAuthenticationSetting,
            // Drive-local Path is no longer set here — the cmdlet sets it on a
            // per-emit ShallowClone copy. Only the entity's own derived state
            // (parsed JSON settings) is computed here.
            e =>
            {
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

        // No initializer: Path is set by the cmdlet on a per-emit ShallowClone
        // copy; no other derived state to compute on this entity.
        PmLicenseInventory = new(this, OrchAPISession.GetPmLicenseInventory);

        PmLicenseContract = new(this, OrchAPISession.GetPmLicenseContract);

        // ProductVersion is org-global (/api/Status/Version is identical across
        // tenants), so it caches per organization. No initializer: Path is set
        // by the cmdlet on a per-emit ShallowClone copy.
        ProductVersion = new(this, OrchAPISession.GetProductVersion);

        // Non-indexed tenant entities
        ActivitySettings = new(this, OrchAPISession.GetActivitySettings, e => e.Path = NameColonSeparator);
        ConnectionString = new(this, OrchAPISession.GetConnectionString, e => e.Path = NameColonSeparator);
        LicenseSettings = new(this, OrchAPISession.GetLicenseSettings, e => e.Path = NameColonSeparator);
        // GetMachineSessionRuntimes() takes no params today; the IncrementalCachePerTenant
        // fetcher discards query/skip/first so cmdlet semantics stay "fetch all" while
        // still benefiting from the always-fetch-and-accumulate exception cache.
        MachineSessionRuntimes = new(this,
            (_, _, _) => OrchAPISession.GetMachineSessionRuntimes(),
            e => e.SessionId ?? 0,
            (e, drivePath) => e.Path = drivePath);
        UserSessions = new(this,
            (query, skip, first) => OrchAPISession.GetGlobalSessions(query, skip, first),
            session => session.Id ?? 0,
            (session, drivePath) => session.Path = drivePath);

        RuntimesForFolder = new(this, OrchAPISession.GetRuntimesForFolder);
        AllRobotsAcrossFolders = new(this, OrchAPISession.FindAllRobotsAcrossFolders, e => e.Path = NameColonSeparator);
        PersonalWorkspaces = new(this, OrchAPISession.GetPersonalWorkspaces, e => e.Path = NameColonSeparator);
        Roles = new(this, OrchAPISession.GetRoles, e => e.Path = NameColonSeparator);

        LibrariesInTenant = new(this, () => OrchAPISession.GetLibraries(null), e => e.Path = NameColonSeparator);
        LibrariesInHost = new(this, () => OrchAPISession.GetLibraries(LibraryHostFeedId), e => e.Path = NameColonSeparator);

        Settings = new(this, OrchAPISession.GetSettings, e => e.Path = NameColonSeparator);
        UpdateSettings = new(this, OrchAPISession.GetUpdateSettings, e => e.Path = NameColonSeparator);
        Webhooks = new(this, OrchAPISession.GetWebhooks, e => e.Path = NameColonSeparator);
        WebhookEventTypes = new(this, OrchAPISession.GetWebhookEventTypes, e => e.Path = NameColonSeparator);

        // ListCachePerFolder keys by folder.Id ?? 0; translate the sentinel back to null so the
        // legacy tenant-wide query (no X-UIPATH-OrganizationUnitId header) is issued for root.
        DfEntities = new(this, folderId => OrchAPISession.GetDfEntities(folderId == 0 ? null : (Int64?)folderId) ?? []);

        LibraryFeeds = new(this, OrchAPISession.GetLibraryFeeds, null);

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
                    // Null-forgiving on Users / UsersDetailed: this lambda runs only after the
                    // constructor has finished, by which time both fields are initialized. The
                    // analyzer can't see that, since these caches are initialized later in the
                    // same ctor than this Robots assignment.
                    var users = Users!.Get();
                    foreach (var user in users)
                    {
                        UsersDetailed!.Get(user.Id!.Value);
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
                return results.Select(r => r.Item).ToList();
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

        // Keyed list entities
        LicenseNamedUsers = new(this,
            robotType => OrchAPISession.GetLicensesNamedUser(robotType),
            (license, robotType) =>
            {
                license.RobotType = robotType;
                license.Path = NameColonSeparator;
                license.PathRobotType = NameColonSeparator + robotType;
            },
            StringComparer.OrdinalIgnoreCase);

        LibraryVersions = new(this,
            libraryId => OrchAPISession.GetLibraryVersions(libraryId)
                .OrderBy(v => v.Version!, VersionComparer.Instance),
            (library, _) => library.Path = NameColonSeparator);

        LibraryVersionsInHostFeed = new(this,
            libraryId => OrchAPISession.GetLibraryVersions(libraryId, LibraryHostFeedId)
                .OrderBy(v => v.Version!, VersionComparer.Instance),
            (library, _) => library.Path = NameColonSeparator);

        Packages = new(this,
            feedId => OrchAPISession.GetPackages(feedId));
        // No initializer: GetPackages wrapper sets Path per-call from the
        // caller's folder context (feedFolder isn't derivable from feedId alone).

        PmUserLicenseGroupAllocations = new(this,
            // API doesn't take partitionGlobalId; cache key is (partitionGlobalId, groupId)
            // because allocations are scoped to an org's groups.
            (_, groupId) => OrchAPISession.GetPmLicenseGroupAllocations(groupId));
        // No initializer: GetPmLicensedGroupAllocations wrapper sets
        // GroupName / PathGroupName per-call from the NuLicensedGroup arg
        // (only group.id participates in the cache key).

        LicenseRuntimes = new(this,
            robotType => OrchAPISession.GetLicensesRuntime(robotType),
            (license, robotType) =>
            {
                license.RobotType = robotType;
                license.Path = NameColonSeparator + robotType;
            },
            StringComparer.OrdinalIgnoreCase);

        // Single-keyed entities
        MachineClientSecrets = new(this, licenseKey => OrchAPISession.GetMachineClientSecret(licenseKey));

        ExecutionSettings = new(this,
            scope => OrchAPISession.GetExecutionSettings(scope)?.Configuration,
            (arr, _) =>
            {
                if (arr is null) return;
                foreach (var setting in arr)
                {
                    setting.Path = NameColonSeparator;
                }
            });

        SearchPmDirectoryCache = new(this,
            (partitionGlobalId, key) => OrchAPISession.SearchPmDirectory(partitionGlobalId, key));
        // No initializer: PmDirectoryEntityInfo's drive-local Path is set by
        // the caller cmdlet on a per-emit ShallowClone copy.

        PmUserPreferences = new(this,
            (partitionGlobalId, key) =>
            {
                // Self-only: the connected user's id is the Orchestrator Key, which
                // equals the identity `sub` the Setting API expects as `userId`.
                // CurrentUser is assigned later in this ctor but is always set by the
                // time this fetch lambda runs, hence the null-forgiving access.
                var userId = CurrentUser!.Get()?.Key;
                if (string.IsNullOrEmpty(userId)) return null;
                // The Setting GET needs explicit key filters; fetch just this key.
                return OrchAPISession.GetUserSettings(partitionGlobalId, userId, [key])
                    ?.FirstOrDefault(s => s is not null && string.Equals(s.key, key, StringComparison.OrdinalIgnoreCase));
            });

        SearchDirectoryCache = new(this,
            cacheKey =>
            {
                // Cache key is "searchWord|domain" — split so we forward both to
                // the API call. Empty domain segment falls back to "autogen"
                // inside OrchAPISession.SearchDirectory.
                int sep = cacheKey.IndexOf('|');
                string searchWord = sep >= 0 ? cacheKey.Substring(0, sep) : cacheKey;
                string? domain = sep >= 0 && sep < cacheKey.Length - 1
                    ? cacheKey.Substring(sep + 1)
                    : null;
                return OrchAPISession.SearchDirectory(searchWord, domain);
            },
            (arr, _) =>
            {
                if (arr is null) return;
                foreach (var v in arr)
                {
                    v.Path = NameColonSeparator;
                }
            });

        PmGroupMembers = new(this);
        RobotLogs = new(this);

        PmAvailableUserBundles = new(this,
            // The API doesn't take partitionGlobalId (just groupId), but the
            // result is still org-scoped, so the cache is keyed by
            // (partitionGlobalId, groupId).
            (_, groupId) => OrchAPISession.GetPmLicensedGroupsAvailableLicenses(groupId),
            (bundles, _) =>
            {
                // Path is no longer attached to the shared singleton (the cache is
                // org-scoped). The nested AvailableUserBundle[] entries get their
                // human-readable .name resolved once at fetch time — that mapping
                // is intrinsic to the entity, not drive-local.
                foreach (var bundle in bundles.availableUserBundles ?? [])
                {
                    if (AvailableUserBundlesItems.Items.TryGetValue(bundle.code ?? "", out var name))
                    {
                        bundle.name = name;
                    }
                }
            });

        UsersDetailed = new(this,
            userId => OrchAPISession.GetUser(userId),
            (detailedUser, _) => detailedUser.Path = NameColonSeparator);

        Users = new(this,
            () => OrchAPISession.GetUsers() ?? [],
            user => user.Path = NameColonSeparator);

        Alerts = new(this,
            (query, skip, first) => OrchAPISession.GetAlerts(query, skip, first),
            alert => alert.Id,
            (alert, drivePath) => alert.Path = drivePath);

        CurrentUser = new(this,
            getter: () =>
            {
                try
                {
                    ExtendedUser? exUser = OrchAPISession.GetCurrentUserExtended();
                    if (exUser is not null)
                    {
                        _partitionGlobalId = string.IsNullOrEmpty(exUser.AccountId)
                            ? exUser.TenantKey   // on-premises
                            : exUser.AccountId;  // Automation Cloud
                        _tenantId = exUser.TenantId;
                        _tenantKey = exUser.TenantKey;
                        if (exUser.PersonalWorkspace is not null)
                        {
                            exUser.PersonalWorkspace.Path = NameColon;
                        }
                    }
                    return (User?)exUser;
                }
                catch (Exception)
                {
                    // Fallback for older Orchestrators where the Extended
                    // endpoint isn't available. Side-effect setup of
                    // partition/tenant ids is skipped on this path (matches
                    // pre-migration behavior). If this also throws, the cache
                    // class catches HttpResponseException /
                    // DeterministicApiException and stores it for reuse.
                    return OrchAPISession.GetCurrentUser();
                }
            },
            initializer: user => user.Path = NameColonSeparator);

        AuditLogs = new(this,
            (query, skip, first) => OrchAPISession.GetAuditLogs(query, skip, first),
            log => log.Id ?? 0,
            (log, drivePath) =>
            {
                log.Path = drivePath;
                if (log.Entities is not null && log.Entities.Length > 0)
                {
                    string pathId = log.GetPSPath();
                    foreach (var entity in log.Entities)
                    {
                        entity.Path = drivePath;
                        entity.PathId = pathId;
                        entity.CustomDataExpanded = JsonTools.JsonToDictionary(entity.CustomData);
                    }
                }
            },
            // Preserve previously-fetched Details when an updated row arrives in
            // a subsequent batch — GetAuditLogDetails populates Details on the
            // cached entity and we don't want a fresh list-query to wipe that.
            (newLog, cached) =>
            {
                if (cached?.Details is not null) newLog.Details = cached.Details;
            });

        PmAuditLogs = new(this,
            (query, skip, first) => OrchAPISession.GetPmAuditLog(GetPartitionGlobalId(), query, skip, first),
            log => log, // identity key — relies on PmAuditLog.GetHashCode/Equals
            (log, drivePath) =>
            {
                log.Path = drivePath;
                log.auditLogDetailsExpanded = JsonTools.JsonToDictionary(log.auditLogDetails);
            });

        Calendars = new(this,
            () => OrchAPISession.GetCalendars() ?? [],
            calendar => calendar.Path = NameColonSeparator);

        CalendarsDetailed = new(this,
            calendarId => OrchAPISession.GetCalendar(calendarId),
            (detailedCalendar, _) => detailedCalendar.Path = NameColonSeparator);

        AssetLinks = new(this,
            key => OrchAPISession.GetFoldersForAsset(key.folderId, key.assetId));
        // No initializer: GetFoldersForAsset wrapper sets each AccessibleFolder's
        // Path / PathName per-call from the caller's Folder + Asset references
        // (neither is derivable from just the id tuple).

        QueueLinks = new(this,
            key => OrchAPISession.GetFoldersForQueue(key.folderId, key.queueId));
        // Same pattern as AssetLinks: Path / PathName set per-call in the wrapper.

        BucketLinks = new(this,
            key => OrchAPISession.GetFoldersForBucket(key.folderId, key.bucketId));
        // Same pattern as AssetLinks / QueueLinks.

        PackageVersions = new(this,
            key => OrchAPISession.GetPackageVersions(key.feedId, key.packageId)
                .OrderBy(p => p.Version!, VersionComparer.Instance));
        // No initializer: GetPackageVersions wrapper sets Path per-call from
        // the caller's Folder reference (not derivable from the id tuple).

        PackageEntryPoints = new(this,
            key => OrchAPISession.GetPackageEntryPoints(key.feedId, key.packageId, key.version));
        // No initializer needed: PackageEntryPoint has no per-call fields.

        JobsHavingExecutionMedia = new(this,
            // OrchAPISession.GetExecutionMedia takes only (folderId, skip, first);
            // the IncrementalCachePerFolder fetcher signature includes query/
            // orderBy/orderAscending which we ignore.
            (folderId, _, skip, first, _, _) => OrchAPISession.GetExecutionMedia(folderId, skip, first),
            media => media.Id ?? 0,
            (media, folderPath) => media.Path = folderPath);

        TestCaseExecutions = new(this,
            // GetTestCaseExecutions takes (folderId, filter, skip, first); the
            // fetcher signature additionally has orderBy/orderAscending which
            // we ignore.
            (folderId, filter, skip, first, _, _) =>
                OrchAPISession.GetTestCaseExecutions(folderId, filter, skip, first),
            tce => tce.Id ?? 0,
            (tce, folderPath) => tce.Path = folderPath);

        TestCaseAssertions = new(this,
            // GetTestCaseExecutionWithAssertions returns the parent TestCaseExecution;
            // we keep only its TestCaseAssertions list (the parent itself lives in
            // the TestCaseExecutions cache via a different fetch path).
            (folderId, tceId) =>
                OrchAPISession.GetTestCaseExecutionWithAssertions(folderId, tceId)?.TestCaseAssertions
                    ?? Enumerable.Empty<TestCaseAssertion>(),
            (assertion, folderPath, tceId) =>
            {
                assertion.Path = folderPath;
                assertion.TestCaseExecutionId = tceId;
            });
        // TestSetExecutionName / PathTestSetExecutionName are set in callers
        // because they require a cross-cache lookup into TestSetExecutions.
        // TestSetExecutionName / PathTestSetExecutionName are set in the
        // GetTestCaseExecutions wrapper after Fetch (they require cross-cache
        // lookup into TestSetExecutions, which the initializer can't access).

        TestSetExecutions = new(this,
            // GetTestSetExecutions takes (folderId, query, skip, first); same
            // signature shape as TestCaseExecutions.
            (folderId, query, skip, first, _, _) =>
                OrchAPISession.GetTestSetExecutions(folderId, query, skip, first),
            tse => tse.Id ?? 0,
            (tse, folderPath) => tse.Path = folderPath);

        QueueItems = new(this,
            // OrchAPISession.GetQueueItems takes (folderId, filter, skip, first,
            // orderBy, orderAscending); matches IncrementalCachePerFolder's
            // fetcher signature exactly.
            OrchAPISession.GetQueueItems,
            qi => qi.Id ?? 0);
        // No initializer: GetQueueItems wrapper sets Path / PathName / Name
        // per-call from the caller's Folder + QueueDefinition references (none
        // are derivable from just the item id).

        // Non-indexed folder entities
        // Confirmed that the below returns an error in 11.1. TODO: How do we get feedId? How does this work in version 12 and later?
        EntitiesSummary = new(this, fid => OrchAPISession.GetEntitiesSummary(fid!.Value), (e, folderPath) => e.Path = folderPath);
        FolderFeedId = new(this, OrchAPISession.GetFolderFeedId, null, 12);
        ActionCatalogs = new(this, OrchAPISession.GetTaskCatalogs, (e, folderPath) => e.Path = folderPath, 16); // Confirmed no error is returned in v16
        ApiTriggers = new(this, OrchAPISession.GetHttpTriggers, (e, folderPath) => e.Path = folderPath, 18); // Confirmed not present in the v17 web interface (executing in v17 does not return an error, though)
        BusinessRules = new(this, OrchAPISession.GetBusinessRules, (e, folderPath) => e.Path = folderPath);
        Connections = new(this, OrchAPISession.GetConnections, (e, folderPath) => e.Path = folderPath, 20); // Connection Service v1 (Integration Service); gated at API v20
        EventTriggers = new(this, OrchAPISession.GetEventTriggers, (e, folderPath) => e.Path = folderPath, 18);
        Buckets = new(this, OrchAPISession.GetBuckets, (e, folderPath) => e.Path = folderPath);
        Environments = new(this, OrchAPISession.GetEnvironments, (e, folderPath) => e.Path = folderPath);
        FolderUsersWithNoInherited = new(this, fid => OrchAPISession.GetUsersForFolder(fid, false), (e, folderPath) => e.Path = folderPath);
        FolderUsersWithInherited = new(this, fid => OrchAPISession.GetUsersForFolder(fid, true), (e, folderPath) => e.Path = folderPath);
        MachineSessionRuntimesByFolder = new(this,
            (folderId, query, skip, first, _, _) => OrchAPISession.GetMachineSessionRuntimesByFolderId(folderId, query, skip, first),
            e => e.SessionId ?? 0,
            (e, folderPath) => e.Path = folderPath);
        Queues = new(this, OrchAPISession.GetQueues, (e, folderPath) => e.Path = folderPath);
        Reviewers = new(this, OrchAPISession.GetReviewers);
        RobotsFromFolder = new(this, OrchAPISession.GetRobotsFromFolder, (e, folderPath) => e.Path = folderPath);
        Sessions = new(this, OrchAPISession.GetSessions, (e, folderPath) => e.Path = folderPath);
        Tasks = new(this, OrchAPISession.GetTasks, (e, folderPath) => e.Path = folderPath);
        TestCases = new(this, OrchAPISession.GetTestCases, (e, folderPath) => e.Path = folderPath); // Confirmed not in v17 web interface, but apparently not dependent on API version
        TestDataQueues = new(this, OrchAPISession.GetTestDataQueues, (e, folderPath) => e.Path = folderPath); // Confirmed not in v17 web interface, but apparently not dependent on API version
        TestSets = new(this, OrchAPISession.GetTestSets, (e, folderPath) => e.Path = folderPath); // Confirmed not in v17 web interface, but apparently not dependent on API version

        Releases = new(this,
            folderId =>
            {
                // The list endpoint accepts an OData $expand string. v12+ adds EntryPoint.
                string query = OrchAPISession.ApiVersion >= 12
                    ? "&$expand=Environment,CurrentVersion,ReleaseVersions,EntryPoint"
                    : "&$expand=Environment,CurrentVersion,ReleaseVersions";
                return OrchAPISession.GetReleases(folderId, query);
            },
            (release, folderPath) => release.Path = folderPath);

        ReleasesDetailed = new(this,
            (folderId, releaseId) => OrchAPISession.GetReleaseById(folderId, releaseId, "?$expand=ReleaseVersions,EntryPoint"),
            (release, folderPath, _) => release.Path = folderPath);

        Triggers = new(this,
            folderId => OrchAPISession.GetProcessSchedules(folderId),
            (trigger, folderPath) => trigger.Path = folderPath);

        TriggersDetailed = new(this,
            (folderId, scheduleId) => OrchAPISession.GetProcessSchedule(folderId, scheduleId),
            (trigger, folderPath, _) => trigger.Path = folderPath);
        TestSetSchedules = new(this, OrchAPISession.GetTestSetSchedules, (e, folderPath) => e.Path = folderPath); // Confirmed not in v17 web interface, but apparently not dependent on API version
        UserRobots = new(this, OrchAPISession.GetUserRobots);

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

        // Completer-only state-scoped Job caches.
        FaultedJobs = new ListCachePerFolder<Job>(
            this,
            folderId => OrchAPISession.GetJobs(folderId,
                "&$filter=(State%20eq%20%27Faulted%27)", 0, ulong.MaxValue, null, false),
            (job, folderPath) => job.Path = folderPath
        );
        SuspendedJobs = new ListCachePerFolder<Job>(
            this,
            folderId => OrchAPISession.GetJobs(folderId,
                "&$filter=(State%20eq%20%27Suspended%27)", 0, ulong.MaxValue, null, false),
            (job, folderPath) => job.Path = folderPath
        );
        StoppableJobs = new ListCachePerFolder<Job>(
            this,
            folderId => OrchAPISession.GetJobs(folderId,
                "&$filter=((ProcessType%20eq%20%27Process%27)%20and%20((State%20eq%20%27Pending%27)%20or%20(State%20eq%20%27Running%27)%20or%20(State%20eq%20%27Stopping%27)%20or%20(State%20eq%20%27Suspended%27)%20or%20(State%20eq%20%27Resumed%27)))",
                0, ulong.MaxValue, null, false),
            (job, folderPath) => job.Path = folderPath
        );
    }

    internal List<Folder>? _dicFolders; // sorted by OrchDirectory and DisplayName
    internal List<Folder>? _dicFoldersForEnumFolders;
    // Why: prior code mutated _dicFolders incrementally (assign → AddRange → AddRange…); a
    // concurrent reader could pass the null-check and return a partially-built list, causing
    // ItemExists() → false for folders that actually exist. Build into locals, publish once.
    private readonly object _foldersInitLock = new();
    public ReadOnlyCollection<Folder> GetFolders()
    {
        var snapshot = Volatile.Read(ref _dicFolders);
        if (snapshot is not null) return snapshot.AsReadOnly();

        lock (_foldersInitLock)
        {
            if (_dicFolders is not null) return _dicFolders.AsReadOnly();

            OrchAPISession.EnsureAuthenticated();

            var tasks = new List<Task>();

            // get current user to get my own personal workspace
            ExtendedUser user = null;
            List<PersonalWorkspace> personalWorkspaces = null;
            if (!OrchAPISession.AuthManager.IsConfidentialApp)
            {
                tasks.Add(Task.Run(() =>
                {
                    // Auxiliary fetch — the result only enriches folder enumeration with the
                    // current user's personal workspace. Failures here (typically missing optional
                    // scopes) are non-fatal; folders are fetched independently below.
                    try { user = CurrentUser.Get() as ExtendedUser; }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"GetCurrentUser failed: {ex.Message}"); }
                }));

                // get personal workspaces that being explored
                tasks.Add(Task.Run(() =>
                {
                    // Auxiliary fetch — see comment on the GetCurrentUser task above.
                    try { personalWorkspaces = PersonalWorkspaces.Get(); }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"PersonalWorkspaces.Get failed: {ex.Message}"); }
                }));
            }

            // get folders
            List<Folder> folders = null;
            tasks.Add(Task.Run(() =>
            {
                // If an exception occurs, let it propagate
                folders = OrchAPISession.GetFolders().ToList();
            }));

            Task.WaitAll([.. tasks]);

            // Build into local lists; publish atomically below so concurrent readers
            // never see a partially populated cache.
            var newDicFolders = new List<Folder>();
            var newDicFoldersForEnumFolders = new List<Folder>();

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
                newDicFolders.Add(user.PersonalWorkspace);
                newDicFoldersForEnumFolders.Add(user.PersonalWorkspace);
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
                        newDicFolders.Add(pwFolder);
                        newDicFoldersForEnumFolders.Add(pwFolder);
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
            if (folders is not null)
            {
                #region Build newDicFolders
                // 1. First, add all folders directly under the root
                // 1-1. Add all personal workspace folders (already done above)
                // 1-2. Add the remaining folders directly under the root
                newDicFolders.AddRange(folders
                    .Where(f => !f.FullyQualifiedName!.Contains('/'))
                    .OrderBy(f => f.FullyQualifiedNameOrderable));

                // 2. Add all folders under personal workspace folders
                newDicFolders.AddRange(folders
                    .Where(f => f.FeedType == "PersonalWorkspace")
                    .Where(f => f.FullyQualifiedName!.Contains('/')) // This filter may not be necessary, but keeping it
                    .OrderBy(f => f.FullyQualifiedNameOrderable));

                // 3. Add all remaining folders
                newDicFolders.AddRange(folders
                    .Where(f => f.FeedType != "PersonalWorkspace")
                    .Where(f => f.FullyQualifiedName!.Contains('/'))
                    .OrderBy(f => f.FullyQualifiedNameOrderable));
                #endregion

                #region Build newDicFoldersForEnumFolders
                // newDicFoldersForEnumFolders is used by OrchDriveInfo.EnumFolders(). The sort order differs slightly.
                // 1. First, add all personal workspace folders (only root-level ones are done so far)
                // Add folders under personal workspace folders, then re-sort by FullyQualifiedName
                newDicFoldersForEnumFolders.AddRange(folders
                    .Where(f => f.FeedType == "PersonalWorkspace"));
                newDicFoldersForEnumFolders = newDicFoldersForEnumFolders
                    .OrderBy(f => f.FullyQualifiedNameOrderable)
                    .ToList();

                // 2. Add all remaining folders
                newDicFoldersForEnumFolders.AddRange(folders
                    .Where(f => f.FeedType != "PersonalWorkspace")
                    .OrderBy(f => f.FullyQualifiedNameOrderable));
                #endregion
            }

            // Publish — EnumFolders reads _dicFoldersForEnumFolders directly, so publish it
            // first; that way any reader that has just observed a non-null _dicFolders will
            // also see the matching _dicFoldersForEnumFolders.
            Volatile.Write(ref _dicFoldersForEnumFolders, newDicFoldersForEnumFolders);
            Volatile.Write(ref _dicFolders, newDicFolders);
            return newDicFolders.AsReadOnly();
        }
    }

    // Append a newly-created folder to both caches under the same lock GetFolders uses.
    // No-op when a cache slot is null (a concurrent Clear-OrchCache happened; the next
    // GetFolders rebuilds from the API and the new folder is picked up). Sort order is
    // not restored — call sites that need it null the caches afterwards so GetFolders
    // rebuilds.
    internal void AppendFolderToCache(Folder folder)
    {
        lock (_foldersInitLock)
        {
            _dicFolders?.Add(folder);
            _dicFoldersForEnumFolders?.Add(folder);
        }
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
