---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Copy-OrchPackage
---

# Copy-OrchPackage

## SYNOPSIS

Copies process packages to another folder or Orchestrator instance.

## SYNTAX

### __AllParameterSets

```
Copy-OrchPackage [-Id] <string[]> [[-Version] <string[]>] [-Destination] <string[]> [-Path <string>]
 [-Recurse] [-Depth <uint>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Copies process packages (NuGet packages containing automation processes) from a source folder feed to one or more destination folder feeds in UiPath Orchestrator. The destination can be a different folder on the same Orchestrator instance or a folder on a different Orchestrator instance (cross-drive copy), enabling migration between environments.

The cmdlet downloads each package version from the source and uploads it to each destination. If a package with the same Id and version already exists in the destination, the copy is skipped and a non-terminating error is written. If the source and destination resolve to the same folder, the operation is silently skipped.

The -Id, -Version, -Path, and -Destination parameters support wildcards and tab completion. Press [Ctrl+Space] or [Tab] to see available values. The -Destination completion lists available folder feed paths, excluding the source folder.

With -Recurse, the cmdlet copies packages from all subfolders, preserving the folder hierarchy relative to the source root at each destination. The -Recurse parameter can only be used when the source folder is the root folder.

Note that -Path is a single string (not string[]), unlike most other cmdlets.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: GET /odata/Processes/UiPath.Server.Configuration.OData.DownloadPackage, POST /odata/Processes/UiPath.Server.Configuration.OData.UploadPackage

OAuth required scopes: OR.Execution or OR.Execution.Read (source), OR.Execution (destination)

Required permissions: (Packages.View or FolderPackages.View - source) and (Packages.Create or FolderPackages.Create - destination)

## EXAMPLES

### Example 1: Copy a package to another folder

```powershell
PS Orch1:\Shared> Copy-OrchPackage BlankProcess19 Orch1:\Dept#2
```

Copies all versions of "BlankProcess19" from the current folder (Shared) to the Dept#2 folder. Because -Id is at position 0 and -Destination is at position 2, the parameter names can be omitted when -Version is not specified.

### Example 2: Copy a specific version to another folder

```powershell
PS Orch1:\Shared> Copy-OrchPackage BlankProcess19 1.0.3 Orch1:\Dept#2
```

Copies only version 1.0.3 of "BlankProcess19" to the Dept#2 folder. All three positional parameters (-Id, -Version, -Destination) are specified without parameter names.

### Example 3: Copy packages to another Orchestrator instance

```powershell
PS Orch1:\Shared> Copy-OrchPackage * -Destination Orch2:\Shared
```

Copies all packages and all their versions from the Shared folder on Orch1 to the Shared folder on Orch2. Cross-drive copy enables migration between Orchestrator instances.

### Example 4: Copy packages recursively preserving folder hierarchy

```powershell
PS C:\> Copy-OrchPackage -Path Orch1:\ -Recurse * Orch2:\
```

Copies all packages from all folders on Orch1 to Orch2, preserving the folder hierarchy. Subfolders are matched by relative path on the destination. The -Recurse parameter can only be used when the source is the root folder.

### Example 5: Preview copy with -WhatIf

```powershell
PS Orch1:\Shared> Copy-OrchPackage Blank* -Destination Orch1:\Dept#2 -WhatIf
```

Shows which package versions would be copied without executing the operation. Useful for verifying wildcard patterns before a bulk copy.

## PARAMETERS

### -Path

Specifies the source folder. If not specified, the current folder is used. Unlike most other cmdlets, this parameter accepts a single string (not an array). Tab completion lists available folder feed paths.

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

Includes the source folder and all its subfolders in the copy operation. The folder hierarchy relative to the source root is preserved at the destination. Can only be used when the source folder is the root folder.

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

Specifies the destination folder feed paths. This is a mandatory parameter. Multiple destinations can be specified to copy packages to several folders at once. Can be a folder on the same Orchestrator instance or on a different instance for cross-instance migration. Tab completion lists available folder feed paths, excluding the source folder.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: (All)
  Position: 2
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Id

Specifies the Id of the process packages to copy. This is a mandatory parameter. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests package IDs from the source folder.

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

Specifies the version of the process packages to copy. If not specified, all versions are copied. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests versions for the selected package IDs.

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

Shows what would happen if the cmdlet runs. The cmdlet is not run. The output shows the source package path and the destination folder.

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

You can pipe package IDs, versions, and destination paths to this cmdlet via the Id, Version, and Destination properties.

### System.String

You can pipe a source path to this cmdlet via the Path property.

## OUTPUTS

### UiPath.PowerShell.Entities.BulkItemDtoOfString

Returns the upload result for each successfully copied package at the destination.

## NOTES

The -Path parameter is a single string, not a string array. This differs from most other cmdlets in the module that accept string arrays for -Path.

When the source and destination resolve to the same folder, the operation is silently skipped without error.

The cmdlet downloads each package version once and reuses the downloaded content when uploading to multiple destinations, optimizing cross-instance migrations.

Destination folders must have a FolderHierarchy feed type. Folders with other feed types are skipped with a non-terminating error.

If a package with the same Id and version already exists in the destination, the copy is skipped and a non-terminating error is written.

If the destination is a personal workspace folder, the process cache is also cleared after the upload.

## RELATED LINKS

Get-OrchPackage

Get-OrchPackageVersion

Import-OrchPackage

Export-OrchPackage

Remove-OrchPackage
