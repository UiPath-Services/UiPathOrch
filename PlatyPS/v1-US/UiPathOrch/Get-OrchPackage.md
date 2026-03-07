---
document type: cmdlet
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchPackage
---

# Get-OrchPackage

## SYNOPSIS

Gets the process packages.

## SYNTAX

### __AllParameterSets

```
Get-OrchPackage [[-Id] <string[]>] [-Path <string[]>] [-Recurse] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets process packages (NuGet packages containing automation processes) from UiPath Orchestrator folder feeds. Each package represents an automation project published to Orchestrator. The cmdlet returns the latest version summary for each package; use Get-OrchPackageVersion to retrieve all versions.

The -Id parameter supports wildcards and tab completion. Press [Ctrl+Space] or [Tab] to see available package IDs from the target folders. Multiple IDs can be specified using comma-separated text that includes wildcards.

When specifying the -Path and -Recurse parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: GET /odata/Processes&feedId={feedId}

OAuth required scopes: OR.Execution or OR.Execution.Read

Required permissions: (Packages.View - Lists packages in a Tenant Feed) and (FolderPackages.View - Lists packages in a Folder Feed)

## EXAMPLES

### Example 1: Get all packages in the current folder

```powershell
PS Orch1:\Shared> Get-OrchPackage
```

Gets all process packages from the current folder's package feed.

### Example 2: Get a specific package by Id

```powershell
PS Orch1:\Shared> Get-OrchPackage BlankProcess19
```

Gets the process package with the Id "BlankProcess19" from the current folder. Because -Id is at position 0, the parameter name can be omitted.

### Example 3: Get packages using wildcards

```powershell
PS Orch1:\Shared> Get-OrchPackage Blank*
```

Gets all process packages whose Id starts with "Blank" from the current folder.

### Example 4: Get packages recursively from all folders

```powershell
PS Orch1:\> Get-OrchPackage -Recurse
```

Gets all process packages from the tenant root and all its subfolders. Results are grouped by folder feed.

### Example 5: Get packages from a specific folder using -Path

```powershell
PS C:\> Get-OrchPackage -Path Orch1:\Production -Id BlankProcess19
```

Gets the package "BlankProcess19" from the Production folder. When -Path uses an absolute path (Orch1:\...), the command can be run from any location.

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

### -Id

Specifies the Id of the process packages to retrieve. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests package IDs from the target folders.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: (All)
  Position: 0
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
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

You can pipe package IDs to this cmdlet via the Id property.

## OUTPUTS

### UiPath.PowerShell.Entities.Package

Returns Package objects with properties including Id, Version, Description, and FolderPath.

## NOTES

Packages are folder-scoped entities stored in folder feeds. You must navigate to a folder on the Orch: drive or use -Path to specify target folders.

The cmdlet retrieves packages in parallel across multiple folder feeds for efficient operation.

## RELATED LINKS

Get-OrchPackageVersion

Import-OrchPackage

Export-OrchPackage

Remove-OrchPackage

Copy-OrchPackage
