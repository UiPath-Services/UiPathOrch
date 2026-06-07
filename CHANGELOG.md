# Changelog

All notable changes to UiPathOrch are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

_Nothing yet._

## [1.8.1] - 2026-06-06

### Changed

- **`Get-OrchLicense` now prints a readable summary by default.** Instead of dumping the
  raw `Allowed` / `Used` dictionaries and epoch dates, the default view shows
  `Licensed until`, `Subscription`, and a per-license-type **Usage** table
  (`Used / Allowed (Percent)`) — the same "Used of Allowed (%)" the Orchestrator license
  page shows per runtime type. Backing this are new helper properties: `Usage`
  (a `LicenseUsage[]` of `Type` / `Used` / `Allowed` / `Percent`, one row per type with
  `Allowed > 0`) and `ExpireDateLocal` / `GracePeriodEndDateLocal` (the epoch dates as
  `DateTime`). The raw `Allowed` / `Used` / `ExpireDate` fields are unchanged.
- **`Get-OrchLicenseRuntime`'s default table now mirrors the tenant license page.**
  Columns keep their property names except where the *value* is genuinely transformed to
  match the *License: &lt;type&gt;* screen, which then carry the web label — `Template`
  (the machine's `MachineName`, shown only for template machines) and `Robots` (`N/A` for
  template machines, otherwise `RobotsCount`). `IsLicensed` keeps its name but renders as
  a colored dot (green = licensed, orange = not). Columns auto-size. Previously the wide
  raw `MachineName` value pushed the rest of the columns off-screen.
- **`New-Item` on a Document Understanding drive emits the canonical project path.** The
  created project is now returned with its drive-qualified `FullName`, matching the
  provider's `Get-ChildItem` / `Get-Item` output.

### Fixed

- **`Get-OrchJobMedia` / `Export-OrchJobMedia` and `Get-OrchTestCaseAssertion` now use the
  shared HTTP pipeline.** The job-media and assertion-screenshot downloads were bypassing
  the central send chokepoint, so they missed silent token refresh on a 401, transient
  (429 / 503 / 504) retry with `Retry-After` backoff, and Ctrl+C cancellation. They now
  route through the same path as every other cmdlet.
- **Test Manager listings now page correctly.** The Test Manager paginated reads did not
  advance their offset, so a list longer than one page re-fetched the first page (returning
  duplicates and missing the later entries), and one non-paginated path could loop. They
  now advance per page and stop on the final page, returning complete, de-duplicated
  results.
- **An external-storage 401 during bucket file upload/download no longer drops your
  Orchestrator session.** Blob I/O now surfaces the storage error on its own instead of
  treating a storage 401 as an Orchestrator auth failure, which had cleared the cached
  token.
- **`Copy-OrchQueueItem` now rate-limits every batch, not just the first.** The 601 ms
  pause was measured from a single timestamp taken before the loop, so only the first
  100-item batch was delayed; large queues could trip the API rate limit. It is now
  reset per cycle. The output is unchanged — the successfully-copied source items,
  ready to pipe into `Remove-OrchQueueItem` for a copy-then-delete "move" (each queue
  item is a transaction, so an item moved to another queue/tenant must be removed from
  the source to avoid double-processing).
- **`Import-OrchQueueItem`'s output is now accurately documented.** The help claimed
  "no output on success", but the cmdlet returns the `BulkOperationResponse` (check
  `.Success`) on a successful add, `FailedQueueItem` objects for rejected rows, and
  `CSVParseError` objects when the CSV can't be parsed. The OUTPUTS section and
  `[OutputType]` now declare all three. No behavior change.
- **`New-OrchApiTrigger` / `Update-OrchApiTrigger` now work below ApiVersion 20.** The
  create/update payload always carried `RunAsCaller`, a recent Cloud-only HttpTrigger
  field absent from the on-prem swagger through v20. Strict servers reject the unknown
  field with `httpTrigger must not be null` (HTTP 400 — the body fails to deserialize), so
  API-trigger creation failed on on-prem Orchestrator (verified on 25.10.2 / ApiVersion
  17). `RunAsCaller` is now stripped below ApiVersion 20 — mirroring the existing
  `ProcessSchedule` version-field handling — while Cloud (ApiVersion 20+) still sends it.

## [1.8.0] - 2026-06-05

### Added

- **`-LiteralPath` on every folder-/drive-scoped cmdlet.** A literal
  (non-wildcard) counterpart to `-Path`, carrying `[Alias("PSPath")]` so it
  binds from the automatic `PSPath` note-property that `Get-ChildItem` /
  `Get-Item` attach to every item. `dir Orch1:\ -Recurse | Update-OrchProcess *
  -WhatIf` now targets each folder's own path with no `-Path`, and
  `dir | Select-Object PSPath | Export-Csv` round-trips through
  `Import-Csv | <cmdlet> -LiteralPath`. The provider-qualified PSPath form
  (`UiPathOrch\UiPathOrch::Orch1:\X`) is accepted by the path resolvers.

### Changed

- **BREAKING: the `Path` property has been removed from folder (`dir`) items.**
  A folder previously carried a `Path` that returned its *parent* directory;
  folders now follow the FileSystem-provider convention — a `dir` item exposes
  `FullName` (its own path) plus the automatic `PSPath`, and no `Path`. Scripts
  that read `(dir Orch1:\...).Path` (which returned the parent) should use
  `FullName` / `PSPath` for the folder's own path, or `Split-Path` for the
  parent. Content entities (assets, queues, jobs, …) are unaffected — their
  `Path` still names the containing folder.
- **BREAKING: `Get-OrchProductVersion` now returns camelCase property names**
  (matching the API), is cached per organization rather than per drive, and
  reports timestamps in local time. Scripts reading the previous PascalCase
  property names must update.
- **`New-PmRobotAccount` / `Set-PmRobotAccount`: `-Name` is now the primary
  parameter** (with `-UserName` kept as an alias), so a robot account is
  addressed by the same `-Name` as the rest of the cmdlet family and a
  `Get | Set` object pipe round-trips.
- **CSV multi-value columns now round-trip by a consistent rule.** A column that
  `-ExportCsv` writes as a single comma-joined cell (list attributes such as
  roles, group memberships, ExecutorRobots) is split back on import; an
  identity/selector column (Name, UserName, Path, …) is taken as a single value.
  Commas and wildcard metacharacters inside a value are escaped on export and
  restored on import, so they survive the round-trip.
- **`Remove-OrchMachine` and `Remove-OrchTestCase` delete in bulk.** Wildcard-,
  comma-, and CSV-specified targets are coalesced into one bulk-delete API call
  per folder instead of one call per item.
- **`-WhatIf` / `-Confirm` prompts read consistently** — ShouldProcess action
  strings are normalized to the `Verb Noun` convention across the module.

### Fixed

- **`Get-Item` on a folder now returns a drive-qualified `PSPath`.** It
  previously built `PSPath` from the fully-qualified name without the drive
  prefix (`UiPathOrch\UiPathOrch::\Autopilot`), so `Get-Item … | <cmdlet>
  -LiteralPath` lost the drive. It now matches `Get-ChildItem`
  (`…::Orch1:\Autopilot`).
- **`Get-Item` / `Get-ChildItem` on DU and TM projects now stamp the project's
  own drive-qualified path.** They previously emitted the bare shared cache
  object, so `(Get-Item Du1:\proj).FullName` could be empty or leak another
  drive's last value; each emit is now a per-drive stamped clone.
- **`Get-OrchLog`'s cache no longer accumulates duplicate rows when overlapping
  logs are fetched more than once.** Robot-log entries all arrive with `Id == 0`
  from the server, so the per-folder cache deduplicates by *value* — `Log`
  overrides `Equals`/`GetHashCode` across every field. A previous thread-safety
  change had swapped the backing `HashSet<Log>` for a `ConcurrentBag<Log>`,
  which silently dropped that value-dedup: re-running an overlapping
  `Get-OrchLog` query (or paging the same window again) re-appended the same
  rows, inflating the accumulated set that a no-filter `Get-OrchLog` prints. The
  set-backed store is restored, so identical re-fetches collapse to a single
  copy. The race the swap guarded against is not reachable for this cache — the
  only writer is `Get-OrchLog`, which fetches folders sequentially, and no
  completer or parallel path writes it.
- **Names containing a single quote no longer break OData queries** (the
  "O'Brien" problem). `$filter` string literals are single-quote-escaped and
  URL-encoded, and OData literals in function-import keys and `robotType` are
  escaped as well.
- **`New-PmRobotAccount` / `Set-PmRobotAccount` with several comma-separated
  names now create or update every one**, not just the first.
- **A group name or robot name containing a comma or wildcard metacharacter now
  round-trips** through `New-PmUser` and `New-`/`Set-PmRobotAccount`
  `-GroupName` / `-ExecutorRobots` CSV export and import; `UnescapeBackticks` no
  longer drops an escaped backtick.
- **The internal API-version value is published atomically**, avoiding a torn
  read under parallel requests.
- Hardened `Enable-`/`Disable-OrchLicenseRuntime` key escaping and the bucket
  client's HTTP initialization.

## [1.7.3] - 2026-06-04

### Changed

- **Identity lookups (Pm users / groups / robot accounts, …) now make about half the requests.**
  The three internal pagination loops were unified into one; the identity enumerator now stops
  on a short page like the others instead of always fetching an extra empty page, so a typical
  (small) identity result is one request instead of two. The shared loop also derives the page
  size from the request rather than a hardcoded literal.
- **Ctrl+C now interrupts an in-flight request.** A long or stalled HTTP call (pagination,
  a copy, a raw `Invoke-OrchApi`) could previously be interrupted only between calls; the
  single HTTP path now cancels the in-flight send — and any retry backoff — on Ctrl+C, so
  a hung endpoint no longer blocks for up to the request timeout.
- **Transient failures and expired tokens now recover automatically.** The single HTTP
  path retries `429` / `503` / `504` with capped exponential backoff (honoring a server
  `Retry-After`), and on a `401` it re-authenticates and retries once, so a token that
  expired or was rotated mid-operation recovers without surfacing an error. If a freshly
  issued token is still rejected — a genuinely broken credential — an auth circuit breaker
  trips so the rest of the operation fails fast instead of re-authenticating and retrying
  per item; it resets on `Import-OrchConfig`.
- **`Copy-Item -Recurse -WhatIf` now previews every subfolder, not just the top one.**
  A recursive `-WhatIf` used to print a single `Copy Folder` line for the folder you
  named; it now walks the whole source tree and prints one line per folder, each naming
  where that folder would be copied (e.g. `Item: 'Orch1:\A\Sub' Destination:
  'Orch1:\Shared\A'`). The actual copy and a declined `-Confirm` are unaffected.
- **A root-to-root `copy` reports tenant-entity counts and warns when folders are skipped.**
  `copy Source:\ Destination:\` now shows how many of each tenant-level entity would be
  copied — e.g. `Copy Library` on `Item: 'Source:\* (43)'` instead of a bare `*` — visible
  under `-WhatIf` / `-Confirm`. And because a root copy without `-Recurse` copies no folders,
  it now warns (`Copying tenant-level entities only. N folder(s) and their entities are not
  copied without -Recurse.`) so the omission isn't mistaken for an empty tenant.
- **Copying a personal workspace with no counterpart in the destination now warns with
  guidance instead of erroring.** A recursive root copy (`copy Source:\ Destination:\
  -Recurse`) used to emit an error for each source personal workspace the destination
  hadn't opened yet — including under `-WhatIf`, where erroring during a preview is
  wrong. It now writes a warning explaining that a personal workspace must be opened
  ("start exploring") in the Orchestrator web UI on **both** tenants (the API cannot do
  this) before it can be copied, then continues with the rest of the copy.

### Fixed

- **`ConvertTo-Json` of a tag nested beyond `-Depth` no longer serializes it as a string.**
  `Tag` overrode `ToString()` to return `"DisplayName=DisplayValue"`, which `ConvertTo-Json`
  emitted verbatim for any tag past the `-Depth` cut (instead of the structured object / the
  usual truncation marker). The override is removed; the `DisplayName=DisplayValue` form used
  by tag columns in `-ExportCsv` is preserved via an explicit formatter.
- **A `401` mid-session no longer wedges the cache against a recoverable token.** When an
  access token expired or was rotated server-side, the resulting `401 Unauthorized` was
  cached as a "deterministic" failure, so every later read on that cache slot re-threw the
  stale `401` *before* the session could re-authenticate — leaving the drive stuck until
  `Clear-OrchCache` / `Import-OrchConfig`. A `401` is now treated as recoverable (never
  cached), so the next call re-authenticates with a fresh token and continues.
- **`Copy-Item -WhatIf` / `-Confirm` now names the destination when copying a bucket.**
  The bucket copy's `ShouldProcess` message listed only the source
  (`Copy Bucket` on `Orch1:\Shared\MyBucket`), unlike every other entity, which shows
  `Item: '<source>' Destination: '<destination>'`. Buckets now use the same shape, so
  a `-WhatIf` / `-Confirm` line says where the bucket would be copied to. Affects
  `Copy-OrchBucket` and bucket copies done by `copy -Recurse`.

## [1.7.2] - 2026-06-03

### Added

- **`New-PmRobotAccount`** — creates a robot account, erroring if one with the
  name already exists (strict create, like `New-Item`); the existing-account
  counterpart of the create-or-update `Set-PmRobotAccount`. It shares all
  parameters and the create path with `Set-PmRobotAccount` (only the
  already-exists case differs). `-GroupName` sets the new account's initial
  groups, so `Get-PmRobotAccount -ExportCsv` round-trips through it. To add or
  remove individual group memberships of an *existing* robot account, prefer
  `Add-`/`Remove-PmGroupMember -Type DirectoryRobotUser` (additive).
- **`Get-`/`Set-`/`Copy-PmUserPreference`** — read, write and migrate **your own**
  organization-level portal preferences (theme, language, ...) via the identity
  `Setting` API as generic `-Key` / `-Value` pairs. Preferences are personal, so the
  cmdlets always act on the connected user (there is no `-UserName`); they require a
  non-confidential application or PAT and skip — with an error — any drive connected
  with a confidential application (which has no user). `Get-PmUserPreference -ExportCsv`
  writes `Path,Key,Value`, which round-trips through `Import-Csv | Set-PmUserPreference`
  (rows for the same drive are coalesced into one request). `Copy-PmUserPreference
  -Destination` copies your settings to yourself in another organization (the identity
  user is resolved per organization). `-Key` tab-completes the known portal keys, and
  `-Value` adapts to the chosen `-Key` (e.g. `UserLanguage.Language` lists
  `ja`, `en`, … with friendly labels); completions insert the raw value, so the
  friendly names never reach the command or CSV. Reads are cached per
  organization and key (invalidated when `Set-`/`Copy-PmUserPreference` writes that
  key). The output groups under a per-user header (`User: <drive>\<user>`); the
  `UserLanguage.Date` timestamp is shown as a local date/time for readability, while
  the stored value stays the raw string so CSV round-trips.
- **`Get-`/`Set-`/`Copy-PmNotificationSubscription`** — read, change and migrate **your own**
  notification subscriptions (which events notify you, and by which delivery mode)
  via the notification service. `Get-PmNotificationSubscription` lists one row per
  `(topic, mode)` — publisher, topic group, name, `Mode` (`InApp` / `Email`) and
  whether you're subscribed — grouped by publisher; `-Publisher` / `-Mode` filter and
  `-IncludeHidden` reveals non-visible topics. `Get-PmNotificationSubscription -ExportCsv`
  writes `Path,Publisher,Group,Topic,DisplayName,Mode,IsSubscribed`, which round-trips
  through `Import-Csv | Set-PmNotificationSubscription`. `Set-PmNotificationSubscription
  -Topic <name|guid> -Mode <InApp|Email> -Subscribed <true|false>` toggles a subscription
  (topic names are resolved per drive); rows for the same drive are coalesced into one
  request, so `Get-… | … | Set-…` round-trips. `Copy-PmNotificationSubscription
  -Destination` migrates your subscriptions to yourself in another organization, matching
  topics by name (their GUIDs differ per org) and skipping topics/modes a destination
  doesn't offer and mandatory topics. Self-only: the service uses the token's user, so
  there is no `-UserName`. Automation Cloud only.

### Changed

- **`Copy-OrchTestDataQueue` (and `copy -Recurse` of test data queues) now copies
  items whose data predates a schema change.** The destination queue is created with
  its schema's top-level `required` list removed so items that omit a now-required
  field are accepted, then the original schema is restored once the items are uploaded —
  the destination ends up identical to the source (required enforced for new items,
  legacy items intact). Items are also uploaded in batches with a per-item fallback:
  when a bulk batch is rejected for a content-schema violation (`409 Conflict`) each
  item is retried individually so one bad row is reported on its own instead of
  failing the whole queue; any other failure (auth / not-found / transient) is
  reported once and stops the copy instead of retrying every row. Large queues no
  longer risk a single over-size request.
- **`Import-OrchTestDataQueueItem` now uploads in batches with a per-item fallback.**
  Previously every row was POSTed in a single bulk call, so one malformed row failed
  the whole file. Now a batch rejected for a content-schema violation (`409 Conflict`)
  is retried one item at a time — the good rows still land and only the bad rows are
  reported — while any other failure (auth / not-found / transient) is reported once
  and stops the import rather than flooding the caller (same strategy as
  `Copy-OrchTestDataQueue`).
- **The browser sign-in ("Connected") page lists the Document Understanding / Test
  Manager drives too.** When `Import-OrchConfig` mounts the `…Du:` / `…Tm:` shadow
  drives alongside an Orchestrator drive (i.e. when the drive's scope includes Du. /
  TM. scopes), the page now shows them as well — e.g. `Mounted as Orch1:, Orch1Du:,
  Orch1Tm:` — instead of just the Orchestrator drive.

## [1.7.1] - 2026-06-02

### Changed

- **`-MachineRobots` matches robot / machine / session names exactly
  (case-insensitive) rather than as wildcards.** A trigger binding is a precise
  assignment, and a robot user name is domain-qualified (`domain\user`) where
  the `\` was previously mis-read as a wildcard escape — so the
  serialized/displayed value didn't round-trip back in. List multiple bindings
  as explicit array elements instead of a `*` pattern. Affects
  `New-`/`Update-OrchTrigger` and `New-`/`Update-OrchApiTrigger`.
- **`Copy-Orch*` `-Recurse` now mirrors the source tree, creating missing
  destination subfolders on demand** instead of erroring
  `… does not exist` and skipping them. Created folders are plain modern
  folders with no package feed. The destination root must still exist, and
  folders **directly under the tenant root are not auto-created** — a
  top-level folder's package-feed setting can't be inferred from the source,
  so create those explicitly. `-WhatIf` previews each `New Folder` plus the
  copy into it. Affects `Copy-OrchActionCatalog`, `Copy-OrchApiTrigger`,
  `Copy-OrchAsset`, `Copy-OrchBucket`, `Copy-OrchFolderMachine`,
  `Copy-OrchFolderUser`, `Copy-OrchProcess`, `Copy-OrchQueue`,
  `Copy-OrchQueueItem`, `Copy-OrchTestDataQueue`, `Copy-OrchTestSet`,
  `Copy-OrchTestSetSchedule`, and `Copy-OrchTrigger`. (`Copy-OrchPackage` /
  `Copy-OrchLibrary` are excluded — packages and libraries have restricted
  placement.)

### Deprecated

- **`Set-PmRobotAccount`'s `GroupName0`–`GroupName9` parameters / CSV columns
  are deprecated** in favour of a single comma-separated `GroupName` column.
  `Get-PmRobotAccount -ExportCsv` now writes that one `GroupName` column (and is
  no longer capped at 10 groups per robot); `Import-Csv | Set-PmRobotAccount`
  reads it through the existing `-GroupName` parameter. CSVs produced by older
  versions (the numbered columns) still import, now with a one-time deprecation
  warning — re-export to migrate.

### Fixed

- **`New-`/`Update-OrchTrigger` and `New-`/`Update-OrchApiTrigger`
  `-MachineRobots` now resolve robot accounts and modern folder-user-bound
  robots**, not just users with a classic Unattended Robot configured. The resolver matched the supplied user name only
  against `User.UnattendedRobot.UserName` and read the RobotId from there, so a
  robot account — or any robot whose identity lives on `RobotProvision` or the
  account login — failed to resolve: the trigger was saved with the MachineId
  but no RobotId, and a `… is not configured as Unattended Robot` warning was
  emitted. It now matches the same merged robot view the read side already uses
  (`Robot.Username` = `UnattendedRobot.UserName ?? RobotProvision.UserName`,
  with a fallback to the account login `Robot.User.UserName`) and takes the
  RobotId from the matched robot, so the binding is sent in full. All four
  cmdlets share one resolver, so all are fixed. `-MachineRobots` tab-completion
  is now consistent across them — `New-*` list the folder's available robots
  (including robot accounts and modern robots, previously omitted) and
  `Update-*` show the trigger's current binding. `Get-OrchTrigger` /
  `Get-OrchApiTrigger` now render each MachineRobots entry in the same one-line
  JSON shape that `-MachineRobots` accepts, so a displayed binding pastes
  straight back.
- **`Add-OrchUser` now succeeds against older Orchestrator (e.g. 22.10, which
  reports API version 15).** The `POST /odata/Users` body omitted `UpdatePolicy`
  (which older Orchestrator requires) and always sent
  `NotificationSubscription.Export` (added in API version 16, so absent on older
  servers — which strict-bind and reject the unknown field); either caused the
  request to be rejected with a bare `The input was not valid.` It now defaults
  `UpdatePolicy` to `{ Type = "None" }` when the caller specifies no update policy
  (only at API version &gt;= 15, where `UserDto` has the field), and drops
  `Export` at API version &lt; 16 — version boundaries confirmed against the
  per-version `UserNotificationSubscription` / `UserDto` swagger schemas. The
  existing `RateLimitsDaily` / `RateLimitsRealTime` stripping (added in API
  version 18) is unchanged.

### Performance

- **`Copy-Orch*` resolves the destination calendar from the cached calendar
  list** instead of re-fetching the whole tenant calendar list once per copied
  trigger (cross-tenant copies of triggers bound to a calendar).

## [1.7.0] - 2026-06-01

**Breaking change.** Renames the Platform Management license **operation**
cmdlets to a uniform `<Verb>-Pm{Group,User}License` shape, so a
`Get-Pm{Group,User}License -ExportCsv` round-trips into the matching
`Add-Pm{Group,User}License`. No aliases are kept — the old names are gone;
update any scripts that use them.

### Changed

| Old name | New name |
|---|---|
| `Get-PmLicensedGroup` | `Get-PmGroupLicense` |
| `Add-PmLicenseToPmLicensedGroup` | `Add-PmGroupLicense` |
| `Remove-PmLicenseFromPmLicensedGroup` | `Remove-PmGroupLicense` |
| `Remove-PmAllocationFromPmLicensedGroup` | `Remove-PmGroupLicenseAllocation` |
| `Get-PmLicensedUser` | `Get-PmUserLicense` |
| `Add-PmLicenseToPmLicensedUser` | `Add-PmUserLicense` |
| `Remove-PmLicenseFromPmLicensedUser` | `Remove-PmUserLicense` |

The entity-delete cmdlets `Remove-PmLicensedGroup` and `Remove-PmLicensedUser`
(which drop a group/user from the licensed set entirely) and the read-only
`Get-PmLicense`, `Get-PmLicenseAllocation`, `Get-PmLicenseInventory`, and
`Get-PmLicenseContract` reference cmdlets are unchanged. Cmdlet behavior,
parameters, and output are otherwise identical — only the names changed. The
`FullyQualifiedErrorId` values these cmdlets emit on error were also aligned to
the new names (e.g. `AddPmUserLicenseError`).

## [1.6.2] - 2026-06-01

Adds `Move-Orch{Asset,Bucket,Queue}` for relocating an entity between folders
in one drive, and `Get-PmLicensedUser -ExportCsv` to complete the licensed-user
CSV round trip. Plus ShouldProcess wording cleanup and a CSV documentation pass.

### Added

- **`Move-OrchAsset` / `Move-OrchBucket` / `Move-OrchQueue`** — relocate an
  entity from one folder to another within the same tenant drive. An
  asset/bucket/queue is a single tenant-level entity surfaced into folders via
  the share endpoint; the move issues one atomic `ShareToFolders` request
  (`toAdd=[destination]`, `toRemove=[source]`) so the entity leaves the source
  and stays a first-class entity in the destination — keeping its Id and its
  data (asset value, queue items, bucket Identifier/storage). It is a true move
  of the one entity, not a copy. Same-drive only (a cross-drive destination is
  an error pointing at `Copy-Orch*`). `-Destination` is a single folder (a
  wildcard must expand to exactly one). With `-Recurse` the source tree is
  mirrored under the destination — an entity in a subfolder lands in the
  matching subfolder (created on demand as a plain modern folder, no package
  feed), not flattened into the destination root — and `-WhatIf` previews the
  folder creations and the moves in execution order.
- **`Get-PmLicensedUser -ExportCsv` / `-CsvEncoding`** — export the licensed
  users as one row per (user, license) with `Path` / `UserName` / `License`
  columns, round-tripping into `Add-PmLicenseToPmLicensedUser`
  (`Import-Csv ... | Add-PmLicenseToPmLicensedUser`). The `UserName` column
  binds to the Add cmdlet's `-Email` parameter via its `-UserName` alias; the
  License Accountant API returns an empty email and carries the login in the
  name field, so the column is `UserName`. Orphaned license rows (licenses not
  tied to a directory user) are excluded, since they can't be reassigned on
  import. This mirrors the `Get-PmLicensedGroup -ExportCsv` round trip.

### Changed

- **ShouldProcess wording uses plain verbs, no glyphs.** `Move-Orch*` prints
  `Move <Entity> to <folder>` (not `→`), and `Add-Orch*Link` /
  `Remove-Orch*Link` print `Add/Remove <Link> to/from <folder>` (was
  `→` / `✗`). These were the only cmdlets using arrow glyphs in their
  `-WhatIf` / `-Confirm` text; they now match the plain-verb convention used
  everywhere else, including the existing `Move-OrchFolderUser`. The link
  cmdlets' help `-WhatIf` examples were corrected to match.

### Docs

- **CSV Export & Import guide** clarifies that importing is **additive** —
  none of the import verbs treat the CSV as the desired full state, so removing
  a row never deletes the corresponding entity (use the matching `Remove-`
  cmdlet). Added a **Bulk Delete via CSV** section (a cmdlet needs no
  `-ExportCsv` of its own to join a CSV workflow) and a **snapshot / restore**
  example (record enabled triggers, disable all, re-enable only those).
- **`Get-PmLicensedUser` help** documents `-ExportCsv`; **`Get-PmLicensedGroup`
  help** was corrected to describe the shipped `-ExportCsv` (group-level
  `Path`/`GroupName`/`License` rows round-tripping into
  `Add-PmLicenseToPmLicensedGroup`) instead of the pre-1.6.1 allocation CSV.
- `Enable-OrchTrigger` / `Disable-OrchTrigger` help now links to the CSV guide.

### Tests

- Integration Pester for `Move-Orch*` (relocate, value/items/Identifier
  preserved, `-WhatIf`, same-folder no-op, `-Recurse` mirroring) and for
  `Get-PmLicensedUser -ExportCsv` (columns, orphan exclusion, round trip);
  xUnit shape tests for the Move cmdlets.

## [1.6.1] - 2026-05-31

A round-trip and resolution fix for the Platform Management license-group
cmdlets, plus a stack of hardening fixes from a code-quality pass.

### Fixed

- **`Get-PmLicensedGroup -ExportCsv` → `Add-PmLicenseToPmLicensedGroup`
  round trip.** The export previously emitted member-oriented rows that
  bound to no `Add-PmLicenseToPmLicensedGroup` parameter, so the CSV could
  not be re-imported to assign licenses. It now emits one row per
  *(group, license)* with `Path` / `GroupName` / `License` columns matching
  the cmdlet's parameters (`License` is the friendly bundle name).
  **This changes the exported CSV layout** from the prior member columns.
- **`Add-PmLicenseToPmLicensedGroup` group and license resolution.** Group
  names were matched by directory-search prefix, so a request for `GroupA`
  could also hit `GroupA2`; they are now matched exactly (wildcards still
  honored). License names were matched against an API field that is empty on
  the live server, so a friendly name silently resolved to nothing; they are
  now resolved through the bundle catalog (friendly name *or* raw code).
  Pipeline rows are aggregated by group name (case-insensitive) so multiple
  rows for one group merge into a single atomic-replace update instead of
  clobbering each other.
- **Argument completers no longer leak exceptions into `<Tab>`.** A transient
  auth / HTTP failure (or a cached error) during tab completion previously
  surfaced as a raw error in the prompt; completion now degrades to "no
  matches" while still propagating cancellation.
- **Token refresh no longer pins a stale token.** A token endpoint returning
  `200` without an `access_token` would advance the expiry behind an
  unchanged Bearer header; the session now fails / forces re-authentication.
- **HTTP error-response bodies are capped in exception messages** (8 KB), so
  a large non-JSON error payload can no longer flood `Start-Transcript` /
  CI logs.
- **`Get-Orch{Asset,Bucket,Queue}Link` survives an unexpected per-item
  failure.** Orchestrator-side errors were already reported per item and the
  run continued, but a non-`OrchException` fault while draining one result
  (e.g. an `IOException` from writing a CSV row) would abort the whole
  cmdlet; such faults now become a non-terminating error for that item and
  the remaining items still process. `Ctrl+C` cancellation still stops the
  run promptly.
- **`Enable-/Disable-OrchEventTrigger` reported a copy-pasted error id.** A
  drive-level failure surfaced with `FullyQualifiedErrorId` `GetApiTriggerError`
  (left over from the API-trigger cmdlet); it now correctly reports
  `GetEventTriggerError`.

### Changed

- **`Add-PmLicenseToPmLicensedGroup` `-License` tab completion** now lists
  only licenses the group does **not** already hold. For multiple groups
  (wildcard or comma-separated `-GroupName`) it offers the union of each
  group's available licenses minus only those held by *every* named group.
- **`Set-OrchLocation` raises proper error records when the module can't be
  resolved.** A missing module (`Set-OrchLocation NoSuchModule`) or an
  ambiguous wildcard match used to surface a bare `InvalidOperationException`;
  it now throws a terminating `ErrorRecord` with a specific id
  (`ModuleNotFound` / `AmbiguousModuleName`) and category, so the failure is
  catchable and self-explanatory.

## [1.6.0] - 2026-05-30

Adds the `PmLicensed*User` cmdlet family, an `Add-OrchFolderUser
-Domain` escape hatch for EntraID-federated OnPrem tenants, and CSV
import for queue / test data queue items. Tighter per-cmdlet warning
scoping plus a stack of asset / auth / Copy-Orch* fixes.

### Added

- **`Add-PmLicenseToPmLicensedUser`** — allocate one or more user-bundle
  licenses to a Platform Management user. Atomic-replace PUT against the
  License Accountant API; the cmdlet merges with the user's existing
  bundles and submits one request per user per drive, so multiple
  `-License` values for the same user fold into a single round trip.
- **`Remove-PmLicenseFromPmLicensedUser`** — strip one or more bundles from
  a Platform Management user. Mirror of the Add cmdlet using the same PUT
  endpoint with the user's current bundle set minus the matched codes;
  `-License *` empties the bundle list (the row remains as "No license").
- **`Remove-PmLicensedUser`** — drop the licensed-user row entirely,
  including the empty-bundle rows left behind by
  `Remove-PmLicenseFromPmLicensedUser <user> *`. Batched DELETE per drive
  (N users = 1 round trip).
- **`Add-OrchFolderUser -Domain <name>`** — opt-in override for the
  directory Search call's `domain=` parameter. Required for EntraID-
  federated OnPrem tenants where the default `autogen` is rejected with
  "An unknown failure has occurred" (the web UI's Domain dropdown supplies
  values like `frc`, `root`). Affects both Robot resolution and the
  AssignDomainUser payload; safe to omit on Automation Cloud and
  non-federated OnPrem (behavior is byte-for-byte identical to v1.5.3).
- **CSV import for `Import-OrchQueueItem` and `Import-OrchTestDataQueueItem`**
  — load queue / test data queue items from a CSV file, matching the
  Orchestrator web "Upload Items" behavior: web-parity 15,000-row cap and a
  shared multi-line CSV parser (quoted fields may span lines). Test data
  queue values are coerced per the queue's schema (integer/number
  invariant-parsed, strings preserved).

### Fixed

- **`Add-OrchFolderUser` routed to the wrong endpoint on OnPrem.** The
  cmdlet was choosing the OData operation by `ApiVersion`, but
  `AssignDirectoryUser` is Cloud-only — OnPrem 22.10 / 23.4 / 25.10 all
  expect `AssignDomainUser` regardless of API version. Route by `IsCloud`
  instead; honor the directory's `domain` value (where the lookup returns
  one) in the assignment payload.
- **`Get-OrchAsset -ExportCredentialCsv` corrupted output for values
  containing commas / quotes / newlines.** Username, store, and free-form
  fields were emitted without CSV escaping, so round-trips through
  `Import-Csv | Set-OrchCredentialAsset` could shift values across
  columns. Every field now goes through `EscapeCsvValue`; covered by
  `CredentialCsvRoundTripTests`.
- **`Set-OrchAsset` invalid `-ValueType` error didn't explain positional
  binding.** Users hitting "ValueType 'foo' is invalid" couldn't tell why
  their first positional argument was being read as `-ValueType` — the
  error now spells out the positional order
  (`Set-OrchAsset <ValueType> <Name>`) and points at `-Name` if the user
  meant an asset name.
- **`Invoke-OrchApi -Body` failed on PSObject inputs.** Piping
  `ConvertTo-Json` output (a PSObject-wrapped string) directly tripped
  `System.Text.Json` object-cycle detection. The cmdlet now unwraps to
  `.BaseObject` before serialization.
- **`Copy-Orch*` emitted mismatched `FullyQualifiedErrorId` strings.** The
  error IDs didn't match the cmdlet they came from, so scripts filtering
  on `ErrorRecord.FullyQualifiedErrorId` couldn't trap them reliably.
  Fixed across `Copy-OrchQueueItem`, `CopyTestSet`, `CopyTestSetSchedule`,
  `CopyTrigger`.
- **`PathInfoComparer` violated the `Equals` / `GetHashCode` contract.**
  Equal instances could return different hash codes, breaking hash-based
  collections; now derives both from the same canonical form and guards
  against null.
- **Token expiry hard-coded 1h instead of honoring `expires_in`.** The
  IdP-reported lifetime is now parsed (JSON number or quoted-numeric
  string) and used directly; 1h fallback kept for PAT / user-password
  flows that don't report a lifetime. Prevents a too-late refresh / 401
  on Automation Suite / OnPrem identities with shorter policies.
- **PKCE listener hung indefinitely when the browser landed on an
  Identity error page.** Added a 3-minute backstop so CI / automation
  contexts can't deadlock on a never-arriving redirect; surfaces an
  actionable terminating error pointing at `Resolve-OrchAuthError`.
- **Discover `ApiVersion` from the `api-supported-versions` response
  header.** Replaces the post-auth `GetActivitySettings` round trip with
  a free read from any API response. Settings-based detection stays as
  defense in depth (guarded by `ApiVersion is null`, so the two paths
  converge).
- **Cmdlets emitted unrelated drives' PendingWarning.**
  `OrchestratorPSCmdlet.BeginProcessing` walked every registered drive,
  so a routine cmdlet on `Orch1:` could surface
  `WARNING: ... 'local:\'` lines that the user wasn't operating on.
  Filter to drives the cmdlet actually targets (current location +
  bound `-Path` / `-Destination`) via a virtual `GetTargetDriveNames()`
  hook with a smart default covering the two dominant conventions.

