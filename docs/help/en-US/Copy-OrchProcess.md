---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchProcess.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Copy-OrchProcess
---

# Copy-OrchProcess

## SYNOPSIS

Copies processes to another folder.

## SYNTAX

### __AllParameterSets

```
Copy-OrchProcess [-Path <string>] [-Recurse] [-Depth <uint>] [-Name] <string[]>
 [-Destination] <string> [-Confirm] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Copies processes from one UiPath Orchestrator folder to another. The cmdlet creates a new process in the destination folder using the same package and version as the source process. Cross-drive copy is supported, allowing processes to be copied between different Orchestrator instances (e.g., from Orch1: to Orch2:).

If the source and destination folders are the same, the process is silently skipped.

This cmdlet supports ShouldProcess. Use -WhatIf to preview which processes would be copied, or -Confirm to be prompted before each copy operation.

The -Name, -Destination, and -Path parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values. The -Name completion is dynamically populated from actual process names in the source folders. Multiple values can be specified using comma-separated text that includes wildcards.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: POST /odata/Releases

OAuth required scopes: OR.Execution or OR.Execution.Write

Required permissions: Processes.Create (destination), Processes.View (source)

## EXAMPLES

### Example 1: Copy a process to another folder

```powershell
PS Orch1:\Shared> Copy-OrchProcess BlankProcess19 Dept#2
```

Copies the process named "BlankProcess19" from the current folder (Shared) to the Dept#2 folder on the same Orchestrator instance.

### Example 2: Copy processes using a wildcard

```powershell
PS Orch1:\Shared> Copy-OrchProcess Blank* Dept#2
```

Copies all processes matching "Blank*" from the current folder to the Dept#2 folder.

### Example 3: Preview copy with -WhatIf

```powershell
PS Orch1:\Shared> Copy-OrchProcess BlankProcess19 Dept#2 -WhatIf
```

```output
What if: Performing the operation "Copy Process" on target "Item: 'BlankProcess19 [Shared]' Destination: 'Dept#2'".
```

Shows what would happen without actually copying.

### Example 4: Copy a process across Orchestrator instances

```powershell
PS C:\> Copy-OrchProcess -Path Orch1:\Shared BlankProcess19 Orch2:\Shared
```

Copies the process named "BlankProcess19" from the Shared folder on Orch1 to the Shared folder on Orch2. The package is automatically uploaded to the destination Orchestrator feed if it does not already exist.

### Example 5: Copy all processes recursively to another instance

```powershell
PS Orch1:\> Copy-OrchProcess -Recurse * Orch2:\Shared
```

Copies all processes from all folders on Orch1 to the Shared folder on Orch2.

## PARAMETERS

### -Path

Specifies the source folder. If not specified, the current folder is used as the source. Supports wildcards.

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

Includes the source folder and all its subfolders in the operation.

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

Specifies the depth for recursion into the source folders. A depth of 0 targets only the current folder with no subfolders included. When -Depth is specified, -Recurse is implied.

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

### -Destination

Specifies the destination folder where processes will be copied. Supports wildcards. Can reference a folder on a different Orchestrator drive (e.g., Orch2:\Production) for cross-instance copy.

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

Specifies the names of the processes to copy. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests process names from the source folders.

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

### System.String

You can pipe a destination folder path to this cmdlet via the Destination property.

## OUTPUTS

### UiPath.PowerShell.Entities.Release

Returns the newly created Release object in the destination folder. When the source and destination are the same folder, the process is silently skipped and no output is returned.

## NOTES

Processes are folder-scoped entities. You must navigate to a folder on the Orch: drive or use -Path to specify the source folder.

When performing cross-drive copy (e.g., Orch1: to Orch2:), the cmdlet automatically handles package upload to the destination Orchestrator feed. The destination Orchestrator must have the same or compatible package feed configuration.

If a process with the same name already exists in the destination folder, the behavior depends on the Orchestrator configuration for handling duplicate processes.

## RELATED LINKS

[Get-OrchProcess](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchProcess.md)

[Remove-OrchProcess](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchProcess.md)

[Update-OrchProcess](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Update-OrchProcess.md)
