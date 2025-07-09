---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Add-OrchAssetLink

## SYNOPSIS
Adds links to the specified assets.

## SYNTAX

```
Add-OrchAssetLink [-Name] <String[]> [-Link] <String[]> [-Path <String[]>] [-ProgressAction <ActionPreference>]
 [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The Add-OrchAssetLink cmdlet creates links for assets, allowing them to be shared across multiple folders within UiPath Orchestrator tenants. This cmdlet enables asset sharing by linking existing assets from one folder to other specified folders.

This is a folder entity cmdlet. If you receive the error "Use Set-Location cmdlet (cd command) to navigate to the target folder first", navigate to the folder containing the source assets or use the -Path parameter to specify the target folder.

Use the -Name parameter to specify which assets to link and the -Link parameter to specify the target folders where the asset links should be created. Asset links provide access to the same asset data across multiple folder contexts without duplicating the asset content.

The cmdlet supports wildcard patterns for both asset names and folder links, enabling bulk operations for multiple assets and folders simultaneously.

Primary Endpoint: POST /odata/Assets/UiPath.Server.Configuration.OData.ShareToFolders

OAuth required scopes: OR.Assets or OR.Assets.Write

Required permissions: Assets.Edit

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Add-OrchAssetLink SampleBooleanAsset "Orch1:\root"
```

Links the SampleBooleanAsset from the current folder (Shared) to the root folder using positional parameters.

### Example 2
```powershell
PS C:\> Add-OrchAssetLink -Path Orch1:\Shared DatabaseConfig Orch1:\Dept#2, Orch1:\root
```

Links the DatabaseConfig asset from Shared folder to both Dept#2 and root folders using explicit path specification.

### Example 3
```powershell
PS Orch1:\Shared> Add-OrchAssetLink *Config* Orch1:\Dept#2
```

Links all assets containing Config in their name from the current folder to Dept#2 folder using wildcards.

### Example 4
```powershell
PS Orch1:\Shared> Add-OrchAssetLink SampleBooleanAsset, APIKey Orch1:\Dept#2 -WhatIf
```

Shows what would happen when linking multiple assets to Dept#2 folder using -WhatIf for safety.

### Example 5
```powershell
PS C:\> Add-OrchAssetLink -Path Orch1:\Shared *Sample* Orch1:\root, Orch1:\Dept#2
```

Links all assets containing "Sample" from Shared folder to multiple target folders simultaneously.

## PARAMETERS

### -Link
Specifies the folders to be added as links.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Name
Specifies the Name of the assets to be updated.


```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
Specifies the target folder. If not specified, the current folder will be targeted.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -ProgressAction
{{ Fill ProgressAction Description }}

```yaml
Type: ActionPreference
Parameter Sets: (All)
Aliases: proga

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Confirm
Prompts you for confirmation before running the cmdlet.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: cf

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -WhatIf
Shows what would happen if the cmdlet runs.
The cmdlet is not run.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: wi

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### System.Object
## NOTES
This is a folder entity cmdlet. You must navigate to a folder using Set-Location or specify the source folder using the -Path parameter.

Asset links allow the same asset to be accessed from multiple folders without duplicating the actual asset content. Use Get-OrchAssetLink to verify successful link creation.

## RELATED LINKS

[Get-OrchAsset](Get-OrchAsset.md)

[Get-OrchAssetLink](Get-OrchAssetLink.md)

[Remove-OrchAssetLink](Remove-OrchAssetLink.md)


