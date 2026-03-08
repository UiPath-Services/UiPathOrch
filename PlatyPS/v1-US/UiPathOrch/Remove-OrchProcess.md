---
document type: cmdlet
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Remove-OrchProcess
---

# Remove-OrchProcess

## SYNOPSIS

Removes processes from UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
Remove-OrchProcess [-Name] <string[]> [-Path <string[]>] [-Recurse] [-Depth <uint>] [-WhatIf]
 [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Removes (deletes) processes from UiPath Orchestrator folders. A process is the association between a package and a folder; removing a process does not delete the underlying package from the Orchestrator feed.

This cmdlet supports ShouldProcess. Use -WhatIf to preview which processes would be removed, or -Confirm to be prompted before each removal.

The -Name and -Path parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values. The -Name completion is dynamically populated from actual process names in the target folders. Multiple values can be specified using comma-separated text that includes wildcards.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: DELETE /odata/Releases({processId})

OAuth required scopes: OR.Execution

Required permissions: Processes.Delete

## EXAMPLES

### Example 1: Remove a process by name

```powershell
PS Orch1:\Shared> Remove-OrchProcess BlankProcess19
```

Removes the process named "BlankProcess19" from the current folder.

### Example 2: Remove processes using a wildcard

```powershell
PS Orch1:\Shared> Remove-OrchProcess *Test*
```

Removes all processes matching the wildcard pattern "*Test*" from the current folder.

### Example 3: Preview removal with -WhatIf

```powershell
PS Orch1:\Shared> Remove-OrchProcess *Old* -WhatIf
```

```output
What if: Performing the operation "Remove Process" on target "OldProcess [Shared]".
```

Shows which processes would be removed without actually removing them.

### Example 4: Remove a process from a specific folder

```powershell
PS C:\> Remove-OrchProcess -Path Orch1:\Shared BlankProcess19
```

Removes the process named "BlankProcess19" from the Shared folder. When -Path uses an absolute path (Orch1:\...), the command can be run from any location.

### Example 5: Remove processes recursively

```powershell
PS Orch1:\> Remove-OrchProcess -Recurse *Legacy* -Confirm
```

Removes all processes matching "*Legacy*" from all folders, prompting for confirmation before each removal.

## PARAMETERS

### -Path

Specifies the target folders. If not specified, the current folder is targeted. Supports wildcards and comma-separated values for multiple folders.

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

### -Name

Specifies the names of the processes to remove. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests process names from the target folders.

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

Shows what would happen if the cmdlet runs. The cmdlet is not run.

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe process names to this cmdlet via the Name property.

## OUTPUTS

### None

This cmdlet does not produce any output.

## NOTES

Processes are folder-scoped entities. You must navigate to a folder on the Orch: drive or use -Path to specify target folders.

Removing a process does not delete the underlying NuGet package from the Orchestrator feed. The package remains available for creating new processes. To manage packages, use the package management cmdlets.

## RELATED LINKS

Get-OrchProcess

Copy-OrchProcess

Update-OrchProcess
