using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Core;

// Test Manager-specific cache classes. Moved out of OrchCache.cs in the 1.5.0
// cache refactor to mirror the OrchDuCache.cs precedent (family-specific cache
// classes live under their family folder; the universal templates stay in
// OrchCache.cs at the top level).
//
// All three are project-scoped (keyed by TmProject.id):
//
//   IncrementalCachePerProject<TKey, TEntity>
//       Filter-and-accumulate cache for large datasets that cannot be
//       enumerated up-front (TmTestExecutionResult).
//
//   TmListCachePerTenant1<T>     (name kept; 1 = "1-arity getter takes a TmProject")
//       Per-project list cache for TmTestCase / TmTestSet / TmTestExecution /
//       TmRequirement / TmProjectPermission.
//
//   TmSingleCachePerTenant1<T>   (name kept; 1 = "1-arity getter")
//       Per-project single-value cache for TmProjectSettings.
//
// Phase 5 of the refactor will consider folding these into a generalized
// PerFolder<TFolderId,...> shape (with TFolderId = string for Tm/Du projects),
// at which point the family-specific subclasses disappear entirely.

/// <summary>
/// Dictionary-type cache that accumulates filtering results per project.
/// Designed for large datasets like TmTestExecutionResult. Does not fetch all items; appends API query results incrementally.
/// </summary>
/// <typeparam name="TKey">Entity key type (e.g., string)</typeparam>
/// <typeparam name="TEntity">Entity type (e.g., TmTestExecutionResult)</typeparam>
public class IncrementalCachePerProject<TKey, TEntity> : ITenantCacheClearable
    where TKey : notnull
{
    private readonly object _lock = new();
    private readonly OrchTmDriveInfo _drive;

    // 1st key: projectId, 2nd key: entityKey
    private volatile ConcurrentDictionary<string, ConcurrentDictionary<TKey, TEntity>>? _cache = null;
    private readonly ExceptionsCachePer<string> _exceptions = new();

    // API call function
    private readonly Func<string, string, IEnumerable<TEntity>> _fetchFunc;

    // Function to get the key from an entity
    private readonly Func<TEntity, TKey?> _getKeyFunc;

    // Initialization processing (e.g., setting Path)
    private readonly Action<TEntity, TmProject>? _initializer;

    // Additional processing before adding to cache (e.g., merging with existing cache)
    private readonly Action<TEntity, TEntity?>? _mergeFunc;

    public IncrementalCachePerProject(
        OrchTmDriveInfo drive,
        Func<string, string, IEnumerable<TEntity>> fetchFunc,
        Func<TEntity, TKey?> getKeyFunc,
        Action<TEntity, TmProject>? initializer = null,
        Action<TEntity, TEntity?>? mergeFunc = null)
    {
        _drive = drive;
        _drive._allTenantCache.Add(this);
        _fetchFunc = fetchFunc;
        _getKeyFunc = getKeyFunc;
        _initializer = initializer;
        _mergeFunc = mergeFunc;
    }

    /// <summary>
    /// Gets the cache Dictionary for a project (for read-only access)
    /// </summary>
    public ConcurrentDictionary<TKey, TEntity>? GetCache(TmProject project)
    {
        if (_cache is null) return null;
        _cache.TryGetValue(project.id ?? "", out var projectCache);
        return projectCache;
    }

    /// <summary>
    /// Calls the API to retrieve entities and adds them to the cache
    /// </summary>
    /// <param name="project">Target project</param>
    /// <param name="subKey">Sub-key (e.g., testExecutionId)</param>
    public ReadOnlyCollection<TEntity> Fetch(TmProject project, string subKey)
    {
        string projectId = project.id ?? "";

        _exceptions.ThrowCachedExceptionIfAny(projectId);

        if (_cache is null)
        {
            lock (_lock)
            {
                _cache ??= new();
            }
        }

        if (!_cache.TryGetValue(projectId, out var projectCache))
        {
            projectCache = new();
            _cache[projectId] = projectCache;
        }

        List<TEntity> fetched;
        try
        {
            fetched = _fetchFunc(projectId, subKey).ToList();
        }
        catch (Exception ex) when (ex is HttpResponseException or DeterministicApiException)
        {
            _exceptions.CacheException(projectId, ex);
            throw;
        }

        foreach (var entity in fetched)
        {
            _initializer?.Invoke(entity, project);

            var key = _getKeyFunc(entity);
            if (key is null) continue;

            // See IncrementalCachePerFolder.Fetch for the atomicity rationale.
            if (_mergeFunc is null)
            {
                projectCache[key] = entity;
            }
            else
            {
                projectCache.AddOrUpdate(
                    key,
                    addValueFactory: _ => entity,
                    updateValueFactory: (_, cached) => { _mergeFunc(entity, cached); return entity; });
            }
        }

        return fetched.AsReadOnly();
    }

    public void ClearCache(string? projectId)
    {
        if (projectId is null) return;
        _cache?.TryRemove(projectId, out _);
        _exceptions.ClearCache(projectId);
    }

    public void ClearCache(TmProject project)
    {
        ClearCache(project.id);
    }

    /// <summary>
    /// Adds a single entity to the cache (for calling after external API retrieval)
    /// </summary>
    public void AddToCache(TmProject project, TEntity entity)
    {
        string projectId = project.id ?? "";

        if (_cache is null)
        {
            lock (_lock)
            {
                _cache ??= new();
            }
        }

        if (!_cache.TryGetValue(projectId, out var projectCache))
        {
            projectCache = new();
            _cache[projectId] = projectCache;
        }

        var key = _getKeyFunc(entity);
        if (key is null) return;

        // See IncrementalCachePerFolder.Fetch for the atomicity rationale.
        if (_mergeFunc is null)
        {
            projectCache[key] = entity;
        }
        else
        {
            projectCache.AddOrUpdate(
                key,
                addValueFactory: _ => entity,
                updateValueFactory: (_, cached) => { _mergeFunc(entity, cached); return entity; });
        }
    }

    public void ClearCache()
    {
        _cache = null;
        _exceptions.ClearCache();
    }
}

