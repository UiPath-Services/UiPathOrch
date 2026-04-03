---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Remove-OrchCalendar
---

# Remove-OrchCalendar

## SYNOPSIS

Removes the calendars.

## SYNTAX

### __AllParameterSets

```
Remove-OrchCalendar [-Name] <string[]> [-Path <string[]>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Removes non-working days calendars from UiPath Orchestrator. The cmdlet deletes the entire calendar, including all its excluded dates. To remove individual dates from a calendar without deleting the calendar itself, use Remove-OrchCalendarDate instead.

Calendar is a tenant entity. Therefore, specify the drive names as the targets on this cmdlet.

This cmdlet supports ShouldProcess. Use -WhatIf to preview which calendars would be removed, or -Confirm to be prompted before each removal.

The -Name and -Path parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values. The -Name completion is dynamically populated from actual calendar names. Multiple values can be specified using comma-separated text that includes wildcards.

Primary Endpoint: DELETE /odata/Calendars({calendarId})

OAuth required scopes: OR.Settings

Required permissions: (Settings.Delete)

## EXAMPLES

### Example 1: Remove a calendar by name

```powershell
PS Orch1:\> Remove-OrchCalendar 'Old Holidays'
```

Removes the calendar named "Old Holidays" from the current Orchestrator drive. Since -Name is a positional parameter (Position 0), the parameter name can be omitted.

### Example 2: Remove calendars using a wildcard

```powershell
PS Orch1:\> Remove-OrchCalendar *2024*
```

Removes all calendars whose names match "*2024*" from the current Orchestrator drive.

### Example 3: Remove a calendar with an absolute path

```powershell
PS C:\> Remove-OrchCalendar -Path Orch1: -Name 'Deprecated Calendar'
```

Removes the calendar named "Deprecated Calendar" from the Orch1: drive. This command can be executed from any drive.

### Example 4: Preview removal with -WhatIf

```powershell
PS Orch1:\> Remove-OrchCalendar 'Japan Holidays' -WhatIf
```

```output
What if: Performing the operation "Remove Calendar" on target "'Japan Holidays [Orch1:]'".
```

Shows what would happen without actually removing the calendar. Use this to verify which calendars would be affected before performing the operation.

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

### -Name

Specifies the Name of the calendars to be removed.

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

### System.String[]

You can pipe calendar names to this cmdlet via the Name property, or drive names via the Path property.

## OUTPUTS

### None

This cmdlet does not produce output upon successful removal.

## NOTES

Calendars are tenant-scoped entities. The -Path parameter targets drives (Orchestrator instances), not folders.

This operation is irreversible. Once a calendar is removed, all its excluded dates are permanently deleted. Use -WhatIf to preview the operation before executing it.

## RELATED LINKS

Get-OrchCalendar

Remove-OrchCalendarDate

Add-OrchCalendarDate

Copy-OrchCalendar
