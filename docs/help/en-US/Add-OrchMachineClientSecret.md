---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Add-OrchMachineClientSecret.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Add-OrchMachineClientSecret
---

# Add-OrchMachineClientSecret

## SYNOPSIS

Generates a new client secret for machines.

## SYNTAX

### __AllParameterSets

```
Add-OrchMachineClientSecret [-Name] <string[]> [-Path <string[]>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Generates a new client secret for the specified machines in UiPath Orchestrator. The generated secret is returned in the output and should be saved immediately, as it cannot be retrieved again after creation.

Machines with a Scope of 'PersonalWorkspace' or 'AutomationCloudRobot' are automatically excluded from the operation.

The -Name parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available machine names. Multiple values can be specified using comma-separated text that includes wildcards.

Machines are tenant-scoped entities. Use the -Path parameter to specify different Orchestrator drives.

Primary Endpoint: POST /api/clientsecrets/{licenseKey}

OAuth required scopes: OR.Machines

Required permissions: Machines.Edit

## EXAMPLES

### Example 1: Generate a secret for a specific machine

```powershell
PS Orch1:\> Add-OrchMachineClientSecret aiai
```

Generates a new client secret for the machine named 'aiai'. The output includes the ClientId, SecretId, and ClientSecret values.

### Example 2: Generate secrets for multiple machines

```powershell
PS Orch1:\> Add-OrchMachineClientSecret m*
```

Generates new client secrets for all machines matching 'm*'.

### Example 3: Preview with -WhatIf

```powershell
PS Orch1:\> Add-OrchMachineClientSecret * -WhatIf
```

Shows which machines would receive new client secrets without actually generating them.

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

Specifies the names of the machines for which to generate client secrets. Supports wildcards. Tab completion dynamically suggests machine names (excluding PersonalWorkspace and AutomationCloudRobot scopes).

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

You can pipe machine names to this cmdlet via the Name property.

## OUTPUTS

### UiPath.PowerShell.Entities.MachineSecretKey

Returns MachineSecretKey objects with properties: Path, Name, ClientId, SecretId, ClientSecret, CreationTime, Description, Type, Scope. The ClientSecret value is only available at creation time and cannot be retrieved later.

## NOTES

Machines are tenant-scoped entities. The -Path parameter specifies Orchestrator drives (not folder paths).

The generated ClientSecret is only returned once at creation time. Make sure to save it securely, as it cannot be retrieved again.

## RELATED LINKS

Get-OrchMachineClientSecretId

Remove-OrchMachineClientSecret

Get-OrchMachine
