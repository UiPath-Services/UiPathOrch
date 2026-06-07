---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Compare-OrchCalendar.md
Locale: en-US
Module Name: UiPathOrch
ms.date: 06/07/2026
PlatyPS schema version: 2024-05-01
title: Compare-OrchCalendar
---

# Compare-OrchCalendar

## SYNOPSIS

Compares calendars between two Orchestrator instances and reports the differences.

## SYNTAX

### __AllParameterSets

```
Compare-OrchCalendar [-Name] <string[]> [-DifferencePath] <string> [[-DifferenceName] <string>]
 [-Path <string>] [-LiteralPath <string>] [-Property <string[]>] [-IncludeEqual]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Compares calendars between a reference instance (-Path) and a difference instance (-DifferencePath). Calendars are tenant-level, so the reference and difference are drives, not folders, and there is no -Recurse. Calendars pair with triggers, so this is useful alongside Compare-OrchTrigger when verifying a schedule migration.

Calendars are matched by Name (not the tenant-local Id) and these properties are compared: TimeZoneId and ExcludedDates. The excluded dates are normalized to an order-independent set of dates, so only an actual change in the set of excluded days is reported.

Each result is an OrchComparison record with a SideIndicator: "<=" reference only, "=>" difference only, "<>" present on both sides but differing (with a per-property Differences breakdown), and "==" equal (only with -IncludeEqual). Without -DifferenceName each reference calendar is compared to the same-named calendar on the difference instance. With -DifferenceName, every reference calendar is compared to that single named calendar.

Primary Endpoint: GET /odata/Calendars

OAuth required scopes: OR.Administration or OR.Administration.Read (both sides)

Required permissions: Calendars.View (both sides)

## EXAMPLES

### Example 1: Verify calendars migrated between tenants

```powershell
PS C:\> Compare-OrchCalendar Orch1: Orch2:
```

Compares every calendar on Orch1 against the same-named calendar on Orch2, showing only the differences (for example a changed set of excluded dates).

### Example 2: Inspect the differences

```powershell
PS C:\> Compare-OrchCalendar Orch1: Orch2: | Where-Object SideIndicator -eq '<>' |
    Select-Object Name -ExpandProperty Differences
```

Lists only the changed calendars and expands each one's property-level differences.

## PARAMETERS

### -DifferenceName

Selects broadcast mode: every reference calendar is compared to this single named calendar in -DifferencePath, even when the names differ.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 2
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -DifferencePath

Specifies the difference (right) Orchestrator drive. Mandatory. Can be the same instance as -Path (for comparing two calendars via -DifferenceName) or a different instance.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 1
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -IncludeEqual

Also emits "==" rows for calendars that match on every compared property. Off by default.

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

### -LiteralPath

Specifies the reference drive by literal path. Its `PSPath` alias also binds the path of items piped from Get-ChildItem / Get-Item.

```yaml
Type: System.String
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

### -Name

Filters the reference calendars by name; supports wildcards. In name-match mode the same filter is applied to the difference side.

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

Specifies the reference (left) Orchestrator drive. If not specified, the current drive is used.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Property

Restricts the comparison to the named properties. Valid names: TimeZoneId, ExcludedDates. Unrecognized names are warned about and ignored.

```yaml
Type: System.String[]
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

### System.String

You can pipe the reference path to this cmdlet (the Path property).

### System.String[]

You can pipe entity names to this cmdlet (the Name property).

## OUTPUTS

### UiPath.PowerShell.Entities.OrchComparison

Returns one comparison record per calendar, with SideIndicator, Name, the single-sided Path and DifferencePath, the per-property Differences (on "<>" rows), and the underlying ReferenceObject / DifferenceObject.

## NOTES

Calendars are matched by Name, case-insensitively. The excluded dates are compared as an order-independent set. This cmdlet is read-only and does not support -WhatIf / -Confirm.

## RELATED LINKS

- [Get-OrchCalendar](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchCalendar.md)
- [Copy-OrchCalendar](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchCalendar.md)
- [Compare-OrchAsset](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Compare-OrchAsset.md)
