using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Core;

// Cache classes for OrchDuDriveInfo.
//
// These mirror the OrchDriveInfo / OrchTmDriveInfo cache families in OrchCache.cs.
// DU has its own drive type (OrchDuDriveInfo : PSDriveInfo) parallel to Orch and
// Tm, so the cache classes are also mirrored rather than reusing the OrchDriveInfo-
// bound versions. Org-scoped variants reach partitionGlobalId via the DU drive's
// ParentDrive (the Orch drive of the same org), matching how the legacy _dicDuRoles
// path computed it.
//
// All classes auto-register in drive._allTenantCache during construction so
// OrchDuDriveInfo.ClearAllCache() can iterate the registry uniformly. This is the
// structural fix for the historical bug where _dicDuExtractors was missed from
// the manual clear loop in OrchDuDriveInfo.ClearAllCache (pre-1.4.3).

/// <summary>
/// Tenant-scoped list cache for the DU drive. Mirrors
/// <see cref="ListCachePerTenant{T}"/> but bound to <see cref="OrchDuDriveInfo"/>.
/// </summary>
public class DuListCachePerTenant<T> : ITenantCacheClearable
{
    private readonly object _lock = new();
    private readonly OrchDuDriveInfo _drive;
    // volatile + publish-after-init: weakly-ordered CPUs must never observe a
    // non-null _cache pointing at a partially-initialized list.
    private volatile List<T>? _cache = null;
    private readonly ExceptionCachePerTenant _exception = new();
    private readonly Func<IEnumerable<T>> _getter;
    private readonly Action<T>? _initializer;

    public DuListCachePerTenant(
        OrchDuDriveInfo drive,
        Func<IEnumerable<T>> getter,
        Action<T>? initializer = null)
    {
        _drive = drive;
        _drive._allTenantCache.Add(this);
        _getter = getter;
        _initializer = initializer;
    }

    public List<T> Get()
    {
        _exception.ThrowCachedExceptionIfAny();

        if (_cache is null)
        {
            try
            {
                lock (_lock)
                {
                    if (_cache is null)
                    {
                        var temp = _getter().ToList();
                        if (_initializer is not null)
                        {
                            foreach (var entity in temp)
                            {
                                _initializer(entity);
                            }
                        }
                        // Publish after init.
                        _cache = temp;
                    }
                }
            }
            catch (Exception ex) when (ex is HttpResponseException or DeterministicApiException)
            {
                _exception.CacheException(ex);
                throw;
            }
        }
        return _cache;
    }

    /// <summary>
    /// Passive read: returns the cached list if a Get() call has previously
    /// succeeded, otherwise null. Does NOT trigger a fetch and does NOT throw
    /// even if an exception is cached. Use from hot paths like provider
    /// NormalizeRelativePath that must not initiate an API call.
    /// </summary>
    public List<T>? CachedValue => _cache;

    public void ClearCache()
    {
        _cache = null;
        _exception.ClearCache();
    }
}

/// <summary>
/// Organization-scoped list cache for the DU drive. Mirrors the list-only path
/// of <see cref="ListCachePerOrganization{T}"/> (no ExpandDetail) but bound to
/// <see cref="OrchDuDriveInfo"/>. Static storage means all DU drives pointing at
/// the same org share the cache; per-partition locks serialize the fetch per org
/// while leaving other orgs parallel.
/// </summary>
public class DuListCachePerOrganization<T> : ITenantCacheClearable
{
    private static readonly ConcurrentDictionary<string, List<T>> _cache = [];
    private static readonly ExceptionsCachePer<string> _exception = new();
    // Per-partition lock — same partition → same lock → fetch serialized;
    // different partitions → different locks → parallel. Avoids the
    // instance-lock-guards-static-cache anti-pattern.
    private static readonly ConcurrentDictionary<string, object> _partitionLocks = new();

    private readonly OrchDuDriveInfo _drive;
    // input: partitionGlobalId
    private readonly Func<string, IEnumerable<T>> _getter;
    private readonly Action<T>? _initializer;

    public DuListCachePerOrganization(
        OrchDuDriveInfo drive,
        Func<string, IEnumerable<T>> getter,
        Action<T>? initializer = null)
    {
        _drive = drive;
        _drive._allTenantCache.Add(this);
        _getter = getter;
        _initializer = initializer;
    }

