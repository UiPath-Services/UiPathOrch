---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Copy-OrchWebhook

## SYNOPSIS
Copies webhooks between tenants.

## SYNTAX

```
Copy-OrchWebhook [-Name] <String[]> [-Destination] <String[]> [-Path <String>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The Copy-OrchWebhook cmdlet copies webhooks from source tenants to destination tenants within UiPath Orchestrator. This cmdlet creates copies of webhook configurations, including their endpoint URLs, authentication settings, and event trigger configurations, enabling webhook management across multiple tenant environments.

The cmdlet supports copying webhooks to multiple destination tenants simultaneously. Webhooks can be identified by their Name parameter, and the cmdlet supports wildcard patterns for copying multiple webhooks efficiently.

Use the -Name parameter to specify which webhooks to copy and the -Destination parameter to specify the target tenants. The -Path parameter enables working with multiple source tenants when not operating from within a specific tenant context.

This is a tenant entity cmdlet. The -Path parameter specifies the source drive name (e.g., Orch1:, Orch2:), and -Destination specifies the target tenant drives where webhooks should be copied.

Primary Endpoint: GET /odata/Webhooks, POST /odata/Webhooks

OAuth required scopes: OR.Webhooks

Required permissions: Webhooks.View, Webhooks.Create

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Copy-OrchWebhook ProcessCompletionHook Orch2:
```

Copies the ProcessCompletionHook webhook from the current tenant (Orch1) to Orch2 tenant.

### Example 2
```powershell
PS C:\> Copy-OrchWebhook -Path Orch1: AlertWebhook Orch2:, Orch3:
```

Copies the AlertWebhook from Orch1 to both Orch2 and Orch3 tenants.

### Example 3
```powershell
PS Orch1:\> Copy-OrchWebhook NotificationHook, StatusHook Orch2: -WhatIf
```

Shows what would happen when copying NotificationHook and StatusHook from the current tenant to Orch2.

### Example 4
```powershell
PS C:\> Copy-OrchWebhook -Path Orch1: *Alert* Orch2:
```

Copies all webhooks with names containing Alert from Orch1 to Orch2 using wildcard patterns.

### Example 5
```powershell
PS Orch1:\> Get-OrchWebhook *Notification* | Copy-OrchWebhook -Destination Orch2:, Orch3:
```

Gets all webhooks with names containing Notification and copies them to both Orch2 and Orch3 tenants using pipeline input.

### Example 6
```powershell
PS C:\> Copy-OrchWebhook -Path Orch1: SlackIntegration Orch2: -Confirm
```

Copies the SlackIntegration webhook from Orch1 to Orch2 with confirmation prompts.

## PARAMETERS

### -Confirm
Prompts you for confirmation before running the cmdlet.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: cf

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Destination
Specifies the destination tenant drives where webhooks should be copied.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Name
Specifies the Name of the webhooks to be copied.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
Specifies the source tenant drive. If not specified, the current tenant will be used as the source.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -WhatIf
Shows what would happen if the cmdlet runs.
The cmdlet is not run.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: wi

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ProgressAction
{{ Fill ProgressAction Description }}

```yaml
Type: ActionPreference
Parameter Sets: (All)
Aliases: proga

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

### System.String

## OUTPUTS

### UiPath.PowerShell.Entities.Webhook

## NOTES
This is a tenant entity cmdlet. The -Path parameter specifies drive names (e.g., Orch1:, Orch2:) for source and destination tenants.

Webhooks contain endpoint URLs, authentication settings, and event trigger configurations. When copying across environments, verify that the webhook endpoints are accessible from the destination tenant and update any environment-specific URLs if necessary. Use wildcards for efficient bulk operations and -WhatIf for testing before actual execution.

## RELATED LINKS

[Get-OrchWebhook](Get-OrchWebhook.md)

[Add-OrchWebhook](Add-OrchWebhook.md)

[Remove-OrchWebhook](Remove-OrchWebhook.md)

[Set-OrchWebhook](Set-OrchWebhook.md)
