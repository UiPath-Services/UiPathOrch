---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchTestCaseExecution
---

# Get-OrchTestCaseExecution

## SYNOPSIS

Gets test case execution results from UiPath Orchestrator.

## SYNTAX

### ByName (Default)

```
Get-OrchTestCaseExecution [[-TestSetExecutionName] <string>] [-Name <string[]>] [-Last <string>]
 [-StartTimeAfter <datetime>] [-StartTimeBefore <datetime>] [-Skip <ulong>] [-First <ulong>]
 [-Path <string[]>] [-Recurse] [-Depth <uint>] [<CommonParameters>]
```

### ById

```
Get-OrchTestCaseExecution -TestSetExecutionId <long> [-Name <string[]>] [-Path <string[]>]
 [-Recurse] [-Depth <uint>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets test case execution results from UiPath Orchestrator. Each test case execution represents an individual test case run within a test set execution, and includes properties such as EntryPointPath, Status, StartTime, and EndTime.

In the ByName parameter set, you can specify the test set execution by name using -TestSetExecutionName, and further filter results by time range using -Last, -StartTimeAfter, and -StartTimeBefore. If none of the query parameters (TestSetExecutionName, Last, StartTimeAfter, StartTimeBefore, Skip, First) are specified, the cmdlet outputs cached results from a previous query and displays a warning.

In the ById parameter set, you specify -TestSetExecutionId directly. This parameter set accepts pipeline input via the TestSetExecutionId property, enabling piping from Get-OrchTestSetExecution. Duplicate folder and TestSetExecutionId combinations are automatically skipped.

The -Name parameter filters results by the test case EntryPointPath and supports wildcards. The -TestSetExecutionName parameter supports tab completion.

Results are ordered by TestSetExecutionId and then by EntryPointPath.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: GET /odata/TestCaseExecutions

OAuth required scopes: OR.TestSetExecutions or OR.TestSetExecutions.Read

Required permissions: TestSetExecutions.View

## EXAMPLES

### Example 1: Get test case executions by test set execution name

```powershell
PS Orch1:\root> Get-OrchTestCaseExecution SmokeTest
```

Gets all test case execution results for the test set execution named "SmokeTest" from the current folder. The -TestSetExecutionName parameter is positional (position 0) so the parameter name can be omitted.

### Example 2: Get test case executions from the last year

```powershell
PS Orch1:\root> Get-OrchTestCaseExecution -Last Year
```

Gets all test case execution results from the last year. Valid values for -Last are: Hour, Day, Week, Month, 3Months, 6Months, Year, 3Years.

### Example 3: Get test case executions within a date range

```powershell
PS Orch1:\root> Get-OrchTestCaseExecution -StartTimeAfter "2025-02-01" -StartTimeBefore "2025-03-01"
```

Gets all test case execution results with a StartTime between February 1 and March 1, 2025.

### Example 4: Get test case executions via pipeline from test set execution

```powershell
PS Orch1:\root> Get-OrchTestSetExecution -Last 3Years | Get-OrchTestCaseExecution
```

Pipes test set execution objects to Get-OrchTestCaseExecution to retrieve the individual test case results for each test set execution. The TestSetExecutionId property is bound via the pipeline.

### Example 5: Filter test case executions by name with wildcards

```powershell
PS Orch1:\root> Get-OrchTestCaseExecution -Last 3Years -Name *Main*
```

Gets test case execution results from the last 3 years, filtering to only those whose EntryPointPath matches "\*Main\*".

### Example 6: Get the first 10 test case executions

```powershell
PS Orch1:\root> Get-OrchTestCaseExecution -Last 3Years -First 10
```

Gets the first 10 test case execution results from the last 3 years. Use -Skip and -First for pagination.

## PARAMETERS

### -Path

Specifies the target folder. If not specified, the current folder is targeted.

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

Specifies the depth for recursion into the target folders.
A depth of 0 indicates the current location only, with no subfolders included. When -Depth is specified, -Recurse is implied.

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

Gets only the specified number of objects.
Enter the number of objects to get.

```yaml
Type: System.Nullable`1[System.UInt64]
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: ByName
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Last

Specifies the most recent period for retrieving test case executions. Valid values are: Hour, Day, Week, Month, 3Months, 6Months, Year, 3Years.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: ByName
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

Specifies the EntryPointPath of the test case executions to filter. Supports wildcards and multiple comma-separated values.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
Aliases:
- EntryPointPath
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

### -Skip

Ignores the specified number of objects and then gets the remaining objects.
Enter the number of objects to skip.

```yaml
Type: System.Nullable`1[System.UInt64]
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: ByName
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -StartTimeAfter

Specifies the start date and time for the StartTime of the test case executions to be retrieved.

```yaml
Type: System.Nullable`1[System.DateTime]
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: ByName
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -StartTimeBefore

Specifies the end date and time for the StartTime of the test case executions to be retrieved.

```yaml
Type: System.Nullable`1[System.DateTime]
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: ByName
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -TestSetExecutionId

Specifies the Id of the test set execution to retrieve test case executions for. This parameter is mandatory in the ById parameter set and accepts pipeline input via the TestSetExecutionId property.

```yaml
Type: System.Int64
DefaultValue: ''
SupportsWildcards: false
Aliases:
- Id
ParameterSets:
- Name: ById
  Position: Named
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -TestSetExecutionName

Specifies the name of the test set execution to retrieve test case executions for. Tab completion suggests available test set execution names from the target folders.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: ByName
  Position: 0
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

### System.Int64

You can pipe objects with a TestSetExecutionId property (such as TestSetExecution objects from Get-OrchTestSetExecution) to this cmdlet via the ById parameter set.

### System.String[]

You can pipe folder paths to this cmdlet via the Path property.

## OUTPUTS

### UiPath.PowerShell.Entities.TestCaseExecution

Returns TestCaseExecution objects with properties including Id, EntryPointPath, Status, StartTime, EndTime, TestSetExecutionId, TestSetExecutionName, and Path.

## NOTES

Test case executions are folder-scoped entities. You must navigate to a folder on the Orch: drive or use -Path to specify target folders. Personal workspaces are excluded from enumeration.

When no query parameters (TestSetExecutionName, Last, StartTimeAfter, StartTimeBefore, Skip, First) are specified in the ByName parameter set, the cmdlet outputs cached results from a previous query rather than querying the Orchestrator. A warning is displayed in this case.

## RELATED LINKS

Get-OrchTestCaseAssertion

Get-OrchTestSetExecution

Get-OrchTestCase

Get-OrchTestSet
