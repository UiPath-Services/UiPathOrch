---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchCredentialStore

## SYNOPSIS
Gets the credential stores.

## SYNTAX

```
Get-OrchCredentialStore [[-Name] <String[]>] [-Path <String[]>] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION
The `Get-OrchCredentialStore` cmdlet retrieves credential store information from UiPath Orchestrator. Credential stores are secure repositories used to store and manage credentials and sensitive information that can be accessed by robots and automation processes.

The cmdlet returns comprehensive information about configured credential stores including their types (Database, AWS Secrets Manager, Azure Key Vault, etc.), configuration details, read-only status, and proxy settings when applicable. Credential stores provide centralized, secure credential management for automation workflows.

Orchestrator supports multiple types of credential stores including the built-in Orchestrator Database store and external secure storage services like AWS Secrets Manager, Azure Key Vault, and HashiCorp Vault. Each store type has specific configuration requirements and capabilities.

The cmdlet provides essential information for understanding the credential management landscape, configuring robot access to credentials, and ensuring proper security configuration across automation environments.

Primary Endpoint: GET /odata/CredentialStores, /odata/CredentialStores({credentialStoreId})

OAuth required scopes: [PLACEHOLDER - Credential store scopes]

Required permissions: [PLACEHOLDER - Credential store view permissions]

Primary Endpoint: GET /odata/CredentialStores, /odata/CredentialStores({credentialStoreId})

OAuth required scopes: OR.Settings or OR.Settings.Read

Required permissions: Settings.View

## EXAMPLES

### Example 1
```powershell
PS C:\> {{ Add example code here }}
```

{{ Add example description here }}

## PARAMETERS

### -Name
Specifies the Name of the credential stores to be retrieved.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
Specifies the name of the target drives. If not specified, the current drive will be targeted.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
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

### None
## OUTPUTS

### UiPath.PowerShell.Entities.CredentialStore
## NOTES

## RELATED LINKS
