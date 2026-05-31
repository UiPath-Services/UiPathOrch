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
| `Get-OrchTrigger -ExportCsv` | `Import-Csv` → `New-OrchTrigger` |
| `Get-OrchApiTrigger -ExportCsv` | `Import-Csv` → `New-OrchApiTrigger` or `Update-OrchApiTrigger` |
| `Get-OrchUser -ExportCsv` | `Import-Csv` → `Add-OrchUser` |
| `Get-OrchRole -ExportCsv` | `Import-Csv` → `Set-OrchRole` |
| `Get-OrchFolderUser -ExportCsv` | `Import-Csv` → `Add-OrchFolderUser` |
| `Get-OrchFolderMachine -ExportCsv` | `Import-Csv` → `Add-OrchFolderMachine` |
| `Get-OrchBucket -ExportCsv` | `Import-Csv` → `New-OrchBucket` |
| `Get-OrchTestDataQueue -ExportCsv` | `Import-Csv` → `New-OrchTestDataQueue` |
| `Get-OrchActionCatalog -ExportCsv` | `Import-Csv` → `New-OrchActionCatalog` |
| `Get-OrchWebhook -ExportCsv` | `Import-Csv` → `New-OrchWebhook` or `Update-OrchWebhook` |
| `Get-OrchAssetLink -ExportCsv` | `Import-Csv` → `Add-OrchAssetLink` |
| `Get-OrchBucketLink -ExportCsv` | `Import-Csv` → `Add-OrchBucketLink` |
| `Get-OrchQueueLink -ExportCsv` | `Import-Csv` → `Add-OrchQueueLink` |
| `Get-OrchCalendarDate -ExportCsv` | `Import-Csv` → `Add-OrchCalendarDate` |
| `Get-PmGroup -ExportCsv` | `Import-Csv` → `New-PmGroup` |
| `Get-PmRobotAccount -ExportCsv` | `Import-Csv` → `Set-PmRobotAccount` |
| `Get-PmLicensedGroup -ExportCsv` | `Import-Csv` → `Add-PmLicenseToPmLicensedGroup` |

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

`Get-PmLicensedGroup -ExportCsv` exports one row per (group, license) with
`Path` / `GroupName` / `License` columns, and
`Add-PmLicenseToPmLicensedGroup` imports them — so a group's bundle
assignments can be snapshotted, edited, and reapplied:

```powershell
Get-PmLicensedGroup -Path Orch1: -ExportCsv C:\temp\license-groups.csv
# edit / add / remove rows
Import-Csv C:\temp\license-groups.csv | Add-PmLicenseToPmLicensedGroup
```

The `License` column is the friendly bundle name (e.g.
`Attended - Named User`) — the same value `-License` tab-completes and
accepts. Rows naming the same group (case-insensitively) are merged into a
single update carrying the union of their licenses, so the license API's
atomic-replace behavior never lets one row drop another's bundles. Adding
a license a group already holds is a no-op, so re-importing an unedited
export changes nothing.

> To **remove** a group's licenses from a CSV instead, pipe the same shape
> into `Remove-PmLicenseFromPmLicensedGroup`.

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
