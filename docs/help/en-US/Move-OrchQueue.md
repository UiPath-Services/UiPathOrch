---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Move-OrchQueue.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 06/01/2026
PlatyPS schema version: 2024-05-01
title: Move-OrchQueue
---

# Move-OrchQueue

## SYNOPSIS

Moves a queue from one folder to another within the same Orchestrator drive.

## SYNTAX

### __AllParameterSets

```
Move-OrchQueue [-Path <string[]>] [-Recurse] [-Depth <uint>] [-Name] <string[]>
 [-Destination] <string> [-Confirm] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Moves a queue (queue definition) from its source folder to a destination folder within the same Orchestrator drive (tenant). A queue is a single tenant-level entity surfaced into one or more folders; Move-OrchQueue relocates it so it leaves the source folder and becomes a first-class queue in the destination folder, keeping the same Id. The queue's items move with it — the relocated queue carries its existing queue items, which become visible under the destination folder.

The move is a single atomic operation against the share endpoint (the destination link is added and the source link is removed in one request), so there is no intermediate state where the queue is in both folders or neither.

This is a same-drive operation. The destination must be on the same Orch: drive as the source; a destination on another drive is reported as an error. To copy a queue definition across drives, use Copy-OrchQueue instead.

The -Name parameter selects the queues to move (wildcards supported) and the -Destination parameter is the single target folder. The -Name, -Path, and -Destination parameters support tab completion.

Primary Endpoint: POST /odata/QueueDefinitions/UiPath.Server.Configuration.OData.ShareToFolders

OAuth required scopes: OR.Queues or OR.Queues.Write

Required permissions: Queues.Edit (source and destination)

## EXAMPLES

### Example 1: Move a queue to another folder

```powershell
PS Orch1:\Shared> Move-OrchQueue -Name TestQueue -Destination Orch1:\Dept#2
```

Moves the "TestQueue" queue from the Shared folder to the Dept#2 folder. After the move it is no longer in Shared, and its items are visible under Dept#2.

### Example 2: Preview the move with -WhatIf

```powershell
PS Orch1:\Shared> Move-OrchQueue -Name TestQueue -Destination Orch1:\Dept#2 -WhatIf
```

```output
What if: Performing the operation "Move Queue to Orch1:\Dept#2" on target "Orch1:\Shared\TestQueue".
```

Shows what would happen without executing the command.

### Example 3: Move all matching queues from a specific source folder

```powershell
PS C:\> Move-OrchQueue -Path Orch1:\Shared -Name Test* -Destination Orch1:\Dept#2
```

Moves every queue whose name starts with "Test" from the Shared folder to Dept#2. When -Path uses an absolute path, the command can be run from any location.

### Example 4: Move queues selected from the pipeline

```powershell
PS C:\> Get-OrchQueue -Path Orch1:\Shared -Name Invoices* | Move-OrchQueue -Destination Orch1:\Dept#2
```

Moves the queues emitted by Get-OrchQueue to Dept#2. Name and Path bind from each piped queue; Destination is supplied on the command line.

## PARAMETERS

### -Path

Specifies the source folder containing the queues to move. If not specified, the current folder is used. Supports wildcards and comma-separated values.

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

Includes the source folder and all its subfolders when selecting queues to move. The source tree is mirrored under -Destination: a queue in a source subfolder lands in the matching subfolder under the destination (created if it doesn't exist), not flattened into the destination root.

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

Specifies the subfolder depth when selecting source queues. A depth of 0 targets only the current folder. When -Depth is specified, -Recurse is implied.

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

Specifies the destination folder to move the queues into. This is a mandatory parameter and a single folder (not a list) on the same Orch: drive as the source — a queue has one home folder. A wildcard is accepted but must expand to exactly one folder; a pattern that matches zero or more than one folder is an error. Supports tab completion.

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

Specifies the names of queues to move. This is a mandatory parameter. Supports wildcards and multiple comma-separated values. Tab completion dynamically lists queues from the source folder.

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

You can pipe queue names and the source folder path to this cmdlet via the Name and Path properties.

## OUTPUTS

### None

This cmdlet does not produce any output.

## NOTES

The move is same-drive only. The destination must be on the same Orchestrator drive as the source; a cross-drive destination is rejected with an error pointing at Copy-OrchQueue.

A destination equal to the source folder is a no-op. -Destination is a single folder; a comma-separated list is rejected at bind time, and a wildcard that expands to more than one folder is an error (a queue has a single home folder).

With -Recurse the source tree is mirrored under -Destination (robocopy /MOVE /E semantics): missing destination subfolders are created as plain modern folders with no package feed, and folder creation honors -WhatIf.

Move relocates the one queue together with its items, keeping its Id; it is not a copy. To create a new queue definition, use Copy-OrchQueue.

## RELATED LINKS

[Get-OrchQueue](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchQueue.md)

[Copy-OrchQueue](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchQueue.md)

[Add-OrchQueueLink](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Add-OrchQueueLink.md)

[Remove-OrchQueueLink](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchQueueLink.md)
