using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Core;

public interface ITenantCacheClearable
{
    void ClearCache();
}

public interface IFolderCacheClearable
{
    void ClearCache();
    void ClearCache(Folder folder);
}

public class SingleCachePerTenant<T> : ITenantCacheClearable where T : class
{
    private readonly object _lock = new();
    private readonly OrchDriveInfo _drive;
    // volatile + publish-after-init: ensures readers on weakly-ordered CPUs (e.g. ARM/Apple Silicon)
    // never observe a non-null _cache that points at a partially-initialized object.
    private volatile T? _cache = null;
    private readonly ExceptionCachePerTenant _exception = new();
    private readonly Func<T?> getter;
    private readonly Action<T>? initializer;

    public SingleCachePerTenant(OrchDriveInfo drive, Func<T?> getter, Action<T>? initializer = null)
    {
        _drive = drive;
        _drive._allTenantCache.Add(this);
        this.getter = getter;
        this.initializer = initializer;
    }

    public T? Get()
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
                        var temp = getter();
                        if (temp is not null && initializer is not null)
                        {
                            initializer(temp);
                        }
                        _cache = temp;
                    }
                }
            }
            catch (HttpResponseException ex)
            {
                _exception.CacheException(ex);
                throw;
            }
        }
        return _cache;
    }

    public void ClearCache()
    {
        _cache = null;
        _exception.ClearCache();
    }
}

public class ListCachePerTenant<T> : ITenantCacheClearable
{
    private readonly object _lock = new();
    private readonly OrchDriveInfo _drive;
    private volatile List<T>? _cache = null;
    private readonly ExceptionCachePerTenant _exception = new();
    private readonly Func<IEnumerable<T>> getter;
    private readonly Action<T>? initializer;

    public ListCachePerTenant(OrchDriveInfo drive, Func<IEnumerable<T>> getter, Action<T>? initializer = null)
    {
        _drive = drive;
        _drive._allTenantCache.Add(this);
        this.getter = getter;
        this.initializer = initializer;
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
                        var temp = getter().ToList();
                        if (initializer is not null)
                        {
                            foreach (var entity in temp)
                            {
                                initializer(entity);
                            }
                        }
                        _cache = temp;
                    }
                }
            }
            catch (HttpResponseException ex)
            {
                _exception.CacheException(ex);
                throw;
            }
        }
        return _cache;
    }

    public void ClearCache()
    {
        _cache = null;
        _exception.ClearCache();
    }
}

// Organization entities keyed by partitionGlobalId.
// This represents the cache of unique entities across all organizations.
public class ListCachePerOrganization<T> : ITenantCacheClearable
{
    private readonly object _lock = new();
    private readonly OrchDriveInfo _drive;
    private static readonly ConcurrentDictionary<string, List<T>> _cache = [];
    private static readonly ExceptionsCachePer<string> _exception = new(); // Holds per-org exceptions
    // input: partitionGlobalId
    private readonly Func<string, IEnumerable<T>> _getter;
    private readonly Action<T>? _initializer;

    // The following is also needed for entities that support -ExpandDetail
    private readonly Func<T, string?>? _getterId;
    // Detailed cache of organization entities keyed by (partitionGlobalId, id)
    private static volatile ConcurrentDictionary<(string partitionGlobalId, string id), T?>? _cacheDetailed = null;
    private readonly Func<string, string, T?>? _getterDetailed;
    // Exception cache for detailed organization entity retrieval keyed by (partitionGlobalId, id)
    private static readonly ExceptionsCachePer<(string partitionGlobalId, string id)> _exceptionDetailed = new(); // Holds per (org, id) exceptions

    public ListCachePerOrganization(
        OrchDriveInfo drive,
        //ConcurrentDictionary<string, List<T>> cache,
        Func<string, IEnumerable<T>> getter,
        Action<T>? initializer,
        Func<T, string?>? getterId = null,
        Func<string, string, T?>? getterDetailed = null)
    {
        _drive = drive;
        _drive._allTenantCache.Add(this);
        _getter = getter;
        _initializer = initializer;

        _getterId = getterId;
        _getterDetailed = getterDetailed;
    }

    public IEnumerable<T> Get()
    {
        var partitionGlobalId = _drive.GetPartitionGlobalId();
        if (string.IsNullOrEmpty(partitionGlobalId)) yield break;

        _exception.ThrowCachedExceptionIfAny(partitionGlobalId);

        if (!_cache.TryGetValue(partitionGlobalId, out var cachePerOrg))
        {
            lock (_lock)
            {
                if (!_cache.TryGetValue(partitionGlobalId, out cachePerOrg))
                {
                    try
                    {
                        cachePerOrg = _getter(partitionGlobalId).ToList();
                        _cache[partitionGlobalId] = cachePerOrg;
                    }
                    catch (HttpResponseException ex)
                    {
                        _exception.CacheException(partitionGlobalId, ex);
                        throw;
                    }
                }
            }
        }
        foreach (var t in cachePerOrg.Where(t => t is not null))
        {
            // Return the detailed cache if it exists
            if (_cacheDetailed is not null && _cacheDetailed.TryGetValue((partitionGlobalId, _getterId!(t)!), out var detailedEntity))
            {
                if (_initializer is not null && detailedEntity is not null)
                {
                    _initializer(detailedEntity);
                    yield return detailedEntity;
                }
            }
            else
            {
                if (_initializer is not null)
                {
                    _initializer(t);
                }
                yield return t;
            }
        }
    }

