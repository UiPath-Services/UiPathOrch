---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Enable-OrchWebhook.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Enable-OrchWebhook
---

# Enable-OrchWebhook

## SYNOPSIS

Enables webhooks in UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
Enable-OrchWebhook [-Path <string[]>] [-LiteralPath <string[]>] [-Name] <string[]> [-Confirm] [-WhatIf]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Enables disabled webhooks in UiPath Orchestrator. Only webhooks that are currently disabled are processed; webhooks that are already enabled are skipped.

The -Name parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available webhook names dynamically populated from disabled webhooks in the target drives. Tab completion filters based on current state, showing only disabled webhooks as candidates. Multiple values can be specified using comma-separated text that includes wildcards.

This cmdlet supports ShouldProcess. Use -WhatIf to preview which webhooks would be enabled, or -Confirm to be prompted before each operation.

Primary Endpoint: PATCH /odata/Webhooks({webhookId})

OAuth required scopes: OR.Webhooks

Required permissions: Webhooks.Edit

## EXAMPLES

### Example 1: Enable a specific webhook

```powershell
PS Orch1:\> Enable-OrchWebhook mywebhook
```

Enables the webhook named "mywebhook" on the current drive. If the webhook is already enabled, it is skipped.

### Example 2: Enable webhooks using a wildcard

```powershell
PS Orch1:\> Enable-OrchWebhook my*
```

Enables all disabled webhooks whose name matches "my*" on the current drive.

### Example 3: Enable all disabled webhooks

```powershell
PS Orch1:\> Enable-OrchWebhook *
```

Enables all disabled webhooks on the current drive. Webhooks that are already enabled are silently skipped.

### Example 4: Enable a webhook on a specific drive

```powershell
PS C:\> Enable-OrchWebhook -Path Orch1: mywebhook
```

Enables the webhook named "mywebhook" on the Orch1 drive. When -Path uses an absolute path, the command can be run from any location.

## PARAMETERS

### -Path

Specifies the name of the target drives.
If not specified, the current drive is targeted.

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

### -Name

Specifies the names of the webhooks to enable. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests names of disabled webhooks from the target drives. This parameter is mandatory and positional (position 0).

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe webhook names to this cmdlet via the Name property.

## OUTPUTS

### None

This cmdlet does not produce output. Webhooks are enabled on the Orchestrator server.

## NOTES

Webhooks are tenant-scoped entities. Use an Orch: drive or -Path to specify the target Orchestrator instance.

Only disabled webhooks are processed. Webhooks that are already enabled are silently skipped.

## RELATED LINKS

[Disable-OrchWebhook](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Disable-OrchWebhook.md)

[Get-OrchWebhook](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchWebhook.md)

[Remove-OrchWebhook](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchWebhook.md)
