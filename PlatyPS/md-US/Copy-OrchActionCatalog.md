---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Copy-OrchActionCatalog

## SYNOPSIS
Copies action catalogs to destination folders.

## SYNTAX

```
Copy-OrchActionCatalog [-Name] <String[]> [-Destination] <String> [-Path <String>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The Copy-OrchActionCatalog cmdlet copies action catalogs from source folders to destination folders within UiPath Orchestrator tenants or across different tenants. This cmdlet creates complete copies of action catalogs, including their configurations and metadata.

The cmdlet supports both intra-tenant copying (within the same tenant) and inter-tenant copying (between different tenants). Action catalogs can be copied to maintain consistency across different environments or for backup purposes.

Use the -Name parameter to specify which action catalogs to copy and the -Destination parameter to specify the target folder. The cmdlet supports wildcard patterns for copying multiple action catalogs efficiently.

This is a folder entity cmdlet. Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters. The -Recurse parameter enables copying action catalogs from all subfolders, maintaining the folder structure in the destination.

Primary Endpoint: [PLACEHOLDER - 具体的なAPIエンドポイント]

OAuth required scopes: [PLACEHOLDER]

Required permissions: [PLACEHOLDER]

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Development> Copy-OrchActionCatalog MyActionCatalog Orch1:\Production
```

Copies the MyActionCatalog action catalog from the current folder (Development) to the Production folder within the same tenant using positional parameters.

### Example 2
```powershell
PS C:\> Copy-OrchActionCatalog -Path Orch1:\Development EmailActions Orch2:\Production
```

Copies the EmailActions action catalog from Orch1:\Development to Orch2:\Production, demonstrating inter-tenant action catalog copying.

### Example 3
```powershell
PS Orch1:\Development> Copy-OrchActionCatalog *Email*, *Database* Orch1:\Production -WhatIf
```

Shows what would happen when copying multiple action catalogs with names containing Email or Database from the current folder to the Production folder using -WhatIf for safety.

### Example 4
```powershell
PS C:\> Copy-OrchActionCatalog -Path Orch1:\Development *Custom* Orch2:\Production
```

Copies all action catalogs containing Custom in their name from Orch1:\Development to Orch2:\Production using wildcards for inter-tenant copying.

### Example 5
```powershell
PS Orch1:\> Copy-OrchActionCatalog -Recurse *API* Orch2:\Finance -WhatIf
```

Shows what would happen when copying all action catalogs containing API from all subfolders recursively to Orch2:\Finance.

### Example 6
```powershell
PS Orch1:\Development> Get-OrchActionCatalog *Custom* | Copy-OrchActionCatalog -Destination Orch2:\Production
```

Gets all action catalogs containing Custom in their names and copies them to Orch2:\Production using pipeline input with wildcard filtering.

## PARAMETERS

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
Specifies the Name of the action catalogs to be copied.

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

### -Depth
Specifies the maximum number of subfolder levels to include when using -Recurse parameter.

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
Specifies that action catalogs should be copied from all subfolders recursively, maintaining the folder structure in the destination.

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
Action catalog names can be piped to this cmdlet.

### UiPath.PowerShell.Entities.ActionCatalog
Action catalog objects from Get-OrchActionCatalog can be piped to this cmdlet. The Name property will be automatically mapped to the -Name parameter via ByPropertyName binding.

## OUTPUTS

### None
This cmdlet does not generate any output.

## NOTES
This is a folder entity cmdlet. Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters.

The cmdlet supports both intra-tenant and inter-tenant copying. Use wildcards for efficient bulk operations and -WhatIf for testing before actual execution.

## RELATED LINKS

[Get-OrchActionCatalog](Get-OrchActionCatalog.md)

[Remove-OrchActionCatalog](Remove-OrchActionCatalog.md)
