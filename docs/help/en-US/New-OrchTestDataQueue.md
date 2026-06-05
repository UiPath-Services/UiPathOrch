---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/New-OrchTestDataQueue.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 05/21/2026
PlatyPS schema version: 2024-05-01
title: New-OrchTestDataQueue
---

# New-OrchTestDataQueue

## SYNOPSIS

Creates a new test data queue in UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
New-OrchTestDataQueue [-Path <string[]>] [-LiteralPath <string[]>] [-Name] <string[]>
 [-Confirm] [-ContentJsonSchema <string>] [-Description <string>] [-WhatIf]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Creates a test data queue in the target folder. Test data queues are used to feed parameterised test cases.

The cmdlet defaults -ContentJsonSchema to `{}` (the empty JSON schema, i.e. `any value`) when omitted. The server requires this field to be present, so the default keeps barebones invocations working.

This cmdlet supports ShouldProcess. Use -WhatIf to preview or -Confirm to be prompted.

Primary Endpoint: POST /odata/TestDataQueues

OAuth required scopes: OR.TestDataQueues or OR.TestDataQueues.Write

Required permissions: TestDataQueues.Create

## EXAMPLES

### Example 1: Create a queue with no schema constraint

```powershell
PS Orch1:\Shared> New-OrchTestDataQueue MyDataQueue
```

Creates a test data queue named "MyDataQueue" that accepts items of any shape (ContentJsonSchema defaults to `{}`).

### Example 2: Create a queue with a typed schema

```powershell
PS Orch1:\Shared> New-OrchTestDataQueue MyOrders -ContentJsonSchema '{"type":"object","properties":{"OrderId":{"type":"string"},"Amount":{"type":"number"}}}'
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

### -ContentJsonSchema

JSON schema constraining items added to this queue. Defaults to "{}" (any value) when omitted, which is required by the server.

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

### -Description

A free-form description stored on the entity.

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

Specifies the Name(s) of the test data queue to create.

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

You can pipe queue names to this cmdlet.

### System.String[]

You can pipe queue names to this cmdlet.

## OUTPUTS

### UiPath.PowerShell.Entities.TestDataQueue

Returns the created TestDataQueue entity on success.

## NOTES

Main endpoint called: POST /odata/TestDataQueues

-ContentJsonSchema defaults to `{}` when omitted — the server returns 400 `The ContentJsonSchema field is required.` if the field is missing.

## RELATED LINKS

[Get-OrchTestDataQueue](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchTestDataQueue.md)

[Copy-OrchTestDataQueue](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchTestDataQueue.md)

[Remove-OrchTestDataQueue](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchTestDataQueue.md)

[Get-OrchTestDataQueueItem](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchTestDataQueueItem.md)

