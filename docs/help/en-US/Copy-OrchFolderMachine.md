---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchFolderMachine.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Copy-OrchFolderMachine
---

# Copy-OrchFolderMachine

## SYNOPSIS

Copies machine-to-folder assignments to a destination folder.

## SYNTAX

### __AllParameterSets

```
Copy-OrchFolderMachine [-Name] <string[]> [-Destination] <string> [-Path <string>] [-Recurse]
 [-Depth <uint>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Copies machine-to-folder assignments from a source folder to a destination folder in UiPath Orchestrator. This cmdlet reads machines assigned to the source folder and assigns them to the destination folder. When used with -Recurse, the folder hierarchy is preserved at the destination.

The -Name parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available machine names. Multiple values can be specified using comma-separated text.

If the source and destination resolve to the same folder, the operation is skipped.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: POST /odata/Folders/UiPath.Server.Configuration.OData.UpdateMachinesToFolderAssociations

OAuth required scopes: OR.Folders

Required permissions: Units.Edit or SubFolders.Edit (on both source and destination folders)

## EXAMPLES

### Example 1: Copy machine assignments to another folder

```powershell
PS Orch1:\Shared> Copy-OrchFolderMachine * Orch1:\Dept#2
```

Copies all machine assignments from the Shared folder to the Dept#2 folder.

### Example 2: Copy specific machines across drives

```powershell
PS C:\> Copy-OrchFolderMachine -Path Orch1:\Shared -Destination Orch2:\Shared aiai
```

Copies the 'aiai' machine assignment from the Shared folder on Orch1 to the Shared folder on Orch2.

### Example 3: Copy machine assignments recursively

```powershell
PS Orch1:\> Copy-OrchFolderMachine -Recurse * Orch2:\
```

Copies all machine assignments from all folders on Orch1 to the corresponding folders on Orch2, preserving the folder hierarchy.

### Example 4: Preview copy with WhatIf

```powershell
PS Orch1:\Shared> Copy-OrchFolderMachine * Orch1:\Dept#2 -WhatIf
```

Shows what machine assignments would be copied without actually performing the operation.

## PARAMETERS

### -Path

Specifies the source folder. If not specified, the current folder is used as the source.

```yaml
Type: System.String
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

Includes the source folder and all its subfolders in the copy operation. The folder hierarchy is preserved at the destination.

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

Specifies the depth for recursion into the source folders. A depth of 0 targets only the source folder with no subfolders included. When -Depth is specified, -Recurse is implied.

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

### -Destination

Specifies the destination folder where machine assignments will be copied to.

```yaml
Type: System.String
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

### -Name

Specifies the names of the machines to copy. Tab completion dynamically suggests machine names assigned to the source folders.

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

You can pipe the Destination path to this cmdlet.

## OUTPUTS

### None

This cmdlet does not produce output.

## NOTES

If the source and destination resolve to the same folder, the operation is silently skipped.

When using -Recurse, the relative folder hierarchy from the source is replicated at the destination. Destination subfolders that do not exist are skipped.

## RELATED LINKS

Get-OrchFolderMachine

Add-OrchFolderMachine

Remove-OrchFolderMachine
