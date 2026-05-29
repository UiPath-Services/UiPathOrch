using System.Collections.Concurrent;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Core;

// Platform Management-specific cache classes. Moved out of OrchCache.cs in the
// 1.5.0 cache refactor to mirror the OrchDuCache.cs / OrchTmCache.cs precedent
// (family-specific cache classes live under their family folder; the universal
// templates stay in OrchCache.cs at the top level).

/// <summary>
/// Org-scoped cache backing the Identity <c>/api/Directory/BulkResolveByName</c>
/// endpoint. Bespoke shape — the API takes up to 20 names per call and returns
/// <c>{ name → PmGroupMember | null }</c>, with explicit <c>null</c> entries for
/// names it cannot resolve. We cache those nulls (negative caching) so a second
/// query for an unresolved name skips the API.
///
/// This is concrete rather than generic because the chunk size (20), the
/// composite key shape (kind, name), and the PmGroupMember value type are all
/// fixed by this one endpoint; a generic class would expose unused flexibility
/// for no real consumer.
///
/// Storage is <c>static</c> so all drive instances pointing at the same org
/// share one cache; same-org drives observing the same name see the cached
/// result instead of each issuing their own API call.
/// </summary>
public class PmGroupMembersCache : ITenantCacheClearable
{
    private const int ChunkSize = 20;

    private readonly OrchDriveInfoBase _drive;

    // Shared static storage keyed by (partitionGlobalId, kind, name). null value
    // = "API confirmed this name has no member" (negative caching).
    private static readonly ConcurrentDictionary<(string partitionGlobalId, string kind, string name), PmGroupMember?> _cache = new();
    private static readonly ExceptionsCachePer<string> _exceptions = new();
    private static readonly object _lock = new();

    public PmGroupMembersCache(OrchDriveInfoBase drive)
    {
        _drive = drive;
        _drive._allTenantCache.Add(this);
    }

    /// <summary>
    /// Resolve <paramref name="names"/> under the given <paramref name="kind"/>
    /// (<c>"user"</c> / <c>"group"</c> / <c>"application"</c>). Names already in
    /// the cache are returned immediately; the rest are bulk-fetched from the
    /// API in chunks of <see cref="ChunkSize"/>, populated into the cache
    /// (including <c>null</c> entries for unresolved names), then returned.
    ///
    /// Empty / null names are silently skipped — they cannot be cache keys.
    /// </summary>
    public IDictionary<string, PmGroupMember?> GetMany(string kind, IEnumerable<string> names)
    {
        // Data-fetch path: force the partition lookup (the property is passive).
        var partitionGlobalId = _drive.GetPartitionGlobalId();
        if (string.IsNullOrEmpty(partitionGlobalId)) return new Dictionary<string, PmGroupMember?>();

        _exceptions.ThrowCachedExceptionIfAny(partitionGlobalId);

        var inputNames = names
            .Where(n => !string.IsNullOrEmpty(n))
            .Distinct()
            .ToList();
        if (inputNames.Count == 0) return new Dictionary<string, PmGroupMember?>();

        var uncached = inputNames
            .Where(n => !_cache.ContainsKey((partitionGlobalId, kind, n)))
            .ToList();

        if (uncached.Count > 0)
        {
            lock (_lock)
            {
                // Re-check inside the lock — another thread may have populated.
                uncached = uncached
                    .Where(n => !_cache.ContainsKey((partitionGlobalId, kind, n)))
                    .ToList();

                if (uncached.Count > 0)
                {
                    try
                    {
                        foreach (var chunk in uncached.Chunk(ChunkSize))
                        {
                            var result = _drive.OrchAPISession.PmBulkResolveByName(partitionGlobalId, kind, chunk);
                            foreach (var kvp in result ?? new Dictionary<string, PmGroupMember>())
                            {
                                _cache[(partitionGlobalId, kind, kvp.Key)] = kvp.Value;
                            }
                        }
                    }
                    catch (Exception ex) when (ex is HttpResponseException or DeterministicApiException)
                    {
                        _exceptions.CacheException(partitionGlobalId, ex);
                        throw;
                    }
                }
            }
        }

        var ret = new Dictionary<string, PmGroupMember?>();
        foreach (var name in inputNames)
        {
            if (_cache.TryGetValue((partitionGlobalId, kind, name), out var value))
            {
                ret[name] = value;
            }
            // If the cache somehow doesn't have an entry (the API failed to
            // include the name in its response, defensive), skip it. Callers
            // that need to know "we tried but failed to even get a verdict"
            // should compare ret.Keys to their input.
        }
        return ret;
    }

    public void ClearCache()
    {
        var partitionGlobalId = _drive.PartitionGlobalId;
        if (string.IsNullOrEmpty(partitionGlobalId)) return;
        foreach (var k in _cache.Keys.Where(k => k.partitionGlobalId == partitionGlobalId).ToList())
        {
            _cache.TryRemove(k, out _);
        }
        _exceptions.ClearCache(partitionGlobalId);
    }
}
