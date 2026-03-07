---
document type: cmdlet
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchMachineClientSecretId
---

# Get-OrchMachineClientSecretId

## SYNOPSIS

Gets client secret IDs and creation times for machines.

## SYNTAX

### __AllParameterSets

```
Get-OrchMachineClientSecretId [[-Name] <string[]>] [[-SecretId] <string[]>] [-Path <string[]>]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets client secret information (SecretId, ClientId, ClientSecret, CreationTime) for machines in UiPath Orchestrator. This cmdlet uses multi-threaded processing to retrieve secrets for multiple machines in parallel.

Machines with a Scope of 'PersonalWorkspace' or 'AutomationCloudRobot' are automatically excluded. Machines without a LicenseKey (ClientId) are also excluded.

The -Name and -SecretId parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values. Multiple values can be specified using comma-separated text that includes wildcards.

The output can be piped to Remove-OrchMachineClientSecret to delete specific secrets.

Machines are tenant-scoped entities. Use the -Path parameter to specify different Orchestrator drives.

Primary Endpoint: GET /api/clientsecrets/{licenseKey}

OAuth required scopes: OR.Machines

Required permissions: Machines.View

## EXAMPLES

### Example 1: Get all client secrets for all machines

```powershell
PS Orch1:\> Get-OrchMachineClientSecretId
```

Gets client secret information for all machines in the current Orchestrator tenant.

### Example 2: Get secrets for specific machines

```powershell
PS Orch1:\> Get-OrchMachineClientSecretId machine*
```

Gets client secret information for machines matching `machine*`.

### Example 3: Delete old client secrets

```powershell
PS Orch1:\> Get-OrchMachineClientSecretId | Where-Object CreationTime -LT 2024-10-01 | Remove-OrchMachineClientSecret
```

Gets all client secrets and pipes those created before October 2024 to Remove-OrchMachineClientSecret for deletion.

### Example 4: Get secrets from a specific drive

```powershell
PS C:\> Get-OrchMachineClientSecretId -Path Orch1:\ -Name machine1
```

Gets client secret information for "machine1" from the Orch1 tenant.

## PARAMETERS

### -Path

Specifies the target Orchestrator drives. If not specified, the current drive is targeted.

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

Specifies the names of the machines to query. Supports wildcards. Tab completion dynamically suggests machine names (excluding PersonalWorkspace and AutomationCloudRobot scopes).

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

### -SecretId

Specifies the secret IDs to filter. Supports wildcards. Tab completion dynamically suggests available secret IDs for the selected machines.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: (All)
  Position: 1
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

You can pipe machine names to this cmdlet via the Name property.

## OUTPUTS

### UiPath.PowerShell.Entities.MachineSecretKey

Returns MachineSecretKey objects with properties: Path, Name, ClientId, SecretId, ClientSecret, CreationTime, Description, Type, Scope.

## NOTES

Machines are tenant-scoped entities. The -Path parameter specifies Orchestrator drives (not folder paths).

This cmdlet uses multi-threaded processing to retrieve secrets for multiple machines in parallel, improving performance when querying many machines.

## RELATED LINKS

Add-OrchMachineClientSecret

Remove-OrchMachineClientSecret

Get-OrchMachine
