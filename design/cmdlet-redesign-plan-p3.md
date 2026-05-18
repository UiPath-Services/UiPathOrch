# P3 plan — class-ify the remaining raw caches (with scope corrections)

Status: Shipped in 1.4.2 (master `69fd5fd`). All phases below
(P3.0a–P3.5 + Group F) landed; the P4-candidate "`-First` cmdlet
cache consistency" was also closed post-1.4.1 (commit `9009917`).
Remaining open items live in the parent plan (Type B/C re-evaluation,
`-Expand*` removal) and the AuditLog sibling-cmdlet decision in P2.
Reference implementations: User (`d9e8795` / `d9f550a`), Process
(`7ec1ec3` / `d5977c7`), Trigger (`60facb4` / `670545f`),
AuditLog cache-only (`7015ea5`).
Parent plans: `design/cmdlet-redesign-plan.md`,
`design/cmdlet-redesign-plan-p2.md`.

## Scope

Two concerns folded into one plan:

1. **Scope correction (P3.0)** — three caches I migrated in earlier
   phases as `Per*Tenant` should have been `Per*Organization` because
   they back PM (Platform Management) entities, which are shared
   across all tenants in the same organization. The current
   per-tenant implementation causes duplicate API calls and duplicate
   storage when multiple drives share an org.
2. **Class-ifying the remaining raw caches (P3.1-P3.5)** — 13 raw
   `_dic*` fields in `OrchDriveInfo.cs` to convert to cache classes,
   following the patterns established in P0-P2.

No cmdlet redesign in P3.

## P3.0 — Scope correction for past PM migrations

### Past mistakes

| Cache (current name)            | Wrong class (current)                                | Right class (needed)                                       | Origin commit |
| ------------------------------- | ---------------------------------------------------- | ---------------------------------------------------------- | ------------- |
| `SearchPmDirectoryCache`        | `KeyedSingleCachePerTenant<string, ...>`             | `KeyedSingleCachePerOrganization<string, ...>` 🆕         | `7c1c6c0`     |
| `PmAvailableUserBundles`        | `KeyedSingleCachePerTenant<string, ...>`             | `KeyedSingleCachePerOrganization<string, ...>` 🆕         | `6c89ad0`     |
| `PmUserLicenseGroupAllocations` | `KeyedListCachePerTenant<string, ...>`               | `KeyedListCachePerOrganization<string, ...>` 🆕           | `6ca1c1c`     |

**Impact today**: multiple drives pointing to the same org each hold
their own copy of these caches and each make their own API calls. No
behavior bug — just wasted bandwidth and storage. The `Pm` prefix in
the API path means the data is org-scoped, so different drives' copies
are guaranteed identical (modulo eviction timing).

### New cache classes (sibling org-scoped variants)

- **`KeyedSingleCachePerOrganization<TKey, TEntity>`** — org-scoped
  sibling of `KeyedSingleCachePerTenant`. Storage is `static`
  internally (same pattern as `ListCachePerOrganization`) so all
  drives pointing to the same org share one cache. Keyed by
  `(partitionGlobalId, TKey)`.
- **`KeyedListCachePerOrganization<TKey, TEntity>`** — org-scoped
  sibling of `KeyedListCachePerTenant`. Same storage pattern.

**Constructor signatures** (fetcher takes partitionGlobalId
explicitly, matching the existing `ListCachePerOrganization` pattern):

```csharp
public KeyedSingleCachePerOrganization(
    OrchDriveInfo drive,
    Func<string, TKey, TEntity?> fetchFunc,        // (partitionGlobalId, key) -> entity
    Action<TEntity, TKey>? initializer = null,
    IEqualityComparer<TKey>? keyComparer = null,
    int? supportedApiVersionFrom = null);

public KeyedListCachePerOrganization(
    OrchDriveInfo drive,
    Func<string, TKey, IEnumerable<TEntity>> fetchFunc,
    Action<TEntity, TKey>? initializer = null,
    IEqualityComparer<TKey>? keyComparer = null,
    int? supportedApiVersionFrom = null);
```

