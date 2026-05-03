---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchTaskAcrossFolder.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 04/28/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchTaskAcrossFolder
---

# Get-OrchTaskAcrossFolder

## SYNOPSIS

Gets action-center tasks tenant-wide via the dedicated cross-folder endpoint.

## SYNTAX

### __AllParameterSets

```
Get-OrchTaskAcrossFolder [[-Title] <string[]>] [-Status <string[]>] [-Priority <string[]>]
 [-Path <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Returns tasks from every folder the caller has Tasks.View on, in a single API call. Distinct from `Get-OrchTask -Recurse` which iterates per folder; this cmdlet hits the dedicated server-side aggregation endpoint and is much faster for tenant-wide queries.

Useful for cross-folder dashboards, reports, or audits where you need the full picture of pending action work in the tenant. Each output object's Path is set to the drive's root (e.g., `Orch1:\`) since the result is not folder-scoped.

Primary Endpoint: GET /odata/Tasks/UiPath.Server.Configuration.OData.GetTasksAcrossFolders

OAuth required scopes: OR.Tasks or OR.Tasks.Read

Required permissions: Tasks.View

## EXAMPLES

### Example 1: Get every task in the tenant

```powershell
PS C:\> Get-OrchTaskAcrossFolder -Path Orch1:\
```

Returns every task across the Orch1 tenant in one API call.

### Example 2: Group by folder

```powershell
PS Orch1:\> Get-OrchTaskAcrossFolder | Group-Object OrganizationUnitId
```

Tabulates how many tasks each folder owns.

### Example 3: Status histogram

```powershell
PS Orch1:\> Get-OrchTaskAcrossFolder | Group-Object Status | Format-Table Count, Name
```

Counts Unassigned vs. Pending vs. Completed tenant-wide.

## PARAMETERS

### -Title

Specifies the task title(s) to match. Supports wildcards and multiple comma-separated values.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
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

### -Status

Filters by task status. Valid values: Unassigned, Pending, Completed.

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
AcceptedValues:
- Unassigned
- Pending
- Completed
HelpMessage: ''
```

### -Priority

Filters by task priority. Valid values: Low, Medium, High, Critical.

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
AcceptedValues:
- Low
- Medium
- High
- Critical
HelpMessage: ''
```

### -Path

Specifies the name of the target drive(s). If not specified, the current drive is targeted.

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe Title, Status, or Priority values to this cmdlet via property name binding.

## OUTPUTS

### UiPath.PowerShell.Entities.OrchTask

Returns OrchTask objects with the same shape as Get-OrchTask. The Path property is set to the drive root because results are aggregated cross-folder.

## NOTES

Because Path is the drive root rather than a folder path, downstream cmdlets that need folder context (e.g., Set-OrchTask, Remove-OrchTask) must be given an explicit -Path or rely on each task's OrganizationUnitId rather than the pipeline-bound Path.

## RELATED LINKS

Get-OrchTask

Set-OrchTask

Remove-OrchTask
