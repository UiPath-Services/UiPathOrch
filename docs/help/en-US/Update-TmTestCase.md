---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Update-TmTestCase.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 10/30/2026
PlatyPS schema version: 2024-05-01
title: Update-TmTestCase
---

# Update-TmTestCase

## SYNOPSIS

Updates the name, version, description, pre-condition, post-condition or labels of test cases in Test Manager.

## SYNTAX

### __AllParameterSets

```
Update-TmTestCase [-Name] <string[]> [-NewName <string>] [-Version <string>] [-Description <string>]
 [-PreCondition <string>] [-PostCondition <string>] [-Labels <string[]>]
 [-Path <string[]>] [-LiteralPath <string[]>] [-Recurse] [-Confirm] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Updates the test cases identified by the `-Name` parameter, which supports wildcard matching.
Only the fields given are changed: the name, version, description, pre-condition and post-condition are sent as a JSON Patch, one operation per field that differs from its current value, so a request that changes nothing sends nothing.
`-Labels` replaces the test case's labels with the given list. An empty list leaves the labels unchanged.
Matched test cases are processed in object key order.
This cmdlet operates on the PSDrive of the UiPathOrchTm provider.
If the scope in the configuration file includes "TM.", the PSDrive of the UiPathOrchTm provider will be automatically added.
You can confirm this with the Get-PSDrive cmdlet.
The configuration file can be opened with the Edit-OrchConfig cmdlet.

Primary Endpoint: PATCH /testmanager_/api/v2/{projectId}/testcases/{testCaseId}

Labels Endpoint: POST /testmanager_/api/v2/{projectId}/objectlabels/testCase/addlabels

OAuth Required scopes: TM.TestCases

## EXAMPLES

### Example 1: Rename a test case and bump its version

```powershell
PS Orch1Tm:\MTP> Update-TmTestCase Login -NewName "Sign in with a password" -Version 1.1
```

Renames the test case "Login" in the current project and sets its version to 1.1.

### Example 2: Set the pre- and post-condition of several test cases

```powershell
PS C:\> Update-TmTestCase -Path Orch1Tm:\MTP Auth* -PreCondition "The user is signed out" -PostCondition "The user is signed in"
```

Sets the pre-condition and the post-condition of every test case whose name starts with "Auth" in the project MTP.

### Example 3: Replace the labels

```powershell
PS Orch1Tm:\MTP> Update-TmTestCase Login -Labels regression
```

Replaces the labels of the test case "Login" with the single label "regression".

### Example 4: Update test cases from the pipeline

```powershell
PS Orch1Tm:\MTP> Get-TmTestCase Log* | Update-TmTestCase -Description "Owned by the auth team"
```

The name and the project path of each test case are bound by property name.

## PARAMETERS

### -Name

Specifies the names of the test cases to update.
Wildcard characters are permitted.
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

### -NewName

Specifies the new name of the test cases.

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

### -Version

Specifies the new version of the test cases, as free text. An empty string clears the version.

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
### -Description

Specifies the new description of the test cases. An empty string clears the description.

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

### -PreCondition

Specifies what must be true before the test runs. An empty string clears it.

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
### -PostCondition

Specifies what should be true after the test runs. An empty string clears it.

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
### -Labels

Specifies the names of the labels that replace the test cases' current labels.

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

### System.String[]

You can pipe test case names or paths to this cmdlet.

## OUTPUTS

### None

This cmdlet does not produce pipeline output.

## NOTES

This cmdlet operates on the UiPathOrchTm provider PSDrive. Supports -WhatIf to preview which test cases would be updated.

## RELATED LINKS

[Get-TmTestCase](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-TmTestCase.md)

[New-TmTestCase](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/New-TmTestCase.md)

[Remove-TmTestCase](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-TmTestCase.md)
