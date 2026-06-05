---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchTestSetExecution.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchTestSetExecution
---

# Get-OrchTestSetExecution

## SYNOPSIS

Gets test set executions from UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
Get-OrchTestSetExecution [-Path <string[]>] [-LiteralPath <string[]>] [-Recurse] [-Depth <uint>]
 [[-Name] <string[]>] [-First <ulong>] [-Last <string>] [-Skip <ulong>]
 [-StartTimeAfter <datetime>] [-StartTimeBefore <datetime>] [-Status <string[]>]
 [-TriggerType <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets test set execution information from UiPath Orchestrator folders. A test set execution represents a single run of a test set and contains the results of all test cases in that set.

When no filter parameters (-Last, -StartTimeAfter, -StartTimeBefore, -Status, -TriggerType, -Skip, -First) are specified, the cmdlet returns cached results and displays a warning. To query the Orchestrator directly, specify at least one filter parameter such as `-Last Day`.

The -Name, -Status, and -TriggerType parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values dynamically populated from the target folders. Multiple values can be specified using comma-separated text that includes wildcards.

Valid values for -Status: Pending, Running, Cancelling, Passed, Failed, Cancelled.

Valid values for -TriggerType: Manual, Scheduled, ExternalTool.

Valid values for -Last: Hour, Day, Week, Month, 3Months, 6Months, Year, 3Years.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: GET /odata/TestSetExecutions?$expand=TestSet

OAuth required scopes: OR.TestSetExecutions or OR.TestSetExecutions.Read

Required permissions: TestSetExecutions.View

## EXAMPLES

### Example 1: Get test set executions from the last 3 years

```powershell
PS Orch1:\root> Get-OrchTestSetExecution -Last 3Years
```

Gets all test set executions from the current folder that started within the last 3 years.

### Example 2: Get test set executions by name

```powershell
PS Orch1:\root> Get-OrchTestSetExecution SmokeTest -Last 3Years
```

Gets test set executions named "SmokeTest" from the last 3 years.

### Example 3: Get cancelled test set executions

```powershell
PS Orch1:\root> Get-OrchTestSetExecution -Status Cancelled -Last 3Years
```

Gets all test set executions with a Cancelled status from the last 3 years.

### Example 4: Get test set executions within a date range

```powershell
PS Orch1:\root> Get-OrchTestSetExecution -StartTimeAfter 2025-02-01 -StartTimeBefore 2025-03-01
```

Gets all test set executions that started in February 2025.

### Example 5: Get the most recent test set executions

```powershell
PS Orch1:\root> Get-OrchTestSetExecution -Last 3Years -First 10 | Format-Table Name, Status, StartTime
```

Gets the 10 most recent test set executions from the last 3 years and displays them in a table format.

### Example 6: Get scheduled executions recursively

```powershell
PS Orch1:\> Get-OrchTestSetExecution -Recurse -TriggerType Scheduled -Last 3Years
```

Gets all scheduled test set executions from the last 3 years across all folders.

## PARAMETERS

### -Path

Specifies the target folders. If not specified, the current folder is targeted. Supports wildcards and comma-separated values for multiple folders.

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

### -LiteralPath

Specifies the target folder or drive by literal path -- wildcard metacharacters (`[`, `]`, `*`, `?`) are treated as literal characters rather than patterns. Accepts the same drive-qualified paths as -Path. Its `PSPath` alias also binds the path of items piped from Get-ChildItem / Get-Item, so you can pipe folders directly. Use -LiteralPath instead of -Path when a folder name contains a wildcard metacharacter.

```yaml
Type: System.String[]
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

### -Recurse

Includes the target folder and all its subfolders in the operation.

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

### -Depth

Specifies the depth for recursion into the target folders. A depth of 0 targets only the current folder with no subfolders included. When -Depth is specified, -Recurse is implied.

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

### -First

Gets only the specified number of objects. Enter the number of objects to get.

```yaml
Type: System.Nullable`1[System.UInt64]
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

### -Last

Specifies the most recent period for retrieving test set executions. Valid values are: Hour, Day, Week, Month, 3Months, 6Months, Year, 3Years. Tab completion suggests available values.

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

### -Name

Specifies the names of the test set executions to retrieve. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests test set execution names from the target folders.

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

### -Skip

Ignores the specified number of objects and then gets the remaining objects. Enter the number of objects to skip.

```yaml
Type: System.Nullable`1[System.UInt64]
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

### -StartTimeAfter

Specifies the start date and time for filtering. Only test set executions with a StartTime at or after this value are returned.

```yaml
Type: System.Nullable`1[System.DateTime]
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

### -StartTimeBefore

Specifies the end date and time for filtering. Only test set executions with a StartTime before this value are returned.

```yaml
Type: System.Nullable`1[System.DateTime]
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

### -Status

Specifies the status of the test set executions to retrieve. Valid values are: Pending, Running, Cancelling, Passed, Failed, Cancelled. Supports wildcards. Tab completion suggests available values.

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

### -TriggerType

Specifies the trigger type of the test set executions to retrieve. Valid values are: Manual, Scheduled, ExternalTool. Supports wildcards. Tab completion suggests available values.

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe test set execution names to this cmdlet via the Name property.

### System.String

You can pipe a Last value or folder path to this cmdlet.

### System.DateTime

You can pipe DateTime values to the StartTimeAfter and StartTimeBefore parameters.

### System.UInt64

You can pipe values to the Skip and First parameters.

## OUTPUTS

### UiPath.PowerShell.Entities.TestSetExecution

Returns TestSetExecution objects with properties including Name, Id, Status, StartTime, EndTime, TriggerType, and the associated TestSet details.

## NOTES

Test set executions are folder-scoped entities. You must navigate to a folder on the Orch: drive or use -Path to specify target folders.

When no filter parameters are specified, the cmdlet outputs cached results instead of querying the Orchestrator. A warning message is displayed to inform you. To force a query, specify at least one filter parameter such as `-Last Day`.

## RELATED LINKS

[Start-OrchTestSet](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Start-OrchTestSet.md)

[Stop-OrchTestSetExecution](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Stop-OrchTestSetExecution.md)

[Get-OrchTestSet](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchTestSet.md)
