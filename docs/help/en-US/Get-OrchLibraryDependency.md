---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchLibraryDependency.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 09/30/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchLibraryDependency
---

# Get-OrchLibraryDependency

## SYNOPSIS

Gets the activity packages a library depends on.

## SYNTAX

### __AllParameterSets

```
Get-OrchLibraryDependency [-Path <string[]>] [-LiteralPath <string[]>] [[-Id] <string[]>]
 [[-Version] <string[]>] [-Dependency <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets the dependency list of a library version -- one row per dependency, naming the activity package and the version range the library asked for.

Orchestrator has no endpoint for a package's dependency list. The list lives in the `.nuspec` inside the `.nupkg`, so the library is downloaded and read. This is the same thing the web UI's "Explore package" does, and the same mechanism `Get-OrchPackageDependency` uses for process packages.

The question this answers is the reverse lookup: which libraries use a given activity package, and at which range. Filter the rows with -Dependency to get that in one command.

Without -Version only each library's latest version is read. A library feed keeps every version ever published and each one is a separate download, so reading all of them is opt-in: `-Version *` does it.

Tenant feed only. There is no -HostFeed, for the same reason `Export-OrchLibrary` has none: the Libraries download endpoint takes no feedId, so a host-feed library cannot be fetched.

Results are cached per library id + version for the session; `Clear-OrchCache` drops them. The workflow list inside the same archive is read at the same time, so a later workflow question about the same library costs no extra download.

The work is staged. Listing the libraries is cheap and runs in parallel; the downloads are sequential and run in their own pass with the real count as the progress denominator, so a large `-Version *` can be cancelled before it finishes.

The -Id and -Version parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values dynamically populated from the target tenant. The -Version completer filters based on the currently specified -Id value.

Primary Endpoint: GET /odata/Libraries (list) then GET /odata/Libraries/UiPath.Server.Configuration.OData.DownloadPackage(key='{libraryId}:{version}')

OAuth required scopes: OR.Execution or OR.Execution.Read

Required permissions: Libraries.View

## EXAMPLES

### Example 1: Get the dependencies of every library's latest version

```powershell
PS C:\> Get-OrchLibraryDependency -Path Orch1:

Package      Version Dependency                  Range
-------      ------- ----------                  -----
1万行のログ  1.0.3   UiPath.Excel.Activities     [2.22.0-preview]
ABC.DEF.GHI  1.0.1   UiPath.Testing.Activities   22.7.0-preview
```

With no -Version, the latest version of each library in the tenant feed is read.

### Example 2: Find every library that uses an activity package

```powershell
PS Orch1:\> Get-OrchLibraryDependency -Path Orch1: -Dependency '*Testing*' |
    Format-Table Package, Dependency, Range

Package      Dependency                  Range
-------      ----------                  -----
1万行のログ  UiPath.Testing.Activities   [23.8.0-preview]
ABC.DEF.GHI  UiPath.Testing.Activities   22.7.0-preview
CodedLib     UiPath.Testing.Activities   24.2.0-preview
```

The reason the cmdlet exists: which libraries depend on the Testing activity package, and at which range. A library version with no matching dependency contributes no rows.

### Example 3: Get the dependencies of one library

```powershell
PS Orch1:\> Get-OrchLibraryDependency ABC.DEF.GHI
```

Because -Id is at position 0, the parameter name can be omitted.

### Example 4: Compare versions of one library

```powershell
PS Orch1:\> Get-OrchLibraryDependency ABC.DEF.GHI 1.0.* |
    Sort-Object Dependency, Version | Format-Table Version, Dependency, Range
```

Both -Id and -Version can be specified positionally and support wildcards, so several versions of one library can be listed side by side to see which dependency moved. Every named version is a separate download.

### Example 5: The second call is free

```powershell
PS Orch1:\> Get-OrchLibraryDependency
PS Orch1:\> Get-OrchLibraryDependency
```

Measured on a real tenant of 31 libraries, the latest version of each -- 89 rows -- took 13.8 seconds cold and 0.001 seconds on the second, identical call.

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

### -LiteralPath

Specifies the target folder or drive by literal path -- wildcard metacharacters (`[`, `]`, `*`, `?`) are treated as literal characters rather than patterns. Accepts the same drive-qualified paths as -Path. Its `PSPath` alias also binds the path of items piped from Get-ChildItem / Get-Item, so you can pipe folders directly. Use -LiteralPath instead of -Path when a folder name contains a wildcard metacharacter.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: false
Aliases:
- PSPath
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

### -Id

Specifies the Id of the libraries whose dependencies are read. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests library IDs from the target tenant. If not specified, every library in the tenant feed is read.

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

Specifies the library versions to read. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests available versions based on the specified -Id. If not specified, only the latest version of each library is read; use `-Version *` to read every published version, at one download each.

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

### -Dependency

Filters the rows to the dependencies whose package id matches, for example `UiPath.UIAutomation*`. Supports wildcards and multiple comma-separated values. A library version with no matching dependency contributes no rows.

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe library IDs and versions to this cmdlet via the Id and Version properties, and drive paths via the Path / PSPath properties.

## OUTPUTS

### UiPath.PowerShell.Entities.PackageDependency

Returns one object per dependency with properties Package (the library id), Version, Dependency, Range (the NuGet version range, for example `[2.22.0-preview]`), and TargetFramework, plus the drive-local Path that records the drive the row was read through.

## NOTES

Libraries are tenant-scoped resources (not folder-scoped). They are NuGet packages containing reusable workflows that can be referenced by automation projects.

The dependency list is read out of the `.nuspec` inside the `.nupkg`, because Orchestrator exposes no endpoint for it. The library is downloaded and read, which is what the web UI's "Explore package" does and what `Get-OrchPackageDependency` does for process packages.

Tenant feed only. There is no -HostFeed, for the same reason `Export-OrchLibrary` has none: the Libraries download endpoint takes no feedId, so a host-feed library cannot be fetched.

Without -Version only each library's latest version is read. A library feed keeps every version ever published and each one is a separate download, so reading all of them is opt-in: `-Version *`.

Results are cached per library id + version for the session. The workflow list inside the same archive is read at the same time, so a later workflow question about the same library costs no extra download. Measured on a real tenant, 31 libraries at their latest version -- 89 rows -- took 13.8 seconds cold and 0.001 seconds on the second, identical call. Use `Clear-OrchCache` to drop the cached lists.

The work is staged: listing the libraries is cheap and runs in parallel, while the downloads are sequential and run in their own pass with the real count as the progress denominator, so a large `-Version *` can be cancelled before it finishes.

## RELATED LINKS

[Get-OrchLibrary](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchLibrary.md)

[Get-OrchLibraryVersion](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchLibraryVersion.md)

[Get-OrchPackageDependency](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchPackageDependency.md)

[Get-OrchProcessDependency](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchProcessDependency.md)

[Clear-OrchCache](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Clear-OrchCache.md)
