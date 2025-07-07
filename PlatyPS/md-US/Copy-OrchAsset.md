---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Copy-OrchAsset

## SYNOPSIS
Copies assets to destination folders.

## SYNTAX

```
Copy-OrchAsset [-Name] <String[]> [-Destination] <String> [-Path <String>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The Copy-OrchAsset cmdlet copies assets from source folders to destination folders within UiPath Orchestrator tenants or across different tenants. This cmdlet creates complete copies of assets, including their values, configurations, and metadata.

The cmdlet supports both intra-tenant copying (within the same tenant) and inter-tenant copying (between different tenants). When copying credential assets between tenants, passwords need to be updated using Set-OrchCredentialAsset after the copy operation.

Use the -Name parameter to specify which assets to copy and the -Destination parameter to specify the target folder. The cmdlet supports CSV file input with Name and Destination columns for bulk copy operations, enabling complex asset migration scenarios.

This is a folder entity cmdlet. Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters. The -Recurse parameter enables copying assets from all subfolders, maintaining the folder structure in the destination.

Primary Endpoint:

OAuth required scopes:

Required permissions:

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Development> Copy-OrchAsset DatabaseConnection Orch1:\Production
```

Copies the DatabaseConnection asset from the current folder (Development) to the Production folder within the same tenant using positional parameters.

### Example 2
```powershell
PS Orch2:\> Copy-OrchAsset -Path Orch1:\Development APIKey Orch2:\Production
```

Copies the APIKey asset from Orch1:\Development to Orch2:\Production, demonstrating inter-tenant asset copying.

### Example 3
```powershell
PS Orch1:\Development> Copy-OrchAsset ConfigAsset, DatabaseConnection Orch1:\Production -WhatIf
```

Shows what would happen when copying multiple assets from the current folder to the Production folder using -WhatIf for safety.

### Example 4
```powershell
PS C:\Scripts> Import-Csv asset-migration.csv | Copy-OrchAsset
```

Copies assets using a CSV file with Name and Destination columns for bulk operations. The CSV file is located in the current location (C:\Scripts).
CSV format:
Path,Name,Destination
Orch1:\Development,DatabaseConnection,Orch2:\Production
Orch1:\Development,APIKey,Orch3:\Development

### Example 5
```powershell
PS Orch1:\> Copy-OrchAsset -Path Orch1:\Development *Config* Orch2:\Production
```

Copies all assets containing Config in their name from Orch1:\Development to Orch2:\Production using wildcards for inter-tenant copying.

### Example 6
```powershell
PS Orch1:\> Copy-OrchAsset -Recurse *Database* Orch2:\Finance -WhatIf
```

Shows what would happen when copying all assets containing Database from all subfolders recursively to Orch2:\Finance.

## PARAMETERS

### -Destination
Specifies the destination folders.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Name
Specifies the Name of the assets to be copied.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Path
Specifies the source folders. If not specified, the current folder will be used as the source.

```yaml
Type: String
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

### -Depth
{{ Fill Depth Description }}

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

### -Recurse
{{ Fill Recurse Description }}

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

### System.String[]
Asset names can be piped to this cmdlet.

### UiPath.PowerShell.Entities.Asset
Asset objects from Get-OrchAsset can be piped to this cmdlet. The Name property will be automatically mapped to the -Name parameter via ByPropertyName binding.

### System.Management.Automation.PSCustomObject
CSV data with Path, Name and Destination columns can be piped to this cmdlet via Import-Csv for bulk copy operations.

## OUTPUTS

### None
This cmdlet does not generate any output.

## NOTES
This is a folder entity cmdlet. Use the -Path parameter to specify the source folder or navigate to the source folder using Set-Location.

The cmdlet supports both intra-tenant and inter-tenant copying. When copying credential assets between tenants, use Set-OrchCredentialAsset to update passwords after copying.

For bulk operations, use CSV files with Name and Destination columns. The -Recurse parameter copies assets from all subfolders while maintaining folder structure.

## RELATED LINKS

[Get-OrchAsset](Get-OrchAsset.md)

[Remove-OrchAsset](Remove-OrchAsset.md)

[Set-OrchAsset](Set-OrchAsset.md)

[Set-OrchCredentialAsset](Set-OrchCredentialAsset.md)


