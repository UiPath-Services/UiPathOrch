# UiPathOrch Module - CSV Export & Import Guide

## Overview

UiPathOrch provides two approaches for working with CSV files:

1. **Built-in `-ExportCsv` parameter** on selected Get cmdlets: Exports
   entities in a format that can be directly imported back via pipeline.
2. **Standard PowerShell `Export-Csv`**: Works with any cmdlet output but
   requires manual column selection.

## Exporting to CSV

### Method 1: Built-in `-ExportCsv` (Recommended for round-trip)

Selected Get cmdlets have a `-ExportCsv` parameter that exports entities in a
format compatible with the corresponding New or Update cmdlets.

```powershell
Get-OrchProcess -Path Orch1:\Shared -ExportCsv C:\temp\processes.csv
Get-OrchQueue -Path Orch1:\Shared -ExportCsv C:\temp\queues.csv
Get-OrchMachine -Path Orch1: -ExportCsv C:\temp\machines.csv
Get-OrchAsset -Path Orch1:\Shared -ExportCsv C:\temp\assets.csv
```

Use `-CsvEncoding` to specify the encoding (default is UTF-8 with BOM):

```powershell
Get-OrchProcess -ExportCsv C:\temp\processes.csv -CsvEncoding utf8BOM
```

### Cmdlets with `-ExportCsv` and their import targets

| Export Cmdlet | Import Target |
|---|---|
| `Get-ChildItem -ExportCsv` (dir) | `Import-Csv \| New-Item` |
| `Get-OrchProcess -ExportCsv` | `Import-Csv \| New-OrchProcess` or `Update-OrchProcess` |
| `Get-OrchQueue -ExportCsv` | `Import-Csv \| New-OrchQueue` or `Update-OrchQueue` |
| `Get-OrchMachine -ExportCsv` | `Import-Csv \| New-OrchMachine` |
| `Get-OrchAsset -ExportCsv` | `Import-Csv \| Set-OrchAsset` |
| `Get-OrchAsset -ExportCredentialCsv` | `Import-Csv \| Set-OrchCredentialAsset` |
| `Get-OrchTrigger -ExportCsv` | `Import-Csv \| New-OrchTrigger` |
| `Get-OrchUser -ExportCsv` | `Import-Csv \| Add-OrchUser` |
| `Get-OrchRole -ExportCsv` | `Import-Csv \| Set-OrchRole` |
| `Get-OrchFolderUser -ExportCsv` | `Import-Csv \| Add-OrchFolderUser` |
| `Get-OrchFolderMachine -ExportCsv` | `Import-Csv \| Add-OrchFolderMachine` |
| `Get-OrchBucket -ExportCsv` | `Import-Csv \| New-OrchBucket` |
| `Get-OrchCalendar -ExportCsv` | `Import-Csv \| (manual)` |
| `Get-PmGroup -ExportCsv` | `Import-Csv \| New-PmGroup` |
| `Get-PmRobotAccount -ExportCsv` | `Import-Csv \| Set-PmRobotAccount` |

To export all entities across all folders, run at the root with `-Recurse`:

```powershell
Get-OrchProcess -Path Orch1:\ -Recurse -ExportCsv C:\temp\all-processes.csv
```

### Method 2: Standard `Export-Csv`

Use `Select-Object` to choose columns, then pipe to `Export-Csv`:

```powershell
Get-OrchJob -Path Orch1:\Shared -Last 7d |
    Select-Object ReleaseName, State, StartTime, EndTime |
    Export-Csv C:\temp\jobs.csv -Encoding utf8BOM
```

You can use `[Ctrl+Space]` after `Select-Object` to auto-complete column names.

## Importing from CSV

### Basic Import Pattern

Most UiPathOrch cmdlets accept pipeline input where CSV column names map to
parameter names:

```powershell
Import-Csv C:\temp\processes.csv | Update-OrchProcess
```

The column values of the CSV file are passed to parameters with the same name.
For example, a CSV with columns `Path`, `Name`, `Description` feeds values to
the `-Path`, `-Name`, and `-Description` parameters of the cmdlet.

