---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Update-OrchProcessVersion.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Update-OrchProcessVersion
---

# Update-OrchProcessVersion

## SYNOPSIS

Updates the package version of a process.

## SYNTAX

### ReleaseName (Default)

```
Update-OrchProcessVersion [-Path <string[]>] [-Recurse] [-Depth <uint>]
 [[-Name] <string[]>] [[-Version] <string>] [-Confirm] [-WhatIf] [<CommonParameters>]
```

### ReleaseId

```
Update-OrchProcessVersion [-Path <string[]>] [-Recurse] [-Depth <uint>] [[-Id] <long[]>]
 [[-Version] <string>] [-Confirm] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Updates the package version of a process in UiPath Orchestrator. When -Version is not specified, the process is updated to the latest available package version. If the process is already at the latest version, it is silently skipped. When -Version is specified, the process is updated to that specific version, with wildcard matching supported.

This cmdlet has two parameter sets: ReleaseName (default) identifies processes by name, and ReleaseId identifies processes by their numeric release ID. At least one of -Name or -Id must be specified; an error is raised if neither is provided.

Tab completion for -Name and -Id only shows processes that have two or more versions available, excluding TestAutomationProcess entries. Tab completion for -Version dynamically suggests versions other than the currently deployed version.

This cmdlet supports ShouldProcess. Use -WhatIf to preview the version update, or -Confirm to be prompted before each update.

The -Path parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available values.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: POST /odata/Releases/UiPath.Server.Configuration.OData.UpdateToSpecificPackageVersion

OAuth required scopes: OR.Execution

Required permissions: Processes.Edit

## EXAMPLES

### Example 1: Update a process to the latest version

```powershell
PS Orch1:\Shared> Update-OrchProcessVersion BlankProcess19
```

Updates the process named "BlankProcess19" to the latest available package version. If the process is already at the latest version, no action is taken.

### Example 2: Update a process to a specific version

```powershell
PS Orch1:\Shared> Update-OrchProcessVersion BlankProcess19 1.0.3
```

Updates the process named "BlankProcess19" to version 1.0.3.

### Example 3: Preview version update with -WhatIf

```powershell
PS Orch1:\Shared> Update-OrchProcessVersion BlankProcess19 -WhatIf
```

```output
What if: Performing the operation "Update Process Version to Latest" on target "BlankProcess19 [Shared]".
```

Shows what would happen without actually updating the version.

### Example 4: Update a process by ID

```powershell
PS Orch1:\Shared> Update-OrchProcessVersion -Id 573412 -Version 2.0.*
```

Updates the process with release ID 573412 to the latest version matching the wildcard pattern "2.0.*".

### Example 5: Update processes from a specific folder

```powershell
PS C:\> Update-OrchProcessVersion -Path Orch1:\Production -Name Blank* -Version 3.1.0
```

Updates all processes matching "Blank*" in the Production folder to version 3.1.0. When -Path uses an absolute path (Orch1:\...), the command can be run from any location.

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

### -Id

Specifies the release IDs of the processes to update. Tab completion only suggests processes that have two or more versions available, excluding TestAutomationProcess entries.

```yaml
Type: System.Int64[]
DefaultValue: None
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: ReleaseId
  Position: 0
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Name

Specifies the names of the processes to update. Supports wildcards and multiple comma-separated values. Tab completion only suggests processes that have two or more versions available, excluding TestAutomationProcess entries.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: ReleaseName
  Position: 0
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Version

Specifies the target package version. If not specified, the process is updated to the latest available version. Supports wildcards (e.g., "2.0.*"). Tab completion dynamically suggests versions other than the currently deployed version.

```yaml
Type: System.String
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

### System.Int64[]

You can pipe release IDs to this cmdlet via the Id property.

### System.String

You can pipe a version string to this cmdlet via the Version property.

## OUTPUTS

### None

This cmdlet does not produce standard output. Use -Verbose to see details of version updates performed.

## NOTES

Processes are folder-scoped entities. You must navigate to a folder on the Orch: drive or use -Path to specify target folders.

This cmdlet updates the package version only. To modify other process settings (such as description, priority, or entry point), use Update-OrchProcess instead.

When -Version is not specified and the process is already at the latest version, the process is silently skipped with no error. When -Version is specified, the cmdlet performs a wildcard match against available versions and updates to the matching version.

An error is raised if neither -Name nor -Id is specified.

## RELATED LINKS

[Reset-OrchProcessVersion](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Reset-OrchProcessVersion.md)

[Update-OrchProcess](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Update-OrchProcess.md)

[Get-OrchProcess](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchProcess.md)
