---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchMachineClientSecret.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Remove-OrchMachineClientSecret
---

# Remove-OrchMachineClientSecret

## SYNOPSIS

Removes client secrets from machines.

## SYNTAX

### __AllParameterSets

```
Remove-OrchMachineClientSecret [-Path <string[]>] [-Name] <string[]>
 [-SecretId] <string[]> [-Confirm] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Removes client secrets from machines in UiPath Orchestrator. Both -Name and -SecretId are mandatory parameters. Use Get-OrchMachineClientSecretId to find the secret IDs for a machine.

Machines with a Scope of 'PersonalWorkspace' or 'AutomationCloudRobot' are automatically excluded.

The -Name and -SecretId parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values. Multiple values can be specified using comma-separated text that includes wildcards.

This cmdlet accepts pipeline input from Get-OrchMachineClientSecretId, enabling scenarios like deleting old secrets based on creation time.

Machines are tenant-scoped entities. Use the -Path parameter to specify different Orchestrator drives.

Primary Endpoint: DELETE /api/clientsecrets/{secretId}

OAuth required scopes: OR.Machines

Required permissions: Machines.Edit

## EXAMPLES

### Example 1: Remove a specific client secret

```powershell
PS Orch1:\> Remove-OrchMachineClientSecret aiai 12345
```

Removes the client secret with ID 12345 from the machine named 'aiai'.

### Example 2: Remove all secrets for a machine

```powershell
PS Orch1:\> Remove-OrchMachineClientSecret aiai *
```

Removes all client secrets from the machine named 'aiai'.

### Example 3: Remove old secrets via pipeline

```powershell
PS Orch1:\> Get-OrchMachineClientSecretId | Where-Object CreationTime -LT 2024-10-01 | Remove-OrchMachineClientSecret
```

Gets all client secrets, filters those created before October 2024, and removes them via the pipeline.

### Example 4: Preview removal with -WhatIf

```powershell
PS Orch1:\> Remove-OrchMachineClientSecret m* * -WhatIf
```

Shows which client secrets would be removed from all machines matching 'm*' without actually deleting them.

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

Specifies the names of the machines from which to remove client secrets. Supports wildcards. Tab completion dynamically suggests machine names (excluding PersonalWorkspace and AutomationCloudRobot scopes).

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

### -SecretId

Specifies the secret IDs to remove. Supports wildcards. Tab completion dynamically suggests available secret IDs for the selected machines.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: (All)
  Position: 1
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

You can pipe the Name and SecretId values to this cmdlet. This enables pipeline input from Get-OrchMachineClientSecretId.

## OUTPUTS

### None

This cmdlet does not produce output.

## NOTES

Machines are tenant-scoped entities. The -Path parameter specifies Orchestrator drives (not folder paths).

## RELATED LINKS

[Get-OrchMachineClientSecretId](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchMachineClientSecretId.md)

[Add-OrchMachineClientSecret](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Add-OrchMachineClientSecret.md)

[Get-OrchMachine](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchMachine.md)
