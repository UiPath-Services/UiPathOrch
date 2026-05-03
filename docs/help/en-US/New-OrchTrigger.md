---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/New-OrchTrigger.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: New-OrchTrigger
---

# New-OrchTrigger

## SYNOPSIS

Creates a new trigger in UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
New-OrchTrigger [-Name] <string[]> [-ReleaseName] <string> [-Enabled <string>]
 [-SpecificPriorityValue <int>] [-Priority <string>] [-StartStrategy <int>] [-StopStrategy <string>]
 [-StopProcessExpression <string>] [-KillProcessExpression <string>]
 [-AlertPendingExpression <string>] [-AlertRunningExpression <string>]
 [-ConsecutiveJobFailuresThreshold <int>] [-JobFailuresGracePeriodInHours <int>]
 [-RuntimeType <string>] [-InputArguments <string>] [-ResumeOnSameContext <string>]
 [-RunAsMe <string>] [-IsConnected <string>] [-CalendarName <string>]
 [-ActivateOnJobComplete <string>] [-ItemsActivationThreshold <int>]
 [-ItemsPerJobActivationTarget <int>] [-MaxJobsForActivation <int>]
 [-StartProcessCronDetails <string>] [-StartProcessCron <string>] [-QueueDefinitionName <string>]
 [-TimeZone <string>] [-TimeZoneId <string>] [-StopProcessDate <datetime>]
 [-ExecutorRobots <string[]>] [-MachineRobots <string[]>] [-Path <string[]>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Creates a new trigger (process schedule) in UiPath Orchestrator. A trigger defines a time-based or queue-based schedule for starting an automation process. The trigger is associated with a process (Release) identified by the -ReleaseName parameter.

By default, the trigger is created with -Enabled set to true, -StartStrategy set to 1, -RuntimeType set to Unattended, -ResumeOnSameContext set to false, -RunAsMe set to false, and -StartProcessCron set to "0 0/1 * 1/1 * ? *" (every minute).

The -Name parameter is mandatory and positional (position 0). The -ReleaseName parameter is mandatory and positional (position 1), supporting wildcards and tab completion from available processes in the target folder.

This cmdlet supports ShouldProcess. Use -WhatIf to preview what would be created, or -Confirm to be prompted before each creation.

Primary Endpoint: POST /odata/ProcessSchedules

OAuth required scopes: OR.Execution

Required permissions: Schedules.Create

## EXAMPLES

### Example 1: Create a basic trigger

```powershell
PS Orch1:\Shared> New-OrchTrigger NewTrigger1 BlankProcess19
```

Creates a new trigger named "NewTrigger1" associated with the process "BlankProcess19" in the current folder. The trigger is enabled by default with the default cron schedule (every minute).

### Example 2: Create a trigger with a cron schedule

```powershell
PS Orch1:\Shared> New-OrchTrigger NewTrigger1 BlankProcess19 -StartProcessCron "0 0 0/1 1/1 * ? *" -TimeZone "Tokyo Standard Time"
```

Creates a trigger that runs every hour using Tokyo Standard Time. The -StartProcessCron parameter accepts a Quartz cron expression.

### Example 3: Create a queue-based trigger

```powershell
PS Orch1:\Shared> New-OrchTrigger NewTrigger1 BlankProcess19 -QueueDefinitionName TestQueue2 -ItemsActivationThreshold 10 -ItemsPerJobActivationTarget 5 -MaxJobsForActivation 3
```

Creates a queue-based trigger that activates when the "TestQueue2" has 10 or more pending items. Each job targets 5 items, and up to 3 jobs can be created per activation.

### Example 4: Create a trigger with a specific runtime type

```powershell
PS Orch1:\Shared> New-OrchTrigger NewTrigger1 BlankProcess19 -RuntimeType NonProduction -Enabled false
```

Creates a trigger in the Shared folder with NonProduction runtime type, initially disabled.

### Example 5: Preview trigger creation with WhatIf

```powershell
PS Orch1:\Shared> New-OrchTrigger NewTrigger1 BlankProcess19 -WhatIf
```

Shows what would happen without actually creating the trigger. Useful for verifying the trigger configuration before execution.

## PARAMETERS

### -Path

Specifies the target folder where the trigger will be created. If not specified, the current folder is targeted. Supports wildcards and comma-separated values for multiple folders.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -ActivateOnJobComplete

Specifies whether the trigger should activate again after the triggered job completes. Tab completion suggests "true" and "false" values.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -AlertPendingExpression

Specifies the cron expression for alerting when a triggered job remains in the Pending state. This defines when alert notifications are sent for jobs that have not yet started.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -AlertRunningExpression

Specifies the cron expression for alerting when a triggered job remains in the Running state. This defines when alert notifications are sent for jobs that are still running.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -CalendarName

Specifies the name of the calendar to associate with the trigger. Supports wildcards. Tab completion dynamically suggests available calendar names.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Confirm

Prompts you for confirmation before running the cmdlet.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases:
- cf
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -ConsecutiveJobFailuresThreshold

Specifies the number of consecutive job failures after which the trigger is automatically disabled. This prevents a failing process from being repeatedly triggered.

```yaml
Type: System.Nullable`1[System.Int32]
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Enabled

Specifies whether the trigger is enabled. Default is true. Tab completion suggests "true" and "false" values.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -ExecutorRobots

Specifies the names of the robots that should execute the triggered jobs. Multiple robot names can be specified as a string array.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -InputArguments

Specifies the input arguments to pass to the process when the trigger fires. The value should be a JSON string representing key-value pairs of argument names and their values.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -IsConnected

Specifies whether the trigger is connected. Tab completion suggests "true" and "false" values.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -ItemsActivationThreshold

Specifies the number of pending queue items required to activate the trigger. This parameter is used for queue-based triggers.

```yaml
Type: System.Nullable`1[System.Int32]
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -ItemsPerJobActivationTarget

Specifies the target number of queue items to process per triggered job. This parameter is used for queue-based triggers to determine how items are distributed across jobs.

```yaml
Type: System.Nullable`1[System.Int32]
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -JobFailuresGracePeriodInHours

Specifies the grace period in hours during which consecutive job failures are counted. Failures outside this window are not counted toward the ConsecutiveJobFailuresThreshold.

```yaml
Type: System.Nullable`1[System.Int32]
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -KillProcessExpression

Specifies the cron expression that defines when a running job should be forcefully killed. This is a more aggressive termination compared to StopProcessExpression.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -MachineRobots

Specifies the names of the machine-robot mappings that should execute the triggered jobs. Multiple values can be specified as a string array.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -MaxJobsForActivation

Specifies the maximum number of jobs that can be created per trigger activation. This parameter is used for queue-based triggers to limit concurrent job execution.

```yaml
Type: System.Nullable`1[System.Int32]
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Name

Specifies the names of the triggers to create. Multiple names can be specified as comma-separated values. Tab completion suggests "NewTrigger1" as the default name.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 0
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Priority

Specifies the job priority for triggered jobs. Tab completion suggests available priority values.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -QueueDefinitionName

Specifies the name of the queue that activates this trigger. Supports wildcards. Tab completion dynamically suggests available queue names from the target folder. This parameter is used for queue-based triggers.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -ReleaseName

Specifies the name of the process (Release) to associate with the trigger. Supports wildcards. Tab completion dynamically suggests available process names from the target folder. This parameter is mandatory and positional (position 1).

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: (All)
  Position: 1
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -ResumeOnSameContext

Specifies whether the job should resume on the same execution context. Default is false. Tab completion suggests "true" and "false" values.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -RunAsMe

Specifies whether the triggered job should run under the credentials of the user who created the trigger. Default is false. Tab completion suggests "true" and "false" values.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -RuntimeType

Specifies the runtime type for triggered jobs. Default is Unattended. Tab completion suggests available runtime types.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -SpecificPriorityValue

Specifies a specific numeric priority value for triggered jobs. This provides finer control than the -Priority parameter.

```yaml
Type: System.Nullable`1[System.Int32]
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -StartProcessCron

Specifies the Quartz cron expression that defines the trigger schedule. Default is "0 0/1 * 1/1 * ? *" (every minute).

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -StartProcessCronDetails

Specifies additional cron schedule details in JSON format. This provides a human-readable representation of the cron schedule configuration.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -StartStrategy

Specifies the start strategy for the trigger. Default is 1. The start strategy defines how jobs are created when the trigger fires (e.g., how many jobs to create per trigger activation).

```yaml
Type: System.Nullable`1[System.Int32]
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -StopProcessDate

Specifies the date and time when the trigger should stop firing. After this date, the trigger will no longer create new jobs.

```yaml
Type: System.Nullable`1[System.DateTime]
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -StopProcessExpression

Specifies the cron expression that defines when a running job should be gracefully stopped. This sends a stop signal to the running process.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -StopStrategy

Specifies the strategy used to stop running jobs. Tab completion suggests "SoftStop" and "Kill" values. SoftStop sends a cancellation signal to the process, while Kill forcefully terminates it.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -TimeZone

Specifies the time zone for the trigger schedule using the display name (e.g., "Eastern Standard Time"). Supports wildcards. Tab completion dynamically suggests available time zones.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -TimeZoneId

Specifies the time zone for the trigger schedule using the time zone identifier. This parameter is primarily used for CSV import scenarios.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -WhatIf

Runs the command in a mode that only reports what would happen without performing the actions.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases:
- wi
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String

You can pipe trigger property values to this cmdlet via property name binding.

### System.Int32

You can pipe integer property values (such as StartStrategy, ItemsActivationThreshold) to this cmdlet via property name binding.

### System.DateTime

You can pipe a StopProcessDate value to this cmdlet via property name binding.

### System.String[]

You can pipe string array property values (such as Name, ExecutorRobots, MachineRobots) to this cmdlet via property name binding.

## OUTPUTS

### UiPath.PowerShell.Entities.ProcessSchedule

Returns the newly created ProcessSchedule object with all trigger configuration properties.

## NOTES

Triggers are folder-scoped entities. You must navigate to a folder on the Orch: drive or use -Path to specify the target folder.

The trigger is associated with a process (Release) in the target folder. The -ReleaseName must match an existing process in that folder.

For queue-based triggers, specify -QueueDefinitionName along with -ItemsActivationThreshold, -ItemsPerJobActivationTarget, and -MaxJobsForActivation to configure queue-driven activation.

## RELATED LINKS

Get-OrchTrigger

Update-OrchTrigger

Remove-OrchTrigger

Enable-OrchTrigger

Disable-OrchTrigger
