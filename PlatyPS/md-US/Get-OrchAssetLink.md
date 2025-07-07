---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchAssetLink

## SYNOPSIS
Retrieves folder associations and links for specified assets.

## SYNTAX

```
Get-OrchAssetLink [[-Name] <String[]>] [-Path <String[]>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
The Get-OrchAssetLink cmdlet retrieves folder associations and links for specified assets within UiPath Orchestrator. Assets can be linked to multiple folders within the Orchestrator hierarchy, and this cmdlet provides visibility into those relationships.

Asset links define where assets are accessible within the folder structure, enabling proper access control and organization. Understanding asset-folder relationships is crucial for asset management, security policies, and access control configuration.

This cmdlet operates as a folder entity operation, requiring navigation to the appropriate folder context or specification of target folders using the -Path parameter. Use the -Recurse parameter to include subfolders in the search, and -Depth to control recursion levels.

The cmdlet helps administrators understand asset distribution across folders, troubleshoot access issues, and manage asset organization within complex folder hierarchies.

Primary Endpoint: GET /odata/Assets/UiPath.Server.Configuration.OData.GetFoldersForAsset(id={assetId})

OAuth required scopes: OR.Assets or OR.Assets.Read

Required permissions: Assets.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Production> Get-OrchAssetLink DatabaseConfig
```

Retrieves folder links for the DatabaseConfig asset in the current folder.

### Example 2
```powershell
PS C:\> Get-OrchAssetLink -Path Orch1:\Production -Name *API* -Recurse
```

Gets folder links for all assets with names containing "API" in the Production folder and its subfolders.

### Example 3
```powershell
PS Orch1:\> Get-OrchAssetLink -Recurse | Group-Object AssetName
```

Groups all asset links by asset name across all folders to show asset distribution.

### Example 4
```powershell
PS Orch1:\Development> Get-OrchAssetLink -Name ConnectionString, APIKey
```

Retrieves folder links for specific assets (ConnectionString and APIKey) in the current Development folder.

### Example 5
```powershell
PS C:\> Get-OrchAssetLink -Path Orch1:\Production -Depth 2
```

Gets asset links in the Production folder and up to 2 levels of subfolders.

### Example 6
```powershell
PS Orch1:\> Get-OrchAsset | Get-OrchAssetLink | Select-Object AssetName, FolderPath
```

Gets all assets and their folder associations, displaying asset names and folder paths.

## PARAMETERS

### -Depth
Specifies the depth for recursion into target folders. A depth of 0 indicates the current location only. Higher values include more subfolder levels.

```yaml
Type: UInt32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Name
Specifies the names of assets whose folder links are to be retrieved. Supports wildcard patterns for flexible asset selection.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
Specifies the target folders to search. If not specified, the current folder context will be used. For folder entity operations requiring path specification.

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

### -Recurse
Includes the target folder and all its subfolders in the search operation. Essential for comprehensive asset link discovery.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]
Asset names can be piped to this cmdlet.

### UiPath.PowerShell.Entities.Asset
Asset objects from Get-OrchAsset can be piped to this cmdlet. The Name property will be automatically mapped to the -Name parameter via ByPropertyName binding.

## OUTPUTS

### UiPath.PowerShell.Entities.SimpleFolder
Returns SimpleFolder objects representing the folders where the specified assets are linked or accessible.

## NOTES
This cmdlet is a folder entity operation that requires appropriate folder context or path specification. Asset links define folder-level access to assets within the Orchestrator hierarchy. Use -Recurse and -Depth parameters to control the scope of folder searching. Understanding asset-folder relationships is essential for proper access control and asset management. This operation requires Assets.View permissions in the target folders.

## RELATED LINKS

[Get-OrchAsset](Get-OrchAsset.md)

[Get-OrchFolder](Get-OrchFolder.md)

[Set-OrchAsset](Set-OrchAsset.md)

[Add-OrchAsset](Add-OrchAsset.md)
