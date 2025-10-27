---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchBucket

## SYNOPSIS
Retrieves storage buckets configured in UiPath Orchestrator folders.

## SYNTAX

```
Get-OrchBucket [[-Name] <String[]>] [-Path <String[]>] [-Recurse] [-Depth <UInt32>] [-ExportCsv <String>]
 [-CsvEncoding <Encoding>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
The Get-OrchBucket cmdlet retrieves storage buckets configured within UiPath Orchestrator folders. Storage buckets provide external storage capabilities for automation processes, enabling secure storage and retrieval of files, documents, and other artifacts used by automation workflows.

Storage buckets serve as external storage providers integrated with Orchestrator, supporting various cloud storage services and file systems. Each bucket contains properties such as Name, Description, Identifier (GUID), StorageProvider, StorageContainer, Options, and FoldersCount.

This cmdlet operates as a folder entity operation, requiring navigation to the appropriate folder context or specification of target folders using the -Path parameter. Use the -Recurse parameter to include buckets in subfolders, and -Depth to control recursion levels.

Primary Endpoint: GET /odata/Buckets

OAuth required scopes: OR.Administration or OR.Administration.Read

Required permissions: Buckets.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Get-OrchBucket
```

Retrieves all storage buckets in the current folder, demonstrating basic usage.

### Example 2
```powershell
PS C:\> Get-OrchBucket -Path Orch1:\Shared *Backup*
```

Gets storage buckets with names containing "Backup" in the Shared folder, demonstrating -Path parameter with wildcard filtering.

### Example 3
```powershell
PS Orch1:\> Get-OrchBucket -Recurse *Prod*
```

Gets storage buckets with names containing "Prod" across all folders recursively.

### Example 4
```powershell
PS Orch1:\> Get-OrchBucket -Recurse | Where-Object FoldersCount -gt 0
```

Retrieves storage buckets that are used in at least one folder, using pipeline processing to filter results.

## PARAMETERS

### -Depth
Specifies the depth for recursion into target folders. A depth of 0 indicates the current location only. Higher values include more subfolder levels.

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
Specifies the names of storage buckets to be retrieved. Supports wildcard patterns for flexible bucket selection.

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
Specifies the target folders to search. If not specified, the current folder context will be used. For folder entity operations requiring path specification.

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
Controls how progress information is displayed during command execution. Use 'SilentlyContinue' to suppress progress display.

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
Includes the target folder and all its subfolders in the search operation. Essential for comprehensive storage bucket discovery.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -CsvEncoding
Specifies the character encoding for CSV export. Supports various encoding formats for international compatibility.

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
Specifies the file path for exporting storage bucket information to CSV format. Includes configuration details for documentation and analysis.

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

### UiPath.PowerShell.Entities.Bucket
## NOTES
This cmdlet is a folder entity operation requiring navigation to a folder context or path specification using -Path parameter. Storage buckets provide external storage integration for automation processes. Use -Recurse and -Depth parameters to control the scope of bucket discovery across folder hierarchies. This operation requires Buckets.View permissions in the target folders.



Primary Endpoint: GET /odata/Buckets
OAuth required scopes: OR.Administration or OR.Administration.Read
Required permissions: Buckets.View

## RELATED LINKS

[Get-OrchBucketItem](Get-OrchBucketItem.md)



[Remove-OrchBucket](Remove-OrchBucket.md)

