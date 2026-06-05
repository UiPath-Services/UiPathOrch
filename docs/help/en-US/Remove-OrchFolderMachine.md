---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchFolderMachine.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Remove-OrchFolderMachine
---

# Remove-OrchFolderMachine

## SYNOPSIS

Unassigns machines from folders.

## SYNTAX

### __AllParameterSets

```
Remove-OrchFolderMachine [-Path <string[]>] [-LiteralPath <string[]>] [-Recurse] [-Depth <uint>] [-Name] <string[]>
 [-Confirm] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Removes machine-to-folder assignments in UiPath Orchestrator. This cmdlet unassigns machines from the specified folders, making them no longer available for process execution in those folders.

The -Name parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available machine names assigned to the target folders. Multiple values can be specified using comma-separated text that includes wildcards.

The cmdlet collects matching machines and unassigns them as a batch operation per folder.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: POST /odata/Folders/UiPath.Server.Configuration.OData.UpdateMachinesToFolderAssociations

OAuth required scopes: OR.Folders or OR.Folders.Write

Required permissions: Units.Edit or SubFolders.Edit

## EXAMPLES

### Example 1: Unassign a machine from the current folder

```powershell
PS Orch1:\Shared> Remove-OrchFolderMachine aiai
```

Unassigns the machine named 'aiai' from the current folder.

### Example 2: Unassign machines using wildcards

```powershell
PS Orch1:\Shared> Remove-OrchFolderMachine Test*
```

Unassigns all machines whose names match 'Test*' from the current folder.

### Example 3: Unassign machines from all folders recursively

```powershell
PS Orch1:\> Remove-OrchFolderMachine -Recurse OldMachine
```

Unassigns 'OldMachine' from the current folder and all its subfolders.

### Example 4: Preview removal with WhatIf

```powershell
PS Orch1:\Shared> Remove-OrchFolderMachine * -WhatIf
```

Shows what machines would be unassigned from the current folder without actually performing the operation.

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

### -LiteralPath

Specifies the target folder or drive by literal path -- wildcard metacharacters (`[`, `]`, `*`, `?`) are treated as literal characters rather than patterns. Accepts the same drive-qualified paths as -Path. Its `PSPath` alias also binds the path of items piped from Get-ChildItem / Get-Item, so you can pipe folders directly. Use -LiteralPath instead of -Path when a folder name contains a wildcard metacharacter.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: false
Aliases:
- PSPath
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

Specifies the names of the machines to unassign. Supports wildcards. Tab completion dynamically suggests machine names assigned to the target folders.

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

## OUTPUTS

### None

This cmdlet does not produce output.

## NOTES

The cmdlet collects all matching machines per folder and unassigns them in a single batch API call.

## RELATED LINKS

[Get-OrchFolderMachine](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchFolderMachine.md)

[Add-OrchFolderMachine](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Add-OrchFolderMachine.md)

[Copy-OrchFolderMachine](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchFolderMachine.md)
