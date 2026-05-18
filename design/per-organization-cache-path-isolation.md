# Per-Organization Cache: Path Isolation (PSObject NoteProperty) — SUPERSEDED

> **SUPERSEDED / DO NOT IMPLEMENT.** The PSObject `WithPath` NoteProperty
> mechanism described below does NOT achieve per-drive Path isolation and
> was empirically disproven (2026-05-18). PowerShell keys PSObject
> *instance members* to **base-object identity**, not to the wrapper;
> org-scoped caches return the SAME singleton to every same-org drive, so
> N `WithPath` wrappers over that singleton share ONE `Path` note property
> (last-writer-wins). This doc's own acceptance test
> (`$x = Get-PmAuthenticationSetting -Path A:,B:` same org →
> `$x[0].Path -ne $x[1].Path`) FAILS. Minimal repro (no threads, no
> UiPathOrch): two `[psobject]::new($shared)` + `PSNoteProperty` → both
> report the last value; distinct base → OK.
>
> **Correct design (implemented, validated live):** no PSObject.
> `Path` / `PathGroupName` / `GroupName` are plain
> `[JsonIgnore(Always)]` properties on the DTO; the cache keeps serving
> the shared singleton and NEVER sets them; each cmdlet emit does
> `var c = e.ShallowClone(); c.Path = drive.NameColonSeparator; WriteObject(c);`
> (`ShallowClone()` = `(T)MemberwiseClone()` — a distinct CLR instance
> per emit makes `Path` independent; nested reference members stay shared
> with the singleton, preserving the shared-state intent; output is
> read-only). `OrchExtensions.WithPath` / `WithNoteProperty` were
> removed. `UpdateLicensedGroupResponse` is a non-shared per-call
> response → direct assignment, no clone.
>
> The historical design below is kept for context only.

## Problem

Organization-scoped caches (`SingleCachePerOrganization`,
`ListCachePerOrganization`) hold entities in a `static` dictionary keyed by
`partitionGlobalId` so that all `OrchDriveInfo` instances mapped to the same
organization see the same entity. This is the correct shared-state semantic:
if a user updates a license inventory via web or via another drive, every
drive observing the same org should see the change on next fetch.

The wart is the `Path` field. `Path` is a UiPathOrch-added field that records
which drive surfaced the entity (e.g., `"OrchTest:"`). It is `[JsonIgnore]` —
not part of the API payload — so the field has been treated as drive-local
metadata. But the storage is shared, and the cache's `Get` method runs the
configured `_initializer` (which sets `Path`) **on every hit, in place on the
shared instance**:

```csharp
// SingleCachePerOrganization.Get, cached path
else if (entity is not null && _initializer is not null)
{
    // Execute initializer even when cached (e.g., setting Path)
    _initializer(entity);
}
```

Consequences:

1. **Concurrent fetch race** (already documented as H1). Two drives in the
   same org fetching the entity in parallel may write conflicting `Path`
   values; the last writer wins for both readers. The current short-term
   mitigation is the contract comment at `OrchDriveInfo.cs:1261`:
   `// These should not be fetched in multi-threaded contexts. Path assignment will break.`
   All current `Pm*` cmdlets that touch these entries are written
   single-threaded (their old `OrchThreadPool.RunForEach` blocks are
   commented out) so the contract is honored in tree.

2. **Sequential overwrite still leaks** even with single-threaded cmdlets.
   `Get-OrchPmAuthenticationSetting -Path A:,B:` calls `WriteObject(entity)`
   for each drive in turn. PowerShell pipeline consumption is synchronous,
   so `| Format-Table Path` sees correct values per element. But the user
   collecting into a variable observes both array slots pointing at the
   same instance with the latest `Path`:

   ```powershell
   $x = Get-OrchPmAuthenticationSetting -Path A:,B:
   $x[0].Path  # "B:"  (was "A:" at WriteObject time)
   $x[1].Path  # "B:"
   ```

## Proposed solution

Use PowerShell's extended type system: keep the entity as the shared static
instance, but emit a fresh `PSObject` wrapper per drive at WriteObject time,
attaching `Path` as a `PSNoteProperty` on the wrapper rather than on the
entity itself.

```csharp
foreach (var drive in drives)
{
    var entity = drive.PmAuthenticationSetting.Get();  // shared
    if (entity is not null)
    {
        var pso = new PSObject(entity);
        pso.Properties.Add(new PSNoteProperty("Path", drive.NameColonSeparator));
        WriteObject(pso);
    }
}
```

