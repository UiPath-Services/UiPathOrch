---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Copy-OrchCredentialStore

## SYNOPSIS
Copies credential stores between tenants.

## SYNTAX

```
Copy-OrchCredentialStore [-Name] <String[]> [-Destination] <String[]> [-Path <String>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The Copy-OrchCredentialStore cmdlet copies credential stores from source tenants to destination tenants within UiPath Orchestrator. This cmdlet creates complete copies of credential stores, including their configurations and metadata.

The cmdlet supports copying credential stores to multiple destination tenants simultaneously. Credential stores contain security configurations for external credential management systems and authentication providers.

Use the -Name parameter to specify which credential stores to copy and the -Destination parameter to specify the target tenants. The -Path parameter allows you to specify the source tenant when working with multiple Orchestrator instances.

This is a tenant entity cmdlet. The -Path parameter specifies the source drive name (e.g., Orch1:, Orch2:), and -Destination specifies the target tenant drives where credential stores should be copied.

Primary Endpoint: GET /odata/CredentialStores, GET /odata/CredentialStores({id}), POST /odata/CredentialStores

OAuth required scopes: OR.Settings

Required permissions: Settings.View, Settings.Create

## EXAMPLES

### Example 1
```powershell
PS C:\> Copy-OrchCredentialStore -Path Orch1: *Vault* -Destination Orch2:
```

Copies all credential stores containing Vault in their name from Orch1 to Orch2 using wildcards.

### Example 2
```powershell
PS Orch1:\> Copy-OrchCredentialStore AzureKeyVault, HashiCorpVault Orch2: -WhatIf
```

Shows what would happen when copying AzureKeyVault and HashiCorpVault credential stores from the current tenant to Orch2.

## PARAMETERS

### -Destination
Specifies the destination tenant drives where credential stores should be copied.

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
Specifies the Name of the credential stores to be copied.

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

### -ProgressAction
Controls how progress information is displayed during command execution. Use 'SilentlyContinue' to suppress progress display.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.CredentialStore
## NOTES
This is a tenant entity cmdlet. The -Path parameter specifies drive names (e.g., Orch1:, Orch2:) for source and destination tenants.

Use wildcards for efficient bulk operations and -WhatIf for testing before actual execution. Credential stores contain security configurations for external credential management systems.

## RELATED LINKS

[Get-OrchCredentialStore](Get-OrchCredentialStore.md)

[Set-OrchCredentialStore](Set-OrchCredentialStore.md)

[Remove-OrchCredentialStore](Remove-OrchCredentialStore.md)
