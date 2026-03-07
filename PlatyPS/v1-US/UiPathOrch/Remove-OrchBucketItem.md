---
document type: cmdlet
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Remove-OrchBucketItem
---

# Remove-OrchBucketItem

## SYNOPSIS

Removes files from storage buckets in UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
Remove-OrchBucketItem [[-Name] <string[]>] [[-FullPath] <string[]>] [-Path <string[]>] [-Recurse]
 [-Depth <uint>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Removes (deletes) files from UiPath Orchestrator storage buckets. The -Name parameter selects which buckets to target, and the -FullPath parameter specifies which files to remove within those buckets. Both parameters support wildcards, allowing batch deletion of multiple files.

The -Name and -FullPath parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available bucket names and file paths dynamically populated from the target folders. Multiple values can be specified using comma-separated text that includes wildcards.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

This cmdlet supports ShouldProcess. Use -WhatIf to preview which files would be removed, or -Confirm to be prompted before each deletion.

Folder processing is multi-threaded for improved performance when targeting multiple folders with -Recurse.

Primary Endpoint: DELETE /odata/Buckets({bucketId})/UiPath.Server.Configuration.OData.DeleteBlob

OAuth required scopes: OR.Administration

Required permissions: Buckets.View and BlobFiles.Delete

## EXAMPLES

### Example 1: Remove a specific file from a bucket

```powershell
PS Orch1:\Shared> Remove-OrchBucketItem InvoiceBucket old-report.pdf
```

Removes the file "old-report.pdf" from the storage bucket named "InvoiceBucket" in the current folder. Both -Name and -FullPath are positional parameters so the parameter names can be omitted.

### Example 2: Remove files using wildcards

```powershell
PS Orch1:\Shared> Remove-OrchBucketItem InvoiceBucket *.tmp
```

Removes all files with the .tmp extension from the storage bucket named "InvoiceBucket" in the current folder.

### Example 3: Remove all files from a bucket

```powershell
PS Orch1:\Shared> Remove-OrchBucketItem DataBucket *
```

Removes all files from the storage bucket named "DataBucket" in the current folder. The bucket itself is not deleted.

### Example 4: Remove files from a specific folder

```powershell
PS C:\> Remove-OrchBucketItem -Path Orch1:\Production -Name TempBucket -FullPath *.log
```

Removes all .log files from the storage bucket named "TempBucket" in the Production folder. When -Path uses an absolute path (Orch1:\...), the command can be run from any location.

### Example 5: Remove files recursively with confirmation

```powershell
PS Orch1:\> Remove-OrchBucketItem -Recurse -Name Temp* -FullPath *.tmp -Confirm
```

Removes all .tmp files from all storage buckets matching "Temp*" across all folders recursively, prompting for confirmation before each deletion.

### Example 6: Preview removal with -WhatIf

```powershell
PS Orch1:\Shared> Remove-OrchBucketItem DataBucket report.csv -WhatIf
```

```output
What if: Performing the operation "Remove BucketItem" on target "Orch1:\Shared\DataBucket\report.csv".
```

Shows what would happen without actually removing the file. Use this to verify which files would be affected before performing the operation.

## PARAMETERS

### -Path

Specifies the target folders. If not specified, the current folder is targeted. Supports wildcards and comma-separated values for multiple folders.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Recurse

Includes the target folder and all its subfolders in the operation.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Depth

Specifies the depth for recursion into the target folders. A depth of 0 targets only the current folder with no subfolders included. When -Depth is specified, -Recurse is implied.

```yaml
Type: System.UInt32
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -FullPath

Specifies the FullPath of the files to remove from the storage buckets. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests file paths from the target buckets.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: (All)
  Position: 1
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Name

Specifies the names of storage buckets to remove files from. If not specified, all storage buckets in the target folder are processed. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests bucket names from the target folders.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: (All)
  Position: 0
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -WhatIf

Runs the command in a mode that only reports what would happen without performing the actions.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases:
- wi
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Confirm

Prompts you for confirmation before running the cmdlet.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases:
- cf
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String

You can pipe file paths to this cmdlet via the FullPath property.

### System.String[]

You can pipe bucket names to this cmdlet via the Name property.

## OUTPUTS

### None

This cmdlet does not produce pipeline output. The specified files are deleted from the storage bucket.

## NOTES

Bucket items (files) are children of storage buckets. The -Name parameter identifies the parent bucket, and the -FullPath parameter specifies which files to remove.

Removing a bucket item permanently deletes the file from the storage bucket. This operation cannot be undone. The bucket file cache is cleared after each successful deletion.

If a file cannot be deleted (e.g., due to permissions or a storage provider error), a non-terminating error is written and the cmdlet continues processing remaining files.

## RELATED LINKS

Get-OrchBucketItem

Export-OrchBucketItem

Import-OrchBucketItem

Get-OrchBucket
