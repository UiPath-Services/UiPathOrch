---
title: Logs, Jobs & Queues
nav_order: 6
permalink: /logs-jobs-queues/
---

# Querying Logs, Jobs, Queues & Other High-Volume Data Efficiently

- [Why this matters](#why-this-matters)
- [What these cmdlets are](#what-these-cmdlets-are)
- [The mental model: an accumulating cache](#the-mental-model-an-accumulating-cache)
- [Which parameters trigger a server query?](#which-parameters-trigger-a-server-query)
- [The key behavior: no filter = dump the whole cache](#the-key-behavior-no-filter--dump-the-whole-cache)
- [Re-fetching overwrites the cache](#re-fetching-overwrites-the-cache)
- [Filtering the cached entities efficiently](#filtering-the-cached-entities-efficiently)
- [Per-tenant vs per-folder scope](#per-tenant-vs-per-folder-scope)
- [Resetting the cache](#resetting-the-cache)
- [Gotchas](#gotchas)
- [Cmdlet reference table](#cmdlet-reference-table)

Most `Get-Orch*` cmdlets download **all** entities in scope on first call and
serve every later call from a complete local snapshot (see
[Performance & Cache Management](02-Essentials.md#performance--cache-management)).
A folder-entity cmdlet caches every entity in the target folder; a tenant-entity
cmdlet caches every entity in the target tenant — in both cases the cache is the
*complete* set for that scope.

A second family behaves differently. These cmdlets sit in front of
high-volume, server-side-filtered endpoints (jobs, logs, audit logs, queue
items, …) where downloading *everything* is never an option. They are backed by
an **incremental (accumulating) cache**: instead of downloading everything in
scope, they fetch only the slice you ask for and accumulate those slices
locally. This guide explains how to drive them efficiently.

## Why this matters

A query against Orchestrator is a network round-trip over potentially millions
of rows; reading the local cache is an in-memory dictionary lookup. **Reusing
the cache is orders of magnitude faster and puts essentially zero load on the
server.** The whole point of this cmdlet family is to let you:

1. Pull a bounded slice from the server **once** with a filter, then
2. Inspect, reshape, sort, and re-filter that slice **offline, for free**, as
   many times as you like.

The efficient habit is: **fetch narrow, then work the cache.** Every time you
re-run a filtered query just to `Where-Object` the result a different way, you
are paying for a server round-trip you already have the data for. Avoid that.

## What these cmdlets are

| Cmdlet | Cache scope |
|---|---|
| `Get-OrchJob` | per-folder |
| `Get-OrchQueueItem` | per-folder |
| `Get-OrchLog` | per-folder (robot logs) |
| `Get-OrchJobMedia` | per-folder |
| `Get-OrchTestCaseExecution` | per-folder |
| `Get-OrchTestCaseAssertion` | per-folder |
| `Get-OrchTestSetExecution` | per-folder |
| `Get-OrchAuditLog` | per-tenant |
| `Get-PmAuditLog` | per-tenant |
| `Get-OrchAlert` | per-tenant |
| `Get-OrchMachineSession` | per-tenant / per-folder |
| `Get-OrchUnattendedSession` | per-tenant / per-folder |
| `Get-OrchUserSession` | per-tenant |

If you are unsure whether a given cmdlet is in this family, the tell is in its
help: the DESCRIPTION says *"If no filter parameters are specified, the cmdlet
outputs the cached … contents and displays a warning."*

## The mental model: an accumulating cache

A normal cached cmdlet holds a **complete** set. An incremental-cache cmdlet
holds **only what you have already asked for**, and it *grows* that set with
every filtered query:

1. You call the cmdlet **with a query parameter** (`-Last`, a time range, a
   state, etc.).
2. The cmdlet builds an OData query from those parameters and asks the server
   for exactly that slice, paged by `-Skip` / `-First`.
3. Each returned entity is **merged into a dictionary keyed by id** (per folder
   for folder-scoped caches, per tenant otherwise). See
   [Re-fetching overwrites the cache](#re-fetching-overwrites-the-cache).
4. The freshly fetched batch is returned to the pipeline.

So the cache is **append-only within a session** — it never shrinks and is
never auto-refreshed. It contains the union of every filtered query you have run
so far, not a live snapshot of the server.

```powershell
# Each filtered call hits the server AND adds to the local cache
Get-OrchJob -Path Orch1:\Shared -Last 1d        # fetches + caches yesterday's jobs
Get-OrchJob -Path Orch1:\Shared -State Faulted  # fetches + caches faulted jobs
# The cache now holds the union of both result sets.
```

## Which parameters trigger a server query?

This is the distinction that decides whether a call costs a round-trip or is
free. Parameters fall into three groups.

**Query parameters — build the OData `$filter`/paging; presence forces a server
fetch (and the result is merged into the cache).** For `Get-OrchJob` these are:
`-Last`, `-CreationTimeAfter` / `-CreationTimeBefore`, `-StartTimeAfter` /
`-StartTimeBefore`, `-EndTimeAfter` / `-EndTimeBefore`, `-ResumeTimeAfter` /
`-ResumeTimeBefore`, `-Priority`, `-ReleaseName`, `-ProcessType`, `-SourceType`,
`-Robot`, `-State`, and the paging pair `-Skip` / `-First`. For `Get-OrchAuditLog`:
`-Last`, `-Component`, `-UserName`, `-Action`, `-ExecutionTimeAfter` /
`-ExecutionTimeBefore`, plus `-Skip` / `-First`. Each cmdlet's help lists its own
set under the **Filter** parameter set.

> Note that `-Skip` / `-First` **count as query parameters** — they change the
> server query window, so specifying either one triggers a fetch rather than a
> cache dump.

**Non-query parameters — do NOT, on their own, trigger a server query:**

- `-Path` — selects the target drive / folder(s).
- `-Recurse` — widens folder scope, but still only over folders you query.
- `-OrderBy` / `-OrderAscending` — sort the **cache** output; they do not fetch.
- Output-shaping switches such as `-ExpandEntity` (reshapes what is emitted).
  (`-ExpandDetails` is special: it makes *extra* per-entry detail calls when you
  are already fetching — it is not a `$filter` term.)

**The rule:** if you supply **at least one query parameter**, the cmdlet queries
Orchestrator and merges the result into the cache. If you supply **none**, it
skips the server entirely and dumps the cache (next section).

## The key behavior: no filter = dump the whole cache

**This is the single most important point.** When you run one of these cmdlets
with **no query parameter at all** (only non-query parameters like `-Path` or
`-OrderBy`), it does **not** call Orchestrator. Instead it **outputs the entire
accumulated cache** and writes a warning:

> `[Get-OrchJob] Since no filter parameters were specified, the contents of the
> cache will be output. To query the Orchestrator, please specify at least one
> filter parameter.`

```powershell
# No query parameter → no server call → returns everything fetched so far
Get-OrchJob -Path Orch1:\Shared
Get-OrchJob -Path Orch1:\Shared -OrderBy State   # still cache-only, just sorted
```

Consequences:

- **A no-filter call is free and offline.** It is the cheapest way to re-examine
  data you have already pulled.
- **A no-filter call on a cold cache returns nothing** (just the warning). The
  cache starts empty; run at least one filtered query to populate it.
- **It never reflects new server-side data.** To pick up changes on the server,
  run a *filtered* query again.

## Re-fetching overwrites the cache

The cache is keyed **by entity id**. When a filtered query returns an entity
whose id is already cached, the cached copy is **overwritten in place** with the
freshly fetched one — it is updated, not duplicated. (Where a cmdlet enriches
entities with extra fields, a merge step carries those cached fields forward onto
the refreshed entity, so you don't lose enrichment.)

That is exactly how you **refresh** stale data: re-run a filter that covers the
ids you care about, and their cache entries are replaced with current server
state. The cache only ever *grows in id-count*, but the *content* of any id is
always the most recently fetched version.

```powershell
# Job 12345 is Running when first pulled
Get-OrchJob -Path Orch1:\Shared -Last 1h

# ...time passes; re-query the same window
Get-OrchJob -Path Orch1:\Shared -Last 1h
# Job 12345's cached entry is now overwritten with its latest state (e.g. Successful).
# No duplicate row is created — same id, updated value.
```

> `Get-OrchLog` is a variation on the same idea: the API returns `Log.Id == 0`
> for every robot-log entry, so it cannot key by id. Instead it deduplicates **by
> value** — a row is treated as already-cached when every field matches — so
> repeating an overlapping `Get-OrchLog` query does **not** add duplicate rows.

## Filtering the cached entities efficiently

Once a filtered query has populated the cache, do all further shaping against
the **no-filter** output with ordinary PowerShell — no more server calls:

```powershell
# 1) ONE server round-trip, scoped by time (results cached)
Get-OrchJob -Path Orch1:\Shared\Production -Last 7d | Out-Null

# 2) Now slice the cache as many ways as you want — all offline, all instant
Get-OrchJob -Path Orch1:\Shared\Production | Where-Object State -eq Running
Get-OrchJob -Path Orch1:\Shared\Production | Where-Object { $_.State -eq 'Faulted' } |
    Select-Object Path, ReleaseName, StartTime, Info
Get-OrchJob -Path Orch1:\Shared\Production | Group-Object ReleaseName |
    Sort-Object Count -Descending
Get-OrchJob -Path Orch1:\Shared\Production | Where-Object EndTime -gt (Get-Date).AddHours(-2)
```

The same pattern applies to the other cmdlets:

```powershell
# Audit logs: pull a month once, then mine the cache locally
Get-OrchAuditLog -Last Month | Out-Null
Get-OrchAuditLog | Where-Object Action -eq Delete | Group-Object UserName
Get-OrchAuditLog | Where-Object { $_.Component -eq 'Robots' -and $_.UserName -like '*tsuda*' }

# Queue items: pull failures once, then categorize offline
Get-OrchQueueItem -Path Orch1:\Finance -Status Failed | Out-Null
Get-OrchQueueItem -Path Orch1:\Finance | Group-Object ProcessingException.Reason
```

Tips:

- Prefer `Get-Orch* | Where-Object …` over re-querying with a narrower server
  filter, **once the data is already in the cache** — it's faster and load-free.
- Use `-OrderBy` / `-OrderAscending` on the no-filter call to sort the cache
  without a `Sort-Object` pass (supported by `Get-OrchJob`, `Get-OrchAuditLog`).
- `-Id` reads (or, for some cmdlets, fetches) specific entries. For
  `Get-OrchAuditLog` `-Id` is **cache-only** (no server call) — populate the
  cache with a filtered query first; for `Get-OrchJob` `-Id` looks up each id,
  fetching from the server when needed. `-Id` supports tab completion and
  wildcards against cached ids.

## Per-tenant vs per-folder scope

The cache key differs by cmdlet, and it changes what "the whole cache" means:

- **Per-tenant** caches (`Get-OrchAuditLog`, `Get-OrchAlert`, `Get-PmAuditLog`,
  user sessions): one accumulating set per drive. A no-filter call dumps the
  whole tenant-level cache.
- **Per-folder** caches (`Get-OrchJob`, `Get-OrchQueueItem`, `Get-OrchLog`,
  `Get-OrchJobMedia`, test executions): the cache is partitioned **by folder**.
  A no-filter call dumps the cache **for the targeted folder(s) only**. When you
  fetch with `-Recurse`, each folder accumulates its own slice independently, so
  a later no-filter `-Recurse` call returns the union of the folders you have
  already visited.

```powershell
# Per-folder: each folder gets its own accumulated slice
Get-OrchQueueItem -Path Orch1:\ -Recurse -Status Failed | Out-Null
Get-OrchQueueItem -Path Orch1:\Finance   # only Finance's cached items
```

## Resetting the cache

Because the cache only grows and never expires within a session, clear it when
you want a clean slate or after an error. `Clear-OrchCache` is scope-aware, so
you don't have to flush everything:

```powershell
Clear-OrchCache                      # current drive: all caches (tenant + every folder)
Clear-OrchCache -Path .              # only the current folder's cache (resolves your current location)
Clear-OrchCache -Path Orch1:\Shared  # one specific folder's cache; -Recurse / -Depth extend to subfolders
Clear-OrchCache -Path Orch1:\        # tenant-scoped caches only; per-folder caches left intact
Clear-OrchCache -AllDrives           # every authenticated drive
```

Because the volume cmdlets' caches are **per folder**
([scope](#per-tenant-vs-per-folder-scope)), targeting a single folder is the
efficient way to refresh just the data you're working on. When you're `cd`'d
into a folder, `Clear-OrchCache -Path .` drops only that folder's accumulated
jobs / queue items / logs and leaves every other folder — and the tenant-level
caches — untouched, so a follow-up filtered query re-fetches only what you
cleared. (At the drive root, `-Path .` resolves to the tenant scope instead.)

Run a filtered query afterward to repopulate. As a general rule
([Essentials](02-Essentials.md#critical-ai-execution-rules)), always
`Clear-OrchCache` before retrying after an error.

## Gotchas

- **Empty no-filter output is not "no data on the server"** — it means you
  haven't fetched anything yet this session. Run a filtered query first.
- **The cache can be stale.** It is a session-local accumulation, not a live
  view. If freshness matters, re-run with a filter — the result overwrites the
  matching cached entries (see
  [Re-fetching overwrites the cache](#re-fetching-overwrites-the-cache)).
- **`Get-OrchLog` deduplicates by value, not by id.** The API returns
  `Log.Id == 0` for every robot-log entry, so `Get-OrchLog` can't key by id;
  instead it collapses rows whose fields all match. Overlapping queries therefore
  don't inflate the cache. (The flip side: two genuinely distinct events with
  identical fields count as one cached row — rare in practice.)
- **`-Id` / cache-only reads return nothing on a cold cache** (e.g.
  `Get-OrchAuditLog -Id`). They never trigger a fetch.
- **Don't expect a no-filter call to be a "get everything from the server"
  shortcut.** It is the opposite of the standard cmdlets — it returns *only what
  you already pulled*, offline.

## Cmdlet reference table

| Cmdlet | Filtered call (query param present) | No-filter call |
|---|---|---|
| `Get-OrchJob` | server query (paged), merged/overwritten by id into per-folder cache | dumps cached jobs for the folder, sortable, no server call |
| `Get-OrchQueueItem` | server query, merged by id into per-folder cache | dumps cached items for the folder |
| `Get-OrchLog` | server query, value-deduplicated into per-folder set (`Log.Id == 0`, so keyed by value) | dumps cached logs for the folder |
| `Get-OrchAuditLog` | server query, merged by id into per-tenant cache; `-Id` reads cache | dumps cached audit logs, newest first |
| `Get-PmAuditLog` | server query, merged into per-tenant cache | dumps cached PM audit logs |
| `Get-OrchAlert` | server query, merged into per-tenant cache | dumps cached alerts |
| `Get-OrchJobMedia` | server query, merged into per-folder cache | dumps cached media for the folder |
| `Get-OrchTestCaseExecution` / `Get-OrchTestSetExecution` / `Get-OrchTestCaseAssertion` | server query, merged into per-folder cache | dumps cached executions for the folder |
| `Get-OrchMachineSession` / `Get-OrchUnattendedSession` / `Get-OrchUserSession` | server query, merged into cache | dumps cached sessions |

---

See also: [Essential Guide for AI](02-Essentials.md) ·
[Cmdlet Reference](04-CmdletReference.md) ·
[Troubleshooting](90-Troubleshooting.md)