    public T? Get(string? id)
    {
        if (_getterDetailed is null) throw new NotSupportedException();

        if (string.IsNullOrEmpty(id)) return default;

        var partitionGlobalId = _drive.GetPartitionGlobalId()!;

        _exceptionDetailed.ThrowCachedExceptionIfAny((partitionGlobalId, id));

        if (_cacheDetailed is null)
        {
            lock (_lock)
            {
                _cacheDetailed ??= [];
            }
        }

        if (!_cacheDetailed.TryGetValue((partitionGlobalId, id), out var cachePerOrgDetailed))
        {
            lock (_lock)
            {
                if (!_cacheDetailed.TryGetValue((partitionGlobalId, id), out cachePerOrgDetailed))
                {
                    try
                    {
                        cachePerOrgDetailed = _getterDetailed(partitionGlobalId, id);
                        _cacheDetailed[(partitionGlobalId, id)] = cachePerOrgDetailed;
                    }
                    catch (HttpResponseException ex)
                    {
                        _exception.CacheException(partitionGlobalId, ex);
                        throw;
                    }
                }
            }
        }
        if (_initializer is not null && cachePerOrgDetailed is not null)
        {
            _initializer(cachePerOrgDetailed);
        }

        return cachePerOrgDetailed;
    }

    public void Set(T t)
    {
        var partitionGlobalId = _drive.GetPartitionGlobalId();
        var id = _getterId?.Invoke(t);
        if (string.IsNullOrEmpty(partitionGlobalId) || string.IsNullOrEmpty(id)) return;

        // Update _cache (do nothing if the list does not exist)
        if (_cache.TryGetValue(partitionGlobalId, out var list))
        {
            // No lock needed since this is assumed to be called only from the main thread
            bool replaced = false;
            for (int i = 0; i < list.Count; i++)
            {
                if (string.Compare(_getterId?.Invoke(list[i]), id, true) == 0)
                {
                    list[i] = t;
                    replaced = true;
                    break;
                }
            }
            if (!replaced)
            {
                list.Add(t);
            }
        }

        // Upsert into _cacheDetailed
        if (_cacheDetailed is not null)
        {
            _cacheDetailed[(partitionGlobalId, id)] = t;
        }
    }

    public void ClearCache(string? id)
    {
        if (string.IsNullOrEmpty(_drive._dicPartitionGlobalId)) return;

        if (!string.IsNullOrEmpty(id))
        {
            _cacheDetailed?.TryRemove((_drive._dicPartitionGlobalId, id), out var _);
        }
        _cache.TryRemove(_drive._dicPartitionGlobalId, out var _);
        _exception.ClearCache(_drive._dicPartitionGlobalId);
    }

    public void ClearCache()
    {
        if (string.IsNullOrEmpty(_drive._dicPartitionGlobalId)) return;

        if (_cacheDetailed != null && !_cacheDetailed.IsEmpty)
        {
            foreach (var pair in _cacheDetailed)
            {
                var key = pair.Key;
                if (key.partitionGlobalId == _drive._dicPartitionGlobalId)
                {
                    _cacheDetailed.TryRemove(key, out _);
                }
            }
        }

        _cache.TryRemove(_drive._dicPartitionGlobalId, out _);
        _exception.ClearCache(_drive._dicPartitionGlobalId);
    }

}

// Single organization entity keyed by partitionGlobalId.
// This represents the cache of a single entity shared across the entire organization.
public class SingleCachePerOrganization<T> : ITenantCacheClearable where T : class
{
    private readonly object _lock = new();
    private readonly OrchDriveInfo _drive;

    // Cache per organization (static = shared across all drive instances)
    private static readonly ConcurrentDictionary<string, T> _cache = [];
    private static readonly ExceptionsCachePer<string> _exception = new();

    // getter: takes partitionGlobalId as argument and returns T
    private readonly Func<string, T?> _getter;
    private readonly Action<T>? _initializer;

    public SingleCachePerOrganization(
        OrchDriveInfo drive,
        Func<string, T?> getter,
        Action<T>? initializer = null)
    {
        _drive = drive;
        _drive._allTenantCache.Add(this);
        _getter = getter;
        _initializer = initializer;
    }

