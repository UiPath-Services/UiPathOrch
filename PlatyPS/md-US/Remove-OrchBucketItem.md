---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Remove-OrchBucketItem

## SYNOPSIS
Removes files and items from UiPath Orchestrator storage buckets.

## SYNTAX

```
Remove-OrchBucketItem [-Name <String[]>] [-FullPath <String[]>] [-Path <String[]>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
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

### -Depth
{{ Fill Depth Description }}

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
{{ Fill FullPath Description }}

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

### -Name
{{ Fill Name Description }}

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

### -Path
{{ Fill Path Description }}

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

### -Recurse
{{ Fill Recurse Description }}

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

### -WhatIf
Shows what would happen if the cmdlet runs. The cmdlet is not run.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]
You can pipe bucket names or file paths to this cmdlet.

### UiPath.PowerShell.Entities.BucketItem
You can pipe bucket item objects from Get-OrchBucketItem directly to this cmdlet.

## OUTPUTS

### None
This cmdlet does not generate output. Use Get-OrchBucketItem to verify deletion results.

## NOTES

## RELATED LINKS

[Get-OrchBucketItem](Get-OrchBucketItem.md)
[Import-OrchBucketItem](Import-OrchBucketItem.md)
[Export-OrchBucketItem](Export-OrchBucketItem.md)
[New-OrchBucket](New-OrchBucket.md)
[Remove-OrchBucket](Remove-OrchBucket.md)
[UiPath Orchestrator Documentation](https://docs.uipath.com/)
