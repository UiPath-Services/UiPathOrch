---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Add-TmRequirementTestCase.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 10/31/2026
PlatyPS schema version: 2024-05-01
title: Add-TmRequirementTestCase
---

# Add-TmRequirementTestCase

## SYNOPSIS

Assigns test cases to requirements in Test Manager.

## SYNTAX

### __AllParameterSets

```
Add-TmRequirementTestCase [-Requirement] <string[]> [-TestCase] <string[]> [-Path <string[]>] [-LiteralPath <string[]>]
 [-Confirm] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Assigns the test cases identified by `-TestCase` to the requirements identified by `-Requirement`. Both take names, not ids, so a CSV exported by Get-TmRequirementTestCase -ExportCsv can be imported into another project or tenant.
A name without wildcards must match exactly one requirement or test case: Test Manager allows several with the same name, and a name that matches two is reported as an error rather than guessed. A name that matches nothing is also an error. A wildcard pattern selects every match.
The cmdlet reads each requirement's current test cases first and skips the ones already assigned, then sends one request per requirement for all the records piped to it.
This cmdlet operates on the PSDrive of the UiPathOrchTm provider.
If the scope in the configuration file includes "TM.", the PSDrive of the UiPathOrchTm provider will be automatically added.
You can confirm this with the Get-PSDrive cmdlet.
The configuration file can be opened with the Edit-OrchConfig cmdlet.

Primary Endpoint: POST /testmanager_/api/v2/{projectId}/requirements/{requirementId}/assigntestcases

OAuth Required scopes: TM.Requirements, TM.TestCases

## EXAMPLES

### Example 1: Assign two test cases

```powershell
PS Orch1Tm:\MTP> Add-TmRequirementTestCase Sign-in Login, Logout
```

### Example 2: Assign every test case whose name starts with "Checkout"

```powershell
PS Orch1Tm:\MTP> Add-TmRequirementTestCase Sign-in Checkout*
```

### Example 3: Import assignments exported from another tenant

```powershell
PS C:\> Import-Csv assignments.csv | Add-TmRequirementTestCase -Path Orch2Tm:\MTP
```

`-Path` on the command line takes precedence over the Path column of the CSV.

## PARAMETERS

### -Requirement

Specifies the names of the requirements.
Wildcard characters are permitted. A name without wildcards must match exactly one requirement.
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

### UiPath.PowerShell.Entities.TmRequirementTestCase

You can pipe the output of Get-TmRequirementTestCase, or rows of a CSV file with Path, Requirement and TestCase columns, to this cmdlet.

## OUTPUTS

### None

This cmdlet does not produce pipeline output.

## NOTES

This cmdlet operates on the UiPathOrchTm provider PSDrive. Supports -WhatIf, which names the test cases each requirement would gain.

## RELATED LINKS

[Get-TmRequirementTestCase](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-TmRequirementTestCase.md)

[Remove-TmRequirementTestCase](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-TmRequirementTestCase.md)

[Get-TmTestCase](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-TmTestCase.md)
