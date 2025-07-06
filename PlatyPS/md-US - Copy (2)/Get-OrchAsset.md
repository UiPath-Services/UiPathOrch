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
Gets asset information from UiPath Orchestrator folders. Assets are data stores used by robots to store configuration settings, credentials, and other automation data.

This cmdlet supports filtering by asset name and value type, expanding user-specific values for per-robot assets, and exporting results to CSV files. It can operate on specific folders or recursively across folder hierarchies.

Multiple values for the -Name and -Path parameters can be specified using comma-separated text that includes wildcards. Additionally, you can use autocomplete for these values by pressing [Ctrl+Space] or [Tab].

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: GET /odata/Assets

OAuth required scopes: OR.Assets or OR.Assets.Read

Required permissions: Assets.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Get-OrchAsset
```

Gets assets from the current folder.

### Example 2
```powershell
PS Orch1:\> Get-OrchAsset -Recurse
```

Gets assets from all folders recursively.

### Example 3
```powershell
PS Orch1:\> Get-OrchAsset -Recurse *Config*
```

Gets assets containing "Config" in their name from all folders.

### Example 4
```powershell
PS Orch1:\> Get-OrchAsset -ValueType Credential -Recurse
```

Gets credential assets from all folders.

### Example 5
```powershell
PS Orch1:\> Get-OrchAsset -ExpandUserValues -Recurse
```

Gets assets with expanded user-specific values for per-robot assets.

### Example 6
```powershell
PS Orch1:\> Get-OrchAsset -Recurse | Where-Object {$_.ValueScope -eq "PerRobot"}
```

Gets per-robot scoped assets from all folders.

### Example 7
```powershell
PS Orch1:\> Get-OrchAsset -Recurse -ExportCsv C:\Reports\Assets.csv
```

Exports all assets to CSV with UTF-8 BOM encoding.

### Example 8
```powershell
PS Orch1:\> Get-OrchAsset -Recurse -ExportCredentialCsv C:\Reports\Credentials.csv
```

Exports credential assets to separate CSV file.

## PARAMETERS

### -CsvEncoding
Specifies the encoding for CSV export. Default is UTF-8 with BOM for Excel compatibility.

```yaml
Type: Encoding
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: UTF8 with BOM
Accept pipeline input: False
Accept wildcard characters: False
```

### -Depth
Specifies the depth of folder recursion. A depth of 0 targets only the current folder.

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

### -ExportCredentialCsv
Exports credential assets to CSV file with UTF-8 BOM encoding. Can be used with Set-OrchCredentialAsset for import.

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
Exports results to CSV file with UTF-8 BOM encoding. Automatically converts internal IDs to human-readable names. Can be used with Set-OrchAsset for import.

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
Specifies the names of assets to retrieve. Supports wildcards and multiple values.

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
Specifies target folders. Use comma-separated values for multiple folders. Supports wildcards. If not specified, targets the current folder.

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
Controls how progress information is displayed during cmdlet execution.

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
Includes the target folder and all its subfolders in the operation.

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
Specifies the value types of assets to retrieve. Valid values: Text, Bool, Integer, Credential.

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

### -ExpandUserValues
Expands user-specific values for per-robot assets, showing actual values assigned to each user/machine combination.

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

### System.String[]
Asset names can be piped to this cmdlet.

### UiPath.PowerShell.Entities.Asset
Asset objects can be piped to this cmdlet. The Name property will be automatically mapped to the -Name parameter via ByPropertyName binding.

## OUTPUTS

### UiPath.PowerShell.Entities.Asset
Returns asset objects with metadata including Id, Name, ValueType, Value, ValueScope, and other properties. When -ExpandUserValues is used, includes user-specific values for per-robot assets.

## NOTES
Asset entities are folder-scoped. You must navigate to a folder or use -Path, -Recurse, or -Depth parameters to specify target folders.

Assets can have ValueScope of Global (same value for all users) or PerRobot (different values per user/machine). Use -ExpandUserValues to see actual assigned values for PerRobot assets.

The -ExportCsv and -ExportCredentialCsv parameters create import-ready CSV files with human-readable names instead of internal IDs.

## RELATED LINKS

[Set-OrchAsset](Set-OrchAsset.md)

[Set-OrchCredentialAsset](Set-OrchCredentialAsset.md)

[Add-OrchAssetLink](Add-OrchAssetLink.md)

[Remove-OrchAsset](Remove-OrchAsset.md)

