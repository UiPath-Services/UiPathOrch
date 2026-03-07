---
document type: cmdlet
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Copy-OrchWebhook
---

# Copy-OrchWebhook

## SYNOPSIS

Copies webhooks to another Orchestrator drive.

## SYNTAX

### __AllParameterSets

```
Copy-OrchWebhook [-Name] <string[]> [-Destination] <string[]> [-Path <string>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Copies webhooks from one UiPath Orchestrator drive to another. The cmdlet creates a new webhook in the destination drive using the same configuration as the source webhook, including Url, event subscriptions, Enabled state, Secret, and AllowInsecureSsl settings. Cross-drive copy is supported, allowing webhooks to be copied between different Orchestrator instances (e.g., from Orch1: to Orch2:).

The -Name, -Destination, and -Path parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values. The -Name completion is dynamically populated from actual webhook names in the source drive. Multiple values can be specified using comma-separated text that includes wildcards.

This cmdlet supports ShouldProcess. Use -WhatIf to preview which webhooks would be copied, or -Confirm to be prompted before each copy operation.

Primary Endpoint: GET /odata/Webhooks, POST /odata/Webhooks

OAuth required scopes: OR.Webhooks or OR.Webhooks.Read (source), OR.Webhooks (destination)

Required permissions: Webhooks.View (source), Webhooks.Create (destination)

## EXAMPLES

### Example 1: Copy a webhook to another drive

```powershell
PS Orch1:\> Copy-OrchWebhook mywebhook Orch2:
```

Copies the webhook named "mywebhook" from the current drive (Orch1) to the Orch2 drive.

### Example 2: Copy webhooks using a wildcard

```powershell
PS Orch1:\> Copy-OrchWebhook my* Orch2:
```

Copies all webhooks matching "my*" from the current drive to the Orch2 drive.

### Example 3: Copy a webhook across Orchestrator instances

```powershell
PS C:\> Copy-OrchWebhook -Path Orch1: -Name mywebhook -Destination Orch2:
```

Copies the webhook named "mywebhook" from Orch1 to Orch2. Webhook settings including Url, event subscriptions, and AllowInsecureSsl are carried over.

### Example 4: Preview copy with WhatIf

```powershell
PS Orch1:\> Copy-OrchWebhook * Orch2: -WhatIf
```

Shows what would happen without actually copying. Useful for verifying which webhooks would be affected before execution.

## PARAMETERS

### -Path

Specifies the source drive name.
If not specified, the current drive will be used as the source.

```yaml
Type: System.String
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

### -Destination

Specifies the destination drive names where webhooks will be copied. Supports wildcards. Can reference a different Orchestrator drive (e.g., Orch2:) for cross-instance copy.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
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

### -Name

Specifies the names of the webhooks to copy. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests webhook names from the source drive. This parameter is mandatory and positional (position 0).

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

### System.String

You can pipe a destination drive path to this cmdlet via the Destination property.

## OUTPUTS

### UiPath.PowerShell.Entities.Webhook

Returns the newly created Webhook object in the destination drive.

## NOTES

Webhooks are tenant-scoped entities. Use an Orch: drive or -Path to specify the source Orchestrator instance.

When performing cross-drive copy (e.g., Orch1: to Orch2:), the cmdlet copies webhook settings including Url, event subscriptions, Enabled state, and AllowInsecureSsl. The destination Orchestrator must be accessible and the webhook Url must be reachable from the destination environment.

## RELATED LINKS

Get-OrchWebhook

Remove-OrchWebhook

Enable-OrchWebhook

Disable-OrchWebhook
