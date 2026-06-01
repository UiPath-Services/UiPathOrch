---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Move-OrchBucket.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 06/01/2026
PlatyPS schema version: 2024-05-01
title: Move-OrchBucket
---

# Move-OrchBucket

## SYNOPSIS

Moves a storage bucket from one folder to another within the same Orchestrator drive.

## SYNTAX

### __AllParameterSets

```
Move-OrchBucket [-Path <string[]>] [-Recurse] [-Depth <uint>] [-Name] <string[]>
 [-Destination] <string> [-Confirm] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Moves a storage bucket from its source folder to a destination folder within the same Orchestrator drive (tenant). A bucket is a single tenant-level entity surfaced into one or more folders; Move-OrchBucket relocates it so it leaves the source folder and becomes a first-class bucket in the destination folder, keeping the same Id and the same Identifier (the GUID that ties the bucket definition to its underlying storage). The bucket's file contents are unaffected — the move changes which folder owns the bucket definition, not the storage account behind it.

The move is a single atomic operation against the share endpoint (the destination link is added and the source link is removed in one request), so there is no intermediate state where the bucket is in both folders or neither.

This is a same-drive operation. The destination must be on the same Orch: drive as the source; a destination on another drive is reported as an error. To copy a bucket definition across drives, use Copy-OrchBucket instead.

The -Name parameter selects the buckets to move (wildcards supported) and the -Destination parameter is the single target folder. The -Name, -Path, and -Destination parameters support tab completion.

Primary Endpoint: POST /odata/Buckets/UiPath.Server.Configuration.OData.ShareToFolders

OAuth required scopes: OR.Administration or OR.Administration.Write

Required permissions: Storage Buckets.Edit (source and destination)

## EXAMPLES

### Example 1: Move a bucket to another folder

```powershell
PS Orch1:\Shared> Move-OrchBucket -Name TestBucket -Destination Orch1:\Dept#2
```

Moves the "TestBucket" bucket from the Shared folder to the Dept#2 folder. After the move it is no longer in Shared, and its files remain accessible under Dept#2.

### Example 2: Preview the move with -WhatIf

```powershell
PS Orch1:\Shared> Move-OrchBucket -Name TestBucket -Destination Orch1:\Dept#2 -WhatIf
```

```output
What if: Performing the operation "Move Bucket to Orch1:\Dept#2" on target "Orch1:\Shared\TestBucket".
```

Shows what would happen without executing the command.

### Example 3: Move all matching buckets from a specific source folder

```powershell
PS C:\> Move-OrchBucket -Path Orch1:\Shared -Name Test* -Destination Orch1:\Dept#2
```

Moves every bucket whose name starts with "Test" from the Shared folder to Dept#2. When -Path uses an absolute path, the command can be run from any location.

### Example 4: Move buckets selected from the pipeline

```powershell
PS C:\> Get-OrchBucket -Path Orch1:\Shared -Name Logs* | Move-OrchBucket -Destination Orch1:\Dept#2
```

Moves the buckets emitted by Get-OrchBucket to Dept#2. Name and Path bind from each piped bucket; Destination is supplied on the command line.

## PARAMETERS

### -Path

Specifies the source folder containing the buckets to move. If not specified, the current folder is used. Supports wildcards and comma-separated values.

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

Includes the source folder and all its subfolders when selecting buckets to move. The source tree is mirrored under -Destination: a bucket in a source subfolder lands in the matching subfolder under the destination (created if it doesn't exist), not flattened into the destination root.

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

Specifies the subfolder depth when selecting source buckets. A depth of 0 targets only the current folder. When -Depth is specified, -Recurse is implied.

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

Specifies the destination folder to move the buckets into. This is a mandatory parameter and a single folder (not a list) on the same Orch: drive as the source — a bucket has one home folder. A wildcard is accepted but must expand to exactly one folder; a pattern that matches zero or more than one folder is an error. Supports tab completion.

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

Specifies the names of buckets to move. This is a mandatory parameter. Supports wildcards and multiple comma-separated values. Tab completion dynamically lists buckets from the source folder.

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

You can pipe bucket names and the source folder path to this cmdlet via the Name and Path properties.

## OUTPUTS

### None

This cmdlet does not produce any output.

## NOTES

The move is same-drive only. The destination must be on the same Orchestrator drive as the source; a cross-drive destination is rejected with an error pointing at Copy-OrchBucket.

A destination equal to the source folder is a no-op. -Destination is a single folder; a comma-separated list is rejected at bind time, and a wildcard that expands to more than one folder is an error (a bucket has a single home folder).

With -Recurse the source tree is mirrored under -Destination (robocopy /MOVE /E semantics): missing destination subfolders are created as plain modern folders with no package feed, and folder creation honors -WhatIf.

Move relocates the one bucket definition, keeping its Id and Identifier; the underlying storage and its files are untouched. It is not a copy — to create a new bucket definition, use Copy-OrchBucket.

## RELATED LINKS

[Get-OrchBucket](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchBucket.md)

[Copy-OrchBucket](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchBucket.md)

[Add-OrchBucketLink](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Add-OrchBucketLink.md)

[Remove-OrchBucketLink](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchBucketLink.md)
