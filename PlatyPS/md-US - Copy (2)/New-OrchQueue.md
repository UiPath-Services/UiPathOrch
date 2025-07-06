---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# New-OrchQueue

## SYNOPSIS
Creates queues.

## SYNTAX

```
New-OrchQueue [-Name] <String[]> [-Description <String>] [-AcceptAutomaticallyRetry <String>]
 [-RetryAbandonedItems <String>] [-MaxNumberOfRetries <Int32>] [-EnforceUniqueReference <String>]
 [-Encrypted <String>] [-Release <String>] [-SlaInMinutes <Int32>] [-RiskSlaInMinutes <Int32>]
 [-SpecificDataJsonSchema <String>] [-OutputDataJsonSchema <String>] [-AnalyticsDataJsonSchema <String>]
 [-RetentionAction <String>] [-RetentionPeriod <Int32>] [-RetentionBucket <String>]
 [-StaleRetentionAction <String>] [-StaleRetentionPeriod <Int32>] [-StaleRetentionBucket <String>]
 [-Tags <String[]>] [-Path <String[]>] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## DESCRIPTION
Creates new queues in UiPath Orchestrator folders. Queues are data repositories that enable robots to exchange information and work items in an organized manner, supporting both attended and unattended automation scenarios.

This cmdlet supports CSV import functionality. The format of the importable CSV can be obtained using Get-OrchQueue -Recurse -ExportCsv. This allows for bulk queue creation and configuration.

Queue entities are folder-scoped. You must navigate to a folder or use -Path parameters to specify target folders.

Primary Endpoint: POST /odata/QueueDefinitions/UiPath.Server.Configuration.OData.CreateQueue, GET /odata/Releases, GET /odata/Buckets

OAuth required scopes: OR.Queues.Write

Required permissions: Queues.Create

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> New-OrchQueue InvoiceQueue -WhatIf
```

Shows what would happen when creating a queue without actually creating it.

### Example 2
```powershell
PS Orch1:\Shared> New-OrchQueue InvoiceQueue -Description "Queue for invoice processing"
```

Creates a basic queue with description.

### Example 3
```powershell
PS Orch1:\Shared> New-OrchQueue PaymentQueue -MaxNumberOfRetries 5 -Encrypted true
```

Creates an encrypted queue with custom retry settings.

### Example 4
```powershell
PS Orch1:\Shared> New-OrchQueue OrderQueue -SlaInMinutes 240 -RiskSlaInMinutes 480
```

Creates a queue with SLA configurations for standard and risk processing.

### Example 5
```powershell
PS Orch1:\> New-OrchQueue -Path Orch1:\Shared, Orch1:\Finance TestQueue
```

Creates queues in multiple folders.

### Example 6
```powershell
PS Orch1:\Shared> Import-Csv queues.csv | New-OrchQueue
```

Creates multiple queues from CSV file using pipeline input.

### Example 7
```powershell
PS Orch1:\Shared> New-OrchQueue CriticalQueue -EnforceUniqueReference true -Tags Priority, Urgent
```

Creates a queue with unique reference enforcement and tags.

## PARAMETERS

### -AcceptAutomaticallyRetry
Specifies the AcceptAutomaticallyRetry of the queues to be created.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -AnalyticsDataJsonSchema
Specifies the AnalyticsDataJsonSchema of the queues to be created.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Confirm
Prompts for confirmation before creating queues. Recommended when creating multiple queues.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: cf

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -Description
Specifies the description for the queue, explaining its purpose and usage.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Encrypted
Specifies whether queue data should be encrypted. Valid values: true, false.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -EnforceUniqueReference
Specifies whether queue items must have unique references. Valid values: true, false.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -MaxNumberOfRetries
Specifies the maximum number of retry attempts for failed queue items. Default is typically 1.

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Name
Specifies the names of queues to create. Supports multiple values for bulk creation.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -OutputDataJsonSchema
Specifies the OutputDataJsonSchema of the queues to be created.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Path
Specifies target folders. Use comma-separated values for multiple folders. Supports wildcards. If not specified, targets the current folder.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Release
Specifies the Release of the queues to be created.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -RetentionAction
Specifies the action to take when retention period expires. Valid values include: Delete, Move.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -RetentionBucket
Specifies the RetentionBucket of the queues to be created.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -RetentionPeriod
Specifies the retention period in days for completed queue items.

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -RetryAbandonedItems
Specifies the RetryAbandonedItems of the queues to be created.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -RiskSlaInMinutes
Specifies the Service Level Agreement time in minutes for risk processing scenarios.

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -SlaInMinutes
Specifies the Service Level Agreement time in minutes for standard processing.

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -SpecificDataJsonSchema
Specifies the SpecificDataJsonSchema of the queues to be created.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Tags
Specifies tags for categorizing and organizing the queue.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -WhatIf
Shows what would happen if the cmdlet runs without actually creating queues. Recommended for safety verification.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: wi

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -ProgressAction
Controls how progress information is displayed during cmdlet execution.

```yaml
Type: ActionPreference
Parameter Sets: (All)
Aliases: proga

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -StaleRetentionAction
Specifies the action to take for stale queue items when retention period expires.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -StaleRetentionBucket
{{ Fill StaleRetentionBucket Description }}

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -StaleRetentionPeriod
Specifies the retention period in days for stale queue items.

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]
Queue names can be piped to this cmdlet.

### UiPath.PowerShell.Entities.QueueDefinition
Queue objects can be piped to this cmdlet. Properties will be automatically mapped to parameters via ByPropertyName binding, enabling CSV import scenarios.

## OUTPUTS

### UiPath.PowerShell.Entities.QueueDefinition
Returns queue objects for the created queues with metadata including Name, Description, MaxNumberOfRetries, and other properties.

## NOTES
Queue entities are folder-scoped. You must navigate to a folder or use -Path parameters to specify target folders.

This cmdlet supports CSV import functionality using Import-Csv | New-OrchQueue for bulk queue creation. Use Get-OrchQueue -ExportCsv to generate the correct CSV format.

Use -WhatIf to preview queue creation before actual execution, especially when creating multiple queues.

Queue configuration includes retry policies, encryption settings, SLA configurations, and retention policies for managing automation workflows.

## RELATED LINKS

[Get-OrchQueue](Get-OrchQueue.md)

[Update-OrchQueue](Update-OrchQueue.md)

[Remove-OrchQueue](Remove-OrchQueue.md)

[Copy-OrchQueue](Copy-OrchQueue.md)


