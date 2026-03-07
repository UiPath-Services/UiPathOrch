---
document type: cmdlet
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchWebhook
---

# Get-OrchWebhook

## SYNOPSIS

Gets webhooks from UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
Get-OrchWebhook [[-Name] <string[]>] [-Path <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets webhook information from UiPath Orchestrator. Webhooks allow Orchestrator to send HTTP POST notifications to external URLs when specific events occur (e.g., job completion, queue item creation, robot status changes).

The -Name parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available webhook names dynamically populated from the target drives. Multiple values can be specified using comma-separated text that includes wildcards.

Primary Endpoint: GET /odata/Webhooks

OAuth required scopes: OR.Webhooks or OR.Webhooks.Read

Required permissions: Webhooks.View

## EXAMPLES

### Example 1: Get all webhooks

```powershell
PS Orch1:\> Get-OrchWebhook
```

Gets all webhooks from the current Orchestrator drive and returns webhook information including Name, Url, Enabled, and event subscriptions.

### Example 2: Get a webhook by name

```powershell
PS Orch1:\> Get-OrchWebhook mywebhook
```

Gets the webhook named "mywebhook" from the current drive. The -Name parameter is positional (position 0).

### Example 3: Get webhooks using a wildcard

```powershell
PS Orch1:\> Get-OrchWebhook *web*
```

Gets all webhooks whose name contains "web" from the current drive.

### Example 4: Get webhooks from a specific drive

```powershell
PS C:\> Get-OrchWebhook -Path Orch1: -Name my*
```

Gets webhooks whose name starts with "my" from the Orch1 drive. When -Path uses an absolute path, the command can be run from any location.

## PARAMETERS

### -Path

Specifies the name of the target drives.
If not specified, the current drive will be targeted.

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

### -Name

Specifies the names of the webhooks to retrieve. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests webhook names from the target drives.

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe webhook names to this cmdlet via the Name property.

## OUTPUTS

### UiPath.PowerShell.Entities.Webhook

Returns Webhook objects with properties including Name, Url, Enabled, Secret, SubscribeToAllEvents, and AllowInsecureSsl.

## NOTES

Webhooks are tenant-scoped entities. Use an Orch: drive or -Path to specify the target Orchestrator instance.

## RELATED LINKS

Remove-OrchWebhook

Copy-OrchWebhook

Enable-OrchWebhook

Disable-OrchWebhook
