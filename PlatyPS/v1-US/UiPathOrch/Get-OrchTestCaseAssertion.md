---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchTestCaseAssertion
---

# Get-OrchTestCaseAssertion

## SYNOPSIS

Gets test case assertion results from UiPath Orchestrator.

## SYNTAX

### ByTestSetExecutionName (Default)

```
Get-OrchTestCaseAssertion [[-TestSetExecutionName] <string>] [-Path <string[]>]
 [-ScreenshotPath <string>] [<CommonParameters>]
```

### ById

```
Get-OrchTestCaseAssertion [-Id] <long[]> [-Path <string[]>] [-ScreenshotPath <string>]
 [<CommonParameters>]
```

### ByPipeline

```
Get-OrchTestCaseAssertion -InputObject <Object> [-Path <string[]>] [-ScreenshotPath <string>]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets test case assertion results from UiPath Orchestrator. Assertions are the individual verification points within a test case execution, each containing pass/fail status and optional screenshot evidence.

This cmdlet supports three parameter sets:

- **ByTestSetExecutionName** (default): Specify the test set execution by name using -TestSetExecutionName. The cmdlet resolves the name to a TestSetExecutionId, retrieves all related test case execution IDs, and then fetches assertions for each. If -TestSetExecutionName is not specified, the cmdlet outputs cached results from a previous query and displays a warning.

- **ById**: Specify one or more test case execution IDs directly using -Id. Tab completion suggests available IDs.

- **ByPipeline**: Pipe TestCaseExecution or TestSetExecution objects. The cmdlet uses duck-typing and also accepts any object with an Id or Name property.

When -ScreenshotPath is specified, the cmdlet downloads assertion screenshots (JPEG) to the specified directory. Screenshots are organized into subdirectories by folder name and test set execution name.

The -TestSetExecutionName parameter supports tab completion. The -Id parameter supports tab completion for available test case execution IDs.

Primary Endpoint: GET /odata/TestCaseExecutions({id})?$expand=TestCaseAssertions

OAuth required scopes: OR.TestSetExecutions or OR.TestSetExecutions.Read

Required permissions: TestSetExecutions.View

## EXAMPLES

### Example 1: Get assertions by test set execution name

```powershell
PS Orch1:\Shared> Get-OrchTestCaseAssertion TestSet-TestProject-Migration-01
```

Gets all test case assertions for the test set execution named "TestSet-TestProject-Migration-01" from the current folder. The -TestSetExecutionName parameter is positional (position 0) so the parameter name can be omitted.

### Example 2: Get assertions by test case execution ID

```powershell
PS Orch1:\Shared> Get-OrchTestCaseAssertion -Id 4955039,4955040,4955041
```

Gets all assertions for the test case executions with IDs 4955039, 4955040, and 4955041. The -Id parameter is positional (position 0) so the parameter name can be omitted.

### Example 3: Pipe test case executions to get assertions

```powershell
PS Orch1:\Shared> Get-OrchTestCaseExecution -Last 3Years | Get-OrchTestCaseAssertion
```

Pipes test case execution objects from the last 3 years to retrieve their assertion results via the ByPipeline parameter set.

### Example 4: Get assertions with screenshot download

```powershell
PS Orch1:\Shared> Get-OrchTestCaseAssertion TestSet-TestProject-Migration-01 -ScreenshotPath C:\temp
```

Gets all assertions for the specified test set execution and downloads assertion screenshots to C:\temp. Screenshots are saved as JPEG files organized into subdirectories by folder name and test set execution name. The -ScreenshotPath directory must already exist.

### Example 5: Pipe test set executions to get assertions

```powershell
PS Orch1:\root> Get-OrchTestSetExecution -Last 3Years | Get-OrchTestCaseAssertion
```

Pipes test set execution objects to retrieve all assertions for each test set execution. The cmdlet resolves each TestSetExecution to its constituent test case executions and fetches assertions for each one.

## PARAMETERS

### -Path

Specifies the target folder. If not specified, the current folder is targeted.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: true
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

### -Id

Specifies one or more test case execution IDs to retrieve assertions for. Tab completion suggests available test case execution IDs from the target folders.

```yaml
Type: System.Int64[]
DefaultValue: ''
SupportsWildcards: false
Aliases:
- TestCaseExecutionId
ParameterSets:
- Name: ById
  Position: 0
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -InputObject

Specifies the input object from the pipeline. Accepts TestCaseExecution objects, TestSetExecution objects, or any object with an Id or Name property. When a TestSetExecution object is piped, the cmdlet resolves it to its constituent test case executions and fetches assertions for each.

```yaml
Type: System.Object
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: ByPipeline
  Position: Named
  IsRequired: true
  ValueFromPipeline: true
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -ScreenshotPath

Specifies the local directory path where assertion screenshots should be downloaded. The directory must already exist. Screenshots are saved as JPEG files named with the pattern {TestCaseExecutionId}_{AssertionId}.jpg, organized into subdirectories by folder name and test set execution name.

```yaml
Type: System.String
DefaultValue: ''
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

### -TestSetExecutionName

Specifies the name of the test set execution to retrieve assertions for. Tab completion suggests available test set execution names from the target folders. If not specified, the cmdlet outputs cached results from a previous query.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: ByTestSetExecutionName
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

### System.Object

You can pipe TestCaseExecution objects, TestSetExecution objects, or any object with an Id or Name property to this cmdlet via the ByPipeline parameter set.

## OUTPUTS

### UiPath.PowerShell.Entities.TestCaseAssertion

Returns TestCaseAssertion objects with properties including Id, TestCaseExecutionId, Status, HasScreenshot, ScreenshotPath, Path, TestSetExecutionName, and PathTestSetExecutionName.

## NOTES

Test case assertions are folder-scoped entities. You must navigate to a folder on the Orch: drive or use -Path to specify target folders. Personal workspaces are excluded from enumeration.

When -TestSetExecutionName is not specified in the ByTestSetExecutionName parameter set, the cmdlet outputs cached results from a previous query rather than querying the Orchestrator. A warning is displayed in this case.

Duplicate folder and TestCaseExecutionId combinations are automatically skipped to prevent redundant API calls.

When -ScreenshotPath is specified, only assertions with HasScreenshot set to true are downloaded. Failed downloads produce a warning but do not terminate the cmdlet.

## RELATED LINKS

Get-OrchTestCaseExecution

Get-OrchTestSetExecution

Get-OrchTestCase

Get-OrchTestSet
