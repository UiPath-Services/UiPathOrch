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
# Import all assets listed in the CSV into the current folder,
# regardless of the Path column in the CSV
Import-Csv C:\temp\assets.csv | Set-OrchAsset -Path .
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

### Renaming Folders in Bulk via CSV

`dir -ExportCsv` exports folders with a `Name` column, but `Rename-Item`
expects a `NewName` parameter. Rename the column before importing:

1. Export folders:
   ```powershell
   dir -Recurse -ExportCsv C:\temp\folders.csv
   ```
2. Open the CSV and rename the `Name` column header to `NewName`.
3. Edit the `NewName` values to the desired new names.
4. Import and rename:
   ```powershell
   Import-Csv C:\temp\folders.csv | Rename-Item
   ```
