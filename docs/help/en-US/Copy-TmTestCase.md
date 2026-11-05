---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-TmTestCase.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 11/04/2026
PlatyPS schema version: 2024-05-01
title: Copy-TmTestCase
---

# Copy-TmTestCase

## SYNOPSIS

Copies Test Manager test cases, with their steps, to another project.

## SYNTAX

### __AllParameterSets

```
Copy-TmTestCase [-Path <string[]>] [-LiteralPath <string[]>] [-Name] <string[]>
 [-Destination] <string> [-Confirm] [-Force] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Copies the test cases that match `-Name`, from the Test Manager project at the current location or at `-Path`, into the project at `-Destination`.
The destination can be in another tenant or organization: any project on a UiPathOrchTm drive.
Each test case is copied with its name, version, description, foreign reference, pre-condition, post-condition, manual test steps (in order) and labels.

Test cases are matched with the destination's by their name together with their foreign reference, both exactly (case included; leading and trailing spaces are ignored, and an empty foreign reference matches an empty one), since Test Manager allows neither to be unique:

- When no test case in the destination has the same name and foreign reference, the test case is created (also when the source has several).
- When exactly one in the source and one in the destination have them, the destination test case is reused instead of created, and only its missing labels are added. If it has no steps, the source's steps are copied into it, so a copy that failed partway can simply be run again.
- Otherwise -- several on either side, and at least one in the destination -- it is not known which is which: the test cases are not copied, with a warning. With `-Force` they are created as new test cases (a one-to-one match is still reused).

So running the same copy again adds nothing, unless `-Force` is used.

The copied test cases are also added to the test sets and requirements they belong to in the source, when the destination has a counterpart: a test set of the same name (a requirement of the same name and foreign reference), used only once in the source and once in the destination.
Test sets and requirements are not created; the addition to each existing one is confirmed on its own, so `-WhatIf` shows which ones would change.
Together with `Copy-TmTestSet` and `Copy-TmRequirement`, which add the assigned test cases that already exist at the destination, this makes the result the same whichever of the cmdlets runs first.

Not copied: linked automation (a warning reports how many test cases had it), custom fields, attachments and execution results.

This cmdlet operates on the PSDrive of the UiPathOrchTm provider.
If the scope in the configuration file includes "TM.", the PSDrive of the UiPathOrchTm provider will be automatically added.
You can confirm this with the Get-PSDrive cmdlet.
The configuration file can be opened with the Edit-OrchConfig cmdlet.

Primary Endpoint: POST /testmanager_/api/v2/{projectId}/testcases

Steps Endpoint: POST /testmanager_/api/v2/{projectId}/teststeps

Assignment Endpoints: POST /testmanager_/api/v2/{projectId}/testsets/{id}/assigntestcases, POST /testmanager_/api/v2/{projectId}/requirements/{id}/assigntestcases

Labels Endpoint: POST /testmanager_/api/v2/{projectId}/objectlabels/testCase/addlabels

OAuth Required scopes: TM.TestCases

## EXAMPLES

### Example 1: Copy every test case to another tenant

```powershell
PS Orch1Tm:\MTP> Copy-TmTestCase * Orch2Tm:\MTP
```

Copies all test cases of the current project, with their steps and labels, into the project MTP of the drive Orch2Tm:.

### Example 2: Copy the test cases whose names start with "Login"

```powershell
PS C:\> Copy-TmTestCase -Path Orch1Tm:\MTP Login* Orch1Tm:\MTP2
```

### Example 3: Preview the copy

```powershell
PS Orch1Tm:\MTP> Copy-TmTestCase * Orch2Tm:\MTP -WhatIf
```

Shows the test cases that would be copied, without copying them.

## PARAMETERS

### -Name

Specifies the names of the test cases to copy. Wildcards are permitted.
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

### -Destination

Specifies the project to copy into, as a drive-qualified path such as `Orch2Tm:\MTP`. It must name exactly one existing project.
This parameter is mandatory.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: false
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

### -Path

Specifies the source project. If not specified, the current project is the source.

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

Specifies the source project by literal path -- wildcard metacharacters (`[`, `]`, `*`, `?`) are treated as literal characters rather than patterns. Accepts the same drive-qualified paths as -Path. Its `PSPath` alias also binds the path of items piped from Get-ChildItem / Get-Item, so you can pipe projects directly.

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

### -Force

Creates the test cases that cannot be matched one-to-one with the destination's -- several test cases with the same name and foreign reference on either side -- as new test cases, instead of skipping them.
A test case that matches exactly one in the destination is still reused.

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

You can pipe test case names or project paths to this cmdlet.

## OUTPUTS

### None

This cmdlet does not output anything.

## NOTES

This cmdlet operates on the UiPathOrchTm provider PSDrive. Supports -WhatIf to preview which test cases would be copied.
To copy a whole project -- requirements, test cases with their steps, test sets, assignments, labels and project settings -- use `Copy-Item Orch1Tm:\MTP Orch2Tm:\`.

## RELATED LINKS

[Get-TmTestCase](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-TmTestCase.md)

[Copy-TmRequirement](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-TmRequirement.md)

[Copy-TmTestSet](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-TmTestSet.md)
