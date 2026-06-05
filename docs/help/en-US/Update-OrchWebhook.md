---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Update-OrchWebhook.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 05/05/2026
PlatyPS schema version: 2024-05-01
title: Update-OrchWebhook
---

# Update-OrchWebhook

## SYNOPSIS

Updates an existing webhook in UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
Update-OrchWebhook [-Path <string[]>] [-LiteralPath <string[]>] [-Name] <string[]>
 [-AllowInsecureSsl <string>] [-Confirm] [-Description <string>] [-Enabled <string>]
 [-Events <string[]>] [-Secret <string>] [-SubscribeToAllEvents <string>] [-Url <string>]
 [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Updates an existing webhook in UiPath Orchestrator. Only the parameters that are explicitly specified are modified; all other properties are preserved on the server. The cmdlet sends an HTTP PATCH request, so unspecified fields are not touched.

The primary post-migration use case is to re-supply the webhook `Secret`, which the API never returns from `GET /odata/Webhooks` (and therefore is dropped during `Copy-OrchWebhook`). After copying webhooks to a new tenant, run this cmdlet to re-set the HMAC secret used by the receiving service for signature verification.

The `-Name` parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available webhook names dynamically populated from the target tenant. Wildcards and comma-separated values are supported.

`-Enabled` provides the same effect as `Enable-OrchWebhook` / `Disable-OrchWebhook` in a single update operation. Either approach is valid; the dedicated cmdlets are convenient when toggling state is the only action.

Primary Endpoint: PATCH /odata/Webhooks({id})

OAuth required scopes: OR.Webhooks

Required permissions: Webhooks.Edit, Webhooks.View

## EXAMPLES

### Example 1: Re-set the secret after migration

```powershell
PS Destination:\> Update-OrchWebhook -Name 'OrderCompletedHook' -Secret 'super-secret-hmac-key'
```

Re-supplies the HMAC `Secret` on the destination tenant after `Copy-OrchWebhook` carried over everything except the secret. The receiving service must use the same value to validate signed payloads.

### Example 2: Update the URL

```powershell
PS Destination:\> Update-OrchWebhook -Name 'OrderCompletedHook' -Url 'https://new-host.example.com/orch/hook'
```

Points an existing webhook at a new endpoint URL without disturbing its secret, event subscriptions, or enabled state.

### Example 3: Disable a webhook via Update-OrchWebhook

```powershell
PS Destination:\> Update-OrchWebhook -Name 'OrderCompletedHook' -Enabled false
```

Equivalent to `Disable-OrchWebhook -Name 'OrderCompletedHook'`. Use whichever form is more convenient for your script.

### Example 4: Bulk update by wildcard

```powershell
PS Destination:\> Update-OrchWebhook -Name 'Order*' -AllowInsecureSsl false
```

Tightens TLS verification on all webhooks whose name starts with `Order`.

### Example 5: Preview with WhatIf

```powershell
PS Destination:\> Update-OrchWebhook -Name 'Order*' -Secret 'rotation-2026-q2' -WhatIf
```

Reports which webhooks would be updated without performing any change. Useful for verifying a wildcard match before rotating secrets.

## PARAMETERS

### -Path

Specifies the target Orchestrator drive(s). If not specified, the current drive is targeted.

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

### -Name

Specifies the names of the webhooks to update. Supports wildcards and multiple comma-separated values. Tab completion suggests webhook names from the target drive. This parameter is mandatory and positional (position 0).

```yaml
Type: System.String[]
DefaultValue: ''
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

### -Description

Sets a new description for the webhook. The description is shown in the Orchestrator UI and helps identify the purpose of the webhook.

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

Sets a new endpoint URL that Orchestrator will POST events to.

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

### -Secret

Sets the HMAC secret used by the receiving service to verify signed payloads. The API never returns this value, so it must be re-supplied after `Copy-OrchWebhook` migrations.

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

Toggles whether the webhook is active. Tab completion suggests "true" and "false". Equivalent to `Enable-OrchWebhook` / `Disable-OrchWebhook`.

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

### -AllowInsecureSsl

When set to "true", the webhook delivery accepts endpoints with invalid or self-signed TLS certificates. Use only for development; production endpoints should always use a valid certificate. Tab completion suggests "true" and "false".

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

When set to "true", the webhook receives every Orchestrator event type rather than only the configured `Events`. Tab completion suggests "true" and "false".

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

Replaces the webhook's event subscription set. Wildcards are expanded against the live event-type list, so `-Events task.*,job.completed` subscribes to all task events plus job.completed. A single `;`-joined value (as produced by `Get-OrchWebhook -ExportCsv`) is accepted too. The PATCH replaces `Events` wholesale; supplying `-Events` sets `SubscribeToAllEvents` to false unless that switch is given explicitly. Use `Get-OrchWebhookEventType` to list valid values.

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

### -WhatIf

Shows what would happen if the cmdlet runs. The cmdlet is not run.

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe webhook objects (such as CSV import rows) to this cmdlet. Properties are bound by name (Name, Description, Url, Secret, Enabled, AllowInsecureSsl, SubscribeToAllEvents, Events, Path).

## OUTPUTS

### None

This cmdlet does not produce output. Webhooks are updated in place on the Orchestrator server.

## NOTES

Webhooks are tenant-level entities, not folder-scoped. Use `-Path` to target a specific Orchestrator drive when multiple drives are mounted.

Only explicitly specified parameters are sent to the server. Unspecified fields are preserved as-is — there is no risk of clobbering the existing `Secret` by calling this cmdlet to update an unrelated field.

The migration use case is: after `Copy-OrchWebhook` carries everything except the `Secret`, run `Update-OrchWebhook -Name <name> -Secret <new-value>` to restore HMAC signature verification on the destination tenant.

## RELATED LINKS

[Get-OrchWebhook](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchWebhook.md)

[Copy-OrchWebhook](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchWebhook.md)

[Enable-OrchWebhook](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Enable-OrchWebhook.md)

[Disable-OrchWebhook](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Disable-OrchWebhook.md)

[Remove-OrchWebhook](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchWebhook.md)
