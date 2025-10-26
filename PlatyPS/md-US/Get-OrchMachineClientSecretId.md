---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchMachineClientSecretId

## SYNOPSIS
Retrieves the creation date and time of client secrets from multiple machines.

## SYNTAX

```
Get-OrchMachineClientSecretId [[-Name] <String[]>] [[-SecretId] <String[]>] [-Path <String[]>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
The Get-OrchMachineClientSecretId cmdlet retrieves client secret information for machines, including creation dates and identifiers. Client secrets are used for machine authentication to Orchestrator and have expiration policies for security management.

This is a tenant entity cmdlet. The -Path parameter specifies target tenants using drive names (e.g., Orch1:, Orch2:). If not specified, the current tenant will be targeted.

This cmdlet is useful for security audits, secret rotation management, and identifying secrets that need renewal based on their creation dates.

Primary Endpoint: GET /api/clientsecrets/{licenseKey}

OAuth required scopes: OR.Machines

Required permissions: Machines.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Get-OrchMachineClientSecretId
```

Outputs the issuance date and time of client secrets for all machines in the current tenant.

### Example 2
```powershell
PS Orch1:\> Get-OrchMachineClientSecretId Machine01, Machine02
```

Outputs the issuance date and time of client secrets for the specified machines.

### Example 3
```powershell
PS Orch1:\> Get-OrchMachineClientSecretId *Prod*
```

Gets client secret information for all machines whose names contain "Prod".

### Example 4
```powershell
PS C:\> Get-OrchMachineClientSecretId -Path Orch1:, Orch2: Machine01
```

Gets client secret information for Machine01 across multiple tenants.

### Example 5
```powershell
PS Orch1:\> Get-OrchMachineClientSecretId -SecretId *123456*
```

Gets client secret information for secrets whose IDs contain "123456".

## PARAMETERS

### -Name
Specifies the names of the machines for which client secret information should be retrieved.

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
Specifies the name of the target tenants using drive names. If not specified, the current tenant will be targeted.

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

### -SecretId
Specifies the client secret IDs to be retrieved. This can be used to query specific secrets by their identifiers.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]
## OUTPUTS

### UiPath.PowerShell.Entities.MachineSecretKey
## NOTES

Primary Endpoint: GET /api/clientsecrets
OAuth required scopes: [PLACEHOLDER]
Required permissions: [PLACEHOLDER]

## RELATED LINKS
