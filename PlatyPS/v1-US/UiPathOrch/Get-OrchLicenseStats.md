---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchLicenseStats
---

# Get-OrchLicenseStats

## SYNOPSIS

Gets the licensing usage statistics.

## SYNTAX

### __AllParameterSets

```
Get-OrchLicenseStats [[-Last] <string>] [-Path <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

The `Get-OrchLicenseStats` cmdlet retrieves licensing usage statistics from UiPath Orchestrator. It returns LicenseStatsModel objects containing the robot type, usage count, and timestamp for each data point.

The `-Last` parameter specifies the time period for which to retrieve statistics. If not specified, the default period is the last 7 days (equivalent to `Week`). Valid values are: Day, Week, Month, 3Months, 6Months, Year, 3Years. The `-Last` parameter supports tab completion for these values.

Results are ordered by robot type and then by timestamp.

The cmdlet supports multi-threaded processing when querying multiple drives.

Primary Endpoint: /api/Stats/GetLicenseStats?tenantId={tenantId}&days={days}

OAuth required scopes: OR.Monitoring or OR.Monitoring.Read

Required permissions: License.View

## EXAMPLES

### Example 1: Get license statistics for the default period

```powershell
PS Orch1:\> Get-OrchLicenseStats
```

Gets license statistics for the last 7 days from the current drive.

### Example 2: Get license statistics for the last month

```powershell
PS Orch1:\> Get-OrchLicenseStats Month
```

Gets license statistics for the last month.

### Example 3: Get license statistics for the last year

```powershell
PS Orch1:\> Get-OrchLicenseStats Year
```

Gets license statistics for the last year.

### Example 4: Get license statistics from a specific drive

```powershell
PS C:\> Get-OrchLicenseStats -Path Orch1: -Last Week
```

Gets license statistics for the last week from the drive named `Orch1`.

## PARAMETERS

### -Path

Specifies the name of the target drives.
If not specified, the current drive is targeted.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Last

Specifies the most recent period for retrieving statistics. Valid values are: Day, Week, Month, 3Months, 6Months, Year, 3Years. If not specified, the default period is 7 days (equivalent to `Week`). Tab completion suggests available values.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 0
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String

You can pipe a time period string to the **Last** parameter.

### System.String[]

You can pipe drive names to the **Path** parameter.

## OUTPUTS

### UiPath.PowerShell.Entities.LicenseStatsModel

This cmdlet returns LicenseStatsModel objects containing the robot type, usage count, and timestamp for each data point.

## NOTES

This cmdlet is tenant-scoped and supports multi-threaded processing when querying multiple drives.


## RELATED LINKS

Get-OrchLicense

Get-OrchLicenseNamedUser

Get-OrchLicenseRuntime