    public T? Get()
    {
        var partitionGlobalId = _drive.GetPartitionGlobalId();
        if (string.IsNullOrEmpty(partitionGlobalId)) return null;

        _exception.ThrowCachedExceptionIfAny(partitionGlobalId);

        if (!_cache.TryGetValue(partitionGlobalId, out var entity))
        {
            lock (_lock)
            {
                if (!_cache.TryGetValue(partitionGlobalId, out entity))
                {
                    try
                    {
                        entity = _getter(partitionGlobalId);
                        if (entity is not null)
                        {
                            // Run initializer before publishing to the dictionary so concurrent readers
                            // never observe an entity whose initialization is still in progress.
                            _initializer?.Invoke(entity);
                            _cache[partitionGlobalId] = entity;
                        }
                    }
                    catch (HttpResponseException ex)
                    {
                        _exception.CacheException(partitionGlobalId, ex);
                        throw;
                    }
                }
            }
        }
        else if (entity is not null && _initializer is not null)
        {
            // Execute initializer even when cached (e.g., setting Path)
            _initializer(entity);
        }

        return entity;
    }

    public void ClearCache()
    {
        var partitionGlobalId = _drive._dicPartitionGlobalId;
        if (string.IsNullOrEmpty(partitionGlobalId)) return;

        _cache.TryRemove(partitionGlobalId, out _);
        _exception.ClearCache(partitionGlobalId);
    }
}

// Currently, only Int64 is supported as the index type. This type might benefit from parameterization.
// TIndexEntity: Entity containing the index
// getIndex(TIndexEntity): Func to get the Int64 index value from TIndexEntity
// TEntity: Entity to retrieve and cache
public class IndexedCachePerTenant<TIndexEntity, TEntity> : ITenantCacheClearable where TIndexEntity : notnull
{
    private readonly object _lock = new();
    private readonly OrchDriveInfo _drive;

    // key: TIndexEntity.Id
    private volatile ConcurrentDictionary<Int64, TEntity?>? _cache = null;
    private readonly ExceptionsCachePer<Int64> _exceptions = new();
    private readonly Func<Int64, TEntity?> _fetchFunc; // Passes an index and returns TEntity
    private readonly Func<TIndexEntity, Int64> _getIdFunc;
    private readonly Func<TIndexEntity, string> _getNameFunc;
    private readonly Action<TEntity, string>? _initializer; // Name
    private readonly int? _supportedApiVersionFrom;

    public IndexedCachePerTenant(
        OrchDriveInfo drive,
        Func<Int64, TEntity?> fetchFunc,
        Func<TIndexEntity, Int64> getIdFunc,
        Func<TIndexEntity, string> getNameFunc,
        Action<TEntity, string>? initializer,
        int? supportedApiVersionFrom = null)
    {
        _drive = drive;
        _drive._allTenantCache.Add(this);
        _fetchFunc = fetchFunc;
        _getIdFunc = getIdFunc;
        _getNameFunc = getNameFunc;
        _initializer = initializer;
        _supportedApiVersionFrom = supportedApiVersionFrom;
    }

    public TEntity? Get(TIndexEntity indexEntity)
    {
        if (_drive.OrchAPISession.ApiVersion < _supportedApiVersionFrom)
        {
            return default;
        }

        Int64 index = _getIdFunc(indexEntity);

        _exceptions.ThrowCachedExceptionIfAny(index);

        if (_cache is null)
        {
            lock (_lock)
            {
                _cache ??= [];
            }
        }

        if (!_cache.TryGetValue(index, out var entity))
        {
            try
            {
                entity = _fetchFunc(index);

                if (entity is not null && _initializer is not null)
                {
                    string indexName = _getNameFunc(indexEntity);
                    _initializer(entity, indexName);
                }
                // Publish to the cache only after initialization, so concurrent readers
                // never observe a partially-initialized entity.
                _cache[index] = entity;
            }
            catch (HttpResponseException ex)
            {
                _exceptions.CacheException(index, ex);
                throw;
            }
        }

        return entity;
    }

    public void ClearCache()
    {
        _cache = null;
        _exceptions.ClearCache();
    }
}

