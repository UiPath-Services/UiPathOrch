---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/New-OrchWebhook.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 05/22/2026
PlatyPS schema version: 2024-05-01
title: New-OrchWebhook
---

# New-OrchWebhook

## SYNOPSIS

Creates a new webhook in UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
New-OrchWebhook [-Path <string[]>] [-LiteralPath <string[]>] [-Name] <string[]>
 [-Url] <string> [-AllowInsecureSsl <string>] [-Confirm] [-Description <string>]
 [-Enabled <string>] [-Events <string[]>] [-Secret <string>]
 [-SubscribeToAllEvents <string>] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Creates a webhook subscription on the target drive. A webhook POSTs event notifications to the URL you supply whenever subscribed events occur.

Parameter surface mirrors `Update-OrchWebhook`, so a CSV emitted by `Get-OrchWebhook -ExportCsv` re-imports into either cmdlet. `-Name` and `-Url` are mandatory. When neither `-SubscribeToAllEvents` nor a specific event list is supplied, the cmdlet defaults `SubscribeToAllEvents` to `true` so the new webhook actually fires.

This cmdlet supports ShouldProcess. Use -WhatIf to preview or -Confirm to be prompted.

Primary Endpoint: POST /odata/Webhooks

OAuth required scopes: OR.Webhooks or OR.Webhooks.Write

Required permissions: Webhooks.Create

## EXAMPLES

### Example 1: Create a webhook subscribed to all events

```powershell
PS Orch1:\> New-OrchWebhook -Name MyHook -Url https://example.com/uipath-hook
```

Creates a webhook on the `Orch1:` tenant that POSTs every event to the given URL.

### Example 2: Create a disabled webhook with a signing secret

```powershell
PS Orch1:\> New-OrchWebhook -Name MyHook -Url https://example.com/hook -Secret 's3cr3t' -Enabled false
```

### Example 3: Bulk create from a CSV exported elsewhere

```powershell
PS C:\> Import-Csv webhooks.csv | New-OrchWebhook
```

Pipes rows from `Get-OrchWebhook -ExportCsv` into creation. Every exported column maps to a parameter, so the same CSV round-trips into another tenant.

## PARAMETERS

### -AllowInsecureSsl

Whether to skip TLS certificate validation on delivery. Accepts "true" or "false".

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

### -Enabled

Whether the entity is enabled. Accepts "true" or "false".

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

### -Events

Specific event types to subscribe to (e.g. `task.created`, `job.completed`). Wildcards are expanded against the live event-type list, so `-Events task.*,job.completed` subscribes to all task events plus job.completed. A single `;`-joined value (as produced by `Get-OrchWebhook -ExportCsv`) is accepted too. Supplying `-Events` sets `SubscribeToAllEvents` to false unless that switch is given explicitly. Use `Get-OrchWebhookEventType` to list valid values.

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

### -Name

Specifies the name of the webhook to create.

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

Specifies the target. Webhooks are drive-scoped (e.g. Orch1:). Supports wildcards.

```yaml
Type: System.String[]
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

### -Secret

Shared secret used to sign webhook deliveries so the receiver can verify authenticity.

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

### -SubscribeToAllEvents

Whether the webhook fires on every event type. New-OrchWebhook defaults this to true when omitted.

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

### -Url

The HTTPS endpoint the webhook POSTs to. Mandatory on New-OrchWebhook.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 1
  IsRequired: true
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

You can pipe values to this cmdlet by property name.

### System.String[]

You can pipe names to this cmdlet.

## OUTPUTS

### UiPath.PowerShell.Entities.Webhook

Returns the created Webhook entity on success.

## NOTES

Main endpoint called: POST /odata/Webhooks

Name/Description/Key are honoured on OC 16.0+; on older builds the server ignores them. SubscribeToAllEvents defaults to true when omitted.

## RELATED LINKS

[Get-OrchWebhook](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchWebhook.md)

[Update-OrchWebhook](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Update-OrchWebhook.md)

[Copy-OrchWebhook](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchWebhook.md)

[Remove-OrchWebhook](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchWebhook.md)

[Enable-OrchWebhook](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Enable-OrchWebhook.md)

[Disable-OrchWebhook](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Disable-OrchWebhook.md)

[Test-OrchWebhook](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Test-OrchWebhook.md)

