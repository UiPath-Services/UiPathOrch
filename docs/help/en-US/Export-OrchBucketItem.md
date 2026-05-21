---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Export-OrchBucketItem.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Export-OrchBucketItem
---

# Export-OrchBucketItem

## SYNOPSIS

Exports (downloads) files from storage buckets to the local filesystem.

## SYNTAX

### __AllParameterSets

```
Export-OrchBucketItem [-Path <string>] [-Recurse] [-Depth <uint>] [[-Name] <string[]>]
 [[-FullPath] <string[]>] [[-Destination] <string>] [-Confirm] [-WhatIf]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Downloads files from UiPath Orchestrator storage buckets to the local filesystem. Files are saved to the specified destination directory, organized by bucket name. If no destination is specified, the current filesystem directory is used.

The -Name parameter selects which storage buckets to export from, and the -FullPath parameter filters which files to download. Both parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available bucket names and file paths dynamically populated from the target folders. Multiple values can be specified using comma-separated text that includes wildcards.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

This cmdlet supports ShouldProcess. Use -WhatIf to preview which files would be exported, or -Confirm to be prompted before each download.

Folder processing is multi-threaded for improved performance when targeting multiple folders with -Recurse.

Primary Endpoint: GET /odata/Buckets({bucketId})/UiPath.Server.Configuration.OData.GetReadUri

OAuth required scopes: OR.Administration or OR.Administration.Read

Required permissions: Buckets.View and BlobFiles.View

## EXAMPLES

### Example 1: Export all files from a bucket

```powershell
PS Orch1:\Shared> Export-OrchBucketItem TestBucket2 C:\Export
```

Downloads all files from the storage bucket named "TestBucket2" to the C:\Export directory. Files are saved under C:\Export\TestBucket2\. The -Name and -Destination parameters are positional so parameter names can be omitted.

### Example 2: Export specific files using wildcards

```powershell
PS Orch1:\Shared> Export-OrchBucketItem TestBucket2 *.pdf C:\Export
```

Downloads only PDF files from the storage bucket named "TestBucket2" to C:\Export. The -FullPath parameter is positional (position 1).

### Example 3: Export from a specific folder

```powershell
PS C:\> Export-OrchBucketItem -Path Orch1:\Shared TestBucket2 -Destination C:\Backup
```

Downloads all files from the storage bucket named "TestBucket2" in the Shared folder to C:\Backup. When -Path uses an absolute path (Orch1:\...), the command can be run from any location.

### Example 4: Export files from all buckets recursively

```powershell
PS Orch1:\> Export-OrchBucketItem -Recurse -Destination C:\FullBackup
```

Downloads all files from all storage buckets in all folders recursively. Files are organized by relative folder path and bucket name under the destination directory.

### Example 5: Preview export with -WhatIf

```powershell
PS Orch1:\Shared> Export-OrchBucketItem TestBucket2 *.csv C:\Export -WhatIf
```

```output
What if: Performing the operation "Export BucketItem" on target "Item: Orch1:\Shared\TestBucket2\report.csv Destination: C:\Export\TestBucket2\report.csv".
```

Shows what would happen without actually downloading the files. Use this to verify which files would be affected before performing the operation.

## PARAMETERS

### -Path

Specifies the source folder on the Orchestrator. If not specified, the current folder is used as the source. Supports wildcards.

```yaml
Type: System.String
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

### -Destination

Specifies the local filesystem directory where the files will be saved. If not specified, the current filesystem directory is used. The directory must already exist. Files are organized under subdirectories by relative folder path and bucket name.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 2
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -FullPath

Specifies the FullPath of the files to download from the storage buckets. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests file paths from the target buckets.

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

Specifies the names of storage buckets to export from. If not specified, all storage buckets in the target folder are processed. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests bucket names from the target folders.

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

You can pipe a destination directory path to this cmdlet via the Destination property.

### System.String[]

You can pipe bucket names or file paths to this cmdlet via the Name and FullPath properties.

## OUTPUTS

### None

This cmdlet does not produce pipeline output. Files are written to the local filesystem at the specified destination.

## NOTES

Bucket items (files) are children of storage buckets. The -Name parameter identifies the parent bucket, and the -FullPath parameter filters which files to download.

The destination directory must exist before running this cmdlet; it will not be created automatically. However, subdirectories for bucket names are created automatically as needed.

If the -Destination parameter is not specified, the current filesystem location (not the Orch: drive location) is used. File names with invalid filesystem characters are sanitized automatically.

## RELATED LINKS

[Get-OrchBucketItem](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchBucketItem.md)

[Import-OrchBucketItem](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Import-OrchBucketItem.md)

[Remove-OrchBucketItem](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchBucketItem.md)

[Get-OrchBucket](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchBucket.md)
