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
Get-OrchCalendar [-Path <string[]>] [[-Name] <string[]>] [-CsvEncoding <Encoding>]
 [-ExpandExcludedDate] [-ExportCsv <string>] [-IncludePastDate] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets the non-working days calendars from UiPath Orchestrator. The cmdlet returns calendar objects with summary information (Name, Id) from the list endpoint.

> **Deprecated:** `-ExpandExcludedDate` and `-ExportCsv` on this cmdlet are deprecated and will be removed in a future major release. Both options have been moved to the new `Get-OrchCalendarDate` cmdlet, which fetches each calendar's detail and emits one row per excluded date. Existing CSV files exported via `-ExportCsv` keep importing into `Add-OrchCalendarDate` because the on-disk format is unchanged.

Calendar is a tenant entity. Therefore, specify the drive names as the targets on this cmdlet.

The -Name and -Path parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values. The -Name completion is dynamically populated from actual calendar names. Multiple values can be specified using comma-separated text that includes wildcards.

Primary Endpoint: GET /odata/Calendars

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

### Example 3: Get calendars using wildcards across multiple drives

```powershell
PS C:\> Get-OrchCalendar -Path Orch1: *Calendar*
```

Gets all calendars matching `*Calendar*` from the Orch1 drive.

### Example 4 (deprecated): -ExpandExcludedDate / -ExportCsv

```powershell
PS Orch1:\> Get-OrchCalendar -ExpandExcludedDate -IncludePastDate
PS C:\>     Get-OrchCalendar -Path Orch1: -ExportCsv C:\temp
```

Both invocations emit a deprecation warning and route through the new `Get-OrchCalendarDate` cmdlet. Use `Get-OrchCalendarDate -Name '*' -IncludePastDate` or `Get-OrchCalendarDate -Name '*' -ExportCsv C:\temp` directly instead.

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

**Deprecated.** Routes to `Get-OrchCalendarDate` and emits a deprecation warning. Use `Get-OrchCalendarDate` directly. When present, the output type becomes `ExcludedDateNamed` (one row per excluded date) instead of `ExtendedCalendar`.

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

**Deprecated.** Routes to `Get-OrchCalendarDate -ExportCsv` and emits a deprecation warning. The on-disk CSV format is unchanged, so files exported here still import into `Add-OrchCalendarDate`. Use `Get-OrchCalendarDate -ExportCsv` directly. If a directory path is specified, the default file name "ExportedCalendars.csv" is used.

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

(Deprecated path only.) Includes excluded dates that have already passed (dates before today). Used in combination with the deprecated `-ExpandExcludedDate` / `-ExportCsv` paths. Has no effect on the default list output.

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

[Get-OrchCalendarDate](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchCalendarDate.md)

[Add-OrchCalendarDate](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Add-OrchCalendarDate.md)

[Remove-OrchCalendarDate](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchCalendarDate.md)

[Remove-OrchCalendar](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchCalendar.md)

[Copy-OrchCalendar](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchCalendar.md)