### Docs

- **Help md for the three `PmLicensed*User` cmdlets** added.
- **`Remove-Orch*` documentation pass**: `-Path` pipeline binding,
  no-prompt default, `-WhatIf` preview-then-re-run workflow, and CSV
  round-trip examples for Library / Package / QueueItem / BucketItem.
  Factual corrections to `Remove-OrchQueue`, `Remove-OrchTask`,
  `Remove-OrchQueueItem`, and the three `Remove-Orch*Link` cmdlets'
  `-WhatIf` operation strings.

## [1.5.3] - 2026-05-21

Adds `New-OrchWebhook` and `Get-OrchTestSetDetail`, extends `-ExportCsv`
to more `Get-Orch*` cmdlets, batches `Add-Orch*Link`, trims
`New-/Update-OrchApiTrigger` to the parameters the web form uses, and
unifies the three link output types into one.

### Breaking

- **`Get-Orch{Asset,Bucket,Queue}Link` now output one `EntityLink`** (was
  `AssetLink`/`BucketLink`/`QueueLink`); the per-type id field is unified to
  `Id`. CSV columns (`Path`/`Name`/`Link`) are unchanged, so CSV
  round-trips still work — only scripts using the old type names or a
  per-type id (`$x.AssetId`) need updating.

### Added

- **`New-OrchWebhook`** — the missing New- counterpart to
  `Update-OrchWebhook` (POST `/odata/Webhooks`).
- **`New-/Update-OrchWebhook -Events`** — subscribe to specific event types
  (`string[]`, wildcards, expanded against `Get-OrchWebhookEventType`);
  round-trips through a new `Events` CSV column.
- **`Get-OrchTestSetDetail`** — per-item GET returning populated
  `Packages[]` / `TestCases[]`, so `Get-OrchTestSetDetail X | New-OrchTestSet`
  clones the contents (the LIST `Get-OrchTestSet` returns empty arrays).
- **`-ExportCsv` / `-CsvEncoding`** on `Get-OrchWebhook`,
  `Get-Orch{Asset,Bucket,Queue}Link`, `Get-OrchTestDataQueue`, and
  `Get-OrchActionCatalog` — columns match the corresponding write cmdlet so
  `Import-Csv | New-/Add-` round-trips.

### Changed

- **`Add-Orch{Asset,Bucket,Queue}Link` batch by entity** — one
  `ShareToFolders` call per (source folder, entity) instead of one per CSV
  row. The call is all-or-nothing: if any target folder is denied
  (errorCode 1017) nothing is added for that entity, and the cmdlet names
  the entity and folders so the offending CSV row can be fixed.

### Fixed

- **`New-/Update-OrchApiTrigger -CallingMode`** now lists `LongPolling`
  (was only `AsyncRequestReply` / `FireAndForget`).
- **`OrchException`** surfaces readable text from bulk
  (`result.errors[].description`, e.g. "Username 'x' is already taken.") and
  ABP (`error.details` string) error envelopes instead of dumping raw JSON.
- **`-ExportCsv` / `-CsvEncoding` help** added where `Get-Help` listed
  neither (ActionCatalog, TestDataQueue, Webhook, the link cmdlets).

### Removed

- **`New-/Update-OrchApiTrigger` parameters trimmed to the web "API
  trigger" form's set.** Dropped the callback group (`CallbackMode`,
  `SuccessCallbackUrl`, `FailureCallbackUrl`, `Secret`, `AllowInsecureSsl` —
  `CallbackMode` is read-only, the rest inert for API triggers) and the job
  extras (`JobPriority`, `RunAsMe`, `TargetFramework`,
  `RequiresUserInteraction`, `JobFailuresGracePeriodInHours`). The fields
  stay on the entity (`Get` output) and leave `Get-OrchApiTrigger -ExportCsv`.

### Internal

- `Test-OrchShortenScope` (never exported) retired into a unit test;
  link/fixture round-trip Pester coverage and an error-message extraction
  corpus test added. Gates: build clean, 586 unit + 336 live Pester pass.

## [1.5.2] - 2026-05-21

`New-OrchTestSet` becomes fully usable. v1.5.1 shipped the cmdlet
without exposing the `Packages[]` / `TestCases[]` fields the server
requires, so standalone creation always failed with errorCode 3204.
v1.5.2 adds the parameters and live-verifies the full create cycle on
the yotsuda tenant (TestSet Id 56588, TestCaseCount 2 round-trip).

### Added

- **`New-OrchTestSet -Packages` and `-TestCases`** — typed
  `TestSetPackage[]` / `TestCase[]` arrays. Both accept
  ValueFromPipelineByPropertyName so pipeline-bound TestSet objects
  flow through. Pass empty/null to keep the previous v1.5.1
  "barebones rejected by server" shape; supply both to actually
  create something the server accepts.

### Changed

- **`New-OrchTestSet` re-fetches via GetForEdit after Create.** The
  POST response and the LIST GET both return TestSet rows with the
  Packages and TestCases collections empty (only the per-item
  `GetForEdit` endpoint populates them). The cmdlet now calls
  GetForEdit once after Create so the emitted output reflects the
  actual stored arrays — `Packages=1 TestCases=2` instead of
  `Packages=0 TestCases=0` for the same just-created entity.

- **`New-OrchTestSet.md` help** rewritten to drop the v1.5.1
  "barebones rejected; pipe from Copy" advice (which was misleading —
  pipeline-from-Get does NOT carry the arrays because the LIST
  endpoint omits them) in favour of the actually-working explicit
  `-Packages` / `-TestCases` path, with a worked example against a
  test automation package.

### Live verification

  New-OrchTestSet (explicit Packages + TestCases)  PASS (Id 56588, TestCaseCount 2)
  New-OrchTestSetSchedule                          cmdlet path verified;
                                                   tenant rejected with
                                                   "Test set schedule creation
                                                   ...not allowed for this
                                                   tenant" — server-side
                                                   feature flag, out of cmdlet
                                                   scope. Payload construction
                                                   and TestSetName→Id
                                                   resolution confirmed by the
                                                   reaching-server error.

## [1.5.1] - 2026-05-21

CSV-driven workflow + wire-shape parity release. Closes six
`Copy-Orch* / Get-Orch* / Remove-Orch*` cmdlets that were missing
their `New-` counterparts and adds the first `Update-Orch*` for the
HttpTrigger surface. `Get-OrchApiTrigger -ExportCsv` round-trips
cleanly into `New-OrchApiTrigger` / `Update-OrchApiTrigger`. Two
serialize / payload bugs surfaced by live dev-tools captures (yotsuda
tenant) are fixed in the shared helper so every cmdlet that touches
HttpTriggers or trigger MachineRobots benefits.

### Added

- **`New-OrchApiTrigger`** — wraps POST `/odata/HttpTriggers`. Full
  TriggerBase + HttpTrigger field surface (Name, Release, Method, Slug,
  CallingMode, AllowInsecureSsl, RunAsCaller, JobPriority, RunAsMe,
  RuntimeType, TargetFramework, ResumeOnSameContext, RequiresUserInteraction,
  StopStrategy, StopJobAfterSeconds / KillJobAfterSeconds,
  AlertPendingJobAfterSeconds / AlertRunningJobAfterSeconds,
  RemoteControlAccess, ConsecutiveJobFailuresThreshold /
  JobFailuresGracePeriodInHours, InputArguments, MachineRobots).
  Defaults `Tags=[]`, `MachineRobots=[{}]` (placeholder), and
  `Slug=Name` when those parameters are omitted — the server returns a
  generic 500 `An error has occurred.` if any of the three is absent
  from the POST body.

- **`Update-OrchApiTrigger`** — wraps PUT `/odata/HttpTriggers({id})`
  where id is the GUID `HttpTrigger.Id`. Mirrors the New surface plus
  `-NewName` (rename). Uses dirty-detection (no PUT if every passed-in
  value already matches server state) and re-fetches the trigger after
  PUT so the cmdlet output reflects post-update state.

- **`Get-OrchApiTrigger -ExportCsv` / `-CsvEncoding`** — emit every
  API trigger reachable under the target path as a CSV whose column
  names match `New-OrchApiTrigger` / `Update-OrchApiTrigger`
  parameters. ReleaseKey is resolved to Release Name on emit so the
  CSV is human-editable; MachineRobots is serialised via the existing
  shared helper, preserving binding identity through the round-trip.

- **`New-OrchTestSet`** — wraps POST `/odata/TestSets`. Known
  limitation: the wrapped endpoint rejects barebones calls with
  errorCode 3204 (`Test Set is empty. It should have at least one
  package and one test case.`); standalone creation needs Packages[]
  and TestCases[] which aren't yet exposed as parameters — pipe a
  complete TestSet from `Copy-OrchTestSet` until standalone-creation
  parameter expansion lands.

- **`New-OrchTestSetSchedule`** — wraps POST `/odata/TestSetSchedules`.
  Mandatory `-TestSetName` resolves to TestSetId at submit time.
  CronExpression defaults to every-minute; TimeZoneId defaults to the
  local time zone.

- **`New-OrchTestDataQueue`** — wraps POST `/odata/TestDataQueues`.
  Defaults `-ContentJsonSchema` to `{}` (empty schema = any value)
  when omitted; the server returns 400 `The ContentJsonSchema field
  is required.` without that field.

- **`New-OrchActionCatalog`** — wraps POST
  `/odata/TaskCatalogs/UiPath.Server.Configuration.OData.CreateTaskCatalog`.
  External noun matches the in-product UI label; the wire entity is
  the legacy `TaskCatalog`.

- **PlatyPS help md** for each of the six new cmdlets, plus expanded
  `Get-OrchApiTrigger.md` covering `-ExportCsv` / `-CsvEncoding` and
  the CSV-roundtrip workflow.

### Changed

- **`HttpTrigger` entity** gains `RunAsCaller` (bool) and `Key`
  (string), both confirmed against live dev-tools POST/PUT captures.
  `RunAsCaller` was previously silently dropped on every POST/PUT
  built from a cmdlet.

- **`SerializeMachineRobotSessions` falls back past `Robot.Username`
  for Modern (folder-user-bound) robots.** The previous implementation
  emitted only `robot?.Username`, which Modern robots leave null
  (carrying the real user name on `Robot.User.UserName` instead). The
  result was that a user-bound trigger round-tripped as `[{}]` in the
  CSV cell, and the next `Update-Orch*Trigger` cleared the binding.
  Fixed by chaining `robot?.Username → robot?.User?.UserName →
  mr.RobotUserName` and emitting the first non-empty value. Shared
  helper, so the fix benefits every cmdlet that touches trigger
  MachineRobots (`Get-OrchTriggerDetail`, `Get-OrchApiTrigger`,
  `New/Update-OrchTrigger`, `New/Update-OrchApiTrigger`).

- **`HttpMethodItems` candidates switched to mixed case** (`Get`,
  `Post`, `Put`, `Delete`, `Patch`) matching the wire format the
  server returns on GET — uppercase candidates would round-trip but
  read back lower-case, confusing dirty-detection on subsequent
  Updates.

- **`OrchAPISession.CreateTestSet` return type:** `void` → `TestSet?`.
  The POST response body was previously captured but discarded; the
  new return matches every other Create method in the file and lets
  `New-OrchTestSet` emit the created entity.

### Fixed

- **`New-OrchApiTrigger` POST 500 from missing structural fields.**
  Wire investigation confirmed the server treats `Tags`,
  `MachineRobots`, and `Slug` as required even though Swagger marks
  them optional. The cmdlet now defaults them when callers omit, so
  barebones invocations succeed.

- **`Get-OrchApiTrigger -ExportCsv` MachineRobots round-trip wipes
  Modern-robot bindings.** Covered by the
  `SerializeMachineRobotSessions` change above.

## [1.5.0] - 2026-05-21

Architecture cleanup release. The cache layer collapses into a single
registry-driven hierarchy on a new cross-family base class. `Clear-OrchCache`
gains folder-level granularity with auth-safe scope dispatch. Every shipped
help md is aligned against swagger v20.0, and 8 previously-undocumented
cmdlets join the help corpus.

### Added

- **`Clear-OrchCache` -- scope-aware `-Path` dispatch.** Path shape now selects
  the clear scope:
  - `Clear-OrchCache .` (most common) — clear only the current folder's cache;
    tenant catalog and other folders stay warm.
  - `Clear-OrchCache Orch1:\Shared` — per-folder clear (named folder).
  - `Clear-OrchCache Orch1:\` — tenant + organization caches (the
    root-visible entities).
  - `Clear-OrchCache Orch1:` — drive-level full clear (preserves
    backward-compat with existing scripts).
  - `Clear-OrchCache -Recurse` / `-Depth` — subfolder fan-out for folder paths.
  Every drive is auth-state-gated: unauthenticated drives are silently
  skipped, so no PKCE flow is provoked just to confirm an empty cache.

- **8 PlatyPS help md files** for previously undocumented cmdlets:
  `Add/Get/Remove-OrchBucketLink`, `Add/Get/Remove-OrchQueueLink`,
  `Remove-OrchAssetLink`, `Get-OrchProductVersion`. Endpoints HTTP-log-verified,
  OAuth scopes swagger-verified.

### Changed

- **Help mds: OAuth scope lines aligned with swagger v20.0.** Swept every
  shipped help md and corrected 52 scope drifts. 8 were real corrections
  (e.g., `OR.Execution` → `OR.Jobs` on Trigger cmdlets, `OR.Execution.Read` →
  `OR.Monitoring.Read` on `Get-OrchJobMedia`); the other 44 added the
  `.Write` / `.Read` alternative to scope lines that previously listed only the
  parent scope. The `Copy-OrchAsset/Calendar/Machine/User/Webhook` mds retain
  their richer `(source) / (destination)` annotation.

- **Help mds: SYNTAX parameter order standardised** — Path/Recurse/Depth
  first, positional next (in position order), named last (alphabetical). New
  `PlatyPS/Reorder-MdSyntaxParameters.ps1` applies the same canonical order to
  the .md sources that the existing `Reorder-SyntaxParameters.ps1` applies to
  the built MAML; both layers now produce the same order.

- **Cache layer refactor (4 phases plus cleanup).** Collapses a long-standing
  duplication shape across the cache classes:
  - `OrchDriveInfoBase` extracted as the common base for `OrchDriveInfo`,
    `OrchDuDriveInfo`, `OrchTmDriveInfo`. Hoists `_allTenantCache`,
    `_allFolderCache`, `_NameColon`, `_NameColonSeparator`, `RootFolder`, the
    `ClearAllCache` iteration, and abstract accessors for `OrchAPISession` /
    `PartitionGlobalId`.
  - Cache class ctors generalised to accept `OrchDriveInfoBase`, so the
    universal templates serve every family.
  - Family-specific cache duplicates deleted (`DuListCachePerTenant`,
    `DuListCachePerOrganization`, `DuKeyedListCachePerOrganization`,
    `TmListCachePerTenant0`, `TmSingleCachePerTenant0` — pure byte-equivalent
    forks of the universal classes).
  - Family-specific cache classes moved to their family folders:
    `TestManagerCmdlet/OrchTmCache.cs`, `PlatformManagementCmdlet/OrchPmCache.cs`,
    mirroring the existing `DocumentUnderstandingCmdlet/OrchDuCache.cs`
    precedent.
  Net code reduction ≈ 400 lines after the new base file. No behavior change
  for consumers.

### Removed

- **`Edit-OrchConfig -EditorType` parameter retired.** The parameter had been
  marked `[Obsolete]` and hidden from completion in favor of the switch-typed
  `-UseDefaultEditor`. Migration:
  - `-EditorType Default` → `-UseDefaultEditor`
  - `-EditorType Notepad` (or any other value) → omit the switch (Notepad-first
    is the default).
  `-UseDefaultEditor` is Windows-only; on Linux/macOS the cmdlet has always
  cd'd to the config directory and printed an edit-then-Import prompt
  (documented in NOTES on the help md).

- **Du*/Tm0 cache class names** (see *Changed* above). These were internal
  classes; no cmdlet output or public surface is affected.

### Internal

- New `OrchDriveInfoBase.IsAuthenticated` probe property — never triggers a
  token request, used by `Clear-OrchCache` to skip unauthenticated drives
  without invoking PKCE.
- `OrchPSDriveInfoBase` renamed to `OrchDriveInfoBase` (consistency with the
  concrete subclasses, which don't carry the "PS" infix) and moved from
  `OrchestratorCmdlet/` to top-level `UiPathOrch/`, alongside the other
  cross-family infrastructure files (`AuthManager.cs`, `OrchAPISession.cs`,
  `OrchCache.cs`, `OrchEntities.cs`, `OrchExtensions.cs`).
- xUnit suite: 320 passing.

## [1.4.3] - 2026-05-20

Patch release. Adds `Resolve-OrchAuthError` for diagnosing failed
PKCE sign-ins, wires PKCE error messages to point at it, fixes a
`Clear-OrchCache` miss for `DuExtractor` data, and finishes the
DU/TM cache migration started in 1.4.2.

### Added

- **`Resolve-OrchAuthError` — local diagnostic for failed PKCE /
  browser sign-ins.** Takes the URL the browser was left on,
  decodes it entirely locally (no network), and returns
  `ErrorCode`, `TraceId`, `ClientId`, `RedirectUri`, `Scopes`,
  and an actionable `RecommendedAction`. Handles both URL shapes
  seen in the wild (login-error with `returnUrl`, and web-error
  with base64 `errorId`). Tailored diagnoses for `#219`,
  `invalid_request` / Invalid redirect_uri, `invalid_scope`; for
  unrecognised codes asks the user to forward the `traceId` to
  UiPath Identity. xUnit fixtures are verbatim customer payloads.

### Fixed

- **`Clear-OrchCache` silently left stale `DuExtractor` data
  behind on DU drives.** `OrchDuDriveInfo.ClearAllCache` was a
  hand-maintained list and `_dicDuExtractors` was missing from it.
  DU caches now register themselves with the drive's registry and
  `ClearAllCache` iterates uniformly — the class of bug is
  structurally impossible. xUnit regression test added.

- **`DuExtractor` `Format.ps1xml` group header rendered as null.**
  The view referenced a `PathProject` field the entity didn't
  have. Switched to `Path`, which the cmdlet now correctly stamps.

### Changed

- **PKCE / sign-in failure messages now point at
  `Resolve-OrchAuthError`.** When the browser is left on an
  Identity error page and the user Ctrl+Cs the cmdlet, the
  terminating error instructs them to copy the URL, run
  `cd $HOME`, then `Resolve-OrchAuthError '<url>'`. The `cd`
  step is needed because PSReadLine tab-completion of the cmdlet
  name on an Orch drive would re-trigger PKCE before Enter. The
  thrown exception type was swapped from `OperationCanceledException`
  to `InvalidOperationException` so the custom message is printed
  verbatim (PS canonicalises OCE messages on cancellation paths).

- **DU cache architecture: 12 raw `_dic*` fields on
  `OrchDuDriveInfo` migrated to 6 typed cache classes.** `DuRoles`
  and `DuUsers` are now **org-scoped** (singleton across drives in
  the same org) with the PM* 1.4.2 path-isolation pattern — cuts
  API calls when multiple DU drives in the same org are used
  together. The other 4 caches remain per-tenant. 6 DU entities
  gain `ShallowClone()` for the emit-path clone.

- **TM `_dicTmProjects` migrated to a new
  `TmListCachePerTenant0<T>`.** Finishes the cache migration
  started in 1.4.2 — no `_dic*` accumulators remain on
  `OrchTmDriveInfo`.

### Internal

- xUnit suite 300 → 320 (+11 `AuthErrorUrlParserTests`,
  +7 `OrchDuDriveInfoCacheRegistrationTests`, +2 others).

