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
    private readonly OrchDriveInfoBase _drive;
    // volatile + publish-after-init: ensures readers on weakly-ordered CPUs (e.g. ARM/Apple Silicon)
    // never observe a non-null _cache that points at a partially-initialized object.
    private volatile T? _cache = null;
    private readonly ExceptionCachePerTenant _exception = new();
    private readonly Func<T?> getter;
    private readonly Action<T>? initializer;

    public SingleCachePerTenant(OrchDriveInfoBase drive, Func<T?> getter, Action<T>? initializer = null)
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
            catch (Exception ex) when (ex is HttpResponseException or DeterministicApiException)
            {
                _exception.CacheException(ex);
                throw;
            }
        }
        return _cache;
    }

    /// <summary>
    /// Passive read: returns the cached value if a Get() call has previously
    /// succeeded, otherwise null. Does NOT trigger a fetch and does NOT throw
    /// even if an exception is cached. Use for best-effort reads (e.g. "show
    /// the username if we happen to know it") where surfacing an error is
    /// undesirable.
    /// </summary>
    public T? CachedValue => _cache;

    public void ClearCache()
    {
        _cache = null;
        _exception.ClearCache();
    }
}

public class ListCachePerTenant<T> : ITenantCacheClearable
{
    private readonly object _lock = new();
    private readonly OrchDriveInfoBase _drive;
    private volatile List<T>? _cache = null;
    private readonly ExceptionCachePerTenant _exception = new();
    private readonly Func<IEnumerable<T>> getter;
    private readonly Action<T>? initializer;

    public ListCachePerTenant(OrchDriveInfoBase drive, Func<IEnumerable<T>> getter, Action<T>? initializer = null)
    {
        _drive = drive;
        _drive._allTenantCache.Add(this);
        this.getter = getter;
        this.initializer = initializer;
    }

    // Peek at the cached list without triggering a fetch. Used by providers
    // (e.g., OrchDuProvider / OrchTmProvider) to enumerate children only when
    // the cache is already populated, avoiding implicit API calls during
    // wildcard expansion.
    public List<T>? CachedValue => _cache;

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
            catch (Exception ex) when (ex is HttpResponseException or DeterministicApiException)
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

/// <summary>
/// Tenant-scoped cache of the drive's folder catalog. Unlike the generic entity
/// caches, folders are the navigation backbone — a tree with dual identity (numeric
/// <c>Id</c> and path <c>FullyQualifiedName</c>) and two sort projections — so this
/// class owns ONLY storage, atomic publish, incremental append and tenant-scoped
/// invalidation. The fetch/enrichment/ordering and all tree navigation
/// (GetFolder / HasSubfolders / EnumFolders) stay on <c>OrchDriveInfo</c> and just
/// read these lists.
///
/// Self-registers into <c>_allTenantCache</c>, so <c>ClearTenantCache()</c> /
/// <c>Clear-OrchCache</c> flush it automatically — no hand-maintained null-out
/// (the bug class the registry was introduced to kill).
///
/// Deliberately does NOT cache exceptions (unlike the generic bases): folders are
/// the backbone, so a transient fetch failure must not brick all navigation until
/// the next Clear-OrchCache. A failed build throws and the next Get rebuilds.
/// </summary>
public class FolderCache : ITenantCacheClearable
{
    // Holds both projections so they publish together in a single reference write —
    // a reader never observes a main view without its matching enum view.
    private sealed class Views
    {
        public Views(List<Folder> main, List<Folder> enumView) { Main = main; EnumView = enumView; }
        public List<Folder> Main { get; }
        public List<Folder> EnumView { get; }
    }

    private readonly object _lock = new();
    // volatile + publish-after-build: readers on weakly-ordered CPUs never observe a
    // non-null cache that points at a partially-built pair.
    private volatile Views? _cache;
    private readonly Func<(List<Folder> main, List<Folder> enumView)> _builder;

    public FolderCache(OrchDriveInfoBase drive, Func<(List<Folder> main, List<Folder> enumView)> builder)
    {
        drive._allTenantCache.Add(this);
        _builder = builder;
    }

    private Views GetOrBuild()
    {
        var snapshot = _cache;
        if (snapshot is not null) return snapshot;

        lock (_lock)
        {
            if (_cache is not null) return _cache;
            var (main, enumView) = _builder();
            var views = new Views(main, enumView);
            _cache = views; // single atomic publish of both projections
            return views;
        }
    }

    // Main view (web-UI sort) backing GetFolders(); builds on first access.
    public List<Folder> GetMain() => GetOrBuild().Main;

    // Enum view backing EnumFolders(); builds on first access.
    public List<Folder> GetEnumView() => GetOrBuild().EnumView;

    // Passive peeks: return the cached projection without triggering a fetch (null
    // until built). Used by paths that must not hit the API before authentication.
    public List<Folder>? CachedMain => _cache?.Main;
    public List<Folder>? CachedEnumView => _cache?.EnumView;

    // Append a freshly-created folder to both projections under the build lock.
    // No-op when the cache is null (a concurrent ClearCache happened; the next
    // GetMain()/GetEnumView() rebuilds from the API and picks the folder up). Sort
    // order is not restored — call sites that need it clear the cache afterwards.
    public void Append(Folder folder)
    {
        lock (_lock)
        {
            var snapshot = _cache;
            snapshot?.Main.Add(folder);
            snapshot?.EnumView.Add(folder);
        }
    }

    // Remove a deleted folder — and any descendants, since a server-side folder delete
    // cascades — from both projections under the lock, matched by FullyQualifiedName.
    // No-op when the cache is null. Targeted: avoids a full clear + refetch on delete.
    public void Remove(Folder folder)
    {
        var self = folder?.FullyQualifiedName;
        if (string.IsNullOrEmpty(self)) return;
        string prefix = self + "/";
        lock (_lock)
        {
            var snapshot = _cache;
            if (snapshot is null) return;
            bool Match(Folder f) =>
                string.Equals(f.FullyQualifiedName, self, StringComparison.OrdinalIgnoreCase)
                || (f.FullyQualifiedName?.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ?? false);
            snapshot.Main.RemoveAll(Match);
            snapshot.EnumView.RemoveAll(Match);
        }
    }

