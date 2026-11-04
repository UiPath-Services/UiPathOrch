---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-TmTestSetTestCase.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 10/31/2026
PlatyPS schema version: 2024-05-01
title: Remove-TmTestSetTestCase
---

# Remove-TmTestSetTestCase

## SYNOPSIS

Unassigns test cases from test sets in Test Manager.

## SYNTAX

### __AllParameterSets

```
Remove-TmTestSetTestCase [-Path <string[]>] [-LiteralPath <string[]>]
 [-TestSet] <string[]> [-TestCase] <string[]> [-Confirm] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Unassigns the test cases identified by `-TestCase` from the test sets identified by `-TestSet`. The test cases themselves are not removed.
A name without wildcards must match exactly one test set, and exactly one of the test cases assigned to it; a name that matches two, or none, is reported as an error. A wildcard pattern selects every match.
The cmdlet sends one request per test set for all the records piped to it.
This cmdlet operates on the PSDrive of the UiPathOrchTm provider.
If the scope in the configuration file includes "TM.", the PSDrive of the UiPathOrchTm provider will be automatically added.
You can confirm this with the Get-PSDrive cmdlet.
The configuration file can be opened with the Edit-OrchConfig cmdlet.

Primary Endpoint: POST /testmanager_/api/v2/{projectId}/testsets/{testSetId}/unassigntestcases

OAuth Required scopes: TM.TestSets, TM.TestCases

## EXAMPLES

### Example 1: Unassign a test case

```powershell
PS Orch1Tm:\MTP> Remove-TmTestSetTestCase Smoke Logout
```

### Example 2: Preview unassigning every test case from the test sets matching Nightly*

```powershell
PS Orch1Tm:\MTP> Remove-TmTestSetTestCase Nightly* * -WhatIf
```

## PARAMETERS

### -TestSet

Specifies the names of the test sets.
Wildcard characters are permitted. A name without wildcards must match exactly one test set.
This parameter is mandatory.

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

### -TestCase

Specifies the names of the test cases.
Wildcard characters are permitted. A name without wildcards must match exactly one test case.
This parameter is mandatory.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: (All)
  Position: 1
  IsRequired: true
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

### -Confirm

Prompts you for confirmation before running the cmdlet.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: False
SupportsWildcards: false
Aliases:
- cf
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

### -WhatIf

Shows what would happen if the cmdlet runs.
The cmdlet is not run.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: False
SupportsWildcards: false
Aliases:
- wi
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

### UiPath.PowerShell.Entities.TmTestSetTestCase

You can pipe the output of Get-TmTestSetTestCase, or rows of a CSV file with Path, TestSet and TestCase columns, to this cmdlet.

## OUTPUTS

### None

This cmdlet does not produce pipeline output.

## NOTES

This cmdlet operates on the UiPathOrchTm provider PSDrive. Supports -WhatIf, which names the test cases each test set would lose.

## RELATED LINKS

[Get-TmTestSetTestCase](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-TmTestSetTestCase.md)

[Add-TmTestSetTestCase](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Add-TmTestSetTestCase.md)

[Remove-TmTestCase](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-TmTestCase.md)
