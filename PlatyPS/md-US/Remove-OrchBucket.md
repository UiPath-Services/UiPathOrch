---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Remove-OrchBucket

## SYNOPSIS
Removes storage buckets.

## SYNTAX

```
Remove-OrchBucket [-Name] <String[]> [-Path <String[]>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The Remove-OrchBucket cmdlet removes storage buckets from UiPath Orchestrator. This operation permanently deletes the bucket and any contained folders or files, so use with caution.

**This is a folder entity cmdlet.** To use this cmdlet, you must first navigate to the target folder using Set-Location (cd), or specify the target folders using the -Path, -Recurse, or -Depth parameters. If you attempt to run this cmdlet without being in a folder context, you will receive the error: \"Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters.\"

**Warning**: This operation is destructive and cannot be undone. All data stored in the bucket will be permanently deleted. Consider using -WhatIf to preview the operation before execution, and -Confirm for additional safety.

Primary Endpoint: DELETE /odata/Buckets({id})
OAuth required scopes: OR.Administration or OR.Administration.Write
Required permissions: Buckets.Delete

## EXAMPLES

### Example 1
```powershell
Remove-OrchBucket TestBucket -WhatIf
```

Shows what would happen when removing the \"TestBucket\" bucket without actually deleting it.

### Example 2
```powershell
Remove-OrchBucket OldBucket -Confirm
```

Removes the \"OldBucket\" bucket with confirmation prompt for safety.

### Example 3
```powershell
Remove-OrchBucket Temp* -WhatIf
```

Shows what would happen when removing all buckets starting with \"Temp\" using wildcard pattern.

### Example 4
```powershell
Get-OrchBucket | Where-Object {$_.Description -like \"*obsolete*\"} | Remove-OrchBucket -WhatIf
```

Shows what would happen when removing buckets that have \"obsolete\" in their description using pipeline processing.

### Example 5
```powershell
Remove-OrchBucket -Path Orch1:\Development Archive*, Backup* -Confirm
```

Removes buckets matching \"Archive*\" and \"Backup*\" patterns in the Development folder with confirmation.

### Example 6
```powershell
Remove-OrchBucket -Recurse TempData -WhatIf
```

Shows what would happen when removing \"TempData\" buckets recursively across all subfolders.

## PARAMETERS

### -Depth
Specifies the maximum depth of recursion when used with -Recurse parameter.

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
Specifies the name(s) of the bucket(s) to remove. Supports wildcard patterns for bulk operations.

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
Specifies the path(s) to the target folder(s) containing the buckets to remove. Use this parameter to remove buckets from specific folders without changing your current location.

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
Indicates that the cmdlet should process all subfolders recursively when used with -Path.

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

### -Confirm
Prompts you for confirmation before running the cmdlet. Recommended for production environments.

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
Shows what would happen if the cmdlet runs. The cmdlet is not run. This is highly recommended for destructive operations.

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

### None
## OUTPUTS

### System.Object
## NOTES
- **DESTRUCTIVE OPERATION**: This command permanently deletes buckets and all their contents
- Always use -WhatIf first to preview the operation
- Consider using -Confirm for additional safety in production environments
- Wildcard patterns allow bulk deletion but require extra caution
- Pipeline processing enables complex filtering scenarios
- Ensure proper backup procedures before removing important buckets

## RELATED LINKS

[Get-OrchBucket](Get-OrchBucket.md)
[New-OrchBucket](New-OrchBucket.md)
[Copy-OrchBucket](Copy-OrchBucket.md)
