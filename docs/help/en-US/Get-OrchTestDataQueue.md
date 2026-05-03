---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchTestDataQueue.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchTestDataQueue
---

# Get-OrchTestDataQueue

## SYNOPSIS

Gets test data queues from Orchestrator folders.

## SYNTAX

### __AllParameterSets

```
Get-OrchTestDataQueue [[-Name] <string[]>] [-Path <string[]>] [-Recurse] [-Depth <uint>]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets test data queue information from UiPath Orchestrator. Test data queues store structured test data that can be consumed by test cases during automated test execution.

The -Name parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available test data queue names. Multiple values can be specified using comma-separated text that includes wildcards.

The cmdlet supports multi-threaded folder processing for improved performance when querying across multiple folders. Personal folders are excluded from processing.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: GET /odata/TestDataQueues

OAuth required scopes: OR.TestDataQueues or OR.TestDataQueues.Read

Required permissions: TestDataQueues.View

## EXAMPLES

### Example 1: Get all test data queues in the current folder

```powershell
PS Orch1:\root> Get-OrchTestDataQueue
```

Gets all test data queues from the current folder.

### Example 2: Get test data queues by name

```powershell
PS Orch1:\root> Get-OrchTestDataQueue テスト*
```

Gets test data queues whose names match 'テスト\*' from the current folder.

### Example 3: Get test data queues recursively

```powershell
PS Orch1:\> Get-OrchTestDataQueue -Recurse
```

Gets all test data queues from the current folder and all its subfolders.

### Example 4: Get test data queues from a specific folder

```powershell
PS C:\> Get-OrchTestDataQueue -Path Orch1:\root
```

Gets all test data queues from the root folder on Orch1.

## PARAMETERS

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

### -Name

Specifies the names of the test data queues to retrieve. Supports wildcards. Tab completion dynamically suggests test data queue names from the target folders.

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

You can pipe test data queue names to this cmdlet via the Name property.

## OUTPUTS

### UiPath.PowerShell.Entities.TestDataQueue

Returns TestDataQueue objects representing test data queues with properties including Name and Id.

## NOTES

The cmdlet uses multi-threaded folder processing for improved performance when querying across multiple folders. Personal folders are excluded from processing.

## RELATED LINKS

Remove-OrchTestDataQueue

Copy-OrchTestDataQueue

Get-OrchTestDataQueueItem
