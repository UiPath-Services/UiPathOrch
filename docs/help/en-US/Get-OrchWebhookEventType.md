---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchWebhookEventType.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 04/28/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchWebhookEventType
---

# Get-OrchWebhookEventType

## SYNOPSIS

Lists the event types that webhooks can subscribe to.

## SYNTAX

### __AllParameterSets

```
Get-OrchWebhookEventType [[-Name] <string[]>] [-Path <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Returns the catalog of event types a webhook can subscribe to (job lifecycle, queue item state changes, robot connectivity, schedule events, task events, etc.). Use this cmdlet to discover the exact event names and groups before creating or updating a webhook with `New-OrchWebhook` or `Update-OrchWebhook`.

The -Name parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see the available event-type names from the target drives. Multiple values can be specified using comma-separated text that includes wildcards.

Primary Endpoint: GET /odata/Webhooks/UiPath.Server.Configuration.OData.GetEventTypes

OAuth required scopes: OR.Webhooks or OR.Webhooks.Read

Required permissions: Webhooks.View

## EXAMPLES

### Example 1: List all event types

```powershell
PS Orch1:\> Get-OrchWebhookEventType
```

Returns the full set of event types available in the tenant, grouped by domain (job, queue, robot, schedule, task, etc.).

### Example 2: Filter by group prefix using a wildcard

```powershell
PS Orch1:\> Get-OrchWebhookEventType job.*
```

Returns only the job-related event types (e.g., job.created, job.started, job.completed, job.faulted, job.stopped).

### Example 3: Group event types by domain

```powershell
PS Orch1:\> Get-OrchWebhookEventType | Group-Object Group
```

Tabulates how many event types each group exposes.

## PARAMETERS

### -Name

Specifies the names of the event types to retrieve. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests event-type names cached from the target drives.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: (All)
  Position: 0
  IsRequired: false
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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe event-type names to this cmdlet via the Name property.

## OUTPUTS

### UiPath.PowerShell.Entities.WebhookEventType

Returns WebhookEventType objects with Name and Group properties.

## NOTES

The event-type catalog is tenant-scoped reference data and is cached per drive. Use Clear-OrchCache if a tenant-side configuration change introduces new event types and you want to refresh the local cache.

## RELATED LINKS

[Get-OrchWebhook](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchWebhook.md)

[Test-OrchWebhook](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Test-OrchWebhook.md)
