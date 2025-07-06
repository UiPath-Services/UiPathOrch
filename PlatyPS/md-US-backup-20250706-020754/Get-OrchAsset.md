---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchAsset

## SYNOPSIS
Gets the assets from UiPath Orchestrator.

## SYNTAX

```
Get-OrchAsset [[-Name] <String[]>] [-ValueType <String[]>] [-ExpandUserValues] [-Path <String[]>] [-Recurse]
 [-Depth <UInt32>] [-ExportCsv <String>] [-ExportCredentialCsv <String>] [-CsvEncoding <Encoding>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
The `Get-OrchAsset` cmdlet retrieves asset information from UiPath Orchestrator. Assets are data stores that can be used by robots to store configuration settings, credentials, and other data needed for automation processes.

This cmdlet supports filtering by asset name and value type, expanding user-specific values, and exporting results to CSV files. It can operate on specific folders or recursively across folder hierarchies.

Multiple values for the -Name and -Path parameters can be specified using comma-separated text that includes wildcards. Additionally, you can use autocomplete for these values by pressing [Ctrl+Space] or [Tab].

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: GET /odata/Assets

OAuth required scopes: OR.Assets or OR.Assets.Read

Required permissions: Assets.View

## EXAMPLES

### Example 1
```powershell
PS C:\> Set-Location Orch1:\
PS Orch1:\> Get-OrchAsset -Recurse
```

Gets all assets from all folders in the current Orchestrator instance recursively.

### Example 2
```powershell
PS Orch1:\> Get-OrchAsset -Recurse -ExpandUserValues
```

Gets all assets recursively and expands user-specific values for per-robot assets.

### Example 3
```powershell
PS Orch1:\> Get-OrchAsset my*asset
```

Gets assets whose names start with "my" and end with "asset" using wildcard pattern matching. To display an asset with an asterisk in its name literally, escape it with a backtick: `Get-OrchAsset "my`*asset"`.

### Example 4
```powershell
PS Orch1:\> Get-OrchAsset -Recurse a*,y*
```

Gets assets whose names start with "a" or "y" from all folders recursively.

### Example 5
```powershell
PS Orch1:\> Get-OrchAsset -Recurse | Format-List
```

Gets all assets recursively and displays them in a detailed list format.

### Example 6
```powershell
PS Orch1:\> Get-OrchAsset -Recurse | Select-Object Name, Value
```

Gets all assets recursively and displays only the Name and Value properties.

### Example 7
```powershell
PS Orch1:\> Get-OrchAsset -Recurse | ConvertTo-Json
```

Gets all assets recursively and converts the output to JSON format.

### Example 8
```powershell
PS Orch1:\> Get-OrchAsset -Recurse | Where-Object { $_.Value -eq $false }
```

Filters to show only assets that have a boolean value of False.

### Example 9
```powershell
PS Orch1:\> Get-OrchAsset -Recurse > C:\output.txt
```

Gets all assets recursively and redirects the output to a text file.

### Example 10
```powershell
PS Orch1:\> Get-OrchAsset -Recurse -ExportCsv "C:\assets.csv"
```

Gets all assets recursively and exports them to a CSV file.

### Example 11
```powershell
PS Orch1:\> Get-OrchAsset -Recurse -ExportCredentialCsv "C:\credentials.csv"
```

Gets all assets recursively and exports credential assets to a separate CSV file.

### Example 12
```powershell
PS Orch1:\> Get-OrchAsset -ValueType "Text","Bool" -Recurse
```

Gets only Text and Boolean type assets from all folders recursively.

## PARAMETERS

### -CsvEncoding
Specifies the encoding for CSV export operations. Common values include UTF8, UTF8BOM, ASCII.

```yaml
Type: Encoding
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: UTF8
Accept pipeline input: False
Accept wildcard characters: False
```

### -Depth
Specifies the depth for recursion into the target folders. A depth of 0 indicates the current location only, with no subfolders included. For example, 1 covers the current folder and immediate subfolders.

```yaml
Type: UInt32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: Unlimited depth
Accept pipeline input: False
Accept wildcard characters: False
```

### -ExportCredentialCsv
Exports credential asset data to the specified CSV file path. The exported CSV can be imported later using Set-OrchCredentialAsset.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ExportCsv
Exports the asset data to the specified CSV file path. The exported CSV can be imported later using Set-OrchAsset.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Name
Specifies the name(s) of the assets to retrieve. Supports wildcards and multiple values. You can use autocomplete by pressing [Ctrl+Space] or [Tab].

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: All assets
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
Specifies the folder path(s) to search for assets. Supports wildcards and multiple values. If not specified, the current location is used.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: Current location
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -ProgressAction
Specifies how PowerShell responds to progress updates generated by a script, cmdlet, or provider, such as the progress bars generated by the Write-Progress cmdlet. Valid values are: SilentlyContinue, Stop, Continue, Inquire, Ignore, Suspend.

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
Includes assets from subfolders when retrieving assets. This is a folder entity operation parameter.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -ValueType
Specifies the value type(s) of assets to retrieve. Valid values include: Text, Bool, Integer, Credential.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: All types
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -ExpandUserValues
Expands and displays user-specific values for per-robot assets. This shows the actual values assigned to each user/machine combination for assets with PerRobot scope.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.Asset
## NOTES
- Assets can have different value types: Text, Bool, Integer, and Credential
- The ValueScope property indicates whether the asset is Global or PerRobot
- PerRobot assets can have different values for each user/machine combination
- Use -ExpandUserValues to see the actual values for PerRobot assets
- Some asset fields are nested and may require expansion to see complete information
- The -ExportCsv and -ExportCredentialCsv parameters create files that can be used with Set-OrchAsset and Set-OrchCredentialAsset for bulk operations

## RELATED LINKS

[Set-OrchAsset](Set-OrchAsset.md)
[Add-OrchAssetLink](Add-OrchAssetLink.md)
[Remove-OrchAsset](Remove-OrchAsset.md)
[Set-OrchCredentialAsset](Set-OrchCredentialAsset.md)
[about_UiPathOrch](about_UiPathOrch.md)




