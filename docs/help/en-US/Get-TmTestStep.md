---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-TmTestStep.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 10/30/2026
PlatyPS schema version: 2024-05-01
title: Get-TmTestStep
---

# Get-TmTestStep

## SYNOPSIS

Gets the manual steps of test cases in Test Manager.

## SYNTAX

### __AllParameterSets

```
Get-TmTestStep [[-TestCase] <string[]>] [[-OrderNo] <int[]>] [-Path <string[]>] [-LiteralPath <string[]>] [-Recurse]
 [-ExportCsv <string>] [-CsvEncoding <Encoding>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets the steps of the test cases identified by the `-TestCase` parameter, which supports wildcard matching. Without `-TestCase`, the steps of every test case in the project are returned.
A test step has no name: it is identified by its test case and its position, `orderNo`, which counts from 0. Negative positions count from the end: -1 is the last step.
The steps of each test case are returned in position order. Each step carries the name of its test case in the `TestCase` property and the project path in the `Path` property, so it can be piped to the other test step cmdlets.
This cmdlet operates on the PSDrive of the UiPathOrchTm provider.
If the scope in the configuration file includes "TM.", the PSDrive of the UiPathOrchTm provider will be automatically added.
You can confirm this with the Get-PSDrive cmdlet.
The configuration file can be opened with the Edit-OrchConfig cmdlet.

Primary Endpoint: GET /testmanager_/api/v2/{projectId}/teststeps/testcase/{testCaseId}

OAuth Required scopes: TM.TestCases

## EXAMPLES

### Example 1: List the steps of a test case

```powershell
PS Orch1Tm:\MTP> Get-TmTestStep Login
```

Lists the steps of the test case "Login" in the current project.

### Example 2: Get the first and the last step

```powershell
PS Orch1Tm:\MTP> Get-TmTestStep Login 0,-1
```

### Example 3: Export the steps of several test cases to CSV

```powershell
PS C:\> Get-TmTestStep -Path Orch1Tm:\MTP Checkout* -ExportCsv steps.csv
```

The CSV has the columns Path, TestCase, OrderNo, Description, ExpectedResult and ClipboardData: the parameters of New-TmTestStep.

### Example 4: Recreate the exported steps in another project

```powershell
PS C:\> Import-Csv steps.csv | New-TmTestStep -Path Orch2Tm:\MTP
```

`-Path` on the command line takes precedence over the Path column; without it, the steps are added to the project named in the CSV. The test cases must already exist in the target project and should have no steps: each row is inserted at its OrderNo. Leave an OrderNo cell empty, or remove the column, to append the row instead.

## PARAMETERS

### -TestCase

Specifies the names of the test cases whose steps to get.
Wildcard characters are permitted.
If omitted, every test case in the project is targeted.

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

### -OrderNo

Specifies the positions of the steps to get. Positions count from 0; negative positions count from the end (-1 is the last step). If omitted, or bound from an empty CSV cell, every step is returned. A position out of range is reported as an error.

```yaml
Type: System.Int32[]
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 1
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Path

Specifies the target project. If not specified, the current project is targeted.

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

Specifies the target project by literal path -- wildcard metacharacters (`[`, `]`, `*`, `?`) are treated as literal characters rather than patterns. Accepts the same drive-qualified paths as -Path. Its `PSPath` alias also binds the path of items piped from Get-ChildItem / Get-Item, so you can pipe projects directly.

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

Includes the target project and all its subfolders in the operation.

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

Specifies the path of a CSV file to export the steps to, instead of writing them to the pipeline. If a folder is given, the file is named ExportedTmTestSteps.csv. The columns are Path, TestCase, OrderNo, Description, ExpectedResult and ClipboardData, so `Import-Csv | New-TmTestStep` recreates the steps. Path and TestCase are wildcard-escaped, because New-TmTestStep treats them as wildcard patterns.

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe test case names or paths to this cmdlet.

## OUTPUTS

### UiPath.PowerShell.Entities.TmTestStep

The test steps, with `orderNo`, `description`, `expectedResult` and `clipboardData`.

## NOTES

This cmdlet operates on the UiPathOrchTm provider PSDrive. The steps are cached per test case; the test step cmdlets clear a test case's cached steps when they change any of them. Changes made in the web app show after Clear-OrchCache.

## RELATED LINKS

[New-TmTestStep](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/New-TmTestStep.md)

[Update-TmTestStep](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Update-TmTestStep.md)

[Move-TmTestStep](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Move-TmTestStep.md)

[Remove-TmTestStep](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-TmTestStep.md)

[Get-TmTestCase](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-TmTestCase.md)