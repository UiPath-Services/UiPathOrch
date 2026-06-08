---
title: CSV Export & Import
nav_order: 5
permalink: /csv-export-import/
---

# UiPathOrch Module - CSV Export & Import Guide

- [Overview](#overview)
- [Exporting to CSV](#exporting-to-csv)
- [Importing from CSV](#importing-from-csv)

## Overview

Any cmdlet output can be exported to CSV using the standard PowerShell
`Select-Object | Export-Csv` pattern. However, producing a CSV that can be
imported back into a UiPathOrch cmdlet requires the column names to match
the target cmdlet's parameter names. To make this easy, selected Get cmdlets
have a built-in `-ExportCsv` parameter that exports entities in an importable
format.

## Exporting to CSV

### Built-in `-ExportCsv` Parameter

The `-ExportCsv` parameter exports entities with column names that match the
parameter names of the corresponding New or Update cmdlets, so the CSV can
be directly piped back via `Import-Csv`.

```powershell
Get-OrchProcess -Path Orch1:\Shared -ExportCsv C:\temp\processes.csv
Get-OrchQueue -Path Orch1:\Shared -ExportCsv C:\temp\queues.csv
Get-OrchAsset -Path Orch1:\Shared -ExportCsv C:\temp\assets.csv
```

To export all entities across all folders, add `-Recurse` at the root:

```powershell
Get-OrchProcess -Path Orch1:\ -Recurse -ExportCsv C:\temp\all-processes.csv
```

Use `-CsvEncoding` to specify the encoding.

#### Cmdlets with `-ExportCsv` and their import targets

| Export Cmdlet | Import Target |
|---|---|
| `Get-ChildItem -ExportCsv` (dir) | `Import-Csv` → `New-Item` |
| `Get-OrchProcess -ExportCsv` | `Import-Csv` → `New-OrchProcess` or `Update-OrchProcess` |
| `Get-OrchQueue -ExportCsv` | `Import-Csv` → `New-OrchQueue` or `Update-OrchQueue` |
| `Get-OrchMachine -ExportCsv` | `Import-Csv` → `New-OrchMachine` |
| `Get-OrchAsset -ExportCsv` | `Import-Csv` → `Set-OrchAsset` |
| `Get-OrchAsset -ExportCredentialCsv` | `Import-Csv` → `Set-OrchCredentialAsset` |
| `Get-OrchSecretAsset -ExportCsv` | `Import-Csv` → `Set-OrchSecretAsset` |
| `Get-OrchTrigger -ExportCsv` | `Import-Csv` → `New-OrchTrigger` |
| `Get-OrchApiTrigger -ExportCsv` | `Import-Csv` → `New-OrchApiTrigger` or `Update-OrchApiTrigger` |
| `Get-OrchUser -ExportCsv` | `Import-Csv` → `Add-OrchUser` |
| `Get-OrchRole -ExportCsv` | `Import-Csv` → `Set-OrchRole` |
| `Get-OrchFolderUser -ExportCsv` | `Import-Csv` → `Add-OrchFolderUser` |
| `Get-OrchFolderMachine -ExportCsv` | `Import-Csv` → `Add-OrchFolderMachine` |
| `Get-OrchBucket -ExportCsv` | `Import-Csv` → `New-OrchBucket` |
| `Get-OrchTestDataQueue -ExportCsv` | `Import-Csv` → `New-OrchTestDataQueue` |
| `Get-OrchTestSetSchedule -ExportCsv` | `Import-Csv` → `New-OrchTestSetSchedule` or `Update-OrchTestSetSchedule` |
| `Get-OrchActionCatalog -ExportCsv` | `Import-Csv` → `New-OrchActionCatalog` |
| `Get-OrchWebhook -ExportCsv` | `Import-Csv` → `New-OrchWebhook` or `Update-OrchWebhook` |
| `Get-OrchAssetLink -ExportCsv` | `Import-Csv` → `Add-OrchAssetLink` |
| `Get-OrchBucketLink -ExportCsv` | `Import-Csv` → `Add-OrchBucketLink` |
| `Get-OrchQueueLink -ExportCsv` | `Import-Csv` → `Add-OrchQueueLink` |
| `Get-OrchCalendar -ExportCsv` | `Import-Csv` → `Add-OrchCalendarDate` |
| `Get-OrchCalendarDate -ExportCsv` | `Import-Csv` → `Add-OrchCalendarDate` |
| `Get-PmGroup -ExportCsv` | `Import-Csv` → `New-PmGroup` |
| `Get-PmRobotAccount -ExportCsv` | `Import-Csv` → `Set-PmRobotAccount` (or `New-PmRobotAccount`) |
| `Get-PmUserPreference -ExportCsv` | `Import-Csv` → `Set-PmUserPreference` |
| `Get-PmNotificationSubscription -ExportCsv` | `Import-Csv` → `Set-PmNotificationSubscription` |
| `Get-PmUser -ExportCsv` | `Import-Csv` → `New-PmUser` or `Update-PmUser` |
| `Get-PmGroupMember -ExportCsv` | `Import-Csv` → `Add-PmGroupMember` |
| `Get-PmGroupLicense -ExportCsv` | `Import-Csv` → `Add-PmGroupLicense` |
| `Get-PmUserLicense -ExportCsv` | `Import-Csv` → `Add-PmUserLicense` |
| `Get-DuUser -ExportCsv` | `Import-Csv` → `Add-DuUser` |

#### Credential Assets (`-ExportCredentialCsv`)

`Get-OrchAsset -ExportCredentialCsv` exports credential assets with a
`CredentialPassword` column. The Orchestrator API cannot retrieve passwords,
so this column is exported empty. You must manually enter the passwords in
the CSV before importing with `Set-OrchCredentialAsset`.

```powershell
Get-OrchAsset -Path Orch1:\ -Recurse -ExportCredentialCsv C:\temp\creds.csv
# Manually enter the CredentialPassword column in the CSV
Import-Csv C:\temp\creds.csv | Set-OrchCredentialAsset
```

### Standard `Export-Csv`

Use `Select-Object` to choose columns, then pipe to `Export-Csv`.
Column names can be comma-separated and support wildcards:

```powershell
Get-OrchJob -Path Orch1:\Shared -Last 7d |
    Select-Object ReleaseName, State, *Time |
    Export-Csv C:\temp\jobs.csv -Encoding utf8BOM
```

You can use `[Ctrl+Space]` after `Select-Object` to auto-complete column
names.

### Targeting folders by path: `-Path` and `-LiteralPath`

Every folder-/drive-scoped cmdlet accepts both `-Path` (wildcards supported)
and `-LiteralPath` (literal — no wildcard interpretation). `-LiteralPath` has
`[Alias("PSPath")]`, so it binds from the automatic **`PSPath`** note-property
that `Get-ChildItem` / `Get-Item` attach to every item. You can therefore pipe
folders straight into a cmdlet to operate on each folder's *own* path.

The pipe earns its place when you first **filter or hand-pick** the folders —
something a `-Path` wildcard can't express, because path wildcards match each
segment separately and don't cross the hierarchy:

```powershell
# Match a name at ANY depth (a -Path wildcard only matches one level):
dir Orch1:\ -Recurse | Where-Object DisplayName -like '*Prod*' |
    Update-OrchProcess * -A4R_Enabled False -WhatIf

# ...or target a subset by a folder property the path can't express:
dir Orch1:\ -Recurse | Where-Object FeedType -eq FolderHierarchy |
    Update-OrchProcess * -A4R_Enabled False -WhatIf
```

When you just want *every* folder, no pipe is needed — `Update-OrchProcess`
already recurses on its own: `Update-OrchProcess -Recurse * -A4R_Enabled False -WhatIf`.

Folders follow the FileSystem-provider convention: a `dir` item exposes
`FullName` (its own path) and `PSPath`, **but no `Path` property** (a folder's
`Path` used to return its *parent*; that property was removed). To round-trip
through CSV and target each folder itself, export `PSPath`:

```powershell
# Export folder paths, (optionally edit the CSV,) then re-apply to each folder:
dir Orch1:\ -Recurse | Select-Object PSPath | Export-Csv C:\temp\dir.csv
Import-Csv C:\temp\dir.csv | Update-OrchProcess * -A4R_Enabled False -WhatIf
```

`Import-Csv` restores the `PSPath` column, which binds to `-LiteralPath`. The
provider-qualified form (`UiPathOrch\UiPathOrch::Orch1:\Finance`) is accepted —
the drive qualifier is resolved automatically. Prefer `-LiteralPath` over
`-Path` when a folder name contains a wildcard metacharacter (`[ ] * ?`).

## Importing from CSV

### How It Works

CSV column names must match the parameter names of the target cmdlet. When
a CSV is piped into a cmdlet, each column value is bound to the parameter
with the same name. This mechanism works with all cmdlets that accept
pipeline input, including New, Update, Set, Add, Copy, and Remove cmdlets.

**Importing never deletes what the CSV omits.** The import verb decides how
each row is applied, but none of them treat the CSV as the desired full state:

- `New-` / `Set-` / `Update-` — create or update the entity named by the row.
- `Add-` — add the row's items to the entity's existing collection (roles,
  licenses, folder links). Re-importing an unchanged export is a no-op.
- `Remove-` — delete the entity, or remove the row's items from a collection.

To *drop* something, delete it explicitly with the matching `Remove-` cmdlet —
removing a row from the CSV and re-importing does **not** remove the
corresponding entity or collection item.

For example, when you import the following CSV into `Copy-OrchAsset`:

| Path | Name | Destination |
|---|---|---|
| Orch1:\Shared | Asset#1 | Orch2:\Shared |
| Orch1:\Folder | Asset#2 | Orch2:\AnotherFolder |

Each row is equivalent to:

```powershell
Copy-OrchAsset -Path Orch1:\Shared -Name Asset#1 -Destination Orch2:\Shared
Copy-OrchAsset -Path Orch1:\Folder -Name Asset#2 -Destination Orch2:\AnotherFolder
```

Parameters specified on the command line override CSV column values:

```powershell
# Copy every asset listed in the CSV to one destination folder,
# regardless of the Destination column in the CSV
Import-Csv C:\temp\assets.csv | Copy-OrchAsset -Destination Orch2:\Shared
```

**Tips:**
- Empty cells in CSV are treated as "not specified" and do not overwrite
  existing values.
- Boolean values accept `true`/`false` (case-insensitive).
- Preview an import with `-WhatIf` before running it for real — the
  state-changing cmdlets (New, Update, Set, Add, Copy, Remove) support it,
  so each row reports what it *would* do without making any change:
  ```powershell
  Import-Csv C:\temp\assets.csv | Set-OrchAsset -WhatIf
  ```
- Use `Get-Help <CmdletName> -Parameter *` to see all available parameters
  and their types.

### Finding a cmdlet's CSV columns and creating a template

A CSV imports into a cmdlet when its **column headers match the cmdlet's parameter names**.
Here are three ways to get those headers and a starter file, easiest first.

**1. Export a real example.** For any cmdlet that has `-ExportCsv` (see the `-ExportCsv` table
under [Exporting to CSV](#exporting-to-csv) above), the quickest template is an actual export — it
already has the exact headers and real values to copy and edit. Even a single exported row is a
perfect template: keep the header, replace the values.

```powershell
Get-OrchQueue -Path Orch1:\Shared -ExportCsv C:\temp\queues.csv   # edit, then Import-Csv | New-OrchQueue
```

**2. Discover the columns for any import cmdlet.** The importable columns are the parameters that
bind from the pipeline by property name. This helper lists them — always current, nothing to go
stale:

```powershell
function Get-OrchCsvColumn {
    param([Parameter(Mandatory)][string] $CommandName)
    (Get-Command $CommandName).Parameters.Values |
        Where-Object {
            $_.Attributes | Where-Object {
                ($_ -is [System.Management.Automation.ParameterAttribute]) -and $_.ValueFromPipelineByPropertyName
            }
        } | ForEach-Object Name
}

Get-OrchCsvColumn New-OrchQueue
# Name, Description, AcceptAutomaticallyRetry, ... , RetentionAction, RetentionPeriod, Path, LiteralPath
```

`Get-Help New-OrchQueue -Parameter *` shows the same names with their types and which are
**required** — the minimum set of columns a row needs.

**3. Generate a header-only template** from those columns, fill in one row per entity, and import:

```powershell
(Get-OrchCsvColumn New-OrchQueue) -join ',' | Set-Content -Encoding utf8 C:\temp\new-queues.csv
# open the file, add a row per queue, then preview before committing:
Import-Csv C:\temp\new-queues.csv | New-OrchQueue -Path Orch1:\Shared -WhatIf
```

You rarely need every column — keep the ones you set and delete the rest; omitted/empty columns
fall back to defaults (see the Tips above).

#### How a row targets a folder

Every folder-scoped import cmdlet has **`Path`** (and `LiteralPath`) columns that say which folder
the row applies to. Put the folder in the `Path` column, **or** omit it and pass `-Path` on the
command line to apply every row to one folder:

```powershell
Import-Csv C:\temp\new-queues.csv | New-OrchQueue -Path Orch1:\Shared   # all rows -> Shared
```

A few verbs add their own columns: `Copy-Orch*` adds **`Destination`** (e.g. `Copy-OrchAsset`:
`Path, Name, Destination`), `Add-Orch*Link` uses `Path, Name, Link`, and folder creation
(`New-Item`) uses `Path, Name`.

> **Tip — let completion write the columns for you.** Because the CSV columns *are* the parameter
> names, you don't have to memorize them: type the cmdlet and press `[Tab]` or `[Ctrl+Space]` to
> cycle parameter names, and press it again after a value to complete folder paths, entity names,
> and enum values. See [Completion](00-GettingStarted.md#completion).

### Bulk Update via CSV

Export existing entities, edit the CSV, and re-import to update in bulk:

```powershell
# 1. Export current processes
Get-OrchProcess -Path Orch1:\Shared -ExportCsv C:\temp\processes.csv

# 2. Edit the CSV (e.g., change RetentionPeriod column for all rows)

# 3. Import and update
Import-Csv C:\temp\processes.csv | Update-OrchProcess
```

This pattern works for Update-OrchProcess, Update-OrchQueue, and other
Update cmdlets.

### Bulk Delete via CSV

A cmdlet does not need its own `-ExportCsv` parameter to take part in a CSV
workflow — any `Get-Orch*` output can be shaped with `Select-Object` and piped
through `Export-Csv`, then re-imported into a state-changing cmdlet. Entity
deletion can also be done in bulk via CSV import:

```powershell
# Enumerate all assets
Get-OrchAsset -Path Orch1:\ -Recurse | select Path,Name | Export-Csv c:RemoveAssets.csv

# Open .csv-associated editor, remove the assets you want to keep, and leave the ones you want to delete
c:RemoveAssets.csv      # press Tab to expand to the absolute path

# Bulk-delete the assets
Import-Csv c:RemoveAssets.csv | Remove-OrchAsset -WhatIf
```

Drop the `-WhatIf` once the preview lists exactly the rows you intend to delete.
`Remove-Orch*` cmdlets only need the columns that identify each entity (here
`Path` and `Name`), so a trimmed `Select-Object` projection is enough — the
other columns a full export would carry are simply ignored.

The same shape works for other `Remove-Orch*` cmdlets (for example
`Get-OrchQueue ... | select Path,Name | Export-Csv ...` then
`Import-Csv ... | Remove-OrchQueue`).

### Snapshot State, Change It, Restore It

A CSV can also record a *subset* of entities so you can restore exactly that
subset later. For example, record which triggers are currently enabled, disable
every trigger (e.g. for a maintenance window), then re-enable only the ones
that were on before:

```powershell
# Record the currently-enabled triggers
Get-OrchTrigger -Path Orch1:\ -Recurse | where Enabled -EQ $true | Export-Csv c:triggers.csv

# Disable all triggers
Disable-OrchTrigger -Path Orch1:\ -Recurse *

# ... maintenance window ...

# Re-enable only the triggers that were enabled before
Import-Csv c:triggers.csv | Enable-OrchTrigger -WhatIf
```

`Enable-OrchTrigger` binds each row's `Path` and `Name` from the CSV, so the
full `Get-OrchTrigger` export needs no trimming — the extra columns are
ignored. Drop the `-WhatIf` once the preview is correct.

### Sharing Assets, Buckets, and Queues Across Folders (Links)

`Get-Orch{Asset,Bucket,Queue}Link -ExportCsv` exports one row per
(source folder, entity, linked folder) with `Path` / `Name` / `Link`
columns, and `Add-Orch{Asset,Bucket,Queue}Link` imports them — so the
folder-sharing layout can be snapshotted, edited, and reapplied:

```powershell
Get-OrchAssetLink -Path Orch1:\ -Recurse -ExportCsv C:\temp\asset-links.csv
# edit / add / remove rows
Import-Csv C:\temp\asset-links.csv | Add-OrchAssetLink
```

`Add-Orch*Link` batches every row for the same entity into a single API
call. The share is all-or-nothing: if you lack permission on any one
target folder in a row's group, none of that entity's links are added and
the cmdlet reports the entity and folder that were rejected — fix or drop
that row and re-run.

### License Groups

`Get-PmGroupLicense -ExportCsv` exports one row per (group, license) with
`Path` / `GroupName` / `License` columns, and
`Add-PmGroupLicense` imports them — so a group's bundle
assignments can be snapshotted, edited, and reapplied:

```powershell
Get-PmGroupLicense -Path Orch1: -ExportCsv C:\temp\license-groups.csv
# edit / add / remove rows
Import-Csv C:\temp\license-groups.csv | Add-PmGroupLicense
```

The `License` column is the friendly bundle name (e.g.
`Attended - Named User`) — the same value `-License` tab-completes and
accepts. Rows naming the same group (case-insensitively) are merged into a
single update carrying the union of their licenses, so the license API's
atomic-replace behavior never lets one row drop another's bundles. Adding
a license a group already holds is a no-op, so re-importing an unedited
export changes nothing.

The round trip is **additive** — like `Get-OrchUser -ExportCsv` →
`Add-OrchUser`, the CSV adds licenses and never revokes the ones you leave
out.

> To **remove** a group's licenses from a CSV instead, pipe the same shape
> into `Remove-PmGroupLicense`.

### License Users

`Get-PmUserLicense -ExportCsv` is the user-level counterpart of the License
Groups round trip above. It exports one row per (user, license) with
`Path` / `UserName` / `License` columns, and `Add-PmUserLicense`
imports them:

```powershell
Get-PmUserLicense -Path Orch1: -ExportCsv C:\temp\license-users.csv
# edit / add / remove rows
Import-Csv C:\temp\license-users.csv | Add-PmUserLicense
```

The `UserName` column carries the user's login (which the License Accountant
API returns in its `name` field; the `email` field is empty there). It binds
to the Add cmdlet's `-Email` parameter through that parameter's `-UserName`
alias; `License` is the friendly bundle name. Orphaned license rows (licenses
not tied to a directory user) are left out of the export, since they can't be
re-assigned to a user on import. As with groups, the round trip is
**additive** — rows for the same user fold into a single atomic-replace
update, adding a license the user already holds is a no-op, and re-importing
an unedited export changes nothing.

> To **remove** a user's licenses from a CSV instead, pipe the same shape
> into `Remove-PmUserLicense`.

### Portal Preferences

`Get-PmUserPreference -ExportCsv` exports your own portal preferences (theme,
language, favorites, ...) with `Path` / `Key` / `Value` columns, and
`Set-PmUserPreference` imports them — so you can snapshot, edit, and reapply
them:

```powershell
Get-PmUserPreference -ExportCsv C:\temp\prefs.csv
# edit the Value column
Import-Csv C:\temp\prefs.csv | Set-PmUserPreference
```

The cmdlets act on the connected user only (there is no `-UserName`), and rows
for the same drive fold into a single request. `Copy-PmUserPreference` migrates
the same preferences to yourself in another organization without a CSV. Note
that a value written this way is stored correctly, but an already-signed-in
browser keeps rendering the previous theme/language until you re-sign-in or
clear the site's local storage (see [Troubleshooting](90-Troubleshooting.md)).

### Notification Subscriptions

`Get-PmNotificationSubscription -ExportCsv` exports your own notification
subscriptions — one row per `(topic, mode)` — with
`Path` / `Publisher` / `Group` / `Topic` / `DisplayName` / `Mode` / `IsSubscribed`
columns, and `Set-PmNotificationSubscription` imports them, so you can snapshot
your notification choices, flip the ones you want, and reapply:

```powershell
Get-PmNotificationSubscription -ExportCsv C:\temp\notif.csv
# edit the IsSubscribed column (true / false)
Import-Csv C:\temp\notif.csv | Set-PmNotificationSubscription
```

`Set-PmNotificationSubscription` binds `Path`, `Topic`, `Mode` and `IsSubscribed`;
`Publisher` / `Group` / `DisplayName` are context columns that make the file
readable and are ignored on import. Rows for the same drive fold into one
request, and topics are matched by name. Mandatory topics are read-only — the
service rejects a row that flips one off.

`IsSubscribed` is stored as the text `true` / `false`, which the cmdlet parses
back. This is also *why* `-Subscribed` is a `[string]` parameter rather than a
`[bool]`: `Import-Csv` produces only strings, and a PowerShell `[bool]` / `[bool?]`
parameter rejects string values (it binds `$true` / `$false` / `1` / `0`, not
`"true"` / `"false"`), so a boolean-typed parameter could not round-trip through
CSV at all. The same applies to the other `true`/`false` columns elsewhere in
this guide.

`Copy-PmNotificationSubscription` migrates the same choices to yourself in
another organization directly (no CSV needed).

### Creating Folders in Bulk via CSV

1. Export the folder structure from the source tenant:
   ```powershell
   dir -Recurse -ExportCsv C:\temp\folders.csv
   ```
2. Replace the drive name in the `Path` column (e.g., `Orch1:` → `Orch2:`).
   The hierarchy in the `Path` column must be preserved for subfolders to be
   created in the correct location.
3. Import and create:
   ```powershell
   Import-Csv C:\temp\folders.csv | New-Item
   ```

### Copying, Moving, and Deleting Folders in Bulk via CSV

Whole folders (with their contents) are copied, moved, and deleted with the built-in `Copy-Item`,
`Move-Item`, and `Remove-Item` (see [Folder Operations](08-FolderOperations.md)). Drive a bulk run
from a CSV of folder paths.

The natural workflow is **export every folder, edit the CSV, then import.** Name the path column
**`Path`** (the parameter every one of these cmdlets uses) and the rows pipe straight in — no
`ForEach-Object`, even with a second column.

1. **Export every folder** (`-Recurse`) so you can see the whole tree and pick from it. `PSPath`
   is each folder's own path (press `[Ctrl+Space]` after `Select-Object` to complete the name):
   ```powershell
   dir Orch1:\ -Recurse | Select-Object PSPath | Export-Csv C:\temp\folders.csv
   ```
2. **Edit the CSV**: rename the `PSPath` column header to **`Path`**, keep the rows you want, and —
   to move/copy each folder to its own target — add a `Destination` column with each target folder.
3. **Import.** The columns bind to the parameters by name, so the rows pipe straight in:
   ```powershell
   # Delete each listed folder (and everything in it)
   Import-Csv C:\temp\folders.csv | Remove-Item -Recurse -WhatIf

   # Move / copy each folder to its row's Destination column
   Import-Csv C:\temp\folders.csv | Move-Item -WhatIf
   Import-Csv C:\temp\folders.csv | Copy-Item -WhatIf

   # ...or send them all to one folder with -Destination on the command line
   # (then the CSV needs only the Path column)
   Import-Csv C:\temp\folders.csv | Move-Item -Destination Orch1:\Archive -WhatIf
   ```
   Drop `-WhatIf` once the preview is right.

Notes:

- **`-Recurse` (delete)** is required because each folder is removed *with everything inside it*;
  without it `Remove-Item` stops at a non-empty folder with *"…has children and the Recurse
  parameter was not specified"*. `-Force` does **not** substitute — it skips confirmations, not the
  `-Recurse` requirement.
- **`Copy-Item`** copies each listed folder and its entities, but not its subfolders — those come
  in as their own rows. To clone a whole subtree in one shot, copy its root directly instead:
  `Copy-Item Orch1:\Src Orch2:\ -Recurse` (see the [Migration & Copy Guide](04-MigrationGuide.md)).
- **Why `Path`, not `LiteralPath`?** `-Path` binds both by value *and* by property name, so a
  `Path` column binds it by name and can sit beside a `Destination`/`NewName` column. `-LiteralPath`
  is by-property-name only, so a two-column row instead binds the whole row object to `-Path` *by
  value* and fails (*"a drive with the name '@{LiteralPath=…' does not exist"*). The one trade-off:
  `-Path` treats `[ ] * ?` as wildcards, so for a folder whose name contains one of those, bind
  `-LiteralPath` yourself in a `ForEach-Object`, e.g.
  `Import-Csv … | ForEach-Object { Remove-Item -LiteralPath $_.Path -Recurse }`.

### Renaming Folders in Bulk via CSV

Same workflow, with a `NewName` column:

1. Export the folders and open the CSV:
   ```powershell
   dir Orch1:\ -Recurse | Select-Object PSPath | Export-Csv C:\temp\rename.csv
   ```
2. Rename the `PSPath` header to **`Path`**, **add a `NewName` column** with each new (leaf) name,
   and keep just the rows you want.
3. Import:
   ```powershell
   # rename.csv columns: Path, NewName
   Import-Csv C:\temp\rename.csv | Rename-Item -WhatIf
   ```

Drop `-WhatIf` once the preview is right.

## Known limitations: commas and wildcards in multi-value cells

A few columns hold multiple values in one cell: a row's `Roles`, a machine's
`RobotUsers`, a trigger's `ExecutorRobots`, and a user's or robot account's group
memberships. These are exported as a single comma-joined cell and split back on
import. A value that itself contains a comma still round-trips: the export escapes
an in-element comma (writing it as a backtick followed by the comma) and the
import treats that escaped comma as a literal, not a delimiter.

The following are **not yet** fully round-trip-safe (marked `TODO(csv-escape)` in
the source):

- A literal backtick inside a value, on the resolving (non-wildcard) import path,
  can be dropped.

Workaround: put each value in its own row (or its own array element) instead of a
comma-joined cell, and avoid commas, backticks, and wildcard characters in the
value itself.
