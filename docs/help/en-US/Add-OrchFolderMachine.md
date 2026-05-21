---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Add-OrchFolderMachine.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Add-OrchFolderMachine
---

# Add-OrchFolderMachine

## SYNOPSIS

Assigns machines to folders.

## SYNTAX

### __AllParameterSets

```
Add-OrchFolderMachine [-Path <string[]>] [-Recurse] [-Depth <uint>] [-Name] <string[]>
 [[-PropagateToSubFolders] <string>] [-Confirm] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Assigns machines to folders in UiPath Orchestrator. This cmdlet adds machine-to-folder associations, making machines available for process execution in the specified folders. Personal folders are automatically excluded from the operation.

The -Name parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available machine names that can be assigned (machines not already assigned to the folder). Multiple values can be specified using comma-separated text.

The -PropagateToSubFolders parameter controls whether the machine assignment is inherited by subfolders. Tab completion suggests 'true' or 'false'.

The cmdlet collects all assignment requests during pipeline processing and executes them as batch operations in EndProcessing for efficiency.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: POST /odata/Folders/UiPath.Server.Configuration.OData.UpdateMachinesToFolderAssociations

OAuth required scopes: OR.Folders

Required permissions: Units.Edit or SubFolders.Edit

## EXAMPLES

### Example 1: Assign a machine to the current folder

```powershell
PS Orch1:\Shared> Add-OrchFolderMachine aiai
```

Assigns the machine named 'aiai' to the current folder.

### Example 2: Assign a machine with inheritance enabled

```powershell
PS Orch1:\Shared> Add-OrchFolderMachine aiai true
```

Assigns 'aiai' to the current folder and propagates the assignment to all subfolders.

### Example 3: Assign machines to multiple folders recursively

```powershell
PS Orch1:\> Add-OrchFolderMachine -Recurse m1,m2
```

Assigns 'm1' and 'm2' to the current folder and all its subfolders.

### Example 4: Preview assignments with WhatIf

```powershell
PS Orch1:\Shared> Add-OrchFolderMachine m* -WhatIf
```

Shows what machines would be assigned without actually performing the operation.

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

Specifies the names of the machines to assign. Tab completion dynamically suggests machine names that are available for assignment to the target folders.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: false
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

### -PropagateToSubFolders

Specifies whether the machine assignment should be inherited by subfolders. When set to 'true', the machine becomes available in all subfolders of the target folder. Tab completion suggests 'true' or 'false'.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: false
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

You can pipe machine names to this cmdlet via the Name property.

### System.String

You can pipe the PropagateToSubFolders value to this cmdlet.

## OUTPUTS

### None

This cmdlet does not produce output.

## NOTES

Personal folders are automatically excluded from the operation.

The cmdlet batches all machine assignments per folder and executes them as a single API call for efficiency.

If -PropagateToSubFolders is set to 'true', the inherit flag is set in a separate API call after the machine is assigned.

## RELATED LINKS

[Get-OrchFolderMachine](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchFolderMachine.md)

[Remove-OrchFolderMachine](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchFolderMachine.md)

[Enable-OrchFolderMachineInherit](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Enable-OrchFolderMachineInherit.md)
