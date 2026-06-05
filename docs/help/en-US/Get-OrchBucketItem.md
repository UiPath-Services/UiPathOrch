---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchBucketItem.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchBucketItem
---

# Get-OrchBucketItem

## SYNOPSIS

Gets the items (files) stored in storage buckets.

## SYNTAX

### __AllParameterSets

```
Get-OrchBucketItem [-Path <string[]>] [-LiteralPath <string[]>] [-Recurse] [-Depth <uint>] [[-Name] <string[]>]
 [[-FullPath] <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets the files (blob items) stored in UiPath Orchestrator storage buckets. Each bucket item is a file with a FullPath property that identifies its location within the bucket. You can filter by bucket name and file path using wildcards.

The -Name parameter selects which storage buckets to query, and the -FullPath parameter filters the items within those buckets. Both parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available bucket names and file paths dynamically populated from the target folders. Multiple values can be specified using comma-separated text that includes wildcards.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Folder processing is multi-threaded for improved performance when targeting multiple folders with -Recurse.

Primary Endpoint: GET /odata/Buckets({bucketId})/UiPath.Server.Configuration.OData.GetFiles

OAuth required scopes: OR.Administration or OR.Administration.Read

Required permissions: Buckets.View and BlobFiles.View

## EXAMPLES

### Example 1: Get all items in all buckets

```powershell
PS Orch1:\Shared> Get-OrchBucketItem
```

Gets all files from all storage buckets in the current folder.

### Example 2: Get items in a specific bucket

```powershell
PS Orch1:\Shared> Get-OrchBucketItem TestBucket2
```

Gets all files stored in the storage bucket named "TestBucket2". The -Name parameter is positional (position 0) so the parameter name can be omitted.

### Example 3: Get items by file path with wildcards

```powershell
PS Orch1:\Shared> Get-OrchBucketItem TestBucket2 *.csv
```

Gets all CSV files from the storage bucket named "TestBucket2". The -FullPath parameter is positional (position 1) so the parameter name can be omitted.

### Example 4: Get items from a specific folder

```powershell
PS C:\> Get-OrchBucketItem -Path Orch1:\Production TestBucket2
```

Gets all files from the storage bucket named "TestBucket2" in the Production folder. When -Path uses an absolute path (Orch1:\...), the command can be run from any location.

### Example 5: Get items recursively from all folders

```powershell
PS Orch1:\> Get-OrchBucketItem -Recurse | Format-Table Name, FullPath, ContentType
```

Gets all bucket items from all folders recursively and displays them in a table format with selected properties.

### Example 6: Get items from multiple buckets using wildcards

```powershell
PS Orch1:\Shared> Get-OrchBucketItem Test* *.csv
```

Gets all CSV files from all storage buckets whose name starts with "Test" in the current folder.

## PARAMETERS

### -Path

Specifies the target folders. If not specified, the current folder is targeted. Supports wildcards and comma-separated values for multiple folders.

```yaml
Type: System.String[]
DefaultValue: None
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

### -LiteralPath

Specifies the target folder or drive by literal path -- wildcard metacharacters (`[`, `]`, `*`, `?`) are treated as literal characters rather than patterns. Accepts the same drive-qualified paths as -Path. Its `PSPath` alias also binds the path of items piped from Get-ChildItem / Get-Item, so you can pipe folders directly. Use -LiteralPath instead of -Path when a folder name contains a wildcard metacharacter.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: false
Aliases:
- PSPath
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
DefaultValue: False
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
DefaultValue: None
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

Specifies the FullPath of the files to retrieve from the storage buckets. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests file paths from the target buckets.

```yaml
Type: System.String[]
DefaultValue: None
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

Specifies the names of storage buckets to query. If not specified, all storage buckets in the target folder are queried. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests bucket names from the target folders.

```yaml
Type: System.String[]
DefaultValue: None
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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe bucket names or file paths to this cmdlet via the Name and FullPath properties.

## OUTPUTS

### UiPath.PowerShell.Entities.BlobFile

Returns BlobFile objects representing the files stored in the specified storage buckets. Each BlobFile contains properties such as FullPath, ContentType, and other file metadata.

## NOTES

Bucket items (BlobFile objects) are children of storage buckets. The -Name parameter identifies the parent bucket, and the -FullPath parameter filters the files within that bucket.

If a bucket cannot be accessed (e.g., due to permissions), a non-terminating error is written and the cmdlet continues processing remaining buckets.

## RELATED LINKS

[Get-OrchBucket](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchBucket.md)

[Export-OrchBucketItem](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Export-OrchBucketItem.md)

[Import-OrchBucketItem](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Import-OrchBucketItem.md)

[Remove-OrchBucketItem](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchBucketItem.md)
