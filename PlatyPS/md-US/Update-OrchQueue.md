---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Update-OrchQueue

## SYNOPSIS
Updates queues.

## SYNTAX

```
Update-OrchQueue [-Name] <String[]> [-NewName <String>] [-Description <String>]
 [-AcceptAutomaticallyRetry <String>] [-RetryAbandonedItems <String>] [-MaxNumberOfRetries <Int32>]
 [-Release <String>] [-SlaInMinutes <Int32>] [-RiskSlaInMinutes <Int32>] [-SpecificDataJsonSchema <String>]
 [-OutputDataJsonSchema <String>] [-AnalyticsDataJsonSchema <String>] [-RetentionAction <String>]
 [-RetentionPeriod <Int32>] [-RetentionBucket <String>] [-StaleRetentionAction <String>]
 [-StaleRetentionPeriod <Int32>] [-StaleRetentionBucket <String>] [-Tags <String[]>] [-Path <String[]>]
 [-Recurse] [-Depth <UInt32>] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The Update-OrchQueue cmdlet modifies the properties of existing queues in UiPath Orchestrator. You can update various queue configurations including descriptions, retry settings, SLA times, data schemas, retention policies, and process assignments.

This is a folder entity cmdlet. Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters. The -Recurse parameter enables updating queues from all subfolders.

The cmdlet allows updating critical queue configurations such as automatic retry settings, maximum retry counts, SLA minutes for performance monitoring, and retention policies for queue item cleanup. You can also update JSON schemas for specific data, output data, and analytics data validation.

Queue updates support CSV import functionality. Use Get-OrchQueue -Recurse -ExportCsv to obtain the proper format for bulk queue updates, enabling efficient management of multiple queues across different folders.

Use the -NewName parameter to rename queues while maintaining their configuration and historical data. The -Tags parameter allows updating queue categorization for better organization and filtering.

Primary Endpoint: POST /odata/QueueDefinitions/UiPath.Server.Configuration.OData.EditQueue, GET /odata/QueueRetention({queueId}), GET /odata/Releases, GET /odata/Buckets

OAuth required scopes: OR.Queues or OR.Queues.Write

Required permissions: Queues.Edit

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Update-OrchQueue TestQueue -Description "Updated queue description" -WhatIf
```

Tests updating a queue's description using -WhatIf to preview the operation before execution.

### Example 2
```powershell
PS Orch1:\Shared> Update-OrchQueue TestQueue -SlaInMinutes 60 -RiskSlaInMinutes 30
```

Updates the SLA settings for a queue with 60 minutes for normal SLA and 30 minutes for risk SLA.

### Example 3
```powershell
PS Orch1:\Shared> Update-OrchQueue TestQueue -AcceptAutomaticallyRetry True -MaxNumberOfRetries 3 -RetryAbandonedItems False
```

Configures retry settings for a queue to automatically retry failed items up to 3 times, but not retry abandoned items.

### Example 4
```powershell
PS Orch1:\Shared> Update-OrchQueue TestQueue -NewName ProductionQueue -Tags Production,Critical
```

Renames a queue and updates its tags for better organization and filtering.

### Example 5
```powershell
PS Orch1:\> Update-OrchQueue -Path Orch1:\Shared,Orch1:\Development -Recurse *Test* -Description "Updated test queues"
```

Updates all queues with Test in their name across multiple folders recursively. The correct parameter order is -Path, -Recurse, -Depth, then the positional Name parameter.

### Example 6
```powershell
PS Orch1:\Shared> Update-OrchQueue TestQueue -RetentionAction DeleteItems -RetentionPeriod 30 -StaleRetentionAction MoveToBucket -StaleRetentionPeriod 7 -StaleRetentionBucket ArchiveBucket
```

Configures comprehensive retention policies for queue items including automatic deletion after 30 days and archiving stale items.

### Example 7
```powershell
PS C:\> Import-Csv queues.csv | Update-OrchQueue -WhatIf
```

Tests bulk queue updates from a CSV file. The CSV should contain columns like Name, Description, SlaInMinutes, MaxNumberOfRetries.

### Example 8
```powershell
PS Orch1:\Shared> Update-OrchQueue TestQueue -SpecificDataJsonSchema '{"type":"object","properties":{"Priority":{"type":"string"}}}' -OutputDataJsonSchema '{"type":"object","properties":{"Result":{"type":"string"}}}'
```

Updates the JSON schemas for queue item specific data and output data validation.

## PARAMETERS

### -AcceptAutomaticallyRetry
Specifies the AcceptAutomaticallyRetry of the queues to be updated.

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
Specifies the AnalyticsDataJsonSchema of the queues to be updated.

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
Prompts you for confirmation before running the cmdlet.

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

### -Depth
Specifies the depth for recursion into the target folders. A depth of 0 indicates the current location only, with no subfolders included.

```yaml
Type: UInt32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Description
Specifies the Description of the queues to be updated.

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
Specifies the MaxNumberOfRetries of the queues to be updated.

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
Specifies the Name of the queues to be updated.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -OutputDataJsonSchema
Specifies the OutputDataJsonSchema of the queues to be updated.

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
Specifies the target folder. If not specified, the current folder will be targeted.

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

### -Recurse
Specifies that the operation should include the target folder and all its subfolders.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -Release
Specifies the Release of the queues to be updated.

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
Specifies the RetentionAction of the queues to be updated.

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
Specifies the RetentionBucket of the queues to be updated.

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
Specifies the RetentionPeriod of the queues to be updated.

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
Specifies the RetryAbandonedItems of the queues to be updated.

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
Specifies the RiskSlaInMinutes of the queues to be updated.

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
Specifies the SlaInMinutes of the queues to be updated.

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
Specifies the SpecificDataJsonSchema of the queues to be updated.

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
Specifies the Tags of the queues to be updated.

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
Shows what would happen if the cmdlet runs. The cmdlet is not run.

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
Specifies the action preference for how PowerShell should handle progress information.

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

### -NewName
Specifies the new name for the queue.

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

### -StaleRetentionAction
Specifies the action to take when queue items become stale.

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
Specifies the target bucket for stale queue items when using MoveToBucket action.

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
Specifies the number of days after which queue items are considered stale.

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
### System.String
### System.Nullable`1[[System.Int32, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
## OUTPUTS

### UiPath.PowerShell.Entities.QueueDefinition
## NOTES

## RELATED LINKS