The class handles `_drive.GetPartitionGlobalId()` lookup + null/empty
bail; the fetcher receives the non-null partitionGlobalId. Callers
don't have to remember to call `GetPartitionGlobalId()!` inside their
lambda (the current `Per*Tenant` lambdas do, leading to redundant
lookups on every fetch).

**API surface (intentionally narrow)**: `Get(key)` /
`ClearCache(key)` / `ClearCache()`. No `Set` / upsert method (unlike
the existing `ListCachePerOrganization.Set(t)` line 262). The PM
caches today are read-only from cmdlet perspective — they're
populated on-demand and invalidated on mutation cmdlets. Add a
mutation API only when a consumer actually needs it.

### partitionGlobalId null/empty handling

Both new classes follow the existing `PerOrganization` pattern: if
`_drive.GetPartitionGlobalId()` returns null or empty, `Get` returns
`default(TEntity)` (or empty collection for the List variant) and
`ClearCache` is a no-op. The drive simply doesn't participate in the
org cache when it lacks a partitionGlobalId.

### `ClearCache()` semantics (current drive's org only)

The new classes follow the existing `ListCachePerOrganization`
behavior (line 307-325): `ClearCache()` removes only the entry
keyed by **this drive's** partitionGlobalId. Other orgs' cache
entries stay intact. Same-org drives (which share the static
storage) **do** see their cache cleared, which is the desired
behavior for `Clear-OrchCache`.

### Concurrency: single-threaded fetch

PM API calls under concurrent load are wasteful — two drives in the
same org issuing the same query in parallel both go to the network.
Worse, they can return inconsistent snapshots that then disagree
about cache contents.

**Decision (per user direction)**: serialize PM cache fetches with a
**single global lock** in each new class. Concurrency across distinct
`partitionGlobalId`s is possible in principle (one lock per org) but
adds complexity for marginal benefit — accept single-threaded as the
initial implementation. Optimize later if PM operations become a
bottleneck.

**Lock scope precisely**:

- **Fetch path** (cache miss): hold the static `_lock` from the
  `TryGetValue` miss through the `_fetchFunc` call and cache write.
  Ensures at most one in-flight fetch per closed generic.
- **Cache read** (hit): lock-free. `ConcurrentDictionary.TryGetValue`
  is thread-safe.
- **`ClearCache`**: lock-free. `ConcurrentDictionary.TryRemove` is
  atomic; concurrent readers see the entry either before or after
  removal, never partial.

This matches the existing `PerOrganization` classes' threading model.

### Phasing within P3.0

- **P3.0a**: add `KeyedSingleCachePerOrganization<TKey, TEntity>` and
  `KeyedListCachePerOrganization<TKey, TEntity>` to `OrchCache.cs`.
  Single commit, no consumers yet (parallel to how
  `KeyedSingleCachePerTenant` was added in `aa4b66b`).
