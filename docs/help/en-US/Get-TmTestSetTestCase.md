---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-TmTestSetTestCase.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 10/31/2026
PlatyPS schema version: 2024-05-01
title: Get-TmTestSetTestCase
---

# Get-TmTestSetTestCase

## SYNOPSIS

Gets the test cases assigned to test sets in Test Manager.

## SYNTAX

### __AllParameterSets

```
Get-TmTestSetTestCase [-Path <string[]>] [-LiteralPath <string[]>] [-Recurse]
 [[-TestSet] <string[]>] [[-TestCase] <string[]>] [-CsvEncoding <Encoding>]
 [-ExportCsv <string>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets the test cases assigned to the test sets identified by the `-TestSet` parameter, which supports wildcard matching. Without `-TestSet`, the assignments of every test set in the project are returned.
Each assignment is returned as one object with the project path, the test set's name and the test case's name, so it can be piped to Remove-TmTestSetTestCase, or exported with `-ExportCsv` and imported into another project or tenant with Add-TmTestSetTestCase.
The test sets are returned in object key order; the test cases of each, in the order they were created. Test Manager does not keep the order in which test cases were assigned.
This cmdlet operates on the PSDrive of the UiPathOrchTm provider.
If the scope in the configuration file includes "TM.", the PSDrive of the UiPathOrchTm provider will be automatically added.
You can confirm this with the Get-PSDrive cmdlet.
The configuration file can be opened with the Edit-OrchConfig cmdlet.

Primary Endpoint: GET /testmanager_/api/v2/{projectId}/testsets/{testSetId}/assignedtestcaseids

OAuth Required scopes: TM.TestSets, TM.TestCases

## EXAMPLES

### Example 1: List the test cases of a test set

```powershell
PS Orch1Tm:\MTP> Get-TmTestSetTestCase Smoke
```

### Example 2: Find the test sets a test case is assigned to

```powershell
PS Orch1Tm:\MTP> Get-TmTestSetTestCase -TestCase Login | Format-Table TestSet, TestCase
```

The default view groups the output by test set; Format-Table with the columns named lists the matches in one table instead.

### Example 3: Export every assignment in a project to CSV

```powershell
PS C:\> Get-TmTestSetTestCase -Path Orch1Tm:\MTP -ExportCsv assignments.csv
```

The CSV has the columns Path, TestSet and TestCase.

### Example 4: Assign the same test cases in another tenant

```powershell
PS C:\> Import-Csv assignments.csv | Add-TmTestSetTestCase -Path Orch2Tm:\MTP
```

The test sets and the test cases must already exist in the target project. `-Path` on the command line takes precedence over the Path column.

## PARAMETERS

### -TestSet

Specifies the names of the test sets whose test cases to get.
Wildcard characters are permitted.
If omitted, every test set in the project is targeted.

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

### -TestCase

Specifies the names of the test cases to list.
Wildcard characters are permitted.
If omitted, every assigned test case is listed.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
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

Specifies the path of a CSV file to export the assignments to, instead of writing them to the pipeline. If a folder is given, the file is named ExportedTmTestSetTestCases.csv. The columns are Path, TestSet and TestCase, so `Import-Csv | Add-TmTestSetTestCase` assigns the same test cases in another project. The names are wildcard-escaped. A name used by more than one test set or test case in the project is reported with a warning, because importing its rows will fail.

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

You can pipe test set names, test case names and project paths to this cmdlet.

## OUTPUTS

### UiPath.PowerShell.Entities.TmTestSetTestCase

One object per assigned test case, with Path, TestSet, TestCase, testSetId and testCaseId, and the test case's objKey, foreignReference, version, automationTestCaseName, updated and description for display. Test Manager allows several test cases with the same name; objKey and foreignReference (not in the default view) tell them apart.

## NOTES

This cmdlet operates on the UiPathOrchTm provider PSDrive. The assignments are read with one request per test set and are not cached.

## RELATED LINKS

[Add-TmTestSetTestCase](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Add-TmTestSetTestCase.md)

[Remove-TmTestSetTestCase](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-TmTestSetTestCase.md)

[Get-TmTestSet](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-TmTestSet.md)

[Get-TmTestCase](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-TmTestCase.md)
