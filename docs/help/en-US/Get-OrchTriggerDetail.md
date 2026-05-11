---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchTriggerDetail.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 05/11/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchTriggerDetail
---

# Get-OrchTriggerDetail

## SYNOPSIS

Gets per-trigger detailed information from UiPath Orchestrator folders.

## SYNTAX

### __AllParameterSets

```
Get-OrchTriggerDetail [-Name] <string[]> [-Path <string[]>] [-Recurse] [-Depth <uint>]
 [-ExportCsv <string>] [-CsvEncoding <Encoding>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets the per-trigger detail payload (StopProcessExpression, KillProcessExpression, ExecutorRobots, MachineRobots, calendar/queue references, etc.) from UiPath Orchestrator folders. The list endpoint backing `Get-OrchTrigger` returns shallow ProcessSchedule records; this cmdlet calls the per-id detail endpoint for each matched trigger and additionally side-fetches the trigger's executor-robot assignments (those come from a separate endpoint).

`-Name` is mandatory by design — the detail path makes one API call per matched trigger plus a side fetch for ExecutorRobots, so a default "all triggers" would fan out unexpectedly on large folders. Wildcards (including `*`) are accepted; the user just has to type the selector explicitly.

The CSV format produced by `-ExportCsv` matches the shape used by `Get-OrchTrigger -ExportCsv`. ExecutorRobots and MachineRobots IDs are resolved to human-readable names before writing.

The -Name and -Path parameters support tab completion. -Name completion is dynamically populated from actual triggers in the target folders.

Primary Endpoint: GET /odata/ProcessSchedules({processScheduleId})
Secondary endpoint: GET /odata/ProcessSchedules({id})/UiPath.Server.Configuration.OData.GetRobotIdsForSchedule for ExecutorRobots.

OAuth required scopes: OR.Execution or OR.Execution.Read

Required permissions: ProcessSchedules.View

## EXAMPLES

### Example 1: Detail for all triggers in the current folder

```powershell
PS Orch1:\Shared> Get-OrchTriggerDetail -Name '*'
```

Returns the detailed payload for every trigger in `Orch1:\Shared`. The explicit `*` reminds the user that the operation fans out across every trigger; omitting `-Name` would prompt instead of silently fetching everything.

### Example 2: Detail for a specific trigger

```powershell
PS Orch1:\Shared> Get-OrchTriggerDetail Daily_Cleanup
```

Returns the detailed payload for the named trigger. `-Name` is positional (Position 0), so the parameter name can be omitted.

### Example 3: Recursive across a folder tree

```powershell
PS Orch1:\> Get-OrchTriggerDetail -Path Shared -Name '*' -Recurse
```

Walks every folder under `Shared` (recursively) and emits detail for every matched trigger.

### Example 4: Export to CSV

```powershell
PS C:\> Get-OrchTriggerDetail -Path Orch1:\Shared -Name '*' -ExportCsv C:\temp
```

Exports the detailed payload of every trigger in `Orch1:\Shared` to `C:\temp\ExportedTriggers.csv`. ExecutorRobots and MachineRobots IDs are resolved to human-readable names in the CSV.

## PARAMETERS

### -Name

Specifies the trigger names to retrieve detail for. Mandatory by design — see DESCRIPTION. Wildcards (including `*`) are accepted.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: (All)
  Position: 0
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Path

Specifies the target folders. If not specified, the current folder is targeted.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
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

### -Recurse

Walks subfolders recursively from each `-Path` root.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: False
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Depth

Limits how deep `-Recurse` descends.

```yaml
Type: System.UInt32
DefaultValue: 0
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -ExportCsv

Specifies the directory or file path to export trigger details as a CSV file. If a directory path is specified, the default file name `ExportedTriggers.csv` is used.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -CsvEncoding

Specifies the encoding for CSV export. Default is UTF-8 with BOM for Excel compatibility. Tab completion suggests all available system encodings (e.g., utf-8, shift_jis, us-ascii).

```yaml
Type: System.Text.Encoding
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
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

You can pipe trigger names to this cmdlet via the Name property.

## OUTPUTS

### UiPath.PowerShell.Entities.ProcessSchedule

Returns the detailed ProcessSchedule entity (one per matched trigger).

## NOTES

Each matched trigger triggers one detail-endpoint call plus a side fetch for ExecutorRobots. For large folders, prefer narrow `-Name` wildcards over `*` to limit fan-out.

## RELATED LINKS

[Get-OrchTrigger](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchTrigger.md)

[New-OrchTrigger](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/New-OrchTrigger.md)

[Update-OrchTrigger](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Update-OrchTrigger.md)

[Remove-OrchTrigger](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchTrigger.md)
