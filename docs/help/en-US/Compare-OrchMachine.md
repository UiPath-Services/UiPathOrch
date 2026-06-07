---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Compare-OrchMachine.md
Locale: en-US
Module Name: UiPathOrch
ms.date: 06/07/2026
PlatyPS schema version: 2024-05-01
title: Compare-OrchMachine
---

# Compare-OrchMachine

## SYNOPSIS

Compares machines between two Orchestrator instances and reports the differences.

## SYNTAX

### __AllParameterSets

```
Compare-OrchMachine [-Name] <string[]> [-DifferencePath] <string> [[-DifferenceName] <string>]
 [-Path <string>] [-LiteralPath <string>] [-Property <string[]>] [-IncludeEqual]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Compares machines between a reference instance (-Path) and a difference instance (-DifferencePath). Machines are tenant-level, so the reference and difference are drives (e.g. Orch1:, Orch2:), not folders, and there is no -Recurse. The primary use is migration readiness: machines must exist on the target with matching runtime capacity for jobs to run.

Machines are matched by Name (not the tenant-local Id) and these properties are compared: Description, Type, Scope, NonProductionSlots, UnattendedSlots, HeadlessSlots, TestAutomationSlots, AutomationCloudSlots, AutomationCloudTestAutomationSlots, AutomationType, TargetFramework, and Tags. The license key and client secret are not compared.

Each result is an OrchComparison record with a SideIndicator: "<=" reference only, "=>" difference only, "<>" present on both sides but differing (with a per-property Differences breakdown), and "==" equal (only with -IncludeEqual). Without -DifferenceName each reference machine is compared to the same-named machine on the difference instance. With -DifferenceName, every reference machine is compared to that single named machine.

Primary Endpoint: GET /odata/Machines

OAuth required scopes: OR.Machines or OR.Machines.Read (both sides)

Required permissions: Machines.View (both sides)

## EXAMPLES

### Example 1: Verify machines exist on the target with matching capacity

```powershell
PS C:\> Compare-OrchMachine Orch1: Orch2:
```

Compares every machine on Orch1 against the same-named machine on Orch2, showing only the differences. A "<=" means the machine is missing on Orch2.

### Example 2: Check only slot capacity

```powershell
PS C:\> Compare-OrchMachine Orch1: Orch2: -Property UnattendedSlots,HeadlessSlots,TestAutomationSlots
```

Restricts the comparison to the runtime slot counts.

## PARAMETERS

### -DifferenceName

Selects broadcast mode: every reference machine is compared to this single named machine in -DifferencePath, even when the names differ.

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

Specifies the difference (right) Orchestrator drive. Mandatory. Can be the same instance as -Path (for comparing two machines via -DifferenceName) or a different instance.

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

Also emits "==" rows for machines that match on every compared property. Off by default.

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

Filters the reference machines by name; supports wildcards. In name-match mode the same filter is applied to the difference side.

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

Restricts the comparison to the named properties. Valid names: Description, Type, Scope, NonProductionSlots, UnattendedSlots, HeadlessSlots, TestAutomationSlots, AutomationCloudSlots, AutomationCloudTestAutomationSlots, AutomationType, TargetFramework, Tags. Unrecognized names are warned about and ignored.

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

Returns one comparison record per machine, with SideIndicator, Name, the single-sided Path and DifferencePath, the per-property Differences (on "<>" rows), and the underlying ReferenceObject / DifferenceObject.

## NOTES

Machines are matched by Name, case-insensitively. This cmdlet is read-only and does not support -WhatIf / -Confirm.

## RELATED LINKS

- [Get-OrchMachine](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchMachine.md)
- [Copy-OrchMachine](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchMachine.md)
- [Compare-OrchAsset](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Compare-OrchAsset.md)