    public void ClearCache() => _cache = null;

    // Test-only: publish a folder catalog directly, bypassing the API builder, so provider
    // navigation (wildcard globbing, HasChildItems, Get-Item / Split-Path / PSParentPath) can be
    // exercised in a runspace without a live tenant. See OrchDriveInfo.SeedFolderCatalogForTest.
    internal void SeedForTest(List<Folder> main, List<Folder> enumView) => _cache = new Views(main, enumView);
}

// Organization entities keyed by partitionGlobalId.
// This represents the cache of unique entities across all organizations.
public class ListCachePerOrganization<T> : ITenantCacheClearable
{
    private readonly OrchDriveInfoBase _drive;
    private static readonly ConcurrentDictionary<string, List<T>> _cache = [];
    private static readonly ExceptionsCachePer<string> _exception = new(); // Holds per-org exceptions
    // Per-partition lock for the list fetch path. Same partition → same lock
    // instance → fetch serialized; different partitions → different locks →
    // parallel. Replaces the prior instance-_lock-guards-a-static-cache anti-
    // pattern under which two drives in the same org would each enter their
    // own critical section and double-fetch.
    private static readonly ConcurrentDictionary<string, object> _partitionLocks = new();
    // input: partitionGlobalId
    private readonly Func<string, IEnumerable<T>> _getter;
    private readonly Action<T>? _initializer;

    // The following is also needed for entities that support -ExpandDetail
    private readonly Func<T, string?>? _getterId;
    // Detailed cache of organization entities keyed by (partitionGlobalId, id).
    // Eagerly initialized — the empty ConcurrentDictionary cost is trivial and
    // the null state was only there to avoid that cost.
    private static readonly ConcurrentDictionary<(string partitionGlobalId, string id), T?> _cacheDetailed = new();
    // Separate per-partition lock dict for the detail path so a list fetch and
    // a detail fetch on the same org can proceed concurrently.
    private static readonly ConcurrentDictionary<string, object> _detailedPartitionLocks = new();
    private readonly Func<string, string, T?>? _getterDetailed;
    // Exception cache for detailed organization entity retrieval keyed by (partitionGlobalId, id)
    private static readonly ExceptionsCachePer<(string partitionGlobalId, string id)> _exceptionDetailed = new(); // Holds per (org, id) exceptions

    public ListCachePerOrganization(
        OrchDriveInfoBase drive,
        Func<string, IEnumerable<T>> getter,
        Action<T>? initializer = null,
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
        // Data-fetch path: force the partition lookup. The PartitionGlobalId
        // property is passive (returns null until populated) so a Get() at
        // session start would silently yield nothing instead of authenticating.
        var partitionGlobalId = _drive.GetPartitionGlobalId();
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
                        // Run the initializer once, before publishing — it now
                        // only handles entity-only derived state (drive-local
                        // Path is set by the cmdlet on a per-emit ShallowClone copy).
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
            // Prefer the detailed cache entry when one exists (initializer was
            // also run inside Get(id)'s fetch path). Detail lookup requires a
            // getterId — entities like PmLicenses that don't support detail
            // fetch leave it null and just yield the list entry directly.
            if (_getterId is not null && !_cacheDetailed.IsEmpty)
            {
                var id = _getterId(t);
                if (id is not null
                    && _cacheDetailed.TryGetValue((partitionGlobalId, id), out var detailedEntity)
                    && detailedEntity is not null)
                {
                    yield return detailedEntity;
                    continue;
                }
            }
            yield return t;
        }
    }

    public T? Get(string? id)
    {
        if (_getterDetailed is null) throw new NotSupportedException();

        if (string.IsNullOrEmpty(id)) return default;

        // Data-fetch path: force the partition lookup (the property is passive).
        var partitionGlobalId = _drive.GetPartitionGlobalId()!;

        _exceptionDetailed.ThrowCachedExceptionIfAny((partitionGlobalId, id));

        if (!_cacheDetailed.TryGetValue((partitionGlobalId, id), out var cachePerOrgDetailed))
        {
            var partitionLock = _detailedPartitionLocks.GetOrAdd(partitionGlobalId, _ => new object());
            lock (partitionLock)
            {
                if (!_cacheDetailed.TryGetValue((partitionGlobalId, id), out cachePerOrgDetailed))
                {
                    try
                    {
                        cachePerOrgDetailed = _getterDetailed(partitionGlobalId, id);
                        // Run initializer once before publishing — entity-only
                        // derived state. Drive-local Path is set by the cmdlet
                        // on a per-emit ShallowClone copy.
                        if (_initializer is not null && cachePerOrgDetailed is not null)
                        {
                            _initializer(cachePerOrgDetailed);
                        }
                        _cacheDetailed[(partitionGlobalId, id)] = cachePerOrgDetailed;
                    }
                    catch (Exception ex) when (ex is HttpResponseException or DeterministicApiException)
                    {
                        _exceptionDetailed.CacheException((partitionGlobalId, id), ex);
                        throw;
                    }
                }
            }
        }
        // No cache-hit re-init.

        return cachePerOrgDetailed;
    }