/// <summary>
/// Tenant-scoped cache of List&lt;TEntity&gt; entries keyed by an arbitrary
/// <typeparamref name="TKey"/> (typically string — e.g. robot type, feed id,
/// query string). Each key is fetched on first access via <c>fetchFunc</c>;
/// subsequent <c>Get(key)</c> calls return the cached list. Per-key
/// HttpResponseException caching mirrors the other indexed cache classes,
/// so a 4xx/5xx for one key sticks until the next <c>ClearCache(key)</c>.
/// </summary>
public class KeyedListCachePerTenant<TKey, TEntity> : ITenantCacheClearable
    where TKey : notnull, IEquatable<TKey>
{
    private readonly object _lock = new();
    private readonly OrchDriveInfo _drive;

    private volatile ConcurrentDictionary<TKey, List<TEntity>>? _cache = null;
    private readonly ExceptionsCachePer<TKey> _exceptions = new();
    private readonly Func<TKey, IEnumerable<TEntity>> _fetchFunc;
    private readonly Action<TEntity, TKey>? _initializer;
    private readonly IEqualityComparer<TKey>? _keyComparer;
    private readonly int? _supportedApiVersionFrom;

    public KeyedListCachePerTenant(
        OrchDriveInfo drive,
        Func<TKey, IEnumerable<TEntity>> fetchFunc,
        Action<TEntity, TKey>? initializer = null,
        IEqualityComparer<TKey>? keyComparer = null,
        int? supportedApiVersionFrom = null)
    {
        _drive = drive;
        _drive._allTenantCache.Add(this);
        _fetchFunc = fetchFunc;
        _initializer = initializer;
        _keyComparer = keyComparer;
        _supportedApiVersionFrom = supportedApiVersionFrom;
    }

    public ReadOnlyCollection<TEntity> Get(TKey key)
    {
        if (_drive.OrchAPISession.ApiVersion < _supportedApiVersionFrom)
        {
            return new List<TEntity>().AsReadOnly();
        }

        _exceptions.ThrowCachedExceptionIfAny(key);

        if (_cache is null)
        {
            lock (_lock)
            {
                _cache ??= _keyComparer is null
                    ? new ConcurrentDictionary<TKey, List<TEntity>>()
                    : new ConcurrentDictionary<TKey, List<TEntity>>(_keyComparer);
            }
        }

        if (!_cache.TryGetValue(key, out var list))
        {
            try
            {
                list = _fetchFunc(key).ToList();
                if (_initializer is not null)
                {
                    foreach (var entity in list)
                    {
                        _initializer(entity, key);
                    }
                }
                // Publish to the cache only after init, so concurrent readers never
                // observe a partially-initialized list.
                _cache[key] = list;
            }
            catch (HttpResponseException ex)
            {
                _exceptions.CacheException(key, ex);
                throw;
            }
        }

        return list.AsReadOnly();
    }

    public void ClearCache(TKey key)
    {
        _cache?.TryRemove(key, out _);
        _exceptions.ClearCache(key);
    }

    public void ClearCache()
    {
        _cache = null;
        _exceptions.ClearCache();
    }
}

public class SingleCachePerFolder<T> : IFolderCacheClearable
{
    private readonly object _lock = new();
    private readonly OrchDriveInfo _drive;
    private volatile ConcurrentDictionary<Int64, T?>? _cache = null;
    private readonly ExceptionsCachePer<Int64> _exceptions = new();
    private readonly Func<Int64?, T?> _getter;
    private readonly Action<T, string>? _initializer;
    private readonly int? _supportedApiVersionFrom;

    public SingleCachePerFolder(
        OrchDriveInfo drive,
        Func<Int64?, T?> getter,
        Action<T, string>? initializer = null,
        int? supportedApiVersionFrom = null)
    {
        _drive = drive;
        _drive._allFolderCache.Add(this);
        _getter = getter;
        _initializer = initializer;
        _supportedApiVersionFrom = supportedApiVersionFrom;
    }

    public T? Get(Folder folder)
    {
        if (folder?.Id is null || _drive.OrchAPISession.ApiVersion < _supportedApiVersionFrom)
        {
            return default;
        }

        _exceptions.ThrowCachedExceptionIfAny(folder.Id.Value);

        if (_cache is null)
        {
            lock (_lock)
            {
                _cache ??= [];
            }
        }
        if (!_cache.TryGetValue(folder.Id!.Value, out var cachePerFolder))
        {
            try
            {
                cachePerFolder = _getter(folder.Id.Value);

                if (cachePerFolder is not null && _initializer is not null)
                {
                    string folderPath = folder.GetPSPath();
                    _initializer(cachePerFolder, folderPath);
                }
                // Publish after init.
                _cache[folder.Id.Value] = cachePerFolder;
            }
            catch (HttpResponseException ex)
            {
                _exceptions.CacheException(folder.Id ?? 0, ex);
                throw;
            }

        }
        return cachePerFolder;
    }

    public void ClearCache(Int64 folderId)
    {
        _cache?.TryRemove(folderId, out _);
        _exceptions.ClearCache(folderId);
    }

    public void ClearCache(Folder folder)
    {
        ClearCache(folder.Id!.Value);
    }

    public void ClearCache()
    {
        _cache = null;
        _exceptions.ClearCache();
    }
}

public class ListCachePerFolder<T> : IFolderCacheClearable
{
    private readonly object _lock = new();
    private readonly OrchDriveInfo _drive;
    private volatile ConcurrentDictionary<Int64, List<T>>? _cache = null;
    private readonly ExceptionsCachePer<Int64> _exceptions = new();
    private readonly Func<Int64, IEnumerable<T>> _getter;
    private readonly Action<T, string>? _initializer;
    private readonly int? _supportedApiVersionFrom;

