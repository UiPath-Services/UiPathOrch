---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchPackageWorkflow.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 09/30/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchPackageWorkflow
---

# Get-OrchPackageWorkflow

## SYNOPSIS

Gets the workflows inside a package version.

## SYNTAX

### __AllParameterSets

```
Get-OrchPackageWorkflow [-Path <string[]>] [-LiteralPath <string[]>] [-Recurse] [[-Id] <string[]>]
 [[-Version] <string[]>] [-EntryPoint] [-Workflow <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets the `.xaml` workflows carried by a process package version in a folder feed -- one row per file, at the path it has inside the project, flagged with which ones the project publishes as entry points. This is the file list the web UI's "Explore package" shows.

Orchestrator has no endpoint for a package's file list. The only way to it is the one that web UI takes: download the whole `.nupkg` and read what is inside. This cmdlet makes the same round trip; the workflows are the `.xaml` entries, and the entry points come from `project.json` -- its `main` fills IsMain, and `entryPoints[].filePath` fills IsEntryPoint.

This is the feed-side view. The usual way in is `Get-OrchProcessWorkflow`, which resolves the package version a folder actually deployed (the release's ProcessKey / ProcessVersion) and that folder's feed, so the caller looks up neither. Use this cmdlet for a package that is not deployed anywhere, or to compare two versions of the same package.

Omitting -Version reads the latest version of each package, which is what a new deployment would pick up. Giving -Version selects among the versions the feed holds, with wildcards.

Only the workflow and dependency lists are cached; the package bytes are discarded once read. The cache key is feed + package id + version, because the same id and version can hold different content in a folder's own feed and in the tenant feed. Folders that share a feed therefore share one download, and this cmdlet, `Get-OrchPackageDependency` and the process-side pair read the same cache entry -- asking both questions about a package costs one download, not two. `Clear-OrchCache` resets it.

The -Id and -Version parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values. The -Id completion lists package IDs from the target folders, and the -Version completion lists versions for the selected packages.

When specifying the -Path and -Recurse parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: GET /odata/Processes/UiPath.Server.Configuration.OData.DownloadPackage(key='{packageId}:{version}')&feedId={feedId}

OAuth required scopes: OR.Execution or OR.Execution.Read

Required permissions: (Packages.View - Downloads a package from a Tenant Feed) and (FolderPackages.View - Downloads a package from a Folder Feed)

## EXAMPLES

### Example 1: Get the workflows of the latest version of a package

```powershell
PS C:\> Get-OrchPackageWorkflow -Path Orch1:\Shared -Id BlankProcess19 |
    Format-Table Package, Version, Workflow, IsEntryPoint, IsMain
```

With no -Version, the latest version the feed holds is read. Because -Id is at position 0, the parameter name can be omitted.

### Example 2: Compare the file list of two versions

```powershell
PS Orch1:\Shared> Get-OrchPackageWorkflow BlankProcess19 1.0.* |
    Sort-Object Version, Workflow | Format-Table Version, Workflow, Length
```

Both -Id and -Version can be specified positionally and support wildcards, so the `.xaml` set of several versions can be listed side by side to see which files were added or removed.

### Example 3: Only the entry points

```powershell
PS Orch1:\Shared> Get-OrchPackageWorkflow BlankProcess19 -EntryPoint
```

Narrows the rows to the workflows the project publishes as entry points.

### Example 4: Find a workflow file across the feeds

```powershell
PS Orch1:\> Get-OrchPackageWorkflow -Recurse -Workflow '*Invoice*'
```

Lists every package in the tenant's feeds that carries a workflow matching the pattern. -Workflow matches against the path inside the project and supports wildcards (`Sub/*.xaml` works as well).

### Example 5: A package that is not deployed anywhere

```powershell
PS Orch1:\Shared> Get-OrchPackage | Where-Object Id -notin (Get-OrchProcess).ProcessKey |
    Get-OrchPackageWorkflow -EntryPoint
```

Id, Version and Path bind from the piped Package objects by property name, so the feed can be inspected without a release to go through.

### Example 6: Both questions, one download

```powershell
PS Orch1:\Shared> Get-OrchPackageDependency BlankProcess19
PS Orch1:\Shared> Get-OrchPackageWorkflow BlankProcess19
```

The second command answers from the cache entry the first one filled. Measured on a real tenant, the first call for a folder of processes took 3.0 seconds and the second 0.009 seconds.

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

Specifies the Id of the process packages whose workflows are read. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests package IDs from the target folders. If not specified, every package in the target feeds is read.

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

Specifies the versions to read. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests versions for the selected package IDs. If not specified, the latest version of each package is read.

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

### -EntryPoint

Returns only the workflows the project publishes as entry points (`project.json` `main` and `entryPoints[].filePath`).

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

### -Workflow

Filters the rows by the workflow path inside the project, for example `*Invoice*` or `Sub/*.xaml`. Supports wildcards and multiple comma-separated values. A package version with no matching workflow contributes no rows.

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

You can pipe package IDs and versions to this cmdlet via the Id and Version properties, and folder paths via the Path / PSPath properties.

## OUTPUTS

### UiPath.PowerShell.Entities.PackageWorkflow

Returns one object per `.xaml` with properties Package, Version, Workflow (the path inside the project), Length (the uncompressed size in bytes), IsEntryPoint, and IsMain, plus the drive-local Path and FeedId that record the folder and feed the row was read through.

## NOTES

Packages are folder-scoped entities stored in folder feeds. You must navigate to a folder on the Orch: drive or use -Path to specify target folders.

The file list is read out of the `.nupkg` itself, because Orchestrator exposes no endpoint for it. The whole package is downloaded; only the workflow and dependency lists are kept, and the package bytes are discarded. Entry points come from `project.json`: `main` sets IsMain, `entryPoints[].filePath` sets IsEntryPoint.

The download is cached per feed + package id + version -- the same id and version can hold different content in a folder's own feed and in the tenant feed, so the feed is part of the identity. Folders that share a feed share one download, and `Get-OrchPackageDependency` and the process-side cmdlets read the same cache entry. Measured on a real tenant, the first call for a folder of processes took 3.0 seconds and a second identical call 0.009 seconds; a package shared by two processes was downloaded once. Use `Clear-OrchCache` to drop the cached lists.

Without -Version the latest version of each package is read. Naming several packages without -Version therefore costs one download per package.

## RELATED LINKS

[Get-OrchPackageDependency](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchPackageDependency.md)

[Get-OrchProcessWorkflow](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchProcessWorkflow.md)

[Get-OrchProcessDependency](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchProcessDependency.md)

[Get-OrchPackage](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchPackage.md)

[Get-OrchPackageVersion](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchPackageVersion.md)

[Clear-OrchCache](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Clear-OrchCache.md)