    public IEnumerable<T> Get()
    {
        var partitionGlobalId = _drive.ParentDrive.GetPartitionGlobalId();
        if (string.IsNullOrEmpty(partitionGlobalId)) yield break;

        _exception.ThrowCachedExceptionIfAny(partitionGlobalId);

        if (!_cache.TryGetValue(partitionGlobalId, out var cachePerOrg))
        {
            var partitionLock = _partitionLocks.GetOrAdd(partitionGlobalId, _ => new object());
            lock (partitionLock)
            {
                if (!_cache.TryGetValue(partitionGlobalId, out cachePerOrg))
                {
                    try
                    {
                        cachePerOrg = _getter(partitionGlobalId).ToList();
                        if (_initializer is not null)
                        {
                            foreach (var t in cachePerOrg)
                            {
                                if (t is not null) _initializer(t);
                            }
                        }
                        _cache[partitionGlobalId] = cachePerOrg;
                    }
                    catch (Exception ex) when (ex is HttpResponseException or DeterministicApiException)
                    {
                        _exception.CacheException(partitionGlobalId, ex);
                        throw;
                    }
                }
            }
        }

        foreach (var t in cachePerOrg.Where(t => t is not null))
        {
            yield return t;
        }
    }

    public void ClearCache()
    {
        var partitionGlobalId = _drive.ParentDrive._partitionGlobalId;
        if (string.IsNullOrEmpty(partitionGlobalId)) return;

        _cache.TryRemove(partitionGlobalId, out _);
        _exception.ClearCache(partitionGlobalId);
    }
}

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

/// <summary>
/// Organization-scoped per-key list cache for the DU drive. Mirrors
/// <see cref="KeyedListCachePerOrganization{TKey,TEntity}"/> but bound to
/// <see cref="OrchDuDriveInfo"/>. Used for DU caches keyed by
/// (tenantKey, projectId) within an org (DuUsers).
/// </summary>
public class DuKeyedListCachePerOrganization<TKey, TEntity> : ITenantCacheClearable
    where TKey : notnull, IEquatable<TKey>
{
    private static readonly ConcurrentDictionary<(string partitionGlobalId, TKey key), List<TEntity>> _cache = new();
    private static readonly ExceptionsCachePer<(string partitionGlobalId, TKey key)> _exceptions = new();
    // Per-partition lock — see KeyedSingleCachePerOrganization rationale.
    private static readonly ConcurrentDictionary<string, object> _partitionLocks = new();

    private readonly OrchDuDriveInfo _drive;
    private readonly Func<string, TKey, IEnumerable<TEntity>> _fetchFunc;
    private readonly Action<TEntity, TKey>? _initializer;

    public DuKeyedListCachePerOrganization(
        OrchDuDriveInfo drive,
        Func<string, TKey, IEnumerable<TEntity>> fetchFunc,
        Action<TEntity, TKey>? initializer = null)
    {
        _drive = drive;
        _drive._allTenantCache.Add(this);
        _fetchFunc = fetchFunc;
        _initializer = initializer;
    }

    public ReadOnlyCollection<TEntity> Get(TKey key)
    {
        var partitionGlobalId = _drive.ParentDrive.GetPartitionGlobalId();
        if (string.IsNullOrEmpty(partitionGlobalId))
        {
            return new List<TEntity>().AsReadOnly();
        }

        var compositeKey = (partitionGlobalId, key);
        _exceptions.ThrowCachedExceptionIfAny(compositeKey);

        if (_cache.TryGetValue(compositeKey, out var list)) return list.AsReadOnly();

        var partitionLock = _partitionLocks.GetOrAdd(partitionGlobalId, _ => new object());
        lock (partitionLock)
        {
            if (_cache.TryGetValue(compositeKey, out list)) return list.AsReadOnly();

            try
            {
                list = _fetchFunc(partitionGlobalId, key).ToList();
                if (_initializer is not null)
                {
                    foreach (var entity in list) _initializer(entity, key);
                }
                _cache[compositeKey] = list;
                return list.AsReadOnly();
            }
            catch (Exception ex) when (ex is HttpResponseException or DeterministicApiException)
            {
                _exceptions.CacheException(compositeKey, ex);
                throw;
            }
        }
    }

    public void ClearCache(TKey key)
    {
        // Read the cached partition id directly; don't trigger auth on
        // Clear-OrchCache. Mirrors KeyedListCachePerOrganization.
        var partitionGlobalId = _drive.ParentDrive._partitionGlobalId;
        if (string.IsNullOrEmpty(partitionGlobalId)) return;

        var compositeKey = (partitionGlobalId, key);
        _cache.TryRemove(compositeKey, out _);
        _exceptions.ClearCache(compositeKey);
    }

    public void ClearCache()
    {
        var partitionGlobalId = _drive.ParentDrive._partitionGlobalId;
        if (string.IsNullOrEmpty(partitionGlobalId)) return;

        foreach (var k in _cache.Keys.Where(k => k.partitionGlobalId == partitionGlobalId).ToList())
        {
            _cache.TryRemove(k, out _);
        }
        _exceptions.ClearCache(k => k.partitionGlobalId == partitionGlobalId);
    }
}
