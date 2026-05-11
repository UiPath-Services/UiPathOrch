# P2 plan — apply Calendar's structural pattern (cache split + sibling cmdlet + helper extract)

Status: Draft.
Reference implementation: Calendar (commits `7293117`, `8a74209`, `b82b647`).
Parent plan: `design/cmdlet-redesign-plan.md`.

## Scope

Apply the same structural pattern as the Calendar work to **three** Type A
entities: User, Process, Trigger. AuditLog deferred (see [Deferred /
out of scope](#deferred--out-of-scope)).

Each migration is two steps:

- **Cache split** — separate the list-shaped payload from the per-id
  detail payload. Each lives in its own cache class.
- **Cmdlet redesign** — introduce a sibling `Get-Orch<X>Detail` that
  owns the per-id path; deprecate the legacy `-Expand*` switch on the
  existing list cmdlet via a static helper delegate.

Calendar's CSV-deprecation specifically (`-ExportCsv` rerouted because
Calendar's CSV row shape mismatched its declared output type) does NOT
apply here. All three entities have CSV row shape == output type, so
their `-ExportCsv` stays supported with no deprecation. Only
`-ExpandDetails` is deprecated.

## Per-entity analysis

Each subsection covers (a) current cache layout, (b) proposed cache
classes, (c) external callsite count for clear-cache invalidations,
(d) cmdlet redesign, (e) what's tricky.

### User

**Current cache:**
- `_dicUsers`: `ConcurrentDictionary<long, User>` populated by a single
  bulk fetch (`OrchAPISession.GetUsers().ToDictionary(...)`).
- `UsersDetailed`: already migrated to
  `KeyedSingleCachePerTenant<long, User>` (commit `3748814`). The
  initializer there currently mirrors detailed entries back into
  `_dicUsers` — that mirror must be removed in P2.1 (the list cache
  becomes a separate type, and we are accepting a stale-list trade-off
  matching Calendar's design).

**Proposed:**
- `Users`: `ListCachePerTenant<User>` (replaces `_dicUsers`). Drops
  the dict-by-id internal lookup; no remaining consumers need it
  after the mirror is removed.
- `UsersDetailed`: stays as `KeyedSingleCachePerTenant<long, User>`,
  but its initializer's `_dicUsers[...] = detailedUser` write is
  deleted as part of the same commit.

**External callsites of `_dicUsers = null`** (7):
AddUser, CopyUser ×2, RemoveRoleFromUser, RemoveUser, UpdateUser,
CopyItem. (CopyAsset has two commented-out occurrences — leave
those alone.) Each becomes `Users.ClearCache()`.

**Cmdlets:**
- `Get-OrchUser` — keep. Default = list shallow. `-ExportCsv` keeps
  doing what it does (User CSV with detail enrichment via
  `UsersDetailed.Get(id)` per matched user). Output type remains
  `User` in both cases — no `-ExportCsv` deprecation.
- `Get-OrchUser -ExpandDetails` — deprecated; emits warning,
  delegates to `Get-OrchUserDetail`'s static helper.
- `Get-OrchUserDetail` (new):
  - `-UserName` mandatory; **wildcards accepted** (including `*`).
    Same explicit-fan-out philosophy as `Get-OrchCalendarDate -Name`.
  - `-FullName`, `-Type` are optional additional filters (mirror the
    parent cmdlet's filter set so users can pipe the same shape).
  - Output: `User` (detailed).
  - Reuses `TenantUserUserNameCompleter` for `-UserName`.

**Helper signature:** plain parameters (5-6 args: caller, drives,
userNameWildcards, fullNameWildcards, typeWildcards, writer). Not
worth packing into a struct yet; can refactor if a fourth migration
hits the same shape.

**Tricky:**
- The `_dicUsers` mirror in `UsersDetailed`'s initializer (commit
  `3748814`) MUST be removed in P2.1. If left in, the new `Users`
  list cache class won't see the writes (the mirror writes to a raw
  dict that no longer exists), causing a build error or silent stale
  data depending on how the migration is sequenced.

**Estimated commits:** 2.
1. Cache split: `_dicUsers` → `Users` (ListCachePerTenant);
   delete the `_dicUsers` mirror in `UsersDetailed`'s initializer;
   update 7 external callsites.
2. `Get-OrchUserDetail` cmdlet + helper extract +
   `-ExpandDetails` deprecation; psd1 export; help md (existing +
   new); test additions (smoke + R*-style deprecation warning test).
   PlatyPS rebuild done at end of this commit.

### Process (Release)

**Current cache:**
- `_dicReleases`:
  `ConcurrentDictionary<Int64, ConcurrentDictionary<Int64, Release>>`
  per-folder, dict-by-id within folder. Bulk fetch per folder.
- `_dicReleasesDetailed`: same shape. Per-folder per-id detail.

**Proposed:**
- `Releases`: `ListCachePerFolder<Release>` (drops dict-by-id within
  folder; same trade-off as Users).
- `ReleasesDetailed`: `KeyedSingleCachePerFolder<long, Release>`
  (the new class — see [New cache class](#new-cache-class-keyedsinglecacheperfolder)).

**External callsites:**
- `_dicReleases = null` (list-cache full clear): **0**.
- `_dicReleasesDetailed?.TryRemove(folder.Id ?? 0, out _)` (per-folder
  detail clear): **2** (UpdateProcess, CopyItem). Each becomes
  `ReleasesDetailed.ClearCache(folder)`.

**Cmdlets:**
- `Get-OrchProcess` — keep. Default = list. `-ExportCsv` kept
  (output type matches CSV row shape: Release).
- `Get-OrchProcess -ExpandDetails` — deprecated; emits warning,
  delegates.
- `Get-OrchProcessDetail` (new) — `-Name` mandatory, wildcards
  accepted; output `Release`. Naming uses `Detail` suffix
  (no existing `Add-OrchProcessDetail` / `Remove-OrchProcessDetail`
  to anchor a noun).

**Tricky:**
- The `&$expand=Environment,CurrentVersion,ReleaseVersions,EntryPoint`
  query string in `GetReleases` (line 884-891) is API-version-aware.
  Move into the `Releases` cache class's fetcher closure (which
  already has access to `OrchAPISession.ApiVersion`).
- `GetReleaseById` (line 939-942) mirrors detailed result back into
  `_dicReleases`. Drop that mirror after split.

**Estimated commits:** 2.
1. Cache split + 2 external callsites updated.
2. Cmdlet redesign + helper + psd1 + help + tests.

### Trigger (ProcessSchedule)

**Current cache:**
- `_dicTriggers`:
  `ConcurrentDictionary<Int64, ConcurrentDictionary<Int64, ProcessSchedule>>`
  per-folder, dict-by-id within folder.
- `_dicTriggersDetailed`: same shape. Per-folder per-id detail.

**Proposed:**
- `Triggers`: `ListCachePerFolder<ProcessSchedule>`.
- `TriggersDetailed`: `KeyedSingleCachePerFolder<long, ProcessSchedule>`.

**External callsites:**
- `_dicTriggers = null` (list-cache full clear): **0**.
- `_dicTriggersDetailed?.TryRemove(folder.Id ?? 0, out _)` plus
  paired `_dicTriggersDetailed_Exceptions.ClearCache()`: **9**
  occurrences across 5 cmdlets (CopyTrigger, EnableTriggerBase,
  NewTrigger, RemoveTrigger, UpdateTrigger). Each pair collapses
  into `TriggersDetailed.ClearCache(folder)`.

**Cmdlets:**
- `Get-OrchTrigger` — keep. Default = list. `-ExportCsv` kept (output
  type matches: ProcessSchedule).
- `Get-OrchTrigger -ExpandDetails` — deprecated; delegates.
- `Get-OrchTriggerDetail` (new) — `-Name` mandatory, wildcards
  accepted; output `ProcessSchedule`.

**Tricky:**
- `GetTrigger` also fetches `ExecutorRobots` (line 771-775) inside the
  detail path. Keep this enrichment in the static helper, not in the
  cache class itself: the cache class is "fetch one entity by id";
  the executor-robots side fetch is a follow-up enrichment and would
  pollute the cache class's contract.
- Same cross-cache write to `_dicTriggers` as Process — drop it.
- This is the largest external-callsite migration (9 vs Process's 2).

**Estimated commits:** 2.
1. Cache split + 9 external callsites updated.
2. Cmdlet redesign + helper + psd1 + help + tests.

## New cache class: `KeyedSingleCachePerFolder<TKey, TEntity>`

Required for Process and Trigger. Sibling of the existing
`KeyedSingleCachePerTenant<TKey, TEntity>` but folder-scoped.

Shape (mirrors `KeyedSingleCachePerTenant` and `SingleCachePerFolder`):

```csharp
public class KeyedSingleCachePerFolder<TKey, TEntity> : IFolderCacheClearable
    where TKey : notnull, IEquatable<TKey>
{
    private volatile ConcurrentDictionary<Int64, ConcurrentDictionary<TKey, TEntity?>>? _cache;
    private readonly ExceptionsCachePer<(Int64 folderId, TKey key)> _exceptions;

    public KeyedSingleCachePerFolder(
        OrchDriveInfo drive,
        Func<Int64, TKey, TEntity?> fetchFunc,
        Action<TEntity, string, TKey>? initializer = null,
        IEqualityComparer<TKey>? keyComparer = null,
        int? supportedApiVersionFrom = null);

    public TEntity? Get(Folder folder, TKey key);
    public void ClearCache(Folder folder, TKey key);
    public void ClearCache(Folder folder);  // all keys in folder
    public void ClearCache();               // all folders
}
```

**Patterns inherited from siblings:**
- Init-then-publish (no concurrent reader sees a partially initialized
  entity).
- Per-key exception caching using a tuple key `(folderId, TKey)`.
- `IFolderCacheClearable` registration via `_drive._allFolderCache.Add(this)`
  so a folder removal flushes its slice.
- `_supportedApiVersionFrom` gate.

**Why not reuse `KeyedSingleCachePerTenant` with composite key
`(folderId, TKey)`?**
1. The folder-scoped infra (`IFolderCacheClearable`,
   `_allFolderCache`) handles folder lifecycle properly. A
   tenant-scoped cache wouldn't get folder-removal sweeps.
2. The natural selector "drop everything for folder X" is a single
   call, not a `ClearCache(predicate)` scan.

**Where to introduce the class:** Bundle into the same commit as
P2.2 (Process) — its first user. A standalone class commit with no
consumer adds boilerplate without exercise; bundling means the
class lands tested.

## Phasing

| Phase | Content                                                                                                              |
| ----- | -------------------------------------------------------------------------------------------------------------------- |
| P2.1  | User cache split + cmdlet redesign + help + tests. No new class; tenant-scoped only.                                 |
| P2.2  | Add `KeyedSingleCachePerFolder` + Process cache split + cmdlet redesign + help + tests.                              |
| P2.3  | Trigger cache split + cmdlet redesign + help + tests. (Reuses class added in P2.2.)                                  |

Each P2.x is roughly the same shape (~2-3 commits) as Calendar's
`7293117`–`b82b647` chain. Sequence is User → Process → Trigger
because: User has no new class to introduce; Process introduces the
new class with the simpler external callsite footprint (2 vs 9);
Trigger applies the same to the larger callsite set.

## Test scope per entity

Calendar's pattern was 3 tests: (a) smoke for the new cmdlet,
(b) deprecation warning test for `-Expand*`, (c) CSV round-trip
equivalence between legacy `-ExportCsv` and canonical
`Get-OrchCalendarDate -ExportCsv`.

For User / Process / Trigger, test (c) does NOT apply — `-ExportCsv`
on the legacy cmdlet is not deprecated and there's no second path to
compare against. So **2 tests per entity**:
- `Get-Orch<X>Detail -<Selector> '*'` smoke (does not throw).
- `Get-Orch<X> -ExpandDetails` emits a deprecation warning naming
  `Get-Orch<X>Detail` and still returns data.

## PlatyPS rebuild

Each phase's "help" sub-step modifies markdown sources under
`docs/help/en-US/`. PlatyPS rebuild (`PlatyPS\Build-Help.ps1
-SkipDeploy`) runs once at the end of each phase, just before commit
of the cmdlet-redesign chunk. The XML output in `Staging/en-US/` is
gitignored and gets deployed by the user's own deploy step.

## Completers

New `Get-Orch<X>Detail` cmdlets reuse the parent cmdlet's existing
completers (e.g. `TenantUserUserNameCompleter`,
`ProcessNameCompleter`, `TriggerNameCompleter`). No completer changes
are needed in this phase. If a completer's logic is tied to a
specific cmdlet's parameter binding it can stay private; otherwise
elevate to internal in a small separate commit.

## Deferred / out of scope

**AuditLog.** The split-rationale that drove Calendar/User/Process/
Trigger doesn't fit AuditLog:

- Single existing cache (`_dicAuditLogs` →
  `IncrementalCachePerTenant<long, AuditLog>` is a clean 1:1 swap,
  no list/detail dual-cache to disentangle).
- `-ExpandDetails` doesn't change output type; it just fetches each
  log's `.Details` and mutates the entity in place.
- The "discoverability" + "mandatory selector to prevent N+1"
  argument still applies, but the cache simplification half of the
  argument doesn't.

So AuditLog gets at most a **cache-only** migration (drop-in
`IncrementalCachePerTenant`) with no cmdlet redesign for now. The
sibling-cmdlet decision is reopened separately.

**Open question deferred with AuditLog:** If we eventually do introduce
`Get-OrchAuditLogDetail`, the mandatory selector is unobvious — there
is no name; candidates are `-Id <long[]>` (precise) or re-running the
original query (`-Query`, `-Skip`, `-First`). Decide alongside the
sibling-cmdlet question.
