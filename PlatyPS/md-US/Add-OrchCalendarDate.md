---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Add-OrchCalendarDate

## SYNOPSIS
Adds dates to the Non-Working Days calendars.

## SYNTAX

```
Add-OrchCalendarDate [-Name] <String[]> [[-ExcludedDate] <DateTime[]>] [-IncludePastDate] [-Path <String[]>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The Add-OrchCalendarDate cmdlet adds specified dates to Non-Working Days calendars in UiPath Orchestrator. This cmdlet allows you to define non-working days such as holidays, maintenance periods, or other business-specific dates when automated processes should not run.

Use the -Name parameter to specify which calendars to update and the -ExcludedDate parameter to provide the dates to add as non-working days. The cmdlet supports adding multiple dates to multiple calendars simultaneously.

By default, the cmdlet prevents adding past dates to maintain calendar accuracy. Use the -IncludePastDate parameter to allow adding historical dates when needed for calendar setup or data migration scenarios.

This cmdlet operates on tenant-level calendar entities and supports wildcard patterns for calendar names, enabling bulk operations across multiple calendars.

Primary Endpoint: GET /odata/Calendars, GET /odata/Calendars({calendarId}), POST /odata/Calendar, PUT /odata/Calendars({calendarId})

OAuth required scopes: OR.Settings

Required permissions: Settings.Create Settings.Edit

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Add-OrchCalendarDate ほえほえ (Get-Date).AddDays(7)
```

Adds a date 7 days from today to the "ほえほえ" calendar as a non-working day using positional parameters.

### Example 2
```powershell
PS Orch1:\> Add-OrchCalendarDate ほえほえ "2025-12-25", "2025-12-26"
```

Adds Christmas Day and Boxing Day to the "ほえほえ" calendar as non-working days.

### Example 3
```powershell
PS Orch1:\> Add-OrchCalendarDate ほえほえ*, ふがふが* (Get-Date).AddDays(14)
```

Adds a date 14 days from today to all calendars matching "ほえほえ*" and "ふがふが*" patterns using wildcards.

### Example 4
```powershell
PS Orch1:\> Add-OrchCalendarDate ほえほえカレンダー "2025-01-01" -IncludePastDate -WhatIf
```

Shows what would happen when adding a past date to the calendar with the -IncludePastDate parameter using -WhatIf for safety.

### Example 5
```powershell
PS Orch1:\> $holidays = "2025-12-25", "2025-12-31", "2026-01-01"
PS Orch1:\> Add-OrchCalendarDate ほえほえ $holidays
```

Adds multiple holiday dates stored in a variable to the calendar.

## PARAMETERS

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

### -ExcludedDate
Specifies the non-working days to add to the calendars.

```yaml
Type: DateTime[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Name
Specifies the Name of the calendars to which the non-working days will be added.

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
Specifies the name of the target drives. If not specified, the current drive will be targeted.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -WhatIf
Shows what would happen if the cmdlet runs.
The cmdlet is not run.

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

### -IncludePastDate
Allows the addition of dates earlier than yesterday.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]
Calendar names can be piped to this cmdlet.

### System.DateTime[]
DateTime objects can be piped to this cmdlet for the -ExcludedDate parameter.

### UiPath.PowerShell.Entities.Calendar
Calendar objects from Get-OrchCalendar can be piped to this cmdlet. The Name property will be automatically mapped to the -Name parameter via ByPropertyName binding.

## OUTPUTS

### None
This cmdlet does not generate any output.

## NOTES
This cmdlet operates on tenant-level calendar entities. By default, past dates cannot be added to maintain calendar accuracy. Use the -IncludePastDate parameter when adding historical dates is necessary.

Use Get-OrchCalendar to list available calendars and verify successful date additions.

## RELATED LINKS

[Get-OrchCalendar](Get-OrchCalendar.md)

[Remove-OrchCalendarDate](Remove-OrchCalendarDate.md)

[Copy-OrchCalendar](Copy-OrchCalendar.md)
