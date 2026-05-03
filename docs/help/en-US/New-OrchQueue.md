---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/New-OrchQueue.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: New-OrchQueue
---

# New-OrchQueue

## SYNOPSIS

Creates a new queue definition in UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
New-OrchQueue [-Name] <string[]> [-Description <string>] [-AcceptAutomaticallyRetry <string>]
 [-RetryAbandonedItems <string>] [-MaxNumberOfRetries <int>] [-EnforceUniqueReference <string>]
 [-Encrypted <string>] [-Release <string>] [-SlaInMinutes <int>] [-RiskSlaInMinutes <int>]
 [-SpecificDataJsonSchema <string>] [-OutputDataJsonSchema <string>]
 [-AnalyticsDataJsonSchema <string>] [-RetentionAction <string>] [-RetentionPeriod <int>]
 [-RetentionBucket <string>] [-StaleRetentionAction <string>] [-StaleRetentionPeriod <int>]
 [-StaleRetentionBucket <string>] [-Tags <string[]>] [-Path <string[]>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Creates a new queue definition in UiPath Orchestrator. A queue definition configures how queue items are processed, including retry behavior, encryption, unique reference enforcement, SLA monitoring, and retention policies.

The -Name parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see auto-generated unique queue names that avoid conflicts with existing queues in the target folder.

When -RetentionAction and -RetentionPeriod are not specified, the cmdlet defaults to a RetentionAction of "Delete" and a RetentionPeriod of 30 days (API v16+).

When specifying the -Path parameter, place it immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: POST /odata/QueueDefinitions

OAuth required scopes: OR.Queues

Required permissions: Queues.Create

## EXAMPLES

### Example 1: Create a basic queue

```powershell
PS Orch1:\Shared> New-OrchQueue NewQueue1
```

Creates a new queue definition named "NewQueue1" in the current folder with default settings. RetentionAction defaults to "Delete" and RetentionPeriod defaults to 30 days.

### Example 2: Create a queue with description and retry settings

```powershell
PS Orch1:\Shared> New-OrchQueue NewQueue1 -Description "Processes incoming orders" -AcceptAutomaticallyRetry true -MaxNumberOfRetries 3
```

Creates a new queue named "NewQueue1" with a description, automatic retry enabled, and a maximum of 3 retries for failed items.

### Example 3: Create a queue with encryption and unique reference enforcement

```powershell
PS Orch1:\Shared> New-OrchQueue NewQueue1 -EnforceUniqueReference true -Encrypted true -Description "Encrypted processing queue"
```

Creates a new queue named "NewQueue1" in the Shared folder with encryption enabled and unique reference enforcement to prevent duplicate queue items.

### Example 4: Create a queue with retention settings

```powershell
PS Orch1:\Shared> New-OrchQueue NewQueue1 -RetentionAction Archive -RetentionPeriod 90 -RetentionBucket TestBucket2 -StaleRetentionAction Delete -StaleRetentionPeriod 180
```

Creates a new queue named "NewQueue1" with an archive retention action that moves completed items to "TestBucket2" after 90 days, and deletes stale items after 180 days.

### Example 5: Preview queue creation with WhatIf

```powershell
PS C:\> New-OrchQueue -Path Orch1:\Shared -Name NewQueue1 -Description "New processing queue" -WhatIf
```

Displays what would happen if the queue were created without actually creating it. Useful for verifying parameters before execution.

## PARAMETERS

### -Path

Specifies the target folders where the queue will be created. If not specified, the current folder is targeted. Supports wildcards and comma-separated values for multiple folders.

```yaml
Type: System.String[]
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

### -AcceptAutomaticallyRetry

Specifies whether failed queue items are automatically retried. Tab completion suggests "true" and "false" values. When set to "true", items that fail during processing are automatically moved back to the New status for retry, up to the MaxNumberOfRetries limit.

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

### -AnalyticsDataJsonSchema

Specifies the JSON schema for analytics-specific data fields on queue items. This schema defines the structure of custom analytics data that can be attached to items in this queue.

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

### -Description

Specifies a description for the queue definition. This text appears in the Orchestrator UI and helps identify the purpose of the queue.

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

### -Encrypted

Specifies whether queue item data is encrypted at rest. Tab completion suggests "true" and "false" values. Once a queue is created with encryption enabled, this setting cannot be changed.

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

### -EnforceUniqueReference

Specifies whether queue items must have unique reference values. Tab completion suggests "true" and "false" values. When enabled, attempting to add a queue item with a duplicate reference will be rejected.

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

### -MaxNumberOfRetries

Specifies the maximum number of times a failed queue item can be retried. This setting is effective only when AcceptAutomaticallyRetry is set to "true".

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

### -Name

Specifies the name of the queue to create. This parameter is mandatory and positional (position 0). Tab completion suggests auto-generated unique names that avoid conflicts with existing queues in the target folder.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: false
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

### -OutputDataJsonSchema

Specifies the JSON schema for output-specific data fields on queue items. This schema defines the structure of output data that automation workflows attach to processed items.

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

### -Release

Specifies the name of the process (Release) to associate with this queue. Supports wildcards. Tab completion dynamically suggests process names from the target folder.

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

### -RetentionAction

Specifies the action to take on completed queue items after the retention period expires. Tab completion suggests "Delete" and "Archive". When not specified, defaults to "Delete" (API v16+).

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

### -RetentionBucket

Specifies the name of the storage bucket for archived queue items. This parameter is relevant only when RetentionAction is set to "Archive". Supports wildcards. Tab completion dynamically suggests available bucket names from the target folder.

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

### -RetentionPeriod

Specifies the number of days to retain completed queue items before the retention action is applied. When not specified, defaults to 30 days (API v16+).

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

### -RetryAbandonedItems

Specifies whether abandoned queue items (items left in InProgress status) are automatically retried. Tab completion suggests "true" and "false" values.

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

### -RiskSlaInMinutes

Specifies the risk SLA threshold in minutes. When a queue item's processing time approaches this threshold, it is flagged as at risk of breaching the SLA. This value should be less than SlaInMinutes.

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

### -SlaInMinutes

Specifies the SLA (Service Level Agreement) threshold in minutes. Queue items that exceed this processing time are considered to have breached the SLA.

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

### -SpecificDataJsonSchema

Specifies the JSON schema for input-specific data fields on queue items. This schema defines the structure of custom data that can be attached to items when they are added to the queue.

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

Specifies one or more tags to assign to the queue definition. Tags help categorize and filter queues in the Orchestrator UI.

```yaml
Type: System.String[]
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

You can pipe queue names to this cmdlet via the Name property.

## OUTPUTS

### UiPath.PowerShell.Entities.QueueDefinition

Returns the created QueueDefinition object with properties including Name, Description, MaxNumberOfRetries, AcceptAutomaticallyRetry, EnforceUniqueReference, Encrypted, SlaInMinutes, RiskSlaInMinutes, and FolderPath.

## NOTES

Queues are folder-scoped entities. You must navigate to a folder on the Orch: drive or use -Path to specify the target folder.

When -RetentionAction and -RetentionPeriod are not specified, the cmdlet applies default values of "Delete" and 30 days respectively (API v16+).

The -Name completer auto-generates unique queue names that avoid conflicts with existing queues in the target folder.

## RELATED LINKS

Get-OrchQueue

Update-OrchQueue

Remove-OrchQueue
