---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Disable-OrchTrigger

## SYNOPSIS
Disables specified automation triggers to prevent scheduled execution.

## SYNTAX

```
Disable-OrchTrigger [-Name] <String[]> [-Path <String[]>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The Disable-OrchTrigger cmdlet disables specified automation triggers within UiPath Orchestrator, preventing them from executing according to their configured schedules or conditions. Triggers define when and how automation processes should be executed, and disabling them temporarily suspends automation execution.

Disabling triggers is useful for maintenance periods, troubleshooting, testing scenarios, or when you need to temporarily halt specific automation processes without deleting the trigger configuration. The trigger settings are preserved and can be easily re-enabled using Enable-OrchTrigger.

This cmdlet operates as a folder entity operation, requiring navigation to the appropriate folder context or specification of target folders using the -Path parameter. Use the -Recurse parameter to include triggers in subfolders, and -Depth to control recursion levels.

Disabling triggers directly affects automation execution schedules. Use -WhatIf to preview the operation and -Confirm for confirmation prompts when disabling multiple triggers that may impact business processes.

Primary Endpoint: POST /odata/ProcessSchedules/UiPath.Server.Configuration.OData.SetEnabled

OAuth required scopes: OR.Jobs

Required permissions: Schedules.Edit

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Disable-OrchTrigger DailyReportTrigger -WhatIf
```

Shows what would happen when disabling the DailyReportTrigger in the current Production folder.

### Example 2
```powershell
PS C:\> Disable-OrchTrigger -Path Orch1:\Production -Name *Maintenance* -Confirm
```

Disables all triggers with names containing "Maintenance" in the Production folder with confirmation prompts.

### Example 3
```powershell
PS Orch1:\> Disable-OrchTrigger -Recurse TestTrigger1, TestTrigger2
```

Disables TestTrigger1 and TestTrigger2 across all folders.

### Example 4
```powershell
PS Orch1:\Development> Disable-OrchTrigger *Debug* -WhatIf
```

Shows what would happen when disabling all triggers with names containing "Debug" in the Development folder.

### Example 5
```powershell
PS C:\> Disable-OrchTrigger -Path Orch1:\Production -Recurse -Depth 1 -Name WeekendTrigger
```

Disables WeekendTrigger in the Production folder and its immediate subfolders.

### Example 6
```powershell
PS Orch1:\> Get-OrchTrigger -Recurse | Where-Object {$_.StartProcessNextOccurrence -lt (Get-Date).AddDays(1)} | Disable-OrchTrigger -Confirm
```

Disables all triggers scheduled to execute within the next day with confirmation prompts.

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
Specifies the names of triggers to be disabled. Supports wildcard patterns for flexible trigger selection.

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
Includes the target folder and all its subfolders in the operation. Essential for comprehensive trigger management across folder hierarchies.

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

### -Confirm
Prompts you for confirmation before running the cmdlet. Highly recommended when disabling triggers that affect business processes.

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
Shows what would happen if the cmdlet runs without actually disabling the triggers. Highly recommended for previewing the operation scope.

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
This cmdlet is a folder entity operation that suspends automation trigger execution without deleting trigger configuration. Disabling triggers directly affects automation schedules and may impact business processes. Use -WhatIf to preview operations and -Confirm for safety when disabling multiple triggers. The operation requires Schedules.Edit permissions in the target folders. Disabled triggers can be re-enabled using Enable-OrchTrigger while preserving all configuration settings.

## RELATED LINKS

[Enable-OrchTrigger](Enable-OrchTrigger.md)

[Get-OrchTrigger](Get-OrchTrigger.md)


[Remove-OrchTrigger](Remove-OrchTrigger.md)
