---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchJobStats.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchJobStats
---

# Get-OrchJobStats

## SYNOPSIS

Gets job statistics from UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
Get-OrchJobStats [[-Path] <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets job count statistics from UiPath Orchestrator. The cmdlet returns CountStats objects that contain aggregated job counts grouped by status (such as Successful, Faulted, Stopped, and Running).

The -Path parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available drives.

Primary Endpoint: GET /api/Stats/GetJobsStats

OAuth required scopes: OR.Monitoring or OR.Monitoring.Read

Required permissions: Jobs.View

## EXAMPLES

### Example 1: Get job statistics for the current drive

```powershell
PS Orch1:\> Get-OrchJobStats
```

Gets job statistics from the current Orchestrator drive.

### Example 2: Get job statistics from a specific drive

```powershell
PS C:\> Get-OrchJobStats Orch1:
```

Gets job statistics from the Orch1: drive by specifying the drive name as a positional parameter.

### Example 3: Get job statistics from multiple drives

```powershell
PS C:\> Get-OrchJobStats Orch1:,Orch2:
```

Gets job statistics from both Orch1: and Orch2: drives. The queries run in parallel across drives.

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

### System.String[]

You can pipe drive names to this cmdlet via the Path property.

## OUTPUTS

### UiPath.PowerShell.Entities.CountStats

Returns CountStats objects containing aggregated job count statistics grouped by status.

## NOTES

This cmdlet queries the Orchestrator Stats API to retrieve job statistics. When multiple drives are specified, queries are executed in parallel using the thread pool.

## RELATED LINKS

[Get-OrchPSDrive](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchPSDrive.md)
