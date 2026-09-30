---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchProcessDependency.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 09/30/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchProcessDependency
---

# Get-OrchProcessDependency

## SYNOPSIS

Gets the activity packages a process depends on.

## SYNTAX

### __AllParameterSets

```
Get-OrchProcessDependency [-Path <string[]>] [-LiteralPath <string[]>] [-Recurse] [-Depth <uint>]
 [[-Name] <string[]>] [[-Dependency] <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets the dependency list of the package version a process actually runs -- one row per dependency, naming the activity package and the version range the project asked for.

Orchestrator has no endpoint for a package's dependencies. The only way to the list is the one the web UI's "Explore package" takes: download the whole `.nupkg` and read what is inside it. This cmdlet makes the same round trip and reads the dependencies out of the `.nuspec`.

Going through the process rather than the package is what makes the question cheap to ask. The cmdlet resolves the package version that folder actually deployed (the release's ProcessKey / ProcessVersion) and that folder's feed, so the caller looks up neither. For a package that is not deployed anywhere, or to compare two versions of one package, use `Get-OrchPackageDependency`, which is the feed-side view of the same data.

The main reason the cmdlet exists is the sweep: `Get-OrchProcessDependency -Path Orch1:\ -Recurse -Dependency 'UiPath.UIAutomation*'` lists which processes across the tenant use that activity package, and at which range.

Only the dependency and workflow lists are cached; the package bytes are discarded once read. The cache key is feed + package id + version, because the same id and version can hold different content in a folder's own feed and in the tenant feed. Folders that share a feed therefore share one download, and this cmdlet and `Get-OrchProcessWorkflow` share the same cache entry -- asking both questions about a package costs one download, not two. `Clear-OrchCache` resets it.

A process whose release names no package version is skipped with a warning.

The -Name parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available process names dynamically populated from the target folders. Multiple values can be specified using comma-separated text that includes wildcards.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: GET /odata/Releases (to resolve the deployed version and the folder feed), then GET /odata/Processes/UiPath.Server.Configuration.OData.DownloadPackage(key='{processKey}:{processVersion}')&feedId={feedId}

OAuth required scopes: OR.Execution or OR.Execution.Read

Required permissions: Processes.View (reads the releases) and (Packages.View - Downloads a package from a Tenant Feed) and (FolderPackages.View - Downloads a package from a Folder Feed)

## EXAMPLES

### Example 1: Get the dependencies of a process

```powershell
PS C:\> Get-OrchProcessDependency -Path Orch1:\Shared -Name BlankProcess*

Process        Package        Version Dependency                        Range
-------        -------        ------- ----------                        -----
BlankProcess19 BlankProcess19 1.0.3   UiPath.CodedWorkflows             [24.10.1]
BlankProcess19 BlankProcess19 1.0.3   UiPath.Cryptography.Activities    [1.6.1]
BlankProcess19 BlankProcess19 1.0.3   UiPath.System.Activities.Runtime  [25.4.4]
BlankProcess19 BlankProcess19 1.0.3   UiPath.UIAutomation.Activities    [22.10.3]
```

The four activity packages the deployed version 1.0.3 of BlankProcess19 depends on. The version the folder runs was resolved from the release; it was not given on the command line.

### Example 2: Get the dependencies of every process in the current folder

```powershell
PS Orch1:\Shared> Get-OrchProcessDependency
```

Gets the dependencies of every process in the current folder. Because -Name is at position 0, the parameter name can be omitted when a selector is given.

### Example 3: Find every process that uses an activity package

```powershell
PS Orch1:\> Get-OrchProcessDependency -Recurse -Dependency 'UiPath.UIAutomation*'
```

The sweep the cmdlet exists for: which processes in the tenant use the UI Automation activity package, and at which range. -Dependency is at position 1 and supports wildcards.

### Example 4: Group a tenant by activity package version

```powershell
PS Orch1:\> Get-OrchProcessDependency -Recurse -Dependency 'UiPath.UIAutomation*' |
    Group-Object Range | Sort-Object Name
```

Shows how the tenant's processes are spread across ranges of one activity package -- the shape of an upgrade.

### Example 5: Pipe processes into the cmdlet

```powershell
PS Orch1:\Shared> Get-OrchProcess Blank* | Get-OrchProcessDependency
```

Name and Path bind from the piped Release objects by property name, so a selection made with `Get-OrchProcess` can be reused.

### Example 6: The second call is free

```powershell
PS Orch1:\Shared> Measure-Command { Get-OrchProcessDependency } | Select-Object TotalSeconds
PS Orch1:\Shared> Measure-Command { Get-OrchProcessDependency } | Select-Object TotalSeconds
```

Measured on a real tenant, the first call for a folder of processes took 3.0 seconds and the second 0.009 seconds. A package shared by two processes is downloaded once.

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

### -Depth

Specifies the depth for recursion into the target folders. A depth of 0 targets only the current folder with no subfolders included. When -Depth is specified, -Recurse is implied.

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

### -Name

Specifies the names of the processes whose dependencies are read. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests process names from the target folders. If not specified, every process in the target folders is read.

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

### -Dependency

Filters the rows to the dependencies whose package id matches, for example `UiPath.UIAutomation*`. Supports wildcards and multiple comma-separated values. A process with no matching dependency contributes no rows.

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

You can pipe process names to this cmdlet via the Name property, and folder paths via the Path / PSPath properties.

## OUTPUTS

### UiPath.PowerShell.Entities.ProcessDependency

Returns one object per dependency with properties Process, Package, Version, Dependency, Range (the NuGet version range, for example `[22.10.3]`), and TargetFramework, plus the drive-local Path and FeedId that record the folder and feed the row was read through.

## NOTES

Processes are folder-scoped entities. You must navigate to a folder on the Orch: drive or use -Path to specify target folders.

The dependency list is read out of the `.nuspec` inside the `.nupkg`, because Orchestrator exposes no endpoint for it. The whole package is downloaded; only the dependency and workflow lists are kept, and the package bytes are discarded.

The download is cached per feed + package id + version -- the same id and version can hold different content in a folder's own feed and in the tenant feed, so the feed is part of the identity. Folders that share a feed share one download, and `Get-OrchProcessWorkflow` reads the same cache entry. Measured on a real tenant, the first call for a folder of processes took 3.0 seconds and a second identical call 0.009 seconds; a package shared by two processes was downloaded once. Use `Clear-OrchCache` to drop the cached lists.

A process whose release names no package version is skipped with a warning.

## RELATED LINKS

[Get-OrchProcessWorkflow](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchProcessWorkflow.md)

[Get-OrchPackageDependency](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchPackageDependency.md)

[Get-OrchPackageWorkflow](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchPackageWorkflow.md)

[Get-OrchProcess](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchProcess.md)

[Get-OrchPackage](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchPackage.md)

[Get-OrchPackageVersion](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchPackageVersion.md)

[Clear-OrchCache](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Clear-OrchCache.md)
