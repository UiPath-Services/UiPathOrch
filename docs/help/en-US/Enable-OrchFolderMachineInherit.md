---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Enable-OrchFolderMachineInherit.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Enable-OrchFolderMachineInherit
---

# Enable-OrchFolderMachineInherit

## SYNOPSIS

Enables machine inheritance to subfolders.

## SYNTAX

### __AllParameterSets

```
Enable-OrchFolderMachineInherit [-Name] <string[]> [-Path <string[]>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Enables machine inheritance (PropagateToSubFolders) for machines assigned to folders in UiPath Orchestrator. When inheritance is enabled, a machine assigned to a parent folder becomes automatically available in all its subfolders.

The -Name parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available machine names. Tab completion only suggests machines that currently have inheritance disabled. Multiple values can be specified using comma-separated text that includes wildcards.

Primary Endpoint: POST /odata/Folders/UiPath.Server.Configuration.OData.ToggleFolderMachineInherit

OAuth required scopes: OR.Folders or OR.Folders.Write

Required permissions: Units.Edit or SubFolders.Edit

## EXAMPLES

### Example 1: Enable inheritance for a machine

```powershell
PS Orch1:\Shared> Enable-OrchFolderMachineInherit aiai
```

Enables inheritance for 'aiai' in the current folder, making it available in all subfolders of Shared.

### Example 2: Enable inheritance for all machines using wildcards

```powershell
PS Orch1:\Shared> Enable-OrchFolderMachineInherit *
```

Enables inheritance for all machines assigned to the current folder that currently have inheritance disabled.

### Example 3: Enable inheritance from a specific folder

```powershell
PS C:\> Enable-OrchFolderMachineInherit -Path Orch1:\Shared ba*
```

Enables inheritance for machines matching 'ba*' in the Shared folder on Orch1.

## PARAMETERS

### -Path

Specifies the target folders. If not specified, the current folder is targeted.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
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

Specifies the names of the machines to enable inheritance for. Supports wildcards. Tab completion dynamically suggests machine names that currently have inheritance disabled in the target folders.

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

This cmdlet only affects machines that are directly assigned to the folder and currently have PropagateToSubFolders set to false.

The cmdlet uses multi-threaded folder processing for improved performance when operating across multiple folders.

## RELATED LINKS

[Disable-OrchFolderMachineInherit](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Disable-OrchFolderMachineInherit.md)

[Get-OrchFolderMachine](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchFolderMachine.md)

[Add-OrchFolderMachine](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Add-OrchFolderMachine.md)
