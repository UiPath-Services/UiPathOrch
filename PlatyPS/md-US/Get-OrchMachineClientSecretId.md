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
Get-OrchMachineClientSecretId
```

Outputs the issuance date and time of client secrets for all machines in the current tenant.

### Example 2
```powershell
Get-OrchMachineClientSecretId Machine01, Machine02
```

Outputs the issuance date and time of client secrets for the specified machines.

### Example 3
```powershell
Get-OrchMachineClientSecretId *Prod*
```

Gets client secret information for all machines whose names contain "Prod".

### Example 4
```powershell
Get-OrchMachineClientSecretId -Path Orch1:, Orch2: Machine01
```

Gets client secret information for Machine01 across multiple tenants.

### Example 5
```powershell
Get-OrchMachineClientSecretId -SecretId *abc123*
```

Gets client secret information for secrets whose IDs contain "abc123".

### Example 6
```powershell
Get-OrchMachineClientSecretId | Where-Object {$_.CreationTime -LT '2024/10/01'} | Remove-OrchMachineClientSecret
```

Deletes all client secrets issued before 2024/10/01 for all machines in the current tenant.

### Example 7
```powershell
Get-OrchMachine | Get-OrchMachineClientSecretId | Select-Object MachineName, SecretId, CreationTime
```

Gets client secret information for all machines via pipeline and displays key properties.

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
Machine names and secret IDs can be piped to this cmdlet.

### UiPath.PowerShell.Entities.Machine
Machine objects can be piped to this cmdlet. The Name property will be automatically mapped to the -Name parameter via ByPropertyName binding.

## OUTPUTS

### UiPath.PowerShell.Entities.MachineSecretKey

## NOTES

## RELATED LINKS
