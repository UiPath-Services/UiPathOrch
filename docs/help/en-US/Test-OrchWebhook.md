---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Test-OrchWebhook.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 04/28/2026
PlatyPS schema version: 2024-05-01
title: Test-OrchWebhook
---

# Test-OrchWebhook

## SYNOPSIS

Sends a Ping event to a webhook to verify connectivity.

## SYNTAX

### __AllParameterSets

```
Test-OrchWebhook [-Name] <string[]> [-Path <string[]>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Triggers a ping delivery against a registered webhook so you can verify the destination URL is reachable, the payload is parsed correctly, and any signature/auth handshake works end-to-end. Orchestrator constructs a synthetic ping event, signs it with the webhook secret if configured, and POSTs it to the webhook URL.

This is the same operation that the Orchestrator UI invokes from the "Test" / "Ping" button on the Webhooks page. The cmdlet returns the PingEventDto that Orchestrator generated, including the EventId and timestamps, so you can correlate the delivery on the receiving side.

The -Name parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available webhook names.

Primary Endpoint: POST /odata/Webhooks({key})/UiPath.Server.Configuration.OData.Ping

OAuth required scopes: OR.Webhooks or OR.Webhooks.Write

Required permissions: Webhooks.View

## EXAMPLES

### Example 1: Ping a single webhook by name

```powershell
PS Orch1:\> Test-OrchWebhook mywebhook
```

Sends a ping event to the webhook named "mywebhook" and returns the PingEventDto on success.

### Example 2: Ping every webhook whose name starts with "prod-"

```powershell
PS Orch1:\> Test-OrchWebhook 'prod-*'
```

Pings every webhook whose name starts with "prod-" using a wildcard.

### Example 3: Preview without sending

```powershell
PS Orch1:\> Test-OrchWebhook mywebhook -WhatIf
```

Shows what would happen without actually sending the ping.

## PARAMETERS

### -Name

Specifies the names of the webhooks to ping. Supports wildcards and multiple comma-separated values. Tab completion suggests webhook names from the target drives.

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

### -Path

Specifies the name of the target drives. If not specified, the current drive is targeted.

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

### System.String[]

You can pipe webhook names to this cmdlet via the Name property.

## OUTPUTS

### UiPath.PowerShell.Entities.WebhookPingResult

Returns the PingEventDto that Orchestrator generated, including Type, EventId, EntityKey, Timestamp, EventTime, EventSourceId, TenantId, and OrganizationUnitId.

## NOTES

Pinging an Enabled webhook will fire a real HTTP delivery to the configured URL. Plan accordingly if the webhook target is production.

## RELATED LINKS

Get-OrchWebhook

Get-OrchWebhookEventType

Enable-OrchWebhook

Disable-OrchWebhook
