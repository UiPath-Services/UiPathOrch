---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Import-OrchBucketItem

## SYNOPSIS
Imports files and folders into UiPath Orchestrator storage buckets.

## SYNTAX

```
Import-OrchBucketItem [-Source] <String[]> [[-Name] <String[]>] [-Path <String[]>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION

The Import-OrchBucketItem cmdlet imports files and directories from the local file system into UiPath Orchestrator storage buckets. It provides comprehensive support for single files, multiple files, wildcard patterns, and recursive directory structures.

Primary Endpoint: POST /odata/Buckets({bucketId})/UiPath.Server.Configuration.OData.UploadFiles

OAuth required scopes: OR.Administration

Required permissions: Buckets.Edit and BlobFiles.Create

## EXAMPLES

### Example 1: Import a single file
```powershell
PS Orch1:\Shared> Import-OrchBucketItem -Source C:\Documents\report.pdf -Name DocumentsBucket
```

Imports a single PDF file into the specified bucket in the current Orchestrator folder.

### Example 2: Import multiple files with wildcards
```powershell
PS Orch1:\Production> Import-OrchBucketItem -Source C:\Data\*.json, C:\Logs\*.log -Name ConfigBucket
```

Imports multiple file types using wildcard patterns.

### Example 3: Import from file system using -Path
```powershell
PS C:\> Import-OrchBucketItem -Path Orch1:\Production -Source C:\Config\*.json -Name ConfigBucket
```

Imports files from the file system to a specific Orchestrator folder using -Path parameter.

### Example 4: Recursive directory import
```powershell
PS Orch1:\Backup> Import-OrchBucketItem -Source C:\ExportedStructure\ -Recurse
```

Recursively imports a directory structure. Bucket names are inferred from folder structure. Target folders and buckets must exist.

### Example 5: Preview operation with -WhatIf
```powershell
PS C:\Data> Import-OrchBucketItem -Source *.* -Name DataBucket -Path Orch1:\Testing -WhatIf
```

Shows what files would be imported without performing the actual operation.

### Example 6: Import with bucket wildcards
```powershell
PS Orch1:\Development> Import-OrchBucketItem -Source C:\Templates\*.json -Name Project*_Config
```

Uses bucket name wildcards to target multiple matching buckets.

### Example 7: Cross-folder import workflow
```powershell
PS Orch1:\> Set-Location TargetFolder
PS Orch1:\TargetFolder> New-OrchBucket -Name ImportedBucket -Description "Restored from export"
PS Orch1:\TargetFolder> Import-OrchBucketItem -Source C:\Backup\*.* -Name ImportedBucket
```

Demonstrates the complete workflow for cross-folder imports with proper setup.

## PARAMETERS

### -Source
Specifies the source files or directories to import. Supports file paths, directory paths, arrays, and wildcard patterns.

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

### -Name
Specifies the target storage bucket name(s). Supports wildcard patterns for multiple bucket operations.

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

### -Path
Specifies the target folder path within Orchestrator. If not specified, the current folder is used.

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
Imports files from subdirectories recursively. Cannot be used with -Name parameter.

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

### -Depth
Specifies the maximum depth for recursive operations. Only effective when -Recurse is used.

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

### -WhatIf
Shows what would happen if the cmdlet runs without performing the actual import.

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

### -Confirm
Prompts for confirmation before running the cmdlet.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]
You can pipe file paths or directory paths to this cmdlet.

## OUTPUTS

### UiPath.PowerShell.Entities.Bucket
Returns the bucket object(s) containing the imported files.

## NOTES

Parameter Restrictions: -Recurse and -Name cannot be used together.

File Overwriting: Files with identical names in the same bucket will overwrite existing files. The last imported file takes precedence.

Name Parameter Requirement: When importing individual files without -Recurse, the -Name parameter is required. The cmdlet will warn and skip files when -Name is missing.

Bucket Auto-Discovery: When -Name is not specified with -Recurse, bucket names are inferred from the source directory structure.

Cross-Folder Import: Files can be imported to different Orchestrator folders, but target folders and buckets must exist beforehand.

Wildcard Support: File patterns like *.txt, report_2024_*.pdf, *.json, *.xml and bucket patterns like Project*, Test_*_Bucket are supported.

Best Practices: Always specify -Name for predictable bucket targeting. Use -WhatIf to preview operations before execution. Ensure unique file names to avoid unintended overwrites. Pre-create target folder structures for cross-folder imports. Use wildcard patterns for efficient bulk operations.

## RELATED LINKS

[Export-OrchBucketItem]()

[Get-OrchBucketItem]()

[New-OrchBucket]()

[Remove-OrchBucket]()

[UiPath Orchestrator Documentation](https://docs.uipath.com/)
