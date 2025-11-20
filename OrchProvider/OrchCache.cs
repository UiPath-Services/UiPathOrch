using System.Collections.Concurrent;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Core;

public interface ITenantCacheClearable
{
    void ClearCache();
}

// IndexedListCachePerTenant で使う予定なのだけど、頑張って実装しなくてもいい気がしてきたな。。
//public interface IIndexedTenantCacheClearable<TIndex>
//{
//    void ClearCache();
//    void ClearCache(TIndex index);
//}

public interface IFolderCacheClearable
{
    void ClearCache();
    void ClearCache(Folder folder);
}

public class SingleCachePerTenant<T> : ITenantCacheClearable where T : class
{
    private readonly OrchDriveInfo _drive;
    private T? _cache = null;
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
                lock (this)
                {
                    if (_cache is null)
                    {
                        _cache = getter();
                        if (_cache is not null && initializer is not null)
                        {
                            initializer(_cache);
                        }
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
    private readonly OrchDriveInfo _drive;
    private List<T>? _cache = null;
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
                lock (this)
                {
                    if (_cache is null)
                    {
                        _cache = getter().ToList();
                        if (initializer is not null)
                        {
                            foreach (var entity in _cache)
                            {
                                initializer(entity);
                            }
                        }
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

// 今のところ、インデックスとして string のみをサポート
// 今のところ、これを使用しているのは LibraryVersion のみ
// IndexedListCachePerTenant<Library, LibraryVersion> として使う
// 実装中。。
//public class IndexedListCachePerTenant<TIndexEntity, TEntity> : IIndexedTenantCacheClearable<string> where TIndexEntity : notnull
//{
//    private readonly OrchDriveInfo _drive;

//    // key: TIndexEntity.Id
//    private ConcurrentDictionary<string, List<TEntity>>? _cache = null;
//    private readonly ConcurrentDictionary<string, ExceptionCachePerTenant> _exceptions = new();
//    private readonly Func<string, IEnumerable<TEntity>> _fetchFunc; // index を渡すと TEntity の列挙が返る
//    private readonly Func<TIndexEntity, string> _getIdFunc;
//    private readonly Func<TIndexEntity, string> _getNameFunc;
//    private readonly Action<TEntity, string, string>? _initializer; // 名前, ドライブ名\名前
//    private readonly int? _supportedApiVersionFrom;

//    public IndexedListCachePerTenant(
//        OrchDriveInfo drive,
//        Func<string, IEnumerable<TEntity>> fetchFunc,
//        Func<TIndexEntity, string> getIdFunc,
//        Func<TIndexEntity, string> getNameFunc,
//        Action<TEntity, string, string>? initializer,
//        int? supportedApiVersionFrom = null)
//    {
//        _drive = drive;
//        _drive._allTenantCache.Add(this);
//        _fetchFunc = fetchFunc;
//        _getIdFunc = getIdFunc;
//        _getNameFunc = getNameFunc;
//        _initializer = initializer;
//        _supportedApiVersionFrom = supportedApiVersionFrom;
//    }

//    public ICollection<TEntity> Get(TIndexEntity indexEntity)
//    {
//        if (_drive.OrchAPISession.ApiVersion < _supportedApiVersionFrom)
//        {
//            return [];
//        }

//        string index = _getIdFunc(indexEntity);

//        if (_exceptions.TryGetValue(index, out var ex))
//        {
//            ex.ThrowCachedExceptionIfAny();
//        }

//        if (_cache is null)
//        {
//            lock (this)
//            {
//                if (_cache is null)
//                {
//                    _cache = [];
//                }
//            }
//        }
//        if (!_cache.TryGetValue(index, out var entities))
//        {
//            try
//            {
//                entities = _fetchFunc(index).ToList();
//                _cache[index] = entities;

//                if (_initializer is not null)
//                {
//                    string indexName = _getNameFunc(indexEntity);
//                    string path = _drive.NameColonSeparator;
//                    foreach (var entity in entities)
//                    {
//                        _initializer(entity, path, indexName);
//                    }
//                }
//            }
//            catch (HttpResponseException ex)
//            {
//                _exceptions.CacheException(ex);
//                throw;
//            }
//        }

//        return entities;
//    }

//    public void ClearCache()
//    {
//        _cache = null;
//        foreach (var e in _exceptions.Values)
//        {
//            e.ClearCache();
//        }
//    }

//    public void ClearCache(string index)
//    {
//        _cache?.TryRemove(index, out _);
//        _exceptions.ClearCache(); // ちょっと横着な実装だけど、機能的にはこれで問題ないはず
//    }

//    public void ClearCache(TIndexEntity indexEntity)
//    {
//        ClearCache(_getIdFunc(indexEntity));
//    }
//}

// partitionGlobalId をキーとする、組織エンティティ
// これはすべての組織の、固有のエンティティのキャッシュを表す。
public class ListCachePerOrganization<T> : ITenantCacheClearable
{
    private readonly OrchDriveInfo _drive;
    private static readonly ConcurrentDictionary<string, List<T>> _cache = [];
    private static readonly ExceptionsCachePer<string> _exception = new(); // per org の例外をこれで保持
    // input: partitionGlobalId
    private readonly Func<string, IEnumerable<T>> _getter;
    private readonly Action<T>? _initializer;

    // -ExpandDetail できるエンティティについては、下記も必要となる
    private readonly Func<T, string?>? _getterId;
    // (partitionGlobalId, id) をキーとする、組織エンティティの詳細キャッシュ
    private static ConcurrentDictionary<(string partitionGlobalId, string id), T?>? _cacheDetailed = null;
    private readonly Func<string, string, T?>? _getterDetailed;
    // (partitionGlobalId, id) をキーとする、組織エンティティの詳細取得時の例外キャッシュ
    private static readonly ExceptionsCachePer<(string partitionGlobalId, string id)> _exceptionDetailed = new(); // per (org, id) の例外をこれで保持

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
            lock (this)
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
            // 詳細キャッシュがあれば、それを返す
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
            lock (this)
            {
                _cacheDetailed ??= [];
            }
        }

        if (!_cacheDetailed.TryGetValue((partitionGlobalId, id), out var cachePerOrgDetailed))
        {
            lock (this)
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

        // _cache の更新（リストが存在しなければ何もしない）
        if (_cache.TryGetValue(partitionGlobalId, out var list))
        {
            // メインスレッドのみから呼ばれる前提なので lock は不要
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

        // _cacheDetailed の Upsert
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

    //public void ClearCacheAll()
    //{
    //    _cache.Clear();
    //    _exception.ClearCache();
    //}
}

// partitionGlobalId をキーとする、組織の単一エンティティ
// これは組織全体で共有される単一のエンティティのキャッシュを表す
public class SingleCachePerOrganization<T> : ITenantCacheClearable where T : class
{
    private readonly OrchDriveInfo _drive;
    
    // 組織ごとにキャッシュ（static = 全ドライブインスタンスで共有）
    private static readonly ConcurrentDictionary<string, T> _cache = [];
    private static readonly ExceptionsCachePer<string> _exception = new();
    
    // getter: partitionGlobalId を引数に取り、T を返す
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
            lock (this)
            {
                if (!_cache.TryGetValue(partitionGlobalId, out entity))
                {
                    try
                    {
                        entity = _getter(partitionGlobalId);
                        if (entity is not null)
                        {
                            _cache[partitionGlobalId] = entity;
                            _initializer?.Invoke(entity);
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
            // キャッシュ済みの場合も initializer を実行（Pathの設定など）
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

// 今のところ、インデックスとしては Int64 のみをサポート。この型もパラメータ化した方がいいのかもしれない
// TIndexEntity: インデックスを含むエンティティ
// getIndex(TIndexEntity): TIndexEntity から Int64 のインデックス値を取得する Func
// TEntity: 取得してキャッシュするエンティティ
public class IndexedCachePerTenant<TIndexEntity, TEntity> : ITenantCacheClearable where TIndexEntity : notnull
{
    private readonly OrchDriveInfo _drive;

    // key: TIndexEntity.Id
    private ConcurrentDictionary<Int64, TEntity?>? _cache = null;
    private readonly ExceptionsCachePer<Int64> _exceptions = new();
    private readonly Func<Int64, TEntity?> _fetchFunc; // index を渡すと TEntity が返る
    private readonly Func<TIndexEntity, Int64> _getIdFunc;
    private readonly Func<TIndexEntity, string> _getNameFunc;
    private readonly Action<TEntity, string>? _initializer; // 名前
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
            lock (this)
            {
                _cache ??= [];
            }
        }

        if (!_cache.TryGetValue(index, out var entity))
        {
            try
            {
                entity = _fetchFunc(index);
                _cache[index] = entity;

                if (entity is not null && _initializer is not null)
                {
                    string indexName = _getNameFunc(indexEntity);
                    _initializer(entity, indexName);
                }
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

public class SingleCachePerFolder<T> : IFolderCacheClearable
{
    private readonly OrchDriveInfo _drive;
    private ConcurrentDictionary<Int64, T?>? _cache = null;
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
            lock (this)
            {
                _cache ??= [];
            }
        }
        if (!_cache.TryGetValue(folder.Id!.Value, out var cachePerFolder))
        {
            try
            {
                cachePerFolder = _getter(folder.Id.Value);
                _cache[folder.Id.Value] = cachePerFolder;

                if (cachePerFolder is not null && _initializer is not null)
                {
                    string folderPath = folder.GetPSPath();
                    _initializer(cachePerFolder, folderPath);
                }
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
    private readonly OrchDriveInfo _drive;
    private ConcurrentDictionary<Int64, List<T>>? _cache = null;
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

        _exceptions.ThrowCachedExceptionIfAny(folder.Id!.Value);

        if (_cache is null)
        {
            lock (this)
            {
                _cache ??= [];
            }
        }

        if (!_cache.TryGetValue(folder.Id!.Value, out var cachePerFolder))
        {
            try
            {
                cachePerFolder = _getter(folder.Id.Value).ToList();
                _cache[folder.Id.Value] = cachePerFolder;

                if (_initializer is not null)
                {
                    string folderPath = folder.GetPSPath();
                    foreach (var entity in cachePerFolder)
                    {
                        _initializer(entity, folderPath);
                    }
                }
            }
            catch (HttpResponseException ex)
            {
                _exceptions.CacheException(folder.Id ?? 0, ex);
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
        // root folder は、Id が null であることに注意！
        ClearCache(folder.Id);
    }

    public void ClearCache()
    {
        _cache = null;
        _exceptions.ClearCache();
    }
}

// 今のところ、インデックスとしては Int64 のみをサポート。この型もパラメータ化した方がいいのかもしれない
// TODO: GetById() を実装すると良いのではないか？
// TIndexEntity: インデックスを含むエンティティ
// getIndex(TIndexEntity): TIndexEntity から Int64 のインデックス値を取得する Func
// TEntity: 取得してキャッシュするエンティティ
public class IndexedListCachePerFolder<TIndexEntity, TEntity> : IFolderCacheClearable where TIndexEntity : notnull
{
    private readonly OrchDriveInfo _drive;

    // 1st key: folderId
    // 2nd key: TIndexEntity.Id
    private ConcurrentDictionary<Int64, Dictionary<Int64, List<TEntity>>>? _cache = null;
    // この例外キャッシュの機能がいまいちだな。。フォルダごとにキャッシュをクリアできるといいのに。
    private readonly ExceptionsCachePer<(Int64, Int64)> _exceptions = new();
    private readonly Func<Int64, TIndexEntity, IEnumerable<TEntity>> _fetchFunc; // folderId と index を渡すと TEntity の列挙が返る
    private readonly Func<TIndexEntity, Int64> _getIdFunc;
    private readonly Func<TIndexEntity, string> _getNameFunc;
    private readonly Action<TEntity, string, string, string>? _initializer; // フォルダパス, 名前, フォルダパス\名前
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
            lock (this)
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
                cachePerFolder[index] = entities;

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
        _exceptions.ClearCache(); // ちょっと横着な実装だけど、機能的にはこれで問題ないはず
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
            _exceptions.ClearCache(); // ちょっと横着な実装だけど、機能的にはこれで問題ないはず
        }
    }

    public void ClearCache()
    {
        _cache = null;
        _exceptions.ClearCache();
    }
}

#region TestManager 用キャッシュクラス

// fetchFunc: 引数なしで、Enumerable<T> を返す Func
// 今のところ、TmProject のみで使用している
// なんだかすごく遅くなってしまったので一旦ボツ。たぶんロックをもう少し広くかけないと、API を何度も呼び出してしまうのではないか。
//public class TestListCachePerTenant0<T> : ITenantCacheClearable
//{
//    private readonly OrchTmDriveInfo _drive;
//    private List<T>? _cache = null;
//    private readonly ExceptionCachePerTenant _exception = new();
//    private readonly Func<IEnumerable<T>?> _fetchFunc;
//    private readonly Action<T>? _initializer;
//    private readonly int? _supportedApiVersionFrom;

//    public TestListCachePerTenant0(
//        OrchTmDriveInfo drive,
//        Func<IEnumerable<T>?> fetchFunc,
//        Action<T>? initializer = null,
//        int? supportedApiVersionFrom = null)
//    {
//        _drive = drive;
//        _fetchFunc = fetchFunc;
//        _drive._allTenantCache.Add(this);
//        _initializer = initializer;
//        _supportedApiVersionFrom = supportedApiVersionFrom;
//    }

//    public List<T> Get()
//    {
//        if (_drive.OrchAPISession.ApiVersion < _supportedApiVersionFrom)
//        {
//            return [];
//        }

//        _exception.ThrowCachedExceptionIfAny();

//        if (_cache is null)
//        {
//            lock (this)
//            {
//                _cache ??= [];
//            }
//        }

//        if (_cache is not null)
//        {
//            try
//            {
//                _cache = _fetchFunc()?.ToList();

//                if (_initializer is not null && _cache is not null)
//                {
//                    foreach (var entity in _cache)
//                    {
//                        _initializer(entity);
//                    }
//                }
//            }
//            catch (HttpResponseException ex)
//            {
//                _exception.CacheException(ex);
//                throw;
//            }
//        }
//        else
//        {
//            _cache = [];
//        }

//        return _cache!;
//    }

//    public void ClearCache()
//    {
//        _cache = null;
//        _exception.ClearCache();
//    }
//}

// fetchFunc: 引数に TmProject を取り、Enumerable<T> を返す Func
public class TestListCachePerTenant1<T> : ITenantCacheClearable
{
    private readonly OrchTmDriveInfo _drive;
    private ConcurrentDictionary<string, List<T>>? _cache = null;
    private readonly ExceptionsCachePer<string> _exceptions = new();
    private readonly Func<TmProject, IEnumerable<T>> _fetchFunc;
    private readonly Action<T, TmProject>? _initializer;
    private readonly int? _supportedApiVersionFrom;

    public TestListCachePerTenant1(
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

    public List<T> Get(TmProject project)
    {
        if (_drive.OrchAPISession.ApiVersion < _supportedApiVersionFrom)
        {
            return [];
        }

        _exceptions.ThrowCachedExceptionIfAny(project.id!);

        if (_cache is null)
        {
            lock (this)
            {
                _cache ??= [];
            }
        }

        if (!_cache.TryGetValue(project.id!, out var cachePerFolder))
        {
            try
            {
                cachePerFolder = _fetchFunc(project).ToList();
                _cache[project.id!] = cachePerFolder;

                if (_initializer is not null)
                {
                    string projectPath = project.GetPSPath();
                    foreach (var entity in cachePerFolder)
                    {
                        _initializer(entity, project);
                    }
                }
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
}

// getFunc: 引数なしで、T を返す Func
public class TestSingleCachePerTenant0<T> : ITenantCacheClearable
{
    private readonly OrchTmDriveInfo _drive;
    private T? _cache = default;
    private readonly ExceptionCachePerTenant _exception = new();
    private readonly Func<T?> _getFunc;
    private readonly Action<T>? _initializer;
    private readonly int? _supportedApiVersionFrom;

    public TestSingleCachePerTenant0(
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
                _cache = _getFunc();

                if (_initializer is not null && _cache is not null)
                {
                    _initializer(_cache);
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
        _cache = default;
        _exception.ClearCache();
    }
}

// getFunc: 引数に TmProject を取り、T を返す Func
public class TestSingleCachePerTenant1<T> : ITenantCacheClearable
{
    private readonly OrchTmDriveInfo _drive;
    private ConcurrentDictionary<string, T?>? _cache = null;
    private readonly ExceptionsCachePer<string> _exceptions = new();
    private readonly Func<TmProject, T?> _getFunc;
    private readonly Action<T, TmProject>? _initializer;
    private readonly int? _supportedApiVersionFrom;

    public TestSingleCachePerTenant1(
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
            lock (this)
            {
                _cache ??= [];
            }
        }

        if (!_cache.TryGetValue(project.id!, out var cachePerProject))
        {
            try
            {
                cachePerProject = _getFunc(project);
                _cache[project.id!] = cachePerProject;

                if (_initializer is not null && cachePerProject is not null)
                {
                    string projectPath = project.GetPSPath();
                    _initializer(cachePerProject, project);
                }
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