### Overriding CSV Values with Parameters

Parameters specified on the command line override CSV column values:

```powershell
# Update all processes listed in CSV, but force them into Orch1:\Shared
Import-Csv C:\temp\processes.csv | Update-OrchProcess -Path Orch1:\Shared
```

### Bulk Update via CSV

Export existing entities, edit the CSV, and re-import to update in bulk:

```powershell
# 1. Export current processes
Get-OrchProcess -Path Orch1:\Shared -ExportCsv C:\temp\processes.csv

# 2. Edit the CSV (e.g., change RetentionPeriod column for all rows)

# 3. Import and update
Import-Csv C:\temp\processes.csv | Update-OrchProcess
```

This pattern works for Update-OrchProcess, Update-OrchQueue, and other Update
cmdlets.

### CSV Column Reference for Update Cmdlets

#### Update-OrchProcess

| Column | Type | Description |
|---|---|---|
| Path | string | Target folder path (e.g., `Orch1:\Shared`) |
| Name | string | Process name (supports wildcards) |
| Version | string | Package version |
| Description | string | Process description |
| EntryPoint | string | Entry point path (e.g., `Main.xaml`) |
| InputArguments | string | JSON input arguments |
| SpecificPriorityValue | int | Priority value (5-95) |
| HiddenForAttendedUser | bool | Hide from attended users |
| RemoteControlAccess | string | `None`, `ReadOnly`, `Full` |
| RetentionAction | string | `Delete` or `Archive` |
| RetentionPeriod | int | Days to retain |
| RetentionBucket | string | Bucket name for Archive |
| StaleRetentionAction | string | `Delete` or `Archive` |
| StaleRetentionPeriod | int | Days to retain stale |
| StaleRetentionBucket | string | Bucket name for stale Archive |
| ErrorRecordingEnabled | bool | Enable error recording |
| Quality | int | Screenshot quality |
| Frequency | int | Screenshot frequency (ms) |
| Duration | int | Screenshot duration (s) |
| AutoStartProcess | bool | Auto-start process |
| AlwaysRunning | bool | Keep process always running |
| A4R_Enabled | bool | Enable Healing Agent |
| A4R_HealingEnabled | bool | Enable self-healing |
| VideoRecordingType | string | `None`, `Failed`, `All` |
| QueueItemVideoRecordingType | string | `None`, `Failed` |
| MaxDurationSeconds | int | Max video duration (s) |
| Tags | string | Comma-separated tags |

#### Update-OrchQueue

| Column | Type | Description |
|---|---|---|
| Path | string | Target folder path |
| Name | string | Queue name (supports wildcards) |
| NewName | string | New queue name |
| Description | string | Queue description |
| AcceptAutomaticallyRetry | bool | Auto-retry failed items |
| RetryAbandonedItems | bool | Retry abandoned items |
| MaxNumberOfRetries | int | Maximum retries |
| Release | string | Associated process name |
| SlaInMinutes | int | SLA timeout (minutes) |
| RiskSlaInMinutes | int | Risk SLA timeout (minutes) |
| SpecificDataJsonSchema | string | Specific data JSON schema |
| OutputDataJsonSchema | string | Output data JSON schema |
| AnalyticsDataJsonSchema | string | Analytics data JSON schema |
| RetentionAction | string | `Delete` or `Archive` |
| RetentionPeriod | int | Days to retain |
| RetentionBucket | string | Bucket name for Archive |
| StaleRetentionAction | string | `Delete` or `Archive` |
| StaleRetentionPeriod | int | Days to retain stale |
| StaleRetentionBucket | string | Bucket name for stale Archive |
| Tags | string | Comma-separated tags |

### Tips

- Empty cells in CSV are treated as "not specified" and do not overwrite
  existing values.
- To clear a string property, the CSV cell must contain an empty string
  (not be absent).
- Boolean values accept `true`/`false` (case-insensitive).
- Use `Get-Help <CmdletName> -Parameter *` to see all available parameters
  and their types.
