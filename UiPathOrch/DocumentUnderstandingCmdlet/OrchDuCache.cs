using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Core;

// DU-specific cache classes that don't yet have a universal counterpart in
// OrchCache.cs.
//
// Until the 1.5.0 cache refactor, this file also mirrored ListCachePerTenant /
// ListCachePerOrganization / KeyedListCachePerOrganization as Du*-prefixed
// classes; those were retired when the universal classes were generalized to
// accept OrchDriveInfoBase. Only DuListCachePerProject remains, because there
// is no universal "PerProject" scope yet -- Phase 5 of the refactor will fold
// it into a generalized PerFolder<string, ...> shape.

/// <summary>
/// Per-project list cache for the DU drive: one list of <typeparamref name="TEntity"/>
/// per <see cref="DuProject"/>, scoped to a single tenant (this drive instance).
/// Concrete <see cref="DuProject"/>-keyed analog of <see cref="KeyedListCachePerTenant{TKey,TEntity}"/>
/// — used for DocumentTypes, Classifiers, and Extractors. The project-keyed
/// API (<c>Get(DuProject)</c>, not <c>Get(string)</c>) is the only key shape
/// these endpoints take, so the class is concrete on <see cref="DuProject"/>
/// rather than generic over <c>TKey</c>; the storage uses <c>project.id</c>
/// internally for the dictionary.
/// </summary>
public class DuListCachePerProject<TEntity> : ITenantCacheClearable
{
    private readonly object _lock = new();
    private readonly OrchDuDriveInfo _drive;
    private volatile ConcurrentDictionary<string, List<TEntity>>? _cache = null;
    private readonly ExceptionsCachePer<string> _exceptions = new();
    private readonly Func<DuProject, IEnumerable<TEntity>> _fetchFunc;
    private readonly Action<TEntity, DuProject>? _initializer;

    public DuListCachePerProject(
        OrchDuDriveInfo drive,
        Func<DuProject, IEnumerable<TEntity>> fetchFunc,
        Action<TEntity, DuProject>? initializer = null)
    {
        _drive = drive;
        _drive._allTenantCache.Add(this);
        _fetchFunc = fetchFunc;
        _initializer = initializer;
    }

    public ReadOnlyCollection<TEntity> Get(DuProject project)
    {
        if (project.id is null) return new List<TEntity>().AsReadOnly();

        _exceptions.ThrowCachedExceptionIfAny(project.id);

        if (_cache is null)
        {
            lock (_lock)
            {
                _cache ??= new ConcurrentDictionary<string, List<TEntity>>();
            }
        }

        if (!_cache.TryGetValue(project.id, out var list))
        {
            // Double-check inside the lock so concurrent callers don't both fetch.
            lock (_lock)
            {
                if (!_cache.TryGetValue(project.id, out list))
                {
                    try
                    {
                        list = _fetchFunc(project).ToList();
                        if (_initializer is not null)
                        {
                            foreach (var entity in list)
                            {
                                _initializer(entity, project);
                            }
                        }
                        // Publish after init.
                        _cache[project.id] = list;
                    }
                    catch (Exception ex) when (ex is HttpResponseException or DeterministicApiException)
                    {
                        _exceptions.CacheException(project.id, ex);
                        throw;
                    }
                }
            }
        }

        return list.AsReadOnly();
    }

    public void ClearCache(string projectId)
    {
        _cache?.TryRemove(projectId, out _);
        _exceptions.ClearCache(projectId);
    }

    public void ClearCache()
    {
        _cache = null;
        _exceptions.ClearCache();
    }
}
