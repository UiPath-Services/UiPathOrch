---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Copy-OrchTrigger

## SYNOPSIS
Copies triggers to destination folders.

## SYNTAX

```
Copy-OrchTrigger [-Name] <String[]> [-Destination] <String> [-Path <String>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The Copy-OrchTrigger cmdlet copies triggers from source folders to destination folders within UiPath Orchestrator tenants or across different tenants. This cmdlet creates complete copies of triggers, including their scheduling configurations, process associations, and execution parameters.

The cmdlet supports both intra-tenant copying (within the same tenant) and inter-tenant copying (between different tenants). Triggers define automated process execution schedules and conditions, making this cmdlet essential for deploying automation schedules across different environments.

Use the -Name parameter to specify which triggers to copy and the -Destination parameter to specify the target folder. The cmdlet supports wildcard patterns for copying multiple triggers efficiently. Note that copied triggers may need adjustment if the associated processes have different names in the destination folder.

This is a folder entity cmdlet. Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters. The -Recurse parameter enables copying triggers from all subfolders, maintaining the folder structure in the destination.

Primary Endpoint: GET /odata/ProcessSchedules({key}), GET /odata/Releases, POST /odata/ProcessSchedules

OAuth required scopes: OR.Jobs OR.Execution

Required permissions: Schedules.View, Schedules.Create, Processes.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Development> Copy-OrchTrigger DailyReportTrigger Orch1:\Production
```

Copies the DailyReportTrigger from the current folder (Development) to the Production folder within the same tenant using positional parameters.

### Example 2
```powershell
PS C:\> Copy-OrchTrigger -Path Orch1:\Development ScheduledBackup Orch2:\Production
```

Copies the ScheduledBackup trigger from Orch1:\Development to Orch2:\Production, demonstrating inter-tenant trigger copying.

### Example 3
```powershell
PS Orch1:\Development> Copy-OrchTrigger *Daily*, *Weekly* Orch1:\Production -WhatIf
```

Shows what would happen when copying multiple triggers with names containing Daily or Weekly from the current folder to the Production folder using -WhatIf for safety.

### Example 4
```powershell
PS C:\> Copy-OrchTrigger -Path Orch1:\Development *Scheduled* Orch2:\Production
```

Copies all triggers containing Scheduled in their name from Orch1:\Development to Orch2:\Production using wildcards for inter-tenant copying.

### Example 5
```powershell
PS Orch1:\> Copy-OrchTrigger -Recurse *Automated* Orch2:\Finance -WhatIf
```

Shows what would happen when copying all triggers containing Automated from all subfolders recursively to Orch2:\Finance.

### Example 6
```powershell
PS Orch1:\Development> Get-OrchTrigger *Batch* | Copy-OrchTrigger -Destination Orch2:\Production
```

Gets all triggers containing Batch in their names and copies them to Orch2:\Production using pipeline input.

## PARAMETERS

### -Destination
Specifies the destination folder where triggers should be copied.

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
Specifies the Name of the triggers to be copied.

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
Specifies that triggers should be copied from all subfolders recursively, maintaining the folder structure in the destination.

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

### UiPath.PowerShell.Entities.ProcessSchedule
## NOTES
This is a folder entity cmdlet. Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters.

Triggers contain scheduling configurations and process associations. When copying across environments, verify that associated processes exist in the destination folder and adjust trigger configurations if necessary. Use wildcards for efficient bulk operations and -WhatIf for testing before actual execution.

## RELATED LINKS

[Get-OrchTrigger](Get-OrchTrigger.md)

[Remove-OrchTrigger](Remove-OrchTrigger.md)

[Set-OrchTrigger](Set-OrchTrigger.md)

[Start-OrchTrigger](Start-OrchTrigger.md)
