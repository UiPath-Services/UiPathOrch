---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchCalendarDate.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 05/11/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchCalendarDate
---

# Get-OrchCalendarDate

## SYNOPSIS

Gets the excluded dates of UiPath Orchestrator non-working days calendars.

## SYNTAX

### __AllParameterSets

```
Get-OrchCalendarDate [-Path <string[]>] [-Name] <string[]> [-CsvEncoding <Encoding>]
 [-ExportCsv <string>] [-IncludePastDate] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets the per-date excluded dates from each matched calendar. The list endpoint (`Get-OrchCalendar`) returns calendars without their excluded date payload; this cmdlet calls the per-id detail endpoint for each matched calendar and emits one `ExcludedDateNamed` row per excluded date.

`-Name` is mandatory by design — the detail path makes one API call per matched calendar, so a default "all calendars" would fan out unexpectedly on large tenants. Wildcards (including `*`) are accepted; the user just has to type the selector explicitly.

By default only dates from today onward are returned. Use `-IncludePastDate` to include past dates.

The CSV format produced by `-ExportCsv` (`Path`, `Name`, `ExcludedDate`) round-trips with `Add-OrchCalendarDate`, so an exported file can be re-imported into another tenant or the same tenant after `Remove-OrchCalendar` / re-create.

Calendar is a tenant entity. Therefore, specify the drive names as the targets on this cmdlet.

The -Name and -Path parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values. The -Name completion is dynamically populated from actual calendar names.

Primary Endpoint: GET /odata/Calendars({calendarId})

OAuth required scopes: OR.Settings or OR.Settings.Read

Required permissions:

## EXAMPLES

### Example 1: Excluded dates for all calendars on the current drive

```powershell
PS Orch1:\> Get-OrchCalendarDate -Name '*'
```

Lists every excluded date (from today onward) on every calendar in `Orch1:\`. The explicit `*` reminds the user that the operation fans out across every calendar; omitting `-Name` would prompt instead of silently fetching everything.

### Example 2: Excluded dates for a specific calendar including past dates

```powershell
PS Orch1:\> Get-OrchCalendarDate MyCalendar1 -IncludePastDate
```

Returns every excluded date on `MyCalendar1`, including dates that have already passed. `-Name` is positional (Position 0), so the parameter name can be omitted.

### Example 3: Export to CSV (round-trips with Add-OrchCalendarDate)

```powershell
PS C:\> Get-OrchCalendarDate -Path Orch1: -Name '*' -ExportCsv C:\temp
```

Exports every excluded date on `Orch1:` to `C:\temp\ExportedCalendarDates.csv`. The CSV columns (`Path`, `Name`, `ExcludedDate`) match the `Add-OrchCalendarDate` import shape — the file can be piped directly to `Import-Csv | Add-OrchCalendarDate`.

### Example 4: Filter by calendar name wildcard

```powershell
PS Orch1:\> Get-OrchCalendarDate -Name 'Holiday*'
```

Returns excluded dates only for calendars whose name starts with `Holiday`.

## PARAMETERS

### -Name

Specifies the calendar names to retrieve detail for. Mandatory by design — see DESCRIPTION. Wildcards (including `*`) are accepted.

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

### -Path

Specifies the name of the target drives. If not specified, the current drive is targeted.

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

### -IncludePastDate

Includes excluded dates that have already passed (dates before today). By default, only dates from today onward are returned.

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

Specifies the directory or file path to export excluded dates as a CSV file. If a directory path is specified, the default file name `ExportedCalendarDates.csv` is used. The CSV format (`Path`, `Name`, `ExcludedDate`) is round-trip compatible with `Add-OrchCalendarDate`.

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe calendar names to this cmdlet via the Name property, or drive names via the Path property.

## OUTPUTS

### UiPath.PowerShell.Entities.ExcludedDateNamed

One object per excluded date, with `Path`, `Name` (calendar name), `PathName` (drive + calendar name), and `ExcludedDate`.

## NOTES

Calendars are tenant-scoped entities. The -Path parameter targets drives (Orchestrator instances), not folders.

Each matched calendar triggers one detail-endpoint call. For large tenants, prefer narrow `-Name` wildcards over `*` to limit fan-out.

## RELATED LINKS

[Get-OrchCalendar](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchCalendar.md)

[Add-OrchCalendarDate](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Add-OrchCalendarDate.md)

[Remove-OrchCalendarDate](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchCalendarDate.md)
