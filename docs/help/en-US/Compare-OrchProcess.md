---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Compare-OrchProcess.md
Locale: en-US
Module Name: UiPathOrch
ms.date: 06/07/2026
PlatyPS schema version: 2024-05-01
title: Compare-OrchProcess
---

# Compare-OrchProcess

## SYNOPSIS

Compares deployed processes (releases) between two folders or Orchestrator instances and reports the differences.

## SYNTAX

### __AllParameterSets

```
Compare-OrchProcess [-Name] <string[]> [-DifferencePath] <string> [[-DifferenceName] <string>]
 [-Path <string>] [-LiteralPath <string>] [-Property <string[]>] [-Recurse] [-Depth <uint>]
 [-IncludeEqual] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Compares deployed processes (releases) between a reference location (-Path, or the current folder) and a difference location (-DifferencePath), in the spirit of Compare-Object but resolved over Orchestrator drives and folders. The primary use is migration verification: after copying processes between tenants or folders, confirm they were deployed with the same package version and settings.

Processes are matched by Name (not Id, which is tenant-local) and only migration-relevant settings are compared: ProcessKey, ProcessVersion, Description, EntryPointPath, InputArguments, EnvironmentName, ProcessType, JobPriority, SpecificPriorityValue, TargetFramework, IsAttended, RequiresUserInteraction, RemoteControlAccess, RetentionAction, RetentionPeriod, and Tags. Volatile fields such as Id, Key, the numeric EnvironmentId, and timestamps are ignored; EnvironmentName is compared instead so it survives a tenant move.

Each result is an OrchComparison record with a SideIndicator: "<=" reference only, "=>" difference only, "<>" present on both sides but differing (with a per-property Differences breakdown), and "==" equal (only with -IncludeEqual). Without -DifferenceName each reference process is compared to the same-named process in the corresponding difference folder (mirrored relative path with -Recurse). With -DifferenceName, every reference process is compared to that single named process in -DifferencePath.

Primary Endpoint: GET /odata/Releases

OAuth required scopes: OR.Execution or OR.Execution.Read (both sides)

Required permissions: Processes.View (both sides)

## EXAMPLES

### Example 1: Verify processes migrated to another tenant

```powershell
PS C:\> Compare-OrchProcess Orch1:\Finance Orch2:\Finance
```

Compares every process in Finance on Orch1 against the same-named process in Finance on Orch2, showing only the differences.

### Example 2: Find version drift across a whole tree

```powershell
PS C:\> Compare-OrchProcess -Path Orch1:\Shared -DifferencePath Orch2:\Shared -Recurse |
    Where-Object SideIndicator -eq '<>'
```

Walks Shared and all its subfolders and lists only the processes that differ (for example, a different deployed ProcessVersion).

### Example 3: Include matching processes

```powershell
PS C:\> Compare-OrchProcess Orch1:\Finance Orch2:\Finance -IncludeEqual
```

Adds "==" rows for processes that match on every compared property.

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

Selects broadcast mode. When set, every reference process is compared to this single named process in -DifferencePath, even when the names differ.

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

Specifies the difference (right) folder. This is a mandatory parameter. Can be a folder on the same Orchestrator instance or on a different instance for cross-instance comparison.

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

Also emits "==" rows for processes that match on every compared property. Off by default.

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

Specifies the reference folder by literal path -- wildcard metacharacters (`[`, `]`, `*`, `?`) are treated as literal characters. Its `PSPath` alias also binds the path of items piped from Get-ChildItem / Get-Item.

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

Filters the reference processes by name; supports wildcards. In name-match mode the same filter is applied to the difference side.

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

Restricts the comparison to the named properties. Valid names: ProcessKey, ProcessVersion, Description, EntryPointPath, InputArguments, EnvironmentName, ProcessType, JobPriority, SpecificPriorityValue, TargetFramework, IsAttended, RequiresUserInteraction, RemoteControlAccess, RetentionAction, RetentionPeriod, Tags. Unrecognized names are warned about and ignored.

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

Returns one comparison record per process, with SideIndicator, Name, the single-sided Path and DifferencePath, the per-property Differences (on "<>" rows), and the underlying ReferenceObject / DifferenceObject.

## NOTES

Full, bidirectional diffs come from the -Path / -DifferencePath form. Driving the reference side from the pipeline (Get-OrchProcess ... | Compare-OrchProcess) binds Name per piped item, so the comparison is reference-scoped and cannot surface "=>" (processes that exist only on the difference side). Use the -Path form to detect difference-side additions.

Processes are matched by Name, case-insensitively. This cmdlet is read-only and does not support -WhatIf / -Confirm.

## RELATED LINKS

- [Get-OrchProcess](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchProcess.md)
- [Copy-OrchProcess](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchProcess.md)
- [Compare-OrchAsset](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Compare-OrchAsset.md)
