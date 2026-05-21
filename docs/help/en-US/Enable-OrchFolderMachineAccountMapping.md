---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Enable-OrchFolderMachineAccountMapping.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Enable-OrchFolderMachineAccountMapping
---

# Enable-OrchFolderMachineAccountMapping

## SYNOPSIS

Enables account-to-machine mappings in folders.

## SYNTAX

### __AllParameterSets

```
Enable-OrchFolderMachineAccountMapping [-Path <string[]>] [-Recurse] [-Depth <uint>]
 [-Name] <string[]> [[-UserName] <string[]>] [-Confirm] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Enables account-to-machine mappings in UiPath Orchestrator folders. This cmdlet maps user accounts (robots) to specific machines within a folder, allowing those users to execute automations on the specified machines. Only machines that are directly assigned to the folder (not inherited via PropagateToSubFolders) can be configured.

The -Name parameter specifies the machine name and supports tab completion. The -UserName parameter specifies the user account to map and supports tab completion. Tab completion for -UserName only suggests users that are not yet mapped to the machine. Multiple values can be specified using comma-separated text that includes wildcards.

Personal workspace folders are automatically excluded from the operation.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: POST /odata/Folders/UiPath.Server.Configuration.OData.SetMachineRobots

OAuth required scopes: OR.Robots

Required permissions: SubFolders.Edit or Units.Edit

## EXAMPLES

### Example 1: Map a user account to a machine

```powershell
PS Orch1:\Shared> Enable-OrchFolderMachineAccountMapping aiai testrobot01
```

Maps the user 'testrobot01' to 'aiai' in the current folder.

### Example 2: Map all users to a machine

```powershell
PS Orch1:\Shared> Enable-OrchFolderMachineAccountMapping aiai *
```

Maps all available user accounts to 'aiai' in the current folder.

### Example 3: Map users to machines across folders

```powershell
PS Orch1:\> Enable-OrchFolderMachineAccountMapping -Recurse ai* testrobot*
```

Maps all domain users to machines matching 'ai*' across all subfolders.

### Example 4: Preview mapping with WhatIf

```powershell
PS Orch1:\Shared> Enable-OrchFolderMachineAccountMapping aiai * -WhatIf
```

Shows what account-to-machine mappings would be enabled without actually performing the operation.

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

### -Recurse

Includes the target folder and all its subfolders in the operation.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: False
SupportsWildcards: false
Aliases: []
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

### -Depth

Specifies the depth for recursion into the target folders. A depth of 0 targets only the current folder with no subfolders included. When -Depth is specified, -Recurse is implied.

```yaml
Type: System.UInt32
DefaultValue: None
SupportsWildcards: false
Aliases: []
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

### -Name

Specifies the machine names to configure. Supports wildcards. Tab completion dynamically suggests machine names that are directly assigned (not inherited) to the target folders.

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

### -UserName

Specifies the user account names to map to the machine. Supports wildcards. Tab completion dynamically suggests user accounts that are not yet mapped to the specified machine. Multiple values can be specified using comma-separated text.

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

You can pipe machine names to this cmdlet via the Name property, and user names via the UserName property.

## OUTPUTS

### None

This cmdlet does not produce output.

## NOTES

Only machines that are directly assigned to the folder (PropagateToSubFolders = false) can be configured. Inherited machine assignments are excluded.

Personal workspace folders are automatically excluded from the operation.

## RELATED LINKS

[Disable-OrchFolderMachineAccountMapping](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Disable-OrchFolderMachineAccountMapping.md)

[Get-OrchFolderMachineAccountMapping](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchFolderMachineAccountMapping.md)

[Get-OrchFolderMachine](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchFolderMachine.md)
