---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/New-OrchTestSetSchedule.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 05/21/2026
PlatyPS schema version: 2024-05-01
title: New-OrchTestSetSchedule
---

# New-OrchTestSetSchedule

## SYNOPSIS

Creates a new TestSet schedule in UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
New-OrchTestSetSchedule [-Name] <string[]> -TestSetName <string> [-CronExpression <string>]
 [-Description <string>] [-Enabled <string>] [-TimeZoneId <string>] [-Path <string[]>] [-WhatIf]
 [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Creates a cron-driven schedule that runs an existing TestSet. The TestSet must already exist in the target folder — the cmdlet resolves -TestSetName to a TestSetId before POSTing the schedule.

Defaults: CronExpression to every-minute, TimeZoneId to the local time zone, Enabled to true.

This cmdlet supports ShouldProcess. Use -WhatIf to preview or -Confirm to be prompted.

Primary Endpoint: POST /odata/TestSetSchedules

OAuth required scopes: OR.TestSetSchedules or OR.TestSetSchedules.Write

Required permissions: TestSetSchedules.Create

## EXAMPLES

### Example 1: Schedule a TestSet to run every minute

```powershell
PS Orch1:\Shared> New-OrchTestSetSchedule -Name DailyRun -TestSetName MyTestSet
```

Creates a schedule named "DailyRun" that fires every minute (default CronExpression) and runs the existing "MyTestSet".

### Example 2: Schedule with explicit cron expression

```powershell
PS Orch1:\Shared> New-OrchTestSetSchedule -Name HourlyRun -TestSetName MyTestSet -CronExpression '0 0 * * * ?' -TimeZoneId 'Tokyo Standard Time'
```

Creates an hourly schedule pinned to JST.

## PARAMETERS

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

### -CronExpression

Quartz cron expression. Defaults to every-minute when omitted.

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

### -Description

A free-form description stored on the entity.

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

### -Enabled

Whether the entity is enabled at creation. Accepts "true" or "false". The server defaults to true when omitted.

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

### -Name

Specifies the Name(s) of the TestSet schedule to create.

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

### -Path

Specifies the target folder(s). If not specified, the current folder is targeted. Supports wildcards.

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

### -TestSetName

Name of the existing TestSet this schedule fires.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -TimeZoneId

IANA / Windows time zone ID used to interpret -CronExpression.

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

You can pipe schedule names to this cmdlet.

### System.String[]

You can pipe schedule names to this cmdlet.

## OUTPUTS

### UiPath.PowerShell.Entities.TestSetSchedule

Returns the created TestSetSchedule entity on success.

## NOTES

Main endpoint called: POST /odata/TestSetSchedules

-TestSetName is mandatory and must reference an existing TestSet in the same folder.

## RELATED LINKS

[Get-OrchTestSetSchedule](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchTestSetSchedule.md)

[Copy-OrchTestSetSchedule](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchTestSetSchedule.md)

[Remove-OrchTestSetSchedule](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchTestSetSchedule.md)

[Enable-OrchTestSetSchedule](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Enable-OrchTestSetSchedule.md)

[Disable-OrchTestSetSchedule](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Disable-OrchTestSetSchedule.md)

[New-OrchTestSet](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/New-OrchTestSet.md)

