# Cmdlet redesign plan: `-Expand*` switch decomposition

Status: Draft, accepted-direction.
Target release for Phase 1: 1.3.0.

## Problem

A subset of `Get-*` cmdlets accepts an `-Expand*` switch that fundamentally
changes the cmdlet's behavior, output type, or both. The pattern has caused
real bugs (most recently `Get-OrchCalendar -ExpandExcludedDate` returning
zero rows because the underlying cache could not reliably distinguish
list-shaped from detail-shaped payloads) and adds avoidable complexity to
the cache layer.

## Affected cmdlets (survey)

Three patterns coexist:

### Type A — list endpoint + per-id detail endpoint
The `-Expand*` switch triggers a per-entity detail call after a bulk list.
List and detail return different payload shapes. The cache layer sees both
shapes for the same entity type, which has been the source of bugs.

| Cmdlet                  | Switch                  |
| ----------------------- | ----------------------- |
| `Get-OrchCalendar`      | `-ExpandExcludedDate`   |
| `Get-OrchUser`          | `-ExpandDetails`        |
| `Get-OrchProcess`       | `-ExpandDetails`        |
| `Get-OrchTrigger`       | `-ExpandDetails`        |
| `Get-OrchAuditLog`      | `-ExpandDetails`        |

### Type B — parent + related child entities
The switch expands a related entity inline. Whether redesign is warranted
depends on the entity. Out of scope for the initial pass.

| Cmdlet                  | Switch              |
| ----------------------- | ------------------- |
| `Get-OrchAuditLog`      | `-ExpandEntity`     |
| `Get-OrchMachine`       | `-ExpandRobotUser`  |
| `Get-OrchRole`          | `-ExpandPermission` |
| `Set-OrchRole`          | `-ExpandPermission` |
| `Get-PmLicensedGroup`   | `-ExpandAllocation` |
| `Get-PmRobotAccount`    | `-ExpandGroup`      |

### Type C — value transformation only
No extra API call. The switch flattens a nested value into row form. No
cache impact, low payoff to redesign. Out of scope.

| Cmdlet                  | Switch               |
| ----------------------- | -------------------- |
| `Get-OrchAsset`         | `-ExpandUserValues`  |
| `Get-OrchCredentialAsset` | `-ExpandUserValues` |
| `Get-OrchSecretAsset`   | `-ExpandUserValues`  |

## Design principles

1. **One cmdlet, one responsibility, one output type.** A switch should not
   change the output shape of a cmdlet.
2. **Cache layout follows cmdlet layout.** If two cmdlets exist for list and
   detail, they own separate caches. The cache layer never has to decide
   "is this entry shallow or detailed?"
3. **Pipe-friendly.** `Get-OrchCalendar -Name X | Get-OrchExcludedDate` is
   the natural shape; the new cmdlet must accept the parent entity over the
   pipeline by property name.
4. **API call discipline.** Detail-fetching cmdlets must not make it easy
   to accidentally fan out to dozens or hundreds of API calls. Make the
   selector mandatory (see below).

## Phase 1 scope (1.3.0)

For each Type A cmdlet, introduce a new sibling cmdlet that owns the
detail path. The `-Expand*` switch on the existing cmdlet stays, but
delegates to the new cmdlet's implementation and emits a deprecation
warning. No removal in 1.3.0.

| Existing                                          | New (1.3.0)              | Output               |
| ------------------------------------------------- | ------------------------ | -------------------- |
| `Get-OrchCalendar [-ExpandExcludedDate]`          | `Get-OrchCalendarDate`   | `ExcludedDateNamed`  |
| `Get-OrchUser [-ExpandDetails]`                   | `Get-OrchUserDetail`     | `User` (detailed)    |
| `Get-OrchProcess [-ExpandDetails]`                | `Get-OrchProcessDetail`  | `Release` (detailed) |
| `Get-OrchTrigger [-ExpandDetails]`                | `Get-OrchTriggerDetail`  | (detailed)           |
| `Get-OrchAuditLog [-ExpandDetails]`               | `Get-OrchAuditLogDetail` | (detailed)           |

Naming convention:

