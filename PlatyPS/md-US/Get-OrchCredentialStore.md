---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchCredentialStore

## SYNOPSIS
Retrieves credential stores configured in UiPath Orchestrator.

## SYNTAX

```
Get-OrchCredentialStore [[-Name] <String[]>] [-Path <String[]>] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION
The Get-OrchCredentialStore cmdlet retrieves credential stores configured within UiPath Orchestrator. Credential stores provide secure external storage for sensitive credentials, enabling integration with external secret management systems such as AWS Secrets Manager, Azure Key Vault, and database-based credential storage.

Each credential store contains properties such as Name, Type (storage provider), ProxyId, ProxyType, HostName, AdditionalConfiguration (provider-specific settings), and read-only status. These stores serve as secure repositories for automation credentials used by robots and processes.

This cmdlet operates as a tenant-level entity operation, retrieving credential stores from the specified Orchestrator environment. The AdditionalConfiguration property contains provider-specific settings with sensitive values masked for security.

Primary Endpoint: GET /odata/CredentialStores

OAuth required scopes: OR.Settings or OR.Settings.Read

Required permissions: Settings.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Get-OrchCredentialStore
```

Retrieves all credential stores, displaying Id, Name, Type, ProxyId, ProxyType, HostName, IsReadOnly, and masked AdditionalConfiguration.

### Example 2
```powershell
PS Orch1:\Shared> Get-OrchCredentialStore | ConvertTo-Json -Depth 3
```

Displays detailed credential store properties in JSON format, including all configuration details with sensitive values masked.

### Example 3
```powershell
PS C:\> Get-OrchCredentialStore -Path Orch1: -Name "AWS*"
```

Gets all credential stores with names starting with "AWS" in the Orch1 tenant.

### Example 4
```powershell
PS Orch1:\> Get-OrchCredentialStore | Where-Object {$_.Type -eq "AWS Secrets Manager"}
```

Retrieves all AWS Secrets Manager credential stores.

### Example 5
```powershell
PS Orch1:\> Get-OrchCredentialStore | Select-Object Name, Type, @{Name="IsReadOnly";Expression={$_.Details.IsReadOnly}}
```

Displays credential store names, types, and read-only status for overview.

### Example 6
```powershell
PS Orch1:\> Get-OrchCredentialStore | Group-Object Type
```

Groups credential stores by their provider type (AWS Secrets Manager, Database, etc.).

## PARAMETERS

### -Name
Specifies the names of credential stores to be retrieved. Supports wildcard patterns for flexible store selection.

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
Specifies the target tenant drives. If not specified, the current tenant will be targeted. For tenant-level operations.

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

### System.String[]
Credential store names can be piped to this cmdlet.

### UiPath.PowerShell.Entities.CredentialStore
CredentialStore objects can be piped to this cmdlet. The Name property will be automatically mapped to the -Name parameter via ByPropertyName binding.

## OUTPUTS

### UiPath.PowerShell.Entities.CredentialStore
Returns CredentialStore objects containing information about configured credential stores. Key properties include:
- Path: Current tenant context path
- Id: Unique numeric credential store identifier
- Name: Display name of the credential store
- Type: Provider type (e.g., "AWS Secrets Manager", "Database")
- ProxyId: Proxy configuration identifier (nullable)
- ProxyType: Proxy type information (nullable)
- HostName: Host name information (nullable)
- AdditionalConfiguration: Provider-specific configuration with masked sensitive values
- Details.IsReadOnly: Read-only status flag

## NOTES
This cmdlet is a tenant-level entity operation for accessing credential store configurations. Credential stores provide secure external storage for automation credentials with various provider types including AWS Secrets Manager and database storage. Sensitive configuration values are masked in the output for security. This operation requires Settings.View permissions.

## RELATED LINKS

[Add-OrchCredentialStore](Add-OrchCredentialStore.md)

[Set-OrchCredentialStore](Set-OrchCredentialStore.md)

[Remove-OrchCredentialStore](Remove-OrchCredentialStore.md)

[Test-OrchCredentialStore](Test-OrchCredentialStore.md)
