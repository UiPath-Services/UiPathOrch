---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Compare-OrchFolderUser.md
Locale: en-US
Module Name: UiPathOrch
ms.date: 06/07/2026
PlatyPS schema version: 2024-05-01
title: Compare-OrchFolderUser
---

# Compare-OrchFolderUser

## SYNOPSIS

Compares folder user assignments between two folders or Orchestrator instances and reports the differences.

## SYNTAX

### __AllParameterSets

```
Compare-OrchFolderUser [-Name] <string[]> [-DifferencePath] <string> [[-DifferenceName] <string>]
 [-Path <string>] [-LiteralPath <string>] [-Property <string[]>] [-Recurse] [-Depth <uint>]
 [-IncludeEqual] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Compares the user assignments of a reference folder (-Path, or the current folder) against those of a difference folder (-DifferencePath). The primary use is access-migration verification: confirm the right users have the right roles in each folder after a copy between folders or tenants.

Assignments are matched by user name and these properties are compared: the user Type and the assigned Roles (compared as an order-independent set of role names). Only directly-assigned (not inherited) assignments are compared, so an inherited role from a parent folder is not counted as a folder-level assignment.

Each result is an OrchComparison record with a SideIndicator: "<=" assigned only on the reference side, "=>" assigned only on the difference side, "<>" assigned on both sides but with a different role set or type (with a per-property Differences breakdown), and "==" identical (only with -IncludeEqual). Without -DifferenceName each reference user is compared to the same-named user in the corresponding difference folder (mirrored relative path with -Recurse). With -DifferenceName, every reference user is compared to that single named user.

Primary Endpoint: GET /odata/Folders/UiPath.Server.Configuration.OData.GetUsersForFolder

OAuth required scopes: OR.Folders or OR.Folders.Read (both sides)

Required permissions: Subfolders.View, Units.View (both sides)

## EXAMPLES

### Example 1: Verify folder access migrated to another tenant

```powershell
PS C:\> Compare-OrchFolderUser * Orch2:\Finance -Path Orch1:\Finance
```

Compares the user assignments of Finance on Orch1 against Finance on Orch2, showing only the differences (missing users, extra users, or changed role sets).

### Example 2: Audit role drift across a whole tree

```powershell
PS C:\> Compare-OrchFolderUser -Path Orch1:\Shared -DifferencePath Orch2:\Shared -Recurse |
    Where-Object SideIndicator -eq '<>' | Select-Object Name -ExpandProperty Differences
```

Walks Shared and all subfolders, lists only the users whose role set differs, and expands the per-property differences.

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

Selects broadcast mode: every reference user is compared to this single named user in -DifferencePath, even when the names differ.

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

Also emits "==" rows for assignments that match on every compared property. Off by default.

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

Filters by user name (the assignment's match key); supports wildcards. In name-match mode the same filter is applied to the difference side.

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

Restricts the comparison to the named properties. Valid names: Type, Roles. Unrecognized names are warned about and ignored.

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

Returns one comparison record per folder user, with SideIndicator, Name, the single-sided Path and DifferencePath, the per-property Differences (on "<>" rows), and the underlying ReferenceObject / DifferenceObject.

## NOTES

Only directly-assigned (not inherited) folder assignments are compared. Assignments are matched by user name, case-insensitively. This cmdlet is read-only and does not support -WhatIf / -Confirm.

## RELATED LINKS

- [Get-OrchFolderUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchFolderUser.md)
- [Copy-OrchFolderUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchFolderUser.md)
- [Compare-OrchAsset](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Compare-OrchAsset.md)
