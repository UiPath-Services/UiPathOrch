---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/New-TmTestSet.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 10/30/2026
PlatyPS schema version: 2024-05-01
title: New-TmTestSet
---

# New-TmTestSet

## SYNOPSIS

Creates test sets in a Test Manager project.

## SYNTAX

### __AllParameterSets

```
New-TmTestSet [-Path <string[]>] [-LiteralPath <string[]>] [-Name] <string[]> [-Confirm]
 [-Description <string>] [-EnableCoverage <string>] [-Labels <string[]>] [-WhatIf]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Creates one test set for each name given to the `-Name` parameter, in the Test Manager project at the current location or at `-Path`.
The fields match the Test Manager web form for a new test set: Name, Description, Enable RPA activity coverage calculation (`-EnableCoverage`) and Labels.
Labels are added by a second request once the test set exists. If that request fails, an error is reported, and the test set is still output without its labels.
The created test sets are output, so they can be piped to `Update-TmTestSet` or `Remove-TmTestSet`.
This cmdlet operates on the PSDrive of the UiPathOrchTm provider.
If the scope in the configuration file includes "TM.", the PSDrive of the UiPathOrchTm provider will be automatically added.
You can confirm this with the Get-PSDrive cmdlet.
The configuration file can be opened with the Edit-OrchConfig cmdlet.

Primary Endpoint: POST /testmanager_/api/v2/{projectId}/testsets

Labels Endpoint: POST /testmanager_/api/v2/{projectId}/objectlabels/testSet/addlabels

OAuth Required scopes: TM.TestSets

## EXAMPLES

### Example 1: Create a test set

```powershell
PS Orch1Tm:\MTP> New-TmTestSet "Nightly regression" -Description "Runs every night" -EnableCoverage true
```

Creates a test set in the current project. The name contains spaces so it must be quoted.

### Example 2: Create several test sets with labels

```powershell
PS C:\> New-TmTestSet -Path Orch1Tm:\MTP Smoke, Regression -Labels release-1.0
```

Creates the test sets "Smoke" and "Regression" in the project MTP, each with the label "release-1.0".

### Example 3: Preview the creation

```powershell
PS Orch1Tm:\MTP> New-TmTestSet Smoke -WhatIf
```

Shows the test set that would be created, without creating it.

## PARAMETERS

### -Name

Specifies the names of the test sets to create. One test set is created for each name.
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

### -Description

Specifies the description of the test sets.

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

### -EnableCoverage

Turns on "Enable RPA activity coverage calculation" for the test sets: true or false. Off when omitted.

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
AcceptedValues:
- 'True'
- 'False'
HelpMessage: ''
```

### -Labels

Specifies the names of the labels to add to the test sets. Each value is split on commas, so one CSV cell can hold several labels; write a comma that is part of a label name as `` `, ``.

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

You can pipe test set names or project paths to this cmdlet.

## OUTPUTS

### UiPath.PowerShell.Entities.TmTestSet

The created test sets.

## NOTES

This cmdlet operates on the UiPathOrchTm provider PSDrive. Supports -WhatIf to preview which test sets would be created.

## RELATED LINKS

[Get-TmTestSet](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-TmTestSet.md)

[Update-TmTestSet](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Update-TmTestSet.md)

[Remove-TmTestSet](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-TmTestSet.md)
