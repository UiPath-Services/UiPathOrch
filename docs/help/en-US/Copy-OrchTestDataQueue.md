---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchTestDataQueue.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Copy-OrchTestDataQueue
---

# Copy-OrchTestDataQueue

## SYNOPSIS

Copies test data queues to a destination folder.

## SYNTAX

### __AllParameterSets

```
Copy-OrchTestDataQueue [-Path <string>] [-Recurse] [-Depth <uint>] [-Name] <string[]>
 [-Destination] <string> [-Confirm] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Copies test data queues from a source folder to a destination folder in UiPath Orchestrator. Each queue's items are copied along with its definition. When used with -Recurse, the folder hierarchy is preserved at the destination.

The -Name parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available test data queue names. Multiple values can be specified using comma-separated text that includes wildcards.

If the source and destination resolve to the same folder, the operation is skipped.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: POST /odata/TestDataQueues

OAuth required scopes: OR.TestDataQueues or OR.TestDataQueues.Write

Required permissions: TestDataQueues.View (source); TestDataQueues.Create and TestDataQueues.Edit (destination — Edit is used to restore each queue's schema after its items are uploaded, see NOTES)

## EXAMPLES

### Example 1: Copy test data queues to another folder

```powershell
PS Orch1:\Shared> Copy-OrchTestDataQueue * Orch1:\Dept#2
```

Copies all test data queues from the Shared folder to the Dept#2 folder.

### Example 2: Copy test data queues across drives

```powershell
PS C:\> Copy-OrchTestDataQueue -Path Orch1:\Shared * Orch2:\Shared
```

Copies all test data queues from the Shared folder on Orch1 to the Shared folder on Orch2.

### Example 3: Copy test data queues recursively

```powershell
PS Orch1:\> Copy-OrchTestDataQueue -Recurse * Orch2:\
```

Copies all test data queues from all folders on Orch1 to the corresponding folders on Orch2, preserving the folder hierarchy.

### Example 4: Preview copy with WhatIf

```powershell
PS Orch1:\Shared> Copy-OrchTestDataQueue * Orch1:\Dept#2 -WhatIf
```

Shows what test data queues would be copied without actually performing the operation.

## PARAMETERS

### -Path

Specifies the source folder. If not specified, the current folder is used as the source.

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

Includes the source folder and all its subfolders in the copy operation. The folder hierarchy is preserved at the destination.

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

Specifies the depth for recursion into the source folders. A depth of 0 targets only the source folder with no subfolders included. When -Depth is specified, -Recurse is implied.

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

### -Destination

Specifies the destination folder where test data queues will be copied to.

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

Specifies the names of the test data queues to copy. Supports wildcards. Tab completion dynamically suggests test data queue names from the source folders.

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

Shows what would happen if the cmdlet runs.
The cmdlet is not run.

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

You can pipe test data queue names to this cmdlet via the Name property.

### System.String

You can pipe the Destination path to this cmdlet.

## OUTPUTS

### UiPath.PowerShell.Entities.TestDataQueue

Returns TestDataQueue objects representing the copied test data queues.

## NOTES

If the source and destination resolve to the same folder, the operation is silently skipped.

When using -Recurse, the relative folder hierarchy from the source is replicated at the destination; destination subfolders that do not exist are created.

A queue's items often predate a change to its schema, so some may omit a field the schema now marks `required`. To copy them faithfully, the destination queue is first created with the schema's top-level `required` list removed (so every item is accepted), then the original schema is restored once the items are uploaded — the destination ends up identical to the source: `required` is enforced for new items, and the legacy items are intact. Restoring the schema needs `TestDataQueues.Edit` on the destination; if that step fails, a warning is written and the items are still copied (the schema is left relaxed).

Items are uploaded in batches; if a batch is rejected, each item in it is retried one at a time, so a single malformed item is reported on its own (and skipped) instead of failing the whole queue.

## RELATED LINKS

[Get-OrchTestDataQueue](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchTestDataQueue.md)

[Remove-OrchTestDataQueue](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchTestDataQueue.md)
