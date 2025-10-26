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

Asset links are folder entities that require navigation to the appropriate folder context first. Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters.

This cmdlet operates as a folder entity operation, requiring navigation to the appropriate folder context or specification of target folders using the -Path parameter. Use the -Recurse parameter to include subfolders in the search, and -Depth to control recursion levels.

The cmdlet helps administrators understand asset distribution across folders, troubleshoot access issues, and manage asset organization within complex folder hierarchies.

Primary Endpoint: GET /odata/Assets/UiPath.Server.Configuration.OData.GetFoldersForAsset(id={assetId})

OAuth required scopes: OR.Assets or OR.Assets.Read

Required permissions: Assets.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Get-OrchAssetLink DatabaseConfig
```

Retrieves folder links for the DatabaseConfig asset in the current folder.

### Example 2
```powershell
PS Orch1:\> Get-OrchAssetLink -Recurse *API*
```

Gets folder links for all assets with names containing "API" from all folders recursively.

### Example 3
```powershell
PS C:\> Get-OrchAssetLink -Path Orch1:\Production,Orch1:\Development -Name ConnectionString
```

Retrieves folder links for the ConnectionString asset from Production and Development folders.

### Example 4
```powershell
PS Orch1:\> Get-OrchAssetLink -Recurse | Select-Object Path, DisplayName, FullyQualifiedName
```

Gets all asset links recursively and displays key properties with Path shown first.

### Example 5
```powershell
PS Orch1:\> Get-OrchAssetLink -Recurse -Depth 2
```

Gets asset links in current folder and up to 2 levels of subfolders.

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

### -ProgressAction
Controls how progress information is displayed during command execution. Use 'SilentlyContinue' to suppress progress display.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.SimpleFolder
## NOTES
This cmdlet is a folder entity operation that requires appropriate folder context or path specification. Asset links define folder-level access to assets within the Orchestrator hierarchy. Use -Recurse and -Depth parameters to control the scope of folder searching. Understanding asset-folder relationships is essential for proper access control and asset management. This operation requires Assets.View permissions in the target folders.



Primary Endpoint: GET /odata/Assets
OAuth required scopes: OR.Assets or OR.Assets.Read
Required permissions: Assets.View

## RELATED LINKS

[Get-OrchAsset](Get-OrchAsset.md)

[Get-OrchFolder](Get-OrchFolder.md)

[Set-OrchAsset](Set-OrchAsset.md)

[Add-OrchAsset](Add-OrchAsset.md)
