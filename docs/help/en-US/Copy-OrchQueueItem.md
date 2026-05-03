---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchQueueItem.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Copy-OrchQueueItem
---

# Copy-OrchQueueItem

## SYNOPSIS

Copies queue items from one queue to another.

## SYNTAX

### __AllParameterSets

```
Copy-OrchQueueItem [-Name] <string[]> [-Destination] <string> [-Path <string>] [-Recurse]
 [-Depth <uint>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Copies queue items with a Status of "New" from one UiPath Orchestrator queue to another. Items are copied in batches of 100 using the BulkAddQueueItem API. Cross-drive copy is supported, allowing items to be copied between different Orchestrator instances (e.g., from Orch1: to Orch2:).

Only items with Status "New" are eligible for copy. Items with any other status (e.g., InProgress, Failed, Successful) are silently skipped.

The cmdlet enforces a rate limit of 601 milliseconds between API calls to avoid overloading the Orchestrator server. If some items fail to copy, the cmdlet outputs warnings for each failed item while continuing to process the remaining items.

This cmdlet supports ShouldProcess. Use -WhatIf to preview which queue items would be copied, or -Confirm to be prompted before each copy operation.

The -Name, -Destination, and -Path parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values. The -Name completion is dynamically populated from actual queue names in the source folders. Multiple values can be specified using comma-separated text that includes wildcards.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: POST /odata/Queues/UiPathODataSvr.BulkAddQueueItems

OAuth required scopes: OR.Queues

Required permissions: Queues.View + Transactions.View (source), Queues.Edit + Transactions.Create (destination)

## EXAMPLES

### Example 1: Copy queue items to another folder

```powershell
PS Orch1:\Shared> Copy-OrchQueueItem TestQueue2 Dept#2
```

Copies all queue items with Status "New" from the TestQueue2 in the current folder (Shared) to the TestQueue2 in the Dept#2 folder on the same Orchestrator instance.

### Example 2: Copy queue items using a wildcard

```powershell
PS Orch1:\Shared> Copy-OrchQueueItem Test* Dept#2
```

Copies all "New" queue items from queues matching "Test*" in the current folder to the matching queues in the Dept#2 folder.

### Example 3: Preview copy with -WhatIf

```powershell
PS Orch1:\Shared> Copy-OrchQueueItem TestQueue2 Dept#2 -WhatIf
```

```output
What if: Performing the operation "Copy QueueItem" on target "Item: 'TestQueue2 [Shared]' Destination: 'Dept#2'".
```

Shows what would happen without actually copying any items.

### Example 4: Copy queue items across Orchestrator instances

```powershell
PS C:\> Copy-OrchQueueItem -Path Orch1:\Shared TestQueue2 Orch2:\Shared
```

Copies all "New" queue items from the TestQueue2 in the Shared folder on Orch1 to the TestQueue2 in the Shared folder on Orch2.

### Example 5: Copy queue items recursively

```powershell
PS Orch1:\> Copy-OrchQueueItem -Recurse Test* Dept#2
```

Copies all "New" queue items from all queues matching "Test*" across all folders on Orch1 to the Dept#2 folder.

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

Specifies the destination folder where queue items will be copied. Supports wildcards. Can reference a folder on a different Orchestrator drive (e.g., Orch2:\Production) for cross-instance copy.

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

Specifies the names of the queues whose items will be copied. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests queue names from the source folders.

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

### UiPath.PowerShell.Entities.QueueItem

Returns the successfully copied QueueItem objects in the destination queue. If some items fail to copy, warnings are emitted for the failed items and only the successfully copied items are returned.

## NOTES

Only queue items with Status "New" are copied. Items with any other status are excluded from the copy operation.

Items are copied in batches of 100 per API call. A rate limit of 601 milliseconds is enforced between API calls to avoid overloading the Orchestrator server.

If some items in a batch fail, the cmdlet continues processing remaining items and outputs a warning for each failure. This is a partial-failure-tolerant operation.

Queue items are folder-scoped entities. You must navigate to a folder on the Orch: drive or use -Path to specify the source folder.

## RELATED LINKS

Get-OrchQueueItem

Copy-OrchQueue
