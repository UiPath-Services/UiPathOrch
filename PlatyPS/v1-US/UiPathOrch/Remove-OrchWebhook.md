---
document type: cmdlet
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Remove-OrchWebhook
---

# Remove-OrchWebhook

## SYNOPSIS

Removes webhooks from UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
Remove-OrchWebhook [-Name] <string[]> [-Path <string[]>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Removes webhooks from UiPath Orchestrator. The webhooks matching the specified names in the target drives are permanently deleted.

The -Name parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available webhook names dynamically populated from the target drives. Multiple values can be specified using comma-separated text that includes wildcards.

This cmdlet supports ShouldProcess. Use -WhatIf to preview which webhooks would be removed, or -Confirm to be prompted before each removal.

Primary Endpoint: DELETE /odata/Webhooks({webhookId})

OAuth required scopes: OR.Webhooks

Required permissions: Webhooks.Delete

## EXAMPLES

### Example 1: Remove a specific webhook

```powershell
PS Orch1:\> Remove-OrchWebhook OldNotifier
```

Removes the webhook named "OldNotifier" from the current drive.

### Example 2: Remove webhooks using a wildcard

```powershell
PS Orch1:\> Remove-OrchWebhook *Test*
```

Removes all webhooks whose name contains "Test" from the current drive.

### Example 3: Preview removal with WhatIf

```powershell
PS Orch1:\> Remove-OrchWebhook *Legacy* -WhatIf
```

Displays what would happen if all webhooks matching "*Legacy*" were removed, without actually deleting them. Useful for verifying wildcard matches before execution.

### Example 4: Remove a webhook from a specific drive

```powershell
PS C:\> Remove-OrchWebhook -Path Orch1: -Name ObsoleteHook
```

Removes the webhook named "ObsoleteHook" from the Orch1 drive. When -Path uses an absolute path, the command can be run from any location.

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

### -Name

Specifies the names of the webhooks to remove. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests webhook names from the target drives. This parameter is mandatory and positional (position 0).

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

This cmdlet does not produce output. Webhooks are deleted from the Orchestrator server.

## NOTES

Webhooks are tenant-scoped entities. Use an Orch: drive or -Path to specify the target Orchestrator instance.

Removing a webhook is a permanent operation. Use -WhatIf to preview which webhooks would be affected before executing the command.

## RELATED LINKS

Get-OrchWebhook

Copy-OrchWebhook
