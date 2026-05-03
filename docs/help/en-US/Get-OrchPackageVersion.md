---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchPackageVersion.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchPackageVersion
---

# Get-OrchPackageVersion

## SYNOPSIS

Gets the versions of process packages.

## SYNTAX

### __AllParameterSets

```
Get-OrchPackageVersion [[-Id] <string[]>] [[-Version] <string[]>] [-Path <string[]>] [-Recurse]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets all versions of process packages (NuGet packages containing automation processes) from UiPath Orchestrator folder feeds. While Get-OrchPackage returns only the latest version summary for each package, this cmdlet retrieves every published version, making it useful for auditing version history or identifying specific versions for export or removal.

The -Id and -Version parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values. The -Id completion lists package IDs from the target folders, and the -Version completion lists versions for the selected packages.

When specifying the -Path and -Recurse parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: GET /odata/Processes/UiPath.Server.Configuration.OData.GetProcessVersions(processId='{processId}')&feedId={feedId}

OAuth required scopes: OR.Execution or OR.Execution.Read

Required permissions: (Packages.View - Lists versions of a package in a Tenant Feed) and (FolderPackages.View - Lists versions of a package in a Folder Feed)

## EXAMPLES

### Example 1: Get all versions of all packages

```powershell
PS Orch1:\Shared> Get-OrchPackageVersion
```

Gets all versions of all process packages in the current folder.

### Example 2: Get versions of a specific package

```powershell
PS Orch1:\Shared> Get-OrchPackageVersion BlankProcess19
```

Gets all versions of the package "BlankProcess19" from the current folder. Because -Id is at position 0, the parameter name can be omitted.

### Example 3: Get a specific version of a package

```powershell
PS Orch1:\Shared> Get-OrchPackageVersion BlankProcess19 1.0.*
```

Gets all versions of "BlankProcess19" that match the wildcard pattern `1.0.*` (e.g., 1.0.1, 1.0.2, 1.0.3). Both -Id and -Version can be specified positionally.

### Example 4: Get versions from a specific folder using -Path

```powershell
PS C:\> Get-OrchPackageVersion -Path Orch1:\Production -Id BlankProcess19
```

Gets all versions of "BlankProcess19" from the Production folder. When -Path uses an absolute path (Orch1:\...), the command can be run from any location.

### Example 5: Get versions recursively across all folders

```powershell
PS Orch1:\> Get-OrchPackageVersion -Recurse Blank*
```

Gets all versions of packages whose Id starts with "Blank" from all folders in the tenant.

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

Specifies the Id of the process packages to retrieve versions for. Tab completion dynamically suggests package IDs from the target folders.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: false
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

### -Version

Specifies the version of the process packages to retrieve. Tab completion dynamically suggests versions for the selected package IDs.

```yaml
Type: System.String[]
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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe package IDs and versions to this cmdlet via the Id and Version properties.

## OUTPUTS

### UiPath.PowerShell.Entities.Package

Returns Package objects for each version, with properties including Id, Version, Description, and FolderPath.

## NOTES

Packages are folder-scoped entities stored in folder feeds. You must navigate to a folder on the Orch: drive or use -Path to specify target folders.

This cmdlet first retrieves the list of packages (filtered by -Id), then for each package retrieves all versions (filtered by -Version). This may result in multiple API calls for large package collections.

## RELATED LINKS

[Get-OrchPackage](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchPackage.md)

[Export-OrchPackage](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Export-OrchPackage.md)

[Remove-OrchPackage](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchPackage.md)

[Copy-OrchPackage](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchPackage.md)