1. If the entity already has `Add-Orch<X>` and/or `Remove-Orch<X>`
   sibling cmdlets, the new Get cmdlet uses the same noun
   (`Get-OrchCalendarDate` pairs with `Add-OrchCalendarDate`,
   `Remove-OrchCalendarDate`). This keeps the verb triple discoverable
   and gives users a natural mental model: "the noun is the thing,
   the verb is the operation."
2. Otherwise, use `Get-Orch<Entity>Detail`
   (`Get-OrchUserDetail` etc.) since there is no sibling to anchor on.

### Mandatory selector on new cmdlets

To prevent casual large-fan-out usage:

- Make `-Name` (or the equivalent selector) **mandatory** on each new
  detail cmdlet.
- Wildcards remain accepted, so `*` still gives the all-entities case
  but the user has to type it explicitly. The friction is the point —
  it makes the cost of the operation visible.

## Backward compatibility

- The existing `-Expand*` switch stays functional in 1.3.0 and beyond.
- When invoked, the existing cmdlet emits a `Write-Warning` describing
  the new cmdlet to use, then delegates to the new cmdlet's
  implementation (no behavior change for the user).
- Removal is deferred to a future major release. The exact target
  version is intentionally not pinned in the deprecation message — to
  be decided based on usage telemetry / Slack feedback after a real
  deprecation period.

### Code dedup via static helper extraction

The new cmdlet owns the canonical implementation as an `internal static`
helper. The legacy `-Expand*` branch on the old cmdlet calls the same
helper. Result: zero duplication; the helper has exactly one definition.

The Calendar reference implementation (commit `8a74209`) follows this
pattern. See `UiPathOrch/OrchestratorCmdlet/CalendarCmdlet/GetCalendarDate.cs`
for the canonical helper, and `GetCalendar.cs` for the deprecated-path
delegate. The same shape applies to User / Process / Trigger / AuditLog.

### `-ExportCsv` on the legacy cmdlet

Type A list cmdlets that historically wrote a "detail-shaped" CSV (e.g.
`Get-OrchCalendar -ExportCsv` emits per-date rows so the file
round-trips with `Add-OrchCalendarDate`) follow the same deprecation
treatment as `-Expand*`: the `-ExportCsv` path on the legacy cmdlet
emits a deprecation warning pointing users at the new cmdlet's
`-ExportCsv`. The CSV format is preserved so existing exported files
still import.

For cmdlets where `-ExportCsv` doesn't change the output type (e.g.
`Get-OrchUser`'s CSV is just User rows enriched with detail), it stays
supported on the legacy cmdlet without deprecation, and the new
`Get-Orch<Entity>Detail -ExportCsv` shares the same writer via the
helper. Decide per-entity whether the CSV is "detail-shaped" or
"list-shaped"; deprecate the legacy `-ExportCsv` only when the cmdlet's
declared output type doesn't match the CSV's row shape.

When the legacy switch is finally removed, the change is mechanical: drop
the `if (ExpandExcludedDate.IsPresent)` branch and the parameter
declaration. The helper stays.

## Cache implications

This plan validates work already in progress: the Calendar cache has
been split into `Calendars` (list) + `CalendarsDetailed` (detail) in
commit `7293117`. The same split should be applied to User, Process,
Trigger, and AuditLog as part of the broader cache migration effort
(separate from this cmdlet plan, but proceeding in lockstep).

## Phasing

| Phase | Content                                                                                    |
| ----- | ------------------------------------------------------------------------------------------ |
| P0    | Calendar cache split (done — `7293117`)                                                    |
| P1    | This plan, plus Calendar reference implementation: `Get-OrchCalendarDate` + helper extract (done — `8a74209`) |
| P2    | Apply same pattern to User, Process, Trigger, AuditLog                                     |
| P3    | Type B / Type C re-evaluation (separate planning)                                          |
| P4    | Removal of `-Expand*` switches (next major; timing TBD)                                    |

## Open questions / out of scope

- Type B cmdlets: case-by-case judgment needed. Some `-Expand*` flags
  (e.g. `-ExpandPermission` on `Get-OrchRole`) may be the natural
  "complete view" of the parent and not warrant decomposition. Defer to
  a separate planning document.
- Type C cmdlets: no cache impact, low ROI. Defer indefinitely.
- Telemetry: no usage telemetry exists today. Removal timing will rely
  on Slack feedback during the deprecation period.
