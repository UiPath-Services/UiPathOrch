---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchTrigger.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchTrigger
---

# Get-OrchTrigger

## SYNOPSIS

Gets triggers from UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
Get-OrchTrigger [[-Name] <string[]>] [-Path <string[]>] [-Recurse] [-Depth <uint>] [-ExpandDetails]
 [-ExportCsv <string>] [-CsvEncoding <Encoding>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets trigger (process schedule) information from UiPath Orchestrator folders. Triggers define time-based or queue-based schedules for starting automation processes.

By default, this cmdlet returns basic trigger properties such as Name, ReleaseName, Enabled, StartProcessCron, and RuntimeType. When -ExpandDetails is specified, the cmdlet fetches individual trigger details by calling GET /odata/ProcessSchedules({processScheduleId}) for each trigger, providing additional properties.

The -Name parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available trigger names dynamically populated from the target folders. Multiple values can be specified using comma-separated text that includes wildcards.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: GET /odata/ProcessSchedules, GET /odata/ProcessSchedules({processScheduleId})

OAuth required scopes: OR.Execution or OR.Execution.Read

Required permissions: Schedules.View

## EXAMPLES

### Example 1: Get all triggers in the current folder

```powershell
PS Orch1:\root> Get-OrchTrigger
```

Gets all triggers from the current folder and returns basic trigger information.

### Example 2: Get a trigger by name

```powershell
PS Orch1:\root> Get-OrchTrigger "high trigger"
```

Gets the trigger named "high trigger" from the current folder. The name contains a space so it must be quoted. The -Name parameter is positional (position 0).

### Example 3: Get triggers using a wildcard

```powershell
PS Orch1:\root> Get-OrchTrigger *trigger*
```

Gets all triggers whose name contains "trigger" from the current folder.

### Example 4: Get triggers from a specific folder

```powershell
PS C:\> Get-OrchTrigger -Path Orch1:\root *trigger*
```

Gets triggers whose name contains "trigger" from the root folder. When -Path uses an absolute path (Orch1:\...), the command can be run from any location.

### Example 5: Get triggers recursively from all folders

```powershell
PS Orch1:\> Get-OrchTrigger -Recurse
```

Gets all triggers from all folders recursively. When run from the root folder, this shows all triggers across all folders in the tenant.

### Example 6: Export triggers to CSV

```powershell
PS Orch1:\root> Get-OrchTrigger -ExportCsv c:triggers.csv
```

Exports all triggers in the current folder to a CSV file. The CSV includes comprehensive columns such as Path, Name, ReleaseName, Enabled, SpecificPriorityValue, StartStrategy, StopStrategy, StopProcessExpression, KillProcessExpression, AlertPendingExpression, AlertRunningExpression, ConsecutiveJobFailuresThreshold, JobFailuresGracePeriodInHours, RuntimeType, InputArguments, ResumeOnSameContext, RunAsMe, IsConnected, CalendarName, ActivateOnJobComplete, ItemsActivationThreshold, ItemsPerJobActivationTarget, MaxJobsForActivation, StartProcessCron, StartProcessCronDetails, QueueDefinitionName, TimeZoneId, StopProcessDate, ExecutorRobots, and MachineRobots. When the current location is an Orch: drive, prefix the filename with c: to write to the filesystem.

## PARAMETERS

### -Path

Specifies the target folders. If not specified, the current folder is targeted. Supports wildcards and comma-separated values for multiple folders.

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

Includes the target folder and all its subfolders in the operation.

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

Specifies the depth for recursion into the target folders. A depth of 0 targets only the current folder with no subfolders included. When -Depth is specified, -Recurse is implied.

```yaml
Type: System.UInt32
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

### -ExpandDetails

Fetches detailed trigger information by calling GET /odata/ProcessSchedules({processScheduleId}) for each trigger individually. Without this switch, only the properties returned by the list endpoint are populated.

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

### -ExportCsv

Exports triggers to the specified CSV file path. The CSV includes comprehensive trigger information with headers: Path, Name, ReleaseName, Enabled, SpecificPriorityValue, StartStrategy, StopStrategy, StopProcessExpression, KillProcessExpression, AlertPendingExpression, AlertRunningExpression, ConsecutiveJobFailuresThreshold, JobFailuresGracePeriodInHours, RuntimeType, InputArguments, ResumeOnSameContext, RunAsMe, IsConnected, CalendarName, ActivateOnJobComplete, ItemsActivationThreshold, ItemsPerJobActivationTarget, MaxJobsForActivation, StartProcessCron, StartProcessCronDetails, QueueDefinitionName, TimeZoneId, StopProcessDate, ExecutorRobots, and MachineRobots. Requires a filesystem path (not an Orch: drive path).

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

### -Name

Specifies the names of triggers to retrieve. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests trigger names from the target folders.

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

Returns ProcessSchedule objects with properties including Name, ReleaseName, Enabled, StartProcessCron, RuntimeType, and other trigger configuration properties. When -ExpandDetails is specified, additional properties are populated from individual trigger detail queries.

## NOTES

Triggers are folder-scoped entities. You must navigate to a folder on the Orch: drive or use -Path to specify target folders.

The -ExportCsv parameter produces a CSV format that is compatible with Update-OrchTrigger for bulk import and modification workflows.

## RELATED LINKS

New-OrchTrigger

Update-OrchTrigger

Remove-OrchTrigger

Enable-OrchTrigger

Disable-OrchTrigger
