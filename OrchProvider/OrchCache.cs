using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Core
{
    public interface ICacheClearable
    {
        void ClearCache();
    }

    public class SingleCachePerTenant<T> : ICacheClearable where T : class
    {
        private readonly OrchDriveInfo _drive;
        private T? _cache = null;
        private readonly ExceptionCachePerTenant _exception = new();
        private readonly Func<T?> getter;
        private readonly Action<T>? initializer;

        public SingleCachePerTenant(OrchDriveInfo drive, Func<T?> getter, Action<T>? initializer = null)
        {
            _drive = drive;
            _drive._cacheItems.Add(this);
            this.getter = getter;
            this.initializer = initializer;
        }

        public T? Get()
        {
            _exception.ThrowCachedExceptionIfAny();

            if (_cache == null)
            {
                try
                {
                    lock (this)
                    {
                        if (_cache == null)
                        {
                            _cache = getter();
                            if (_cache != null && initializer != null)
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

    public class ListCachePerTenant<T> : ICacheClearable
    {
        private readonly OrchDriveInfo _drive;
        private List<T>? _cache = null;
        private readonly ExceptionCachePerTenant _exception = new();
        private readonly Func<IEnumerable<T>> getter;
        private readonly Action<T>? initializer;

        public ListCachePerTenant(OrchDriveInfo drive, Func<IEnumerable<T>> getter, Action<T>? initializer = null)
        {
            _drive = drive;
            _drive._cacheItems.Add(this);
            this.getter = getter;
            this.initializer = initializer;
        }

        public List<T> Get()
        {
            _exception.ThrowCachedExceptionIfAny();

            if (_cache == null)
            {
                try
                {
                    lock (this)
                    {
                        if (_cache == null)
                        {
                            _cache = getter().ToList();
                            if (initializer != null)
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

    public class ListCachePerOrganization<T> : ICacheClearable
    {
        private readonly OrchDriveInfo _drive;
        private List<T>? _cache = null;
        private readonly ExceptionCachePerTenant _exception = new();
        private readonly Func<string, IEnumerable<T>> getter;
        private readonly Action<T>? initializer;

        public ListCachePerOrganization(OrchDriveInfo drive, Func<string, IEnumerable<T>> getter, Action<T>? initializer = null)
        {
            _drive = drive;
            _drive._cacheItems.Add(this);
            this.getter = getter;
            this.initializer = initializer;
        }

        public List<T> Get()
        {
            _exception.ThrowCachedExceptionIfAny();

            if (_cache == null)
            {
                try
                {
                    lock (this)
                    {
                        if (_cache == null)
                        {
                            var partitionGlobalId = _drive.GetPartitionGlobalId();
                            _cache = getter(partitionGlobalId!).ToList();
                            if (initializer != null)
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

    public class SingleCachePerFolder<T> : ICacheClearable
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
            _drive._cacheItems.Add(this);
            _getter = getter;
            _initializer = initializer;
            _supportedApiVersionFrom = supportedApiVersionFrom;
        }

        public T? Get(Folder folder)
        {
            if (folder?.Id == null || _drive.OrchAPISession.ApiVersion < _supportedApiVersionFrom)
            {
                return default;
            }

            _exceptions.ThrowCachedExceptionIfAny(folder.Id.Value);

            if (_cache == null)
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

                    if (cachePerFolder != null && _initializer != null)
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

    //public class ListCachePer<Key, T> : ICacheClearable where Key : IEquatable<Key>
    //{
    //    private readonly OrchDriveInfo _drive;
    //    private ConcurrentDictionary<Key, List<T>>? _cache = null;
    //    private readonly ExceptionsCachePer<Key> _exceptions = new();
    //    private readonly Func<Key, IEnumerable<T>> _getter;
    //    //private readonly Action<T, string>? _initializer;
    //    private readonly int? _supportedApiVersionFrom;

    //    public ListCachePer(
    //        OrchDriveInfo drive,
    //        Func<Key, IEnumerable<T>> getter,
    //        //Action<T, string>? initializer = null,
    //        int? supportedApiVersionFrom = null)
    //    {
    //        _drive = drive;
    //        _drive._cacheItems.Add(this);
    //        _getter = getter;
    //        //_initializer = initializer;
    //        _supportedApiVersionFrom = supportedApiVersionFrom;
    //    }

    //    public List<T> Get(Key key)
    //    {
    //        if (_drive.OrchAPISession.ApiVersion < _supportedApiVersionFrom)
    //        {
    //            return Enumerable.Empty<T>().ToList();
    //        }

    //        _exceptions.ThrowCachedExceptionIfAny(key);

    //        if (_cache == null)
    //        {
    //            lock (this)
    //            {
    //                _cache ??= [];
    //            }
    //        }
    //        if (!_cache.TryGetValue(key, out var cachePerFolder))
    //        {
    //            try
    //            {
    //                cachePerFolder = _getter(key).ToList();
    //                _cache[key] = cachePerFolder;

    //                //if (_initializer != null)
    //                //{
    //                //    string folderPath = folder.GetPSPath();
    //                //    foreach (var entity in cachePerFolder)
    //                //    {
    //                //        _initializer(entity, folderPath);
    //                //    }
    //                //}
    //            }
    //            catch (HttpResponseException ex)
    //            {
    //                _exceptions.CacheException(key, ex);
    //                throw;
    //            }

    //        }
    //        return cachePerFolder;
    //    }

    //    public void ClearCache(Key key)
    //    {
    //        _cache?.TryRemove(key, out _);
    //        _exceptions.ClearCache(key);
    //    }

    //    public void ClearCache()
    //    {
    //        _cache = null;
    //        _exceptions.ClearCache();
    //    }
    //}

    public class ListCachePerFolder<T> : ICacheClearable
    {
        private readonly OrchDriveInfo _drive;
        private ConcurrentDictionary<Int64, List<T>>? _cache = null;
        private readonly ExceptionsCachePer<Int64> _exceptions = new();
        private readonly Func<Int64, IEnumerable<T>>? _getter;
        private readonly Func<Int64, string?, ulong, ulong, IEnumerable<T>>? _getter2;
        private readonly Action<T, string>? _initializer;
        private readonly int? _supportedApiVersionFrom;

        private ListCachePerFolder(
            OrchDriveInfo drive,
            Action<T, string>? initializer,
            int? supportedApiVersionFrom)
        {
            _drive = drive;
            _drive._cacheItems.Add(this);
            _initializer = initializer;
            _supportedApiVersionFrom = supportedApiVersionFrom;
        }

        public ListCachePerFolder(
            OrchDriveInfo drive,
            Func<Int64, IEnumerable<T>> getter,
            Action<T, string>? initializer = null,
            int? supportedApiVersionFrom = null) : this(drive, initializer, supportedApiVersionFrom)
        {
            _getter = getter;
        }

        public ListCachePerFolder(
            OrchDriveInfo drive,
            Func<Int64, string?, ulong, ulong, IEnumerable<T>> getter2,
            Action<T, string>? initializer = null,
            int? supportedApiVersionFrom = null) : this(drive, initializer, supportedApiVersionFrom)
        {
            _getter2 = getter2;
        }

        public List<T> Get(Folder folder, string? query = null, ulong skip = 0, ulong first = ulong.MaxValue)
        {
            return GetFromCache(folder, (folderId) =>
                _getter != null ? _getter(folderId) :
                _getter2 != null ? _getter2(folderId, query, skip, first) :
                throw new InvalidOperationException()
            );
        }

        private List<T> GetFromCache(Folder folder, Func<Int64, IEnumerable<T>> fetchFunc)
        {
            if (_drive.OrchAPISession.ApiVersion < _supportedApiVersionFrom)
            {
                return [];
            }

            _exceptions.ThrowCachedExceptionIfAny(folder.Id!.Value);

            if (_cache == null)
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
                    cachePerFolder = fetchFunc(folder.Id.Value).ToList();
                    _cache[folder.Id.Value] = cachePerFolder;

                    if (_initializer != null)
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

    // 今のところ、インデックスとしては Int64 のみをサポート。この型もパラメータ化した方がいいのかもしれない
    // TIndexEntity: インデックスを含むエンティティ
    // getIndex(TIndexEntity): TIndexEntity から Int64 のインデックス値を取得する Func
    // TEntity: 取得してキャッシュするエンティティ
    public class IndexedListCachePerFolder<TIndexEntity, TEntity> : ICacheClearable where TIndexEntity : notnull
    {
        private readonly OrchDriveInfo _drive;

        // 1st key: folderId
        // 2nd key: TIndexEntity.Id
        private ConcurrentDictionary<Int64, Dictionary<Int64, List<TEntity>>>? _cache = null;
        private readonly ExceptionsCachePer<Int64> _exceptions = new();
        private readonly Func<Int64, Int64, IEnumerable<TEntity>> _fetchFunc; // folderId と index を渡すと TEntity の列挙が返る
        private readonly Func<TIndexEntity, Int64> _getIdFunc;
        private readonly Action<TEntity, string, TIndexEntity>? _initializer; // string はフォルダパス
        private readonly int? _supportedApiVersionFrom;

        public IndexedListCachePerFolder(
            OrchDriveInfo drive,
            Func<Int64, Int64, IEnumerable<TEntity>> fetchFunc,
            Func<TIndexEntity, Int64> getIdFunc,
            Action<TEntity, string, TIndexEntity>? initializer,
            int? supportedApiVersionFrom = null)
        {
            _drive = drive;
            _drive._cacheItems.Add(this);
            _fetchFunc = fetchFunc;
            _getIdFunc = getIdFunc;
            _initializer = initializer;
            _supportedApiVersionFrom = supportedApiVersionFrom;
        }

        public ICollection<TEntity> Get(Folder folder, TIndexEntity indexEntity)
        {
            if (_drive.OrchAPISession.ApiVersion < _supportedApiVersionFrom)
            {
                return [];
            }

            _exceptions.ThrowCachedExceptionIfAny(folder.Id!.Value);

            if (_cache == null)
            {
                lock (this)
                {
                    if (_cache == null)
                    {
                        _cache = [];
                    }
                }
            }

            Int64 folderId = folder.Id!.Value;
            Int64 index = _getIdFunc(indexEntity);

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

                    if (_initializer != null)
                    {
                        string folderPath = folder.GetPSPath();
                        foreach (var entity in entities)
                        {
                            _initializer(entity, folderPath, indexEntity);
                        }
                    }
                }
                catch (HttpResponseException ex)
                {
                    _exceptions.CacheException(folder.Id ?? 0, ex);
                    throw;
                }
            }

            return entities;
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
}
