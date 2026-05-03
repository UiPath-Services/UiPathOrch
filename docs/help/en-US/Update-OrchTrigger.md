---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Update-OrchTrigger.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Update-OrchTrigger
---

# Update-OrchTrigger

## SYNOPSIS

Updates existing triggers in UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
Update-OrchTrigger [-Name] <string[]> [-NewName <string>] [-Enabled <string>]
 [-SpecificPriorityValue <int>] [-Priority <string>] [-StartStrategy <int>] [-StopStrategy <string>]
 [-StopProcessExpression <string>] [-KillProcessExpression <string>]
 [-AlertPendingExpression <string>] [-AlertRunningExpression <string>]
 [-ConsecutiveJobFailuresThreshold <int>] [-JobFailuresGracePeriodInHours <int>]
 [-RuntimeType <string>] [-InputArguments <string>] [-ResumeOnSameContext <string>]
 [-RunAsMe <string>] [-IsConnected <string>] [-CalendarName <string>]
 [-ActivateOnJobComplete <string>] [-ItemsActivationThreshold <int>]
 [-ItemsPerJobActivationTarget <int>] [-MaxJobsForActivation <int>]
 [-StartProcessCronDetails <string>] [-StartProcessCron <string>] [-ReleaseName <string>]
 [-QueueDefinitionName <string>] [-TimeZone <string>] [-TimeZoneId <string>]
 [-StopProcessDate <datetime>] [-ExecutorRobots <string[]>] [-MachineRobots <string[]>]
 [-Path <string[]>] [-Recurse] [-Depth <uint>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Updates existing triggers (process schedules) in UiPath Orchestrator. Only the parameters that are explicitly specified are modified; all other properties are preserved from the current trigger definition. The cmdlet deep copies the existing trigger before applying changes to ensure existing values are not inadvertently lost.

The -Name parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available trigger names dynamically populated from the target folders. Multiple values can be specified using comma-separated text that includes wildcards.

This cmdlet supports CSV import via the pipeline. The importable CSV format can be obtained using `Get-OrchTrigger -Recurse -ExportCsv c:triggers.csv`, then modified and piped back to Update-OrchTrigger.

This cmdlet supports ShouldProcess. Use -WhatIf to preview which triggers would be updated, or -Confirm to be prompted before each update.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: PUT /odata/ProcessSchedules({triggerId})

OAuth required scopes: OR.Execution

Required permissions: Schedules.Edit

## EXAMPLES

### Example 1: Update a trigger schedule

```powershell
PS Orch1:\root> Update-OrchTrigger "high trigger" -StartProcessCron "0 0 8 1/1 * ? *"
```

Updates the cron schedule of the trigger named "high trigger" to run daily at 8:00 AM. All other trigger properties remain unchanged.

### Example 2: Rename a trigger

```powershell
PS Orch1:\root> Update-OrchTrigger q -NewName NewTriggerName
```

Renames the trigger from "q" to "NewTriggerName" in the current folder. All other trigger properties are preserved.

### Example 3: Update triggers using a wildcard

```powershell
PS Orch1:\root> Update-OrchTrigger *trigger* -RuntimeType NonProduction
```

Updates all triggers matching "*trigger*" in the current folder to use the NonProduction runtime type.

### Example 4: Update multiple triggers by name

```powershell
PS Orch1:\root> Update-OrchTrigger "high trigger",q -Enabled false
```

Disables two specified triggers in the current folder. Multiple trigger names can be specified as comma-separated values.

### Example 5: Import updates from CSV

```powershell
PS C:\> Import-Csv c:\triggers.csv | Update-OrchTrigger
```

Imports trigger definitions from a CSV file and updates the corresponding triggers. The CSV format matches the output of `Get-OrchTrigger -ExportCsv`. The Path column in the CSV determines the target folder for each trigger.

### Example 6: Update triggers recursively

```powershell
PS Orch1:\> Update-OrchTrigger -Recurse -Name "high trigger" -TimeZone "Tokyo Standard Time"
```

Updates the trigger named "high trigger" across all folders recursively, setting the time zone to Tokyo Standard Time.

### Example 7: Preview updates with WhatIf

```powershell
PS Orch1:\root> Update-OrchTrigger * -Enabled false -WhatIf
```

Displays what would happen if all triggers were disabled, without actually performing the update. Useful for verifying wildcard matches before execution.

## PARAMETERS

### -Path

Specifies the target folders. If not specified, the current folder is targeted. Supports wildcards and comma-separated values for multiple folders.

```yaml
Type: System.String[]
DefaultValue: None
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

### -Recurse

Includes the target folder and all its subfolders in the operation.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: False
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

### -Depth

Specifies the depth for recursion into the target folders. A depth of 0 targets only the current folder with no subfolders included. When -Depth is specified, -Recurse is implied.

```yaml
Type: System.UInt32
DefaultValue: None
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

### -ActivateOnJobComplete

Specifies whether the trigger should activate again after the triggered job completes. Tab completion suggests "true" and "false" values.

```yaml
Type: System.String
DefaultValue: None
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
DefaultValue: None
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
DefaultValue: None
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
DefaultValue: None
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
DefaultValue: False
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
DefaultValue: None
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

Specifies whether the trigger is enabled. Tab completion suggests "true" and "false" values.

```yaml
Type: System.String
DefaultValue: None
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
DefaultValue: None
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
DefaultValue: None
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
DefaultValue: None
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
DefaultValue: None
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
DefaultValue: None
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
DefaultValue: None
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
DefaultValue: None
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
DefaultValue: None
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

Specifies the names of the triggers to update. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests trigger names from the target folders. This parameter is mandatory and positional (position 0).

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
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

### -NewName

Specifies a new name for the trigger. Use this parameter to rename an existing trigger. Only a single trigger can be renamed at a time (do not use wildcards with -Name when renaming).

```yaml
Type: System.String
DefaultValue: None
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

### -Priority

Specifies the job priority for triggered jobs. Tab completion suggests available priority values.

```yaml
Type: System.String
DefaultValue: None
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
DefaultValue: None
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

Specifies the name of the process (Release) to associate with the trigger. Supports wildcards. Tab completion dynamically suggests available process names from the target folder. Use this to change the process associated with an existing trigger.

```yaml
Type: System.String
DefaultValue: None
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

### -ResumeOnSameContext

Specifies whether the job should resume on the same execution context. Tab completion suggests "true" and "false" values.

```yaml
Type: System.String
DefaultValue: None
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

Specifies whether the triggered job should run under the credentials of the user who created the trigger. Tab completion suggests "true" and "false" values.

```yaml
Type: System.String
DefaultValue: None
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

Specifies the runtime type for triggered jobs. Tab completion suggests available runtime types.

```yaml
Type: System.String
DefaultValue: None
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

Specifies the Quartz cron expression that defines the trigger schedule.

```yaml
Type: System.String
DefaultValue: None
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
DefaultValue: None
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

Specifies the start strategy for the trigger. The start strategy defines how jobs are created when the trigger fires (e.g., how many jobs to create per trigger activation).

```yaml
Type: System.Nullable`1[System.Int32]
DefaultValue: None
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
DefaultValue: None
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
DefaultValue: None
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
DefaultValue: None
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
DefaultValue: None
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

Shows what would happen if the cmdlet runs. The cmdlet is not run.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: False
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

You can pipe string array property values (such as Name, ExecutorRobots, MachineRobots) to this cmdlet via property name binding. CSV import rows are bound by property name including Path, Name, and all trigger configuration properties.

## OUTPUTS

### UiPath.PowerShell.Entities.ProcessSchedule

Returns the updated ProcessSchedule object with all trigger configuration properties.

## NOTES

Triggers are folder-scoped entities. You must navigate to a folder on the Orch: drive or use -Path to specify target folders.

Only explicitly specified parameters are modified. The cmdlet deep copies the current trigger definition before applying changes, ensuring that unspecified properties retain their existing values.

This cmdlet supports CSV import via the pipeline. Export triggers using `Get-OrchTrigger -Recurse -ExportCsv c:triggers.csv`, modify the CSV file, then import using `Import-Csv c:\triggers.csv | Update-OrchTrigger`. The Path column in the CSV determines the target folder for each trigger.

## RELATED LINKS

[Get-OrchTrigger](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchTrigger.md)

[New-OrchTrigger](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/New-OrchTrigger.md)

[Remove-OrchTrigger](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchTrigger.md)

[Enable-OrchTrigger](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Enable-OrchTrigger.md)

[Disable-OrchTrigger](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Disable-OrchTrigger.md)
