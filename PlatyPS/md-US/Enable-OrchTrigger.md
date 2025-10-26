---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Enable-OrchTrigger

## SYNOPSIS
Enables specified automation triggers to resume scheduled execution.

## SYNTAX

```
Enable-OrchTrigger [-Name] <String[]> [-Path <String[]>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The Enable-OrchTrigger cmdlet enables specified automation triggers within UiPath Orchestrator, allowing them to resume scheduled execution of automation processes. Triggers define when and how automation processes should be executed, including time-based schedules, queue-based triggers, and other automation activation conditions.

When triggers are enabled, they become active and will execute according to their configured schedule or conditions. This cmdlet is essential for resuming automation execution after maintenance periods, troubleshooting, or when triggers have been temporarily disabled.

This cmdlet operates as a folder entity operation, requiring navigation to the appropriate folder context or specification of target folders using the -Path parameter. Use the -Recurse parameter to include triggers in subfolders, and -Depth to control recursion levels.

Enabling triggers is a critical operation that directly affects automation execution schedules. Use -WhatIf to preview the operation and -Confirm for confirmation prompts when enabling multiple triggers.

Primary Endpoint: POST /odata/ProcessSchedules/UiPath.Server.Configuration.OData.SetEnabled

OAuth required scopes: OR.Jobs

Required permissions: Schedules.Edit

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Enable-OrchTrigger DailyReportTrigger
```

Enables the DailyReportTrigger in the current Production folder.

### Example 2
```powershell
PS C:\> Enable-OrchTrigger -Path Orch1:\Production -Name *Invoice* -WhatIf
```

Shows what would happen when enabling all triggers with names containing "Invoice" in the Production folder.

### Example 3
```powershell
PS Orch1:\> Enable-OrchTrigger -Recurse MonthlyTrigger, WeeklyTrigger -Confirm
```

Enables MonthlyTrigger and WeeklyTrigger across all folders with confirmation prompts.

### Example 4
```powershell
PS Orch1:\Development> Enable-OrchTrigger TestTrigger1, TestTrigger2
```

Enables multiple test triggers in the Development folder.

### Example 5
```powershell
PS C:\> Enable-OrchTrigger -Path Orch1:\Production -Recurse -Depth 2 -Name *Schedule*
```

Enables all triggers with names containing "Schedule" in the Production folder and up to 2 levels of subfolders.

### Example 6
```powershell
PS Orch1:\> Get-OrchTrigger | Where-Object {$_.Enabled -eq $false} | Enable-OrchTrigger -WhatIf
```

Shows what would happen when enabling all currently disabled triggers using pipeline input.

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
Specifies the names of triggers to be enabled. Supports wildcard patterns for flexible trigger selection.

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
Prompts you for confirmation before running the cmdlet. Recommended when enabling multiple triggers that affect automation schedules.

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
Shows what would happen if the cmdlet runs without actually enabling the triggers. Recommended for previewing the operation scope.

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
This cmdlet is a folder entity operation that activates automation triggers for scheduled execution. Enabling triggers directly affects automation schedules and process execution. Use -WhatIf to preview operations and -Confirm for safety when enabling multiple triggers. The operation requires Schedules.Edit permissions in the target folders. Once enabled, triggers will execute according to their configured schedules and conditions.

## RELATED LINKS

[Disable-OrchTrigger](Disable-OrchTrigger.md)

[Get-OrchTrigger](Get-OrchTrigger.md)

[Set-OrchTrigger](Set-OrchTrigger.md)

[Add-OrchTrigger](Add-OrchTrigger.md)
