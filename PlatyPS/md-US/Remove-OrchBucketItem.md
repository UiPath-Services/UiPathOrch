---
external help file: UiPath.PowerShell.OrchProvider.dll-help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Remove-OrchBucketItem

## SYNOPSIS
Removes files and items from UiPath Orchestrator storage buckets.

## SYNTAX

```
Remove-OrchBucketItem [-Path <String[]>] [-Recurse] [-Depth <UInt32>] [[-Name] <String[]>] [[-FullPath] <String[]>] [-Confirm] [-WhatIf] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION

The Remove-OrchBucketItem cmdlet removes files and items stored within UiPath Orchestrator storage buckets. This cmdlet supports single file deletion, multiple file deletion, wildcard patterns, and recursive deletion operations.

Before removing items from buckets, you must navigate to the target folder using Set-Location (cd), or specify the target folders using the -Path parameter. If you attempt to run this cmdlet without being in a folder context, you will receive an error. Since deletion operations are irreversible, it is strongly recommended to use the -WhatIf parameter to preview operations before execution.

Storage buckets provide file storage capabilities for automation processes, allowing them to store, retrieve, and manage files during execution. This cmdlet is essential for maintaining clean storage environments and managing file lifecycles in automation workflows.

Primary Endpoint: DELETE /odata/Buckets({bucketId})/UiPath.Server.Configuration.OData.DeleteFile

OAuth required scopes: OR.Administration

Required permissions: Buckets.View and BlobFiles.Delete

## EXAMPLES

### Example 1: Basic file removal with wildcard patterns and preview
```powershell
PS Orch1:\Testing> Remove-OrchBucketItem TestBucket *.log,*.txt -WhatIf
```

Shows how to preview deletion operations before execution using wildcard patterns to target multiple file types.

### Example 2: Cross-folder removal with -Path parameter
```powershell
PS C:\> Remove-OrchBucketItem -Path Orch1:\Development,Orch1:\Shared *TempBucket *.txt
```

Demonstrates cross-folder operations using the -Path parameter to target buckets in multiple folders simultaneously.

### Example 3: Recursive removal with confirmation
```powershell
PS Orch1:\Projects> Remove-OrchBucketItem -Recurse -Name ProjectBucket -Confirm
```

Recursively removes all items from storage buckets named "ProjectBucket" in the current folder with confirmation prompts.

## PARAMETERS

### -Path <String[]>
Specifies the path(s) to the target folder(s) containing the buckets from which items will be removed. Use this parameter to remove bucket items from specific folders without changing your current location.

`yaml
Type: String[]
Parameter Sets: (All)
Aliases: 

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
`

### -Recurse [<SwitchParameter>]
Removes items from subdirectories recursively. Use this parameter when you need to clean up bucket contents with deep hierarchical structures.

`yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: 

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
`

### -Depth <UInt32>
Specifies the maximum depth for recursive removal operations. Only effective when -Recurse is used. This parameter helps prevent unintended deletion of deeply nested items.

`yaml
Type: UInt32
Parameter Sets: (All)
Aliases: 

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
`

### -Name <String[]>
Specifies the name(s) of the storage bucket(s) from which items will be removed. Supports wildcard patterns and can target multiple buckets simultaneously.

`yaml
Type: String[]
Parameter Sets: (All)
Aliases: 

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
`

### -FullPath <String[]>
Specifies the full path(s) of specific files within the bucket to be removed. Supports wildcard patterns for batch deletion of files matching specific criteria.

`yaml
Type: String[]
Parameter Sets: (All)
Aliases: 

Required: False
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
`

### -Confirm [<SwitchParameter>]
Prompts you for confirmation before running the cmdlet. Since deletion operations are irreversible, it is recommended to use this parameter when working with critical data.

`yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: cf

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
`

### -WhatIf [<SwitchParameter>]
Shows what would happen if the cmdlet runs without performing the actual deletion. Use this parameter to preview the impact of removal operations before execution.

`yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: wi

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
`

### -ProgressAction <ActionPreference>
Controls how progress information is displayed during command execution. Use 'SilentlyContinue' to suppress progress display when removing large numbers of items.

`yaml
Type: ActionPreference
Parameter Sets: (All)
Aliases: proga

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
`

## INPUTS

### System.String[]
You can pipe bucket names or file paths to this cmdlet.

### UiPath.PowerShell.Entities.BucketItem
You can pipe bucket item objects from Get-OrchBucketItem directly to this cmdlet.

## OUTPUTS

### None
This cmdlet does not generate output. Use Get-OrchBucketItem to verify deletion results.


## RELATED LINKS

[Get-OrchBucketItem](Get-OrchBucketItem.md)
[Import-OrchBucketItem](Import-OrchBucketItem.md)
[Export-OrchBucketItem](Export-OrchBucketItem.md)
[New-OrchBucket](New-OrchBucket.md)
[Remove-OrchBucket](Remove-OrchBucket.md)
[UiPath Orchestrator Documentation](https://docs.uipath.com/)
