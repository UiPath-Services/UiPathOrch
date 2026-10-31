---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/New-TmTestCase.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 10/30/2026
PlatyPS schema version: 2024-05-01
title: New-TmTestCase
---

# New-TmTestCase

## SYNOPSIS

Creates test cases in a Test Manager project.

## SYNTAX

### __AllParameterSets

```
New-TmTestCase [-Name] <string[]> [-Version <string>] [-Description <string>] [-Labels <string[]>] [-Path <string[]>]
 [-LiteralPath <string[]>] [-Confirm] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Creates one test case for each name given to the `-Name` parameter, in the Test Manager project at the current location or at `-Path`.
The fields match the Test Manager web form for a new test case: Name, Version, Description and Labels. The pre-condition and the post-condition are set afterwards with `Update-TmTestCase`, as in the web app.
Labels are added by a second request once the test case exists. If that request fails, an error is reported, and the test case is still output without its labels.
The created test cases are output, so they can be piped to `Update-TmTestCase` or `Remove-TmTestCase`.
This cmdlet operates on the PSDrive of the UiPathOrchTm provider.
If the scope in the configuration file includes "TM.", the PSDrive of the UiPathOrchTm provider will be automatically added.
You can confirm this with the Get-PSDrive cmdlet.
The configuration file can be opened with the Edit-OrchConfig cmdlet.

Primary Endpoint: POST /testmanager_/api/v2/{projectId}/testcases

Labels Endpoint: POST /testmanager_/api/v2/{projectId}/objectlabels/testCase/addlabels

OAuth Required scopes: TM.TestCases

## EXAMPLES

### Example 1: Create a test case

```powershell
PS Orch1Tm:\MTP> New-TmTestCase "Sign in with a password" -Version 1.0.0 -Description "Valid user name and password"
```

Creates a test case in the current project. The name contains spaces so it must be quoted.

### Example 2: Create several test cases with labels

```powershell
PS C:\> New-TmTestCase -Path Orch1Tm:\MTP Login, Logout -Labels smoke, auth
```

Creates the test cases "Login" and "Logout" in the project MTP, each with the labels "smoke" and "auth".

### Example 3: Preview the creation

```powershell
PS Orch1Tm:\MTP> New-TmTestCase Login -WhatIf
```

Shows the test case that would be created, without creating it.

## PARAMETERS

### -Name

Specifies the names of the test cases to create. One test case is created for each name.
This parameter is mandatory.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: false
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

### -Version

Specifies the version of the test cases, as free text (for example 1.0.0). If omitted, the test cases have no version.

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

Specifies the description of the test cases.

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

Specifies the names of the labels to add to the test cases. Each value is split on commas, so one CSV cell can hold several labels; write a comma that is part of a label name as `` `, ``.

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

### UiPath.PowerShell.Entities.TmTestCase

The created test cases.

## NOTES

This cmdlet operates on the UiPathOrchTm provider PSDrive. Supports -WhatIf to preview which test cases would be created.

## RELATED LINKS

[Get-TmTestCase](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-TmTestCase.md)

[Update-TmTestCase](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Update-TmTestCase.md)

[Remove-TmTestCase](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-TmTestCase.md)
