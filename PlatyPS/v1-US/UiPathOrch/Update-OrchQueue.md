---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Update-OrchQueue
---

# Update-OrchQueue

## SYNOPSIS

Updates existing queue definitions in UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
Update-OrchQueue [-Name] <string[]> [-NewName <string>] [-Description <string>]
 [-AcceptAutomaticallyRetry <string>] [-RetryAbandonedItems <string>] [-MaxNumberOfRetries <int>]
 [-Release <string>] [-SlaInMinutes <int>] [-RiskSlaInMinutes <int>]
 [-SpecificDataJsonSchema <string>] [-OutputDataJsonSchema <string>]
 [-AnalyticsDataJsonSchema <string>] [-RetentionAction <string>] [-RetentionPeriod <int>]
 [-RetentionBucket <string>] [-StaleRetentionAction <string>] [-StaleRetentionPeriod <int>]
 [-StaleRetentionBucket <string>] [-Tags <string[]>] [-Path <string[]>] [-Recurse] [-Depth <uint>]
 [-WhatIf] [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Updates existing queue definitions in UiPath Orchestrator. Only the parameters that are explicitly specified are modified; all other properties are preserved from the current queue definition. The cmdlet deep copies the current queue definition before applying changes to ensure existing values are not inadvertently lost.

The -Name parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available queue names dynamically populated from the target folders. Multiple values can be specified using comma-separated text that includes wildcards.

The -Tags parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see existing queue tags that can be reused.

This cmdlet supports CSV import via the pipeline. The importable CSV format can be obtained using `Get-OrchQueue -Recurse -ExportCsv c:queues.csv`, then modified and piped back to Update-OrchQueue.

Current retention settings are retrieved before the update to preserve unmodified retention values (API v16+).

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: POST /odata/QueueDefinitions/UiPath.Server.Configuration.OData.EditQueue, GET /odata/QueueRetention({queueId}), GET /odata/Releases, GET /odata/Buckets

OAuth required scopes: OR.Queues, OR.Execution.Read, OR.Administration.Read

Required permissions: Queues.Edit, Queues.View, Processes.View, Buckets.View

## EXAMPLES

### Example 1: Update a queue description

```powershell
PS Orch1:\Shared> Update-OrchQueue TestQueue2 -Description "Updated test queue"
```

Updates the description of the queue named "TestQueue2" in the current folder. All other properties remain unchanged.

### Example 2: Rename a queue

```powershell
PS Orch1:\Shared> Update-OrchQueue TestQueue123 -NewName TestQueue456
```

Renames the queue from "TestQueue123" to "TestQueue456" in the current folder. All other queue properties are preserved.

### Example 3: Update retention settings

```powershell
PS Orch1:\Shared> Update-OrchQueue TestQueue2 -RetentionAction Archive -RetentionPeriod 90 -RetentionBucket TestBucket2
```

Updates the retention settings for "TestQueue2" in the Shared folder to archive completed items to "TestBucket2" after 90 days.

### Example 4: Preview updates with WhatIf

```powershell
PS Orch1:\Shared> Update-OrchQueue Test* -Description "Batch updated" -WhatIf
```

Displays what would happen if all queues matching "Test*" were updated, without actually performing the update. Useful for verifying wildcard matches before execution.

### Example 5: Update multiple queues by name

```powershell
PS Orch1:\Shared> Update-OrchQueue TestQueue2,TestQueue123 -EnforceUniqueReference true
```

Enables unique reference enforcement on two specified queues in the current folder. Multiple queue names can be specified as comma-separated values.

### Example 6: Import updates from CSV

```powershell
PS C:\> Import-Csv c:\queues.csv | Update-OrchQueue
```

Imports queue definitions from a CSV file and updates the corresponding queues. The CSV format matches the output of `Get-OrchQueue -ExportCsv`. The Path column in the CSV determines the target folder for each queue.

### Example 7: Update queues recursively

```powershell
PS Orch1:\> Update-OrchQueue -Recurse -Name TestQueue2 -AcceptAutomaticallyRetry true -MaxNumberOfRetries 5
```

Updates the queue named "TestQueue2" across all folders recursively, enabling automatic retry with a maximum of 5 retries.

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

### -AcceptAutomaticallyRetry

Specifies whether failed queue items are automatically retried. Tab completion suggests "true" and "false" values. When set to "true", items that fail during processing are automatically moved back to the New status for retry, up to the MaxNumberOfRetries limit.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -AnalyticsDataJsonSchema

Specifies the JSON schema for analytics-specific data fields on queue items. This schema defines the structure of custom analytics data that can be attached to items in this queue.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Description

Specifies a new description for the queue definition. This text appears in the Orchestrator UI and helps identify the purpose of the queue.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -MaxNumberOfRetries

Specifies the maximum number of times a failed queue item can be retried. This setting is effective only when AcceptAutomaticallyRetry is set to "true".

```yaml
Type: System.Nullable`1[System.Int32]
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

### -Name

Specifies the names of the queues to update. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests queue names from the target folders. This parameter is mandatory and positional (position 0).

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

### -NewName

Specifies a new name for the queue. Use this parameter to rename an existing queue definition. Only a single queue can be renamed at a time (do not use wildcards with -Name when renaming).

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -OutputDataJsonSchema

Specifies the JSON schema for output-specific data fields on queue items. This schema defines the structure of output data that automation workflows attach to processed items.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Release

Specifies the name of the process (Release) to associate with this queue. Supports wildcards. Tab completion dynamically suggests process names from the target folder.

```yaml
Type: System.String
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

### -RetentionAction

Specifies the action to take on completed queue items after the retention period expires. Tab completion suggests "Delete" and "Archive".

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -RetentionBucket

Specifies the name of the storage bucket for archived queue items. This parameter is relevant only when RetentionAction is set to "Archive". Supports wildcards. Tab completion dynamically suggests available bucket names from the target folder.

```yaml
Type: System.String
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

### -RetentionPeriod

Specifies the number of days to retain completed queue items before the retention action is applied.

```yaml
Type: System.Nullable`1[System.Int32]
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

### -RetryAbandonedItems

Specifies whether abandoned queue items (items left in InProgress status) are automatically retried. Tab completion suggests "true" and "false" values.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -RiskSlaInMinutes

Specifies the risk SLA threshold in minutes. When a queue item's processing time approaches this threshold, it is flagged as at risk of breaching the SLA. This value should be less than SlaInMinutes.

```yaml
Type: System.Nullable`1[System.Int32]
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

### -SlaInMinutes

Specifies the SLA (Service Level Agreement) threshold in minutes. Queue items that exceed this processing time are considered to have breached the SLA.

```yaml
Type: System.Nullable`1[System.Int32]
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

### -SpecificDataJsonSchema

Specifies the JSON schema for input-specific data fields on queue items. This schema defines the structure of custom data that can be attached to items when they are added to the queue.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -StaleRetentionAction

Specifies the action to take on stale queue items (items stuck in New or InProgress status) after the stale retention period expires. Tab completion suggests "Delete" and "Archive".

```yaml
Type: System.String
DefaultValue: ''
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

### -StaleRetentionBucket

Specifies the name of the storage bucket for archived stale queue items. This parameter is relevant only when StaleRetentionAction is set to "Archive". Supports wildcards. Tab completion dynamically suggests available bucket names from the target folder.

```yaml
Type: System.String
DefaultValue: ''
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

### -StaleRetentionPeriod

Specifies the number of days to retain stale queue items before the stale retention action is applied.

```yaml
Type: System.Nullable`1[System.Int32]
DefaultValue: ''
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

### -Tags

Specifies one or more tags to assign to the queue definition. Tags help categorize and filter queues in the Orchestrator UI. Tab completion suggests existing queue tags for reuse.

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

### -WhatIf

Shows what would happen if the cmdlet runs. The cmdlet is not run.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: False
SupportsWildcards: false
Aliases:
- wi
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

### -Confirm

Prompts you for confirmation before running the cmdlet.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: False
SupportsWildcards: false
Aliases:
- cf
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

You can pipe queue definition objects (such as CSV import rows) to this cmdlet. Properties are bound by name, including Path, Name, Description, AcceptAutomaticallyRetry, RetryAbandonedItems, MaxNumberOfRetries, Release, and all retention-related properties.

## OUTPUTS

### None

This cmdlet does not produce output. Queue definitions are updated in place on the Orchestrator server.

## NOTES

Queues are folder-scoped entities. You must navigate to a folder on the Orch: drive or use -Path to specify target folders.

Only explicitly specified parameters are modified. The cmdlet deep copies the current queue definition and retrieves current retention settings (API v16+) before applying changes, ensuring that unspecified properties retain their existing values.

This cmdlet supports CSV import via the pipeline. Export queues using `Get-OrchQueue -Recurse -ExportCsv c:queues.csv`, modify the CSV file, then import using `Import-Csv c:\queues.csv | Update-OrchQueue`. The Path column in the CSV determines the target folder for each queue.

## RELATED LINKS

Get-OrchQueue

New-OrchQueue

Remove-OrchQueue