Properties:

- The cached entity stays a single instance per org. Web-side updates remain
  visible to every drive on cache refresh, preserving the shared-state intent.
- Each `WriteObject` produces an independent `PSObject` wrapper carrying its
  own `Path` note property. `$x[0]` and `$x[1]` are different `PSObject`s.
- `$x[0].BaseObject` and `$x[1].BaseObject` are the same instance — that's
  the truth of the model and is the correct behavior for callers comparing
  org state across drives.
- Property access from PowerShell (`$x[0].Path`) resolves to the note
  property first, so per-drive context is preserved without mutating shared
  state. Internal C# code that wants the drive's path takes it from the
  drive directly, not from `entity.Path`.

## Migration plan

This is a meaningful refactor and is **deferred to the next release** after
the current B2B/cache-class release.

Affected surface:

### Entity types — drop the `Path` field

`OrchEntities.cs`:

- `PmAuthenticationRoot` (L4158-)
- `LicenseInventory` (L3817-)
- `AccountLicense` (L3867-)

And the list-cached entities (these need the same treatment if we extend the
PSObject wrap to `ListCachePerOrganization`):

- `PmUser`, `PmGroup`, `PmRobotAccount`, `ExternalClient`, `ExternalResource`,
  `AvailableUserBundle`, `TenantAllocation`, `NuLicensedGroup`,
  `NuLicensedUser`, `AccessAllowedMember`

### Cache classes — drop the `initializer` responsibility

- `SingleCachePerOrganization`: remove `Action<T>? initializer` ctor
  parameter, remove the cached-hit `_initializer(entity)` block.
- `ListCachePerOrganization`: same. The detailed-entry path
  (`_getterDetailed`) gets the same treatment.

### Cmdlets — wrap at WriteObject

3 single-entity cmdlets:

- `GetPmAuthenticationSetting.cs`
- `GetPmLicenseInventory.cs`
- `GetPmLicenseContract.cs`

~20 list-entity cmdlets. (See callers of `drive.PmUsers.Get()` etc.)

A small helper could simplify the wrap:

```csharp
public static PSObject WithPath<T>(this T entity, string path)
{
    var pso = new PSObject(entity);
    pso.Properties.Add(new PSNoteProperty("Path", path));
    return pso;
}
```

### Format definitions

`Format.ps1xml` (if it references the `Path` field by name) should keep
working — note properties resolve through the same `Properties` collection
PowerShell formatters consume.

### Test strategy

New tests (in `Tests/SelfContained.Tests.ps1` or a new file):

- `$x = Get-OrchPmAuthenticationSetting -Path A:,B:`,
  assert `$x[0].Path -ne $x[1].Path`.
- `$x[0].BaseObject` and `$x[1].BaseObject` are the same instance (verify
  the shared-state intent is preserved).
- Run the existing pipeline-style usage
  (`Get-OrchPmAuthenticationSetting | Format-Table`) and confirm no
  regression.

## Why not DeepCopy

A simpler-sounding fix is `DeepCopy` the cached entity inside `Get`. That
breaks the shared-state contract: a web-side update would only be observed
by the first drive to fetch after the cache invalidation; subsequent drives
would see their own copies until they reclear. The PSObject wrap keeps the
contract intact while still letting each output carry its own per-drive
metadata.

## Phase breakdown

To keep individual commits reviewable the refactor is split into three
phases:

- **Phase 1 — `SingleCachePerOrganization`** (3 entities, 3 cmdlets).
  Done in commit `bf579c3`. `LicenseInventory`, `AccountLicense`,
  `PmAuthenticationRoot` lost their `Path` field; `Get-PmLicenseInventory`,
  `Get-PmLicenseContract`, `Get-PmAuthenticationSetting` wrap their output
  in `PSObject` + `Path` `NoteProperty` via the new
  `OrchExtensions.WithPath<T>`.
- **Phase 2 — `ListCachePerOrganization`** (10 entities, ~25 cmdlets).
  Done. All 10 entities (PmUser, PmGroup, PmRobotAccount, ExternalClient,
  ExternalResource, AvailableUserBundle, TenantAllocation, NuLicensedGroup,
  NuLicensedUser, AccessAllowedMember) lost their `Path` field. Sub-entity
  `NuLicensedGroupMember` was pulled in too because it inherits from
  `NuLicensedUser`. `ListCachePerOrganization.Get` / `Get(id)` now run the
  initializer once at publish time (no cache-hit re-init). The 8
  `GetPSPath(this T)` extensions for the org-scoped types changed signature
  to `GetPSPath(this T, string drivePath)`; ~50 callers updated.
  `DriveScopedCompleter<TEntity>.GetTipHelp` also picked up an
  `OrchDriveInfo drive` parameter so PM entity completers can supply the
  drive path. Cmdlets wrap their `WriteObject` output via
  `entity.WithPath(drive.NameColonSeparator)`.
