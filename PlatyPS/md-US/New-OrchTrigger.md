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

Time-based triggers use cron expressions to schedule process execution at specific times or intervals. Use the -StartProcessCron parameter to define the schedule and -TimeZone to specify the timezone for execution. Calendar integration is supported through the -CalendarName parameter to respect business days and holidays.

PS Orch1:\> Queue-based triggers monitor queue items and automatically start processes when specific conditions are met. Use -QueueDefinitionName to specify the target queue, -ItemsActivationThreshold to set the minimum number of items required for activation, and -ItemsPerJobActivationTarget to control how many items each job should process.

The cmdlet supports various execution strategies including robot allocation, failure handling, and performance monitoring. Use -ExecutorRobots or -MachineRobots to specify execution targets, and configure failure thresholds with -ConsecutiveJobFailuresThreshold and -JobFailuresGracePeriodInHours.

Advanced trigger configurations include stop conditions, alert expressions, and runtime type specifications. The -RunAsMe parameter enables triggers to run with the creator's credentials, while -ResumeOnSameContext ensures consistency in execution context.

This is a folder entity cmdlet that operates within specific folders. Use Set-Location to navigate to the target folder or specify folders using the -Path parameter.

Primary Endpoint: POST /odata/ProcessSchedules, GET /odata/Releases, GET /odata/QueueDefinitions, GET /odata/Robots

OAuth required scopes: OR.Execution or OR.Execution.Write

