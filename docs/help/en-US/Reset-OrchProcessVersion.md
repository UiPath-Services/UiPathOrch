---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Reset-OrchProcessVersion.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Reset-OrchProcessVersion
---

# Reset-OrchProcessVersion

## SYNOPSIS

Rolls back the process to its previous package version.

## SYNTAX

### __AllParameterSets

```
Reset-OrchProcessVersion [-Path <string[]>] [-Recurse] [-Depth <uint>] [-Name] <string[]>
 [-Confirm] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Rolls back the process package version to the previously deployed version using the Orchestrator RollbackReleaseVersion API. This is useful for reverting a process to a known-good version after a failed deployment or when issues are discovered in the current version.

Tab completion for -Name only shows processes that have two or more versions available, excluding TestAutomationProcess entries. This ensures that only processes with a valid rollback target are suggested.

This cmdlet supports ShouldProcess. Use -WhatIf to preview which processes would be rolled back, or -Confirm to be prompted before each rollback.

The -Name and -Path parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: POST /odata/Releases({processIds})/UiPath.Server.Configuration.OData.RollbackToPreviousReleaseVersion?mergePackageTags=false

OAuth required scopes: OR.Execution

Required permissions: Processes.Edit

## EXAMPLES

### Example 1: Roll back a process to the previous version

```powershell
PS Orch1:\Shared> Reset-OrchProcessVersion BlankProcess19
```

Rolls back the process named "BlankProcess19" in the current folder to its previous package version.

### Example 2: Preview rollback with -WhatIf

```powershell
PS Orch1:\Shared> Reset-OrchProcessVersion BlankProcess19 -WhatIf
```

```output
What if: Performing the operation "Reset Process Version" on target "BlankProcess19 [Shared]".
```

Shows what would happen without actually rolling back the process.

### Example 3: Roll back a process from a specific folder

```powershell
PS C:\> Reset-OrchProcessVersion -Path Orch1:\Production BlankProcess19
```

Rolls back the process named "BlankProcess19" in the Production folder to its previous version. When -Path uses an absolute path (Orch1:\...), the command can be run from any location.

### Example 4: Roll back multiple processes using a wildcard

```powershell
PS Orch1:\Shared> Reset-OrchProcessVersion Blank* -Confirm
```

Rolls back all processes matching "Blank*" in the current folder, prompting for confirmation before each rollback.

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

Specifies the names of the processes to roll back. Tab completion only suggests processes that have two or more versions available, excluding TestAutomationProcess entries.

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

This cmdlet rolls back to the immediately previous version only. To update to a specific version, use Update-OrchProcessVersion instead.

The process must have at least two versions available for rollback. If only one version exists, the operation will fail.

## RELATED LINKS

[Update-OrchProcessVersion](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Update-OrchProcessVersion.md)

[Get-OrchProcess](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchProcess.md)

[Update-OrchProcess](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Update-OrchProcess.md)
