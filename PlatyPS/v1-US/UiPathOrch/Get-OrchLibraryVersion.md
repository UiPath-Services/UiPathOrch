---
document type: cmdlet
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchLibraryVersion
---

# Get-OrchLibraryVersion

## SYNOPSIS

Gets the versions of library packages from UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
Get-OrchLibraryVersion [[-Id] <string[]>] [[-Version] <string[]>] [-HostFeed] [-Path <string[]>]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets version information for library packages (NuGet packages containing reusable workflows) from UiPath Orchestrator. While `Get-OrchLibrary` returns one entry per library, this cmdlet returns all published versions of each library. Both -Id and -Version parameters support wildcards, allowing flexible queries such as retrieving all versions of a specific library or finding a particular version across multiple libraries.

By default, versions from the tenant feed are returned. Use the -HostFeed switch to retrieve versions from the host feed instead.

The -Id and -Version parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values dynamically populated from the target tenant. The -Version completer filters based on the currently specified -Id value.

Primary Endpoint: /odata/Libraries/UiPath.Server.Configuration.OData.GetVersions(packageId='{libraryId}')

OAuth required scopes: OR.Execution or OR.Execution.Read

Required permissions: Libraries.View

## EXAMPLES

### Example 1: Get all versions of all libraries

```powershell
PS Orch1:\> Get-OrchLibraryVersion
```

Gets all versions of all library packages from the tenant feed. This may return multiple entries per library, one for each published version.

### Example 2: Get all versions of a specific library

```powershell
PS Orch1:\> Get-OrchLibraryVersion ABC.DEF.GHI
```

Gets all published versions of the library "ABC.DEF.GHI". The -Id parameter is positional (position 0), so the parameter name can be omitted.

### Example 3: Get a specific version of a library

```powershell
PS Orch1:\> Get-OrchLibraryVersion ABC.DEF.GHI 1.0.*
```

Gets all versions of "ABC.DEF.GHI" that match the pattern `1.0.*` (e.g., 1.0.0, 1.0.1, 1.0.2). Both -Id (position 0) and -Version (position 1) are positional and support wildcards.

### Example 4: Get versions from the host feed

```powershell
PS Orch1:\> Get-OrchLibraryVersion -HostFeed -Id ABC.*
```

Gets all versions of libraries whose Id starts with "ABC." from the host-level feed instead of the tenant feed.

## PARAMETERS

### -Path

Specifies the target Orchestrator drives. If not specified, the current drive is targeted. Use tab completion to see available drives.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: false
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

### -HostFeed

Retrieves library versions from the host-level feed instead of the tenant feed. When the Orchestrator is configured with a host feed, this switch allows access to library versions shared across all tenants.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
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

Specifies the Id of the library packages whose versions to retrieve. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests library IDs from the target tenant.

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

### -Version

Specifies the version of the library packages to retrieve. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests available versions based on the specified -Id.

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe library IDs and versions to this cmdlet via the Id and Version properties.

## OUTPUTS

### UiPath.PowerShell.Entities.Library

Returns Library objects with properties including Id, Version, and Path. Each object represents a specific version of a library package.

## NOTES

Libraries are tenant-scoped resources (not folder-scoped). They are NuGet packages containing reusable workflows that can be referenced by automation projects.

## RELATED LINKS

Get-OrchLibrary

Export-OrchLibrary

Remove-OrchLibrary

Copy-OrchLibrary
