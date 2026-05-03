---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchMachine.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Remove-OrchMachine
---

# Remove-OrchMachine

## SYNOPSIS

Removes machines from UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
Remove-OrchMachine [-Name] <string[]> [-Path <string[]>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Removes machine definitions from UiPath Orchestrator. The -Name parameter supports wildcards to remove multiple machines at once.

The -Name parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available machine names. Multiple values can be specified using comma-separated text that includes wildcards.

Machines are tenant-scoped entities. Use the -Path parameter to specify different Orchestrator drives.

Primary Endpoint: DELETE /odata/Machines({machineId})

OAuth required scopes: OR.Machines

Required permissions: Machines.Delete

## EXAMPLES

### Example 1: Remove a specific machine

```powershell
PS Orch1:\> Remove-OrchMachine m1
```

Removes the machine named 'm1' from the current Orchestrator tenant.

### Example 2: Remove machines with wildcard

```powershell
PS Orch1:\> Remove-OrchMachine m*
```

Removes all machines whose names start with 'm' from the current Orchestrator tenant.

### Example 3: Preview removal with -WhatIf

```powershell
PS Orch1:\> Remove-OrchMachine Test* -WhatIf
```

Shows which machines would be removed without actually deleting them.

### Example 4: Remove a machine from a specific drive

```powershell
PS C:\> Remove-OrchMachine -Path Orch1:\ pool
```

Removes the machine named 'pool' from the Orch1 tenant. When -Path uses an absolute path, the command can be run from any location.

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

Specifies the names of the machines to remove. Supports wildcards. Tab completion dynamically suggests machine names from the target drives.

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

### None

This cmdlet does not produce output.

## NOTES

Machines are tenant-scoped entities. They are not associated with specific folders.

## RELATED LINKS

[Get-OrchMachine](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchMachine.md)

[New-OrchMachine](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/New-OrchMachine.md)

[Update-OrchMachine](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Update-OrchMachine.md)

[Copy-OrchMachine](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchMachine.md)
