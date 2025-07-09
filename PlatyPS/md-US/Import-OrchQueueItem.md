---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Import-OrchQueueItem

## SYNOPSIS
Imports queue items from CSV files to specified queues.

## SYNTAX

```
Import-OrchQueueItem [-Name] <String[]> -ImportCsv <String[]> [[-CsvEncoding] <Encoding>]
 [[-CommitType] <String>] [-Path <String[]>] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## DESCRIPTION
The Import-OrchQueueItem cmdlet imports queue items from CSV files into UiPath Orchestrator queues within specified folders. This cmdlet enables bulk loading of work items from external data sources, making it essential for migrating data, setting up test environments, or loading work items from external systems.

Queue items represent units of work that robots process sequentially. Importing queue items from CSV files allows for efficient bulk data loading, migration from other systems, or preparation of test data for automation scenarios.

Use the -Name parameter to specify which queues should receive the imported items. The -ImportCsv parameter specifies the CSV files containing the queue item data. The cmdlet supports various CSV encodings and commit strategies for different import scenarios.

This cmdlet operates on folders. Use -Path parameter to specify target folders where the queues are located. The CSV files must contain properly formatted queue item data with appropriate column headers.

Primary Endpoint: [PLACEHOLDER - 具体的なAPIエンドポイント]

OAuth required scopes: OR.Queues or OR.Queues.Write

Required permissions: Queues.Edit

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Development> Import-OrchQueueItem WorkQueue -ImportCsv "C:\Data\workitems.csv"
```

Imports queue items from workitems.csv file into the WorkQueue in the current folder (Development).

### Example 2
```powershell
PS C:\> Import-OrchQueueItem -Path Orch1:\Production -Name ProcessingQueue -ImportCsv "C:\Exports\items.csv" -CsvEncoding UTF8
```

Imports queue items from items.csv with UTF8 encoding into ProcessingQueue in the Production folder.

### Example 3
```powershell
PS Orch1:\Development> Import-OrchQueueItem DataQueue, BulkQueue -ImportCsv "C:\Data\bulk_items.csv" -WhatIf
```

Shows what would happen when importing queue items from bulk_items.csv into DataQueue and BulkQueue in the current folder.

### Example 4
```powershell
PS C:\> Import-OrchQueueItem -Path Orch1:\Development -Name ImportQueue -ImportCsv "C:\Migration\*.csv" -CommitType Immediate
```

Imports queue items from all CSV files in C:\Migration directory into ImportQueue using immediate commit strategy.

### Example 5
```powershell
PS Orch1:\Production> Import-OrchQueueItem TestQueue -ImportCsv "C:\TestData\scenario1.csv", "C:\TestData\scenario2.csv" -Confirm
```

Imports queue items from multiple CSV files into TestQueue with confirmation prompts.

### Example 6
```powershell
PS C:\> Import-OrchQueueItem -Path Orch1:\Development -Name *ProcessQueue -ImportCsv "C:\Data\items.csv" -CsvEncoding Unicode
```

Imports queue items from items.csv with Unicode encoding into all queues with names ending in ProcessQueue.

## PARAMETERS

### -CommitType
Specifies the commit strategy for the import operation. Valid values include Immediate, Batch, or Transaction.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 3
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -CsvEncoding
Specifies the encoding of the CSV files. If not specified, UTF8 encoding will be used.

```yaml
Type: Encoding
Parameter Sets: (All)
Aliases:

Required: False
Position: 2
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Name
Specifies the names of the queues where items should be imported.

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

### -Path
Specifies the target folders containing the queues. If not specified, the current folder will be targeted.

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

### -ProgressAction
{{ Fill ProgressAction Description }}

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

### -Confirm
Prompts you for confirmation before running the cmdlet.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: cf

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -WhatIf
Shows what would happen if the cmdlet runs.
The cmdlet is not run.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: wi

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ImportCsv
Specifies the CSV files containing queue item data to be imported.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.FailedQueueItem
## NOTES
CSV files must contain properly formatted queue item data with appropriate column headers. The columns should match the queue item properties and specific data expected by the target queue. Ensure CSV files are properly encoded to avoid data corruption during import.

Import operations can be time-consuming for large CSV files. Use appropriate commit strategies based on data volume and transaction requirements. Test imports with -WhatIf before executing large data imports.

## RELATED LINKS

[Get-OrchQueue](Get-OrchQueue.md)

[Get-OrchQueueItem](Get-OrchQueueItem.md)

[Add-OrchQueueItem](Add-OrchQueueItem.md)

[Remove-OrchQueueItem](Remove-OrchQueueItem.md)
