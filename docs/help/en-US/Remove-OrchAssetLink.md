---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchAssetLink.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 05/21/2026
PlatyPS schema version: 2024-05-01
title: Remove-OrchAssetLink
---

# Remove-OrchAssetLink

## SYNOPSIS

Removes folder links from assets.

## SYNTAX

### __AllParameterSets

```
Remove-OrchAssetLink [-Path <string[]>] [-Recurse] [-Depth <uint>] [-Name] <string[]>
 [-Link] <string[]> [-Confirm] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Removes folder links from assets in UiPath Orchestrator. The opposite of Add-OrchAssetLink: detaches the specified destination folders from the asset so they can no longer access it. The owning folder always retains access; this cmdlet only removes shared-access entries.

The -Name parameter specifies which assets to unlink, and the -Link parameter specifies the folders to remove from each asset's link list. Both the source and destination folders must be on the same Orchestrator instance (same drive).

The -Name, -Path, and -Link parameters support tab completion. The -Name completion dynamically lists assets from the source folder(s). The -Link completion lists the folders currently linked to each asset.

Use -Recurse (optionally with -Depth) to walk subfolders of -Path looking for matching assets — useful when removing a link across many folders that share the same asset name.

Primary Endpoint: POST /odata/Assets/UiPath.Server.Configuration.OData.ShareToFolders (same endpoint as Add-OrchAssetLink; the two are distinguished by the request body)

OAuth required scopes: OR.Assets or OR.Assets.Write

Required permissions: Assets.Edit

## EXAMPLES

### Example 1: Remove an asset link

```powershell
PS Orch1:\Shared> Remove-OrchAssetLink -Name TestAsset1 -Link Orch1:\Dept#2
```

Removes the Dept#2 folder from "TestAsset1"'s link list. Dept#2 no longer has access to the asset; Shared (the owning folder) is unaffected.

### Example 2: Preview removal with -WhatIf

```powershell
PS Orch1:\Shared> Remove-OrchAssetLink -Name TestAsset1 -Link Orch1:\Dept#2 -WhatIf
```

```output
What if: Performing the operation "Remove AssetLink 'Orch1:\Dept#2'" on target "Orch1:\Shared\TestAsset1".
```

Shows what would happen without executing.

### Example 3: Remove multiple links across multiple assets

```powershell
PS Orch1:\Shared> Remove-OrchAssetLink -Name Test* -Link Orch1:\Dept#2, Orch1:\Dept#3
```

Removes both the Dept#2 and Dept#3 folder links from every asset matching "Test*".

### Example 4: Recursively unlink across subfolders

```powershell
PS C:\> Remove-OrchAssetLink -Path Orch1:\Depts\* -Recurse -Depth 2 -Name SharedAsset -Link Orch1:\Common
```

Walks the subfolders of Orch1:\Depts up to 2 levels deep, finds assets named "SharedAsset", and removes the Common folder from each one's link list.

## PARAMETERS

### -Name

Specifies the names of assets to unlink. This is a mandatory parameter. Supports wildcards and multiple comma-separated values. Tab completion dynamically lists assets from the source folder(s).

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

### -Link

Specifies the destination folder paths to remove from the assets' link lists. This is a mandatory parameter. Supports wildcards and multiple comma-separated values. Tab completion lists the folders currently linked to each target asset.

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

### -Path

Specifies the source folder(s) containing the assets. If not specified, the current folder is used. Supports wildcards and comma-separated values.

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

Recursively searches subfolders of -Path for matching assets. Combine with -Depth to limit how deep the recursion goes.

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

Limits how many levels of subfolder are searched when -Recurse is specified. 0 means only the immediate -Path folder is searched. Has no effect without -Recurse.

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

You can pipe asset names and link folder paths to this cmdlet via the Name, Link, and Path properties.

## OUTPUTS

### None

This cmdlet does not produce any output.

## NOTES

The owning folder of an asset always retains access; this cmdlet only removes shared-access entries from linked folders. To delete an asset entirely use Remove-OrchAsset.

If a folder in -Link is not currently linked to the asset, the operation succeeds without error for that target.

## RELATED LINKS

[Add-OrchAssetLink](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Add-OrchAssetLink.md)

[Get-OrchAssetLink](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchAssetLink.md)

[Remove-OrchAsset](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchAsset.md)

[Remove-OrchBucketLink](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchBucketLink.md)

[Remove-OrchQueueLink](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchQueueLink.md)
