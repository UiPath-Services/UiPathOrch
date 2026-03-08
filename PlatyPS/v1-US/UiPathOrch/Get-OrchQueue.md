---
document type: cmdlet
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchQueue
---

# Get-OrchQueue

## SYNOPSIS

Gets queue definitions from UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
Get-OrchQueue [[-Name] <string[]>] [-Path <string[]>] [-Recurse] [-Depth <uint>]
 [-ExportCsv <string>] [-CsvEncoding <Encoding>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets queue definition information from UiPath Orchestrator folders. A queue definition describes the configuration of a queue including its name, retry settings, encryption, SLA thresholds, retention policies, and associated process (Release). Queue items are added to and processed from these queue definitions by automation workflows.

The -Name parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available queue names dynamically populated from the target folders. Multiple values can be specified using comma-separated text that includes wildcards.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Folder processing is multi-threaded for improved performance when targeting multiple folders with -Recurse.

Primary Endpoint: GET /odata/QueueDefinitions

OAuth required scopes: OR.Queues or OR.Queues.Read

Required permissions: Queues.View

## EXAMPLES

### Example 1: Get all queues in the current folder

```powershell
PS Orch1:\Shared> Get-OrchQueue
```

Gets all queue definitions from the current folder and returns their properties including Name, Description, MaxNumberOfRetries, AcceptAutomaticallyRetry, and EnforceUniqueReference.

### Example 2: Get a queue by name

```powershell
PS Orch1:\Shared> Get-OrchQueue TestQueue2
```

Gets the queue definition named "TestQueue2" from the current folder. The -Name parameter is positional (position 0) so the parameter name can be omitted.

### Example 3: Get queues by name with wildcards

```powershell
PS Orch1:\Shared> Get-OrchQueue Test*
```

Gets all queue definitions whose name starts with "Test" from the current folder. The -Name parameter supports wildcards.

### Example 4: Get queues from a specific folder

```powershell
PS C:\> Get-OrchQueue -Path Orch1:\Production TestQueue2
```

Gets the queue definition named "TestQueue2" from the Production folder. When -Path uses an absolute path (Orch1:\...), the command can be run from any location.

### Example 5: Get queues recursively from all folders

```powershell
PS Orch1:\> Get-OrchQueue -Recurse | Format-Table Name, Description, MaxNumberOfRetries
```

Gets all queue definitions from all folders recursively and displays them in a table format with selected properties.

### Example 6: Export queues to CSV

```powershell
PS Orch1:\Shared> Get-OrchQueue -ExportCsv c:queues.csv
```

Exports all queue definitions in the current folder to a CSV file. The CSV includes columns such as Path, Name, Description, AcceptAutomaticallyRetry, RetryAbandonedItems, MaxNumberOfRetries, EnforceUniqueReference, Encrypted, Release, SlaInMinutes, RiskSlaInMinutes, SpecificDataJsonSchema, OutputDataJsonSchema, AnalyticsDataJsonSchema, RetentionAction, RetentionPeriod, RetentionBucket, StaleRetentionAction, StaleRetentionPeriod, StaleRetentionBucket, and Tags. BucketId values are resolved to human-readable BucketName and ReleaseId is resolved to the Release name. When the current location is an Orch: drive, prefix the filename with c: to write to the filesystem.

### Example 7: Export queues recursively

```powershell
PS Orch1:\> Get-OrchQueue -Recurse -ExportCsv c:all-queues.csv
```

Exports queue definitions from all folders recursively to a CSV file.

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

### -ExportCsv

Exports queue definitions to the specified CSV file path. The CSV includes comprehensive queue information with headers: Path, Name, Description, AcceptAutomaticallyRetry, RetryAbandonedItems, MaxNumberOfRetries, EnforceUniqueReference, Encrypted, Release, SlaInMinutes, RiskSlaInMinutes, SpecificDataJsonSchema, OutputDataJsonSchema, AnalyticsDataJsonSchema, RetentionAction, RetentionPeriod, RetentionBucket, StaleRetentionAction, StaleRetentionPeriod, StaleRetentionBucket, and Tags. BucketId values are resolved to human-readable BucketName. ReleaseId is resolved to the Release name. Requires a filesystem path (not an Orch: drive path).

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

Specifies the names of queues to retrieve. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests queue names from the target folders.

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

You can pipe queue names to this cmdlet via the Name property.

## OUTPUTS

### UiPath.PowerShell.Entities.QueueDefinition

Returns QueueDefinition objects with properties including Name, Description, MaxNumberOfRetries, AcceptAutomaticallyRetry, RetryAbandonedItems, EnforceUniqueReference, Encrypted, SlaInMinutes, RiskSlaInMinutes, and FolderPath.

## NOTES

Queues are folder-scoped entities. You must navigate to a folder on the Orch: drive or use -Path to specify target folders.

The -ExportCsv parameter resolves BucketId values to human-readable BucketName and ReleaseId to the Release name in the exported CSV. The exported CSV format is compatible with Update-OrchQueue for bulk import operations.

## RELATED LINKS

New-OrchQueue

Update-OrchQueue

Remove-OrchQueue

Copy-OrchQueue

Get-OrchQueueItem
