---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchPackageDependency.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 09/30/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchPackageDependency
---

# Get-OrchPackageDependency

## SYNOPSIS

Gets the activity packages a package version depends on.

## SYNTAX

### __AllParameterSets

```
Get-OrchPackageDependency [-Path <string[]>] [-LiteralPath <string[]>] [-Recurse] [[-Id] <string[]>]
 [[-Version] <string[]>] [-Dependency <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets the dependency list of a process package version in a folder feed -- one row per dependency, naming the activity package and the version range the project asked for.

Orchestrator has no endpoint for a package's dependencies. The only way to the list is the one the web UI's "Explore package" takes: download the whole `.nupkg` and read what is inside it. This cmdlet makes the same round trip and reads the dependencies out of the `.nuspec`.

This is the feed-side view. The usual way in is `Get-OrchProcessDependency`, which resolves the package version a folder actually deployed (the release's ProcessKey / ProcessVersion) and that folder's feed, so the caller looks up neither. Use this cmdlet for a package that is not deployed anywhere, or to compare two versions of the same package.

Omitting -Version reads the latest version of each package, which is what a new deployment would pick up. Giving -Version selects among the versions the feed holds, with wildcards.

Only the dependency and workflow lists are cached; the package bytes are discarded once read. The cache key is feed + package id + version, because the same id and version can hold different content in a folder's own feed and in the tenant feed. Folders that share a feed therefore share one download, and this cmdlet, `Get-OrchPackageWorkflow` and the process-side pair read the same cache entry -- asking both questions about a package costs one download, not two. `Clear-OrchCache` resets it.

The -Id and -Version parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values. The -Id completion lists package IDs from the target folders, and the -Version completion lists versions for the selected packages.

When specifying the -Path and -Recurse parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: GET /odata/Processes/UiPath.Server.Configuration.OData.DownloadPackage(key='{packageId}:{version}')&feedId={feedId}

OAuth required scopes: OR.Execution or OR.Execution.Read

Required permissions: (Packages.View - Downloads a package from a Tenant Feed) and (FolderPackages.View - Downloads a package from a Folder Feed)

## EXAMPLES

### Example 1: Get the dependencies of the latest version of a package

```powershell
PS C:\> Get-OrchPackageDependency -Path Orch1:\Shared -Id BlankProcess19

Package        Version Dependency                        Range
-------        ------- ----------                        -----
BlankProcess19 1.0.3   UiPath.CodedWorkflows             [24.10.1]
BlankProcess19 1.0.3   UiPath.Cryptography.Activities    [1.6.1]
BlankProcess19 1.0.3   UiPath.System.Activities.Runtime  [25.4.4]
BlankProcess19 1.0.3   UiPath.UIAutomation.Activities    [22.10.3]
```

With no -Version, the latest version the feed holds is read. Because -Id is at position 0, the parameter name can be omitted.

### Example 2: Compare versions of one package

```powershell
PS Orch1:\Shared> Get-OrchPackageDependency BlankProcess19 1.0.* |
    Sort-Object Dependency, Version | Format-Table Version, Dependency, Range
```

Both -Id and -Version can be specified positionally and support wildcards, so several versions of one package can be listed side by side to see which dependency moved.

### Example 3: Find every package that uses an activity package

```powershell
PS Orch1:\> Get-OrchPackageDependency -Recurse -Dependency 'UiPath.UIAutomation*'
```

The feed-side sweep: which packages in the tenant's feeds depend on the UI Automation activity package, and at which range. Use `Get-OrchProcessDependency` instead to ask the same of the versions folders actually run.

### Example 4: A package that is not deployed anywhere

```powershell
PS Orch1:\Shared> Get-OrchPackage | Where-Object Id -notin (Get-OrchProcess).ProcessKey |
    Get-OrchPackageDependency
```

Id, Version and Path bind from the piped Package objects by property name, so the feed can be inspected without a release to go through.

### Example 5: The second call is free

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

Specifies the Id of the process packages whose dependencies are read. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests package IDs from the target folders. If not specified, every package in the target feeds is read.

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

### -Dependency

Filters the rows to the dependencies whose package id matches, for example `UiPath.UIAutomation*`. Supports wildcards and multiple comma-separated values. A package version with no matching dependency contributes no rows.

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

### UiPath.PowerShell.Entities.PackageDependency

Returns one object per dependency with properties Package, Version, Dependency, Range (the NuGet version range, for example `[22.10.3]`), and TargetFramework, plus the drive-local Path and FeedId that record the folder and feed the row was read through.

## NOTES

Packages are folder-scoped entities stored in folder feeds. You must navigate to a folder on the Orch: drive or use -Path to specify target folders.

The dependency list is read out of the `.nuspec` inside the `.nupkg`, because Orchestrator exposes no endpoint for it. The whole package is downloaded; only the dependency and workflow lists are kept, and the package bytes are discarded.

The download is cached per feed + package id + version -- the same id and version can hold different content in a folder's own feed and in the tenant feed, so the feed is part of the identity. Folders that share a feed share one download, and `Get-OrchPackageWorkflow` and the process-side cmdlets read the same cache entry. Measured on a real tenant, the first call for a folder of processes took 3.0 seconds and a second identical call 0.009 seconds; a package shared by two processes was downloaded once. Use `Clear-OrchCache` to drop the cached lists.

Without -Version the latest version of each package is read. Naming several packages without -Version therefore costs one download per package.

## RELATED LINKS

[Get-OrchPackageWorkflow](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchPackageWorkflow.md)

[Get-OrchProcessDependency](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchProcessDependency.md)

[Get-OrchProcessWorkflow](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchProcessWorkflow.md)

[Get-OrchPackage](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchPackage.md)

[Get-OrchPackageVersion](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchPackageVersion.md)

[Get-OrchProcess](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchProcess.md)

[Clear-OrchCache](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Clear-OrchCache.md)