    public ListCachePerFolder(
        OrchDriveInfo drive,
        Func<Int64, IEnumerable<T>> getter,
        Action<T, string>? initializer = null,
        int? supportedApiVersionFrom = null)
    {
        _drive = drive;
        _getter = getter;
        _drive._allFolderCache.Add(this);
        _initializer = initializer;
        _supportedApiVersionFrom = supportedApiVersionFrom;
    }

    public List<T> Get(Folder folder)
    {
        if (_drive.OrchAPISession.ApiVersion < _supportedApiVersionFrom)
        {
            return [];
        }

        // Root folder has a null Id; key it as 0 (same sentinel as IncrementalCachePerFolder).
        Int64 folderId = folder.Id ?? 0;

        _exceptions.ThrowCachedExceptionIfAny(folderId);

        if (_cache is null)
        {
            lock (_lock)
            {
                _cache ??= [];
            }
        }

        if (!_cache.TryGetValue(folderId, out var cachePerFolder))
        {
            try
            {
                cachePerFolder = _getter(folderId).ToList();

                if (_initializer is not null)
                {
                    string folderPath = folder.GetPSPath();
                    foreach (var entity in cachePerFolder)
                    {
                        _initializer(entity, folderPath);
                    }
                }
                // Publish after init.
                _cache[folderId] = cachePerFolder;
            }
            catch (HttpResponseException ex)
            {
                _exceptions.CacheException(folderId, ex);
                throw;
            }
        }
        return cachePerFolder;
    }

    public void ClearCache(Int64? folderId)
    {
        if (folderId is null) return;
        _cache?.TryRemove(folderId.Value, out _);
        _exceptions.ClearCache(folderId.Value);
    }

    public void ClearCache(Folder folder)
    {
        // Note that the root folder has a null Id!
        ClearCache(folder.Id);
    }

    public void ClearCache()
    {
        _cache = null;
        _exceptions.ClearCache();
    }
}

// Currently, only Int64 is supported as the index type. This type might benefit from parameterization.
// TIndexEntity: Entity containing the index
// getIndex(TIndexEntity): Func to get the Int64 index value from TIndexEntity
// TEntity: Entity to retrieve and cache
public class IndexedListCachePerFolder<TIndexEntity, TEntity> : IFolderCacheClearable where TIndexEntity : notnull
{
    private readonly object _lock = new();
    private readonly OrchDriveInfo _drive;

    // 1st key: folderId
    // 2nd key: TIndexEntity.Id
    private volatile ConcurrentDictionary<Int64, Dictionary<Int64, List<TEntity>>>? _cache = null;
    // The exception cache functionality is not great... It would be nice to be able to clear the cache per folder.
    private readonly ExceptionsCachePer<(Int64, Int64)> _exceptions = new();
    private readonly Func<Int64, TIndexEntity, IEnumerable<TEntity>> _fetchFunc; // Passes folderId and index, returns an enumeration of TEntity
    private readonly Func<TIndexEntity, Int64> _getIdFunc;
    private readonly Func<TIndexEntity, string> _getNameFunc;
    private readonly Action<TEntity, string, string, string>? _initializer; // folder path, name, folder path\name
    private readonly int? _supportedApiVersionFrom;

    public IndexedListCachePerFolder(
        OrchDriveInfo drive,
        Func<Int64, TIndexEntity, IEnumerable<TEntity>> fetchFunc,
        Func<TIndexEntity, Int64> getIdFunc,
        Func<TIndexEntity, string> getNameFunc,
        Action<TEntity, string, string, string>? initializer,
        int? supportedApiVersionFrom = null)
    {
        _drive = drive;
        _drive._allFolderCache.Add(this);
        _fetchFunc = fetchFunc;
        _getIdFunc = getIdFunc;
        _getNameFunc = getNameFunc;
        _initializer = initializer;
        _supportedApiVersionFrom = supportedApiVersionFrom;
    }

    public ICollection<TEntity> Get(Folder folder, TIndexEntity indexEntity)
    {
        if (_drive.OrchAPISession.ApiVersion < _supportedApiVersionFrom)
        {
            return [];
        }

        Int64 index = _getIdFunc(indexEntity);

        _exceptions.ThrowCachedExceptionIfAny((folder.Id!.Value, index));

        if (_cache is null)
        {
            lock (_lock)
            {
                if (_cache is null)
                {
                    _cache = [];
                }
            }
        }

        Int64 folderId = folder.Id!.Value;

        if (!_cache.TryGetValue(folderId, out var cachePerFolder))
        {
            cachePerFolder = [];
            _cache[folderId] = cachePerFolder;
        }

        if (!cachePerFolder.TryGetValue(index, out var entities))
        {
            try
            {
                entities = _fetchFunc(folderId, indexEntity).ToList();

                if (_initializer is not null)
                {
                    string folderPath = folder.GetPSPath();
                    string indexName = _getNameFunc(indexEntity);
                    string entityPath = System.IO.Path.Combine(folderPath, indexName);
                    foreach (var entity in entities)
                    {
                        _initializer(entity, folderPath, indexName, entityPath);
                    }
                }
                // Publish after init.
                cachePerFolder[index] = entities;
            }
            catch (HttpResponseException ex)
            {
                _exceptions.CacheException((folder.Id ?? 0, index), ex);
                throw;
            }
        }

        return entities;
    }

