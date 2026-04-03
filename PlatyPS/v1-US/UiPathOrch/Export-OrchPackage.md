---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Export-OrchPackage
---

# Export-OrchPackage

## SYNOPSIS

Exports process packages from UiPath Orchestrator to local .nupkg files.

## SYNTAX

### __AllParameterSets

```
Export-OrchPackage [-Id] <string[]> [[-Version] <string[]>] [[-Destination] <string>]
 [-Path <string[]>] [-Recurse] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Exports process packages (NuGet packages containing automation processes) from UiPath Orchestrator folder feeds to the local filesystem as .nupkg files. The cmdlet downloads the specified package versions and saves them to the -Destination directory.

The -Id and -Version parameters support wildcards and tab completion. Press [Ctrl+Space] or [Tab] to see available values. The -Id completion lists package IDs from the target folders, and the -Version completion lists versions for the selected packages. Multiple values can be specified using comma-separated text that includes wildcards.

When -Recurse is specified, packages from all subfolders are exported, with each subfolder's packages saved to a corresponding subdirectory under -Destination, preserving the folder hierarchy.

If -Destination is not specified, the current filesystem location is used.

When specifying the -Path and -Recurse parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: GET /odata/Processes/UiPath.Server.Configuration.OData.DownloadPackage(key='{processId}:{processVersion}')&feedId={feedId}

OAuth required scopes: OR.Execution or OR.Execution.Read

Required permissions: (Packages.View - Downloads a package from a Tenant Feed) and (FolderPackages.View - Downloads a package from a Folder Feed)

## EXAMPLES

### Example 1: Export all versions of a specific package

```powershell
PS Orch1:\Shared> Export-OrchPackage MyProcess
```

Exports all versions of the package "MyProcess" to the current filesystem directory. Because -Id is at position 0, the parameter name can be omitted.

### Example 2: Export a specific version to a destination directory

```powershell
PS Orch1:\Shared> Export-OrchPackage MyProcess 1.0.0 C:\packages
```

Exports version 1.0.0 of "MyProcess" to the C:\packages directory. All three positional parameters (-Id, -Version, -Destination) are specified without parameter names.

### Example 3: Export all packages using wildcards

```powershell
PS Orch1:\Shared> Export-OrchPackage * -Destination C:\backup
```

Exports all versions of all packages from the current folder to C:\backup.

### Example 4: Export packages from a specific folder using -Path

```powershell
PS C:\> Export-OrchPackage -Path Orch1:\Production -Id *Invoice* -Destination C:\export
```

Exports all packages containing "Invoice" in their Id from the Production folder. When -Path uses an absolute path (Orch1:\...), the command can be run from any location.

### Example 5: Export packages recursively preserving folder structure

```powershell
PS Orch1:\> Export-OrchPackage -Recurse * -Destination C:\full-backup
```

Exports all packages from all folders, creating subdirectories under C:\full-backup that match the Orchestrator folder hierarchy.

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

Includes the target folder and all its subfolders in the operation. Exported packages from subfolders are saved to corresponding subdirectories under -Destination.

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

### -Destination

Specifies the destination directory on the local filesystem for exported .nupkg files. If not specified, the current filesystem location is used. The directory is created automatically if it does not exist.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 2
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Id

Specifies the Id of the process packages to export. This is a mandatory parameter. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests package IDs from the target folders.

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

### -Version

Specifies the version of the process packages to export. If not specified, all versions are exported. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests versions for the selected package IDs.

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

You can pipe package IDs and versions to this cmdlet via the Id and Version properties.

### System.String

You can pipe a destination path to this cmdlet via the Destination property.

## OUTPUTS

### None

This cmdlet does not produce pipeline output. The .nupkg files are saved to the -Destination directory.

## NOTES

If the -Destination directory does not exist, the cmdlet creates it automatically. If the current location is on an Orch: drive and -Destination is not specified, the cmdlet falls back to the current filesystem location.

When exporting with -Recurse, the subfolder hierarchy is preserved using folder display names that are safe for the filesystem.

The exported .nupkg files can be re-imported using Import-OrchPackage.

## RELATED LINKS

Import-OrchPackage

Get-OrchPackage

Get-OrchPackageVersion

Remove-OrchPackage

Copy-OrchPackage
