---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchLibrary.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchLibrary
---

# Get-OrchLibrary

## SYNOPSIS

Gets the library packages from UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
Get-OrchLibrary [[-Id] <string[]>] [-HostFeed] [-Path <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets library packages (NuGet packages containing reusable workflows) from UiPath Orchestrator. Libraries are tenant-scoped resources stored in the Orchestrator NuGet feed. By default, libraries from the tenant feed are returned. Use the -HostFeed switch to retrieve libraries from the host feed instead.

The -Id parameter supports tab completion and wildcards. Press [Ctrl+Space] or [Tab] to see available library IDs dynamically populated from the target tenant. When no -Id is specified, all libraries are returned.

Primary Endpoint: GET /odata/Libraries

OAuth required scopes: OR.Execution or OR.Execution.Read

Required permissions: Libraries.View

## EXAMPLES

### Example 1: Get all libraries in the current tenant

```powershell
PS Orch1:\> Get-OrchLibrary
```

Gets all library packages from the tenant feed of the current Orchestrator drive. Returns Library objects ordered alphabetically by Id.

### Example 2: Get a specific library by Id

```powershell
PS Orch1:\> Get-OrchLibrary ABC.DEF.GHI
```

Gets the library package with the Id "ABC.DEF.GHI" from the tenant feed. The -Id parameter is positional (position 0), so the parameter name can be omitted.

### Example 3: Get libraries using wildcards

```powershell
PS Orch1:\> Get-OrchLibrary ABC.*
```

Gets all library packages whose Id starts with "ABC." from the tenant feed. The -Id parameter supports wildcards for flexible filtering.

### Example 4: Get libraries from the host feed

```powershell
PS Orch1:\> Get-OrchLibrary -HostFeed
```

Gets all library packages from the host-level feed instead of the tenant feed.

### Example 5: Get libraries from multiple Orchestrator instances

```powershell
PS C:\> Get-OrchLibrary -Path Orch1:\,Orch2:\ -Id ABC.*
```

Gets libraries matching `ABC.*` from both Orch1 and Orch2 tenants. When -Path uses absolute paths, the command can be run from any location.

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

Retrieves libraries from the host-level feed instead of the tenant feed. When the Orchestrator is configured with a host feed, this switch allows access to libraries shared across all tenants.

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

Specifies the Id of the library packages to retrieve. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests library IDs from the target tenant.

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

You can pipe library IDs to this cmdlet via the Id property.

## OUTPUTS

### UiPath.PowerShell.Entities.Library

Returns Library objects with properties including Id, Version, and Path.

## NOTES

Libraries are tenant-scoped resources (not folder-scoped). They are NuGet packages containing reusable workflows that can be referenced by automation projects.

## RELATED LINKS

[Get-OrchLibraryVersion](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchLibraryVersion.md)

[Import-OrchLibrary](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Import-OrchLibrary.md)

[Export-OrchLibrary](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Export-OrchLibrary.md)

[Remove-OrchLibrary](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchLibrary.md)

[Copy-OrchLibrary](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchLibrary.md)
