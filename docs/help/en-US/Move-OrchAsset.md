---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Move-OrchAsset.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 06/01/2026
PlatyPS schema version: 2024-05-01
title: Move-OrchAsset
---

# Move-OrchAsset

## SYNOPSIS

Moves an asset from one folder to another within the same Orchestrator drive.

## SYNTAX

### __AllParameterSets

```
Move-OrchAsset [-Path <string[]>] [-Recurse] [-Depth <uint>] [-Name] <string[]>
 [-Destination] <string[]> [-Confirm] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Moves an asset from its source folder to a destination folder within the same Orchestrator drive (tenant). An asset is a single tenant-level entity that is surfaced into one or more folders; Move-OrchAsset relocates it so it leaves the source folder and becomes a first-class asset in the destination folder, keeping the same Id and its value. No copy is made — the one asset is moved.

The move is a single atomic operation against the share endpoint (the destination link is added and the source link is removed in one request), so there is no intermediate state where the asset is in both folders or neither.

This is a same-drive operation. The destination must be on the same Orch: drive as the source; a destination on another drive is reported as an error. To copy an asset across drives, use Copy-OrchAsset instead.

The -Name parameter selects the assets to move (wildcards supported) and the -Destination parameter is the single target folder. The -Name, -Path, and -Destination parameters support tab completion.

Primary Endpoint: POST /odata/Assets/UiPath.Server.Configuration.OData.ShareToFolders

OAuth required scopes: OR.Assets or OR.Assets.Write

Required permissions: Assets.Edit (source and destination)

## EXAMPLES

### Example 1: Move an asset to another folder

```powershell
PS Orch1:\Shared> Move-OrchAsset -Name TestAsset1 -Destination Orch1:\Dept#2
```

Moves the "TestAsset1" asset from the Shared folder to the Dept#2 folder. After the move it is no longer in Shared.

### Example 2: Preview the move with -WhatIf

```powershell
PS Orch1:\Shared> Move-OrchAsset -Name TestAsset1 -Destination Orch1:\Dept#2 -WhatIf
```

```output
What if: Performing the operation "Move Asset → Orch1:\Dept#2" on target "Orch1:\Shared\TestAsset1".
```

Shows what would happen without executing the command.

### Example 3: Move all matching assets from a specific source folder

```powershell
PS C:\> Move-OrchAsset -Path Orch1:\Shared -Name Test* -Destination Orch1:\Dept#2
```

Moves every asset whose name starts with "Test" from the Shared folder to Dept#2. When -Path uses an absolute path, the command can be run from any location.

### Example 4: Move assets selected from the pipeline

```powershell
PS C:\> Get-OrchAsset -Path Orch1:\Shared -Name Db* | Move-OrchAsset -Destination Orch1:\Dept#2
```

Moves the assets emitted by Get-OrchAsset to Dept#2. Name and Path bind from each piped asset; Destination is supplied on the command line.

## PARAMETERS

### -Path

Specifies the source folder containing the assets to move. If not specified, the current folder is used. Supports wildcards and comma-separated values.

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

Includes the source folder and all its subfolders when selecting assets to move.

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

Specifies the subfolder depth when selecting source assets. A depth of 0 targets only the current folder. When -Depth is specified, -Recurse is implied.

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

Specifies the destination folder to move the assets into. This is a mandatory parameter and must resolve to a single folder on the same Orch: drive as the source. Supports wildcards and tab completion; a pattern that matches more than one folder is reported as ambiguous.

```yaml
Type: System.String[]
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

Specifies the names of assets to move. This is a mandatory parameter. Supports wildcards and multiple comma-separated values. Tab completion dynamically lists assets from the source folder.

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

You can pipe asset names and the source folder path to this cmdlet via the Name and Path properties.

## OUTPUTS

### None

This cmdlet does not produce any output.

## NOTES

The move is same-drive only. The destination must be on the same Orchestrator drive as the source; a cross-drive destination is rejected with an error pointing at Copy-OrchAsset.

A destination equal to the source folder is a no-op. A -Destination pattern that resolves to more than one folder is reported as ambiguous (an asset has a single home folder).

Move relocates the one shared asset, keeping its Id and value; it is not a copy. To duplicate an asset (create a new one), use Copy-OrchAsset.

## RELATED LINKS

[Get-OrchAsset](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchAsset.md)

[Copy-OrchAsset](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchAsset.md)

[Add-OrchAssetLink](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Add-OrchAssetLink.md)

[Remove-OrchAssetLink](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchAssetLink.md)
