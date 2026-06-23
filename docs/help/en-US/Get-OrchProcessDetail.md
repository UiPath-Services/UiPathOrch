---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchProcessDetail.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 05/11/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchProcessDetail
---

# Get-OrchProcessDetail

## SYNOPSIS

Gets per-release detailed information from UiPath Orchestrator folders.

## SYNTAX

### __AllParameterSets

```
Get-OrchProcessDetail [-Path <string[]>] [-LiteralPath <string[]>] [-Recurse] [-Depth <uint>] [-Name] <string[]>
 [-CsvEncoding <Encoding>] [-ExportCsv <string>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets the per-release detail payload (ProcessSettings, VideoRecordingSettings, RetentionAction / RetentionPeriod / RetentionBucketId, EntryPointPath) from UiPath Orchestrator folders. The list endpoint backing `Get-OrchProcess` returns shallow Release records; this cmdlet calls the per-id detail endpoint for each matched release, then enriches the result with EntryPointPath (resolved from the package feed) and retention info (separate endpoint).

`-Name` is mandatory by design — the detail path makes one API call per matched release plus side fetches per release, so a default "all releases" would fan out unexpectedly on large folders. Wildcards (including `*`) are accepted; the user just has to type the selector explicitly.

The CSV format produced by `-ExportCsv` matches the shape used by `Get-OrchProcess -ExportCsv`. BucketId values are resolved to human-readable BucketName before writing.

The -Name and -Path parameters support tab completion. -Name completion is dynamically populated from actual releases in the target folders.

Primary Endpoint: GET /odata/Releases({releaseId})?$expand=ReleaseVersions,EntryPoint
Secondary endpoints: GET /odata/Releases/UiPath.Server.Configuration.OData.GetReleaseRetention(...) and the package entry-point lookup.

OAuth required scopes: OR.Execution or OR.Execution.Read

Required permissions: Processes.View

## EXAMPLES

### Example 1: Detail for all releases in the current folder

```powershell
PS Orch1:\Shared> Get-OrchProcessDetail -Name *
```

Returns the detailed payload for every release in `Orch1:\Shared`. The explicit `*` reminds the user that the operation fans out across every release; omitting `-Name` would prompt instead of silently fetching everything.

### Example 2: Detail for a specific release

```powershell
PS Orch1:\Shared> Get-OrchProcessDetail BlankProcess19
```

Returns the detailed payload for the named release. `-Name` is positional (Position 0), so the parameter name can be omitted.

### Example 3: Recursive across a folder tree

```powershell
PS Orch1:\> Get-OrchProcessDetail -Path Shared -Name * -Recurse
```

Walks every folder under `Shared` (recursively) and emits detail for every matched release.

### Example 4: Export to CSV

```powershell
PS C:\> Get-OrchProcessDetail -Path Orch1:\Shared -Name * -ExportCsv C:\temp
```

Exports the detailed payload of every release in `Orch1:\Shared` to `C:\temp\ExportedProcesses.csv`. RetentionBucketId / StaleRetentionBucketId values are resolved to human-readable bucket names in the CSV.

## PARAMETERS

### -Name

Specifies the release names to retrieve detail for. Mandatory by design — see DESCRIPTION. Wildcards (including `*`) are accepted.

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

### -Path

Specifies the target folders. If not specified, the current folder is targeted.

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

Walks subfolders recursively from each `-Path` root.

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

Limits how deep `-Recurse` descends.

```yaml
Type: System.UInt32
DefaultValue: 0
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

### -ExportCsv

Specifies the directory or file path to export release details as a CSV file. If a directory path is specified, the default file name `ExportedProcesses.csv` is used.

```yaml
Type: System.String
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

### -CsvEncoding

Specifies the encoding for CSV export. Default is UTF-8 with BOM for Excel compatibility. Tab completion suggests all available system encodings (e.g., utf-8, shift_jis, us-ascii).

```yaml
Type: System.Text.Encoding
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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe release names to this cmdlet via the Name property.

## OUTPUTS

### UiPath.PowerShell.Entities.Release

Returns the detailed Release entity (one per matched release).

## NOTES

Each matched release triggers one detail-endpoint call plus a small number of side fetches (entry-point resolution and retention lookup). For large folders, prefer narrow `-Name` wildcards over `*` to limit fan-out.

## RELATED LINKS

[Get-OrchProcess](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchProcess.md)

[Update-OrchProcess](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Update-OrchProcess.md)

[Remove-OrchProcess](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchProcess.md)