    public void ClearCache(Int64 folderId)
    {
        _cache?.TryRemove(folderId, out _);
        _exceptions.ClearCache(); // A bit of a lazy implementation, but should be functionally fine
    }

    public void ClearCache(Folder folder)
    {
        ClearCache(folder.Id!.Value);
    }

    public void ClearCache(Folder folder, Int64 id)
    {
        if (_cache?.TryGetValue(folder.Id.GetValueOrDefault(), out var cachePerFolder) ?? false)
        {
            cachePerFolder.Remove(id);
            _exceptions.ClearCache(); // A bit of a lazy implementation, but should be functionally fine
        }
    }

    public void ClearCache()
    {
        _cache = null;
        _exceptions.ClearCache();
    }
}

#region Incremental Cache Classes (for Job, AuditLog)

/// <summary>
/// Dictionary-type cache that accumulates filtering results per folder.
/// Designed for large datasets like Jobs. Does not fetch all items; appends API query results incrementally.
/// </summary>
/// <typeparam name="TKey">Entity key type (e.g., Int64)</typeparam>
/// <typeparam name="TEntity">Entity type (e.g., Job)</typeparam>
public class IncrementalCachePerFolder<TKey, TEntity> : IFolderCacheClearable
    where TKey : notnull
{
    private readonly object _lock = new();
    private readonly OrchDriveInfo _drive;

    // 1st key: folderId, 2nd key: entityKey
    private volatile ConcurrentDictionary<Int64, ConcurrentDictionary<TKey, TEntity>>? _cache = null;
    private readonly ExceptionsCachePer<Int64> _exceptions = new();

    // API call function
    private readonly Func<Int64, string?, ulong, ulong, string?, bool, IEnumerable<TEntity>> _fetchFunc;

    // Function to get the key from an entity
    private readonly Func<TEntity, TKey?> _getKeyFunc;

    // Initialization processing (e.g., setting Path)
    private readonly Action<TEntity, string>? _initializer;

    // Additional processing before adding to cache (e.g., merging with existing cache)
    private readonly Action<TEntity, TEntity?>? _mergeFunc;

    public IncrementalCachePerFolder(
        OrchDriveInfo drive,
        Func<Int64, string?, ulong, ulong, string?, bool, IEnumerable<TEntity>> fetchFunc,
        Func<TEntity, TKey?> getKeyFunc,
        Action<TEntity, string>? initializer = null,
        Action<TEntity, TEntity?>? mergeFunc = null)
    {
        _drive = drive;
        _drive._allFolderCache.Add(this);
        _fetchFunc = fetchFunc;
        _getKeyFunc = getKeyFunc;
        _initializer = initializer;
        _mergeFunc = mergeFunc;
    }

    /// <summary>
    /// Gets the cache Dictionary for a folder (for read-only access)
    /// </summary>
    public ConcurrentDictionary<TKey, TEntity>? GetCache(Folder folder)
    {
        if (_cache is null) return null;
        _cache.TryGetValue(folder.Id ?? 0, out var folderCache);
        return folderCache;
    }

    /// <summary>
    /// Calls the API to retrieve entities and adds them to the cache
    /// </summary>
    public ReadOnlyCollection<TEntity> Fetch(
        Folder folder,
        string? query = null,
        ulong skip = 0,
        ulong first = ulong.MaxValue,
        string? orderBy = null,
        bool orderAscending = false)
    {
        Int64 folderId = folder.Id ?? 0;

        _exceptions.ThrowCachedExceptionIfAny(folderId);

        if (_cache is null)
        {
            lock (_lock)
            {
                _cache ??= new();
            }
        }

        if (!_cache.TryGetValue(folderId, out var folderCache))
        {
            folderCache = new();
            _cache[folderId] = folderCache;
        }

        List<TEntity> fetched;
        try
        {
            fetched = _fetchFunc(folderId, query, skip, first, orderBy, orderAscending).ToList();
        }
        catch (HttpResponseException ex)
        {
            _exceptions.CacheException(folderId, ex);
            throw;
        }

        string folderPath = folder.GetPSPath();
        foreach (var entity in fetched)
        {
            _initializer?.Invoke(entity, folderPath);

            var key = _getKeyFunc(entity);
            if (key is not null)
            {
                // Merge processing (e.g., carrying over values from existing cache to new entities)
                if (_mergeFunc is not null && folderCache.TryGetValue(key, out var cached))
                {
                    _mergeFunc(entity, cached);
                }
                folderCache[key] = entity;
            }
        }

        return fetched.AsReadOnly();
    }

    public void ClearCache(Int64 folderId)
    {
        _cache?.TryRemove(folderId, out _);
        _exceptions.ClearCache(folderId);
    }

    public void ClearCache(Folder folder)
    {
        ClearCache(folder.Id ?? 0);
    }


    /// <summary>
    /// Adds a single entity to the cache (for calling after external API retrieval)
    /// </summary>
    public void AddToCache(Folder folder, TEntity entity)
    {
        Int64 folderId = folder.Id ?? 0;

        if (_cache is null)
        {
            lock (_lock)
            {
                _cache ??= new();
            }
        }

        if (!_cache.TryGetValue(folderId, out var folderCache))
        {
            folderCache = new();
            _cache[folderId] = folderCache;
        }

        var key = _getKeyFunc(entity);
        if (key is not null)
        {
            if (_mergeFunc is not null && folderCache.TryGetValue(key, out var cached))
            {
                _mergeFunc(entity, cached);
            }
            folderCache[key] = entity;
        }
    }

    public void ClearCache()
    {
        _cache = null;
        _exceptions.ClearCache();
    }
}

