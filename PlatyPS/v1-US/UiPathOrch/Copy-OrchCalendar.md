---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Copy-OrchCalendar
---

# Copy-OrchCalendar

## SYNOPSIS

Copies non-working days calendars to another Orchestrator instance.

## SYNTAX

### __AllParameterSets

```
Copy-OrchCalendar [-Name] <string[]> [-Destination] <string[]> [-Path <string>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Copies non-working days calendars from one UiPath Orchestrator instance to another. The cmdlet creates new calendars on the destination drive with the same name and excluded dates as the source calendars. Cross-drive copy is supported, allowing calendars to be copied between different Orchestrator instances (e.g., from Orch1: to Orch2:).

Calendar is a tenant entity. The -Path parameter specifies the source drive, and the -Destination parameter specifies one or more destination drives.

This cmdlet supports ShouldProcess. Use -WhatIf to preview which calendars would be copied, or -Confirm to be prompted before each copy operation.

The -Name, -Destination, and -Path parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values. The -Name completion is dynamically populated from actual calendar names on the source drive. Multiple values can be specified using comma-separated text that includes wildcards.

Primary Endpoint: GET /odata/Calendars, GET /odata/Calendars({calendarId}), POST /odata/Calendars

OAuth required scopes: OR.Settings.Read (source), OR.Settings (destination)

Required permissions: (source: Settings.View, destination: Settings.Create)

## EXAMPLES

### Example 1: Copy a calendar to another Orchestrator instance

```powershell
PS Orch1:\> Copy-OrchCalendar 'Japan Holidays' Orch2:
```

Copies the calendar named "Japan Holidays" from the current drive (Orch1:) to Orch2:. Since -Name and -Destination are positional parameters (Position 0 and 1), their parameter names can be omitted.

### Example 2: Copy all calendars to another Orchestrator instance

```powershell
PS Orch1:\> Copy-OrchCalendar * Orch2:
```

Copies all calendars from Orch1: to Orch2:.

### Example 3: Copy calendars across drives using absolute paths

```powershell
PS C:\> Copy-OrchCalendar -Path Orch1: *Holiday* Orch2:
```

Copies all calendars matching "*Holiday*" from Orch1: to Orch2:. This command can be executed from any drive.

### Example 4: Preview copy with -WhatIf

```powershell
PS Orch1:\> Copy-OrchCalendar 'US Holidays' Orch2: -WhatIf
```

```output
What if: Performing the operation "Copy Calendar" on target "Item: 'US Holidays [Orch1:]' Destination: 'Orch2:\'".
```

Shows what would happen without actually copying. Use this to verify which calendars would be affected before performing the operation.

## PARAMETERS

### -Path

Specifies the source drive name.
If not specified, the current drive will be used as the source.

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

### -Destination

Specifies the destination drive names. Supports wildcards. Can reference a different Orchestrator drive (e.g., Orch2:) for cross-instance copy. Multiple destinations can be specified to copy calendars to several Orchestrator instances at once.

```yaml
Type: System.String[]
DefaultValue: None
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

### -Name

Specifies the Name of the calendars to be copied.

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

You can pipe calendar names to this cmdlet via the Name property, or destination drive names via the Destination property.

### System.String

You can pipe a source drive path to this cmdlet via the Path property.

## OUTPUTS

### UiPath.PowerShell.Entities.ExtendedCalendar

Returns the calendar objects that were copied. If a calendar with the same name already exists on the destination drive, an error is returned for that calendar.

## NOTES

Calendars are tenant-scoped entities. The -Path and -Destination parameters target drives (Orchestrator instances), not folders.

When performing cross-drive copy (e.g., Orch1: to Orch2:), the cmdlet recreates the calendar with all its excluded dates on the destination Orchestrator. The calendar's internal Id and Key are assigned by the destination server.

If a calendar with the same name already exists on the destination drive, an error is returned for that calendar.

## RELATED LINKS

Get-OrchCalendar

Add-OrchCalendarDate

Remove-OrchCalendar
