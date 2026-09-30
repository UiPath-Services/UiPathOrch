---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchProcessWorkflow.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 09/30/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchProcessWorkflow
---

# Get-OrchProcessWorkflow

## SYNOPSIS

Gets the workflows inside the package a process runs.

## SYNTAX

### __AllParameterSets

```
Get-OrchProcessWorkflow [-Path <string[]>] [-LiteralPath <string[]>] [-Recurse] [-Depth <uint>]
 [[-Name] <string[]>] [[-Workflow] <string[]>] [-EntryPoint] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets the `.xaml` workflows carried by the package version a process actually runs -- one row per file, at the path it has inside the project, flagged with which ones the project publishes as entry points.

Orchestrator has no endpoint for a package's file list. The only way to it is the one the web UI's "Explore package" takes: download the whole `.nupkg` and read what is inside. This cmdlet makes the same round trip; the workflows are the `.xaml` entries, and the entry points come from `project.json` -- its `main` fills IsMain, and `entryPoints[].filePath` fills IsEntryPoint.

Going through the process rather than the package is what makes the question cheap to ask. The cmdlet resolves the package version that folder actually deployed (the release's ProcessKey / ProcessVersion) and that folder's feed, so the caller looks up neither. For a package that is not deployed anywhere, or to compare two versions of one package, use `Get-OrchPackageWorkflow`, which is the feed-side view of the same data.

Only the workflow and dependency lists are cached; the package bytes are discarded once read. The cache key is feed + package id + version, because the same id and version can hold different content in a folder's own feed and in the tenant feed. Folders that share a feed therefore share one download, and this cmdlet and `Get-OrchProcessDependency` share the same cache entry -- asking both questions about a package costs one download, not two. `Clear-OrchCache` resets it.

A process whose release names no package version is skipped with a warning.

The -Name parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available process names dynamically populated from the target folders. Multiple values can be specified using comma-separated text that includes wildcards.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: GET /odata/Releases (to resolve the deployed version and the folder feed), then GET /odata/Processes/UiPath.Server.Configuration.OData.DownloadPackage(key='{processKey}:{processVersion}')&feedId={feedId}

OAuth required scopes: OR.Execution or OR.Execution.Read

Required permissions: Processes.View (reads the releases) and (Packages.View - Downloads a package from a Tenant Feed) and (FolderPackages.View - Downloads a package from a Folder Feed)

## EXAMPLES

### Example 1: Get the workflows of a process

```powershell
PS C:\> Get-OrchProcessWorkflow -Path Orch1:\Shared -Name '複数ほえほえ' |
    Format-Table Process, Workflow, IsEntryPoint, IsMain

Process    Workflow   IsEntryPoint IsMain
-------    --------   ------------ ------
複数ほえほえ    Main.xaml          True   True
複数ほえほえ    Sub.xaml           True  False
複数ほえほえ    Sub2.xaml          True  False
```

The three workflows the deployed package carries. All three are published as entry points, and `Main.xaml` is the project's main workflow.

### Example 2: Get the workflows of every process in the current folder

```powershell
PS Orch1:\Shared> Get-OrchProcessWorkflow
```

Gets the workflows of every process in the current folder. Because -Name is at position 0, the parameter name can be omitted when a selector is given.

### Example 3: Only the entry points

```powershell
PS Orch1:\Shared> Get-OrchProcessWorkflow BlankProcess19 -EntryPoint
```

Narrows the rows to the workflows the project publishes as entry points -- the ones a job can be started on.

### Example 4: Find a workflow file across the tenant

```powershell
PS Orch1:\> Get-OrchProcessWorkflow -Recurse -Workflow '*Invoice*'
```

Lists every process in the tenant whose package carries a workflow matching the pattern. -Workflow is at position 1, matches against the path inside the project, and supports wildcards (`Sub/*.xaml` works as well).

### Example 5: Pipe processes into the cmdlet

```powershell
PS Orch1:\Shared> Get-OrchProcess Blank* | Get-OrchProcessWorkflow -EntryPoint
```

Name and Path bind from the piped Release objects by property name, so a selection made with `Get-OrchProcess` can be reused.

### Example 6: Both questions, one download

```powershell
PS Orch1:\Shared> Get-OrchProcessDependency BlankProcess19
PS Orch1:\Shared> Get-OrchProcessWorkflow BlankProcess19
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

Specifies the names of the processes whose workflows are read. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests process names from the target folders. If not specified, every process in the target folders is read.

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

### -Workflow

Filters the rows by the workflow path inside the project, for example `*Invoice*` or `Sub/*.xaml`. Supports wildcards and multiple comma-separated values. A process with no matching workflow contributes no rows.

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe process names to this cmdlet via the Name property, and folder paths via the Path / PSPath properties.

## OUTPUTS

### UiPath.PowerShell.Entities.ProcessWorkflow

Returns one object per `.xaml` with properties Process, Package, Version, Workflow (the path inside the project), Length (the uncompressed size in bytes), IsEntryPoint, and IsMain, plus the drive-local Path and FeedId that record the folder and feed the row was read through.

## NOTES

Processes are folder-scoped entities. You must navigate to a folder on the Orch: drive or use -Path to specify target folders.

The file list is read out of the `.nupkg` itself, because Orchestrator exposes no endpoint for it. The whole package is downloaded; only the workflow and dependency lists are kept, and the package bytes are discarded. Entry points come from `project.json`: `main` sets IsMain, `entryPoints[].filePath` sets IsEntryPoint.

The download is cached per feed + package id + version -- the same id and version can hold different content in a folder's own feed and in the tenant feed, so the feed is part of the identity. Folders that share a feed share one download, and `Get-OrchProcessDependency` reads the same cache entry. Measured on a real tenant, the first call for a folder of processes took 3.0 seconds and a second identical call 0.009 seconds; a package shared by two processes was downloaded once. Use `Clear-OrchCache` to drop the cached lists.

A process whose release names no package version is skipped with a warning.

## RELATED LINKS

[Get-OrchProcessDependency](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchProcessDependency.md)

[Get-OrchPackageWorkflow](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchPackageWorkflow.md)

[Get-OrchPackageDependency](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchPackageDependency.md)

[Get-OrchProcess](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchProcess.md)

[Get-OrchPackageVersion](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchPackageVersion.md)

[Clear-OrchCache](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Clear-OrchCache.md)