## [1.4.2] - 2026-05-19

Patch release. Two pipeline-input bug fixes in helper functions, three
raw `_dic*` caches retired in favor of small focused cache classes,
and the cmdlet-redesign-plan-p3 doc closes out (raw `_dic*`
accumulator pattern eliminated except for `_dicFolders` itself).

### Fixed

- **`Enable-/Disable-OrchPersonalWorkspace` and
  `Enable-/Disable-OrchUserAttended` lost all but the last input
  when piped to.** These functions declared
  `[Parameter(ValueFromPipelineByPropertyName)]` but had no explicit
  `process` block, so `Get-OrchUser | Disable-OrchUserAttended`
  silently disabled only the last user. Direct invocation
  (`-UserName a,b,c`) was unaffected and remains unchanged.

- **`Logging.Enabled` now defaults to `true` when the `Logging`
  section is present but `Enabled` is unspecified.** Previously a
  config with only `Logging.Level` set (and `Enabled` omitted)
  produced no log output at all — the log folder was created (by
  `Get-OrchLogLocation`) but stayed empty, with no signal why. This
  matched the drive-level `Enabled` convention (null → on) nowhere,
  and was a frequent support trap when diagnosing auth issues. An
  explicit `Logging.Enabled: false` is still respected; absent
  `Logging` section still means no logging.

- **Auth-diagnostics / HTTP log was lost when a sign-in hung or
  failed (e.g. PKCE `#219`).** Even with logging correctly enabled,
  `AsyncLogWriter`'s time-based flush was only re-evaluated when the
  *next* entry was dequeued — so a lone buffered entry with no
  follow-up (exactly the pre-auth diagnostics block written just
  before a PKCE listener that then blocks on a browser callback that
  never comes) was never written: the log folder was created but the
  file stayed empty. Together with the `Logging.Enabled` fix above,
  this is the second half of the "log folder exists but is empty when
  diagnosing an auth failure" support trap. The interval flush is now
  a real deadline (capped at the time remaining until the next flush
  is due), so a single buffered entry is persisted within
  `flushIntervalMs` even while the producer is idle or blocked; batch
  flush and graceful-dispose drain are unchanged. Regression tests
  added — the existing `AsyncLogWriter` suite all disposed before
  asserting (dispose drains), so none covered the idle-flush path.
  Note: this restores log *capture* during a failed sign-in; it does
  not change authentication behaviour itself.

- **PM records could carry another drive's per-drive `Path`** — when
  results were retained (a variable, or queried across same-org
  drives) every element showed the *last* drive's `Path`. The 1.4.0
  fix had attached `Path` via a per-`WriteObject` PSObject
  `NoteProperty`, which didn't hold (PowerShell binds PSObject members
  to the shared cached base object). `Path` is a plain DTO property
  again, Shallow-cloned per emit before it's stamped — independent per
  drive, the rest still shared with the singleton, and safe under the
  now-parallel `Get-Pm*`.

