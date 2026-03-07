---
document type: cmdlet
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Import-OrchBucketItem
---

# Import-OrchBucketItem

## SYNOPSIS

Imports (uploads) files from the local filesystem into storage buckets.

## SYNTAX

### __AllParameterSets

```
Import-OrchBucketItem [-Source] <string[]> [[-Name] <string[]>] [-Path <string[]>] [-Recurse]
 [-Depth <uint>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Uploads files from the local filesystem into UiPath Orchestrator storage buckets. The -Source parameter specifies local files or directories to upload, and the target bucket is determined either by the -Name parameter or automatically from the local directory structure.

When -Name is not specified, the cmdlet automatically determines the target bucket name from the parent directory of each source file. This allows you to organize local files in directories named after buckets and import them in a single operation.

When -Source specifies a directory and -Recurse is used, the cmdlet enumerates files under the directory respecting the -Depth parameter. The directory structure is used to infer the target bucket names and folder paths. You cannot specify -Recurse and -Name at the same time.

The -Name parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available bucket names dynamically populated from the target folders.

This cmdlet supports ShouldProcess. Use -WhatIf to preview which files would be uploaded, or -Confirm to be prompted before each upload.

Primary Endpoint: POST /odata/Buckets({bucketId})/UiPath.Server.Configuration.OData.GetWriteUri

OAuth required scopes: OR.Administration

Required permissions: Buckets.View and BlobFiles.Create

## EXAMPLES

### Example 1: Import a file into a specific bucket

```powershell
PS Orch1:\Shared> Import-OrchBucketItem C:\Data\TestBucket2\report.pdf
```

Uploads the file report.pdf into the storage bucket named "TestBucket2" in the current folder. The bucket name is automatically determined from the parent directory name of the source file. The -Source parameter is positional (position 0) so the parameter name can be omitted.

### Example 2: Import a file into a named bucket

```powershell
PS Orch1:\Shared> Import-OrchBucketItem C:\Data\report.pdf DataBucket
```

Uploads the file report.pdf into the storage bucket named "DataBucket" in the current folder. The -Name parameter is positional (position 1) so the parameter name can be omitted.

### Example 3: Import files from a directory

```powershell
PS Orch1:\Shared> Import-OrchBucketItem C:\Export\TestBucket2\
```

Uploads all files directly under the C:\Export\TestBucket2\ directory into the storage bucket named "TestBucket2". The bucket name is inferred from the directory name.

### Example 4: Import files recursively from a directory structure

```powershell
PS Orch1:\> Import-OrchBucketItem C:\Export -Recurse
```

Uploads all files under C:\Export recursively. The directory structure is used to determine the target folders and bucket names. Each file's nearest parent directory name is used as the bucket name.

### Example 5: Import from a specific folder

```powershell
PS C:\> Import-OrchBucketItem -Path Orch1:\Shared -Source C:\Data\report.pdf -Name TestBucket2
```

Uploads report.pdf into the storage bucket named "TestBucket2" in the Shared folder. When -Path uses an absolute path (Orch1:\...), the command can be run from any location.

### Example 6: Preview import with -WhatIf

```powershell
PS Orch1:\Shared> Import-OrchBucketItem C:\Data\report.pdf DataBucket -WhatIf
```

```output
What if: Performing the operation "Import BucketItem" on target "Item: 'C:\Data\report.pdf' Folder: 'Orch1:\Shared' Bucket: 'DataBucket'".
```

Shows what would happen without actually uploading the files. Use this to verify which files would be affected before performing the operation.

## PARAMETERS

### -Path

Specifies the target folder on the Orchestrator where the bucket exists. If not specified, the current folder is used. Supports wildcards.

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

Specifies that -Source directories should be enumerated recursively. When used, the directory structure determines the target folder and bucket mapping. Cannot be used together with -Name.

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

Specifies the depth for recursion when enumerating -Source directories. A depth of 0 includes only files directly under the source directory. When -Depth is specified, -Recurse is implied.

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

### -Name

Specifies the name of the target storage bucket. If not specified, the bucket name is automatically determined from the parent directory of each source file. Supports wildcards. Tab completion dynamically suggests bucket names from the target folders. Cannot be used together with -Recurse.

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

### -Source

Specifies the local filesystem file or directory paths to upload. This parameter is mandatory. Supports wildcards for file path resolution. Only FileSystem provider paths are accepted.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: (All)
  Position: 0
  IsRequired: true
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

You can pipe source file paths to this cmdlet via the Source property.

### System.String[]

You can pipe bucket names to this cmdlet via the Name property.

## OUTPUTS

### None

This cmdlet does not produce pipeline output. Files are uploaded to the specified storage bucket on the Orchestrator.

## NOTES

Bucket items (files) are children of storage buckets. The -Name parameter identifies the target bucket.

When -Name is not specified, the cmdlet uses the parent directory name of each source file to determine the target bucket. If the bucket name cannot be matched exactly (case-insensitive), the cmdlet attempts a wildcard match where underscores in the directory name are treated as single-character wildcards. This handles cases where export may have replaced special characters with underscores.

The -Source parameter only accepts FileSystem provider paths. Attempting to use an Orch: drive path as a source will result in an error.

You cannot specify -Recurse (or -Depth greater than 1) and -Name at the same time. When using -Recurse, the directory structure determines the target bucket mapping automatically.

## RELATED LINKS

Get-OrchBucketItem

Export-OrchBucketItem

Remove-OrchBucketItem

Get-OrchBucket