- **P3.0b**: re-migrate the 3 caches to the org-scoped classes.
  Each migration changes three things in the same commit:
  1. **Field type**: `KeyedSingleCachePerTenant<...>` →
     `KeyedSingleCachePerOrganization<...>` (or List variant).
  2. **Ctor lambda**: current lambdas call
     `_drive.GetPartitionGlobalId()` inside (the Tenant variant
     doesn't accept partitionGlobalId), so the lambda must rewrite to
     accept the partitionGlobalId argument. Example:
     ```csharp
     // Before (P2.x, tenant-scoped):
     SearchPmDirectoryCache = new(this,
         key => OrchAPISession.SearchPmDirectory(GetPartitionGlobalId()!, key),
         (arr, _) => { ... });

     // After (P3.0b, org-scoped):
     SearchPmDirectoryCache = new(this,
         (partitionGlobalId, key) => OrchAPISession.SearchPmDirectory(partitionGlobalId, key),
         (arr, _) => { ... });
     ```
  3. **External callsites**: most are unchanged (calls go through
     the same `<X>.Get(key)` / `<X>.ClearCache(...)` signatures). The
     field type swap shouldn't break any caller; verify per-cache
     during the commit.

  **One commit per cache** — each commit is independently revertable
  and the empirical verification (a deploy + Verbose-log inspection
  to confirm duplicate API calls have vanished) is per-cache.

## P3.1-P3.5 — Class-ify the remaining raw caches

External callsite counts measured against `master` at `7015ea5`.

### Group A — Tuple-key single-value PerTenant (3, link caches only)

P3.1 will be the **first practical use of `KeyedSingleCachePerTenant`
with a tuple TKey**. Tuple-keyed `ExceptionsCachePer<(folderId, key)>`
is already in production via `KeyedSingleCachePerFolder` (commit
`7ec1ec3`), so the exception-cache half is known to work.

| Field                       | TKey                          | TEntity                  | Class                                                   |
| --------------------------- | ----------------------------- | ------------------------ | ------------------------------------------------------- |
| `_dicAssetLinks`            | `(folderId, assetId)`         | `AccessibleFoldersDto?`  | `KeyedSingleCachePerTenant`                             |
| `_dicQueueLinks`            | `(folderId, queueId)`         | `AccessibleFoldersDto?`  | `KeyedSingleCachePerTenant`                             |
| `_dicBucketLinks`           | `(folderId, bucketId)`        | `AccessibleFoldersDto?`  | `KeyedSingleCachePerTenant`                             |

`_dicPmBulkResolveByName` was originally in this group but moved to
out-of-scope (see [Out of scope / future](#out-of-scope--future-p4-candidates)).
It uses a batched bulk-lookup access pattern (chunks of 20 names per
API call) that doesn't fit `KeyedSingleCachePerOrganization`'s
single-key `Get(key)` API. Both the scope correction (org-scoped) and
the batched-lookup shape need a different design.

The 3 link caches expose invalidation through `Clear<X>LinkCache`
wrappers (line 683, 885, 1220 in OrchDriveInfo). Wrapper bodies
become thin shims:

```csharp
public void ClearAssetLinkCache(Int64 assetId) =>
    AssetLinks.ClearCache(k => k.assetId == assetId);
```

`ClearCache(predicate)` walks `_cache.Keys` and `_exceptions` keys via
the predicate. O(N) per call; acceptable for typical tenant sizes.

### Group B — Tuple-key list PerTenant (2)

Absorbed by **existing** `KeyedListCachePerTenant<TKey, TEntity>`.

| Field                       | TKey                                 | TEntity              |
| --------------------------- | ------------------------------------ | -------------------- |
| `_dicPackageVersions`       | `(feedId, packageId)`                | `Package`            |
| `_dicPackageEntryPoint`     | `(feedId, pkgId, version)`           | `PackageEntryPoint`  |

The 2-level dict in `_dicPackageVersions` flattens to a tuple key.
`_dicPackageEntryPoint` is already tuple-keyed at the API call site.

`_dicPackages_Exceptions` was left behind by the earlier Packages
migration in `4cf1a30` because `GetPackageVersions` still referenced
it. **Verify during P3.2** that this is still the case; if it is, the
reference goes away with the migration. If it's already orphan, drop
in the same commit.

### Group C — Per-folder accumulate, query-driven (2)

Both have `-First` / `-Skip` / filter parameters in their cmdlets,
which makes them **incremental-accumulate** by nature (filter-driven
fetch, side-effect accumulation for downstream readers).

Absorbed by **existing** `IncrementalCachePerFolder<TKey, TEntity>`,
**not** `ListCachePerFolder`.

| Field                              | TEntity                | External raw-field | Notes                                                                                                                             |
| ---------------------------------- | ---------------------- | ------------------ | --------------------------------------------------------------------------------------------------------------------------------- |
| `_dicJobsHavingExecutionMedia`     | `ExecutionMedia`       | 7                  | Per-folder; cmdlet has `-Skip`/`-First`. Today the wrapper writes synthetic `Job` rows to the `Jobs` cache as a side effect — drop that mirror. |
| `_dicTestCaseExecutions`           | `TestCaseExecution`    | 6                  | Per-folder; cmdlet has `-Skip`/`-First`. Today the wrapper has a "use cache only when no filter/skip/first" guard — replace with always-fetch+accumulate (matching other `-First` cmdlets). |

The current `_dicTestCaseExecutions` filter-bypass logic was an
ad-hoc workaround for the lack of an incremental cache. With
`IncrementalCachePerFolder`, every fetch (filter or not) accumulates
into the cache and returns the freshly-fetched subset — the same
semantic the existing `Get-OrchAuditLog` / `Get-OrchJob` /
`Get-OrchQueueItem` (post-P3.4) provide.

### Group D — Per-folder, per-id, list-of-T (1) — **needs new class**

Needs **new** `KeyedListCachePerFolder<TKey, TEntity>` (sibling of
`KeyedSingleCachePerFolder` added in `7ec1ec3`).

| Field                       | TKey | TEntity              |
| --------------------------- | ---- | -------------------- |
| `_dicTestCaseAssertions`    | `long` (testCaseExecId) | `TestCaseAssertion`  |

Bundle the new class with the migration in a single commit, mirroring
how `KeyedSingleCachePerFolder` was bundled with Process in `7ec1ec3`.

### Group E — Per-folder query-driven, accumulate-only (2)

Absorbed by **existing** `IncrementalCachePerFolder<long, TEntity>`.

| Field                       | TEntity              | External raw-field |
| --------------------------- | -------------------- | ------------------ |
| `_dicTestSetExecutions`     | `TestSetExecution`   | 4                  |
| `_dicQueueItems`            | `QueueItem`          | 6                  |

Both follow the same pattern as the migrated `_dicAuditLogs`
(`7015ea5`):
- Filter-driven `Fetch` always hits the API; results accumulate into
  the cache.
- The cmdlet has a "no filter = display cache contents" path
  (TestSetExecution: line 226-249; QueueItem: line 287-293). The cmdlet
  rewrites these to read from `<X>.GetCache()` instead of the raw dict.
- Completers reuse the same cache via `GetCache()`.

**`_dicQueueItems` 3-level structure**: today the cache is
`folderId → queueName → queueItemId → QueueItem`. The per-queue
middle level is a display-time grouping artifact, not a data-model
requirement (QueueItem already carries its queue name). Flatten to
`IncrementalCachePerFolder<long, QueueItem>`; the cmdlet's "no filter
= cache display" path regroups by queue name at display time using
`GroupBy`.

**`GetQueueItemById` cache write**: today's per-id detail fetcher
writes to the same dict. After migration, the wrapper calls
`QueueItems.AddToCache(folder, item)` (per-folder `AddToCache(Folder,
TEntity)` exists at OrchCache.cs:1239).

### Group F — Genuinely bespoke (0, was 2)

Both former Group F entries migrated post-1.4.1.

`_dicPmAuditLogs` → `IncrementalCachePerTenant<PmAuditLog, PmAuditLog>`
by using the entity itself as the key (`log => log`). The
`ConcurrentDictionary<PmAuditLog, PmAuditLog>` honors
`PmAuditLog.GetHashCode/Equals`, preserving the structural-dedup
semantic of the previous `HashSet<PmAuditLog>`. The org-scope
correction noted previously (PM endpoint → should be org-scoped) is
still pending — when an accumulating org class lands, this would
migrate again.

`_dicRobotLogs` → concrete `RobotLogsCache` class (concrete because
the `Log.Id == 0` server bug forces the accumulate-into-bag pattern
that fits no existing generic class). Per-folder
`ConcurrentBag<Log>` accumulator, `IFolderCacheClearable`-registered
so `Clear-OrchCache -Path <folder>` flushes per folder. The
cmdlet's "no filter = display cache" path reads from
`RobotLogs.GetCache(folder)`.

The decision to keep the new classes concrete (vs generic) matches
the `PmGroupMembersCache` rationale: each is one-of-a-kind today, and
a generic class would expose unused flexibility for no real consumer.
Revisit when a second consumer of either shape appears.

## Phasing

| Phase     | Content                                                                                                                                  |
| --------- | ---------------------------------------------------------------------------------------------------------------------------------------- |
| **P3.0a** | Add `KeyedSingleCachePerOrganization` + `KeyedListCachePerOrganization` to `OrchCache.cs`. Both use static internal storage and a static global lock (sequential fetch). |
| **P3.0b** | Re-migrate the 3 PM caches (`SearchPmDirectoryCache`, `PmAvailableUserBundles`, `PmUserLicenseGroupAllocations`) to the new org-scoped classes. Single commit per cache (smaller diffs, easier verification). |
| P3.1      | Group A — 4 link/lookup caches → `KeyedSingleCachePerTenant` (+ `Per Organization` for `PmBulkResolveByName`). First tuple-TKey usage; smoke-test on first deploy. |
| P3.2      | Group B — `_dicPackageVersions` and `_dicPackageEntryPoint` → `KeyedListCachePerTenant`. Drop `_dicPackages_Exceptions` after verifying its current use. |
| P3.3      | Group C — `_dicJobsHavingExecutionMedia` and `_dicTestCaseExecutions` → `IncrementalCachePerFolder`. Drop the JobsHavingExecutionMedia cross-cache mirror; replace TestCaseExecutions' filter-bypass with always-fetch+accumulate. |
| P3.4      | Group E — `_dicTestSetExecutions` and `_dicQueueItems` → `IncrementalCachePerFolder`. Flatten the QueueItems 3-level structure; route GetQueueItemById through `AddToCache`. |
| P3.5      | Group D — add `KeyedListCachePerFolder<TKey, TEntity>` + migrate `_dicTestCaseAssertions` (single commit, mirroring `7ec1ec3`). |

**Stop-at-P3.3 option**: P3.4 and P3.5 are mechanical and provide
incremental value but no critical correction. P3.0 + P3.1 + P3.2 +
P3.3 cover all the high-value migrations (scope correction + most
common caches + the `-First` consistency fix).

## Out of scope / future (P4 candidates)

Surfaced during P3 review but not addressed by P3 itself:

1. **`-First` cmdlet cache consistency.** Done post-1.4.1 in commit
   `9009917`. A cross-check of all 12 `-First` cmdlets had revealed
   inconsistent cache backings; the three session cmdlets are now
   aligned with the `IncrementalCache` (always-fetch + exception
   cache) shape:
   - `Get-OrchUserSession` had **no cache backing** (direct
     `OrchAPISession.GetGlobalSessions` call in the cmdlet body) →
     now `IncrementalCachePerTenant<long, Session>`; `Path` setup
     moved into the cache class initializer.
   - `Get-OrchUnattendedSession` was `ListCachePerTenant`
     (fetch-once-then-cache-forever, unsuitable for session status)
     → now `IncrementalCachePerTenant<long, MachineSessionRuntime>`.
     External behavior identical ("fetch all") but each call now
     hits the wire instead of returning stale cache.
   - `Get-OrchMachineSession` was `ListCachePerFolder`, same issue →
     now `IncrementalCachePerFolder<long, MachineSessionRuntime>`.
   - `Get-OrchAlert` needed **no change**: it was already on the
     reference `IncrementalCachePerTenant` shape that the others were
     migrated to match.
   10 non-cmdlet callsites switched `.Get(...)` → `.Fetch(...)` for
   the two `MachineSessionRuntime` caches; `.ClearCache()` callsites
   unchanged. Verified: 298/298 `dotnet test` pass + live Orch1
   smoke (200 sessions across 77 machines). The separate P4 plan
   originally anticipated here is no longer needed.
2. **`KeyedSingleCachePerOrganization` for non-keyed Org cache
   variants.** If more PM entities surface that need keyed
   per-organization caching (single or list), the classes added in
   P3.0a become reusable infrastructure.
3. **`_dicPmBulkResolveByName` migration.** Done post-1.4.1 via a
   concrete (non-generic) `PmGroupMembersCache` class. The chunked
   bulk fetch (20 names per API call), the composite key shape
   `(partitionGlobalId, kind, name)`, the `PmGroupMember?` value
   type with explicit null entries for unresolved names (negative
   caching, confirmed against the API), and the chunk size are all
   fixed by this one endpoint; a generic class would expose unused
   flexibility for no real consumer. Storage is `static` so all
   drive instances in the same org share one cache (the org-scope
   correction the P3.0 work applied to `KeyedSingleCachePerOrganization`).
   The `PmBulkResolveByName<T>` wrapper on `OrchDriveInfo` remains
   for the generic `T` input + `unresolvedList` output ergonomics
   that PowerShell cmdlets expect; the cache class handles only the
   string-name → `PmGroupMember?` resolution + storage.

## Process / risk notes

- **Pre-commit `dotnet format --verify-no-changes`** is mandatory for
  every P3 commit to prevent the CI failure that hit `1ec5500` through
  `d5977c7` (fixed in `a3241f2`).
- **Continue observing P2.2 Process behavior change**: `GetReleaseById`
  shifted from "always-fetch with cache write" to "cache-then-fetch"
  in `7ec1ec3`. No reported issues yet; flag any staleness symptoms in
  scripts that update releases programmatically.
- **First tuple-TKey usage in P3.1**: P3.1 simultaneously introduces
  tuple-TKey to **two** cache classes — `KeyedSingleCachePerTenant`
  (link caches) and `KeyedSingleCachePerOrganization`
  (`PmBulkResolveByName`). Tuple TKey in `ExceptionsCachePer` and in
  `KeyedSingleCachePerFolder` is known-good (`7ec1ec3`). Smoke-test
  both code paths on P3.1 deployment.

- **Per-phase verification cadence**: after every P3.x commit
  (including each P3.0b single-cache commit), run the standard
  `dotnet build` → `dotnet test` → `dotnet format --verify-no-changes`
  → deploy + Orch1 smoke check loop before moving to the next phase.
  This mirrors the P2 cadence (deploy + verify between phases) which
  caught several issues early.
- **Sequential PM fetches in P3.0**: a global lock per cache class
  serializes API calls. In practice PM operations are infrequent
  enough that this is fine. Watch for any user-reported slowness in
  PM-heavy scripts after P3.0b deploys.

## Resolved questions

| Question                                                  | Resolution                                                                                                                                       |
| --------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------ |
| `_dicQueueItems` 3-level structure                        | **Flatten.** Per-queue grouping is a display-time concern.                                                                                       |
| `_dicJobsHavingExecutionMedia` cross-cache write to Jobs  | **Drop.** Same trade-off as other list/detail migrations.                                                                                        |
| `_dicTestCaseExecutions` filter-bypass                    | **Remove.** Replace with `IncrementalCachePerFolder` (always-fetch+accumulate), matching other `-First` cmdlets.                                |
| `TestSetExecutions` cache-then-fetch vs always-fetch      | **Always-fetch** (= `IncrementalCachePerFolder`, same as QueueItems / AuditLog). Folder can hold huge entities; status changes frequently.       |
| PM caches: tenant- or org-scoped?                         | **Org-scoped.** Past 3 caches re-migrated in P3.0b. Future PM caches must use the new org-scoped classes.                                       |
| PM cache concurrency                                      | **Single-threaded (global lock).** Optimize per-org parallelism only if profiling shows it matters.                                              |
