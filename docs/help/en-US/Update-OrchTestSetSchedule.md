---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Update-OrchTestSetSchedule.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 05/22/2026
PlatyPS schema version: 2024-05-01
title: Update-OrchTestSetSchedule
---

# Update-OrchTestSetSchedule

## SYNOPSIS

Updates one or more existing TestSet schedules in UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
Update-OrchTestSetSchedule [-Path <string[]>] [-LiteralPath <string[]>] [-Recurse]
 [-Depth <uint>] [-Name] <string[]> [-CalendarName <string>] [-Confirm]
 [-CronExpression <string>] [-Description <string>] [-Enabled <string>]
 [-NewName <string>] [-TestSetName <string>] [-TimeZoneId <string>] [-WhatIf]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Updates fields on existing TestSet schedules in the target folder. Schedules are matched by -Name (wildcards supported). Each non-null parameter overrides the corresponding field; unspecified parameters keep their existing value. Uses dirty-detection: if every passed-in value already matches the server-side state, the PUT is skipped.

Use -NewName to rename a schedule. -TestSetName / -CalendarName are resolved to their Ids at submit time.

Parameter surface mirrors New-OrchTestSetSchedule, so a CSV emitted by `Get-OrchTestSetSchedule -ExportCsv` re-imports into either cmdlet.

NOTE — Unverified, behavior not guaranteed: TestSet schedule create/modify has never been observed to succeed against a live tenant. Every tenant tested (including ones with the Testing feature enabled and test cases / test sets present) rejects it with `errorCode 3234`, "Test set schedule creation and modification is not allowed for this tenant." The restriction is tenant-level and independent of the Testing toggle. The cmdlet sends a well-formed PUT and surfaces the server's response, but its success path is untested.

This cmdlet supports ShouldProcess. Use -WhatIf to preview or -Confirm to be prompted.

Primary Endpoint: PUT /odata/TestSetSchedules({id})

OAuth required scopes: OR.TestSetSchedules or OR.TestSetSchedules.Write

Required permissions: TestSetSchedules.Edit

## EXAMPLES

### Example 1: Disable a schedule

```powershell
PS Orch1:\Shared> Update-OrchTestSetSchedule DailyRun -Enabled false
```

### Example 2: Change the cron expression and time zone

```powershell
PS Orch1:\Shared> Update-OrchTestSetSchedule DailyRun -CronExpression '0 0 9 * * ?' -TimeZoneId 'Tokyo Standard Time'
```

### Example 3: Re-import a previously exported CSV

```powershell
PS C:\> Import-Csv schedules.csv | Update-OrchTestSetSchedule
```

Reapplies the state captured by `Get-OrchTestSetSchedule -ExportCsv`. Only listed schedules are touched; dirty-detection skips rows whose values are unchanged.

## PARAMETERS

### -CalendarName

Name of a calendar that restricts when the schedule may fire. Resolved to CalendarId at submit time.

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

### -CronExpression

Quartz cron expression controlling when the TestSet runs. Defaults to every-minute when omitted.

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

### -Depth

Depth for recursion into the target folders. 0 means the current location only.

```yaml
Type: System.UInt32
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

Whether the entity is enabled. Accepts "true" or "false".

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

Specifies the Name(s) of the entity. For Update-, wildcards match against existing entities in the target.

```yaml
Type: System.String[]
DefaultValue: ''
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

New name to rename the entity to. The lookup is by -Name; -NewName is the post-update name.

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

### -Path

Specifies the target folder(s). TestSet schedules are folder-scoped. Supports wildcards.

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

### -LiteralPath

Specifies the target folder or drive by literal path -- wildcard metacharacters (`[`, `]`, `*`, `?`) are treated as literal characters rather than patterns. Accepts the same drive-qualified paths as -Path. Its `PSPath` alias also binds the path of items piped from Get-ChildItem / Get-Item, so you can pipe folders directly. Use -LiteralPath instead of -Path when a folder name contains a wildcard metacharacter.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: false
Aliases:
- PSPath
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

### -TestSetName

Name of the existing TestSet this schedule runs. Resolved to TestSetId at submit time.

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

### -TimeZoneId

IANA / Windows time zone Id (e.g. "Tokyo Standard Time") used to interpret -CronExpression. Tab-completes to valid Ids.

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

You can pipe values to this cmdlet by property name.

### System.String[]

You can pipe names to this cmdlet.

## OUTPUTS

### UiPath.PowerShell.Entities.TestSetSchedule

Returns the updated TestSetSchedule entity on success (re-fetched after the PUT).

## NOTES

Main endpoint called: PUT /odata/TestSetSchedules({id})

The update body is the existing schedule (deep-copied) plus parameter overrides. -TestSetName / -CalendarName resolve to TestSetId / CalendarId. See the disclaimer above: create/modify is rejected with `errorCode 3234` on every tenant tested, so this cmdlet's success path is unverified and not guaranteed.

## RELATED LINKS

[Get-OrchTestSetSchedule](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchTestSetSchedule.md)

[New-OrchTestSetSchedule](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/New-OrchTestSetSchedule.md)

[Copy-OrchTestSetSchedule](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchTestSetSchedule.md)

[Remove-OrchTestSetSchedule](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchTestSetSchedule.md)

[Enable-OrchTestSetSchedule](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Enable-OrchTestSetSchedule.md)

[Disable-OrchTestSetSchedule](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Disable-OrchTestSetSchedule.md)