- **Cloud sign-in failed with `#219` "user has not accepted the
  invitation" for Entra-ID-federated organizations.** The Cloud
  Identity URL was auto-derived org-scoped
  (`https://cloud.uipath.com/{org}/identity_`); that authorize
  endpoint resolves a federated user into an org membership /
  invitation check that returns errorCode 219. Local UiPath accounts
  were unaffected (which is why it shipped unnoticed). Introduced in
  v0.9.15.5 (commit d57c287, whose comment wrongly assumed "Identity
  now requires /{org}/identity_/"); pinned by a customer release
  bisection (v0.9.15.4 OK → v0.9.15.5 NG). Cloud now auto-derives
  host-level `https://cloud.uipath.com/identity_` (same form as
  Automation Suite), verified end-to-end on current Cloud Identity.
  An explicit `IdentityUrl` still overrides for non-default Identity
  Servers.

### Changed

- **PM audit log cache (`_dicPmAuditLogs`) migrated to
  `IncrementalCachePerTenant`.** Uses the entity as its own key, so
  the previous `HashSet<PmAuditLog>` structural-dedup semantic is
  preserved while the cache joins the standard
  `Get-OrchAuditLog`-style lifecycle (`Fetch` / `GetCache` /
  `ClearCache` / per-tenant exception cache).

- **PM bulk-resolve cache (`_dicPmBulkResolveByName`) migrated to a
  concrete `PmGroupMembersCache`.** Static storage keyed by
  `(partitionGlobalId, kind, name)`, so all drive instances pointing
  at the same org share one cache — bulk resolution (e.g. during
  `Add-OrchFolderUser`) of the same name across multiple drives no
  longer re-pays the API call. Negative caching preserved: the API
  returns `null` for unresolved names and that null is kept so the
  next lookup is a cache hit. The class is concrete (not generic)
  because the chunk size (20), key shape, and value type are all
  fixed by this one endpoint; a generic class would expose unused
  flexibility for no real consumer.

- **Robot log cache (`_dicRobotLogs`) migrated to a concrete
  `RobotLogsCache`.** Per-folder `ConcurrentBag<Log>` accumulator
  (the API returns `Log.Id == 0` so per-id dedup isn't possible),
  `IFolderCacheClearable`-registered, so `Clear-OrchCache -Path
  <folder>` now flushes the right slice without manual null-out.

### Internal

- `Find-OrchFolderNoUserAssigned` now emits a `Write-Verbose` line
  for the folders it silently skips, so `-Verbose` reveals which
  folders failed to resolve.
- ScriptAnalyzer Warnings reduced from 23 → 11 in `Staging/`; the
  remaining 11 are all intentional `Write-Host` in
  `Staging/Examples/*.ps1` sample scripts.
- `design/cmdlet-redesign-plan-p3.md` Group F closes out (0 bespoke
  raw caches remaining outside of `_dicFolders` itself, which
  carries its own multi-phase fetch + lock-and-publish design from
  1.4.1).

## [1.4.1] - 2026-05-15

Patch release. Headline is a thread-safety fix that eliminates
intermittent `Cannot find path '<folder>'` errors under multi-row
`Set-OrchAsset` / `Set-OrchCredentialAsset` / `Set-OrchSecretAsset`
pipelines. Plus deterministic-failure caching and a cache-class
consolidation pass.

### Fixed

- **`Cannot find path '<folder>'` under multi-row `Set-Orch*Asset`
  pipelines.** The folder cache was published incrementally, so a
  parallel worker thread could observe a partial list and miss a
  folder that actually existed. Build and publish are now atomic. The
  same fix protects `Copy-Item`'s mid-copy folder addition.

### Changed

- **`ExceptionCache` now caches deterministic non-HTTP failures.**
  Repeated calls that hit a deterministic gate (e.g., API-version
  checks) no longer re-pay the failure cost. Adds 410 (Gone) and 501
  (Not Implemented) to the HTTP whitelist. `Get-OrchAlert`'s
  API-version guard benefits directly.

- **`CurrentUser` and the four session cmdlets join the standard
  cache hierarchy.** `Get-OrchCurrentUser` now uses
  `SingleCachePerTenant`; `Get-OrchUserSession`,
  `Get-OrchMachineSession`, `Get-OrchUnattendedSession`,
  `Get-OrchAlert` use `IncrementalCachePerTenant` /
  `IncrementalCachePerFolder`. Observable behavior unchanged.

### Internal

- Cmdlet class rename `*Command` → `*Cmdlet` (resolves DTO name
  collisions; cmdlet noun-verb names unchanged).
- `_dic` prefix removed from `OrchDriveInfo` scalar fields that were
  never dictionaries.
- 14 backward-compat shim methods on `OrchDriveInfo` retired; direct
  cache-class access is canonical.
- `ReleaseNotes.md` removed — `CHANGELOG.md` is the single source.
- Per-organization Path isolation Phase 4 cleanup (final loose ends
  on `NuLicensedGroupMember`, `AvailableUserBundles`,
  `UpdateLicensedGroupResponse`).
- Test infrastructure modernized to fixture-based round-trips,
  eliminating tenant-state-dependent assertions.

## [1.4.0] - 2026-05-13

The headline is the **per-organization cache `Path`-isolation refactor**
that was queued by 1.3.0's plan doc and shipped here in three phases.
Org-scoped entities (PM users, groups, robot accounts, external apps,
licenses, allocations, directory search results, etc.) used to carry a
drive-local `Path` field on what was actually a singleton instance
shared across all drives in the same org, which raced under
multi-drive cmdlets. Their `Path` now lands on a `PSObject`
`NoteProperty` per `WriteObject`, so the singleton stays a singleton
and each emitted record carries its own drive context.

Plus: **per-drive auth logging with secret masking** (to support
diagnosing PKCE issues like "user has not accepted the invitation"
errors from the Identity Provider), **PKCE serialization** so
multi-drive auth doesn't crash on a port-8085 listener collision, and
a few smaller table / sort polish items.

### Added

- **Per-drive auth diagnostics + auth HTTP traffic logging** in the
  existing drive log file (`Get-OrchLogLocation`). Triggers once per
  drive per session on first auth flow (PKCE / Confidential App /
  Refresh / Username-Password) or on first API call (PAT mode), so
  support requests can lean on "send me the log file" instead of
  "what's the org URL, what's the IdP, what's the scope, what's the
  app type" round trips. The block includes UiPathOrch / PowerShell
  / .NET / OS versions and the drive's auth-relevant PSDrive settings
  (Root, Edition, IdentityUrl, AppId, RedirectUrl, HttpListener,
  Scope, Username, UseInPrivate, IgnoreSslErrors, ProxyEnabled —
  AppSecret / AccessToken / Password / proxy credentials are
  intentionally excluded). Auth call traffic (`/connect/token`,
  `/api/Account/Authenticate`) is also recorded at the same
  `LoggingLevel` granularity as API calls (Info: summary only;
  Trace/Verbose: with bodies; errors: full), with `access_token`,
  `refresh_token`, `id_token`, `client_secret`, `code`,
  `code_verifier`, `assertion`, `password` redacted to
  `***REDACTED***`.

### Fixed

- **Multi-drive cmdlets no longer crash with "port 8085 in use"
  when more than one drive needs PKCE auth.** PKCE flow's
  `HttpListener` is serialized across drives via a static lock —
  already-authenticated drives still fetch in parallel; only drives
  that need to open the browser queue up behind each other so the
  port handoff is clean.

### Changed

- **Per-organization cache entities no longer carry their
  drive-local `Path` field.** Affected:
  `LicenseInventory`, `AccountLicense`, `PmAuthenticationRoot` (Phase
  1 — `SingleCachePerOrganization`); `PmUser`, `PmGroup`,
  `PmRobotAccount`, `ExternalClient`, `ExternalResource`,
  `AvailableUserBundle`, `TenantAllocation`, `NuLicensedGroup`,
  `NuLicensedUser`, `AccessAllowedMember` and the inherited
  `NuLicensedGroupMember` (Phase 2 —
  `ListCachePerOrganization`); `PmGroupMember`,
  `PmDirectoryEntityInfo` (Phase 3 — sub-entities). Drive context is
  attached as a `PSObject` `NoteProperty` at `WriteObject` time via
  the new `OrchExtensions.WithPath<T>` helper. PowerShell-level
  property access (`$x.Path`) is unchanged from the caller's
  perspective; only the underlying type-system structure moves.

- **Multi-drive output is now sorted by drive name in
  `EnumOrchDrives`** (already true for `EnumPmDrives`). Cmdlets like
  `Get-OrchProductVersion -Path B:,A:` emit in `A:` then `B:` order,
  which makes multi-drive tables read predictably. Fetches still
  run in parallel; only the consumer-side yield order is fixed.

- **`Get-OrchProductVersion` has a typed format view.** Five-column
  table — `Path` / `Version` / `Deployment` / `ConfigsVersion` /
  `Timestamp` — instead of the generic property-list fallback.

### Internal

- `GetPSPath` extension methods for the org-shared entity types now
  take an explicit `drivePath` argument (was: read `entity.Path`).
  Around 50 call sites updated. `DriveScopedCompleter<TEntity>.GetTipHelp`
  also gained an `OrchDriveInfo drive` parameter so PM entity
  completers can supply the path.
- `ListCachePerOrganization.Get` / `Get(id)` run their initializer
  exactly once at publish time (inside the partition lock) rather
  than on every cache hit. The cache-hit re-init was a leftover from
  the `Path`-on-entity model and would race with itself across
  drives in the same org. Detail-lookup path also short-circuits
  when `_getterId` is null.
- `OrchLog`'s `BuildCombinedLogBlock` and `WriteLogBlockAsync` are
  now `internal` so `AuthManager.SendWithLogging` can reuse the same
  block format and writer instead of re-rolling them. A shared
  call-id counter (`OrchAPISession.NextCallId`) means auth calls
  appear in the same chronological log sequence as API calls.
- `design/per-organization-cache-path-isolation.md` updated with
  Phase 1-3 completion notes and a short list of remaining
  cosmetic-alignment work (`UpdateLicensedGroupResponse.Path`,
  `NuLicensedGroupMember.PathGroupName`,
  `AvailableUserBundles.GroupName`/`PathGroupName` — all Response
  wrappers with caller-supplied drive context, not race-prone).

## [1.3.0] - 2026-05-13

Two themes: **Azure AD B2B guest support** across the user/folder-user
cmdlets, and **cache-class hardening** — race conditions and thread-safety
issues uncovered by a methodical review of the cache classes. Plus a new
`Get-OrchProductVersion` cmdlet, an internal `ChainedThreadPool` to bound
the cap on 2-phase fetchers, and near-instant Ctrl+C across more cmdlets.

### Added

- **`Get-OrchProductVersion`** — hits `/api/Status/Version` to surface the
  Orchestrator product version (distinct from the API version surfaced by
  `Get-OrchSetting`). Useful for scripts that need to gate behaviour on
  Orchestrator version rather than API contract.

- **`Get-OrchUserDetail`, `Get-OrchTriggerDetail`, `Get-OrchProcessDetail`** —
  dedicated cmdlets for the per-id detail API calls that were previously
  reachable only through the `-ExpandDetails` switch on `Get-OrchUser`,
  `Get-OrchTrigger`, and `Get-OrchProcess`. The new cmdlets make the
  intent explicit at the call site (the parent `Get-*` cmdlet's
  one-fetch shape stays cheap by default) and the corresponding
  `-ExpandDetails` switches now emit a deprecation warning naming the
  canonical detail cmdlet. The old switches still work for backwards
  compatibility and will be removed in a future major release.

### Fixed

- **Azure AD B2B guest users can now be addressed by their canonical
  EmailAddress.** B2B guests have a mangled tenant-form `UserName`
  (`foo_bar.com#ext#@tenant.onmicrosoft.com`) that differs from their
  EmailAddress (`foo@bar.com`). Previously many cmdlets only matched
  `-UserName` against the mangled tenant form, so a caller who passed the
  EmailAddress they actually remember got "no match" — even though
  `Add-OrchFolderUser` had always accepted EmailAddress via its Identity
  Server lookup, breaking the add/remove symmetry. Now `-UserName`
  matches against UserName OR EmailAddress on:
  `Get-OrchUser`, `Get-OrchUserDetail`, `Remove-OrchUser`,
  `Set-OrchAsset`, `Set-OrchSecretAsset`, `Set-OrchCredentialAsset`,
  `Get-OrchFolderUser`, `Add-OrchRoleToFolderUser`,
  `Remove-OrchRoleFromFolderUser`, `Remove-OrchFolderUser`,
  `Move-OrchFolderUser`, `Copy-OrchFolderUser`, `Remove-OrchRoleFromUser`,
  `Remove-OrchAssetUserValue`. `Copy-Item -Recurse` (folder-user copy
  path) covered too.

- **`Get-OrchQueueItem` no longer fails on strict-OData Orchestrators.**
  Three related issues: the default `-OrderBy DeferDate` was rejected
  ("The property 'DeferDate' cannot be used in the `$orderby`"), filter
  values had unencoded spaces, and a non-`$`-prefixed legacy
  `&orderby=Id desc` was always appended alongside the canonical
  `$orderby`. Default is now `Id`, filter values are percent-encoded,
  and the duplicate clause is dropped.

- **`Remove-OrchAssetUserValue` matches by UserId instead of by
  user-value `UserName`.** UserValue entries store the tenant-form
  UserName for B2B guests, which doesn't match the EmailAddress form a
  caller would naturally type. The cmdlet now resolves the caller's
  `-UserName` patterns to a set of tenant User IDs (matching either
  field) and filters UserValues by the stable UserId. Wildcard
  semantics preserved.

- **`ListCachePerOrganization.Get(id)` cached the detailed-fetch
  exception in the wrong slot.** The `try` block called
  `_exception.CacheException(partitionGlobalId, ex)` (the org-level
  cache) but the matching `ThrowCachedExceptionIfAny` at the top of the
  method read `_exceptionDetailed` (the per-`(org, id)` cache). Cached
  failures were never re-thrown — every retry re-issued the failing API
  call. Now routed to `_exceptionDetailed` with the composite
  `(partitionGlobalId, id)` key.

### Changed

- **Near-instant Ctrl+C across more cmdlets.** `Get-OrchUserDetail`,
  `Get-OrchLibraryVersion`, `Get-OrchTestDataQueueItem`,
  `Get-Orch{Asset,Bucket,Queue}Link`, and `Get-OrchPackageVersion` were
  migrated to a new internal `ChainedThreadPool` plumbing that bounds
  the concurrency cap on 2-phase fetchers (previously each phase opened
  its own `OrchThreadPool` and stacked to cap=4×4=16 against a single
  Orchestrator). The consumer also now bails immediately on Ctrl+C
  instead of waiting for the next iteration boundary.

- **`Tests/SelfContained.Tests.ps1` is tenant-state independent.** The
  suite no longer assumes specific user accounts or folders on the
  tenant, picks an alternate `DirectoryUser` dynamically, and creates
  its own smoke folder. Added the `B2BRegression` Describe (`BF1`-`BF7`)
  covering the EmailAddress-form matching on the FolderUser / User
  cmdlets.

### Internal

- **Cache classes — race-condition fixes.**
  - `IndexedListCachePerFolder`: the inner per-folder dict was plain
    `Dictionary<,>` while the outer was `ConcurrentDictionary`. Cmdlets
    like `Get-OrchTestDataQueueItem` (`ChainedThreadPool` Phase2 over
    queues in one folder) and `Get-OrchProcessRequirement`
    (`OrchThreadPool` over releases in one folder) hit the inner from
    multiple threads sharing the same folderId, risking
    `InvalidOperationException` on rehash and silent stale lookups. Inner
    switched to `ConcurrentDictionary`; first-touch uses `GetOrAdd`.
  - `IncrementalCachePerFolder` / `PerTenant` / `PerProject`: the
    `Fetch` / `AddToCache` "TryGetValue → mergeFunc → indexer-assign"
    sequence was not atomic. Switched all 6 callsites to
    `AddOrUpdate`, so merge + publish can't lose a concurrent writer.
  - Per-Tenant `IndexedCachePerTenant`, `KeyedSingleCachePerTenant`,
    `KeyedListCachePerTenant`, and the three `Tm*` siblings now use
    the same TryGet → lock → TryGet → fetch → publish pattern as
    `SingleCachePerTenant` / `ListCachePerTenant`. Concurrent Get on
    the same key no longer issues duplicate API calls.
    `TmSingleCachePerTenant0` gained its missing `_lock` field.
  - Per-Organization cache classes (`Single`, `List`, `KeyedSingle`,
    `KeyedList`) now serialize fetches per-partition rather than
    per-instance (`Single`/`List`, which allowed duplicate fetches
    across drives in the same org) or per-global (`KeyedSingle`/
    `KeyedList`, which serialized fetches across unrelated orgs).
    A static `ConcurrentDictionary<string, object>` of partition-keyed
    lock objects gives same-org → same-lock → serialized, different-org
    → different-locks → parallel.
  - `IndexedListCachePerFolder.ClearCache(folderId)` /
    `ClearCache(folder, id)` no longer wipe every cached exception in
    the class — they scope the clear to the folder / `(folder, id)`
    actually being invalidated.
- **OrchThreadPool**: dispose race tamed, `Impl` class hidden from
  callers, factory functions no longer marshal through `SyncContext.Post`.
- 5 raw dictionaries on `OrchDriveInfo` migrated to typed cache classes:
  `_dicQueueItems`, `_dicTestCaseExecutions`, `_dicTestSetExecutions`,
  `_dicJobsHavingExecutionMedia` → `IncrementalCachePerFolder`;
  `_dicTestCaseAssertions` → `KeyedListCachePerFolder`.
- `OrchDriveInfo` drops the `GetUsers()` / `GetUser(user)`
  backwards-compat shims; callers now use `drive.Users.Get()` /
  `drive.UsersDetailed.Get(id)` directly.
- README / `psd1` clarify UiPathOrch is open-source and not part of the
  product. (No functional change.)
- `design/per-organization-cache-path-isolation.md` captures the planned
  PSObject-NoteProperty refactor for moving the drive-local `Path` field
  off shared org-scoped entities — deferred to the next release.

## [1.2.2] - 2026-05-10

Patch release: two fixes for `Add-OrchUser` and `Copy-Item`, plus
internal cleanup.

### Fixed

- **`Add-OrchUser` multi-row CSV no longer clobbers earlier-row scalar
  values when a later row's cell is blank.** In the per-(user, role)
  CSV pattern, blank cells on rows 2+ silently cleared values set by
  row 1, dropping the user's `UnattendedRobot` / `ExecutionSettings`
  configuration at POST time.

- **`Copy-Item -Recurse` no longer spams HTTP 500 warnings from
  Test Manager copies on older Orchestrator.** Test Manager API
  endpoints (`/odata/TestSets` etc.) are not serviceable on Orch
  ≤ 23.4 (ApiVersion ≤ 16), and partially broken through Orch 25.10
  (v17). `Copy-Item` now skips those copies on those versions.

### Changed

- **Edition wording aligned with the `OrchEdition` enum.** README,
  release notes, and `Get-PmLicense*` help pages now say "on-premises
  Orchestrator" rather than "Standalone" or "MSI". `"msi"` and
  `"standalone"` aliases in the edition parser are retained for
  backwards compatibility.

- **`AuthManager`'s PKCE listener loop logs caught exceptions** for
  diagnosis (DebugView / VS Output only; Release binaries unchanged).

### Internal

- Link cmdlets consolidated onto three generic base classes; public
  surface preserved.
- `Assign*` / `MergeDescription` helpers cleaned up.
- `AsyncLogTest` console-app project replaced with xunit tests so CI
  exercises `AsyncLogWriter` every build.
- Test coverage expanded: xunit 205 → 292.

## [1.2.1] - 2026-05-08

Patch release: a `Copy-Item` bug that silently shared the source
entity into destination folders on same-drive copies, plus a
follow-up cache-invalidation bug in the link cmdlets that left the
target folder's view stale until the next `Clear-OrchCache`.
`Export-OrchBucketItem -Recurse` also stops duplicating downloads of
linked buckets, and tab completion on the link cmdlets has been
tightened. Most of these surfaced once users started exercising the
asset / queue / bucket link feature shipped in 1.2.0.

### Fixed

- **`Copy-Item` no longer shares the SOURCE entity into destination
  folders on same-drive copies.** When you copied a folder containing
  linked assets / queues / buckets to another folder on the same drive
  (e.g. `Copy-Item Orch1:\TestFixture_Base Orch1:\TestFixture_CopyDest
  -Recurse`), the dst didn't actually get its own copy of those
  entities — it got a pointer to the src. `Get-OrchAsset` against the
  dst returned the same Id as the src, and `Set-OrchAsset` against
  what looked like the dst's asset silently mutated the src's value.
  Only entities that already had at least one link in src were
  affected (unlinked entities were copied cleanly). Cross-drive copies
  between two different drives were unaffected.

  Root cause: the source-to-destination folder mapping inside
  `Copy-Item` matched source folders against the global folder pool by
  full path, so on a same-drive copy the source folders matched only
  themselves — the link-re-establishing step then added the new
  destination folder to the SOURCE entity's share list rather than
  letting `Copy-Item` create a fresh destination entity. The mapping
  now rebases each source link folder relative to the source folder
  being copied and its destination counterpart (handling descendant /
  ancestor / sibling-cousin shapes), so same-drive copies produce
  cleanly-separated destination entities linked together within the
  destination tree only.

- **Stale target-folder cache after `Add-Orch{Asset|Bucket|Queue}Link`
  and `Remove-Orch{Asset|Bucket|Queue}Link`.** After linking an entity
  from `Orch1:\Foo` to `Orch1:\Bar`, queries against `Bar` (e.g.
  `Get-OrchAsset`, `Get-OrchAssetLink`) returned the cached pre-link
  snapshot — the freshly-shared entity was missing from `Bar`'s view
  until the next `Clear-OrchCache`. The cmdlets now precisely
  invalidate the affected entity's link-visibility cache and the link
  target folders' per-folder entity list, instead of either flushing
  every entity's link cache drive-wide or leaving the per-folder list
  cache stale.
### Changed

- **`Export-OrchBucketItem -Recurse` deduplicates linked buckets
  across the walk.** A bucket linked to N folders appears in every
  folder's enumeration with the same Id, so the previous code wrote
  the same items into one folder path per folder it was accessible
  from (e.g. 3 items × 3 folders = 9 file writes for what represents
  3 unique files). The duplicates were technically a faithful mirror
  of the source folder layout, but they wasted disk space and download
  bandwidth, and most users wanted just one copy of each unique file.
  The cmdlet now writes the items under the first folder it sees the
  bucket from and emits a `WriteWarning` for each subsequent
  encounter — the link is surfaced rather than silently dropped, so
  the user can choose to scope their next call (e.g.
  `-Path SomeSpecificFolder`) if they actually wanted the per-folder
  mirror.

- **Tab completion tightened on the link cmdlets.**
  `Get-Orch{Asset|Bucket|Queue}Link` and
  `Remove-Orch{Asset|Bucket|Queue}Link` `-Name` now only suggests
  entity names that actually have a link beyond their home folder
  (the previous behaviour offered every entity in the folder, most of
  which would no-op when the cmdlet ran). `Remove-Orch{...}Link`
  `-Link` now also has a completer that suggests the folders the
  entity is currently linked to, instead of letting PowerShell fall
  back to provider path completion. Both filter against `-Name` /
  `-Link` values already supplied in the same call.

### Internal

- `Copy-Item`'s link-re-establishing step switched from "return after
  the first matching destination folder" to iterating all matches with
  Id-based dedup. Defensive — in normal incremental copies the dedup
  catches the duplicate Ids of shared entities so the end state is
  identical, but the new code is more robust if a single share-API
  call fails partway (the remaining folders still get tried) or if the
  destination happens to contain non-linked duplicates of the same
  name (each Id gets its own attempt instead of silently losing the
  rest).
- **`release.yml` now runs `dotnet format --verify-no-changes` and
  `dotnet test` before `Publish-Module`.** The Release and CI
  workflows previously fired in parallel on a release commit and ran
  on separate runners, so Release usually finished — and shipped to
  PSGallery — before CI reported any failure. The 1.2.0 release
  exposed this when latent format violations on master were caught by
  CI a few seconds after PSGallery had already accepted the publish.
- **3-link test data entities added to `TestData/Fixture`**
  (`asset-shared3` / `bucket-shared3` / `queue-shared3`, each owned
  by Production and linked to Development / QA / SubA), plus a Pester
  `Copy-Item link reproduction` Describe in `CleanTenant.Tests.ps1`
  that copies the test data tree into a sibling root and asserts
  every multi-link entity's link set is reproduced end-to-end.
  Catches the bugs above as Pester regressions.
- Per-entity precision in the link cache invalidation that runs after
  Add/Remove-Link API calls — replaces the previous drive-wide flush
  of every entity's link-visibility cache. Unrelated entities' cached
  data is no longer collateral damage of an unrelated link change.
- Tightened completers share scaffolding through a small generic base
  hierarchy, so the asset / bucket / queue variants are each defined
  in a few lines that just supply the entity-specific API hooks.
- Pre-existing `dotnet format` violations across several source files
  cleaned up, including a control-flow construct where local and CI
  formatters disagreed about indentation.

## [1.2.0] - 2026-05-07

Two themes: **link-management cmdlets for shared assets, buckets, and queues** —
the folder-link feature that the Orchestrator UI surfaces but UiPathOrch had no
way to inspect or manage from PowerShell — and **comprehensive Ctrl+C support**
across the cmdlet surface. Many cmdlets that previously ignored Ctrl+C during
long operations now bail promptly between iterations.

### Added

- **Asset link trio**: `Get-OrchAssetLink`, `Add-OrchAssetLink`,
  `Remove-OrchAssetLink` for managing folder-shared assets. Enumerates assets
  shared across folders, links an asset to additional folders, or unlinks. The
  `AssetLink` entity surfaces `Path / Name / Link / AssetId / FolderId /
  LinkFolderId` for pipeline composition. `Get-OrchAssetLink` streams output in
  folder/asset order with a per-folder lookahead window so consumer drain
  overlaps producer-side API calls.
- **Bucket link trio**: `Get-OrchBucketLink`, `Add-OrchBucketLink`,
  `Remove-OrchBucketLink` mirroring the asset-link cmdlets, with the
  `BucketLink` entity.
- **Queue link trio**: `Get-OrchQueueLink`, `Add-OrchQueueLink`,
  `Remove-OrchQueueLink` mirroring the asset-link cmdlets, with the
  `QueueLink` entity.

### Fixed

- **Ctrl+C now interrupts long-running operations consistently.** Across more
  than 150 sites, cmdlets that iterate over folders, drives, or per-item
  collections now check the cancel token between iterations. Previously many
  cmdlets would only bail at very coarse boundaries — a `Remove-OrchLibrary`
  with hundreds of versions, or an `Update-PmUser` across a dozen drives, would
  run to completion even after Ctrl+C. Affected cmdlets include `Get-OrchJob`,
  `Get-OrchAuditLog`, `Export-OrchJobMedia`, `Remove-OrchLibrary`,
  `Update-OrchProcessVersion`, `Redo-OrchQueueItem`, `Copy-PmGroup`,
  `Copy-PmExternalApplication`, `Set-PmRobotAccount`, `Update-PmUser`,
  `Add-DuUser`, plus many more.
- **Cancellable rate-limit waits.** `Get-OrchJob`, `Redo-OrchQueueItem`, and
  `Get-OrchQueueItem` now use a cancellable wait between paged API calls;
  Ctrl+C no longer has to wait the full 600 ms pacing delay.
- **`Invoke-OrchApi -Headers` content-type routing.** Caller-supplied content
  headers (`Content-Type`, `Content-Disposition`, etc.) used to be silently
  dropped because they were added to `HttpRequestMessage.Headers`, which
  `HttpClient` rejects. They now route to `HttpContent.Headers` and override
  the default `-ContentType` when supplied. **Behaviour change**: explicit
  `-Headers` `Content-Type` now wins over `-ContentType`.
- **`Export-OrchJobMedia` enumeration was walked three times** as a lazy
  enumerable (prefetch / count / download), causing repeated drive/folder
  resolution work. Now materialized with `.ToList()` once.
- **`Get-OrchAssetLink`, `Get-OrchBucketLink`, `Get-OrchQueueLink` Ctrl+C**:
  the consumer drain now bails promptly while in-flight API calls run to
  completion (cache stays atomic per folder).

### Removed

- **Four unregistered experimental cmdlet files** deleted that were left in
  the tree with comments stating they didn't actually work
  (`GetMaintenanceSetting.cs`, `GetPmUserProfile.cs`, `GetDirectoryScope.cs`,
  `UpdatePmUserPreference.cs`). None were in the psd1 export list, none had
  tests, none were referenced from the docs. The corresponding internal
  `OrchAPISession` helpers (`GetPmUserProfile`, `GetPmDirectoryScope`,
  `GetMaintenance`, `PutPmUserPreference`) remain available for future reuse.
- **`Test.cs` (`Get-OrchTest`)** debug-only experimental cmdlet whose
  `EndProcessing` ran a background `Task.Run` to insert `cd orch1:` into the
  PSReadLine buffer two seconds after the cmdlet exited.

### Internal

- New `IEnumerable<T>.WithCancellation(CancellationToken)` and
  `CancellationToken.Sleep(int ms)` extension methods on `OrchCommon.cs`.
  The first replaces the
  `foreach (var x in xs) { token.ThrowIfCancellationRequested(); ... }`
  boilerplate that had accumulated to ~150 sites; the second is a cancellable
  drop-in for `Thread.Sleep` for rate-limit pacing. Net −211 lines of cmdlet
  source.
- `catch (Exception)` blocks that wrapped per-iteration API calls now re-throw
  `OperationCanceledException` first, so cancellation is no longer silently
  turned into a `WriteError` record in cmdlets like `Update-OrchCredentialStore`,
  `Update-OrchWebhook`, `Get-PmLicensedGroup`, and others.

## [1.1.0] - 2026-05-06

The headline change is **multi-tenant Automation Suite support** — earlier
releases mis-classified AS as on-premises, stripped the tenant out of the
URL, and broke every data cmdlet against AS.

### Added

- **`Edition` configuration field** for `UiPathOrchConfig.json`. Accepts
  `Cloud`, `AutomationSuite`, or `OnPremises`. If omitted, the edition is
  inferred from the `Root` URL: hosts containing `uipath.com` → `Cloud`,
  2-segment paths (`/{org}/{tenant}`) → `AutomationSuite`, anything else →
  `OnPremises`. Three-way classification replaces the previous two-way
  Cloud-vs-on-prem split that had no concept of AS.
- **`Edition` property on `Get-OrchPSDrive` output.** Surfaces the resolved
  value (explicit or auto-inferred) so users can verify the classification.
- **AS sample (`Orch7`) in the bundled `UiPathOrchConfig.json` template** for
  multi-tenant `https://YOUR_AS_HOST/YOUR_ORGANIZATION/YOUR_TENANT` URLs.
  The existing `Orch5` sample is now scoped to single-tenant Orchestrator on
  Azure App Service (which has always worked via the on-prem code path).
- **`Update-OrchWebhook`** — partial-update cmdlet for webhooks. Patches any
  combination of `Description`, `Url`, `Secret`, `Enabled`, `AllowInsecureSsl`,
  and `SubscribeToAllEvents` via PATCH `/odata/Webhooks({id})`. Closes the
  post-migration gap where `Copy-OrchWebhook` cannot carry the `Secret` (the
  API never returns it).
- **`Update-OrchBucket`** — in-place update for storage buckets via PUT
  `/odata/Buckets({id})`. Supports `NewName`, `Description`, `StorageProvider`,
  `StorageContainer`, `StorageParameters`, `CredentialStore`, `Password`,
  `Options`, `ExternalName`, `Tags`. Primary use case is re-supplying the
  bucket `Password` (S3 secret access key, Azure storage key, etc.) after
  migration; `Password` is write-only on the server and never returned by GET.
- **`Update-OrchCredentialStore`** — in-place update for credential stores via
  PUT `/odata/CredentialStores({id})`. Supports `NewName`, `HostName`, and
  `AdditionalConfiguration`. The cmdlet fetches detailed store data, strips
  the masked `AdditionalConfiguration` from the deep copy (mirroring the
  `Update-OrchUser` UR_Password pattern) so that PUT only carries a fresh
  value when the user explicitly supplies one.

### Changed

- **`Copy-OrchWebhook` nulls the masked `Secret` on copy** instead of
  forwarding it. Previously the server-masked value would be written to the
  destination as a literal string, silently breaking signature verification.
  A warning now points at `Update-OrchWebhook` for re-supplying the real
  secret. The `Copy-OrchBucket` and `Copy-OrchCredentialStore` warnings were
  also updated to name the corresponding `Update-Orch*` cmdlet, matching the
  `Set-OrchCredentialAsset` / `Set-OrchSecretAsset` warning convention.

### Fixed

- **Multi-tenant Automation Suite routing.** Orchestrator API calls
  (`/odata/...`, `/api/...`) now correctly resolve to
  `https://{host}/{org}/{tenant}/orchestrator_/odata/...` for AS, where
  the gateway requires the `/orchestrator_/` service prefix. Previously
  the calls produced URLs the AS gateway misinterpreted (`tenantName=odata`),
  returning the unregistered-portal HTML page, which then tripped the JSON
  deserializer with `'<' is an invalid start of a value`.
- **`Invoke-OrchApi` default base URL.** Relative paths (default base =
  Orchestrator) now resolve through the edition-aware base, so
  `-ApiPath /odata/Folders` works on AS without spelling out
  `/orchestrator_/odata/Folders`.
- **`Identity` and `Token` endpoint resolution.** AS deployments now correctly
  build `/identity_/connect/token` and `/identity_/connect/authorize` paths
  (matching the Cloud convention) instead of the on-prem `/identity` paths.

### Internal

- New regression test (`BaseUrlRoutingTests.OrchestratorApiPathsMustUseBaseUrlOrchestrator`)
  scans the source for any new code that composes Orchestrator API URLs from
  the raw tenant base instead of the Orchestrator-service base. Future
  contributors get a fast-fail signal if they re-introduce the AS routing bug.
- New regression test (`EmbeddedConfigTemplateTests.EveryLocaleTemplateParsesAndDeserializes`)
  parses every per-locale `UiPathOrchConfig.json` template with the runtime's
  JSON options and deserializes into `UiPathOrchConfig`, catching JSONC syntax
  errors and shape mismatches (e.g. an `Edition` value outside the enum) at
  CI time instead of at first Mount on a user's machine.

## [1.0.1] - 2026-05-05

Compatibility-focused patch. The module now connects to and migrates
data into older Orchestrator builds the previous release silently broke
on. Verified end-to-end (Reset / Import-Fixture / Pester suite / Cloud →
on-prem `Copy-Item -Recurse`) against six servers spanning ApiVersion
11 → 20: OC 20.10.16, 21.10.4, 22.4.4, 22.10.1, 23.4.0, MSI 25.10.2, and
Automation Cloud.

### Fixed

- **Queue API — `RetryAbandonedItems` strict-deserialization regression.**
  WebApi v18.0 added `RetryAbandonedItems` to `QueueDefinitionDto`.
  Sending the field to a pre-v18 server (incl. MSI 25.10 reporting
  ApiVersion 17) caused the body to fail strict deserialization with
  `"command must not be null"` / `"queueDef must not be null"` (HTTP
  400). `New-OrchQueue`, `Set-OrchQueue`, and `Copy-Item` for queues
  now strip the field on `ApiVersion < 18`.
- **Release / ProcessSchedule / Asset DTO field gating.** The same
  strict-deserialization pattern broke `New-OrchProcess`, `Set-OrchProcess`,
  `New-OrchTrigger`, `Set-OrchTrigger`, `Set-OrchAsset`, and the
  matching `Copy-Item` paths on older servers. Per-field thresholds
  added based on each server's `$metadata`:
  - `ReleaseDto`: `EnvironmentVariables` / `MinRequiredRobotVersion` /
    `FolderKey` (< 19); `HiddenForAttendedUser` / `EntryPointPath` (< 17);
    `RemoteControlAccess` / `VideoRecordingSettings` /
    `AutomationHubIdeaUrl` / `RobotSize` (< 16). `Patch` and `Put` now
    share a strip helper with `Post`.
  - `ProcessScheduleDto`: `EntryPointPath` (< 19);
    `ActivateOnJobComplete` / `ConsecutiveJobFailuresThreshold` /
    `JobFailuresGracePeriodInHours` / `CalendarKey` (< 17);
    `AlertPendingExpression` / `AlertRunningExpression` / `RunAsMe` /
    `IsConnected` (< 16).
  - `AssetDto`: `AllowDirectApiAccess` and `SecretValue` (< 20). The
    former carries through `Copy-Item` from Cloud and was the headline
    blocker for Cloud → MSI 23.4 / 25.10 asset migration.
- **`New-OrchProcess` returned `Not Found` on ApiVersion < 12.** The
  `GetPackageEntryPoints` action endpoint is v15+ and 404s on older
  OCs (e.g. OC 20.10). The cmdlet now skips the EntryPoint →
  EntryPointId resolution on `ApiVersion < 12`, and the API helper
  returns an empty enumerable for the same threshold — mirroring the
  existing `GetPackageMainEntryPoint < 12` guard.
- **`New-OrchMachine` 404 from an unnecessary lookup on older OCs.**
  CSV-piped rows surface absent values as `[""]`; the cmdlet was
  hitting `/odata/Robots/.../FindAllAcrossFolders` for nothing, and
  the endpoint 404s on older OCs. Empty / whitespace `RobotUsers`
  values are now normalised to `null` so the call is skipped.

### Added

- New self-contained test infrastructure under `Tests/`:
  - `Reset-Tenant.ps1` — wipes deletable entities preserving the current
    user. Deletes queues before processes so SLA-linked queues don't
    block process removal.
  - `Import-Fixture.ps1` — loads the curated `TestData/Fixture/`
    (CSVs, bucket items, packaged nupkg) into a clean tenant. Remaps
    the source drive prefix and clears the cache after package upload.
  - `CleanTenant.Tests.ps1` — Pester 5 suite (29 cases incl. read,
    update, CSV round-trip, the `Export-OrchBucketItem -Recurse` repro,
    and a Classic-folder smoke test). ~15 s end-to-end once the
    fixture is loaded.
  - Target drive comes from `$env:UIPATHORCH_TEST_DRIVE` (default `local`).

### Documentation

- Just-the-Docs site scaffolded under `docs/`. The cmdlet reference and
  how-to pages drop drifty cmdlet counts and wire `Get-Help -Online`
  through to the new Pages site (`docs/help/<locale>/<cmdlet>.md`).
- PS1 functions (`Functions/*.ps1`) reference the MAML help XML via
  `.EXTERNALHELP` so `Get-Help` returns full content for them too.

### Notes

22.10.1 users lose the ability to **set** the v16-era ProcessSchedule
fields (`RunAsMe` / `IsConnected` / `AlertPendingExpression` /
`AlertRunningExpression`) and `Release.RobotSize` via UiPathOrch. Both
22.4.4 and 22.10.1 report `ApiVersion = 15` but expose different field
sets, and the module cannot distinguish them without per-call metadata
probing. Reading the existing values still works.

## [1.0.0] - 2026-05-03

This release marks API maturity. Going forward, breaking changes
will be major-version bumps per SemVer.

### Added

- **`Invoke-OrchApi`** — diagnostic / raw API cmdlet that uses the
  drive's authenticated session, folder context (`X-UIPATH-OrganizationUnitId`),
  and ApiVersion. Parameter set is a superset of Invoke-RestMethod's
  diagnostic-relevant options (`-Method`, `-Body`, `-Headers`,
  `-OutFile`, `-InFile`, `-StatusCodeVariable`, `-ResponseHeadersVariable`,
  `-SkipHttpErrorCheck`); auth / proxy / cert parameters are dropped
  because the drive owns them. Adds `-Identity` / `-Portal` for the
  alternate base URLs and `-Raw` to keep the OData envelope. JSON
  responses become PSObject; OData `value` arrays are unwrapped and
  each item gets a `Path` property. The default Format view groups
  by Path (PSTypeName `UiPathOrch.ApiResponseItem`). Non-GET methods
  call `ShouldProcess` so `-WhatIf` / `-Confirm` apply. The bearer
  token never leaves the module.
- xUnit coverage for `OrchExtensions` wildcard / value-set helpers.

### Changed

- `Get-OrchPSDrive`: `Claims`, `CurrentUser`, `TenantId`, and
  `TenantKey` are now populated immediately for both Confidential
  and Non-Confidential apps. `CurrentUser` derives from the JWT
  `preferred_username` claim with fallbacks for older Identity
  Server deployments.
- Mount success page shows the authenticated user's name and
  refreshed visual design. Footer replaces "You can close this
  window now." with three GitHub links (Need help? · Report a bug ·
  Ask a question, pointing to README / Issues / Discussions),
  translated for de/fr/ja/ko/ro/tr. Long tenant URLs stay on a
  single line and the card grows horizontally to accommodate them
  (previously `word-break: break-all` broke URLs mid-string).
- `Set-OrchAsset`: "ValueType assumed as Text" is now emitted via
  `WriteWarning` rather than `WriteError`.
- **Authorization header is redacted to `***` in HTTP log files**
  (Trace / Verbose levels). Verbose logs are now safe to attach
  to GitHub issues without leaking active tokens.
- The first-connect logging warning is trimmed from a 9-cmdlet
  paragraph to a single line; the full credential-cmdlet list lives
  in `Docs/05-Troubleshooting.md`.
- `Docs/05-Troubleshooting.md` rewritten to lead with `Invoke-OrchApi`
  recipes; raw-token extraction demoted to a Last Resort section.
- README: status badges added; new "Headless / CI" section covering
  `UIPATHORCH_SUPPRESS_CONFIG_CREATION`, confidential-app guidance,
  and `Invoke-OrchApi` for ad-hoc CI calls without token leakage.
- AI-agent docs (`01-Essentials.md`, `04-MigrationGuide.md`,
  `06-ContributingGuide.md`): removed the "use `Start-Transcript`
  to capture `-Verbose` / `-WhatIf` output" sections. Recent
  PowerShell.MCP versions surface every PowerShell stream
  (Verbose, Debug, Warning, Information, ShouldProcess) directly,
  so the workaround is no longer needed.

### Added warnings

- TLS validation disabled: emit once per session when a drive has
  `IgnoreSslErrors = true` so users are reminded that MITM goes
  undetected on that connection.
- Logging on: emit once per session when `Logging.Enabled = true`
  so users know HTTP bodies hit disk.

### Fixed

- `ProgressReporter.WriteProgress` no longer throws
  `DivideByZeroException` when `totalNum` is 0.
- Token-endpoint failures no longer dump the raw response body
  into the exception message; only the OAuth2 error envelope is
  surfaced.
- `RenewAccessToken`'s catch block no longer pollutes pipeline
  output.

#### Critical bugs surfaced by 2026-05 review

- **`Set-OrchAsset` / `Set-OrchCredentialAsset` / `Set-OrchSecretAsset`
  silent corruption with unassigned -UserName / -MachineName.** When a
  user or machine that existed in the tenant but was NOT assigned to
  the target folder was passed via `-UserName` / `-MachineName`, the
  client built a per-Robot UserValue and PUT it; the server returned
  200 OK but **silently dropped the UserValue and wiped the asset's
  Global Value** (`Value=""`, `ValueScope="PerRobot"`,
  `HasDefaultValue=false`, `UserValues=[]`). The asset was effectively
  erased while the cmdlet appeared to succeed. The candidate set is
  now intersected with `FolderUsersWithInherited.Get(folder)` /
  `FolderMachinesAssigned.Get(folder)` so the call is rejected up
  front with the existing "is not assigned to the folder" error.
  Migration: add the user / machine to the target folder first via
  `Add-OrchFolderUser` / `Add-OrchFolderMachine`. Verified against a
  live tenant; covered by new regression tests R9 / R13 / R14.
- **`Copy-Item` / `Copy-OrchAsset` could carry per-User UserValues
  to a destination folder where the user has no access**, triggering
  the same silent-corruption family as above.
  `CopyItem.FindDstUser` resolved candidates via `dstDrive.GetUsers()`
  tenant-wide without verifying folder access; `FindDstMachine` already
  did the right thing via `FolderMachinesAssigned`. `FindDstUser` now
  also intersects with `FolderUsersWithInherited.Get(dstFolder)`,
  emits a `WriteWarning`, and returns `null` so the caller drops just
  that UserValue (not the whole asset). Inheritance is honored — a
  user added at a parent folder remains a legitimate per-Robot target
  in the child. Covered by new regression test R15.
- `Update-OrchTrigger`, `New-OrchTrigger`, and the
  `OrchAPISession.PutProcessSchedule` fallback built
  `StartProcessCronDetails` as `"{advancedCron":"…"}` (leading `"`
  then `{`) which fails JSON parse — the server rejected the PUT
  with 400. Now uses `JsonSerializer.Serialize` for the cron value
  and proper braces.
- `Open-OrchJob`: `-Id` lacked `Mandatory=true`, so omitting it
  bypassed the parameter binder and crashed in `foreach (var id in Id!)`.
  Now rejected at bind time; the catch path also `continue`s
  rather than falling through to `job!.Key`.
- `Get-DuExtractor`: the `-Name` tab completer copy-pasted from
  `Get-DuClassifier` and called `GetDuDocumentTypes` instead of
  `GetDuExtractors`, returning the wrong list.
- `Get-OrchAuditLog`: `-UserName` resolution was wrapped in
  `try { ... } catch { }`; on resolution failure or zero matches
  no UserName filter was added to the OData query, **silently
  widening results to all users**. The catch is removed and a
  `(UserId eq -1)` sentinel narrows to nothing instead.
- `Copy-Item` action-catalog progress was reported on the
  wrong `ProgressReporter` instance due to a stale variable name
  copy-pasted from the prior block.
- `Import-OrchQueueItem` (`BulkAddQueueItem` payload) and the
  TestDataQueue payload concatenated queue names raw into JSON; a
  name containing `"` or `\` broke the JSON. Now escaped via
  `JsonSerializer.Serialize`.
- `OrchAPISession.GetSessionStats` URL ended with a stray single
  quote (`/api/Stats/GetSessionsStats'`) causing 404.

#### Other fixes

- `Start-OrchJob -RuntimeType` invalid no longer `throw`s; emits
  an `ErrorRecord` (InvalidArgument category) so `-WhatIf` /
  `-Confirm` / `-ErrorAction` apply.
- `Add-OrchCalendarDate -ExcludedDate` no longer drops today (UTC)
  on time zones ahead of UTC. The "drop past dates" filter compared
  UTC-stamped values against `DateTime.Today` (local); now compares
  against `DateTime.UtcNow.Date`.
- `Add-OrchFolderUser`: `First()` on an empty bulk-resolve result
  no longer throws `InvalidOperationException`; switched to
  `FirstOrDefault` and the per-row failure now surfaces a warning
  identifying which user couldn't be resolved.

### Changed

- **`Set-OrchAsset` per-row Description handling now merges across
  pipelined input rows** with priority `non-empty > "" > null`
  (last-writer-wins among non-empty). Practical effect:
  `Set-OrchAsset -Description ""` with no other rows clears the
  existing description (previously: preserved); CSV roundtrip is
  lossless because empty cells lose to the non-empty Description on
  the first row. The same merge rule applies to
  `Set-OrchCredentialAsset` and `Set-OrchSecretAsset`.
- `Invoke-OrchApi` caps the response-body read at 8 KB on HTTP
  errors. Previously the entire body was read just to surface a
  1024-character snippet — the user-facing error message is
  unchanged, but multi-MB HTML error pages no longer waste memory.
- **`Copy-OrchAsset` / `Copy-Item` no longer abort the batch
  when destination resources are missing.** When a referenced
  user, machine, queue, calendar, process, bucket, etc. is not
  present (or not assigned to the destination folder) in the
  target tenant, the cmdlet now emits a Warning and skips just
  the affected piece — one per-User value, one referenced
  calendar, or one whole asset — then continues with the
  remainder. Cross-tenant / cross-folder copies run under
  `$ErrorActionPreference='Stop'` complete the rest of the batch
  instead of stopping at the first gap. Unexpected internal
  errors still surface as `WriteError`.

### Internal

- **2026-05 review pass**: surfaced the Critical bugs above plus
  ~30 smaller items. Most have no Release-build effect (empty-catch
  diagnostic logging via `[Conditional("DEBUG")]`,
  `string.Compare(..., true)` → `StringComparison.OrdinalIgnoreCase`
  semantic-equivalent rewrite for ASCII identifiers, dead-code and
  commented-block removals, typo fixes). HTTP layer hardening:
  `OrchAPISession.HttpClient_Send` builds the response log block
  synchronously to avoid racing with the caller's `using` disposal
  of `HttpResponseMessage`; `AuthManager` wraps response messages
  in `using` and drops the unused `RequestRefreshToken`;
  `AsyncLogWriter` shutdown coordinates the synchronous wait with
  `DisposeAsync`'s CancelAfter window. Shared
  `OrchCsvHelper.CsvLine.Split` (RFC 4180) replaces the per-call
  `Split(',') + Trim('"')` in `LoadUserMappingCsv` and
  `TestUserMappingCsv` so quoted fields with embedded commas no
  longer split mid-field. Added `Tests\SelfContained.Tests.ps1`
  `Describe 'Regression-2026-05'` block (R1..R14) and
  `Tests\UnitTests\CsvLineTests.cs` (16 cases).
- Removed 1595 lines of dead multithreaded variants and orphan
  stubs.
- Removed 792 lines of `#if false` / commented-out alternative
  blocks.
- Extracted `RemoveDriveEntityCmdletBase<T>` /
  `RemoveFolderEntityCmdletBase<T>` for tenant- and folder-scoped
  `Remove-*` cmdlets.
- Extracted `DriveScopedCompleter<T>` and `TmProjectScopedCompleter<T>`
  base classes.
- Symmetric self / other base class for tenant-user completers.
- Added `ResolveDepth` / `ResolveSwitchParameter` helpers and
  migrated completer call sites.
- Replaced the `knownSwitchParameters` whitelist with reflection-
  based switch-parameter detection.
- Consolidated duplicate `StaticCandidate` classes.
- Shared `JobIdCompleter` between `Get-OrchJob` and `Open-OrchJob`.
- `OrchCache`: `volatile` + publish-after-init for race-free reads
  on weak memory models.
- Renamed `MyRegex` → `CommaSeparatedTokenRegex`.

### Build / Release

- `Build-Deploy.ps1` now tracked in repo.
- psd1 hygiene: declare `CompatiblePSEditions`, empty
  `VariablesToExport`.
- Release workflow's Assemble step mirrors `Build-Help.ps1` Step 4
  so MAML help is consistent between local and CI builds.
- `Staging/Examples/` is now packaged with the module (release
  workflow previously omitted the folder). The 11 retained scripts
  were also reviewed and refreshed: cmdlet renames from the `OrchPm*`
  → `Pm*` migration applied to `Remove-TenantUsersFromCsv.ps1`;
  `Send-PendingJobAlerts.ps1` and `Send-SuspendedJobAlerts.ps1` no
  longer track in-script state (so they survive a restart and
  cannot fall into a "miss the first mail, miss it forever" hole);
  `StartJob-WhenJobFailed.ps1` passes a real `[DateTime]` to
  `-CreationTimeAfter`. The broken `Add-TenantUser.ps1` (column
  schema mismatch + dead `Add-OrchPmMemberToPmGroup`) was removed
  rather than rewritten.

## [0.9.18.0] - 2026-04-28
### Added
- Webhooks: `Get-OrchWebhookEventType` (lists tenant event types) and `Test-OrchWebhook` (sends a Ping by name).
- Jobs: `Restart-OrchJob` (Faulted-only) and `Resume-OrchJob` (Suspended-only). Tab completion lists only the actionable jobs.
- Triggers: `Test-OrchTrigger` runs the server-side `ValidateProcessSchedule` pre-flight check and returns `IsValid` + `Errors` per trigger.
- Sessions: `Clear-OrchInactiveSession` bulk-deletes Disconnected / Unresponsive unattended sessions tenant-wide.
- Tasks (action center): `Get-OrchTask`, `Get-OrchTaskAcrossFolder`, `Set-OrchTask`, and `Remove-OrchTask`. `Get-OrchTaskAcrossFolder` resolves each task's actual folder PSPath so the pipeline routes correctly into per-folder cmdlets.

### Changed
- Parallelized `Get-OrchTaskAcrossFolder`, `Get-OrchUserSession`, and `Get-OrchRole` across drives/folders.
- Job tab completion: `Restart-OrchJob` / `Resume-OrchJob` / `Stop-OrchJob` now have separate state-scoped caches (Faulted / Suspended / Stoppable) so they no longer compete on a shared filter. Job tooltip leads with `Id`, then DateTimes, State, and the folder + process name (e.g. `Orch1:\Shared\InvoiceProcess`) so `-Recurse` Tab disambiguates folder.
- `dir -Recurse`: each Directory section stays contiguous (parent-grouped). Personal-workspace-first ordering at the drive root is preserved.

### Fixed
- HTTP response and HTTP client handles are now released properly. Long-running sessions no longer leak connection-pool resources.
- Fixed two concurrency races in tab-completion / cache code that could lose entries or corrupt internal state under parallel use.
- `Import-OrchLibrary` / `Import-OrchPackage`: empty or malformed server responses now return null instead of throwing a NullReferenceException.
- Removed two unreachable Format views.

## [0.9.17.0] - 2026-04-25
### Added
- Secret-typed assets (Orchestrator v20+): added `Set-OrchSecretAsset`, `Get-OrchSecretAsset`, and a type-agnostic `Remove-OrchAssetUserValue`. `Set-OrchSecretAsset` treats an empty `-SecretValue` as a silent no-op so CSV round-trip is safe (the API masks `SecretValue` on GET). Removing a per-robot entry on a Secret uses `Remove-OrchAssetUserValue` (the empty-delete convention from Text/Bool/Integer does not apply to Secret).

- `Get-OrchCredentialAsset`: new cmdlet returning only Credential-type assets, with its own `-ExportCsv`. The existing `Get-OrchAsset -ExportCredentialCsv` is unchanged.

- `Format-OrchQueueItem`: new pipeline formatter that groups queue items by `QueueDefinitionId` and emits one `Format-Table` per queue, flattening each item's `SpecificContent` keys into columns. Needed because `Format-Table` locks columns on the first object, silently hiding keys unique to later queues. Paired with a new `Expanded` ScriptProperty (ETS) on `QueueItem` — `Get-OrchQueueItem Q | ForEach-Object Expanded | Format-Table` is the single-queue shortcut.

- `Format-OrchTestDataQueueItem`: new pipeline formatter for test data queues, parsing `ContentJson` and promoting its top-level properties to columns. Groups by queue path; malformed JSON falls back to a raw column rather than aborting.

### Changed
- `Set-OrchAsset`: `-ValueType Credential` and `-ValueType Secret` are now silently skipped (previously `Credential` was skipped but `Secret` would error). Use the type-specific `Set-OrchCredentialAsset` / `Set-OrchSecretAsset`.

- `Get-OrchAsset -ExportCsv`: excludes `Credential` and `Secret` assets (same policy as before, extended to Secret).

- `Copy-OrchAsset`: now supports all five asset types. Secret was previously rejected by the server with "asset secret value cannot be null"; the copy now inserts a `!!!PLEASE UPDATE!!!` placeholder and emits a warning (same pattern as Credential). When the source asset has an `ExternalName` (vault reference), the placeholder is skipped so the vault link is preserved verbatim. This applies to both Global and per-robot UserValue paths.

- `Get-OrchQueueItem`: the 600ms inter-page rate-limit wait is now cancellable (`Ctrl+C` no longer blocks up to 600ms per iteration) and is skipped on the final page (a partial short page means no further API call is coming). Removed 60 lines of commented-out multi-threaded implementation and an unused `WriteQueryUnavailableWarning` method.

- `Get-OrchQueueItem` help: added examples and notes for the `Expanded` ScriptProperty and `Format-OrchQueueItem`.

### Fixed
- v20+ asset retrieval: `/odata/Assets` silently drops Secret-typed assets, while `/odata/Assets/.../GetFiltered` returns `CredentialUsername` as empty for Credential assets. Neither endpoint alone is sufficient. `GetAssets` now merges non-Secret from `/odata/Assets` with Secret-only from `GetFiltered?$filter=ValueType eq 'Secret'` on v20+ servers.

- `Get-OrchSecretAsset -ExpandUserValues`: the Global row was being filtered by `!string.IsNullOrEmpty(asset.Value)` but Secret's `Value` is always null (server masks it), so Global-scope Secret assets were never emitted in the expanded view. Switched the check to `HasDefaultValue`.

- `Copy-OrchAsset` UserValue Credential: the "Please update credential asset passwords" warning only fired for Global-level Credential copies. PerRobot-only Credential copies silently left `!!!PLEASE UPDATE!!!` without any indication. Now warns consistently for both scopes.

- `Copy-OrchAsset` UserValue machine-not-assigned: replaced the duplicate warning + error pair with a single warning (matching other `FindDstMachine` call sites).

### Removed
- Retired `Get-OrchTestDataQueueItemTable`. Use `Get-OrchTestDataQueueItem | Format-OrchTestDataQueueItem` instead. The old function misused the `Get-` verb (emitted format records, not data). Assumed unused; no aliases provided.

## [0.9.16.6] - 2026-04-24
### Fixed
- Install-Module of 0.9.16.5 failed with "authenticode signature of the file 'UiPathOrch.psd1' is not valid" on any machine where the self-signed code-signing certificate was not pre-trusted. Root cause: 0.9.16.5 signed `UiPathOrch.psd1` / `.psm1` / `.ps1xml` / `Functions/*.ps1` in addition to the DLL, and Install-Module verifies Authenticode signatures on script files. Fixed by narrowing the signing scope to `UiPathOrch.dll` only; script files are published unsigned. No functional change.

## [0.9.16.5] - 2026-04-23
### Fixed
- Get-PmLicenseInventory: JSON deserialization failed on tenants with consumable SKUs that report fractional allocation (e.g. AIU `allocated: 1451.8`). Widened `ProductAllocation.total` / `.allocated` from `int?` to `double?`.

### Changed
- Get-PmLicenseInventory / Get-PmLicenseContract: the list-view hint for nested collections no longer suggests `| Format-Table` — e.g. `ProductAllocations : 51 item(s) — use $_.productAllocations` and `Products : N item(s) — use $_.products`.

## [0.9.16.4] - 2026-04-22
### Added
- Get-PmLicenseAllocation: New cmdlet that returns per-tenant robot / runtime license allocations for a UiPath Automation Cloud organization (Robots & Services tab of Admin / Licenses). Parameters: -Tenant (wildcards, tab completion), -Path. Organization-scoped; Automation Cloud only.

- Get-PmLicenseInventory: New cmdlet that returns the organization-level license inventory dashboard, bundling five collections into one object: productAllocations, userLicensingBundles, entitlementUsages, availableServices, mlKeys. Parameters: -Path. Organization-scoped; Automation Cloud only.

- Get-PmLicenseContract: New cmdlet that returns the full license contract for an organization including purchased products, bundle templates, entitlements, ML service keys, and the embedded license payload (preserved verbatim). Parameters: -Path. Organization-scoped; Automation Cloud only.

### Changed
- Added Get-Help documentation for Get-PmLicense and the three new Pm license cmdlets.

## [0.9.16.3] - 2026-04-21
### Added
- Get-PmLicense: New cmdlet that returns tenant-level license inventory (allocated / inUse / total per UserBundle). Parameters: -License / -Code (wildcards, tab completion), -Path, -HasCapacity (allocated < total). Includes a formatted view with a usage bar.

### Changed
- Rewrote about_UiPathOrch help topic to reflect the current feature set.

- Added -UseDefaultEditor switch to Edit-OrchConfig.

### Fixed
- Suppress spurious PipelineStoppedException in WriteError when using Select-Object -First.

- Fixed Import-OrchQueueItem double-serialization bug.

- NavigationCmdletProvider review fixes: GetChildNames now writes the correct name / full-path / isContainer values, GetChildItems / GetChildNames honor Stopping mid-loop, GetItem has null guards, empty RenameItem / RemoveItem overrides were removed so the base class throws PSNotSupportedException, and several cmdlets gained [OutputType(typeof(void))].

### Internal
- Pinned .NET SDK to 8 via global.json.

## [0.9.16.2] - 2026-03-20
### Added
- Get-OrchPSDrive: IdentityUrl is now automatically derived from Root when not explicitly configured, so it is always available in Get-OrchPSDrive output. Previously it was null unless specified in the settings file.

- Get-OrchPSDrive: Added Claims property containing decoded JWT access token claims as a PSObject. Access individual claims via `$drive.Claims.prt_id`, `$drive.Claims.email`, etc. Timestamp claims (exp, iat, nbf, auth_time) are converted to local DateTime.

### Changed
- Refactored Update-OrchMachine to use HTTP PATCH with minimal payloads instead of DeepCopy + PUT. Only specified parameters are sent. Machine slots (UnattendedSlots, NonProductionSlots, TestAutomationSlots) can now be set to 0.

- Refactored Update-OrchUser to use per-property dirty flags instead of IEquatable comparison.

- Refactored Set-OrchAsset and Set-OrchCredentialAsset: extracted helper methods, replaced manual DeepCopy with shared utility, and improved PerRobot value lookup performance with Dictionary-based indexing.

- Removed IEquatable implementations from 14 entity types, no longer needed after the Update cmdlet refactoring.

- Major internal refactoring of the tab completion engine and parallel execution infrastructure.

### Fixed
- Fixed Set-OrchAsset Integer parse error message incorrectly saying "bool" instead of "integer".

- Fixed Set-OrchCredentialAsset: empty CredentialPassword no longer silently deletes the Global credential value. Previously, re-importing a CSV exported by Get-OrchAsset -ExportCredentialCsv (which contains empty password fields) could destroy existing credentials. Use Remove-OrchAsset to delete credential assets instead.

### Breaking Changes
- Set-OrchCredentialAsset: `-CredentialPassword ''` no longer deletes the Global credential value. This prevents accidental credential loss when re-importing exported CSVs. PerRobot entry deletion via `-UserName <user> -CredentialPassword ''` is unchanged.

- Update-OrchMachine: Machine slot parameters (UnattendedSlots, NonProductionSlots, TestAutomationSlots) now accept 0 as a valid value. Previously, 0 was treated as "not specified".

## [0.9.16.1] - 2026-03-18
### Added
- Added Set-OrchSetting cmdlet for updating tenant settings, with a Value completer that shows the current value.

- Added Getting Started guide (Docs/00-GettingStarted.md).

- Added CSV Export & Import guide (Docs/03-CsvExportImport.md).

- Added Pester integration tests (119 tests) covering Machine/Queue/Bucket/Asset CRUD, CSV export/import, cross-tenant copy, folder provider operations, error handling, wildcard support, and large-scale PerRobot asset operations.

### Changed
- Entra ID warning now only fires when the tenant has Entra ID integration and the user is not signed in via Entra ID. Previously it triggered for all non-Entra ID users regardless of tenant configuration.

- Improved cross-version migration compatibility: copy and create operations for processes, machines, webhooks, queues, assets, buckets, schedules, and roles now work with older API versions (including On-prem 20.10 / API v11).

- Improved Update cmdlets: Update-OrchQueue, Update-OrchTrigger, and Update-OrchProcessVersion now detect changes before making API calls, skipping no-op updates.

- Refactored Update-OrchMachine to use HTTP PATCH with minimal payloads instead of DeepCopy + PUT. Only specified parameters are sent. Machine slots (UnattendedSlots, NonProductionSlots, TestAutomationSlots) can now be set to 0.

- Refactored Update-OrchUser to use per-property dirty flags instead of IEquatable comparison.

- Refactored Set-OrchAsset and Set-OrchCredentialAsset: extracted helper methods, replaced manual DeepCopy with shared utility, and improved PerRobot value lookup performance with Dictionary-based indexing.

- Replaced undocumented EditRelease API with public PATCH and PutReleaseRetention endpoints.

- Sort target processes by name in Update-OrchProcess.

- Improved organization ID resolution by extracting it directly from the JWT token, avoiding extra API calls.

- Removed unused IEquatable implementations from 14 entity types.

### Fixed
- Fixed Entra ID warning appearing during tab completion by deferring display to cmdlet execution.

- Fixed Set-OrchAsset Integer parse error message incorrectly saying "bool" instead of "integer".

- Fixed Set-OrchCredentialAsset: empty CredentialPassword no longer silently deletes the Global credential value. Previously, re-importing a CSV exported by Get-OrchAsset -ExportCredentialCsv (which contains empty password fields) could destroy existing credentials. Use Remove-OrchAsset to delete credential assets instead.

- Fixed cache and retention comparison issues in Update cmdlets.

- Fixed Queue ReleaseId and Webhook Name for cross-version copy.

- Fixed role cache not clearing on error in CopyRoles, causing stale data.

- Fixed HTTP response body stream race between caller and async logger.

- Fixed minor resource disposal and thread pool issues.

### Breaking Changes
- Set-OrchCredentialAsset: `-CredentialPassword ''` no longer deletes the Global credential value. This prevents accidental credential loss when re-importing exported CSVs. PerRobot entry deletion via `-UserName <user> -CredentialPassword ''` is unchanged.

- Update-OrchMachine: Machine slot parameters (UnattendedSlots, NonProductionSlots, TestAutomationSlots) now accept 0 as a valid value. Previously, 0 was treated as "not specified".

### Docs
- Reorganized documentation: renamed MigrationGuide to 04-MigrationGuide.md, added cross-version migration guidance, library feed settings, Queue SLA auto-assignment, and user investigation procedure.

## [0.9.16.0] - 2026-03-08
### Added
- Added Import-OrchConfig cmdlet. Imports the configuration file and creates PSDrives for all enabled tenants. Use this to apply configuration changes without restarting the PowerShell session.

- Added New-OrchPSDrive cmdlet. Creates a new PSDrive in the current session without using the configuration file.

- Added Get-OrchConfigPath cmdlet. Returns the path to the UiPathOrch configuration file, allowing AI and scripts to directly read or edit it.

- Added Switch-OrchCurrentUser cmdlet. Opens an InPrivate browser window to re-authenticate with a different account. Useful when SSO auto-login prevents account switching.

- Added Entra ID login warning. When connecting to a drive, UiPathOrch checks the JWT token and displays a warning if the user is not signed in via Entra ID. For AD-integrated organizations, UiPathOrch automatically directs the user to the Entra ID login page during PKCE authentication.

- Added handling for deprecated Alerts API: returns an error on API v18+.

- (Experimental) Added New-OrchUserMappingCsv and Test-OrchUserMappingCsv cmdlets for cross-organization tenant migration with username mapping. New-OrchUserMappingCsv generates a CSV that maps source directory users to destination directory users by searching the destination directory. Test-OrchUserMappingCsv validates the mapping. All copy cmdlets now accept the -UserMappingCsv parameter to translate user references during migration.

### Fixed
- Fixed Cloud PKCE authorize URL to use the updated UiPath Identity Server endpoint with acr_values for organization routing.

- Fixed New-OrchProcess bugs: triple enumeration of packages, Description property leaking between calls, and nullable handling.

- Fixed parameter mutation bugs and ErrorId usage across 47 files.

- Fixed Pm cmdlets race condition by converting from parallel to sequential execution when sharing cache.

- Fixed Personal workspace exclusion across FolderUser, FolderMachine, and Test cmdlets.

- Fixed OutputType attributes: corrected ActionCatalog type, removed incorrect OutputType from Copy cmdlets.

- Fixed ShouldProcess messages: typos and naming consistency.

- Removed uipath.com domain check from Document Understanding and Test Manager drive creation, enabling Automation Suite environments.

### Docs
- Rewrote all cmdlet help documentation using PlatyPS v1. 238 markdown help files with standardized descriptions, 1030+ examples, parameter documentation.

- Removed legacy PDF documentation and Japanese-language help files. All documentation is now in English markdown format.

- Added AI-oriented guide documents (Docs/01-Essentials.md, 02-CmdletReference.md, 03-MigrationGuide.md) for module usage, cmdlet reference, and tenant migration procedures.

### Breaking Changes
- Update cmdlets (Set-Orch*) now treat empty string "" as an intentional value clear. Previously, empty strings were silently ignored.

## [0.9.15.5] - 2026-03-04
- Added Get-OrchTestCaseAssertion cmdlet. Retrieves test case assertions for a given test case execution. Supports pipeline input from Get-OrchTestCaseExecution and can download screenshots using the -ScreenshotPath parameter.

- Added Get-OrchLogLocation cmdlet. Returns the log folder path for a specified drive. Useful for scripting access to log files without opening File Explorer.

- Enhanced Get-OrchTestCaseExecution with new filter parameters: -Last, -StartTimeAfter, and -StartTimeBefore. Also added -TestSetExecutionName filter, -Skip and -First pagination parameters. When -PathTestSetExecutionName is specified, the corresponding TestSetExecution is now fetched automatically if not cached.

- Fixed a bug where Get-OrchJob, Get-OrchQueueItem, and Get-OrchTestCaseExecution could fail with HTTP 400 errors when filtering by a large number of robots or test set executions. The OData filter is now automatically split into batches to stay within API limits.

- Fixed on-premises OAuth authentication failing due to an extra tenant path segment being included in the identity server URL.

- Fixed Get-OrchTestCaseExecution returning duplicate results when used as pipeline input to itself.

- Get-OrchTestSetExecution now outputs cached results with a warning message when no filter parameters are specified, consistent with other cmdlets such as Get-OrchJob and Get-OrchLog.

- Fixed Get-OrchJob compatibility with older on-premises Orchestrator versions. On API versions earlier than 12, the Machine field is excluded from the $expand clause and the ProcessType filter is omitted, preventing request errors on unsupported versions. The threshold has been confirmed up through API version 13, and will be re-evaluated when version 14 is available.

- Fixed on-premises API requests to use X-UIPATH-TenantName header instead of embedding the tenant in the URL path. Also improved Cloud base URL calculation to derive the org path dynamically instead of relying on a hardcoded domain match.

- Tab completion and path resolution now canonicalize folder and project name casing from cache across all providers (Orchestrator, Document Understanding, Test Manager).

- Fixed SpecificPriorityValue being sent to Orchestrator API versions that reject it. The threshold has been raised from ApiVersion 12 to 14, and the value is now cleared after converting to JobPriority.

- Fixed RateLimiter resource leak by adding Dispose and removing unused Release method.

## [0.9.15.4] - 2026-01-13
- Fixed Get-OrchProcess -ExportCsv not retrieving process entry points correctly.

## [0.9.15.3] - 2026-01-08
- Added Get-PmAccessAllowedMember cmdlet to retrieve partition access policy members.

## [0.9.15.2] - 2025-12-05
- Added Remove-OrchEventTrigger cmdlet.

- Added -EntryPointPath alias to -Name parameter of Get-OrchTestCaseExecution.

- Fixed Get-OrchTestCaseExecution filtering by -Name.

- Fixed Get-ChildItem not working properly when there are no folders or projects.

## [0.9.15.1] - 2025-11-30
- DateTime properties now return local time instead of UTC, even when not using the default view. Previously, local time was only displayed in the default view. This is a breaking change if you were relying on UTC timestamps.

- Improved performance by removing ScriptBlock for timezone conversion from format views.

## [0.9.14.19] - 2025-11-20
- Added Get-TmTestExecution cmdlet. Note that this cmdlet is only available on the UiPathOrchTm drive. The UiPathOrchTm drive becomes automatically available when you add the TM.* scope. You can verify this with the Get-PSDrive cmdlet.

- Adjusted the default view for Get-TmRequirement to better align with the Automation Cloud display.

- Changed the default view for Get-TmProjectPermission from list view to table view.

## [0.9.14.18] - 2025-11-17
- Added Get-OrchEventTrigger, Enable-OrchEventTrigger, and Disable-OrchEventTrigger cmdlets to manage event triggers.

## [0.9.14.17] - 2025-10-21
- Added Get-PmAuthenticationSetting cmdlet to retrieve organization authentication settings.

## [0.9.14.16] - 2025-10-09
- Added Get-OrchProcessRequirement cmdlet.

## [0.9.14.15] - 2025-09-27
- Added Remove-OrchBucketItem cmdlet.

- Fixed Import-OrchBucketItem not escaping file names, which caused incorrect names when special characters were used.

- Improved Import-OrchBucketItem to set Content-Type based on file extension.

- Enhanced Import-OrchBucketItem and Export-OrchBucketItem cmdlets to support cancellation with Ctrl+C.

- Updated Get-OrchBucketItem default view to right-align the Size column for better readability.

## [0.9.14.14] - 2025-09-22
- Added Import-OrchBucketItem cmdlet. Upload multiple files at once with wildcards or comma-separated values. You can also re-upload files downloaded with Export-OrchBucketItem while preserving the original folder structure and bucket names.

## [0.9.14.13] - 2025-09-09
- In the Add-OrchFolderUser cmdlet, the API called to search for the user specified with the -UserName parameter has been switched. This change enables usernames containing hyphens (-) to be resolved correctly and improves overall stability. As a result of this update, group names specified with -UserName are now case-sensitive.
  - From: GET /api/DirectoryService/SearchForUsersAndGroups
  - To: POST /api/Directory/BulkResolveByName/{partitionGlobalId}

- Accordingly, in the Get-OrchFolderUser cmdlet, the CSV file output with the -ExportCsv parameter now records group UserName values with case sensitivity preserved.

## [0.9.14.12] - 2025-09-04
- Added the -DisplayName parameter to the Update-PmUser cmdlet, allowing the displayName property to be updated.

## [0.9.14.11] - 2025-08-21
- Added the Export-OrchBucketItem cmdlet. This cmdlet exports files contained in storage buckets to a local folder.

  ```powershell
  # Exports all files from all buckets in the current folder to the current directory on drive c:
  Export-OrchBucketItem

  # Exports all files from the specified bucket in the current folder
  Export-OrchBucketItem MyBucket

  # Exports the specified files from all buckets in the current folder
  Export-OrchBucketItem * MyFile*, YourPic*

  # Exports all files from all buckets across the tenant to the specified local folder
  Export-OrchBucketItem -Path Orch1:\ -Recurse -Destination c:\tmp
  ```

## [0.9.14.10] - 2025-07-30
- Improved the behavior of Risk Management parameters (-WhatIf, -Confirm, -Verbose) in the Add-OrchFolderMachine cmdlet. Previously, when multiple machines were specified for the -Name parameter, these parameters applied collectively. They now apply individually to each machine.

- Added the PropagateToSubFolders column to the default view of the Get-OrchFolderMachine cmdlet.

## [0.9.14.9] - 2025-07-24
- In the previous release, the validator for the -Type parameter of the Add-OrchUser cmdlet was not functioning as expected.

- Improved the implementation of certain validators by replacing the use of runtime type information with generic type parameters.

## [0.9.14.8] - 2025-07-23
- Fixed an issue where the -Recurse parameter of the Add-DuUser cmdlet was not functioning. You can now add users to all Document Understanding projects at once, as shown below:

  Orch1Du:\> Add-DuUser -Recurse DirectoryUser user1@uipath.com, user2@uipath.com 'DU Data Annotator'

- Note: The following usage, which has always worked as intended, achieves the same result:

  Orch1Du:\> Add-DuUser -Path * DirectoryUser user1@uipath.com, user2@uipath.com 'DU Data Annotator'

- Added validators to the following parameters to ensure that errors are raised for invalid values. This is especially helpful when using UiPathOrch via PowerShell.MCP, where LLMs cannot use completers:
  - The -Type parameter of cmdlets such as Get-OrchUser and Add-OrchUser
  - The -Last parameter of cmdlets such as Get-OrchJob and Get-OrchAuditLog

## [0.9.14.7] - 2025-07-17
- The GroupName column was missing from the CSV file output by Get-PmGroupMember -ExportCsv.

- Added -Name as an alias for the -GroupName parameter in cmdlets that handle PmGroup entities. Since the group name is included in the "name" property of the entity returned from the API, this change makes it easier to work with these cmdlets in pipelines. Affected cmdlets:
  - Get-PmGroup
  - Get-PmGroupMember
  - New-PmGroup 
  - Add-PmGroupMember
  - Remove-PmGroup
  - Remove-PmGroupMember
  - Copy-PmGroup
  - Move-PmGroupMember
  - Get-PmLicensedGroup
  - Add-PmLicenseToPmLicensedGroup
  - Remove-PmLicensedGroup
  - Remove-PmLicenseFromPmLicensedGroup
  - Remove-PmAllocationFromPmLicensedGroup

## [0.9.14.6] - 2025-07-10
- Some API version checks were inadvertently left in the test entity copy cmdlets in version 0.9.14.5. These checks have now been removed.

## [0.9.14.5] - 2025-07-10
- Previously, when connecting to a tenant whose Orchestrator API version was below 18, cmdlets for managing test entities would perform no actions (i.e., no API calls were made). This behavior was incorrect. These cmdlets now function normally regardless of the API version. The affected entities include:
  - OrchTestCase
  - OrchTestSet
  - OrchTestCaseExecution
  - OrchTestSetExecution
  - OrchTestSetSchedule
  - OrchTestDataQueue
  - OrchTestDataQueueItem

  If test entities are not enabled for the tenant, running these cmdlets results in errors. These errors are harmless and can be safely ignored. In the future, if a reliable way to detect whether test entities are enabled becomes available, we plan to restore the behavior that disables these cmdlets accordingly.

- The -Name parameter was not set as mandatory for the Enable-OrchFolderMachineInherit and Disable-OrchFolderMachineInherit cmdlets. This has been corrected.

## [0.9.14.4] - 2025-07-05
- The list of personal workspace folders is no longer retrieved when connecting to a tenant configured with a confidential application. This improves connection speed.

## [0.9.14.3] - 2025-07-03
- When searching Active Directory integrated with a tenant, it is now possible to search by values other than identityName. This improves the behavior of the following cmdlets:
  - Add-OrchUser
  - Add-OrchFolderUser
  - Add-PmLicenseToPmUser
  - Search-OrchDirectory

- For the following cmdlets, completion suggestions did not work as expected when pressing Ctrl+Space or Tab with input enclosed in double quotes:
  - Add-OrchFolderUser
  - Add-PmGroupMember
  - Add-PmLicenseToPmUser
  - Add-PmLicenseToPmLicensedGroup
  - Search-OrchDirectory

## [0.9.14.2] - 2025-07-01
- Calls to the GET /api/DirectoryService/SearchForUsersAndGroups endpoint are now made with appropriate rate limiting to respect the API quota. As a result, CSV files containing a large number of users can now be successfully imported using the following cmdlets:
  - Add-OrchUser
  - Add-OrchFolderUser
  - Copy-OrchUser
  - Add-PmLicenseToPmUser
  - Copy-Item

## [0.9.14.1] - 2025-06-30
- The *-Du* and *-Tm* cmdlets now also use the SessionState of their PSCmdlet context. This improves robustness.

## [0.9.14.0] - 2025-06-28
- When used together with PowerShell.MCP, the current location could sometimes not be resolved correctly. This issue was caused by using a cached SessionState in both the context of the PSCmdlet and external components such as parameter completers. The implementation has been modified so that the PSCmdlet context now refers to its own SessionState, resolving the issue.

## [0.9.13.6] - 2025-06-27
- When copying a trigger from a classic folder using the Copy-OrchTrigger cmdlet, the associated machine is now also taken into consideration. Robot accounts were already handled as of version 0.9.13.4, and this change complements that enhancement.

## [0.9.13.5] - 2025-06-27
- The Export-OrchLibrary cmdlet did not correctly resolve the current location when only a drive name was specified for the -Destination parameter. The following usage is now supported:

  ```powershell
  # Exports to the current location of the C: drive:
  Export-OrchLibrary -Path Orch1: YourLibrary 1.0.1 c:

  # Paths on FileSystem PSDrives, such as Temp:, are now handled correctly
  Export-OrchLibrary -Path Orch1: YourLibrary 1.0.1 temp:

  # If -Destination is omitted, exports to the current location of the last-used FileSystem drive:
  Export-OrchLibrary -Path Orch1: YourLibrary 1.0.1
  ```

  Note: This issue had already been addressed for Export-OrchPackage in version 0.9.9.1, but the fix for Export-OrchLibrary had been unintentionally omitted until now.

## [0.9.13.4] - 2025-06-26
- When copying a trigger using the Copy-OrchTrigger cmdlet, if the source folder is a classic folder, the ExecutorRobots property of the destination trigger is now constructed by referencing the classic robot from the source.

## [0.9.13.3] - 2025-06-26
- Improved the behavior of the New-OrchProcess cmdlet.
  - When the -EntryPoint parameter is specified, execution could fail in folders with a feed.
  - When the -Version parameter is not specified, the latest version is now selected automatically.

- The Remove-OrchPackage cmdlet did not previously clear the package cache after execution.

- In cmdlets that update entities, such as Update-OrchTrigger, even for parameters that require resolving names to IDs, omitted parameters no longer overwrite existing values with null. Existing values are now preserved instead.

## [0.9.13.2] - 2025-06-21
- The caches for PmLicensedUser and PmLicensedGroup are now also shared across tenants (PSDrives) that belong to the same organization.

## [0.9.13.1] - 2025-06-21
- The cache for PmGroup is now also shared across tenants (PSDrives) that belong to the same organization.

## [0.9.13.0] - 2025-06-20
- The caches for PmUser, PmRobotAccounts, PmExternalClients, and PmExternalApiResources are now shared across tenants (PSDrives) that belong to the same organization. This change improves performance, reduces memory usage, and ensures consistent behavior when working with multiple tenants within the same organization. Note that caches for other organization-level entities, such as PmGroup, are still managed separately per PSDrive.

- A ReleaseNotes.txt file has been added to the module installation folder. You can navigate to this folder using the Set-OrchLocation cmdlet.

## [0.9.12.12] - 2025-06-18
- The completer for the -EntryPoint parameter of New-OrchProcess did not work as expected when a non-latest version was specified for the -Version parameter.

## [0.9.12.11] - 2025-06-14
- Automation Cloud organization-user creation and deletion API has been deprecated, causing the Get-PmUser, Remove-PmUser, Update-PmUser and Copy-PmUser cmdlets to stop working. These cmdlets now invoke the new private API (version 19.0 remains unchanged), restoring correct operation.

- The New-PmUser, Remove-PmUser, Update-PmUser and Copy-PmUser cmdlets now clear the PmUser and PmGroup caches after execution to ensure data consistency.

## [0.9.12.10] - 2025-06-12
- Made HTTP log output asynchronous, improving performance.

- Updated the module manifest (UiPathOrch.psd1) so that RootModule now points to the .dll, slightly reducing Import-Module time.

- Changed the Get-OrchJob cmdlet to no longer support wildcards for the -State parameter; specifying an invalid state now throws a runtime error to prevent LLMs from passing unsupported values.

## [0.9.12.9] - 2025-06-06
- In entities returned from the Web API, fields that should have been Guids were sometimes returned in a non-Guid format, causing JSON deserialization to fail. Therefore, all of those fields have been changed to the string type. As a result, the reliability of JSON deserialization has been improved.

## [0.9.12.8] - 2025-06-06
- Corrected the CSV output of Get-OrchTrigger -ExportCsv, which had invalid values in the ExecutorRobots column.

- Enabled specifying machines inherited from a parent folder with the -MachineRobots parameter of Update-OrchTrigger.

- The validity check for tenant URLs specified in the configuration file was incorrect, causing URLs such as https://orchestrator.local/ to be mistakenly treated as invalid. MSI Orchestrator URLs can indeed take this form.

- Introduced a Get-OrchHelp cmdlet for LLM with MCP scenarios, intended to allow an LLM to learn how to use the UiPathOrch module. Please try the PowerShell.MCP module: https://www.powershellgallery.com/packages/PowerShell.MCP

## [0.9.12.7] - 2025-06-02
- In Get-OrchUser -ExportCsv, if an Exp* property (for example, ExplicitMayHaveUserSession or ExplicitMayHaveRobotSession) is null, the corresponding May* column (for example, MayHaveUserSession or MayHaveRobotSession) now falls back to its original value.

## [0.9.12.6] - 2025-05-20
- When copying queues using the Copy-Item or Copy-OrchQueue cmdlet, if the source queue does not have StaleRetention policy configured, the destination queue is now automatically set to Delete as the StaleRetentionAction and 180 as the StaleRetentionPeriod.

- The Update-OrchUser cmdlet could fail with an error when ExecutionSetting parameters (-ES_*) were specified. This was because the ExecutionSetting values were applied to both the UnattendedRobot and RobotProvision properties of the user, regardless of whether those properties were present. This behavior has been adjusted: -ES_* parameters are now only applied to UnattendedRobot and RobotProvision if they are not null.

## [0.9.12.5] - 2025-05-18
- Added support for the -Name (alias: -FirstName) and -Surname (alias: -LastName) parameters to the Update-OrchUser cmdlet, enabling user name updates. Please note that these parameters are effective only in MSI Orchestrator. In Automation Cloud, specifying these parameters has no effect.

## [0.9.12.4] - 2025-05-17
- The Add-OrchUser cmdlet could previously fail when attempting to add robot accounts. This issue has been resolved by explicitly setting the following property values when adding a robot account:

  - "MayHavePersonalWorkspace": false
  - "MayHaveRobotSession": false
  - "MayHaveUnattendedSession": true
  - "MayHaveUserSession": false

## [0.9.12.3] - 2025-05-16
- Folder cache was not properly cleared in some cases after executing Clear-OrchCache, or after creating or deleting folders. This issue was introduced in version 0.9.12.2.

- Copy-Item and Copy-OrchQueue failed to copy queues from Automation Cloud to MSI Orchestrator. This issue was introduced in version 0.9.10.7.

- New cmdlets Copy-OrchQueueItem and Remove-OrchQueueItem have been added.
  - Copy-OrchQueueItem copies all queue items with the status New to a queue with the same name in a different folder, and outputs the items that were successfully copied. The output can be piped to Remove-OrchQueueItem to remove those items from the source queue, preventing duplicate transaction processing.

  - Remove-OrchQueueItem outputs any items that failed to be removed. These can be exported to a CSV file using Export-Csv, and later re-imported to retry deletion. To use Remove-OrchQueueItem independently, specify the queue name and the IDs of the items to remove.

- Below is an example of using the Copy-OrchQueueItem and Remove-OrchQueueItem cmdlets:
  - To move items from the MyQueue queue in the \Shared folder to a queue with the same name in the \dst folder:
    PS Orch1\Shared> Copy-OrchQueueItem MyQueue \dst | Remove-OrchQueueItem | Export-Csv c:itemsToRemove.csv -Encoding utf8BOM

  - To move items from all queues in one tenant to the corresponding queues in another tenant with the same folder structure:
    PS Orch1:\> Copy-OrchQueueItem -Recurse * Orch2:\ | Remove-OrchQueueItem | Export-Csv c:itemsToRemove.csv -Encoding utf8BOM

- Note: The Copy-OrchQueueItem cmdlet does not copy the queues themselves. If the destination folder does not contain a queue with the same name, you need to copy the queues beforehand using the Copy-OrchQueue cmdlet. Keep in mind that Copy-OrchQueue copies only the queue definitions. It does not copy the items contained in the queues.
  - To copy all queues in the \Shared folder to the \dst folder:
    PS Orch1\Shared> Copy-OrchQueue * \dst

  - To copy all queues in a tenant to another tenant that has the same folder structure:
    PS Orch1:\> Copy-OrchQueue -Recurse * Orch2:\

- To ensure safe operation, it is strongly recommended to disable triggers and stop automation processes in both source and destination folders before copying queue items.
  - To disable all triggers in both Orch1: and Orch2::
    PS> Disable-OrchTrigger -Path Orch1:\,Orch2:\ -Recurse *

## [0.9.12.2] - 2025-05-02
- Fixed an issue where CSV files generated by Get-OrchAsset -ExportCsv became invalid if the asset description contained commas.

- Fixed an issue where the folder output order was incorrect when using Get-ChildItem -Recurse or specifying the -Recurse parameter with other cmdlets (such as Get-OrchAsset or Get-OrchProcess), if the target was a Personal Workspace containing subfolders.
  - Note: The Personal Workspace can contain subfolders if a Solution has been deployed within it.

- You can now use the proxy settings configured in Internet Options. To enable this, update the configuration file as follows:

  "Proxy": {
    "UseDefaultWebProxy": true,
    "Enabled": true
  },

  - Other properties within the "Proxy" section, such as "Url" and "BypassProxyOnLocal", can remain unchanged. These properties will be ignored when "UseDefaultWebProxy" is set to true.

## [0.9.12.1] - 2025-04-25
- Added Copy-PmGroup and Copy-PmExternalApplication cmdlets.

- The following cmdlets are now available for copying organization entities. When all of these cmdlets are executed, the final set of entities created in the destination will be the same, regardless of the order in which the cmdlets are run:
  - Copy-PmUser
  - Copy-PmRobotAccount
  - Copy-PmExternalApplication 
  - Copy-PmGroup

- All of these cmdlets share the same usage pattern. For example, the following command copies all local users from Orch1: to Orch2:

  PS Orch1:\> Copy-PmUser * Orch2: -WhatIf

  - If the output looks correct, remove the -WhatIf switch and run the command again.

  - Note: If Orch1: and Orch2: belong to the same organization, no action will be taken.

- Copy-PmUser, Copy-PmRobotAccount, and Copy-PmExternalApplication automatically add copied entities to groups with the same name in the destination. If no such group exists, one is automatically created.

- Copy-PmGroup automatically adds local users, robot accounts, external applications, directory users, and directory groups with the same name to the copied group. However, if such entities with the same names do not exist in the destination, they will not be created automatically.

- Added Remove-PmExternalApplication cmdlet.
  - Note: External applications currently in use for the UiPathOrch connection cannot be deleted unless the -Force switch is specified.

- Updated Remove-PmUser to prevent deletion of users currently used for connecting via non-confidential apps in UiPathOrch.

- Fixed an issue where New-OrchProcess did not work in environments using API versions earlier than 19. This issue was introduced in version 0.9.10.9.
  - Note: You can check the Web API version of the target Orchestrator using the Get-OrchPSDrive cmdlet.

- Triggers copied with Copy-OrchTrigger or Copy-Item are now always created as disabled, even if the source trigger is enabled.

- When using the Orch-UpdateUser cmdlet with parameters that modify ExecutionSettings (such as -ES_TracingLevel or -ES_StudioNotifyServer), the settings were previously applied only to the user's UnattendedRobot property. Now, the cmdlet also applies the same values to the user's RobotProvision property. Additionally, these property updates are no longer applied to users whose Type is DirectoryExternalApplication.

## [0.9.12.0] - 2025-04-14
- Cmdlets whose noun names begin with OrchPm (such as Get-OrchPmUser) can now also be used with PSDrives provided by the UiPathOrchDu and UiPathOrchTm providers. These drives are automatically mounted when the corresponding scopes (Document Understanding and Test Manager, respectively) are configured in the settings file. After importing the UiPathOrch module, you can verify the mounted drives using the Get-PSDrive cmdlet.

- Along with this update, the cmdlet names with the OrchPm noun prefix have been renamed to use the shorter Pm prefix instead. For example, Get-OrchPmUser is now renamed to Get-PmUser. We apologize for any inconvenience caused by this change.

## [0.9.11.4] - 2025-04-12
- The Copy-OrchPmRobotAccount cmdlet now automatically creates groups with the same names in the target organization if the groups to which the robots being copied belong do not already exist there.

## [0.9.11.3] - 2025-04-11
- Starting with version 0.9.9.7, you can add a UiPathOrch PSDrive using the New-PSDrive cmdlet. Version 0.9.11.3 (this release) adds support for using a personal access token (PAT) via the new -AccessToken parameter. This allows you to mount a PSDrive without a configuration file or an external app registration:

  PS> New-PSDrive -PSProvider UiPathOrch -Name Orch1 -Root https://cloud.uipath.com/YOUR_ORGANIZATION/YOUR_TENANT -AccessToken YOUR_ACCESS_TOKEN -OAuthScope "OR.Folders OR.Users OR.Settings"

  - For the -AccessToken parameter, specify either a personal access token or a bearer token obtained through another external application connection.

  - Please note that the parameter name for specifying the scopes is -OAuthScope, not -Scope.

  - You must run Import-Module UiPathOrch before executing the New-PSDrive cmdlet.

  - If no configuration file exists when the module is imported, one is automatically created and opened in Notepad. To suppress this behavior, set the environment variable UIPATHORCH_SUPPRESS_CONFIG_CREATION to 1 before importing UiPathOrch. This feature was introduced in UiPathOrch 0.9.9.7.

- Added the -Robot filter parameter to the Get-OrchJob cmdlet. Wildcards are supported.
  - The robot names specified in the -Robot parameter of Get-OrchJob can also be constructed from the output of Get-OrchRobot. For example:

    PS Orch1:\Shared> Get-OrchJob -Robot (Get-OrchRobot | ? Type -eq Unattended | select -ExpandProperty Name) -State Successful -OrderBy EndTime

- Included the robot Name in the default view of the Get-OrchRobot cmdlet.

- The roleAssignmentDtos property, which belongs to the DuUser entity, was not properly expanded when the output of the Get-DuUser cmdlet was piped to ConvertTo-Json.

- Operations on the Document Understanding entity cache were not thread-safe.

## [0.9.11.2] - 2025-04-05
- You can now connect to a tenant using a personal access token. Please configure the PSDrive in your settings file as follows:

"PSDrives": [
  {
    "Name": "Orch1",
    "Root": "https://cloud.uipath.com/YOUR_ORGANIZATION/YOUR_TENANT",
    "AccessToken": "YOUR_PERSONAL_ACCESS_TOKEN",
    "Scope": "OR.Folders.Read OR.Settings.Read OR.Users.Read",
    "Enabled": true
  }
]

- The following changes were made to prevent meaningless errors from appearing when copying entities:
  - In the Copy-OrchRole and Copy-Item cmdlets, if the source role is static and a role with the same name already exists in the destination tenant, the role is now skipped.  

  - In the Copy-OrchCredentialStore cmdlet, if the source credential store is "Orchestrator Database" and a credential store with the same name exists in the destination, the credential store is now skipped.

## [0.9.11.1] - 2025-04-01
- Improved the behavior of the Add-OrchPmGroupMember cmdlet.
  - When more than 21 usernames were specified simultaneously with the -UserName parameter, the operation would fail. Since this cmdlet aggregates multiple rows from the imported CSV file before calling the API, this issue could occur in practice.

  - This issue was due to restrictions of the following endpoint: POST /api/Directory/BulkResolveByName/{partitionGlobalId}

  - When calling this endpoint with a large number of usernames, the call is now automatically split into batches of 20 users to work around this limitation.

- In the provider for UiPathOrchDu (the Document Understanding PS drive), outputs from Get-ChildItem and from other cmdlets that use the -Recurse parameter (such as Get-DuUser) are now sorted by project name.

- For the -Name parameter of the Remove-DuRoleFromDuUser cmdlet, it was previously required to pass the displayName. However, in the Name column of Get-DuUser -ExportCsv, the email is output for users while groups and apps output the displayName. To maintain consistent behavior, when removing a user from a project using Remove-DuRoleFromDuUser, the -Name parameter now requires specifying the user's email.

## [0.9.11.0] - 2025-03-31
- The Copy-Item cmdlet (alias: copy) now copies not only folder entites but also tenant entities. With this change, a lift & shift tenant migration can be completed with a single command:

  PS> copy src:\ dst:\ -Recurse

- When copying the root folder of source tenant to the root folder of destination tenant, Copy-Item copies all tenant entities. These tenant entities include:
  - Libraries
  - Packages
  - Credential Stores
  - Roles
  - Users
  - Machines
  - Calendars
  - Webhooks

  Note that the process for copying the tenant entities mentioned above shares its implementation with cmdlets such as Copy-OrchLibrary and Copy-OrchPackage.

- Below are some examples of executing the Copy-Item cmdlet (copy is an alias for Copy-Item):
  - To copy all tenant entities and all folders:
    PS> copy src:\ dst:\ -Recurse

  - Select the types of tenant-level entities you want to copy, and the folders you wish to include:
    PS> copy src:\ dst:\ -Recurse -Confirm

  - To copy all tenant entities (without copying folders):
    PS> copy src:\ dst:\

  - To copy all folders (without copying tenant entities) Note that in this example, the src:\ root folder itself is not included in 'src:\*':
    PS> copy src:\* dst:\ -Recurse

  - Copy only all folders, without copying any tenant-level entities and the entities contained in the folders:
    PS> copy Orch1:\ Orch2:\ -Recurse -ExcludeEntities

- For detailed instructions on tenant migration and how to use the Copy-Item cmdlet, please refer to the user guide pdf around page 42 in the module's installation directory. To navigate to the installation directory, run the Set-OrchLocation cmdlet after importing this module.

- The Copy-OrchLibrary cmdlet incorrectly copied all libraries regardless of the names specified with the -Id parameter. This bug was introduced in version 0.9.7.10.

## [0.9.10.9] - 2025-03-27
- Two parameters, -A4R_Enabled and -A4R_HealingEnabled, have been added to the New-OrchProcess and Update-OrchProcess cmdlets.

- Additionally, two columns, A4R_Enabled and A4R_HealingEnabled, have been added to the CSV file output by Get-OrchProcess -ExportCsv. This CSV file can be imported using the New-OrchProcess and Update-OrchProcess cmdlets.

## [0.9.10.8] - 2025-03-26
- The New-OrchPmUserBulk cmdlet has been renamed to New-OrchPmUser. We apologize for any inconvenience this change may cause.

- The New-OrchPmUser cmdlet now automatically creates groups with the specified name if the name provided to the -GroupName parameter does not contain wildcard characters and no existing group matches the name.

- The Get-OrchPmUser cmdlet now supports the -ExportCsv parameter. The CSV file generated with this parameter can be imported using the New-OrchPmUser cmdlet.

- The -Name parameter completer now works as expected for the following cmdlets:
  - New-OrchBucket
  - New-OrchMachine
  - New-OrchQueue
  - New-OrchTrigger
  - New-OrchPmGroup

## [0.9.10.7] - 2025-03-18
- Properties have been added to several entity types to align with Automation Cloud API version 19.0.

- Modified the Copy-Item, Copy-OrchProcess, Copy-OrchQueue, New-OrchProcess, Update-OrchProcess, New-OrchQueue, and Update-OrchQueue cmdlets:
  - In Automation Cloud tenants, processes and queues can no longer be created with RetentionAction set to "None". As a result, these cmdlets will now automatically replace "None" with "Delete" if specified.

- The Get-OrchProcess -ExportCsv and Get-OrchQueue -ExportCsv now include the following three columns in the generated CSV files:
  - StaleRetentionAction
  - StaleRetentionPeriod
  - StaleRetentionBucket

  - The New-OrchProcess, Update-OrchProcess, New-OrchQueue, and Update-OrchQueue cmdlets now support parameters with the same names as the newly added CSV columns.

- Improvements to the Update-OrchProcess cmdlet:
  - When specifying -EntryPoint parameter, its EntryPointId was incorrectly being set to RetentionBucketId.

  - The -RetentionBucket parameter's completer no longer displays read-only storage buckets.

- Fix for Get-OrchQueue -ExportCsv:
  - Previously, retention policies were not included in the exported CSV.

- Configuration validation improvement:
  - A warning is now displayed if the AppId specified in the configuration file is invalid (e.g., an empty string or not in GUID format).

## [0.9.10.6] - 2025-03-14
- Some parameters of update-related cmdlets did not support CSV file imports, such as the -Path parameter of the Set-OrchAsset cmdlet.

- Additionally, almost all Get-related cmdlets did not support CSV file imports. All affected cmdlets, including Get-OrchAsset and Get-OrchPackage, have been updated to allow imports from CSV files.

- Please note that, at this time, switch parameters do not support CSV imports.

## [0.9.10.5] - 2025-03-12
- The -ExportCsv parameter of the Get-OrchUser cmdlet now outputs the values of Explicit* in the May* columns of the generated CSV file. It appears that Explicit* contains individual user settings, whereas the May* columns reflect the results of the union of privileges model (which includes both the user's permissions and those of the groups the user belongs to). Since the CSV file generated by the -ExportCsv parameter is intended for import using the Add-OrchUser / Update-OrchUser cmdlets, having the Explicit* values in the May* columns ensures consistent behavior.

- Added the Get-OrchUserPrivilege cmdlet. This cmdlet corresponds to the information displayed in the Summary card on the user editing screen after the introduction of the union of privileges model in the web interface. Please note that the Web API called by this cmdlet is currently private, so there is a possibility that this cmdlet may stop working in the future.

## [0.9.10.4] - 2025-03-08
- The Add-DuRoleToDuUser cmdlet has been renamed to Add-DuUser. Additionally, this cmdlet now allows adding directory users and groups to projects.

- The -ExportCsv parameter has been added to the Get-DuUser cmdlet. The CSV file generated using this parameter can be imported with the Add-DuUser cmdlet. Please note that the import destination is the project specified in the Path column of the CSV file. This can be overridden by specifying the -Path parameter in the command line. Use the following command:

  PS Orch1Du:\YourProj> Import-Csv c:ExportedDuUsers.csv | Add-DuUser -Path . -WhatIf

- The Path property values in entities output by the Get-DuUser, Get-DuDocumentType, Get-DuClassifier, and Get-DuExtractor cmdlets were incorrect.

- The output format of the Get-OrchPmGroupMember cmdlet could be inconsistent.

- The Add-OrchPmGroupMember cmdlet did not always function as intended.

- The Update-OrchUser cmdlet now allows unassigning all roles from users.
  - To unassign all roles from specified user:
    PS Orch1:\> Update-OrchUser <user name> -Roles ""

  - To unassign all roles from all users, robots, groups and applications:
    PS Orch1:\> Update-OrchUser * -Roles ""

  - To unassign all roles from users only (excluding robots, groups, and applications):
    PS Orch1:\> Get-OrchUser -Type DirectoryUser | select UserName | Update-OrchUser -Roles ""

## [0.9.10.3] - 2025-02-26
- Improved the behavior of the Add-OrchUser cmdlet.
  - Fixed an issue where the -MayHaveUserSession parameter was not functioning.

  - Added the -IsExternalLicensed parameter.

  - Previously, the following parameters were not included in the payload when not specified. This has been changed so that they are now automatically included as false when not specified. This change makes the default values more intuitive and user-friendly in the Union of privileges model, where false is a more natural default. Additionally, this change aligns the behavior with the web interface.
    - -MayHaveRobotSession
    - -MayHaveUnattendedSession
    - -MayHavePersonalWorkspace
    - -MayHaveUserSession
    - -RestrictToPersonalWorkspace
    - -IsExternalLicensed

- In line with the above changes, the -ExportCsv parameter of the Get-OrchUser cmdlet has been modified to include the IsExternalLicensed column in the output.

- Added the -IsExternalLicensed parameter to the Update-OrchUser cmdlet also.

## [0.9.10.2] - 2025-02-25
- HTTP logging is now available.
  - Add the following to the global settings in the configuration file. This can be overridden in each PSDrive setting.
    "Logging": {
      "Level": "Info",
      "Enabled": true
    }

  - The following values can be specified for Level:
    - Error: Outputs logs equivalent to Verbose only when the response status is not 200.
    - Info: Outputs a one-line summary for all requests and responses.
    - Trace: Outputs only request headers that contain "UIPATH" in their name, along with the request body and response body.
    - Verbose: Outputs all request headers, request bodies, response headers, and response bodies.

  - The folder where logs are output can be opened using the Open-OrchLogLocation cmdlet.

- The completer for the -InputArguments parameter of Start-OrchJob cmdlet now functions correctly.

- Fixed an issue where an incorrect message was displayed when an error occurred during library and package downloads.

- The Get-OrchPSDrive cmdlet did not output automatically configured HttpListener when HttpListener was not specified in the configuration file.

## [0.9.10.1] - 2025-02-15
- The Add-DuRoleToDuUser and Remove-DuRoleFromDuUser cmdlets have been added.  
  - These cmdlets are used to assign and remove roles for users in Document Understanding projects.

  - Please note that these cmdlets operate on folders (projects) in the PS drives of the UiPathOrchDu provider as their target.  

  - The PS drives of the UiPathOrchDu provider are automatically mounted when scopes starting with Du are specified in the configuration file.

## [0.9.10.0] - 2025-02-14
- It now works with PowerShell 7 on Linux. Its functionality was verified on Ubuntu.

- The configuration file template's newline characters were set to CRLF, but have been changed to LF.

## [0.9.9.7] - 2025-02-12
- The New-PSDrive cmdlet is now supported, allowing you to connect to a PSDrive for a tenant not listed in the configuration file. Use the following command:

  PS> New-PSDrive [-Name] <driveName> [-PSProvider] UiPathOrch [-Root] <tenant url> -AppId <appId> -AppSecret <appSecret> -OAuthScope "OR.Users OR.Folders"

  - Parameter names enclosed in square brackets can be omitted. Note that the New-PSDrive cmdlet has a built-in -Scope parameter, which only accepts Global, Local, or Script. Therefore, use the -OAuthScope parameter instead of -Scope to specify the Scope.

  - Additionally, parameters not specified in the New-PSDrive cmdlet will be automatically supplemented from the global settings in the configuration file, if available.

- When executing Import-Module UiPathOrch, if the configuration file does not exist, it is automatically created and opened in Notepad. However, this behavior can be inconvenient when running UiPathOrch scripts in unattended environments. To suppress this behavior, set the environment variable UIPATHORCH_SUPPRESS_CONFIG_CREATION to 1. In this case, use the New-PSDrive cmdlet to connect to a tenant.

- There were some items in the global settings of the configuration file that were not effective, such as Root and AppId. Now, all items that can be specified for a PSDrive can also be included in the global settings.

## [0.9.9.6] - 2025-02-11
- Comments can now be written in the configuration file.

- As a result, the explanations previously written in the `Description` field of the configuration file template have been moved to comments.

- The configuration file template has been localized. It currently supports the following seven languages:  
  - German
  - English
  - French
  - Japanese
  - Korean
  - Romanian
  - Turkish

- The configuration file must be encoded in UTF-8. To ensure this, a UTF-8 BOM (Byte Order Mark) has been added to the configuration file templates. This should help prevent character corruption when adding local characters to the configuration file.

- To obtain the configuration file template, delete (or rename for backup) the existing configuration file and then run Import-Module UiPathOrch. If no configuration file exists, it will be automatically created from the template.

## [0.9.9.5] - 2025-02-07
- If the Scope was too long in the configuration file, the system failed to connect to the tenant. To address this, we have added a process to automatically shorten the Scope appropriately.
  - If both OR.XXX.Read and OR.XXX.Write are present, they will be replaced with OR.XXX.

  - If OR.XXX is included, OR.XXX.Read and OR.XXX.Write will be excluded.

- Note that the shortened Scope is not automatically written back to the configuration file. To check the shortened Scope, run the following command:
  PS> Get-OrchPSDrive <drive name> | select -ExpandProperty Scope

- Additionally, Get-OrchPSDrive cmdlet outputs various drive-related information, such as BearerToken, TenantId, and PartitionGlobalId. You can retrieve this information with:
  PS> Get-OrchPSDrive | select *

  - This behavior remains unchanged from previous versions.

- If a candidate displayed by the completers contains a comma, it is now automatically enclosed in single quotes.

## [0.9.9.4] - 2025-02-01
- Added the -HostFeed switch parameter to the following cmdlets. When specified, it retrieves library packages from the host feed.
  - Get-OrchLibrary
  - Get-OrchLibraryVersion

## [0.9.9.3] - 2025-01-31
- The Get-OrchLog cmdlet now includes the -JobKey parameter, making it easier to retrieve logs for a specific job.
  - The completer for the -JobKey parameter suggests cached job Key values as candidates.

  - The -JobKey parameter also has an alias, -Key, so you can execute this cmdlet in the following way as well:

    PS Orch1:\Shared> Get-OrchJob -First 1 | Get-OrchLog

    This works because the Path and Key properties of the job entity are redirected to the parameters of Get-OrchLog with the same names.

## [0.9.9.2] - 2025-01-27
- Improved Get-OrchTrigger cmdlet:
  - Fixed an issue where the ExecutorRobots property of triggers could not be retrieved. This required calling an additional endpoint:
    GET /odata/ProcessSchedules/UiPath.Server.Configuration.OData.GetRobotIdsForSchedule(key={processScheduleId}) 

  - When the -ExpandDetails or -ExportCsv switch parameter is specified, the above endpoint is called along with following endpoint. The behavior of calling this endpoint remains unchanged from previous versions:
    GET /odata/ProcessSchedules({processScheduleId})

  - As part of this fix, the CSV file output by Get-OrchTrigger -ExportCsv now include an ExecutorRobots column. Additionally, the MachineRobots column is now serialized in a more appropriate format.

  - Triggers are no longer retrieved from personal workspace folders when the Orchestrator Web API version is below 12, as attempting to do so would result in an error.

- Improved New-OrchTrigger and Update-OrchTrigger cmdlets:
  - Added the -ExecutorRobots parameter, allowing better reflection of trigger dynamic allocation and account mapping information.

  - Enhanced the completers for the -ExecutorRobots and -MachineRobots parameters.
    - For New-OrchTrigger, the completers display available combinations of values, and multiple values can be specified using a comma-separated format.
    - For Update-OrchTrigger, the completers show the values configured for the specified triggers.

- Improved Copy-OrchTrigger and Copy-Item cmdlets:
  - Improved the ability to copy trigger dynamic allocation and account mapping information. However, to fully copy these, the destination folder must have the appropriate robots and sessions configured.

  - Fixed an issue where copying a trigger with a StopProcessDate in the past would fail. Now, if the date is in the past, it is removed, and the trigger is copied with Enabled set to false.

  - When copying queue triggers, the operation now fails if a queue with the same name does not exist in the destination folder.

  - The Copy-Item cmdlet now copies entities in alphabetical order by their names.

- Fixed an issue in the Copy-OrchCalendar cmdlet where all calendars were copied regardless of the value specified in the -Name parameter.

- Improved Add-OrchFolderMachine cmdlet:
  - When the target folder is a personal workspace folder, the cmdlet now skips processing. For example, when adding a machine to all folders directly under the root, unnecessary errors are no longer output:

    PS Orch1:\> Add-OrchFolderMachine -Path * MyMachine

- Improved Get-ChildItem cmdlet (alias: dir):
  - When the Orchestrator API version is outdated and does not return the FolderType property, this cmdlet now sets this property to either Personal or Standard (simulated). This is useful when writing .ps1 scripts to process folders.

- Addressed an issue where inappropriate warnings about OAuth scopes were output when connecting to Orchestrator without OAuth support using a username and password.

- Added three cmdlets for searching the directory service. Each cmdlet corresponds to a different endpoint:
  - Search-OrchDirectory cmdlet
    Endpoint: GET /api/DirectoryService/SearchForUsersAndGroups

  - Search-OrchPmDirectory cmdlet
    Endpoint: GET /api/Directory/Search/{partitionGlobalId}

  - Resolve-OrchPmDirectoryNameBulk cmdlet
    Endpoint: POST /api/Directory/BulkResolveByName/{partitionGlobalId}

## [0.9.9.1] - 2025-01-15
- The Get-OrchPmGroupMember cmdlet has been added.
  - This cmdlet outputs the same content as the existing Get-OrchPmGroup -ExpandMembers.

  - As part of this change, the -ExpandMembers parameter has been removed from the Get-OrchPmGroup cmdlet, and the functionality to expand members has been migrated to the Get-OrchPmGroupMember cmdlet.

  - This update enables more intuitive operations, as follows:
    - The output of Get-OrchPmGroup -ExportCsv can be imported into the New-OrchPmGroup cmdlet.
    - The output of Get-OrchPmGroupMember -ExportCsv can be imported into the Add-OrchPmGroupMember cmdlet.

- The Export-OrchPackage cmdlet has been improved.
  - When a relative path for the -Destination parameter is specified, the path was previously resolved using the process's current working directory, which could lead to unintended behavior.

  - The relative path is now correctly resolved using the current location of the PSDrive.

  - For example, the following command previously failed to write the packages to the correct location. This issue has now been resolved. The first wildcard represents the package name, and the second wildcard represents the version number:

    PS Orch1:\> Export-OrchPackage -Recurse * * c:

  - If the -Destination parameter is not specified, the packages are exported to the current location of the last-used FileSystem drive. This behavior remains unchanged from previous versions.

- The Import-OrchLibrary and Import-OrchPackage cmdlets have also been improved.
  - These cmdlets previously did not function correctly when a relative path was specified for the -Source parameter.

## [0.9.9.0] - 2025-01-13
- Several cmdlets with the verb Add have been renamed to use the verb New. When these cmdlets were initially created, the verb Add was chosen to align with the terminology used in the Orchestrator web interface, as it was considered more intuitive at the time. However, as the number of cmdlets increased, it was determined that New and Add should be used appropriately based on their functionality. We apologize for any inconvenience caused by this change.

- List of cmdlets renamed to use the verb New:
  - Add-OrchMachine -> New-OrchMachine
  - Add-OrchBucket -> New-OrchBucket
  - Add-OrchProcess -> New-OrchProcess
  - Add-OrchQueue -> New-OrchQueue
  - Add-OrchTrigger -> New-OrchTrigger
  - Add-OrchPmUserBulk -> New-OrchPmUserBulk
  - Add-OrchPmGroup -> New-OrchPmGroup

List of cmdlets where the verb was not changed:
  - Add-OrchUser 
  - Add-OrchCalendarDate
  - Add-OrchMachineClientSecret
  - Add-OrchFolderMachine
  - Add-OrchFolderUser
  - Add-OrchRoleToFolderUser
  - Add-OrchAssetLink
  - Add-OrchPmGroupMember
  - Add-OrchPmLicenseToPmLicensedGroup
  - Start-OrchJob

- The above changes are only renaming the cmdlets, and their functionality remains completely unchanged. If you wish to use the cmdlets with their old names, you can set aliases. Aliases can be configured as follows:

  PS> Set-Alias Add-OrchProcess New-OrchProcess

- To configure the alias automatically when the PowerShell console starts, you can add the above command to your profile script. To edit the profile script, execute the following in the PowerShell console:

  PS> notepad $profile

## [0.9.8.24] - 2025-01-13
- When an SSL error, such as the absence of an installed certificate, occurs, the process can now continue by ignoring the error. To ignore SSL errors, add the following to the PSDrive in the configuration file:
  "IgnoreSslErrors": true

- The Add-OrchFolderUser cmdlet now displays an appropriate message if it fails to retrieve roles.

- The Copy-Item and Copy-OrchFolderMachine cmdlets now display an appropriate message if a machine with the same name is already assigned to the destination folder.

- Some operations failed when the target Orchestrator version was outdated. These issues have been resolved, and the cmdlets now handle operations appropriately based on the API version. The correspondence between cmdlets and API versions is as follows:

  - Add-OrchQueue:
    - ApiVer <  16: Does not set RetentionAction and RetentionPeriod, and Calls POST /odata/QueueDefinitions
    - ApiVer >= 16: Sets RetentionAction and RetentionPeriod, and Calls POST /odata/QueueDefinitions/UiPath.Server.Configuration.OData.CreateQueue

  - Get-OrchProcess -ExpandDetails, Update-OrchProcess, Copy-OrchProcess, Copy-Item:
    - ApiVer <  17: Does not call GET /odata/ReleaseRetention({releaseId})
    - ApiVer >= 17: Calls GET /odata/ReleaseRetention({releaseId})

  - Add-OrchProcess, Copy-Item:
    - ApiVer <  17: Calls POST /odata/Releases
    - ApiVer >= 17: Calls POST /odata/Releases/UiPath.Server.Configuration.OData.CreateRelease

  - Get-OrchTestCase, Get-OrchTestSet, Get-OrchTestSetSchedule, Get-OrchTestSetExecution:
    - ApiVer <  18: No action is taken.
    - ApiVer >= 18: Retrieve entities.

## [0.9.8.23] - 2025-01-03
- Added the Redo-OrchQueueItem cmdlet, which retries transaction items by specifying the queue name and the item IDs. It retries only retryable items, defined as those with a Status of Failed and a Revision of either None or InReview.

  - To retry specified items in the queue, use the following command. The -Id parameter, which specifies item IDs, supports auto-completion:

    PS Orch1:\Shared> Redo-OrchQueueItem YourQueueName <item IDs>

  - To retry all failed items in a queue:

    PS Orch1:\Shared> Get-OrchQueueItem YourQueueName -Status Failed -Revision None,InReview | Redo-OrchQueueItem -Verbose

  - Please note that the Get-OrchQueueItem cmdlet can retrieve up to a maximum of 1000 items at a time. If there are more than 1000 retryable items, repeatedly execute following command until the -Verbose parameter no longer outputs anything:

    PS Orch1:\Shared> Get-OrchQueueItem YourQueueName -Status Failed -Revision None,InReview | Redo-OrchQueueItem -Verbose

  - To retry all retryable queue items across all queues in the tenant:

    PS Orch1:\> Get-OrchQueueItem -Recurse * -Status Failed -Revision None,InReview | Redo-OrchQueueItem

- Fixed an issue where the -UserName parameter completer for the Add-OrchPmGroupMember cmdlet was not functioning correctly.

## [0.9.8.22] - 2024-12-16
- In version 0.9.8.16, the parameter name of the Add-OrchProcess cmdlet was changed from -PackageId to -Id. However, the column name in the CSV file output by Get-OrchProcess -ExportCsv was not updated accordingly. As a result, this CSV file could not be imported using the Add-OrchProcess cmdlet.

- When the Get-OrchProcess cmdlet tried to deserialize ReleaseDto from the server, it failed if the ResourceOverwrite property had a value because its type was not documented in the Swagger doc. Based on the JSON returned by Automation Cloud, I defined the ResourceOverwrite type correctly to resolve the error. To account for other Orchestrator versions returning differently defined ResourceOverwrite, deserialization failures now set the value to null instead of throwing an exception. This may require future adjustments.

- A warning is now displayed if the required scope specifications for UiPathOrch are not listed in the configuration file.

- The authentication process can now be canceled with Ctrl+C.

## [0.9.8.21] - 2024-12-12
- The member names included in UpdateInfoDto were incorrect. As a result, information from UpdateInfo was not properly included in the output of cmdlets such as Get-OrchUserSession.

- When importing a CSV using the Add-OrchFolderUser cmdlet, if multiple roles were specified in the Roles column as comma-separated values, a warning stating "No matching role found" was incorrectly displayed, even though the processing completed successfully.

## [0.9.8.20] - 2024-12-04
- Under certain conditions, the Copy-Item cmdlet was failing. This issue has been present since UiPathOrch 0.9.8.11.

- Fixed an issue where the Add-OrchTrigger and Update-OrchTrigger cmdlets could not add multiple account-machine mappings. These mappings can be specified using the -MachineRobots parameter. The valid format for this parameter can be confirmed using auto-completion.

- The -MachineRobots parameter of the Add-OrchTrigger and Update-OrchTrigger cmdlets now supports wildcards for the internal RobotName, MachineName, and HostMachineName fields. While auto-completion is not available for these internal values, using wildcards allows you to perform the intended operation even if the exact RobotName or MachineName is unknown. For example, to add all combinations of RobotName, MachineName, and HostMachineName available in the current folder as account-machine mappings to <your triggers>, use the following command:

  PS Orch1:\Shared> Update-OrchTrigger <your trigger names> -MachineRobots '[{"RobotName":"*","MachineName":"*","HostMachineName":"*"}]'

- The completer for the -UserName parameter in the following functions stopped working in version 0.9.8.16:
  - Enable-OrchUserAttended
  - Enable-OrchPersonalWorkspace
  - Disable-OrchUserAttended
  - Disable-OrchPersonalWorkspace

  Note: Functions refer to cmdlets implemented in .ps1 files. The implementations of the above functions are located in the Functions directory under the UiPathOrch installation directory.

- Changed the output format of the Get-OrchActivitySetting cmdlet from table view to list view. Additionally, the members of SignalR are now expanded and displayed.

## [0.9.8.19] - 2024-11-30
- Added the following cmdlets:
  - Get-OrchFolderMachineAccountMapping
  - Enable-OrchFolderMachineAccountMapping
  - Disable-OrchFolderMachineAccountMapping

- Regarding the Enable and Disable cmdlets mentioned above:
  - The first positional parameter -Name specifies the machine name.
  - The second positional parameter -UserName specifies the account name.
  - These parameters support wildcards. To specify exact names, you can use auto-completion.
  - Additionally, like other cmdlets, the -WhatIf, -Verbose, and -Confirm switch parameters are supported.
  - For example, to remove all account mappings for all machines assigned to all folders, you can use the following command:
    PS Orch1:\> Disable-OrchFolderMachineAccountMapping -Recurse * *

- Added the -Scope parameter to the Add-OrchMachine cmdlet. In addition to Machine Templates, it is now possible to add Standard and Serverless machines.

- Fixed an issue where the header and values in the CSV file output by Get-OrchMachine -ExportCsv were misaligned. Additionally, the following columns are now included in the output. This CSV file can be imported using Add-OrchMachine and Update-OrchMachine cmdlets:
  - Scope
  - RobotUsers
  - UpdatePolicyType
  - UpdatePolicyVersion
  - MaintenanceCron
  - MaintenanceDuration
  - MaintenanceEnabled
  - MaintenanceTimezoneId

## [0.9.8.18] - 2024-11-27
- Added the following parameter to the Add-OrchMachine cmdlet:
  - -RobotUsers 

- Added the following parameters to the Update-OrchMachine cmdlet:
  - -RobotUsers 
  - -AutomationType
  - -TargetFramework

## [0.9.8.17] - 2024-11-26
- The Add-OrchUser cmdlet was unable to add robot accounts with the -UR_Password parameter.

- The -SourceRecurse switch parameter of the Import-OrchPackage cmdlet has been renamed to -Recurse to make it easier for users to find this parameter.

- The -WarnNoMatch parameter in the following cmdlets has been renamed to -NoMatchWarning. This change was made to reduce the risk of user errors, such as mistakenly specifying -WarnNoMatch instead of -WhatIf.
  - Remove-OrchUser
  - Remove-OrchFolderUser
  - Remove-OrchPmUser
  - Remove-OrchPmGroupMember
  - Remove-OrchPmAllocationFromPmLicensedGroup

## [0.9.8.16] - 2024-11-25
- The following cmdlets for managing users now support the -Type parameter:
  - Get-OrchUser
  - Copy-OrchUser
  - Remove-OrchUser
  - Remove-OrchRoleFromUser
  - Add-OrchRoleToFolderUser
  - Remove-OrchRoleFromFolderUser

- Fixed an issue where the completers for the Add-OrchProcess cmdlet were not functioning properly.

- Renamed the -JobId parameter in Get-OrchJob to -Id.

- Renamed the -PackageId parameter in Add-OrchProcess to -Id.

- Resolved an issue in the Add-OrchPmGroupMember cmdlet where the auto-completion for the -UserName parameter included local group names. Since local groups cannot be added to groups, local group names are no longer displayed as suggestions.

- Fixed an issue where the -UserName parameter in the Set-OrchPmRobotAccount cmdlet was unintentionally hidden.

## [0.9.8.15] - 2024-11-22
- When executing cmdlets with parameters instructing the retrieval of detailed information (e.g., Get-OrchProcess -ExpandDetails) across many folders (by specifying parameters such as -Recurse or -Path), an error stating "non-concurrent collections must have exclusive access" rarely occurred.

- When specifying the -Recurse switch parameter for any cmdlets, the output previously mixed personal workspace folders and other folders. This behavior has been corrected so that all personal workspace folders are output first, followed by other folders.

  - Please note that personal workspaces that have been started exploring via the Orchestrator web interface have been operable from earlier versions of UiPathOrch. However, since starting to explore personal workspaces is not supported via API, this operation must be performed manually through the web interface.

  - To reflect personal workspace explorations started on the web in a PowerShell session, please execute the Clear-OrchCache cmdlet to clear the folder cache.

## [0.9.8.14] - 2024-11-21
- The version number is now displayed on the screen after logging in with the browser using OAuth non-confidential settings.

## [0.9.8.13] - 2024-11-20
- The Add-OrchPmGroupMember cmdlet has been improved.
  - When the tenant is integrated with Active Directory, it is now possible to add AD groups to local groups.

  - When querying names in the directory, the queries are now performed in bulk whenever possible.

## [0.9.8.12] - 2024-11-18
- The ResumeVersion property of JobDto was incorrectly defined as a string, which caused an error when retrieving resumed jobs using the Get-OrchJob cmdlet. This has been fixed by changing the ResumeVersion property to int.

- Fixed an issue where the source property of users was not included in the output when executing Get-OrchPmGroup -ExpandMembers.

- Enhanced the processing speed of Get-OrchPmGroup -ExpandMembers.

- Added the following query parameters to the Get-OrchJob cmdlet:
  - -StartTimeAfter
  - -StartTimeBefore
  - -EndTimeAfter
  - -EndTimeBefore
  - -ResumeTimeAfter
  - -ResumeTimeBefore

- Updated the output of the Get-OrchUser cmdlet to include the following properties:
  - ExplicitMayHaveRobotSession
  - ExplicitMayHaveUserSession
  - ExplicitMayHavePersonalWorkspace
  - ExplicitRestrictToPersonalWorkspace

- Fixed an issue where the Directory of triggers did not display correctly immediately after triggers were created using the Add-OrchTrigger cmdlet.

## [0.9.8.11] - 2024-11-12
- When RedirectUrl was set at the root level of the configuration file, connections to confidential app settings could not be established.

- Previously, each entity used a distinct cache implementation, despite the code being nearly identical. To streamline maintenance, the common code has now been extracted and unified. This change improves code maintainability and slightly reduces the module's footprint. Additionally, several minor issues were fixed in the process.

- In the former implementation of Get-OrchLog, logs sorted by TimeStamp could retrieve over 10,000 rows by generating an OData query based on the TimeStamp value of the last row fetched. However, this approach sometimes caused log rows on page boundaries to be missed. Therefore, the automatic page splitting by TimeStamp values has been removed. This change is expected to improve functionality, though as a trade-off, it is no longer possible to retrieve over 10,000 log rows at once. If needed, please adjust the query parameters manually and execute the Get-OrchLog cmdlet in segments. Then, by running the Get-OrchLog cmdlet without parameters, the cached log rows can be combined and output.

- The Get-OrchQueueItem cmdlet has been enhanced.
  - In Automation Cloud, the number of queue items that can be retrieved at once is limited to 100. Previously, specifying -First 100 was required when executing the Get-OrchQueueItem cmdlet to obtain the expected results. This limitation has been removed by automatically paginating in increments of 100 rows.

  - The DateTime values returned by the Get-OrchQueueItem cmdlet (such as DueDate, DeferDate, etc.) are now output in the local time zone. Previously, the default view displayed DateTime values in the local time zone, but they appeared in UTC format when redirected via a pipeline.

  - Several query parameters (such as -DueDateAfter and -DueDateBefore) have been added.

  - The Get-OrchQueueItem cmdlet now displays a progress bar.

## [0.9.8.10] - 2024-11-07
- For cmdlets that copy folder entities (such as Copy-OrchAsset), the process has been modified so that even if reading entities from a folder fails, the operation continues with subsequent folders without interruption.

- The -Recurse switch parameter has been added to the following cmdlets, allowing -Recurse to be used with all cmdlets that copy folder entities:
  - Copy-OrchBucket
  - Copy-OrchProcess
  - Copy-OrchQueue
  - Copy-OrchTestDataQueue
  - Copy-OrchTestSet
  - Copy-OrchTestSetSchedule

- When the source and destination folders have an identical subfolder structure, specifying -Recurse on folder entity copy cmdlets will automatically copy each folder's entities to the corresponding destination folder. For example, to copy all storage buckets from one tenant to another with an identical folder structure, use the following command:
  PS Src:\> Copy-OrchBucket -Recurse * Dst:\

- For the Copy-OrchPmUser cmdlet, when the source user's email is empty, this will now set the destination user's username to the source user's username. Previously, the source user's email was always set as the destination user's username, even if it was empty.

## [0.9.8.9] - 2024-11-06
- When retrieving logs from multiple folders with the Get-OrchLog cmdlet and specifying the -First parameter, it was not handled correctly (the internal counter was not reset). As a result, logs could not be retrieved from the second folder onward.

- The -JobId parameter in the Get-OrchLog cmdlet was not functioning and has been removed.

- The Copy-OrchTrigger, Copy-OrchApiTrigger, and Copy-OrchTestSet cmdlets have been updated to change the method for searching robots in the destination tenant.  
  Previously, the following endpoint was used to search for robots in the destination tenant:
    - GET /odata/Robots/UiPath.Server.Configuration.OData.GetConfiguredRobots?$expand=User
  This has been modified to use the following endpoint for robot searches:
    - GET /odata/Robots/UiPath.Server.Configuration.OData.GetRobotsFromFolder(folderId={folderId})

## [0.9.8.8] - 2024-11-05
- The Move-OrchFolderUser cmdlet has been added. This cmdlet moves folder users within a tenant's folders.

- The Copy-OrchPmUser cmdlet has been added. This cmdlet copies organizational users between organizations. If the groups to which the users belong do not exist in the destination organization, they are automatically created.

- The parameter for cmdlets handling PmUser has been changed from -UserName to -Email. It was more appropriate to use -Email for uniquely identifying PmUser rather than -UserName. Note that the -Email parameter has an alias -UserName. The modified cmdlets are as follows:
  - Get-OrchPmUser
  - Update-OrchPmUser
  - Remove-OrchPmUser

## [0.9.8.7] - 2024-11-04
- When migrating from MSI Orchestrator to Automation Cloud, user identifiers often change from usernames to email addresses. As a result, user migration did not always function as expected. To address this, if a user is not found when searched by username in the destination tenant's directory, the directory is now also searched using the user's email address. This update ensures that the Copy-OrchUser, Copy-Item, Copy-OrchFolderUser, and Copy-OrchAsset cmdlets work as expected when migrating users from MSI Orchestrator to Automation Cloud.

- The following nodes have been added to the root of UiPathOrchConfig.json:
  - Description
  - RedirectUrl
  - HttpListener
  - Scope
  - Enabled

  The values set in these root nodes are shared across all PSDrives as child nodes. These values can be overridden (cascaded) by defining nodes with the same name within each PSDrive's node. This allows for a much more concise configuration file, especially when dealing with many tenants within the same organization. The RedirectUrl setting, in particular, can likely be shared among multiple PSDrive configurations.

- In line with the above changes, the template for UiPathOrchConfig.json has been updated.

- Values cascaded to each PSDrive can now be verified using the Get-OrchPSDrive cmdlet as follows:
  PS> Get-OrchPSDrive | select *

- The Update-OrchTrigger cmdlet has been updated so that passing an empty string to the -MachineRobots parameter now removes MachineRobots.

## [0.9.8.6] - 2024-11-03
- Added the Move-OrchPmGroupMember cmdlet. This cmdlet moves users between organizational groups within a tenant.

## [0.9.8.5] - 2024-11-01
- The Disable-OrchTrigger cmdlet had stopped working in version 0.9.7.14. This issue was introduced during the process of sharing implementation with the Enable-OrchTrigger cmdlet. We apologize for any inconvenience caused by this problem.

- These cmdlets have been renamed
  - From:
    - Add-OrchPmMemberToPmGroup
    - Remove-OrchPmMemberFromPmGroup
  - To:
    - Add-OrchPmGroupMember
    - Remove-OrchPmGroupMember 
  We apologize for any inconvenience caused by this change.

## [0.9.8.4] - 2024-10-30
- Configuration File-Related Updates:
  - The HttpListener in UiPathOrchConfig.json is now automatically configured from RedirectUrl. The value of HttpListener will be the same as RedirectUrl with a trailing slash added. This allows the configuration file to work without specifying HttpListener. However, if the port number specified in RedirectUrl is 1024 or below, you still need to include HttpListener in the configuration file as before.

  - Changed the value of RedirectUrl in the template file of UiPathOrchConfig.json to "http://localhost:8085/Temporary_Listen_Addresses" (in other words, changed the port from 80 to 8085). Also, removed HttpListener from this file. If the configuration file does not exist when Import-Module UiPathOrch is executed, a configuration file will be automatically created from this template.

- Role-Related Updates:
  - Added the -ExportCsv parameter to the Get-OrchRole cmdlet. Like other cmdlets, you can specify a folder or file path for exporting the CSV. If a folder path is specified, it will output with the default file name "ExportedRoles.csv". This CSV file can be imported using the Set-OrchRole cmdlet.

  - Added the Set-OrchRole cmdlet. If a role with the name specified by the -Name parameter (Name column in the CSV) already exists, it will be updated. If it does not exist, a new role with that name will be created.

- Other:
  - The execution result of Get-OrchRole -ExpandPermission was incorrect. I apologize for the inconvenience.

## [0.9.8.3] - 2024-10-28
- The Get-OrchPmAuditLog cmdlet has been added. Similar to Get-OrchJob and Get-OrchLog cmdlets, running it without specifying filter parameters will output the entire cache. Please note that the only filter parameters available for this cmdlet are -Skip and -First.

- The -ExportCsv parameter has been added to the Get-OrchFolderMachine cmdlet. Machines inherited from parent folders will not be included in this CSV file. This CSV file can be imported using the Add-OrchFolderMachine cmdlet. Please be aware that if you are importing it into a different tenant, you need to modify the drive name embedded in the Path column of the CSV file.

- The -PropagateToSubFolders parameter of the Add-OrchFolderMachine cmdlet was not functioning. When the parameter was added in an earlier version, the implementation was forgotten. My apologies for this oversight.

## [0.9.8.2] - 2024-10-27
- The behavior of the Get-OrchAuditLog cmdlet has been improved.  

  - This cmdlet can now call the /odata/AuditLogs/UiPath.Server.Configuration.OData.GetAuditLogDetails(auditLogId={auditLogId}) endpoint. When the -ExpandDetails switch parameter is specified, the output from this endpoint is cached in the Details member of each audit log entry. Additionally, JSON text stored in Details.CustomData is automatically expanded into Details.CustomDataExpanded.

  - When no filter parameters are specified, cached content is now output. Previously retrieved logs can be obtained quickly. However, even in this case, if the -ExpandDetails switch parameter is specified, an API call is made for log entries whose Details are not cached.

  - The Entities.CustomData returned from the API contains JSON text, which is now automatically expanded into Entities.CustomDataExpanded. This content can be easily checked by redirecting the output to the ConvertTo-Json cmdlet.  

  - The output is now sorted in descending order by ExecutionTime with odata query, making the -Skip and -First parameters easier to use.

## [0.9.8.1] - 2024-10-25
- The configuration file property name Proxy.Address has been changed to Proxy.Url for consistency. Apologies for any inconvenience.

## [0.9.8.0] - 2024-10-25
- Support for proxy connections has been added. To configure the proxy, place the following section at the beginning of the UiPathOrchConfig.json file, directly under the root (i.e., parallel to "PSDrives"). This "Proxy" section can also be defined inside each PSDrive, where it will override the root-level "Proxy" settings.

{
  "Proxy": {
    "Address": "http://proxy.example.com:8080",
    "BypassProxyOnLocal": true,
    "UseDefaultCredentials": false,
    "Credentials": {
      "Username": "PROXY_USERNAME",
      "Password": "PROXY_PASSWORD"
    },
    "Enabled": true
  },

  "PSDrives": [
    ...

- For example, to enable the proxy for all PSDrives but disable it for a specific PSDrive, you can include the following in the configuration of that PSDrive:

      "Proxy": {
        "Enabled": false
      }

## [0.9.7.18] - 2024-10-25
- The CSV import process in the Update-OrchUser cmdlet was not handled correctly.

- The -UserName parameter of the Update-OrchPmUser cmdlet did not support CSV imports.

## [0.9.7.17] - 2024-10-23
- Removed the -Email parameter from the Update-OrchPmUser cmdlet. This is because changing the email is not supported by the API, and it would not yield the expected results.

- Changed the endpoint called by the Get-OrchPmUser cmdlet for Automation Cloud:
  - Before: GET /api/User/users/{partitionGlobalId} 
  - After: GET /portal_/api/identity/UserPartition/licenses?partitionGlobalId={partitionGlobalId} 
  - When the target is MSI Orchestrator, the cmdlet will continue to call the following endpoint: GET /api/UserPartition/users/{partitionGlobalId} 

- Added the following Content-Type header to all HTTP GET requests. Generally, this header is not required when performing GET requests to Orchestrator Web API endpoints, but the endpoint called by the above Get-OrchPmUser cmdlet returned an error without it:
  Content-Type: application/json; charset=utf-8

- If the Orchestrator API version is below 16, the processing of the following cmdlets is now skipped. This change also helps prevent unnecessary errors when copying folders in older versions of Orchestrator:
  - Get-OrchApiTrigger
  - Get-OrchTestCase
  - Get-OrchTestCaseExecution
  - Get-OrchTestSet
  - Get-OrchTestSetExecution
  - Get-OrchTestDataQueue

## [0.9.7.16] - 2024-10-21
- The Update-OrchProcessVersion cmdlet now includes the -Id parameter. You can specify the target process using -Id instead of -Name. This allows the cmdlet to be executed without the need for the OR.Execution.Read scope in OAuth (however, OR.Execution.Write is still required).

- The exception handling in the Get-OrchProcess cmdlet was incomplete.

## [0.9.7.15] - 2024-10-21
- The Copy-Item and Copy-OrchFolderUser cmdlets were not copying robots and applications assigned to folders.

- The completer for the -UserName parameter in the Add-OrchFolderUser cmdlet was not functioning under certain conditions.

- After updating PmGroup and PmRobotAccount, the directory cache was not automatically cleared.

- Get-OrchUser -ExportCsv now outputs the UR_CredentialExternalName column.

- Added the -UR_CredentialExternalName parameter to the Add-OrchUser and Update-OrchUser cmdlets.

- The CSV file generated by the following command was inappropriate:
PS Orch1:\> Get-OrchRole -ExpandPermission | Export-Csv <filepath>

## [0.9.7.14] - 2024-10-17
- The Copy-OrchUser cmdlet now searches for a user with the same name as the source user in the destination tenant, and if found, sets that user's identifier.

- The Add-OrchUser cmdlet had recently stopped being able to add robots, but this has been fixed.

## [0.9.7.13] - 2024-10-15
- The Enable-OrchFolderMachineInherit and Disable-OrchFolderMachineInherit cmdlets have been added. These cmdlets replicate the functionality of the "Propagate machine to subfolders" menu in the Orchestrator web interface.

- The Copy-Item and Copy-OrchFolderMachine cmdlets now correctly copy the PropagateToSubFolders property of folder machines. Additionally, these cmdlets no longer copy folder machines where the IsAssigned property is set to False.

- There was an issue where the Copy-OrchMachine cmdlet failed to copy the Account-Machine Mappings of tenant machines under certain conditions. Additionally, if a robot with the same name is not found in the destination tenant, the copy process will now continue with a warning, whereas previously, the operation would fail.

## [0.9.7.12] - 2024-10-15
- The Get-DuRole and Get-DuUser cmdlets have been added. Please note that these cmdlets work on drives of the UiPathOrchDu provider.

- The Get-OrchPSDrive cmdlet now outputs drive information for UiPathOrchDu and UiPathOrchTm providers in addition to the UiPathOrch provider.

## [0.9.7.11] - 2024-10-11
- The Get-OrchMachineClientSecretId cmdlet has been added. By combining it with the Remove-OrchMachineClientSecret cmdlet, it is now easy to bulk delete client secrets issued before a specified date and time. Here's how:
PS Orch1:\> Get-OrchMachineClientSecretId | ? CreationTime -LT '2024/10/01' | Remove-OrchMachineClientSecret

- The Clear-OrchCache cmdlet was not clearing the cache for the Get-OrchLog cmdlet.

## [0.9.7.10] - 2024-10-10
- When outputting a CSV file using -ExportCsv, if -CsvEncoding is specified as UTF, a BOM (Byte Order Mark) is now added to the beginning of the file. This ensures that the CSV file can be opened in Excel without garbled characters in a Japanese environment, even if -CsvEncoding sjis is not specified.

- The Add-OrchMachineSecretKey cmdlet has been renamed to Add-OrchMachineClientSecret.

- The Remove-OrchMachineClientSecret cmdlet has been added.

- The Add-OrchUser and Copy-OrchUser cmdlets now output a warning when no matching roles are found in the destination tenant.

- When copying users with the Copy-OrchUser cmdlet, if the original user has folder roles assigned, those roles are now excluded before copying.

- The Add-OrchUser and Add-OrchFolderUser cmdlets could not add users if their UserName contained characters such as + or _.

- The Copy-OrchTrigger cmdlet now continues copying even if a robot with the same name is not found in the destination tenant, after outputting an error. Previously, the copy would fail if a robot with the same name was not found.

## [0.9.7.9] - 2024-10-07
- The Add-OrchMachineSecretKey cmdlet has been added.

- The Add-OrchFolderUser cmdlet now outputs a warning when no roles matching the wildcard specified in -Roles are found.

- The Copy-OrchRole cmdlet did not copy roles where IsStatic was set to True.

- In recent versions, some cmdlets that manage tenant users had stopped working under certain conditions.

- The Get-OrchUser cmdlet now includes the -ExportCsv parameter. It should export all parameters, including the Execution Settings of Unattended Robots. This CSV file can be imported using the Add-OrchUser and Update-OrchUser cmdlets.

- The Add-OrchUser and Update-OrchUser cmdlets now have many additional parameters. You should now be able to specify all the parameters that can be set through the web interface, including the Execution Settings of Unattended Robots.

- The processing speed of the Get-OrchUser cmdlet has been improved when the -ExpandDetail or -ExportCsv switch parameters are specified. Additionally, a progress bar is now displayed.

- The date and time data included in the entities output by the Get-OrchJob, Get-OrchLog, and Get-OrchAuditLog cmdlets are now output in the local timezone. Previously, they were displayed in the local timezone only when output to the console using the default view, but were output in UTC when piped to other cmdlets.

## [0.9.7.8] - 2024-09-30
- The name of the Add-OrchPmLicenseToPmLicensedGroup cmdlet has been changed to Add-OrchPmLicenseToPmGroup.

- The completer for the Add-OrchPmLicenseToPmGroup cmdlet was not functioning as expected. Additionally, it was not possible to assign licenses to groups that had not yet been assigned any licenses.

- The Remove-OrchPmLicensedGroup cmdlet has been added.

## [0.9.7.7] - 2024-09-29
- Updated the progress bars in the Copy-OrchLibrary and Copy-OrchPackage cmdlets to display overall progress.
- Added the -ProcessType filter parameter to the Get-OrchJob cmdlet.
- Removed the Get-OrchJobVideo cmdlet and replaced it with a function of the same name. The new function achieves the same functionality by calling the Get-OrchJob cmdlet, reducing code duplication and minimizing the module's footprint.

## [0.9.7.6] - 2024-09-28
- The Copy-OrchMachine cmdlet now outputs the LicenseKey and ClientSecret of the machine created in the destination tenant.

## [0.9.7.5] - 2024-09-27
- Added the -Recurse parameter to Copy-OrchPackage. When the feed folders have the same name in both the source and destination tenants, the following command will copy all packages from both the tenant feed and folder feeds. The first asterisk represents the package name, and the second asterisk represents the version number:
PS Src:\> Copy-OrchPackage * * Dst:\ -Recurse

- The Get-OrchProcess cmdlet was not retrieving retention info.

- The Import-OrchPackage cmdlet did not support CSV import.

- In addition to update-related cmdlets, the Get-OrchJob and Get-OrchLog cmdlets now also accept parameters from CSV imports. By specifying the -Path parameter via CSV, you can limit the target folders for processing. Going forward, I plan to modify other Get cmdlets to also accept parameters from CSV imports.

## [0.9.7.4] - 2024-09-24
- Bucket operations enhancements:
  - Added the -ExportCsv parameter to the Get-OrchBucket cmdlet.
  - Added the Add-OrchBucket cmdlet, which allows importing CSV files generated by the Get-OrchBucket cmdlet.

- Machine operations enhancements:
  - The Get-OrchMachine cmdlet now exports Tags along with other machine information.
  - Added the -Tags parameter to the Add-OrchMachine cmdlet for tags assignment during machine creation.

- Process operations enhancements:
  - Added the -ExpandDetails switch parameter in the Get-OrchProcess cmdlet for detailed information retrieval.
  - Fixed an issue where the Get-OrchProcess cmdlet previously output incomplete information when exporting to a CSV file.

- Format Change for the -Tags Parameter:
  The format of the -Tags parameter has changed from JSON format to simple comma-separated text in the following cmdlets:
  - Get-OrchBucket / Add-OrchBucket
  - Get-OrchProcess / Add-OrchProcess
  - Get-OrchQueue / Add-OrchQueue

## [0.9.7.3] - 2024-09-21
- Added the Get-OrchPmLicensedUser cmdlet.

- Added the -ExcludeEntities switch parameter to the Copy-Item cmdlet. When specified, only folders are copied, and folder entities (such as processes, assets, triggers, etc.) contained within them are not copied.

- When copying the root folder using Copy-Item with the -Recurse parameter, the sorting order of the folders at the destination became incorrect.

- The output of the Get-OrchJob cmdlet was incomplete.

- The Path property in the output of the Copy-OrchPmRobotAccount cmdlet was not set correctly.

## [0.9.7.2] - 2024-09-18
- Added the Update-OrchMachine cmdlet.

## [0.9.7.1] - 2024-09-17
- The Remove-OrchRoleFromUser cmdlet had stopped working in the recent version.

- Part of the output of the Get-OrchLicenseStats cmdlet was incorrect.

## [0.9.7.0] - 2024-09-16
- Renamed the following cmdlets from:
  - Get-OrchPmUserLicenseGroup
  - Remove-OrchPmAllocationFromPmUserLicenseGroup
to:
  - Get-OrchPmLicensedGroup
  - Remove-OrchPmAllocationFromPmLicensedGroup

- Added the following cmdlets:
  - Update-OrchPmUser
  - Add-OrchPmLicenseToPmLicensedGroup
  - Remove-OrchPmLicenseFromPmLicensedGroup

- Improvements:
  - Refactored the implementation of the Add-OrchPmMemberToPmGroup cmdlet.
  - Refactored the implementation of the Remove-OrchPmMemberFromPmGroup cmdlet and made the -Type parameter mandatory.

- Note on Get-OrchPmUser cmdlet:
  - For MSI Orchestrator, the Get-OrchPmUser cmdlet now calls the following endpoint (please note that this endpoint is deprecated):
  GET /api/UserPartition/users/{partitionGlobalId}
  - For Cloud Orchestrator, it continues to call the same endpoint as before:
  GET /api/User/users/{partitionGlobalId}

## [0.9.6.23] - 2024-09-11
- The Get-OrchLog cmdlet was unable to properly handle more than 10,000 log entries when the -OrderBy parameter was specified.

- When the -ExportCsv parameter was specified for the Get-OrchPmGroup cmdlet, the column name in the output .csv file was incorrect.

## [0.9.6.22] - 2024-09-09
- Like with other cmdlets, item names in the output of the Get-Item and Get-ChildItem cmdlets can now be displayed using auto-completion.

  ```powershell
  PS Orch1:\> dir | select FullName,FeedType,[press ctrl+space]
  ```

  Note: dir is an alias for Get-ChildItem, and select is an alias for Select-Object.

- In the UiPathOrchDu and UiPathOrchTm providers, the FullName property has been added to DuProject and TmProject. By specifying this in the -Path parameter, you can easily write PowerShell scripts to perform actions on all projects.

  ```powershell
  dir Orch1Du:\ | % {
      # $_ contains the project entity
      $doctype = Get-DuDocumentType -Path $_.FullName

      # Additional complex processing...
  }
  ```

  Note: % is an alias for ForEach-Object cmdlet.

- The Find-OrchFolderNoUserAssigned.ps1 script, which was located in the Examples folder, has been promoted to a function. This function lists all folders that have no users assigned to them. To output the paths of the relevant folders concisely, use the following command:

  ```powershell
  PS> Find-OrchFolderNoUserAssigned Orch1:\ -IncludeInherited | select -ExpandProperty FullName
  ```

- The following cmdlets previously failed when a package with the same name already existed in the destination. They have been modified to skip the API call when a package with the same name is found. This significantly improves processing speed when many packages with the same name already exist:
  - Copy-OrchLibrary
  - Copy-OrchPackage
  - Import-OrchLibrary
  - Import-OrchPackage

- The -ExpandDetails switch parameter has been added to the Get-OrchTrigger cmdlet. This instructs it to call GET /odata/ProcessSchedules({processScheduleId}).

## [0.9.6.21] - 2024-09-08
- The Get-OrchJob, Get-OrchLog, and Get-OrchQueueItem cmdlets now support specifying sorting fields and sorting order. You can specify these using the -OrderBy and -OrderAscending parameters.

- Some parts of the output of the Get-OrchQueueItem cmdlet were not being processed correctly.

## [0.9.6.20] - 2024-09-07
- Added the -ExpandDetails switch parameter to the Get-OrchUser cmdlet. This allows retrieving detailed user information, such as the UpdatePolicy property.

- Copy-OrchUser was not properly copying certain properties, such as the UpdatePolicy property.

- Added the Update-OrchUser cmdlet, which allows updating any user information. For example, to enable attended automation for a user, use the following command:
  PS> Update-OrchUser <username> -MayHaveRobotSession True

- To enable unattended automation for a user, use the following command:
  PS> Update-OrchUser <username> -MayHaveUnattendedSession True -UR_UserName <windows domain\user> -UR_Password <windows password>

- When executing the Enable-OrchUserAttended and Disable-OrchUserAttended cmdlets, some user properties (such as the UpdatePolicy property) were being lost.

- Deprecated the Enable-OrchUserAttended and Disable-OrchUserAttended cmdlets and replaced them with functions of the same names. These functions achieve the same functionality by calling the Update-OrchUser cmdlet, which reduces code duplication and minimizes the module's footprint.

- Similarly, added the Enable-OrchPersonalWorkspace and Disable-OrchPersonalWorkspace functions for enabling or disabling a user's personal workspace. These functions also call the Update-OrchUser cmdlet to achieve their functionality.

- The Remove-OrchPersonalWorkspace cmdlet now automatically disables the target personal workspace folder before removing it. This fix prevents the personal workspace from being recreated immediately after removal.

- When removing a personal workspace using Remove-Item, the target personal workspace folder is now automatically disabled before removal, just as with Remove-OrchPersonalWorkspace.

## [0.9.6.19] - 2024-09-06
- The Copy-OrchFolderUser cmdlet was unable to process directory users.

## [0.9.6.18] - 2024-09-06
- The endpoint called by the Get-OrchPmUser cmdlet has been changed from GET /api/UserPartition/users/{partitionGlobalId} to GET /api/User/users/{partitionGlobalId}.
- The Add-OrchPmUserBulk cmdlet has been added. This cmdlet wraps the Platform Management API endpoint POST /api/User/BulkCreate. It supports importing CSV files to create users in bulk. If there are rows in the CSV that can be aggregated, they will be processed in a single API call.
- The format of the CSV that can be imported is as follows. The only required parameter is Email:
  Path,Email,Name,SurName,DisplayName,Type,BypassBasicAuthRestriction,InvitationAccepted,GroupName
- To specify multiple group names in the GroupName field of a CSV file, enclose the comma-separated group names in double quotes. If you edit the GroupName field in Excel, Excel will automatically add the double quotes.
- For Type, please specify one of the following options. This list can also be viewed using auto-completion on the command line:
  user, robot, directoryUser, directoryGroup, robotAccount, application

## [0.9.6.17] - 2024-09-04
- There was an issue with the cache in the Get-OrchLog cmdlet, where the same log entry was being cached multiple times. Now, log entries that have already been added to the cache are no longer added again.

## [0.9.6.16] - 2024-09-04
- Added the FullName attribute to folder entities retrieved by the dir command. This makes it easier to write scripts that enumerate folders and execute cmdlets by specifying the FullName in the -Path parameter.
- The following script lists all folders in the specified tenant where no users are assigned:

dir Orch1:\ -Recurse | ForEach-Object {
    if (-not (Get-OrchFolderUser -Path $_.FullName)) {
        $_.FullName
    }
}

- The script Find-FoldersNoUserAssigned.ps1 has been added to the Examples folder.

## [0.9.6.15] - 2024-09-03
- When executing the Get-OrchLog cmdlet, an error occurred due to API limitations if the filter specified by the parameters matched more than 10,000 log entries. To address this, the API calls are now automatically divided to retrieve more than 10,000 log entries.
- Please note that the Get-OrchLog cmdlet caches the retrieved log entries. When executed without specifying filter parameters, it will output all cached log entries. This helps avoid repeatedly querying Orchestrator with the same filter, thereby reducing API usage and minimizing the load on Orchestrator.
- To avoid exceeding API rate limits, the Add-OrchFolderUser cmdlet now waits 600 milliseconds between API calls.

## [0.9.6.14] - 2024-09-02
- It is now possible to connect to an Orchestrator built on Azure App Service. Since the Identity Server URL will be different in this case, please specify it in the IdentityUrl key within the UiPathOrchConfig.json. For the other keys in the JSON file, use the same settings as for OAuth confidential or non-confidential configurations.
- The Get-OrchLog cmdlet now displays a progress bar.

## [0.9.6.13] - 2024-09-01
- The processing order of folders when the -Recurse switch parameter was specified was unnatural. It has been changed to process in a more natural order.
- Added the Remove-OrchCalendarDate cmdlet.
- Added the -WarnOnNoMatch switch parameter to the following cmdlets. This will output a warning if the specified user is not found:
  - Remove-OrchFolderUser
  - Remove-OrchPmUser
  - Remove-OrchPmAllocationFromPmUserLicenseGroup
- Removed the -WarnOnNoMatch switch parameter from the following cmdlets. Since these cmdlets do not support wildcards for usernames, it is more natural to output a warning by default if the specified user is not found:
  - Add-OrchFolderUser
  - Add-OrchPmMemberToPmGroup

## [0.9.6.12] - 2024-08-30
- When executing Import-Csv | Add-OrchUser, if the same user appears multiple times in the .csv file, those entries are now consolidated and processed with a single API call to add the user.
- The following cmdlets have had the -Roles parameter given an alias -TenantRoles:
  - Add-OrchUser
  - Remove-RoleFromUser
- The following cmdlets have had the -Roles parameter given an alias -FolderRoles:
  - Add-OrchFolderUser
  - Add-OrchRoleToFolderUser
  - RemoveRoleFromFolderUser
- These new aliases for the parameters allow you to consolidate your .csv files for user management.

## [0.9.6.11] - 2024-08-27
- Some parameter names in the Get-OrchJob and Get-OrchLog cmdlets were incorrect.
- When executing the Get-OrchLog cmdlet without specifying any filter parameters, the contents of the cache are now output.
- The Get-OrchJob cmdlet now displays a progress bar.

## [0.9.6.10] - 2024-08-25
- The Copy-OrchCalendar cmdlet stopped working in version 0.9.6.9. We sincerely apologize for this issue.

## [0.9.6.9] - 2024-08-25
- Added the Add-OrchCalendarDate cmdlet. You can specify multiple calendars using wildcards and commas. If a non-existent calendar name is specified, a new calendar will be created with that name. Multiple dates can be added at once. You can also import dates from a CSV file. To add dates up to yesterday, specify the -AllowPastDates switch parameter.
- Added the -ExportCsv parameter to the Get-OrchCalendar cmdlet. This CSV can be imported using the Add-OrchCalendarDate cmdlet.
- Renamed the Id parameter in the Get-OrchLog cmdlet to -JobId and made this parameter non-mandatory. Additionally, added the -Recurse parameter and several filter parameters. These changes make the cmdlet easier to use.
- Modified the Get-OrchJob and Get-OrchQueueItem cmdlets to process in a single thread to avoid API rate limits, with a 600-millisecond wait between each API call.
- Fixed an issue where the Add-OrchPmMemberToPmGroup cmdlet did not work under certain conditions.
- Updated Set-OrchAsset to prevent the creation of new assets with names containing wildcard characters. If you want to create an asset with a name containing wildcards, escape the name with a backtick.
- Added the -WarnOnNoMatch switch parameter to the following cmdlets. It outputs a warning if the specified user does not exist:
  - Add-OrchUser
  - Remove-OrchUser
  - Add-OrchPmMemberToPmGroup
  - Remove-OrchPmMemberFromPmGroup

## [0.9.6.8] - 2024-08-21
- The Get-OrchPmGroup cmdlet stopped working in version 0.9.6.7. We sincerely apologize for this issue.
- The -ExpandGroup parameter of the Get-OrchPmRobotAccount cmdlet was not functioning correctly.
- Cmdlets that accept the -ExportCsv parameter now escape Path and Name of entities containing wildcard characters when exporting to CSV.

## [0.9.6.7] - 2024-08-19


## [0.9.6.6] - 2024-08-18


## [0.9.6.5] - 2024-08-17


## [0.9.6.4] - 2024-08-16


## [0.9.6.3] - 2024-08-16


## [0.9.6.2] - 2024-08-13


## [0.9.6.1] - 2024-08-08


## [0.9.6.0] - 2024-08-07


## [0.9.5.16] - 2024-08-06


## [0.9.5.15] - 2024-08-06


## [0.9.5.14] - 2024-08-05


## [0.9.5.13] - 2024-08-04


## [0.9.5.12] - 2024-08-01


## [0.9.5.11] - 2024-07-31


## [0.9.5.10] - 2024-07-26


## [0.9.5.9] - 2024-07-25


## [0.9.5.8] - 2024-07-25


## [0.9.5.7] - 2024-07-25


## [0.9.5.6] - 2024-07-21


## [0.9.5.5] - 2024-07-18


## [0.9.5.4] - 2024-07-17


## [0.9.5.3] - 2024-07-11


## [0.9.5.2] - 2024-07-10


## [0.9.5.1] - 2024-07-09


## [0.9.5.0] - 2024-07-06


## [0.9.4.0] - 2024-06-30


## [0.9.3.1] - 2024-06-28


## [0.9.3.0] - 2024-06-26


## [0.9.2.2] - 2024-06-24


## [0.9.2.1] - 2024-06-23


## [0.9.2.0] - 2024-06-20


## [0.9.1.1] - 2024-06-19


## [0.9.1.0] - 2024-06-16


## [0.9.0.1] - 2024-06-13


## [0.9.0.0] - 2024-06-04


## [0.8.11.1] - 2024-05-27


## [0.8.11.0] - 2024-05-26


## [0.8.10.13] - 2024-05-24


## [0.8.10.12] - 2024-05-23


## [0.8.10.11] - 2024-05-21


## [0.8.10.10] - 2024-05-19


## [0.8.10.9] - 2024-05-13


## [0.8.10.8] - 2024-05-10


## [0.8.10.7] - 2024-05-09


## [0.8.10.6] - 2024-05-07


## [0.8.10.5] - 2024-05-06


## [0.8.10.4] - 2024-05-06


## [0.8.10.3] - 2024-05-05


## [0.8.10.2] - 2024-05-03


## [0.8.10.1] - 2024-05-02


## [0.8.10.0] - 2024-04-30


## [0.8.9.8] - 2024-04-19


## [0.8.9.7] - 2024-04-18


## [0.8.9.6] - 2024-04-16


## [0.8.9.5] - 2024-04-15


## [0.8.9.4] - 2024-04-15


## [0.8.9.3] - 2024-04-13


## [0.8.9.2] - 2024-04-12


## [0.8.9.1] - 2024-04-09


## [0.8.9.0] - 2024-04-07


## [0.8.8.3] - 2024-04-02


## [0.8.8.2] - 2024-04-02


## [0.8.8.1] - 2024-04-02


## [0.8.8.0] - 2024-03-29


## [0.8.7.5] - 2024-03-28


## [0.8.7.4] - 2024-03-27


## [0.8.7.3] - 2024-03-24


## [0.8.7.2] - 2024-03-23


## [0.8.7.1] - 2024-03-20


## [0.8.7.0] - 2024-03-18


## [0.8.6.8] - 2024-03-15


## [0.8.6.7] - 2024-03-13


## [0.8.6.6] - 2024-03-12


## [0.8.6.5] - 2024-03-12


## [0.8.6.4] - 2024-03-11


## [0.8.6.3] - 2024-03-10


## [0.8.6.2] - 2024-03-08


## [0.8.6.1] - 2024-03-08


## [0.8.6.0] - 2024-03-04


## [0.8.5.5] - 2024-03-01


## [0.8.5.4] - 2024-02-27


## [0.8.5.3] - 2024-02-27


## [0.8.5.2] - 2024-02-27


## [0.8.5.1] - 2024-02-26


## [0.8.5.0] - 2024-02-25


## [0.8.4.0] - 2024-02-23


## [0.8.3.7] - 2024-02-19


## [0.8.3.6] - 2024-02-18


## [0.8.3.5] - 2024-02-16


## [0.8.3.4] - 2024-02-16


## [0.8.3.3] - 2024-02-15


## [0.8.3.2] - 2024-02-12


## [0.8.3.1] - 2024-02-12


## [0.8.3.0] - 2024-02-11


## [0.8.2.0] - 2024-02-03


## [0.8.1.5] - 2024-01-31


## [0.8.1.4] - 2024-01-31


## [0.8.1.3] - 2024-01-26


## [0.8.1.2] - 2024-01-24


## [0.8.1.1] - 2024-01-17


## [0.8.1.0] - 2024-01-15


## [0.8.0.10] - 2023-12-19


## [0.8.0.9] - 2023-11-16


## [0.8.0.8] - 2023-11-13


## [0.8.0.7] - 2023-11-12


## [0.8.0.6] - 2023-11-06


## [0.8.0.5] - 2023-11-06


## [0.8.0.4] - 2023-11-06


## [0.8.0.3] - 2023-11-05


## [0.8.0.2] - 2023-11-03


## [0.8.0.1] - 2023-11-03


## [0.8.0.0] - 2023-11-03