- **Phase 3 — `PmGroupMember` + `PmDirectoryEntityInfo` sub-entities**.
  Done. `PmGroupMember.Path` / `PathGroupName` dropped (`groupName` stays
  — parent-derived, drive-agnostic). `PmDirectoryEntityInfo.Path`
  dropped. The PmGroup initializer now only sets `m.groupName`;
  `SearchPmDirectoryCache` lost its initializer entirely. The five
  affected `GetPSPath` overloads (`PmGroupMember`,
  `PmDirectoryEntityInfo`, `DirectoryUser` / `DirectoryRobotUser` /
  `DirectoryApplication` — all PmGroupMember subclasses) take a
  `drivePath` parameter. `Get-PmGroupMember` attaches both `Path` and
  `PathGroupName` as PSObject NoteProperties so the format.ps1xml
  `GroupBy PathGroupName` renders the correct drive-local
  `Group: <Drive>:\<GroupName>` per emit. Resolves the
  `Get-PmGroupMember -Path Orch2:` showing `Group: Orch1:\…` regression
  reported during 1.4.0 dogfooding.

## Status

- Acknowledged: 2026-05-13
- Phase 1 shipped: commit `bf579c3`
- Phase 2 shipped: commit `061002e` + null-safety follow-up `9d1c02d`
- Phase 3 shipped: commit `162edca`
- Phase 4 (loose-ends cleanup) shipped: commit `cbacf2c`
  - `UpdateLicensedGroupResponse.Path` / `GroupName` dropped;
    `AddPmLicenseToPmLicensedGroup` and `RemovePmLicenseFromPmLicensedGroup`
    attach them as PSObject NoteProperty at WriteObject time. Not a race
    fix (Response wrapper, not a cache singleton); pure alignment with
    the rest of the org family.
  - `NuLicensedGroupMember.GroupName` / `PathGroupName` dropped.
    `GetPmLicensedGroupAllocations` is now a one-line passthrough;
    `Get-OrchPmLicensedGroup` attaches the labels via
    `.WithPath(...).WithNoteProperty("GroupName", ...).WithNoteProperty("PathGroupName", ...)`
    on the `-ExpandAllocation` and CSV paths.
  - `AvailableUserBundles.Path` / `GroupName` / `PathGroupName` dropped.
    The cache class initializer no longer attaches `Path`; the wrapper
    `GetPmUserLicenseGroupsAvailableLicenses` no longer mutates
    `GroupName` / `PathGroupName`. Existing callers only reach into
    `.availableUserBundles[]` and never read the wrapper-level fields,
    so no per-caller NoteProperty wrapping is needed.

- **2026-05-18 — Phases 1-4 SUPERSEDED.** Live testing on same-org
  multi-drive (`Orch1:`/`OrchTest:`/`Orch2:`, identical
  `partitionGlobalId`) showed every emitted object carrying the last
  drive's `Path` — the PSObject NoteProperty mechanism never delivered
  per-drive isolation (root cause in the SUPERSEDED banner at the top).
  Replaced by plain `[JsonIgnore]` DTO properties + per-emit
  `ShallowClone()` across all org-scoped Pm entities (~17 types) and
  their cmdlets (~24 sites); `WithPath` / `WithNoteProperty` deleted.
  Validated live (Path/PathGroupName/GroupName correct per drive) +
  298/298 unit + `dotnet format` clean. Pending review/commit at time
  of writing.

## Design principle

Per-call `Path` mutation is racy only for **organization-scoped** caches
— the static storage is shared across every `OrchDriveInfo` mapped to
the same org, so concurrent (or sequential, into-variable) emits from
different drives clobber each other. Per-tenant and per-folder caches
don't have this shape: a tenant cache lives on exactly one
`OrchDriveInfo`, a folder cache slice belongs to one folder on one
drive. For those, `entity.Path = drive.NameColonSeparator` (or
`folder.GetPSPath()`) directly on the entity is fine and stays in place
— the org-only treatment (originally the PSObject wrap; now the per-emit
`ShallowClone()` copy) is **not** applied below the organization level.
