---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchQueue.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Remove-OrchQueue
---

# Remove-OrchQueue

## SYNOPSIS

Removes queue definitions from UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
Remove-OrchQueue [-Name] <string[]> [-Path <string[]>] [-Recurse] [-Depth <uint>] [-WhatIf]
 [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Removes queue definitions from UiPath Orchestrator folders. This cmdlet deletes the queue definition itself, not the queue items within it. Errors are handled per-queue, so if one queue fails to be removed the cmdlet continues processing the remaining queues.

The -Name parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available queue names dynamically populated from the target folders. Multiple values can be specified using comma-separated text that includes wildcards.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: DELETE /odata/QueueDefinitions({queueId})

OAuth required scopes: OR.Queues

Required permissions: Queues.Delete

## EXAMPLES

### Example 1: Remove a specific queue

```powershell
PS Orch1:\Shared> Remove-OrchQueue TestQueue2
```

Removes the queue definition named "TestQueue2" from the current folder. You will be prompted for confirmation before the queue is deleted.

### Example 2: Preview removal with WhatIf using wildcards

```powershell
PS Orch1:\Shared> Remove-OrchQueue Test* -WhatIf
```

Displays what would happen if all queues matching "Test*" were removed, without actually deleting them. Useful for verifying wildcard matches before execution.

### Example 3: Remove queues recursively

```powershell
PS Orch1:\> Remove-OrchQueue TestQueue2 -Recurse
```

Removes the queue named "TestQueue2" from the root folder and all subfolders recursively.

### Example 4: Remove a queue from a specific folder

```powershell
PS C:\> Remove-OrchQueue -Path Orch1:\Shared TestQueue2
```

Removes the queue definition named "TestQueue2" from the Shared folder. When -Path uses an absolute path (Orch1:\...), the command can be run from any location.

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

### -Name

Specifies the names of the queues to remove. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests queue names from the target folders. This parameter is mandatory and positional (position 0).

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

### -WhatIf

Shows what would happen if the cmdlet runs. The cmdlet is not run.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: False
SupportsWildcards: false
Aliases:
- wi
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

### -Confirm

Prompts you for confirmation before running the cmdlet.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: False
SupportsWildcards: false
Aliases:
- cf
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

You can pipe queue names to this cmdlet via the Name property.

## OUTPUTS

### None

This cmdlet does not produce output. Queue definitions are removed from the Orchestrator server.

## NOTES

Queues are folder-scoped entities. You must navigate to a folder on the Orch: drive or use -Path to specify target folders.

This cmdlet removes the queue definition, not the queue items. Errors during removal are handled per-queue, allowing the cmdlet to continue processing remaining queues even if one fails.

## RELATED LINKS

Get-OrchQueue

New-OrchQueue

Copy-OrchQueue
