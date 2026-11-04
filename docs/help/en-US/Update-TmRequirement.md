---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Update-TmRequirement.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 10/30/2026
PlatyPS schema version: 2024-05-01
title: Update-TmRequirement
---

# Update-TmRequirement

## SYNOPSIS

Updates the name, description or labels of requirements in Test Manager.

## SYNTAX

### __AllParameterSets

```
Update-TmRequirement [-Path <string[]>] [-LiteralPath <string[]>] [-Recurse]
 [-Name] <string[]> [-Confirm] [-Description <string>] [-Labels <string[]>]
 [-NewName <string>] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Updates the requirements identified by the `-Name` parameter, which supports wildcard matching.
Only the fields given are changed: the name and the description are sent as a JSON Patch, one operation per field that differs from its current value, so a request that changes nothing sends nothing.
`-Labels` replaces the requirement's labels with the given list. An empty list leaves the labels unchanged.
Matched requirements are processed in object key order.
This cmdlet operates on the PSDrive of the UiPathOrchTm provider.
If the scope in the configuration file includes "TM.", the PSDrive of the UiPathOrchTm provider will be automatically added.
You can confirm this with the Get-PSDrive cmdlet.
The configuration file can be opened with the Edit-OrchConfig cmdlet.

Primary Endpoint: PATCH /testmanager_/api/v2/{projectId}/requirements/{requirementId}

Labels Endpoint: POST /testmanager_/api/v2/{projectId}/objectlabels/requirement/addlabels

OAuth Required scopes: TM.Requirements

## EXAMPLES

### Example 1: Rename a requirement

```powershell
PS Orch1Tm:\MTP> Update-TmRequirement Login -NewName "User can sign in"
```

Renames the requirement "Login" in the current project.

### Example 2: Change the description of several requirements

```powershell
PS C:\> Update-TmRequirement -Path Orch1Tm:\MTP Auth* -Description "Reviewed on 2026-10-30"
```

Sets the description of every requirement whose name starts with "Auth" in the project MTP.

### Example 3: Replace the labels

```powershell
PS Orch1Tm:\MTP> Update-TmRequirement Login -Labels regression
```

Replaces the labels of the requirement "Login" with the single label "regression".

### Example 4: Update requirements from the pipeline

```powershell
PS Orch1Tm:\MTP> Get-TmRequirement Log* | Update-TmRequirement -Description "Owned by the auth team"
```

The name and the project path of each requirement are bound by property name.

## PARAMETERS

### -Name

Specifies the names of the requirements to update.
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

Specifies the new name of the requirements.

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

Specifies the new description of the requirements. An empty string clears the description.

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

Specifies the names of the labels that replace the requirements' current labels. Each value is split on commas, so one CSV cell can hold several labels; write a comma that is part of a label name as `` `, ``.

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

You can pipe requirement names or paths to this cmdlet.

## OUTPUTS

### None

This cmdlet does not produce pipeline output.

## NOTES

This cmdlet operates on the UiPathOrchTm provider PSDrive. Supports -WhatIf to preview which requirements would be updated.

## RELATED LINKS

[Get-TmRequirement](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-TmRequirement.md)

[New-TmRequirement](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/New-TmRequirement.md)

[Remove-TmRequirement](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-TmRequirement.md)
