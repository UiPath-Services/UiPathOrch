---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# New-OrchTrigger

## SYNOPSIS
Creates automation triggers for process execution.

## SYNTAX

```
New-OrchTrigger [-Name] <String[]> [-ReleaseName] <String> [-Enabled <String>] [-Priority <String>]
 [-StartStrategy <Int32>] [-StopStrategy <String>] [-StopProcessExpression <String>]
 [-KillProcessExpression <String>] [-AlertPendingExpression <String>] [-AlertRunningExpression <String>]
 [-ConsecutiveJobFailuresThreshold <Int32>] [-JobFailuresGracePeriodInHours <Int32>] [-RuntimeType <String>]
 [-InputArguments <String>] [-ResumeOnSameContext <String>] [-RunAsMe <String>] [-IsConnected <String>]
 [-CalendarName <String>] [-ActivateOnJobComplete <String>] [-ItemsActivationThreshold <Int32>]
 [-ItemsPerJobActivationTarget <Int32>] [-MaxJobsForActivation <Int32>] [-StartProcessCronDetails <String>]
 [-StartProcessCron <String>] [-QueueDefinitionName <String>] [-TimeZone <String>]
 [-StopProcessDate <DateTime>] [-ExecutorRobots <String[]>] [-MachineRobots <String[]>] [-Path <String[]>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The New-OrchTrigger cmdlet creates automation triggers that automatically start process executions based on various conditions. Triggers can be time-based (scheduled), queue-based (activated by queue items), or event-based (triggered by specific conditions).

**This is a folder entity cmdlet.** To use this cmdlet, you must first navigate to the target folder using Set-Location (cd), or specify the target folders using the -Path parameter. If you attempt to run this cmdlet without being in a folder context, you will receive the error: \"Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters.\"

Triggers support various execution strategies including time-based scheduling using Cron expressions, queue-based activation when items are added to queues, and advanced configurations such as calendar integration, robot assignment, and failure handling policies.

Primary Endpoint: POST /odata/ProcessSchedules
OAuth required scopes: OR.Execution or OR.Execution.Write
Required permissions: Execution.Create

## EXAMPLES

### Example 1
```powershell
New-OrchTrigger DailyTrigger InvoiceProcessing
```

Creates a basic trigger named \"DailyTrigger\" for the \"InvoiceProcessing\" process using positional parameters.

### Example 2
```powershell
New-OrchTrigger BusinessHoursTrigger DataProcessor -StartProcessCron \"0 9 * * 1-5\" -TimeZone \"UTC\" -Enabled \"True\"
```

Creates a trigger that runs the DataProcessor process at 9 AM on weekdays using Cron scheduling.

### Example 3
```powershell
New-OrchTrigger QueueTrigger EmailHandler -QueueDefinitionName \"EmailQueue\" -ItemsActivationThreshold 5 -MaxJobsForActivation 3
```

Creates a queue-based trigger that starts the EmailHandler process when 5 or more items are in the EmailQueue, with a maximum of 3 concurrent jobs.

### Example 4
```powershell
New-OrchTrigger CriticalTrigger EmergencyResponse -Priority \"High\" -RuntimeType \"Unattended\" -RunAsMe \"True\" -MachineRobots \"Robot1\", \"Robot2\"
```

Creates a high-priority trigger with specific robot assignments and RunAsMe execution context.

### Example 5
```powershell
New-OrchTrigger -Path Orch1:\Production MonthlyReport ReportGenerator -CalendarName \"BusinessCalendar\" -StopProcessDate (Get-Date).AddMonths(1)
```

Creates a trigger in the Production folder with calendar integration and automatic stop date.

### Example 6
```powershell
New-OrchTrigger FailsafeTrigger BackupProcess -ConsecutiveJobFailuresThreshold 3 -JobFailuresGracePeriodInHours 2 -AlertPendingExpression \"duration > 30\" -WhatIf
```

Shows what would happen when creating a trigger with failure handling and alerting configurations.

## PARAMETERS

### -ActivateOnJobComplete
{{ Fill ActivateOnJobComplete Description }}

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -AlertPendingExpression
{{ Fill AlertPendingExpression Description }}

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -AlertRunningExpression
{{ Fill AlertRunningExpression Description }}

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -CalendarName
{{ Fill CalendarName Description }}

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

### -ConsecutiveJobFailuresThreshold
{{ Fill ConsecutiveJobFailuresThreshold Description }}

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Enabled
Specifies whether the trigger is enabled (\"True\") or disabled (\"False\") upon creation.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -InputArguments
{{ Fill InputArguments Description }}

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -IsConnected
{{ Fill IsConnected Description }}

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -ItemsActivationThreshold
Specifies the minimum number of queue items required to activate the trigger.

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -ItemsPerJobActivationTarget
{{ Fill ItemsPerJobActivationTarget Description }}

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -JobFailuresGracePeriodInHours
{{ Fill JobFailuresGracePeriodInHours Description }}

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -KillProcessExpression
{{ Fill KillProcessExpression Description }}

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -MachineRobots
{{ Fill MachineRobots Description }}

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -MaxJobsForActivation
Specifies the maximum number of concurrent jobs that can be started by this trigger.

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Name
Specifies the name(s) of the trigger(s) to create. Trigger names must be unique within the folder.

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
Specifies the path(s) to the target folder(s) where the trigger(s) will be created. Use this parameter to create triggers in specific folders without changing your current location.

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

### -Priority
Specifies the job priority for triggered executions. Valid values include Low, Normal, High, and Critical.

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

### -QueueDefinitionName
Specifies the queue name for queue-based triggers. The trigger will activate when items are added to this queue.

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

### -ReleaseName
Specifies the name of the process (release) that this trigger will execute. The process must exist in the same folder.

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

### -ResumeOnSameContext
{{ Fill ResumeOnSameContext Description }}

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -RunAsMe
{{ Fill RunAsMe Description }}

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -RuntimeType
Specifies the robot runtime type. Valid values include Unattended, NonProduction, and TestAutomation.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -StartProcessCron
Specifies the Cron expression for time-based scheduling. Use standard Cron format (e.g., \"0 9 * * 1-5\" for 9 AM on weekdays).

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -StartProcessCronDetails
{{ Fill StartProcessCronDetails Description }}

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -StartStrategy
{{ Fill StartStrategy Description }}

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -StopProcessDate
{{ Fill StopProcessDate Description }}

```yaml
Type: DateTime
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -StopProcessExpression
{{ Fill StopProcessExpression Description }}

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -StopStrategy
{{ Fill StopStrategy Description }}

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -TimeZone
{{ Fill TimeZone Description }}

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -WhatIf
Shows what would happen if the cmdlet runs. The cmdlet is not run.

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

### -ExecutorRobots
{{ Fill ExecutorRobots Description }}

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]
### System.String
### System.Nullable`1[[System.Int32, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
### System.Nullable`1[[System.DateTime, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
## OUTPUTS

### UiPath.PowerShell.Entities.ProcessSchedule
## NOTES
- Trigger names must be unique within the folder
- Cron expressions use standard format: second minute hour day month dayofweek year
- Queue-based triggers require existing queues
- Consider using -WhatIf to preview the operation before actual creation
- Time zones should be specified as valid TimeZone identifiers
- Robot assignments must reference existing robots
- Calendar integration requires existing calendar objects

## RELATED LINKS

[Get-OrchTrigger](Get-OrchTrigger.md)
[Update-OrchTrigger](Update-OrchTrigger.md)
[Remove-OrchTrigger](Remove-OrchTrigger.md)
[Enable-OrchTrigger](Enable-OrchTrigger.md)
[Disable-OrchTrigger](Disable-OrchTrigger.md)
