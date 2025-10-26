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
PS Orch1:\Shared> Get-OrchQueue TestQueue123
```

Gets the specific queue named TestQueue123 from the current folder.

### Example 3
```powershell
PS Orch1:\Shared> Get-OrchQueue Test* | Select-Object Name, MaxNumberOfRetries, Encrypted
```

Gets all queues with names starting with Test and displays their retry settings and encryption status.

### Example 4
```powershell
PS C:\> Get-OrchQueue "Orch1:\Shared","Orch1:\Development" -Name ProcessingQueue
```

Gets ProcessingQueue from both Shared and Development folders across specified paths.

### Example 5
```powershell
PS Orch1:\> Get-OrchQueue | Where-Object {$_.Encrypted -eq $true}
```

Gets all encrypted queues from all subfolders recursively.

### Example 6
```powershell
PS Orch1:\Shared> Get-OrchQueue -ExportCsv queues.csv -CsvEncoding UTF8
```

Exports all queues from the current folder to a CSV file with UTF8 encoding for backup or migration.

### Example 7
```powershell
PS Orch1:\Shared> Get-OrchQueue | Where-Object {$_.MaxNumberOfRetries -gt 0} | Select-Object Name, MaxNumberOfRetries
```

Gets queues that have retry configurations and displays their retry settings.

### Example 8
```powershell
PS Orch1:\> Get-OrchQueue "Orch1:\*" -Recurse -Depth 2 | Format-Table
```

Gets queues from all folders within 2 levels depth and groups them by unique reference enforcement setting.

### Example 9
```powershell
PS Orch1:\Shared> Get-OrchQueue | ConvertTo-Json -Depth 2 | Out-File queues.json
```

Converts queue information to JSON format and saves to a file for integration or analysis.

### Example 10
```powershell
PS Orch1:\Shared> Get-OrchQueue | Where-Object {$_.SlaInMinutes} | Select-Object Name, SlaInMinutes, RiskSlaInMinutes
```

Gets queues that have SLA configurations and displays their SLA settings for performance monitoring.

OAuth required scopes: OR.Queues or OR.Queues.Read

Required permissions: Queues.View

### Example 1
```powershell
PS Orch1:\Shared> Get-OrchQueue
```

Gets queues from the current folder.

### Example 2
```powershell
PS Orch1:\> Get-OrchQueue -Recurse
```

Gets queues from all folders recursively.

### Example 3
```powershell
PS Orch1:\> Get-OrchQueue *Invoice*
```

Gets queues containing Invoice in their name from all folders.

### Example 4
```powershell
PS Orch1:\> Get-OrchQueue -Path Orch1:\Shared, Orch1:\Finance -Recurse
```

Gets queues from specific folders and their subfolders.

### Example 5
```powershell
PS Orch1:\> Get-OrchQueue | Where-Object {$_.Encrypted -eq $true}
```

Gets encrypted queues from all folders.

### Example 6
```powershell
PS Orch1:\> Get-OrchQueue | Where-Object {$_.MaxNumberOfRetries -gt 3}
```

Gets queues with more than 3 retry attempts configured.

### Example 7
```powershell
PS Orch1:\> Get-OrchQueue -ExportCsv C:\Reports\Queues.csv
```

Exports all queues to CSV with UTF-8 BOM encoding. The exported CSV can be imported using New-OrchQueue and Update-OrchQueue cmdlets.

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