Required permissions: Execution.Create, Execution.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> New-OrchTrigger DailyReportTrigger BlankProcess19 -WhatIf
```

Tests creating a basic trigger for the BlankProcess19 process using -WhatIf to preview the operation.

### Example 2
```powershell
PS Orch1:\Shared> New-OrchTrigger WeeklyScheduleTrigger BlankProcess19 -StartProcessCron "0 9 * * 1-5" -Priority High
```

Creates a time-based trigger that runs the process every weekday at 9:00 AM with high priority.

### Example 3
```powershell
PS Orch1:\Shared> New-OrchTrigger QueueMonitorTrigger BlankProcess19 -QueueDefinitionName TestQueue123 -ItemsActivationThreshold 5 -MaxJobsForActivation 3
```

Creates a queue-based trigger that starts the process when TestQueue123 has 5 or more items, with a maximum of 3 concurrent jobs.

### Example 4
```powershell
PS Orch1:\Shared> New-OrchTrigger CalendarAwareTrigger BlankProcess19 -StartProcessCron "0 8 * * *" -CalendarName MyCalendar1 -Enabled True
```

Creates a daily trigger at 8:00 AM that respects the business calendar for non-working days.

### Example 5
```powershell
PS C:\> New-OrchTrigger -Path "Orch1:\Development" DevTrigger BlankProcess19 -Priority Normal -ConsecutiveJobFailuresThreshold 3
```

Creates a trigger in the Development folder with failure handling that stops after 3 consecutive failures.

### Example 6
```powershell
PS Orch1:\Shared> New-OrchTrigger RobotSpecificTrigger BlankProcess19 -ExecutorRobots Robot1,Robot2 -RunAsMe True
```

Creates a trigger that runs on specific robots with the creator's credentials.

### Example 7
```powershell
PS Orch1:\Shared> New-OrchTrigger AdvancedQueueTrigger BlankProcess19 -QueueDefinitionName TestQueue123 -ItemsPerJobActivationTarget 10 -ActivateOnJobComplete True -ResumeOnSameContext True
```

Creates an advanced queue trigger that processes 10 items per job, activates on job completion, and maintains execution context.

### Example 8
```powershell
PS Orch1:\Shared> New-OrchTrigger MonitoredTrigger BlankProcess19 -AlertPendingExpression "count() > 5" -AlertRunningExpression "count() > 3" -JobFailuresGracePeriodInHours 2
```

Creates a trigger with monitoring expressions for pending and running jobs, with a 2-hour grace period for failures.

### Example 9
```powershell
PS Orch1:\Shared> New-OrchTrigger StopDateTrigger BlankProcess19 -StartProcessCron "0 */2 * * *" -StopProcessDate (Get-Date).AddDays(30)
```

Creates a trigger that runs every 2 hours but automatically stops after 30 days.

### Example 10
```powershell
PS Orch1:\Shared> New-OrchTrigger InputArgumentsTrigger BlankProcess19 -InputArguments '{Environment:Production,Debug:False}' -RuntimeType Unattended
```

Creates a trigger with specific input arguments in JSON format for unattended execution.

### Example 1
```powershell
PS Orch1:\> New-OrchTrigger DailyTrigger InvoiceProcessing
```

Creates a basic trigger named "DailyTrigger" for the "InvoiceProcessing" process using positional parameters.

### Example 2
```powershell
PS Orch1:\> New-OrchTrigger BusinessHoursTrigger DataProcessor -StartProcessCron "0 9 * * 1-5" -TimeZone UTC -Enabled True
```

Creates a trigger that runs the DataProcessor process at 9 AM on weekdays using Cron scheduling.

### Example 3
```powershell
PS Orch1:\> New-OrchTrigger QueueTrigger EmailHandler -QueueDefinitionName EmailQueue -ItemsActivationThreshold 5 -MaxJobsForActivation 3
```

Creates a queue-based trigger that starts the EmailHandler process when 5 or more items are in the EmailQueue, with a maximum of 3 concurrent jobs.

### Example 4
```powershell
PS Orch1:\> New-OrchTrigger CriticalTrigger EmergencyResponse -Priority High -RuntimeType Unattended -RunAsMe True -MachineRobots Robot1, Robot2
```

Creates a high-priority trigger with specific robot assignments and RunAsMe execution context.

### Example 5
```powershell
PS Orch1:\> New-OrchTrigger -Path Orch1:\Production MonthlyReport ReportGenerator -CalendarName BusinessCalendar -StopProcessDate (Get-Date).AddMonths(1)
```

Creates a trigger in the Production folder with calendar integration and automatic stop date.

### Example 6
```powershell
PS Orch1:\> New-OrchTrigger FailsafeTrigger BackupProcess -ConsecutiveJobFailuresThreshold 3 -JobFailuresGracePeriodInHours 2 -AlertPendingExpression "duration > 30" -WhatIf
```

Shows what would happen when creating a trigger with failure handling and alerting configurations.

## PARAMETERS

### -ActivateOnJobComplete
Specifies whether to activate on job completion. Valid values: True, False.

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
Specifies the expression for pending job alerts.

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
Specifies the expression for running job alerts.

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
Specifies the calendar name to use for scheduling business days and holidays.

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
Specifies the maximum number of consecutive job failures before stopping the trigger.

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
Specifies whether the trigger is enabled ("True") or disabled ("False") upon creation.

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
Specifies input arguments for the process in JSON format.

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
Specifies the connection status requirement. Valid values: True, False.

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
Specifies the target number of items each job should process from the queue.

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
Specifies the grace period in hours for job failure monitoring.

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
Specifies the expression for killing processes.

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
Specifies the machine robots for process execution.

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
Specifies whether to resume on the same context. Valid values: True, False.

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
Specifies whether the trigger should run with the creator's credentials. Valid values: True, False.

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
Specifies the Cron expression for time-based scheduling. Use standard Cron format (e.g., "0 9 * * 1-5" for 9 AM on weekdays).

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
Specifies detailed cron expression information for process scheduling.

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
Specifies the start strategy for the trigger.

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
Specifies the date when the trigger should stop executing.

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
Specifies the expression for stopping processes.

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
Specifies the stop strategy for the trigger.

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
Specifies the timezone for scheduled trigger execution.

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
Specifies how PowerShell responds to progress updates generated by a script, cmdlet, or provider.

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
Specifies the specific robots to execute the triggered process.

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
