---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/New-OrchTaskCatalog.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 05/21/2026
PlatyPS schema version: 2024-05-01
title: New-OrchTaskCatalog
---

# New-OrchTaskCatalog

## SYNOPSIS

Creates a new task catalog in UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
New-OrchTaskCatalog [-Path <string[]>] [-LiteralPath <string[]>] [-Name] <string[]>
 [-Confirm] [-Description <string>] [-Encrypted <string>] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Creates a task catalog in the target folder. Task catalogs group action items (Tasks) for human-in-the-loop workflows.

Formerly New-OrchActionCatalog. The former name still works once the module is loaded, but tab completion does not offer it.

The external noun `TaskCatalog` matches the in-product UI label; the underlying wire entity is `TaskCatalog` (legacy naming).

This cmdlet supports ShouldProcess. Use -WhatIf to preview or -Confirm to be prompted.

Primary Endpoint: POST /odata/TaskCatalogs/UiPath.Server.Configuration.OData.CreateTaskCatalog

OAuth required scopes: OR.Tasks or OR.Tasks.Write

Required permissions: TaskCatalogs.Create

## EXAMPLES

### Example 1: Create a barebones task catalog

```powershell
PS Orch1:\Shared> New-OrchTaskCatalog MyActions
```

Creates a task catalog named "MyActions" in the current folder.

### Example 2: Create an encrypted catalog with a description

```powershell
PS Orch1:\Shared> New-OrchTaskCatalog SecureActions -Description 'Stores sensitive task data' -Encrypted true
```

## PARAMETERS

### -Confirm

Prompts you for confirmation before running the cmdlet.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
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

### -Description

Description shown in the task catalog list.

```yaml
Type: System.String
DefaultValue: ''
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

### -Encrypted

Whether server-side encryption is applied. Accepts "true" or "false".

```yaml
Type: System.String
DefaultValue: ''
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

### -Name

Specifies the Name(s) of the task catalog to create.

```yaml
Type: System.String[]
DefaultValue: ''
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

### -Path

Specifies the target folder(s). If not specified, the current folder is targeted. Supports wildcards.

```yaml
Type: System.String[]
DefaultValue: ''
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

Specifies the target folder or drive by literal path -- wildcard metacharacters (`[`, `]`, `*`, `?`) are treated as literal characters rather than patterns. Accepts the same drive-qualified paths as -Path. Its `PSPath` alias also binds the path of items piped from Get-ChildItem / Get-Item, so you can pipe folders directly. Use -LiteralPath instead of -Path when a folder name contains a wildcard metacharacter.

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

### -WhatIf

Runs the command in a mode that only reports what would happen without performing the actions.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
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

### System.String

You can pipe catalog names to this cmdlet.

### System.String[]

You can pipe catalog names to this cmdlet.

## OUTPUTS

### UiPath.PowerShell.Entities.TaskCatalog

Returns the created TaskCatalog entity on success.

## NOTES

Main endpoint called: POST /odata/TaskCatalogs/UiPath.Server.Configuration.OData.CreateTaskCatalog

The cmdlet noun is `TaskCatalog` (UI label); the wire entity is `TaskCatalog`.

## RELATED LINKS

[Get-OrchTaskCatalog](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchTaskCatalog.md)

[Copy-OrchTaskCatalog](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchTaskCatalog.md)

[Remove-OrchTaskCatalog](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchTaskCatalog.md)

