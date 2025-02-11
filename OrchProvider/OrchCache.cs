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

public class ListCachePerOrganization<T> : ITenantCacheClearable
{
    private readonly OrchDriveInfo _drive;
    private List<T>? _cache = null;
    private readonly ExceptionCachePerTenant _exception = new();
    private readonly Func<string, IEnumerable<T>> getter;
    private readonly Action<T>? initializer;

    public ListCachePerOrganization(OrchDriveInfo drive, Func<string, IEnumerable<T>> getter, Action<T>? initializer = null)
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
                        var partitionGlobalId = _drive.GetPartitionGlobalId();
                        _cache = getter(partitionGlobalId!).ToList();
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
    private readonly Func<Int64, Int64, IEnumerable<TEntity>> _fetchFunc; // folderId と index を渡すと TEntity の列挙が返る
    private readonly Func<TIndexEntity, Int64> _getIdFunc;
    private readonly Func<TIndexEntity, string> _getNameFunc;
    private readonly Action<TEntity, string, string, string>? _initializer; // フォルダパス, 名前, フォルダパス\名前
    private readonly int? _supportedApiVersionFrom;

    public IndexedListCachePerFolder(
        OrchDriveInfo drive,
        Func<Int64, Int64, IEnumerable<TEntity>> fetchFunc,
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
                entities = _fetchFunc(folderId, index).ToList();
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

    public void ClearCache()
    {
        _cache = null;
        _exceptions.ClearCache();
    }
}
