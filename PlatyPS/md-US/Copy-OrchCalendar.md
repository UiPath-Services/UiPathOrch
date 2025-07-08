---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Copy-OrchCalendar

## SYNOPSIS
Copies calendars between tenants.

## SYNTAX

```
Copy-OrchCalendar [-Name] <String[]> [-Destination] <String[]> [-Path <String>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The Copy-OrchCalendar cmdlet copies calendars from source tenants to destination tenants within UiPath Orchestrator. This cmdlet creates complete copies of calendars, including their configurations, excluded dates, and metadata.

The cmdlet supports copying calendars to multiple destination tenants simultaneously. Calendars contain business schedule information used for trigger scheduling and process automation timing.

Use the -Name parameter to specify which calendars to copy and the -Destination parameter to specify the target tenants. The -Path parameter allows you to specify the source tenant when working with multiple Orchestrator instances.

This is a tenant entity cmdlet. The -Path parameter specifies the source drive name (e.g., Orch1:, Orch2:), and -Destination specifies the target tenant drives where calendars should be copied.

Primary Endpoint: GET /odata/Calendars, GET /odata/Calendars({calendarId}), POST /odata/Calendars

OAuth required scopes: OR.Settings

Required permissions: Settings.View, Settings.Create

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Copy-OrchCalendar BusinessHours Orch2:
```

Copies the BusinessHours calendar from the current tenant (Orch1) to Orch2 tenant.

### Example 2
```powershell
PS C:\> Copy-OrchCalendar -Path Orch1: HolidaySchedule Orch2:, Orch3:
```

Copies the HolidaySchedule calendar from Orch1 to both Orch2 and Orch3 tenants.

### Example 3
```powershell
PS Orch1:\> Copy-OrchCalendar BusinessHours, HolidaySchedule Orch2: -WhatIf
```

Shows what would happen when copying BusinessHours and HolidaySchedule calendars from the current tenant to Orch2.

### Example 4
```powershell
PS C:\> Copy-OrchCalendar -Path Orch1: *Schedule* Orch2:
```

Copies all calendars containing Schedule in their name from Orch1 to Orch2 using wildcards.

### Example 5
```powershell
PS Orch1:\> Get-OrchCalendar *Business* | Copy-OrchCalendar -Destination Orch2:, Orch3:
```

Gets all calendars containing Business in their names and copies them to both Orch2 and Orch3 tenants.

### Example 6
```powershell
PS C:\> Copy-OrchCalendar -Path Orch1: WorkingDays Orch2: -Confirm
```

Copies the WorkingDays calendar from Orch1 to Orch2 with confirmation prompts.

## PARAMETERS

### -Destination
Specifies the destination tenant drives where calendars should be copied.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Name
Specifies the Name of the calendars to be copied.

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
Specifies the source tenant drive. If not specified, the current tenant will be used as the source.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]
Calendar names can be piped to this cmdlet.

### UiPath.PowerShell.Entities.Calendar
Calendar objects from Get-OrchCalendar can be piped to this cmdlet. The Name property will be automatically mapped to the -Name parameter via ByPropertyName binding.

## OUTPUTS

### None
This cmdlet does not generate any output.

## NOTES
This is a tenant entity cmdlet. The -Path parameter specifies drive names (e.g., Orch1:, Orch2:) for source and destination tenants.

Use wildcards for efficient bulk operations and -WhatIf for testing before actual execution. Calendars include business schedules and excluded dates information.

## RELATED LINKS

[Get-OrchCalendar](Get-OrchCalendar.md)

[Set-OrchCalendar](Set-OrchCalendar.md)

[Remove-OrchCalendar](Remove-OrchCalendar.md)
