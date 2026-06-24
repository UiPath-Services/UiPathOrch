---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Compare-OrchRole.md
Locale: en-US
Module Name: UiPathOrch
ms.date: 06/07/2026
PlatyPS schema version: 2024-05-01
title: Compare-OrchRole
---

# Compare-OrchRole

## SYNOPSIS

Compares roles between two Orchestrator instances and reports the differences, including the permission matrix.

## SYNTAX

### __AllParameterSets

```
Compare-OrchRole [-Name] <string[]> [-DifferencePath] <string> [[-DifferenceName] <string>]
 [-Path <string>] [-LiteralPath <string>] [-Property <string[]>] [-IncludeEqual]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Compares roles between a reference instance (-Path) and a difference instance (-DifferencePath), in the spirit of Compare-Object but resolved over Orchestrator drives. Roles are tenant-level, so the reference and difference are drives (e.g. Orch1:, Orch2:), not folders, and there is no -Recurse. The primary use is verifying that the security model migrated correctly: that the same roles exist with the same granted permissions.

Roles are matched by Name (not Id, which is tenant-local) and these properties are compared: DisplayName, Type, Groups, IsStatic, IsEditable, and Permissions. The permission matrix is normalized to an order-independent set of granted "Scope:Permission" entries, so a single Permissions difference captures any grant, revoke, or scope change regardless of list order.

Each result is an OrchComparison record with a SideIndicator: "<=" reference only, "=>" difference only, "<>" present on both sides but differing (with a per-property Differences breakdown), and "==" equal (only with -IncludeEqual). Without -DifferenceName each reference role is compared to the same-named role on the difference instance. With -DifferenceName, every reference role is compared to that single named role, even when the names differ.

Primary Endpoint: GET /odata/Roles

OAuth required scopes: OR.Roles or OR.Roles.Read (both sides)

Required permissions: Roles.View (both sides)

## EXAMPLES

### Example 1: Verify roles migrated between tenants

```powershell
PS C:\> Compare-OrchRole * Orch2: -Path Orch1:
```

Compares every role on Orch1 against the same-named role on Orch2, showing only the differences. A "<>" on Permissions means the granted-permission set differs.

### Example 2: See which roles are missing on the target

```powershell
PS C:\> Compare-OrchRole * Orch2: -Path Orch1: | Where-Object SideIndicator -eq '<='
```

Lists roles that exist on Orch1 but not on Orch2.

### Example 3: Compare the permission matrix of two roles

```powershell
PS C:\> Compare-OrchRole -Path Orch1: -Name 'Automation Developer' -DifferencePath Orch1: -DifferenceName 'Automation User' |
    Select-Object Name -ExpandProperty Differences
```

Broadcast mode. Compares two differently named roles on the same tenant and expands the property-level differences, including the Permissions delta.

## PARAMETERS

### -DifferenceName

Selects broadcast mode. When set, every reference role is compared to this single named role in -DifferencePath, even when the names differ.

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

Specifies the difference (right) Orchestrator drive. This is a mandatory parameter. Can be the same instance as -Path (for comparing two roles via -DifferenceName) or a different instance.

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

Also emits "==" rows for roles that match on every compared property. Off by default.

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

Filters the reference roles by name; supports wildcards. In name-match mode the same filter is applied to the difference side.

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

Restricts the comparison to the named properties. Valid names: DisplayName, Type, Groups, IsStatic, IsEditable, Permissions. Unrecognized names are warned about and ignored.

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

Returns one comparison record per role, with SideIndicator, Name, the single-sided Path and DifferencePath, the per-property Differences (on "<>" rows), and the underlying ReferenceObject / DifferenceObject.

## NOTES

The permission matrix comparison considers only granted permissions; a non-granted permission present on one side is not a difference. Roles are matched by Name, case-insensitively. This cmdlet is read-only and does not support -WhatIf / -Confirm.

## RELATED LINKS

- [Get-OrchRole](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchRole.md)
- [Copy-OrchRole](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchRole.md)
- [Compare-OrchAsset](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Compare-OrchAsset.md)
