---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Copy-OrchProcess

## SYNOPSIS
Copies processes to destination folders.

## SYNTAX

```
Copy-OrchProcess [-Name] <String[]> [-Destination] <String> [-Path <String>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The Copy-OrchProcess cmdlet copies processes from source folders to destination folders within UiPath Orchestrator tenants or across different tenants. This cmdlet creates complete copies of processes, including their configurations, parameters, and metadata.

The cmdlet supports both intra-tenant copying (within the same tenant) and inter-tenant copying (between different tenants). Processes contain automation workflows and their execution configurations, making this cmdlet essential for deploying processes across different environments.

Use the -Name parameter to specify which processes to copy and the -Destination parameter to specify the target folder. The cmdlet supports wildcard patterns for copying multiple processes efficiently.

This is a folder entity cmdlet. Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters. The -Recurse parameter enables copying processes from all subfolders, maintaining the folder structure in the destination.

Primary Endpoint: [PLACEHOLDER - 具体的なAPIエンドポイント]

OAuth required scopes: [PLACEHOLDER]

Required permissions: [PLACEHOLDER]

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Development> Copy-OrchProcess InvoiceProcessing Orch1:\Production
```

Copies the InvoiceProcessing process from the current folder (Development) to the Production folder within the same tenant using positional parameters.

### Example 2
```powershell
PS C:\> Copy-OrchProcess -Path Orch1:\Development EmailAutomation Orch2:\Production
```

Copies the EmailAutomation process from Orch1:\Development to Orch2:\Production, demonstrating inter-tenant process copying.

### Example 3
```powershell
PS Orch1:\Development> Copy-OrchProcess *Report*, *Invoice* Orch1:\Production -WhatIf
```

Shows what would happen when copying multiple processes with names containing Report or Invoice from the current folder to the Production folder using -WhatIf for safety.

### Example 4
```powershell
PS C:\> Copy-OrchProcess -Path Orch1:\Development *Automation* Orch2:\Production
```

Copies all processes containing Automation in their name from Orch1:\Development to Orch2:\Production using wildcards for inter-tenant copying.

### Example 5
```powershell
PS Orch1:\> Copy-OrchProcess -Recurse *Daily* Orch2:\Finance -WhatIf
```

Shows what would happen when copying all processes containing Daily from all subfolders recursively to Orch2:\Finance.

### Example 6
```powershell
PS Orch1:\Development> Get-OrchProcess *Scheduled* | Copy-OrchProcess -Destination Orch2:\Production
```

Gets all processes containing Scheduled in their names and copies them to Orch2:\Production using pipeline input.

## PARAMETERS

### -Destination
Specifies the destination folder where processes should be copied.

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
Specifies the Name of the processes to be copied.

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
Specifies the source folder. If not specified, the current folder will be used as the source.

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
Specifies that processes should be copied from all subfolders recursively, maintaining the folder structure in the destination.

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
Process names can be piped to this cmdlet.

### UiPath.PowerShell.Entities.Process
Process objects from Get-OrchProcess can be piped to this cmdlet. The Name property will be automatically mapped to the -Name parameter via ByPropertyName binding.

## OUTPUTS

### None
This cmdlet does not generate any output.

## NOTES
This is a folder entity cmdlet. Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters.

Processes contain automation workflows and execution configurations. Use wildcards for efficient bulk operations and -WhatIf for testing before actual execution.

## RELATED LINKS

[Get-OrchProcess](Get-OrchProcess.md)

[Remove-OrchProcess](Remove-OrchProcess.md)

[Start-OrchProcess](Start-OrchProcess.md)

[Set-OrchProcess](Set-OrchProcess.md)
