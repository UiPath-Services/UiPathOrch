---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchBucketItem

## SYNOPSIS
Retrieves items (files and directories) stored within storage buckets.

## SYNTAX

```
Get-OrchBucketItem [[-Name] <String[]>] [[-FullPath] <String[]>] [-Path <String[]>] [-Recurse]
 [-Depth <UInt32>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
The Get-OrchBucketItem cmdlet retrieves items (files and directories) stored within storage buckets in UiPath Orchestrator. Storage buckets serve as external storage providers for automation processes, and this cmdlet provides visibility into the files and directory structure within these buckets.

Each bucket item contains properties such as FullPath (the file/directory path within the bucket), ContentType (MIME type for files), Size (in bytes for files), and IsDirectory (boolean indicating if the item is a directory).

Bucket items are folder entities that exist within specific folders. Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters.

This cmdlet operates as a folder entity operation, requiring navigation to the appropriate folder context or specification of target folders using the -Path parameter. The cmdlet automatically identifies available buckets in the current context and retrieves their contents.

Primary Endpoint: GET /odata/Buckets({bucketId})/UiPath.Server.Configuration.OData.GetFiles

OAuth required scopes: OR.Administration or OR.Administration.Read

Required permissions: Buckets.View and BlobFiles.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Get-OrchBucketItem
```

Retrieves all items from storage buckets in the current folder.

### Example 2
```powershell
PS Orch1:\> Get-OrchBucketItem -Recurse
```

Gets bucket items from all folders recursively.

### Example 3
```powershell
PS C:\> Get-OrchBucketItem -Path Orch1:\Production -FullPath *Report*
```

Gets bucket items with FullPath containing "Report" from the Production folder.

### Example 4
```powershell
PS Orch1:\> Get-OrchBucketItem -Recurse | Select-Object Path, Bucket, FullPath, Size
```

Gets all bucket items recursively and displays key properties with Path shown first.

### Example 5
```powershell
PS C:\> Get-OrchBucketItem -Path Orch1:\Production,Orch1:\Development -Recurse -Depth 2
```

Gets bucket items from Production and Development folders with maximum depth of 2 levels.

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

### -FullPath
Specifies the full paths of bucket items to be retrieved. Supports wildcard patterns for flexible item selection.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Name
Specifies the names of storage buckets whose items should be retrieved. Supports wildcard patterns for flexible bucket selection.

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

### -Recurse
Includes the target folder and all its subfolders in the search operation. Essential for comprehensive bucket item discovery.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.BlobFile
## NOTES
This cmdlet is a folder entity operation requiring navigation to a folder context or path specification using -Path parameter. The cmdlet retrieves items from all accessible storage buckets in the specified folder context. Output is grouped by bucket showing individual items. This operation requires both Buckets.View and BlobFiles.View permissions in the target folders.



Primary Endpoint: GET /odata/Buckets
OAuth required scopes: OR.Administration or OR.Administration.Read
Required permissions: Buckets.View

## RELATED LINKS

[Get-OrchBucket](Get-OrchBucket.md)



[Remove-OrchBucketItem](Remove-OrchBucketItem.md)