/// <summary>
/// Dictionary-type cache that accumulates filtering results across the entire tenant.
/// Designed for large datasets like AuditLog. Does not fetch all items; appends API query results incrementally.
/// </summary>
/// <typeparam name="TKey">Entity key type (e.g., Int64)</typeparam>
/// <typeparam name="TEntity">Entity type (e.g., AuditLog)</typeparam>
public class IncrementalCachePerTenant<TKey, TEntity> : ITenantCacheClearable
    where TKey : notnull
{
    private readonly object _lock = new();
    private readonly OrchDriveInfo _drive;

    private volatile ConcurrentDictionary<TKey, TEntity>? _cache = null;
    private readonly ExceptionCachePerTenant _exception = new();

    // API call function
    private readonly Func<string?, ulong, ulong, IEnumerable<TEntity>> _fetchFunc;

    // Function to get the key from an entity
    private readonly Func<TEntity, TKey?> _getKeyFunc;

    // Initialization processing (e.g., setting Path)
    private readonly Action<TEntity, string>? _initializer;

    // Additional processing before adding to cache (e.g., merging with existing cache)
    private readonly Action<TEntity, TEntity?>? _mergeFunc;

    public IncrementalCachePerTenant(
        OrchDriveInfo drive,
        Func<string?, ulong, ulong, IEnumerable<TEntity>> fetchFunc,
        Func<TEntity, TKey?> getKeyFunc,
        Action<TEntity, string>? initializer = null,
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
    /// Gets the cache Dictionary (for read-only access)
    /// </summary>
    public ConcurrentDictionary<TKey, TEntity>? GetCache() => _cache;

    /// <summary>
    /// Calls the API to retrieve entities and adds them to the cache
    /// </summary>
    public ReadOnlyCollection<TEntity> Fetch(
        string? query = null,
        ulong skip = 0,
        ulong first = ulong.MaxValue)
    {
        _exception.ThrowCachedExceptionIfAny();

        if (_cache is null)
        {
            lock (_lock)
            {
                _cache ??= new();
            }
        }

        List<TEntity> fetched;
        try
        {
            fetched = _fetchFunc(query, skip, first).ToList();
        }
        catch (HttpResponseException ex)
        {
            _exception.CacheException(ex);
            throw;
        }

        string drivePath = _drive.NameColonSeparator;
        foreach (var entity in fetched)
        {
            _initializer?.Invoke(entity, drivePath);

            var key = _getKeyFunc(entity);
            if (key is not null)
            {
                // Merge processing (e.g., carrying over values from existing cache to new entities)
                if (_mergeFunc is not null && _cache.TryGetValue(key, out var cached))
                {
                    _mergeFunc(entity, cached);
                }
                _cache[key] = entity;
            }
        }

        return fetched.AsReadOnly();
    }


    /// <summary>
    /// Adds a single entity to the cache (for calling after external API retrieval)
    /// </summary>
    public void AddToCache(TEntity entity)
    {
        if (_cache is null)
        {
            lock (_lock)
            {
                _cache ??= new();
            }
        }

        var key = _getKeyFunc(entity);
        if (key is not null)
        {
            if (_mergeFunc is not null && _cache.TryGetValue(key, out var cached))
            {
                _mergeFunc(entity, cached);
            }
            _cache[key] = entity;
        }
    }

    public void ClearCache()
    {
        _cache = null;
        _exception.ClearCache();
    }
}

#endregion



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
        catch (HttpResponseException ex)
        {
            _exceptions.CacheException(projectId, ex);
            throw;
        }

        foreach (var entity in fetched)
        {
            _initializer?.Invoke(entity, project);

            var key = _getKeyFunc(entity);
            if (key is not null)
            {
                // Merge processing (e.g., carrying over values from existing cache to new entities)
                if (_mergeFunc is not null && projectCache.TryGetValue(key, out var cached))
                {
                    _mergeFunc(entity, cached);
                }
                projectCache[key] = entity;
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
        if (key is not null)
        {
            if (_mergeFunc is not null && projectCache.TryGetValue(key, out var cached))
            {
                _mergeFunc(entity, cached);
            }
            projectCache[key] = entity;
        }
    }

    public void ClearCache()
    {
        _cache = null;
        _exceptions.ClearCache();
    }
}

#region Cache classes for TestManager

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
            catch (HttpResponseException ex)
            {
                _exceptions.CacheException(project.id ?? "", ex);
                throw;
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

    /// <summary>
    /// Retrieves cache for multiple projects in parallel.
    /// Projects with existing cache are resolved immediately without starting a thread.
    /// </summary>
    public static OrchThreadPoolImpl<(OrchTmDriveInfo drive, TmProject project), List<T>> RunForEach(
        IEnumerable<(OrchTmDriveInfo drive, TmProject project)> sources,
        Func<OrchTmDriveInfo, TmListCachePerTenant1<T>> getCacheFunc,
        int maxDegreeOfParallelism = 4)
    {
        var sourceList = sources.ToList();
        var threads = new OrchTask<(OrchTmDriveInfo drive, TmProject project), List<T>>[sourceList.Count];
        var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);
        var mainContext = SynchronizationContext.Current;

        for (int i = 0; i < threads.Length; i++)
        {
            threads[i] = new OrchTask<(OrchTmDriveInfo drive, TmProject project), List<T>>();
        }

        foreach (var (source, index) in sourceList.Select((source, index) => (source, index)))
        {
            var cache = getCacheFunc(source.drive);

            // If cache exists, set result immediately (no thread startup)
            if (cache._cache?.TryGetValue(source.project.id!, out var cached) ?? false)
            {
                threads[index].SetResult(source, cached);
                continue;
            }

            // If no cache, retrieve via thread
            Task.Run(async () =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var result = await Task.Run(() => cache.Get(source.project));

                    if (mainContext is not null)
                    {
                        mainContext.Post(_ =>
                        {
                            threads[index].SetResult(source, result);
                        }, null);
                    }
                    else
                    {
                        threads[index].SetResult(source, result);
                    }
                }
                catch (Exception ex)
                {
                    threads[index].SetException(source, source.project.GetPSPath(), source.project, ex);
                }
                finally
                {
                    semaphore.Release();
                }
            });
        }

        return OrchThreadPoolImpl<(OrchTmDriveInfo drive, TmProject project), List<T>>.CreateInstance(threads, semaphore);
    }
}

