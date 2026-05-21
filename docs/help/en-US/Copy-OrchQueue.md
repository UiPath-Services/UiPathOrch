---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchQueue.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Copy-OrchQueue
---

# Copy-OrchQueue

## SYNOPSIS

Copies queue definitions to another folder.

## SYNTAX

### __AllParameterSets

```
Copy-OrchQueue [-Path <string>] [-Recurse] [-Depth <uint>] [-Name] <string[]>
 [-Destination] <string> [-Confirm] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Copies queue definitions from one UiPath Orchestrator folder to another. The cmdlet creates a new queue in the destination folder with the same configuration as the source queue. Cross-drive copy is supported, allowing queues to be copied between different Orchestrator instances (e.g., from Orch1: to Orch2:).

If the source and destination folders are the same, the queue is silently skipped.

When copying recursively with -Recurse, the cmdlet uses relative destination folder mapping to maintain the folder structure. A progress reporter tracks multi-queue operations.

This cmdlet supports ShouldProcess. Use -WhatIf to preview which queues would be copied, or -Confirm to be prompted before each copy operation.

The -Name, -Destination, and -Path parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values. The -Name completion is dynamically populated from actual queue names in the source folders. Multiple values can be specified using comma-separated text that includes wildcards.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: POST /odata/QueueDefinitions

OAuth required scopes: OR.Queues

Required permissions: Queues.View (source), Queues.Create (destination)

## EXAMPLES

### Example 1: Copy a queue to another folder

```powershell
PS Orch1:\Shared> Copy-OrchQueue TestQueue2 Dept#2
```

Copies the queue named "TestQueue2" from the current folder (Shared) to the Dept#2 folder on the same Orchestrator instance.

### Example 2: Copy queues using a wildcard

```powershell
PS Orch1:\Shared> Copy-OrchQueue Test* Dept#2
```

Copies all queues matching "Test*" from the current folder to the Dept#2 folder.

### Example 3: Copy a queue across Orchestrator instances

```powershell
PS C:\> Copy-OrchQueue -Path Orch1:\Shared TestQueue2 Orch2:\Shared
```

Copies the queue named "TestQueue2" from the Shared folder on Orch1 to the Shared folder on Orch2. The queue definition is recreated on the destination Orchestrator with the same configuration.

### Example 4: Copy all queues recursively

```powershell
PS Orch1:\> Copy-OrchQueue -Recurse * Orch2:\Shared
```

Copies all queues from all folders on Orch1 to the Shared folder on Orch2. The -Recurse parameter includes all subfolders in the operation.

### Example 5: Preview copy with -WhatIf

```powershell
PS Orch1:\Shared> Copy-OrchQueue TestQueue2 Dept#2 -WhatIf
```

```output
What if: Performing the operation "Copy Queue" on target "Item: 'TestQueue2 [Shared]' Destination: 'Dept#2'".
```

Shows what would happen without actually copying. Use this to verify which queues would be affected before performing the operation.

## PARAMETERS

### -Path

Specifies the source folder. If not specified, the current folder is used as the source. Supports wildcards.

```yaml
Type: System.String
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

Includes the source folder and all its subfolders in the operation.

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

Specifies the depth for recursion into the source folders. A depth of 0 targets only the current folder with no subfolders included. When -Depth is specified, -Recurse is implied.

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

### -Destination

Specifies the destination folder where queues will be copied. Supports wildcards. Can reference a folder on a different Orchestrator drive (e.g., Orch2:\Production) for cross-instance copy.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: (All)
  Position: 1
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Name

Specifies the names of the queues to copy. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests queue names from the source folders.

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe queue names to this cmdlet via the Name property.

### System.String

You can pipe a destination folder path to this cmdlet via the Destination property.

## OUTPUTS

### UiPath.PowerShell.Entities.QueueDefinition

Returns the newly created QueueDefinition object in the destination folder. When the source and destination are the same folder, the queue is silently skipped and no output is returned.

## NOTES

Queues are folder-scoped entities. You must navigate to a folder on the Orch: drive or use -Path to specify the source folder.

When performing cross-drive copy (e.g., Orch1: to Orch2:), the cmdlet recreates the queue definition on the destination Orchestrator. Queue items (transactions) are not copied; only the queue definition and its configuration are transferred.

If a queue with the same name already exists in the destination folder, an error is returned for that queue.

## RELATED LINKS

[Get-OrchQueue](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchQueue.md)

[Copy-OrchQueueItem](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchQueueItem.md)
