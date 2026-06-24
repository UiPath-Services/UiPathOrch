---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Add-OrchCalendarDate.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Add-OrchCalendarDate
---

# Add-OrchCalendarDate

## SYNOPSIS

Adds dates to the Non-Working Days calendars.

## SYNTAX

### __AllParameterSets

```
Add-OrchCalendarDate [-Path <string[]>] [-LiteralPath <string[]>] [-Name] <string[]> [[-ExcludedDate] <datetime[]>]
 [-Confirm] [-IncludePastDate] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Adds non-working days (holidays or non-business days) to the specified calendars in UiPath Orchestrator. If the specified calendar name does not exist, a new calendar is created with the given excluded dates. If the calendar already exists, the specified dates are merged with the existing excluded dates.

Calendar is a tenant entity. Therefore, specify the drive names as the targets on this cmdlet.

By default, only dates from today onward are accepted. Dates earlier than today are silently ignored unless -IncludePastDate is specified.

Duplicate dates are automatically deduplicated. If the resulting date list is unchanged from the existing calendar, the API call is skipped.

This cmdlet supports ShouldProcess. Use -WhatIf to preview which calendars would be created or updated, or -Confirm to be prompted before each operation.

The -Name, -ExcludedDate, and -Path parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values. The -Name completion is dynamically populated from actual calendar names. The -ExcludedDate completion suggests dates one week after the last specified date.

Primary Endpoint: GET /odata/Calendars, GET /odata/Calendars({calendarId}), POST /odata/Calendars, PUT /odata/Calendars({calendarId})

OAuth required scopes: OR.Settings or OR.Settings.Write

Required permissions: Settings.Create Settings.Edit

## EXAMPLES

### Example 1: Add a holiday to a calendar

```powershell
PS Orch1:\> Add-OrchCalendarDate 'Japan Holidays' 2026-05-05
```

Adds May 5, 2026 as a non-working day to the calendar named "Japan Holidays". Since -Name and -ExcludedDate are positional parameters (Position 0 and 1), their parameter names can be omitted.

### Example 2: Add multiple dates to a calendar

```powershell
PS Orch1:\> Add-OrchCalendarDate 'US Holidays' 2026-07-04,2026-09-07,2026-11-26
```

Adds three dates (July 4, September 7, and November 26, 2026) as non-working days to the "US Holidays" calendar.

### Example 3: Create a new calendar with dates

```powershell
PS Orch1:\> Add-OrchCalendarDate 'New Year 2027' 2027-01-01,2027-01-02,2027-01-03
```

If the calendar "New Year 2027" does not exist, it is created with the specified excluded dates. If it already exists, the dates are merged with the existing ones.

### Example 4: Add a past date using -IncludePastDate

```powershell
PS C:\> Add-OrchCalendarDate -Path Orch1: 'Company Holidays' -ExcludedDate 2025-12-25 -IncludePastDate
```

Adds December 25, 2025 to the "Company Holidays" calendar, even though the date is in the past. Without -IncludePastDate, past dates are silently ignored.

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
  ValueFromPipelineByPropertyName: false
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
  ValueFromPipelineByPropertyName: false
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

Specifies the non-working days to add to the calendars.

```yaml
Type: System.DateTime[]
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 1
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -IncludePastDate

Allows the addition of dates earlier than today.

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

### -Name

Specifies the Name of the calendars to which the non-working days will be added.

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

You can pipe excluded dates to this cmdlet via the ExcludedDate property.

### System.String[]

You can pipe multiple calendar names to this cmdlet via the Name property.

### System.DateTime[]

You can pipe multiple excluded dates to this cmdlet via the ExcludedDate property.

## OUTPUTS

### None

This cmdlet does not produce output. The calendar is created or updated silently.

## NOTES

Calendars are tenant-scoped entities. The -Path parameter targets drives (Orchestrator instances), not folders.

When a calendar name that does not match any existing calendar is specified and the name does not contain wildcard characters, a new calendar is created. If the name contains wildcards and does not match any existing calendar, it is silently skipped.

## RELATED LINKS

[Get-OrchCalendar](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchCalendar.md)

[Remove-OrchCalendarDate](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchCalendarDate.md)

[Remove-OrchCalendar](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchCalendar.md)

[Copy-OrchCalendar](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchCalendar.md)
