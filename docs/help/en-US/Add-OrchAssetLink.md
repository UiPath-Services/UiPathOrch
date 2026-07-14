---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Add-OrchAssetLink.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Add-OrchAssetLink
---

# Add-OrchAssetLink

## SYNOPSIS

Links assets to additional folders.

## SYNTAX

### __AllParameterSets

```
Add-OrchAssetLink [-Path <string[]>] [-LiteralPath <string[]>] [-Name] <string[]> [-Link] <string[]> [-Confirm]
 [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Links assets to additional folders in UiPath Orchestrator. Asset linking allows a global-scope asset defined in one folder to be shared with other folders without duplicating the asset. Linked folders can read the shared asset value, and changes to the asset are reflected in all linked folders.

The -Name parameter specifies which assets to link, and the -Link parameter specifies the destination folders to share the assets with. Both the source and destination folders must be on the same Orchestrator instance (same drive).

The -Name and -Path parameters support tab completion. The -Name completion dynamically lists assets from the source folder. The -Link parameter accepts folder paths on the same Orch: drive.

Primary Endpoint: POST /odata/Assets/UiPath.Server.Configuration.OData.ShareToFolders

OAuth required scopes: OR.Assets or OR.Assets.Write

Required permissions: Assets.Edit

## EXAMPLES

### Example 1: Link an asset to another folder

```powershell
PS Orch1:\Shared> Add-OrchAssetLink -Name TestAsset1 -Link Orch1:\Dept#2
```

Links the "TestAsset1" asset from the Shared folder to the Dept#2 folder. The Dept#2 folder can now access this asset.

### Example 2: Preview link operation with -WhatIf

```powershell
PS Orch1:\Shared> Add-OrchAssetLink -Name TestAsset1 -Link Orch1:\Dept#2 -WhatIf
```

```output
What if: Performing the operation "Add AssetLink to Orch1:\Dept#2" on target "Orch1:\Shared\TestAsset1".
```

Shows what would happen without executing the command.

### Example 3: Link multiple assets to multiple folders

```powershell
PS Orch1:\Shared> Add-OrchAssetLink -Name Test* -Link Orch1:\Dept#2, Orch1:\Dept#3
```

Links all assets matching "Test*" to both the Dept#2 and Dept#3 folders. Both -Name and -Link accept wildcards and comma-separated values.

### Example 4: Link an asset from a specific source folder

```powershell
PS C:\> Add-OrchAssetLink -Path Orch1:\Shared TestAsset1 -Link Orch1:\Dept#2
```

Links the asset from the Shared folder to the Dept#2 folder. When -Path uses an absolute path, the command can be run from any location.

## PARAMETERS

### -Path

Specifies the source folder containing the assets to link. If not specified, the current folder is used. Supports wildcards and comma-separated values.

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

### -LiteralPath

Specifies the target folder or drive by literal path -- wildcard metacharacters (`[`, `]`, `*`, `?`) are treated as literal characters rather than patterns. Accepts the same drive-qualified paths as -Path. Its `PSPath` alias also binds the path of items piped from Get-ChildItem / Get-Item, so you can pipe folders directly. Use -LiteralPath instead of -Path when a folder name contains a wildcard metacharacter.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: false
Aliases:
- PSPath
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

Recursively searches subfolders of -Path for matching assets. Combine with -Depth to limit how deep the recursion goes.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
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

Limits how many levels of subfolder are searched when -Recurse is specified. 0 means only the immediate -Path folder is searched. Has no effect without -Recurse.

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

### -Link

Specifies the destination folder paths to link the assets to. This is a mandatory parameter. The destination folders must be on the same Orchestrator instance (same drive) as the source. Supports wildcards and multiple comma-separated values.

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

Specifies the names of assets to link. This is a mandatory parameter. Supports wildcards and multiple comma-separated values. Tab completion dynamically lists assets from the source folder.

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

You can pipe asset names and link folder paths to this cmdlet via the Name, Link, and Path properties.

## OUTPUTS

### None

This cmdlet does not produce any output.

## NOTES

Asset linking only works for global-scope assets. Per-robot assets (ValueScope = PerRobot) cannot be linked across folders.

The source and destination must be on the same Orchestrator drive. Cross-instance linking is not supported. To share assets across instances, use Copy-OrchAsset instead.

If the asset is already linked to the specified folder, the operation succeeds without error.

## RELATED LINKS

[Get-OrchAssetLink](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchAssetLink.md)

[Get-OrchAsset](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchAsset.md)

[Copy-OrchAsset](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchAsset.md)
