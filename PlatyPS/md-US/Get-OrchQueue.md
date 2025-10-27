---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchQueue

## SYNOPSIS
Gets the queues.

## SYNTAX

```
Get-OrchQueue [[-Name] <String[]>] [-Path <String[]>] [-Recurse] [-Depth <UInt32>] [-ExportCsv <String>]
 [-CsvEncoding <Encoding>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Gets queue information from UiPath Orchestrator folders. Queues are data repositories that enable robots to exchange information and work items in an organized manner, supporting both attended and unattended automation scenarios.

This cmdlet returns queue information including definitions, retry policies, encryption settings, SLA configurations, retention policies, and organizational unit assignments. It operates on folder entities and supports recursive retrieval across folder hierarchies.

Multiple values for the -Name and -Path parameters can be specified using comma-separated text that includes wildcards. Additionally, you can use autocomplete for these values by pressing [Ctrl+Space] or [Tab].

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: GET /odata/QueueDefinitions

OAuth required scopes: OR.Queues or OR.Queues.Read

Required permissions: Queues.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Get-OrchQueue
```

Gets all queues in the current folder (Shared).

### Example 2
```powershell
PS Orch1:\Shared> Get-OrchQueue InvoiceQueue
```

Gets the specific queue named InvoiceQueue from the current folder using the positional parameter.

### Example 3
```powershell
PS C:\> Get-OrchQueue -Path Orch1:\Shared,Orch1:\Development -Name ProcessingQueue
```

Gets ProcessingQueue from both Shared and Development folders across specified paths.

### Example 4
```powershell
PS C:\> Get-OrchQueue -Path Orch1:\ -Recurse
```

Gets all queues from all folders in Orch1 tenant recursively.

## PARAMETERS

### -Depth
Specifies the depth of folder recursion. A depth of 0 targets only the current folder.

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

### -Name
Specifies the names of queues to retrieve. Supports wildcards and multiple values.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
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

### -Recurse
Includes the target folder and all its subfolders in the operation.

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

### -CsvEncoding
Specifies the encoding for CSV export. Default is UTF-8 with BOM for Excel compatibility.

```yaml
Type: Encoding
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: UTF8 with BOM
Accept pipeline input: False
Accept wildcard characters: False
```

### -ExportCsv
Exports results to CSV file with UTF-8 BOM encoding. Automatically converts internal IDs to human-readable names. Can be used with corresponding Import cmdlets.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.QueueDefinition
## NOTES
Queue entities are folder-scoped. You must navigate to a folder or use -Path, -Recurse, or -Depth parameters to specify target folders.

Queues support various configurations including retry policies, encryption, SLA settings, and retention policies for managing automation workflows and data processing.

The -ExportCsv parameter creates import-ready CSV files with human-readable names instead of internal IDs.



Primary Endpoint: GET /odata/QueueDefinitions
OAuth required scopes: OR.Queues or OR.Queues.Read
Required permissions: Queues.View

## RELATED LINKS

[New-OrchQueue](New-OrchQueue.md)

[Update-OrchQueue](Update-OrchQueue.md)

[Remove-OrchQueue](Remove-OrchQueue.md)

[Copy-OrchQueue](Copy-OrchQueue.md)

