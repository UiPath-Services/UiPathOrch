---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: uiPathOrch
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
{{ Fill in the Description }}

Primary Endpoint: GET /api/clientsecrets/{licenseKey}

OAuth required scopes: OR.Machines

Required permissions:

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Get-OrchMachineClientSecretId
```

Outputs the issuance date and time of client secrets for all machines in this tenant.

### Example 2
```powershell
PS Orch1:\> Get-OrchMachineClientSecretId <machine names>
```

Outputs the issuance date and time of client secrets for the specified machines.

### Example 3
```powershell
PS Orch1:\> Get-OrchMachineClientSecretId | ? CreationTime -LT '2024/10/01' | Remove-OrchMachineClientSecret
```

Deletes all client secrets issued before 2024/10/01 for all machines in this tenant.

## PARAMETERS

### -Name
{{ Fill Name Description }}

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
{{ Fill Path Description }}

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
{{ Fill SecretId Description }}

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

## OUTPUTS

### UiPath.PowerShell.Entities.MachineSecretKey

## NOTES

## RELATED LINKS
