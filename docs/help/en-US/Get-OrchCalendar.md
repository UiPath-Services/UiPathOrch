---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchCalendar.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchCalendar
---

# Get-OrchCalendar

## SYNOPSIS

Gets the non-working days calendars.

## SYNTAX

### __AllParameterSets

```
Get-OrchCalendar [[-Name] <string[]>] [-ExpandExcludedDate] [-IncludePastDate] [-Path <string[]>]
 [-ExportCsv <string>] [-CsvEncoding <Encoding>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets the non-working days calendars from UiPath Orchestrator. By default, the cmdlet returns calendar objects with summary information. Use the -ExpandExcludedDate parameter to expand and list the individual excluded dates (non-business days or holidays) within each calendar.

Calendar is a tenant entity. Therefore, specify the drive names as the targets on this cmdlet.

When -ExpandExcludedDate is specified, by default only dates from today onward are returned. Use -IncludePastDate to include dates that have already passed.

The -Name and -Path parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values. The -Name completion is dynamically populated from actual calendar names. Multiple values can be specified using comma-separated text that includes wildcards.

Primary Endpoint: GET /odata/Calendars, GET /odata/Calendars({calendarId})

OAuth required scopes: OR.Settings or OR.Settings.Read

Required permissions:

## EXAMPLES

### Example 1: Get all calendars

```powershell
PS Orch1:\> Get-OrchCalendar
```

Gets all non-working days calendars on the current Orchestrator drive.

### Example 2: Get a specific calendar by name

```powershell
PS Orch1:\> Get-OrchCalendar MyCalendar1
```

Gets the calendar named "MyCalendar1". Since -Name is a positional parameter (Position 0), the parameter name can be omitted.

### Example 3: Get calendars with expanded excluded dates

```powershell
PS Orch1:\> Get-OrchCalendar -ExpandExcludedDate
```

Gets all calendars and expands each one to list the individual excluded dates. Only dates from today onward are returned by default.

### Example 4: Get calendars with all excluded dates including past dates

```powershell
PS Orch1:\> Get-OrchCalendar -ExpandExcludedDate -IncludePastDate
```

Gets all calendars and lists all excluded dates, including dates that have already passed.

### Example 5: Export calendars to CSV

```powershell
PS C:\> Get-OrchCalendar -Path Orch1: -ExportCsv C:\temp
```

Exports the excluded dates of all calendars on Orch1: to a CSV file in the C:\temp directory. The -ExportCsv parameter accepts a directory path, and the CSV file is automatically named ExportedCalendars.csv. The CSV contains columns for Path, Name, and ExcludedDate.

### Example 6: Get calendars using wildcards across multiple drives

```powershell
PS C:\> Get-OrchCalendar -Path Orch1: *Calendar*
```

Gets all calendars matching `*Calendar*` from the Orch1 drive.

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

### -CsvEncoding

Specifies the encoding for CSV export. Default is UTF-8 with BOM for Excel compatibility. Tab completion suggests all available system encodings (e.g., utf-8, shift_jis, us-ascii).

```yaml
Type: System.Text.Encoding
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

### -ExpandExcludedDate

Expands each calendar to list the individual excluded dates (non-business days or holidays). When this parameter is specified, the output type changes from ExtendedCalendar to ExcludedDateNamed, providing the calendar name and each excluded date as separate objects.

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

### -ExportCsv

Specifies the directory or file path to export the calendar data as a CSV file. If a directory path is specified, the default file name "ExportedCalendars.csv" is used. When this parameter is specified, the excluded dates are written to the CSV file instead of being output to the pipeline.

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

### -IncludePastDate

Includes excluded dates that have already passed (dates before today) in the output. By default, only dates from today onward are returned when -ExpandExcludedDate or -ExportCsv is used.

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

Specifies the Name of the calendars to be retrieved.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: (All)
  Position: 0
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
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

### UiPath.PowerShell.Entities.ExtendedCalendar

Returns calendar objects when -ExpandExcludedDate is not specified.

### UiPath.PowerShell.Entities.ExcludedDateNamed

Returns individual excluded date objects when -ExpandExcludedDate is specified.

## NOTES

Calendars are tenant-scoped entities. The -Path parameter targets drives (Orchestrator instances), not folders.

## RELATED LINKS

[Add-OrchCalendarDate](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Add-OrchCalendarDate.md)

[Remove-OrchCalendarDate](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchCalendarDate.md)

[Remove-OrchCalendar](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchCalendar.md)

[Copy-OrchCalendar](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchCalendar.md)
