---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchAsset.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Remove-OrchAsset
---

# Remove-OrchAsset

## SYNOPSIS

Removes assets from UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
Remove-OrchAsset [-Path <string[]>] [-Recurse] [-Depth <uint>] [-Name] <string[]>
 [[-ValueType] <string[]>] [-Confirm] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Removes assets from UiPath Orchestrator folders. This cmdlet removes entire assets including all their per-robot values. It handles all asset types (Text, Bool, Integer, Credential, and Secret).

The -Name parameter supports wildcards to remove multiple assets at once. The -ValueType parameter can filter which asset types to remove, allowing targeted removal such as removing only credential assets.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

The -Name, -ValueType, and -Path parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values. The -Name completion dynamically lists assets from the target folders. The -ValueType completion lists value types of assets matching the -Name filter.

To remove only per-robot values of an asset while keeping the asset itself, use Set-OrchAsset with `-Value ''` and -UserName instead.

Primary Endpoint: DELETE /odata/Assets({assetId})

OAuth required scopes: OR.Assets

Required permissions: Assets.Delete

## EXAMPLES

### Example 1: Remove a specific asset

```powershell
PS Orch1:\Shared> Remove-OrchAsset ObsoleteAsset
```

Removes the asset named "ObsoleteAsset" from the current folder.

### Example 2: Preview removal with -WhatIf

```powershell
PS Orch1:\Shared> Remove-OrchAsset Test* -WhatIf
```

```output
What if: Performing the operation "Remove Asset" on target "Orch1:\Shared\TestAsset1".
What if: Performing the operation "Remove Asset" on target "Orch1:\Shared\TestAsset2024".
```

Shows which assets would be removed without executing the command. Wildcard patterns match multiple assets. Once you have confirmed the listed targets, re-run the same command without -WhatIf to actually delete them.

### Example 3: Remove all credential assets

```powershell
PS Orch1:\Shared> Remove-OrchAsset * -ValueType Credential -WhatIf
```

Previews removal of all credential-type assets in the current folder while leaving Text, Bool, Integer, and Secret assets intact; the -ValueType parameter filters by asset type. Because this would delete every credential asset at once, confirm the listed targets first, then re-run the same command without -WhatIf to delete them.

### Example 4: Remove assets from a specific folder

```powershell
PS C:\> Remove-OrchAsset -Path Orch1:\Shared TempAsset
```

Removes the asset from the Shared folder. When -Path uses an absolute path (Orch1:\...), the command can be run from any location.

### Example 5: Remove assets recursively from all folders

```powershell
PS Orch1:\> Remove-OrchAsset -Recurse Legacy*
```

Removes all assets matching "Legacy*" from the root folder and all subfolders recursively.

### Example 6: Remove assets from immediate subfolders only

```powershell
PS Orch1:\> Remove-OrchAsset -Depth 1 Temp*
```

Removes assets matching "Temp*" from the root folder and its immediate subfolders (depth 1). When -Depth is specified, -Recurse is implied.

### Example 7: Bulk removal of a hand-picked set via CSV

```powershell
PS Orch1:\> Get-OrchAsset -Recurse * | Select-Object Path,Name | Export-Csv c:RemoveAssets.csv
```

Enumerate the candidate assets and export them to a CSV. Because the current location is the Orch1: drive, qualify the file with a filesystem drive — for example `c:RemoveAssets.csv`, which writes to the current directory of the C: drive. A bare path such as `RemoveAssets.csv` would resolve against the Orchestrator drive, which cannot store files.

Open the file in its associated editor by typing its path and pressing Enter, then delete the rows you want to keep, leaving only the rows you want to delete:

```powershell
PS Orch1:\> c:RemoveAssets.csv   # Press Tab to expand to the absolute path
```

Pipe the curated file back into the cmdlet:

```powershell
PS Orch1:\> Import-Csv c:RemoveAssets.csv | Remove-OrchAsset -WhatIf
```

Each CSV column binds to the parameter of the same name (Path and Name), so exactly the rows you left are removed. Use this when the set of assets to delete is arbitrary and cannot be expressed by a single wildcard pattern. For a simple "delete everything matching a pattern" case, no pipeline is needed — `Remove-OrchAsset -Recurse * -WhatIf` does it directly.

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

### -Name

Specifies the names of assets to remove. This is a mandatory parameter. Supports wildcards and multiple comma-separated values. Tab completion dynamically lists assets from the target folders.

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

### -ValueType

Filters assets to remove by value type. When specified, only assets matching the specified value types are removed. Supports wildcards. Tab completion dynamically lists value types of assets matching the -Name filter in the target folders.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: (All)
  Position: 1
  IsRequired: false
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

You can pipe objects to this cmdlet by property name: the `Name` property binds to -Name (asset names) and the `Path` property binds to -Path (target folders). This lets you pipe `Import-Csv` output — or `Get-OrchAsset | Select-Object Path,Name` — directly into the cmdlet.

## OUTPUTS

### None

This cmdlet does not produce any output.

## NOTES

This cmdlet removes entire assets. To remove only per-robot values while keeping the asset, use Set-OrchAsset with `-Value ''` and -UserName.

Unlike Set-OrchAsset, this cmdlet removes assets of all value types including Credential and Secret. Use -ValueType to restrict which types are removed.

By default, this cmdlet does not prompt for confirmation; deletion proceeds immediately. Always preview the targets with -WhatIf first, or use -Confirm to be prompted before each asset is removed.

The cmdlet processes each matching asset individually, so if one removal fails (e.g., due to permissions), remaining assets continue to be processed.

## RELATED LINKS

[Get-OrchAsset](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchAsset.md)

[Set-OrchAsset](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Set-OrchAsset.md)

[Set-OrchCredentialAsset](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Set-OrchCredentialAsset.md)

[Copy-OrchAsset](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchAsset.md)