    public void Set(T t)
    {
        // Data-fetch path: force the partition lookup (the property is passive).
        var partitionGlobalId = _drive.GetPartitionGlobalId();
        var id = _getterId?.Invoke(t);
        if (string.IsNullOrEmpty(partitionGlobalId) || string.IsNullOrEmpty(id)) return;

        // Update _cache (do nothing if the list does not exist)
        if (_cache.TryGetValue(partitionGlobalId, out var list))
        {
            // No lock: this in-place list mutation is reached only from the cmdlet's
            // main pipeline thread. Verified invariant — the sole caller of Set() is
            // OrchDriveInfo.CreatePmGroup, whose only caller is New-PmGroup, which
            // runs a sequential foreach (no RunForEach / parallel fan-out). The Get()
            // / enumeration paths read these lists without locking too, so they rely
            // on the same single-thread assumption.
            // CONTRACT: any future caller that invokes Set() (or a Create* that calls
            // it) from a parallel context (RunForEach lambda, Task, etc.) MUST add
            // read+write synchronization here AND on the enumeration paths first —
            // List<T>.Add / indexer are not atomic and tear under concurrent access.
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
        if (string.IsNullOrEmpty(_drive.PartitionGlobalId)) return;

        if (!string.IsNullOrEmpty(id))
        {
            _cacheDetailed?.TryRemove((_drive.PartitionGlobalId, id), out var _);
        }
        _cache.TryRemove(_drive.PartitionGlobalId, out var _);
        _exception.ClearCache(_drive.PartitionGlobalId);
    }

    public void ClearCache()
    {
        if (string.IsNullOrEmpty(_drive.PartitionGlobalId)) return;

        if (_cacheDetailed != null && !_cacheDetailed.IsEmpty)
        {
            foreach (var pair in _cacheDetailed)
            {
                var key = pair.Key;
                if (key.partitionGlobalId == _drive.PartitionGlobalId)
                {
                    _cacheDetailed.TryRemove(key, out _);
                }
            }
        }

        _cache.TryRemove(_drive.PartitionGlobalId, out _);
        _exception.ClearCache(_drive.PartitionGlobalId);
    }

}

// Single organization entity keyed by partitionGlobalId.
// This represents the cache of a single entity shared across the entire organization.
public class SingleCachePerOrganization<T> : ITenantCacheClearable where T : class
{
    private readonly OrchDriveInfoBase _drive;

    // Cache per organization (static = shared across all drive instances)
    private static readonly ConcurrentDictionary<string, T> _cache = [];
    private static readonly ExceptionsCachePer<string> _exception = new();
    // Per-partition lock: same partition → serialized fetch (no duplicate API
    // calls); different partitions → parallel.
    private static readonly ConcurrentDictionary<string, object> _partitionLocks = new();

    // getter: takes partitionGlobalId as argument and returns T
    private readonly Func<string, T?> _getter;
    private readonly Action<T>? _initializer;

    public SingleCachePerOrganization(
        OrchDriveInfoBase drive,
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
        // Data-fetch path: force the partition lookup (the property is passive).
        var partitionGlobalId = _drive.GetPartitionGlobalId();
        if (string.IsNullOrEmpty(partitionGlobalId)) return null;

        _exception.ThrowCachedExceptionIfAny(partitionGlobalId);

        if (!_cache.TryGetValue(partitionGlobalId, out var entity))
        {
            var partitionLock = _partitionLocks.GetOrAdd(partitionGlobalId, _ => new object());
            lock (partitionLock)
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
                    catch (Exception ex) when (ex is HttpResponseException or DeterministicApiException)
                    {
                        _exception.CacheException(partitionGlobalId, ex);
                        throw;
                    }
                }
            }
        }
        // No cached-hit re-initializer call here. The previous code re-ran
        // _initializer on every cache hit so each drive could write its own
        // Path into the shared static entity — a race-prone hack now obsolete:
        // Path is never set on this shared singleton; the cmdlet sets it on a
        // per-emit ShallowClone copy at WriteObject time.

        return entity;
    }

    public void ClearCache()
    {
        var partitionGlobalId = _drive.PartitionGlobalId;
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
    private readonly OrchDriveInfoBase _drive;

    // key: TIndexEntity.Id
    private volatile ConcurrentDictionary<Int64, TEntity?>? _cache = null;
    private readonly ExceptionsCachePer<Int64> _exceptions = new();
    private readonly Func<Int64, TEntity?> _fetchFunc; // Passes an index and returns TEntity
    private readonly Func<TIndexEntity, Int64> _getIdFunc;
    private readonly Func<TIndexEntity, string> _getNameFunc;
    private readonly Action<TEntity, string>? _initializer; // Name
    private readonly int? _supportedApiVersionFrom;

    public IndexedCachePerTenant(
        OrchDriveInfoBase drive,
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
            // Double-check inside the lock so concurrent first-touch on the
            // same index doesn't issue duplicate API calls — symmetric with
            // SingleCachePerTenant / ListCachePerTenant.
            lock (_lock)
            {
                if (!_cache.TryGetValue(index, out entity))
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
                    catch (Exception ex) when (ex is HttpResponseException or DeterministicApiException)
                    {
                        _exceptions.CacheException(index, ex);
                        throw;
                    }
                }
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
    private readonly OrchDriveInfoBase _drive;

    private volatile ConcurrentDictionary<TKey, List<TEntity>>? _cache = null;
    private readonly ExceptionsCachePer<TKey> _exceptions = new();
    private readonly Func<TKey, IEnumerable<TEntity>> _fetchFunc;
    private readonly Action<TEntity, TKey>? _initializer;
    private readonly IEqualityComparer<TKey>? _keyComparer;
    private readonly int? _supportedApiVersionFrom;

    public KeyedListCachePerTenant(
        OrchDriveInfoBase drive,
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
            // Double-check inside the lock — see IndexedCachePerTenant.Get for
            // the symmetry rationale.
            lock (_lock)
            {
                if (!_cache.TryGetValue(key, out list))
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
                    catch (Exception ex) when (ex is HttpResponseException or DeterministicApiException)
                    {
                        _exceptions.CacheException(key, ex);
                        throw;
                    }
                }
            }
        }

        return list.AsReadOnly();
    }

    public void ClearCache(TKey key)
    {
        _cache?.TryRemove(key, out _);
        _exceptions.ClearCache(key);
    }

    /// <summary>
    /// Drop every entry whose key matches the predicate. Useful for composite-
    /// key caches when an external mutation invalidates only a slice of the
    /// cache (mirrors KeyedSingleCachePerTenant.ClearCache(Func) behavior).
    /// </summary>
    public void ClearCache(Func<TKey, bool> predicate)
    {
        if (_cache is null) return;
        foreach (var key in _cache.Keys.Where(predicate).ToList())
        {
            _cache.TryRemove(key, out _);
            _exceptions.ClearCache(key);
        }
    }

    public void ClearCache()
    {
        _cache = null;
        _exceptions.ClearCache();
    }
}