// fetchFunc: Func that takes a TmProject and returns Enumerable<T>
public class TmListCachePerTenant1<T> : ITenantCacheClearable
{
    private readonly object _lock = new();
    private readonly OrchTmDriveInfo _drive;
    internal volatile ConcurrentDictionary<string, List<T>>? _cache = null;
    private readonly ExceptionsCachePer<string> _exceptions = new();
    private readonly Func<TmProject, IEnumerable<T>> _fetchFunc;
    private readonly Action<T, TmProject>? _initializer;
    private readonly int? _supportedApiVersionFrom;

    public TmListCachePerTenant1(
        OrchTmDriveInfo drive,
        Func<TmProject, IEnumerable<T>> fetchFunc,
        Action<T, TmProject>? initializer = null,
        int? supportedApiVersionFrom = null)
    {
        _drive = drive;
        _fetchFunc = fetchFunc;
        _drive._allTenantCache.Add(this);
        _initializer = initializer;
        _supportedApiVersionFrom = supportedApiVersionFrom;
    }

    /// <summary>
    /// Checks whether the cache exists
    /// </summary>
    public bool HasCache(TmProject project)
    {
        return _cache?.ContainsKey(project.id!) ?? false;
    }

    public List<T> Get(TmProject project)
    {
        if (_drive.OrchAPISession.ApiVersion < _supportedApiVersionFrom)
        {
            return [];
        }

        _exceptions.ThrowCachedExceptionIfAny(project.id!);

        if (_cache is null)
        {
            lock (_lock)
            {
                _cache ??= [];
            }
        }

        if (!_cache.TryGetValue(project.id!, out var cachePerFolder))
        {
            // Double-check inside the lock — see IndexedCachePerTenant.Get for
            // the symmetry rationale.
            lock (_lock)
            {
                if (!_cache.TryGetValue(project.id!, out cachePerFolder))
                {
                    try
                    {
                        cachePerFolder = _fetchFunc(project).ToList();

                        if (_initializer is not null)
                        {
                            string projectPath = project.GetPSPath();
                            foreach (var entity in cachePerFolder)
                            {
                                _initializer(entity, project);
                            }
                        }
                        // Publish after init.
                        _cache[project.id!] = cachePerFolder;
                    }
                    catch (Exception ex) when (ex is HttpResponseException or DeterministicApiException)
                    {
                        _exceptions.CacheException(project.id ?? "", ex);
                        throw;
                    }
                }
            }
        }
        return cachePerFolder;
    }

    public void ClearCache(string? projectId)
    {
        if (projectId is null) return;
        _cache?.TryRemove(projectId, out _);
        _exceptions.ClearCache(projectId);
    }

    public void ClearCache()
    {
        _cache = null;
        _exceptions.ClearCache();
    }
}

// getFunc: Func that takes a TmProject and returns T
public class TmSingleCachePerTenant1<T> : ITenantCacheClearable
{
    private readonly object _lock = new();
    private readonly OrchTmDriveInfo _drive;
    private volatile ConcurrentDictionary<string, T?>? _cache = null;
    private readonly ExceptionsCachePer<string> _exceptions = new();
    private readonly Func<TmProject, T?> _getFunc;
    private readonly Action<T, TmProject>? _initializer;
    private readonly int? _supportedApiVersionFrom;

    public TmSingleCachePerTenant1(
        OrchTmDriveInfo drive,
        Func<TmProject, T?> getFunc,
        Action<T, TmProject>? initializer = null,
        int? supportedApiVersionFrom = null)
    {
        _drive = drive;
        _getFunc = getFunc;
        _drive._allTenantCache.Add(this);
        _initializer = initializer;
        _supportedApiVersionFrom = supportedApiVersionFrom;
    }

    public T? Get(TmProject project)
    {
        if (_drive.OrchAPISession.ApiVersion < _supportedApiVersionFrom)
        {
            return default;
        }

        _exceptions.ThrowCachedExceptionIfAny(project.id!);

        if (_cache is null)
        {
            lock (_lock)
            {
                _cache ??= [];
            }
        }

        if (!_cache.TryGetValue(project.id!, out var cachePerProject))
        {
            // Double-check inside the lock — see IndexedCachePerTenant.Get for
            // the symmetry rationale.
            lock (_lock)
            {
                if (!_cache.TryGetValue(project.id!, out cachePerProject))
                {
                    try
                    {
                        cachePerProject = _getFunc(project);

                        if (_initializer is not null && cachePerProject is not null)
                        {
                            string projectPath = project.GetPSPath();
                            _initializer(cachePerProject, project);
                        }
                        // Publish after init.
                        _cache[project.id!] = cachePerProject;
                    }
                    catch (Exception ex) when (ex is HttpResponseException or DeterministicApiException)
                    {
                        _exceptions.CacheException(project.id ?? "", ex);
                        throw;
                    }
                }
            }
        }
        return cachePerProject;
    }

    public void ClearCache(string? projectId)
    {
        if (projectId is null) return;
        _cache?.TryRemove(projectId, out _);
        _exceptions.ClearCache(projectId);
    }

    public void ClearCache()
    {
        _cache = null;
        _exceptions.ClearCache();
    }
}
