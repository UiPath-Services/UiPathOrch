---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchCalendarDate.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Remove-OrchCalendarDate
---

# Remove-OrchCalendarDate

## SYNOPSIS

Removes dates from the Non-Working Days calendars.

## SYNTAX

### __AllParameterSets

```
Remove-OrchCalendarDate [-Path <string[]>] [-Name] <string[]> [-ExcludedDate] <datetime[]>
 [-Confirm] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Removes specific non-working days (holidays or non-business days) from the specified calendars in UiPath Orchestrator. Unlike Remove-OrchCalendar, which deletes the entire calendar, this cmdlet removes only the specified dates while keeping the calendar intact.

Calendar is a tenant entity. Therefore, specify the drive names as the targets on this cmdlet.

The cmdlet retrieves the current excluded dates from the calendar, removes the specified dates from the list, and updates the calendar with the remaining dates. If the resulting date list is unchanged (i.e., the specified dates were not in the calendar), the API call is skipped.

This cmdlet supports ShouldProcess. Use -WhatIf to preview which calendars would be updated, or -Confirm to be prompted before each update.

The -Name, -ExcludedDate, and -Path parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values. The -Name completion is dynamically populated from actual calendar names. The -ExcludedDate completion suggests dates that exist in the selected calendar (from today onward), excluding dates already specified on the command line.

Primary Endpoint: GET /odata/Calendars, GET /odata/Calendars({calendarId}), PUT /odata/Calendars({calendarId})

OAuth required scopes: OR.Settings

Required permissions: Settings.Create Settings.Edit

## EXAMPLES

### Example 1: Remove a date from a calendar

```powershell
PS Orch1:\> Remove-OrchCalendarDate 'Japan Holidays' 2026-05-05
```

Removes May 5, 2026 from the "Japan Holidays" calendar. Since -Name and -ExcludedDate are positional parameters (Position 0 and 1), their parameter names can be omitted.

### Example 2: Remove multiple dates from a calendar

```powershell
PS Orch1:\> Remove-OrchCalendarDate 'US Holidays' 2026-07-04,2026-09-07
```

Removes July 4 and September 7, 2026 from the "US Holidays" calendar.

### Example 3: Remove dates from a calendar on a specific drive

```powershell
PS C:\> Remove-OrchCalendarDate -Path Orch1: 'Company Holidays' -ExcludedDate 2026-12-25
```

Removes December 25, 2026 from the "Company Holidays" calendar on the Orch1: drive. This command can be executed from any drive.

### Example 4: Preview removal with -WhatIf

```powershell
PS Orch1:\> Remove-OrchCalendarDate 'Japan Holidays' 2026-01-01 -WhatIf
```

```output
What if: Performing the operation "Update Calendar" on target "'Japan Holidays [Orch1:]'".
```

Shows what would happen without actually modifying the calendar. Use this to verify which calendars would be affected before performing the operation.

## PARAMETERS

### -Path

Specifies the name of the target drives.
If not specified, the current drive is targeted.

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

### -ExcludedDate

Specifies the non-working days to remove from the calendars.

```yaml
Type: System.DateTime[]
DefaultValue: None
SupportsWildcards: false
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

### -Name

Specifies the Name of the calendars from which the non-working days will be removed.

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

### -WhatIf

Shows what would happen if the cmdlet runs.
The cmdlet is not run.

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

You can pipe a calendar name to this cmdlet via the Name property.

### System.DateTime

You can pipe an excluded date to this cmdlet via the ExcludedDate property.

### System.String[]

You can pipe multiple calendar names to this cmdlet via the Name property.

### System.DateTime[]

You can pipe multiple excluded dates to this cmdlet via the ExcludedDate property.

## OUTPUTS

### None

This cmdlet does not produce output. The calendar is updated silently.

## NOTES

Calendars are tenant-scoped entities. The -Path parameter targets drives (Orchestrator instances), not folders.

If the specified dates do not exist in the calendar, the calendar is left unchanged and no API call is made.

## RELATED LINKS

[Get-OrchCalendar](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchCalendar.md)

[Add-OrchCalendarDate](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Add-OrchCalendarDate.md)

[Remove-OrchCalendar](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchCalendar.md)

[Copy-OrchCalendar](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchCalendar.md)