/// <summary>
/// Tenant-scoped cache of a single <typeparamref name="TEntity"/> per
/// <typeparamref name="TKey"/>. Same lifecycle and exception-caching contract
/// as <see cref="KeyedListCachePerTenant{TKey,TEntity}"/>, but each key maps
/// to one entity instead of a list. <typeparamref name="TKey"/> can be a
/// tuple (e.g. <c>(folderId, assetId)</c>) for composite-key caches such as
/// the asset/queue/bucket "folders accessible from this folder" links.
///
/// The <see cref="ClearCache(Func{TKey, bool})"/> overload exists for
/// composite keys: callers can drop entries matching a partial key (for
/// example, all entries for a single asset across all folders) without
/// flushing the entire cache.
/// </summary>
public class KeyedSingleCachePerTenant<TKey, TEntity> : ITenantCacheClearable
    where TKey : notnull, IEquatable<TKey>
{
    private readonly object _lock = new();
    private readonly OrchDriveInfoBase _drive;

    private volatile ConcurrentDictionary<TKey, TEntity?>? _cache = null;
    private readonly ExceptionsCachePer<TKey> _exceptions = new();
    private readonly Func<TKey, TEntity?> _fetchFunc;
    private readonly Action<TEntity, TKey>? _initializer;
    private readonly IEqualityComparer<TKey>? _keyComparer;
    private readonly int? _supportedApiVersionFrom;

    public KeyedSingleCachePerTenant(
        OrchDriveInfoBase drive,
        Func<TKey, TEntity?> fetchFunc,
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

    public TEntity? Get(TKey key)
    {
        if (_drive.OrchAPISession.ApiVersion < _supportedApiVersionFrom)
        {
            return default;
        }

        _exceptions.ThrowCachedExceptionIfAny(key);

        if (_cache is null)
        {
            lock (_lock)
            {
                _cache ??= _keyComparer is null
                    ? new ConcurrentDictionary<TKey, TEntity?>()
                    : new ConcurrentDictionary<TKey, TEntity?>(_keyComparer);
            }
        }

        if (!_cache.TryGetValue(key, out var entity))
        {
            // Double-check inside the lock — see IndexedCachePerTenant.Get for
            // the symmetry rationale.
            lock (_lock)
            {
                if (!_cache.TryGetValue(key, out entity))
                {
                    try
                    {
                        entity = _fetchFunc(key);
                        if (entity is not null)
                        {
                            _initializer?.Invoke(entity, key);
                        }
                        // Publish only after init so concurrent readers never observe
                        // a partially-initialized entity.
                        _cache[key] = entity;
                    }
                    catch (Exception ex) when (ex is HttpResponseException or DeterministicApiException)
                    {
                        _exceptions.CacheException(key, ex);
                        throw;
                    }
                }
            }
        }

        return entity;
    }

    public void ClearCache(TKey key)
    {
        _cache?.TryRemove(key, out _);
        _exceptions.ClearCache(key);
    }

    /// <summary>
    /// Drop all entries (and their exception cache entries) whose key matches
    /// the predicate. Useful for composite-key caches when an external mutation
    /// invalidates only a slice of the cache (e.g. one asset's link set across
    /// all folders).
    /// </summary>
    public void ClearCache(Func<TKey, bool> predicate)
    {
        if (_cache is null) return;
        // Snapshot keys before mutation: ConcurrentDictionary.Keys is a live view.
        foreach (var key in _cache.Keys.Where(predicate).ToList())
        {
            _cache.TryRemove(key, out _);
            _exceptions.ClearCache(key);
        }
    }

    public void ClearCache()
    {
        _cache = null;
        _exceptions.ClearCache();
    }
}

/// <summary>
/// Organization-scoped sibling of <see cref="KeyedSingleCachePerTenant{TKey, TEntity}"/>.
/// Caches a single <typeparamref name="TEntity"/> per
/// <c>(partitionGlobalId, TKey)</c>. Storage is <c>static</c>, so all
/// <see cref="OrchDriveInfo"/> instances pointing to the same organization
/// share one cache (no duplicate fetch / storage when multiple drives map to
/// the same org).
///
/// Used by Platform Management (PM) entities that are org-scoped at the API
/// layer (the path includes <c>partitionGlobalId</c>) — e.g. PmAvailableUserBundles,
/// SearchPmDirectoryCache, PmBulkResolveByName.
///
/// Concurrency: PM API calls are serialized via a single static lock per
/// closed generic to avoid duplicate in-flight requests across drives in the
/// same org. Cache reads (hits) are lock-free via ConcurrentDictionary.
/// <see cref="ClearCache()"/> clears only the current drive's org entry, so
/// <c>Clear-OrchCache -Path X</c> on a drive in org A leaves org B's cache
/// intact (but does affect other drives in org A — which is correct, since
/// they share the storage).
///
/// Bails out early (returns default / no-op) when the drive's
/// <c>partitionGlobalId</c> is null or empty.
/// </summary>
public class KeyedSingleCachePerOrganization<TKey, TEntity> : ITenantCacheClearable
    where TKey : notnull, IEquatable<TKey>
{
    // Storage shared across all drives in the same closed generic instantiation.
    private static readonly ConcurrentDictionary<(string partitionGlobalId, TKey key), TEntity?> _cache = new();
    private static readonly ExceptionsCachePer<(string partitionGlobalId, TKey key)> _exceptions = new();
    // Per-partition lock: same partition → serialized fetch; different
    // partitions → parallel. Previously a single global static lock serialized
    // every fetch across all orgs, which was correct but unnecessarily coarse.
    private static readonly ConcurrentDictionary<string, object> _partitionLocks = new();

    private readonly OrchDriveInfoBase _drive;
    private readonly Func<string, TKey, TEntity?> _fetchFunc;
    private readonly Action<TEntity, TKey>? _initializer;
    private readonly int? _supportedApiVersionFrom;

    public KeyedSingleCachePerOrganization(
        OrchDriveInfoBase drive,
        Func<string, TKey, TEntity?> fetchFunc,
        Action<TEntity, TKey>? initializer = null,
        IEqualityComparer<TKey>? keyComparer = null,
        int? supportedApiVersionFrom = null)
    {
        _drive = drive;
        _drive._allTenantCache.Add(this);
        _fetchFunc = fetchFunc;
        _initializer = initializer;
        // keyComparer parameter accepted for signature parity with the per-tenant
        // sibling; since the cache is keyed by tuple (string, TKey), the equality
        // comparer is implicitly the tuple's. Reserved for future use.
        _ = keyComparer;
        _supportedApiVersionFrom = supportedApiVersionFrom;
    }

    public TEntity? Get(TKey key)
    {
        if (_drive.OrchAPISession.ApiVersion < _supportedApiVersionFrom) return default;

        // Data-fetch path: force the partition lookup (the property is passive).
        var partitionGlobalId = _drive.GetPartitionGlobalId();
        if (string.IsNullOrEmpty(partitionGlobalId)) return default;

        var compositeKey = (partitionGlobalId, key);
        _exceptions.ThrowCachedExceptionIfAny(compositeKey);

        if (_cache.TryGetValue(compositeKey, out var cached)) return cached;

        // Serialize fetches within the same partition only — different orgs
        // are independent and can fetch in parallel.
        var partitionLock = _partitionLocks.GetOrAdd(partitionGlobalId, _ => new object());
        lock (partitionLock)
        {
            // Re-check after acquiring the lock (another thread may have populated).
            if (_cache.TryGetValue(compositeKey, out cached)) return cached;

            try
            {
                var entity = _fetchFunc(partitionGlobalId, key);
                if (entity is not null) _initializer?.Invoke(entity, key);
                _cache[compositeKey] = entity;
                return entity;
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
        // Read the cached partition id directly instead of calling
        // GetPartitionGlobalId(), which falls back to an authenticated API
        // call when the JWT lacks prt_id. If the drive has never authed
        // (_partitionGlobalId is null), it can't have contributed entries
        // to this static cache, so a no-op is correct — and avoids
        // triggering PKCE for every unauthed drive on Clear-OrchCache -AllDrives.
        var partitionGlobalId = _drive.PartitionGlobalId;
        if (string.IsNullOrEmpty(partitionGlobalId)) return;

        var compositeKey = (partitionGlobalId, key);
        _cache.TryRemove(compositeKey, out _);
        _exceptions.ClearCache(compositeKey);
    }

    public void ClearCache()
    {
        var partitionGlobalId = _drive.PartitionGlobalId;
        if (string.IsNullOrEmpty(partitionGlobalId)) return;

        // Drop every entry whose first tuple element is this drive's partition.
        // Other orgs' entries (from drives mapped elsewhere) stay intact.
        foreach (var k in _cache.Keys.Where(k => k.partitionGlobalId == partitionGlobalId).ToList())
        {
            _cache.TryRemove(k, out _);
        }
        _exceptions.ClearCache(k => k.partitionGlobalId == partitionGlobalId);
    }
}

/// <summary>
/// Organization-scoped sibling of <see cref="KeyedListCachePerTenant{TKey, TEntity}"/>.
/// Caches a <see cref="List{TEntity}"/> per <c>(partitionGlobalId, TKey)</c>.
/// Same storage / locking / clearing pattern as
/// <see cref="KeyedSingleCachePerOrganization{TKey, TEntity}"/>; see that class
/// for the design rationale.
/// </summary>
public class KeyedListCachePerOrganization<TKey, TEntity> : ITenantCacheClearable
    where TKey : notnull, IEquatable<TKey>
{
    private static readonly ConcurrentDictionary<(string partitionGlobalId, TKey key), List<TEntity>> _cache = new();
    private static readonly ExceptionsCachePer<(string partitionGlobalId, TKey key)> _exceptions = new();
    // Per-partition lock — see KeyedSingleCachePerOrganization.
    private static readonly ConcurrentDictionary<string, object> _partitionLocks = new();

    private readonly OrchDriveInfoBase _drive;
    private readonly Func<string, TKey, IEnumerable<TEntity>> _fetchFunc;
    private readonly Action<TEntity, TKey>? _initializer;
    private readonly int? _supportedApiVersionFrom;

    public KeyedListCachePerOrganization(
        OrchDriveInfoBase drive,
        Func<string, TKey, IEnumerable<TEntity>> fetchFunc,
        Action<TEntity, TKey>? initializer = null,
        IEqualityComparer<TKey>? keyComparer = null,
        int? supportedApiVersionFrom = null)
    {
        _drive = drive;
        _drive._allTenantCache.Add(this);
        _fetchFunc = fetchFunc;
        _initializer = initializer;
        _ = keyComparer;  // see KeyedSingleCachePerOrganization comment
        _supportedApiVersionFrom = supportedApiVersionFrom;
    }

    public ReadOnlyCollection<TEntity> Get(TKey key)
    {
        if (_drive.OrchAPISession.ApiVersion < _supportedApiVersionFrom)
        {
            return new List<TEntity>().AsReadOnly();
        }

        // Data-fetch path: force the partition lookup (the property is passive).
        var partitionGlobalId = _drive.GetPartitionGlobalId();
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
        // Read the cached partition id directly; see KeyedSingleCachePerOrganization
        // for the rationale (don't trigger auth on Clear-OrchCache).
        var partitionGlobalId = _drive.PartitionGlobalId;
        if (string.IsNullOrEmpty(partitionGlobalId)) return;

        var compositeKey = (partitionGlobalId, key);
        _cache.TryRemove(compositeKey, out _);
        _exceptions.ClearCache(compositeKey);
    }

    public void ClearCache()
    {
        var partitionGlobalId = _drive.PartitionGlobalId;
        if (string.IsNullOrEmpty(partitionGlobalId)) return;

        foreach (var k in _cache.Keys.Where(k => k.partitionGlobalId == partitionGlobalId).ToList())
        {
            _cache.TryRemove(k, out _);
        }
        _exceptions.ClearCache(k => k.partitionGlobalId == partitionGlobalId);
    }
}

/// <summary>
/// Folder-scoped sibling of <see cref="KeyedSingleCachePerTenant{TKey, TEntity}"/>:
/// caches a single <typeparamref name="TEntity"/> per <typeparamref name="TKey"/>
/// per folder. Used by per-folder per-id detail caches (Releases by id within a
/// folder, ProcessSchedules by id within a folder, etc.).
///
/// Per-key HttpResponseException caching uses a <c>(folderId, key)</c> tuple so
/// failures don't leak across folders.
///
/// <see cref="IFolderCacheClearable"/> registration via
/// <c>_drive._allFolderCache.Add(this)</c> means a folder removal flushes the
/// folder's slice automatically; callers don't need to remember per-cache cleanup.
/// </summary>
public class KeyedSingleCachePerFolder<TKey, TEntity> : IFolderCacheClearable
    where TKey : notnull, IEquatable<TKey>
{
    private readonly object _lock = new();
    private readonly OrchDriveInfoBase _drive;

    // Outer dict: folderId -> inner dict (key -> entity).
    private volatile ConcurrentDictionary<Int64, ConcurrentDictionary<TKey, TEntity?>>? _cache = null;
    private readonly ExceptionsCachePer<(Int64 folderId, TKey key)> _exceptions = new();
    private readonly Func<Int64, TKey, TEntity?> _fetchFunc;
    private readonly Action<TEntity, string, TKey>? _initializer;
    private readonly IEqualityComparer<TKey>? _keyComparer;
    private readonly int? _supportedApiVersionFrom;

    public KeyedSingleCachePerFolder(
        OrchDriveInfoBase drive,
        Func<Int64, TKey, TEntity?> fetchFunc,
        Action<TEntity, string, TKey>? initializer = null,
        IEqualityComparer<TKey>? keyComparer = null,
        int? supportedApiVersionFrom = null)
    {
        _drive = drive;
        _drive._allFolderCache.Add(this);
        _fetchFunc = fetchFunc;
        _initializer = initializer;
        _keyComparer = keyComparer;
        _supportedApiVersionFrom = supportedApiVersionFrom;
    }

    public TEntity? Get(Folder folder, TKey key)
    {
        if (folder?.Id is null || _drive.OrchAPISession.ApiVersion < _supportedApiVersionFrom)
        {
            return default;
        }

        Int64 folderId = folder.Id.Value;
        _exceptions.ThrowCachedExceptionIfAny((folderId, key));

        if (_cache is null)
        {
            lock (_lock)
            {
                _cache ??= [];
            }
        }

        if (!_cache.TryGetValue(folderId, out var inner))
        {
            inner = _keyComparer is null
                ? new ConcurrentDictionary<TKey, TEntity?>()
                : new ConcurrentDictionary<TKey, TEntity?>(_keyComparer);
            inner = _cache.GetOrAdd(folderId, inner);
        }

        if (!inner.TryGetValue(key, out var entity))
        {
            try
            {
                entity = _fetchFunc(folderId, key);
                if (entity is not null && _initializer is not null)
                {
                    string folderPath = folder.GetPSPath();
                    _initializer(entity, folderPath, key);
                }
                // Publish only after init so concurrent readers never observe a
                // partially-initialized entity.
                inner[key] = entity;
            }
            catch (Exception ex) when (ex is HttpResponseException or DeterministicApiException)
            {
                _exceptions.CacheException((folderId, key), ex);
                throw;
            }
        }

        return entity;
    }

    public void ClearCache(Folder folder, TKey key)
    {
        var folderId = folder.Id ?? 0;
        if (_cache is not null && _cache.TryGetValue(folderId, out var inner))
        {
            inner.TryRemove(key, out _);
        }
        _exceptions.ClearCache((folderId, key));
    }

    public void ClearCache(Folder folder)
    {
        var folderId = folder.Id ?? 0;
        _cache?.TryRemove(folderId, out _);
        // Drop all per-key exception entries belonging to this folder.
        _exceptions.ClearCache(k => k.folderId == folderId);
    }

    public void ClearCache()
    {
        _cache = null;
        _exceptions.ClearCache();
    }
}

/// <summary>
/// Folder-scoped sibling of <see cref="KeyedListCachePerTenant{TKey, TEntity}"/>:
/// caches a <see cref="List{TEntity}"/> per <typeparamref name="TKey"/> per
/// folder. Outer dict is keyed by folderId (matching
/// <see cref="KeyedSingleCachePerFolder{TKey, TEntity}"/>); inner dict maps
/// <typeparamref name="TKey"/> to the list (matching the list-shape of
/// <see cref="KeyedListCachePerTenant{TKey, TEntity}"/>). Used when one fetch
/// returns a collection of children keyed within a parent — e.g. assertions
/// belonging to a TestCaseExecution within a folder.
/// </summary>
public class KeyedListCachePerFolder<TKey, TEntity> : IFolderCacheClearable
    where TKey : notnull, IEquatable<TKey>
{
    private readonly object _lock = new();
    private readonly OrchDriveInfoBase _drive;

    // Outer dict: folderId -> inner dict (key -> list of entities).
    private volatile ConcurrentDictionary<Int64, ConcurrentDictionary<TKey, List<TEntity>>>? _cache = null;
    private readonly ExceptionsCachePer<(Int64 folderId, TKey key)> _exceptions = new();
    private readonly Func<Int64, TKey, IEnumerable<TEntity>> _fetchFunc;
    private readonly Action<TEntity, string, TKey>? _initializer;
    private readonly IEqualityComparer<TKey>? _keyComparer;
    private readonly int? _supportedApiVersionFrom;

    public KeyedListCachePerFolder(
        OrchDriveInfoBase drive,
        Func<Int64, TKey, IEnumerable<TEntity>> fetchFunc,
        Action<TEntity, string, TKey>? initializer = null,
        IEqualityComparer<TKey>? keyComparer = null,
        int? supportedApiVersionFrom = null)
    {
        _drive = drive;
        _drive._allFolderCache.Add(this);
        _fetchFunc = fetchFunc;
        _initializer = initializer;
        _keyComparer = keyComparer;
        _supportedApiVersionFrom = supportedApiVersionFrom;
    }

    public ReadOnlyCollection<TEntity> Get(Folder folder, TKey key)
    {
        if (folder?.Id is null || _drive.OrchAPISession.ApiVersion < _supportedApiVersionFrom)
        {
            return new List<TEntity>().AsReadOnly();
        }

        Int64 folderId = folder.Id.Value;
        _exceptions.ThrowCachedExceptionIfAny((folderId, key));

        if (_cache is null)
        {
            lock (_lock)
            {
                _cache ??= [];
            }
        }

        if (!_cache.TryGetValue(folderId, out var inner))
        {
            inner = _keyComparer is null
                ? new ConcurrentDictionary<TKey, List<TEntity>>()
                : new ConcurrentDictionary<TKey, List<TEntity>>(_keyComparer);
            inner = _cache.GetOrAdd(folderId, inner);
        }

        if (!inner.TryGetValue(key, out var list))
        {
            try
            {
                list = _fetchFunc(folderId, key).ToList();
                if (_initializer is not null)
                {
                    string folderPath = folder.GetPSPath();
                    foreach (var entity in list)
                    {
                        _initializer(entity, folderPath, key);
                    }
                }
                // Publish only after init so concurrent readers never observe a
                // partially-initialized list.
                inner[key] = list;
            }
            catch (Exception ex) when (ex is HttpResponseException or DeterministicApiException)
            {
                _exceptions.CacheException((folderId, key), ex);
                throw;
            }
        }

        return list.AsReadOnly();
    }

    /// <summary>
    /// Returns the per-folder inner dict without fetching. Used by completers
    /// and "no-filter cache display" paths that must not hit the API.
    /// </summary>
    public ConcurrentDictionary<TKey, List<TEntity>>? GetCache(Folder folder)
    {
        if (folder?.Id is null) return null;
        if (_cache is null || !_cache.TryGetValue(folder.Id.Value, out var inner)) return null;
        return inner;
    }

    public void ClearCache(Folder folder, TKey key)
    {
        var folderId = folder.Id ?? 0;
        if (_cache is not null && _cache.TryGetValue(folderId, out var inner))
        {
            inner.TryRemove(key, out _);
        }
        _exceptions.ClearCache((folderId, key));
    }

    public void ClearCache(Folder folder)
    {
        var folderId = folder.Id ?? 0;
        _cache?.TryRemove(folderId, out _);
        // Drop all per-key exception entries belonging to this folder.
        _exceptions.ClearCache(k => k.folderId == folderId);
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
    private readonly OrchDriveInfoBase _drive;
    private volatile ConcurrentDictionary<Int64, T?>? _cache = null;
    private readonly ExceptionsCachePer<Int64> _exceptions = new();
    private readonly Func<Int64?, T?> _getter;
    private readonly Action<T, string>? _initializer;
    private readonly int? _supportedApiVersionFrom;

    public SingleCachePerFolder(
        OrchDriveInfoBase drive,
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
            catch (Exception ex) when (ex is HttpResponseException or DeterministicApiException)
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
    private readonly OrchDriveInfoBase _drive;
    private volatile ConcurrentDictionary<Int64, List<T>>? _cache = null;
    private readonly ExceptionsCachePer<Int64> _exceptions = new();
    private readonly Func<Int64, IEnumerable<T>> _getter;
    private readonly Action<T, string>? _initializer;
    private readonly int? _supportedApiVersionFrom;

    public ListCachePerFolder(
        OrchDriveInfoBase drive,
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
            catch (Exception ex) when (ex is HttpResponseException or DeterministicApiException)
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
    private readonly OrchDriveInfoBase _drive;

    // 1st key: folderId
    // 2nd key: TIndexEntity.Id
    // Inner must be ConcurrentDictionary too — cmdlets like Get-OrchTestDataQueueItem
    // (ChainedThreadPool Phase2 over the queues in one folder) and
    // Get-OrchProcessRequirement (OrchThreadPool.RunForEach over the releases in
    // one folder) hit the inner dict from multiple threads sharing the same
    // folderId. A plain Dictionary can throw InvalidOperationException on rehash
    // or surface stale lookups when a writer is mid-insert.
    private volatile ConcurrentDictionary<Int64, ConcurrentDictionary<Int64, List<TEntity>>>? _cache = null;
    // Labeled tuple so ClearCache overloads can scope their clears via
    // k.folderId / k.indexId predicates (cf. KeyedSingleCachePerFolder).
    private readonly ExceptionsCachePer<(Int64 folderId, Int64 indexId)> _exceptions = new();
    private readonly Func<Int64, TIndexEntity, IEnumerable<TEntity>> _fetchFunc; // Passes folderId and index, returns an enumeration of TEntity
    private readonly Func<TIndexEntity, Int64> _getIdFunc;
    private readonly Func<TIndexEntity, string> _getNameFunc;
    private readonly Action<TEntity, string, string, string>? _initializer; // folder path, name, folder path\name
    private readonly int? _supportedApiVersionFrom;

    public IndexedListCachePerFolder(
        OrchDriveInfoBase drive,
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

        // GetOrAdd is atomic — replaces the "TryGetValue → new → assign" sequence
        // whose race could leak inner dicts under concurrent first-touch on the
        // same folder.
        var cachePerFolder = _cache.GetOrAdd(folderId,
            _ => new ConcurrentDictionary<Int64, List<TEntity>>());

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
            catch (Exception ex) when (ex is HttpResponseException or DeterministicApiException)
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
        _exceptions.ClearCache(k => k.folderId == folderId);
    }

    public void ClearCache(Folder folder)
    {
        ClearCache(folder.Id!.Value);
    }

    public void ClearCache(Folder folder, Int64 id)
    {
        if (_cache?.TryGetValue(folder.Id.GetValueOrDefault(), out var cachePerFolder) ?? false)
        {
            cachePerFolder.TryRemove(id, out _);
            _exceptions.ClearCache((folder.Id ?? 0, id));
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
    private readonly OrchDriveInfoBase _drive;

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

    // Owner-context enrichment: (entity, folderPath, ownerName, ownerPath). Lets a per-Fetch
    // caller stamp fields derived from the OWNING entity that the id-keyed cache can't know on
    // its own — e.g. a QueueItem's queue Name / PSPath. Applied alongside _initializer.
    private readonly Action<TEntity, string, string?, string?>? _ownerInitializer;

    public IncrementalCachePerFolder(
        OrchDriveInfoBase drive,
        Func<Int64, string?, ulong, ulong, string?, bool, IEnumerable<TEntity>> fetchFunc,
        Func<TEntity, TKey?> getKeyFunc,
        Action<TEntity, string>? initializer = null,
        Action<TEntity, TEntity?>? mergeFunc = null,
        Action<TEntity, string, string?, string?>? ownerInitializer = null)
    {
        _drive = drive;
        _drive._allFolderCache.Add(this);
        _fetchFunc = fetchFunc;
        _getKeyFunc = getKeyFunc;
        _initializer = initializer;
        _mergeFunc = mergeFunc;
        _ownerInitializer = ownerInitializer;
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
        bool orderAscending = false,
        string? ownerName = null,
        string? ownerPath = null)
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
        catch (Exception ex) when (ex is HttpResponseException or DeterministicApiException)
        {
            _exceptions.CacheException(folderId, ex);
            throw;
        }

        string folderPath = folder.GetPSPath();
        foreach (var entity in fetched)
        {
            _initializer?.Invoke(entity, folderPath);
            _ownerInitializer?.Invoke(entity, folderPath, ownerName, ownerPath);

            var key = _getKeyFunc(entity);
            if (key is null) continue;

            // Atomic merge+publish. The earlier non-atomic
            //   TryGetValue → mergeFunc → folderCache[key] = entity
            // could lose a concurrent writer's value when a paging Fetch and a
            // sibling AddToCache target the same (folder, key). updateValueFactory
            // may be invoked more than once under contention — current mergeFunc
            // callers carry cached fields onto entity and are idempotent.
            if (_mergeFunc is null)
            {
                folderCache[key] = entity;
            }
            else
            {
                folderCache.AddOrUpdate(
                    key,
                    addValueFactory: _ => entity,
                    updateValueFactory: (_, cached) => { _mergeFunc(entity, cached); return entity; });
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
        if (key is null) return;

        // See Fetch for the atomicity rationale.
        if (_mergeFunc is null)
        {
            folderCache[key] = entity;
        }
        else
        {
            folderCache.AddOrUpdate(
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
    private readonly OrchDriveInfoBase _drive;

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
        OrchDriveInfoBase drive,
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
        catch (Exception ex) when (ex is HttpResponseException or DeterministicApiException)
        {
            _exception.CacheException(ex);
            throw;
        }

        string drivePath = _drive.NameColonSeparator;
        foreach (var entity in fetched)
        {
            _initializer?.Invoke(entity, drivePath);

            var key = _getKeyFunc(entity);
            if (key is null) continue;

            // See IncrementalCachePerFolder.Fetch for the atomicity rationale.
            if (_mergeFunc is null)
            {
                _cache[key] = entity;
            }
            else
            {
                _cache.AddOrUpdate(
                    key,
                    addValueFactory: _ => entity,
                    updateValueFactory: (_, cached) => { _mergeFunc(entity, cached); return entity; });
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
        if (key is null) return;

        // See IncrementalCachePerFolder.Fetch for the atomicity rationale.
        if (_mergeFunc is null)
        {
            _cache[key] = entity;
        }
        else
        {
            _cache.AddOrUpdate(
                key,
                addValueFactory: _ => entity,
                updateValueFactory: (_, cached) => { _mergeFunc(entity, cached); return entity; });
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
/// Per-folder cache backing <c>Get-OrchLog</c>. Bespoke shape — the
/// Orchestrator API returns <c>Log.Id == 0</c> for every entry (server bug),
/// so per-id deduplication isn't possible. Instead, entries accumulate into a
/// <see cref="HashSet{T}"/> per folder that deduplicates by VALUE:
/// <see cref="Log"/> overrides <c>Equals</c>/<c>GetHashCode</c> over all fields,
/// so re-fetching a row already in the cache does not add a duplicate.
///
/// <c>HashSet&lt;Log&gt;.Add</c> is not thread-safe, but that is sound here: the
/// only writer is <c>Get-OrchLog</c>, which fetches folders sequentially (a
/// single folder's set is never touched by two threads at once), and no
/// completer or internal parallel path calls <see cref="Fetch"/>.
///
/// This is concrete rather than generic because the only reason this pattern
/// exists at all is the <c>Log.Id == 0</c> server bug. Other "log" endpoints
/// (e.g., AuditLog) have proper ids and use <c>IncrementalCachePerTenant</c>.
/// </summary>
public class RobotLogsCache : IFolderCacheClearable
{
    private readonly OrchDriveInfoBase _drive;
    private readonly object _lock = new();
    private volatile ConcurrentDictionary<long, HashSet<Log>>? _cache = null;

    public RobotLogsCache(OrchDriveInfoBase drive)
    {
        _drive = drive;
        _drive._allFolderCache.Add(this);
    }

    /// <summary>
    /// Always hits the API; adds the freshly-fetched logs into this folder's
    /// set (after stamping each <c>Log.Path</c>) and returns the new batch.
    /// Rows already present are dropped by <see cref="HashSet{T}"/> value
    /// equality, so the accumulated cache never holds duplicates.
    /// </summary>
    public ReadOnlyCollection<Log> Fetch(Folder folder, string? query, ulong skip, ulong first,
                                          string? orderBy = null, bool orderAscending = false)
    {
        if (_cache is null)
        {
            lock (_lock)
            {
                _cache ??= new();
            }
        }
        var folderLogs = _cache.GetOrAdd(folder.Id ?? 0, _ => new HashSet<Log>());

        var logs = _drive.OrchAPISession.GetRobotLogs(folder.Id ?? 0, query, skip, first, orderBy, orderAscending).ToList();
        string folderPath = folder.GetPSPath();
        foreach (var log in logs)
        {
            log.Path = folderPath;
            folderLogs.Add(log);
        }
        return logs.AsReadOnly();
    }

    /// <summary>
    /// Returns the accumulated set for this folder, or <c>null</c> if nothing
    /// has been fetched yet. Used by <c>Get-OrchLog</c>'s "no filter = display
    /// cache" path.
    /// </summary>
    public HashSet<Log>? GetCache(Folder folder)
    {
        return _cache is not null && _cache.TryGetValue(folder.Id ?? 0, out var set) ? set : null;
    }

    public void ClearCache()
    {
        _cache = null;
    }

    public void ClearCache(Folder folder)
    {
        _cache?.TryRemove(folder.Id ?? 0, out _);
    }
}
