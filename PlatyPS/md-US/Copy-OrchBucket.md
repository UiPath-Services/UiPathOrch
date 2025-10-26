---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Copy-OrchBucket

## SYNOPSIS
Copies storage buckets to destination folders.

## SYNTAX

```
Copy-OrchBucket [[-Name] <String[]>] [-Destination] <String> [-Path <String>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The Copy-OrchBucket cmdlet copies storage buckets from source folders to destination folders within UiPath Orchestrator tenants or across different tenants. This cmdlet creates complete copies of storage buckets, including their configurations and metadata.

The cmdlet supports both intra-tenant copying (within the same tenant) and inter-tenant copying (between different tenants). Storage buckets can be copied to maintain consistency across different environments or for backup and deployment purposes.

Use the -Name parameter to specify which storage buckets to copy and the -Destination parameter to specify the target folder. The cmdlet supports wildcard patterns for copying multiple buckets efficiently.

This is a folder entity cmdlet. Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters. The -Recurse parameter enables copying storage buckets from all subfolders, maintaining the folder structure in the destination.

Primary Endpoint: GET /odata/Buckets, POST /odata/Buckets

OAuth required scopes: OR.Administration or OR.Administration.Read or OR.Administration.Write

Required permissions: Buckets.View, Buckets.Create

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Copy-OrchBucket DocumentStorage Orch1:\Production
```

Copies the DocumentStorage bucket from the current folder (Development) to the Production folder within the same tenant using positional parameters.

### Example 2
```powershell
PS C:\> Copy-OrchBucket -Path Orch1:\Development FileStorage Orch2:\Production
```

Copies the FileStorage bucket from Orch1:\Development to Orch2:\Production, demonstrating inter-tenant bucket copying.

### Example 3
```powershell
PS Orch1:\Development> Copy-OrchBucket *Storage*, *Archive* Orch1:\Production -WhatIf
```

Shows what would happen when copying multiple buckets with names containing Storage or Archive from the current folder to the Production folder using -WhatIf for safety.

### Example 4
```powershell
PS C:\> Copy-OrchBucket -Path Orch1:\Development *Data* Orch2:\Production
```

Copies all buckets containing Data in their name from Orch1:\Development to Orch2:\Production using wildcards for inter-tenant copying.

### Example 5
```powershell
PS Orch1:\> Copy-OrchBucket -Recurse *Backup* Orch2:\Finance -WhatIf
```

Shows what would happen when copying all buckets containing Backup from all subfolders recursively to Orch2:\Finance.

### Example 6
```powershell
PS Orch1:\Development> Get-OrchBucket *Shared* | Copy-OrchBucket -Destination Orch2:\Production
```

Gets all buckets containing Shared in their names and copies them to Orch2:\Production using pipeline input with wildcard filtering.

## PARAMETERS

### -Destination
Specifies the destination folders.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Name
Specifies the Name of the storage buckets to be copied.

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
Specifies the source folders. If not specified, the current folder will be used as the source.

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

### -Depth
Specifies the maximum number of subfolder levels to include when using -Recurse parameter.

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

### -Recurse
Specifies that storage buckets should be copied from all subfolders recursively, maintaining the folder structure in the destination.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.Bucket
## NOTES
This is a folder entity cmdlet. Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters.

The cmdlet supports both intra-tenant and inter-tenant copying. Use wildcards for efficient bulk operations and -WhatIf for testing before actual execution.

## RELATED LINKS

[Get-OrchBucket](Get-OrchBucket.md)

[Remove-OrchBucket](Remove-OrchBucket.md)
