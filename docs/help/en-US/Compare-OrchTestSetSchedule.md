---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Compare-OrchTestSetSchedule.md
Locale: en-US
Module Name: UiPathOrch
ms.date: 06/07/2026
PlatyPS schema version: 2024-05-01
title: Compare-OrchTestSetSchedule
---

# Compare-OrchTestSetSchedule

## SYNOPSIS

Compares test set schedules between two folders or Orchestrator instances and reports the differences.

## SYNTAX

### __AllParameterSets

```
Compare-OrchTestSetSchedule [-Name] <string[]> [-DifferencePath] <string>
 [[-DifferenceName] <string>] [-Path <string>] [-LiteralPath <string>] [-Property <string[]>]
 [-Recurse] [-Depth <uint>] [-IncludeEqual] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Compares test set schedules between a reference location (-Path, or the current folder) and a difference location (-DifferencePath), resolved over Orchestrator drives and folders. The primary use is migration verification.

Schedules are matched by Name (not the tenant-local Id) and these properties are compared: Enabled, TestSetName (the target test set, by name), CronExpression, TimeZoneId, and CalendarName. The numeric TestSetId / CalendarId are tenant-local and are not compared; the target test set and calendar are compared by name instead.

Each result is an OrchComparison record with a SideIndicator: "<=" reference only, "=>" difference only, "<>" present on both sides but differing (with a per-property Differences breakdown), and "==" equal (only with -IncludeEqual). Without -DifferenceName each reference schedule is compared to the same-named schedule in the corresponding difference folder (mirrored relative path with -Recurse). With -DifferenceName, every reference schedule is compared to that single named schedule.

Primary Endpoint: GET /odata/TestSetSchedules

OAuth required scopes: OR.TestSetSchedules or OR.TestSetSchedules.Read (both sides)

Required permissions: TestSetSchedules.View (both sides)

## EXAMPLES

### Example 1: Verify test schedules migrated to another tenant

```powershell
PS C:\> Compare-OrchTestSetSchedule * Orch2:\Finance -Path Orch1:\Finance
```

Compares every test set schedule in Finance on Orch1 against the same-named schedule in Finance on Orch2, showing only the differences (for example a changed cron).

### Example 2: Compare a whole tree

```powershell
PS C:\> Compare-OrchTestSetSchedule * Orch2:\Shared -Path Orch1:\Shared -Recurse
```

Walks Shared and all subfolders and reports the differing schedules.

## PARAMETERS

### -Depth

Specifies the depth for recursion into the reference folders. A depth of 0 targets only the reference folder. When -Depth is specified, -Recurse is implied.

```yaml
Type: System.UInt32
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

### -DifferenceName

Selects broadcast mode: every reference schedule is compared to this single named schedule in -DifferencePath, even when the names differ.

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

Specifies the difference (right) folder. Mandatory. Can be on the same instance or a different instance.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: true
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

Also emits "==" rows for schedules that match on every compared property. Off by default.

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

Specifies the reference folder by literal path. Its `PSPath` alias also binds the path of items piped from Get-ChildItem / Get-Item.

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

Filters the reference schedules by name; supports wildcards. In name-match mode the same filter is applied to the difference side.

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

Specifies the reference (left) folder. If not specified, the current folder is used.

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

### -Property

Restricts the comparison to the named properties. Valid names: Enabled, TestSetName, CronExpression, TimeZoneId, CalendarName. Unrecognized names are warned about and ignored.

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

### -Recurse

Includes the reference folder and all its subfolders. Each is compared against the mirrored relative folder under -DifferencePath.

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

Returns one comparison record per schedule, with SideIndicator, Name, the single-sided Path and DifferencePath, the per-property Differences (on "<>" rows), and the underlying ReferenceObject / DifferenceObject.

## NOTES

Schedules are matched by Name, case-insensitively. The target test set and calendar are compared by name (the numeric ids are tenant-local). This cmdlet is read-only and does not support -WhatIf / -Confirm.

## RELATED LINKS

- [Get-OrchTestSetSchedule](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchTestSetSchedule.md)
- [Compare-OrchTestSet](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Compare-OrchTestSet.md)
- [Compare-OrchAsset](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Compare-OrchAsset.md)
