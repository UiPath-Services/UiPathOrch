---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Import-OrchPackage.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Import-OrchPackage
---

# Import-OrchPackage

## SYNOPSIS

Imports process packages from local .nupkg files into UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
Import-OrchPackage [-Source] <string[]> [[-Path] <string[]>] [-Recurse] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Imports process packages (NuGet .nupkg files) from the local filesystem into UiPath Orchestrator folder feeds. The -Source parameter specifies the path to .nupkg files or directories containing them. Multiple files can be imported in a single operation.

When -Recurse is specified for the -Source parameter, the cmdlet searches subdirectories for .nupkg files and maps them to corresponding Orchestrator folders by relative path. This enables bulk import of previously exported package hierarchies. The -Recurse parameter can only be used when the target folder is the tenant's root folder.

If a package with the same Id and version already exists in the destination folder feed, the import is skipped and an error is written.

Primary Endpoint: POST /odata/Processes/UiPath.Server.Configuration.OData.UploadPackage

OAuth required scopes: OR.Execution

Required permissions: (Packages.Create - Uploads a package to a Tenant Feed) and (FolderPackages.Create - Uploads a package to a Folder Feed)

## EXAMPLES

### Example 1: Import a single package file

```powershell
PS Orch1:\Shared> Import-OrchPackage C:\packages\MyProcess.1.0.0.nupkg
```

Imports the specified .nupkg file into the current folder's package feed. Because -Source is at position 0, the parameter name can be omitted.

### Example 2: Import all packages from a directory

```powershell
PS Orch1:\Shared> Import-OrchPackage C:\packages\
```

Imports all .nupkg files found in the specified directory into the current folder's package feed.

### Example 3: Import packages into a specific folder using -Path

```powershell
PS C:\> Import-OrchPackage C:\packages\MyProcess.1.0.0.nupkg Orch1:\Production
```

Imports the package into the Production folder. Both -Source (position 0) and -Path (position 1) can be specified positionally. When -Path uses an absolute path, the command can be run from any location.

### Example 4: Import packages recursively preserving folder structure

```powershell
PS Orch1:\> Import-OrchPackage C:\exported-packages\ -Recurse
```

Imports all .nupkg files from C:\exported-packages\ and its subdirectories, mapping each subdirectory to the corresponding Orchestrator folder by relative path. The -Recurse parameter can only be used when the current location is the tenant's root folder.

## PARAMETERS

### -Path

Specifies the target Orchestrator folders to import packages into. If not specified, the current folder is targeted. Supports wildcards and comma-separated values for multiple folders.

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

### -Recurse

Searches subdirectories of -Source for .nupkg files and maps them to corresponding Orchestrator folders by relative path. Can only be used when the target folder is the tenant's root folder.

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

### -Source

Specifies the path to .nupkg files or directories containing .nupkg files on the local filesystem. This is a mandatory parameter. Multiple paths can be specified.

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

You can pipe source file paths to this cmdlet via the Source property.

## OUTPUTS

### UiPath.PowerShell.Entities.BulkItemDtoOfString

Returns the upload result for each successfully imported package.

## NOTES

The cmdlet validates that the destination folder has a FolderHierarchy feed type. Folders with other feed types are skipped with a warning.

When -Recurse is used, the directory structure under -Source is expected to match the Orchestrator folder hierarchy. Subdirectories that do not correspond to existing Orchestrator folders are skipped with a warning.

If a package with the same Id and version already exists in the target folder feed, the import is skipped and a non-terminating error is written.

## RELATED LINKS

Export-OrchPackage

Get-OrchPackage

Get-OrchPackageVersion

Remove-OrchPackage

Copy-OrchPackage