// getFunc: Func with no arguments that returns T
public class TmSingleCachePerTenant0<T> : ITenantCacheClearable where T : class
{
    private readonly OrchTmDriveInfo _drive;
    private volatile T? _cache = null;
    private readonly ExceptionCachePerTenant _exception = new();
    private readonly Func<T?> _getFunc;
    private readonly Action<T>? _initializer;
    private readonly int? _supportedApiVersionFrom;

    public TmSingleCachePerTenant0(
        OrchTmDriveInfo drive,
        Func<T?> getFunc,
        Action<T>? initializer = null,
        int? supportedApiVersionFrom = null)
    {
        _drive = drive;
        _getFunc = getFunc;
        _drive._allTenantCache.Add(this);
        _initializer = initializer;
        _supportedApiVersionFrom = supportedApiVersionFrom;
    }

    public T? Get()
    {
        if (_drive.OrchAPISession.ApiVersion < _supportedApiVersionFrom)
        {
            return default;
        }

        _exception.ThrowCachedExceptionIfAny();

        if (_cache is null)
        {
            try
            {
                var temp = _getFunc();

                if (_initializer is not null && temp is not null)
                {
                    _initializer(temp);
                }
                // Publish after init. Note: this class has no lock so two concurrent first
                // callers could each fetch and overwrite, but neither will publish a partially
                // initialized object. The duplicate fetch is a separate concern.
                _cache = temp;
            }
            catch (HttpResponseException ex)
            {
                _exception.CacheException(ex);
                throw;
            }
        }
        return _cache;
    }

    public void ClearCache()
    {
        _cache = null;
        _exception.ClearCache();
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
            catch (HttpResponseException ex)
            {
                _exceptions.CacheException(project.id ?? "", ex);
                throw;
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

#endregion
