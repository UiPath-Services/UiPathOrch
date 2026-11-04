---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Update-TmTestSet.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 10/30/2026
PlatyPS schema version: 2024-05-01
title: Update-TmTestSet
---

# Update-TmTestSet

## SYNOPSIS

Updates the name, description, activity coverage or labels of test sets in Test Manager.

## SYNTAX

### __AllParameterSets

```
Update-TmTestSet [-Path <string[]>] [-LiteralPath <string[]>] [-Recurse]
 [-Name] <string[]> [-Confirm] [-Description <string>] [-EnableCoverage <string>]
 [-Labels <string[]>] [-NewName <string>] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Updates the test sets identified by the `-Name` parameter, which supports wildcard matching.
Only the fields given are changed: the name, the description and "Enable RPA activity coverage calculation" (`-EnableCoverage`, the test set's Configuration tab in the web app) are sent as a JSON Patch, one operation per field that differs from its current value, so a request that changes nothing sends nothing.
`-Labels` replaces the test set's labels with the given list. An empty list leaves the labels unchanged.
Matched test sets are processed in object key order.
This cmdlet operates on the PSDrive of the UiPathOrchTm provider.
If the scope in the configuration file includes "TM.", the PSDrive of the UiPathOrchTm provider will be automatically added.
You can confirm this with the Get-PSDrive cmdlet.
The configuration file can be opened with the Edit-OrchConfig cmdlet.

Primary Endpoint: PATCH /testmanager_/api/v2/{projectId}/testsets/{testSetId}

Labels Endpoint: POST /testmanager_/api/v2/{projectId}/objectlabels/testSet/addlabels

OAuth Required scopes: TM.TestSets

## EXAMPLES

### Example 1: Rename a test set

```powershell
PS Orch1Tm:\MTP> Update-TmTestSet Smoke -NewName "Smoke tests"
```

Renames the test set "Smoke" in the current project.

### Example 2: Turn on activity coverage for several test sets

```powershell
PS C:\> Update-TmTestSet -Path Orch1Tm:\MTP Nightly* -EnableCoverage true
```

Turns on "Enable RPA activity coverage calculation" for every test set whose name starts with "Nightly" in the project MTP.

### Example 3: Replace the labels

```powershell
PS Orch1Tm:\MTP> Update-TmTestSet Smoke -Labels release-1.1
```

Replaces the labels of the test set "Smoke" with the single label "release-1.1".

### Example 4: Update test sets from the pipeline

```powershell
PS Orch1Tm:\MTP> Get-TmTestSet Nightly* | Update-TmTestSet -Description "Owned by the QA team"
```

The name and the project path of each test set are bound by property name.

## PARAMETERS

### -Name

Specifies the names of the test sets to update.
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

Specifies the new name of the test sets.

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

Specifies the new description of the test sets. An empty string clears the description.

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

Turns "Enable RPA activity coverage calculation" on (true) or off (false) for the test sets.

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

Specifies the names of the labels that replace the test sets' current labels. Each value is split on commas, so one CSV cell can hold several labels; write a comma that is part of a label name as `` `, ``.

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

You can pipe test set names or paths to this cmdlet.

## OUTPUTS

### None

This cmdlet does not produce pipeline output.

## NOTES

This cmdlet operates on the UiPathOrchTm provider PSDrive. Supports -WhatIf to preview which test sets would be updated.

## RELATED LINKS

[Get-TmTestSet](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-TmTestSet.md)

[New-TmTestSet](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/New-TmTestSet.md)

[Remove-TmTestSet](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-TmTestSet.md)
