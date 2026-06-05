---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchProcess.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchProcess
---

# Get-OrchProcess

## SYNOPSIS

Gets processes from UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
Get-OrchProcess [-Path <string[]>] [-LiteralPath <string[]>] [-Recurse] [-Depth <uint>] [[-Name] <string[]>]
 [-CsvEncoding <Encoding>] [-ExpandDetails] [-ExportCsv <string>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets process information from UiPath Orchestrator folders. A process (Release entity) links a published package (NuGet) to a folder, making the automation available for execution. The PackageId (ProcessKey) identifies the underlying package, while the process Name is the display name within the folder.

By default, this cmdlet returns basic Release properties such as Name, Description, ProcessVersion, IsLatestVersion, and ProcessType. To fetch additional properties (ProcessSettings, VideoRecordingSettings, RetentionAction / RetentionPeriod / RetentionBucketId, EntryPointPath), use `Get-OrchProcessDetail` — which makes one API call per matched release and requires an explicit `-Name` selector.

> **Deprecated:** `-ExpandDetails` is deprecated and will be removed in a future major release. Use `Get-OrchProcessDetail` instead. `-ExportCsv` continues to be supported and transparently uses the detail cache to enrich the exported rows (the CSV row shape is the Release entity, matching this cmdlet's output type).

The -Name parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available process names dynamically populated from the target folders. Multiple values can be specified using comma-separated text that includes wildcards.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: GET /odata/Releases/?$expand=Environment,CurrentVersion,ReleaseVersions,EntryPoint

OAuth required scopes: OR.Execution or OR.Execution.Read

Required permissions: Processes.View

## EXAMPLES

### Example 1: Get all processes in the current folder

```powershell
PS Orch1:\Shared> Get-OrchProcess
```

Gets all processes from the current folder and returns basic Release information including Name, Description, ProcessVersion, and IsLatestVersion.

### Example 2: Get processes by name with wildcards

```powershell
PS Orch1:\Shared> Get-OrchProcess Blank*
```

Gets all processes whose name starts with "Blank" from the current folder. The -Name parameter is positional (position 0) and supports wildcards.

### Example 3 (deprecated): -ExpandDetails

```powershell
PS Orch1:\Shared> Get-OrchProcess BlankProcess19 -ExpandDetails
```

Emits a deprecation warning and routes through `Get-OrchProcessDetail`. Use `Get-OrchProcessDetail BlankProcess19` directly instead.

### Example 4: Get processes from a specific folder

```powershell
PS C:\> Get-OrchProcess -Path Orch1:\Production BlankProcess19
```

Gets the process named "BlankProcess19" from the Production folder. When -Path uses an absolute path (Orch1:\...), the command can be run from any location.

### Example 5: Get processes recursively from all folders

```powershell
PS Orch1:\> Get-OrchProcess -Recurse | Format-Table Name, ProcessVersion, IsLatestVersion
```

Gets all processes from all folders recursively and displays them in a table format with selected properties.

### Example 6: Export processes to CSV

```powershell
PS Orch1:\Shared> Get-OrchProcess -ExportCsv c:processes.csv
```

Exports all processes in the current folder to a CSV file. The `c:` prefix writes the file to the current directory of the C: drive, which is useful when the current location is an Orch: drive. The CSV includes columns such as Path, Name, Id, Version, Description, EntryPoint, InputArguments, RetentionAction, RetentionPeriod, RetentionBucket, VideoRecordingType, Tags, and more. BucketId values are resolved to human-readable BucketName in the export.

### Example 7: Export processes recursively

```powershell
PS Orch1:\> Get-OrchProcess -Recurse -ExportCsv c:all-processes.csv
```

Exports processes from all folders recursively to a CSV file.

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

### -ExpandDetails

**Deprecated.** Routes to `Get-OrchProcessDetail` and emits a deprecation warning. Use `Get-OrchProcessDetail` directly. When `-ExportCsv` is specified the same detail enrichment runs automatically (and `-ExportCsv` is NOT deprecated, since the CSV row shape matches this cmdlet's output type).

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

### -ExportCsv

Exports processes to the specified CSV file path. The CSV includes comprehensive process information with headers: Path, Name, Id, Version, Description, EntryPoint, InputArguments, SpecificPriorityValue, HiddenForAttendedUser, RemoteControlAccess, RetentionAction, RetentionPeriod, RetentionBucket, StaleRetentionAction, StaleRetentionPeriod, StaleRetentionBucket, ErrorRecordingEnabled, Quality, Frequency, Duration, AutoStartProcess, AlwaysRunning, A4R_Enabled, A4R_HealingEnabled, VideoRecordingType, QueueItemVideoRecordingType, MaxDurationSeconds, and Tags. BucketId values are resolved to human-readable BucketName. Requires a filesystem path (not an Orch: drive path).

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

### -Name

Specifies the names of processes to retrieve. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests process names from the target folders.

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

You can pipe process names to this cmdlet via the Name property.

## OUTPUTS

### UiPath.PowerShell.Entities.Release

Returns Release objects with properties including Name, Description, ProcessVersion, IsLatestVersion, ProcessType, PackageId, and FolderPath. When -ExpandDetails is specified, additional properties are populated including EntryPointPath, RetentionAction, RetentionPeriod, RetentionBucketId, ProcessSettings, and VideoRecordingSettings.

## NOTES

Processes are folder-scoped entities. You must navigate to a folder on the Orch: drive or use -Path to specify target folders.

A process is a Release entity that links a published package (identified by PackageId/ProcessKey) to a folder. The same package can be linked to multiple folders as separate processes.

The -ExportCsv parameter automatically calls -ExpandDetails internally to populate all detail fields. BucketId values are resolved to human-readable BucketName in the exported CSV.

## RELATED LINKS

[Get-OrchProcessDetail](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchProcessDetail.md)

[Update-OrchProcess](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Update-OrchProcess.md)

[Remove-OrchProcess](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchProcess.md)
