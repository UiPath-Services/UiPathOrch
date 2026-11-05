---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-TmRequirement.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 11/04/2026
PlatyPS schema version: 2024-05-01
title: Copy-TmRequirement
---

# Copy-TmRequirement

## SYNOPSIS

Copies Test Manager requirements, with their test case assignments, to another project.

## SYNTAX

### __AllParameterSets

```
Copy-TmRequirement [-Path <string[]>] [-LiteralPath <string[]>] [-Name] <string[]>
 [-Destination] <string> [-Confirm] [-Force] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Copies the requirements that match `-Name`, from the Test Manager project at the current location or at `-Path`, into the project at `-Destination`.
The destination can be in another tenant or organization: any project on a UiPathOrchTm drive.
Each requirement is copied with its name, description, foreign reference and labels, and with its test case assignments.

The assigned test cases themselves are not copied. Each one is looked up in the destination by its name and foreign reference, and assigned when exactly one test case there has them (and only one in the source does).
Copy the test cases first, with `Copy-TmTestCase`, or afterwards: `Copy-TmTestCase` adds the test cases it copies to the matching requirements, so either order gives the same assignments. An assigned test case with no counterpart is reported with a warning.

Requirements are matched with the destination's by their name together with their foreign reference, both exactly (case included; leading and trailing spaces are ignored, and an empty foreign reference matches an empty one), since Test Manager allows neither to be unique:

- When no requirement in the destination has the same name and foreign reference, the requirement is created (also when the source has several).
- When exactly one in the source and one in the destination have them, the destination requirement is reused instead of created, and only its missing labels and assignments are added. So a copy that failed partway can simply be run again.
- Otherwise -- several on either side, and at least one in the destination -- it is not known which is which: the requirements are not copied, with a warning. With `-Force` they are created as new requirements (a one-to-one match is still reused).

Not copied: the connector link to an external tool (Jira, Azure DevOps and so on), custom fields, attachments and defects.

This cmdlet operates on the PSDrive of the UiPathOrchTm provider.
If the scope in the configuration file includes "TM.", the PSDrive of the UiPathOrchTm provider will be automatically added.
You can confirm this with the Get-PSDrive cmdlet.
The configuration file can be opened with the Edit-OrchConfig cmdlet.

Primary Endpoint: POST /testmanager_/api/v2/{projectId}/requirements

Assignment Endpoint: POST /testmanager_/api/v2/{projectId}/requirements/{id}/assigntestcases

Labels Endpoint: POST /testmanager_/api/v2/{projectId}/objectlabels/requirement/addlabels

OAuth Required scopes: TM.Requirements

## EXAMPLES

### Example 1: Copy test cases, then requirements, to another tenant

```powershell
PS Orch1Tm:\MTP> Copy-TmTestCase * Orch2Tm:\MTP
PS Orch1Tm:\MTP> Copy-TmRequirement * Orch2Tm:\MTP
```

Copies all test cases, then all requirements with their assignments, into the project MTP of the drive Orch2Tm:.

### Example 2: Preview the copy

```powershell
PS Orch1Tm:\MTP> Copy-TmRequirement * Orch2Tm:\MTP -WhatIf
```

Shows the requirements that would be copied, without copying them.

## PARAMETERS

### -Name

Specifies the names of the requirements to copy. Wildcards are permitted.
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

Creates the requirements that cannot be matched one-to-one with the destination's -- several requirements with the same name and foreign reference on either side -- as new requirements, instead of skipping them.
A requirement that matches exactly one in the destination is still reused.

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

You can pipe requirement names or project paths to this cmdlet.

## OUTPUTS

### None

This cmdlet does not output anything.

## NOTES

This cmdlet operates on the UiPathOrchTm provider PSDrive. Supports -WhatIf to preview which requirements would be copied.
To copy a whole project -- requirements, test cases with their steps, test sets, assignments, labels and project settings -- use `Copy-Item Orch1Tm:\MTP Orch2Tm:\`.

## RELATED LINKS

[Get-TmRequirement](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-TmRequirement.md)

[Copy-TmTestCase](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-TmTestCase.md)

[Copy-TmTestSet](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-TmTestSet.md)
