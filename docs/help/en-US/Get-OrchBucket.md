---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchBucket.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchBucket
---

# Get-OrchBucket

## SYNOPSIS

Gets storage buckets from UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
Get-OrchBucket [-Path <string[]>] [-Recurse] [-Depth <uint>] [[-Name] <string[]>]
 [-CsvEncoding <Encoding>] [-ExportCsv <string>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets storage bucket information from UiPath Orchestrator folders. A storage bucket is a container for storing files (blob objects) such as documents, images, and other data used by automation workflows. Buckets can use different storage providers (e.g., Orchestrator internal storage, Azure Blob Storage, Amazon S3).

The -Name parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available bucket names dynamically populated from the target folders. Multiple values can be specified using comma-separated text that includes wildcards.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Folder processing is multi-threaded for improved performance when targeting multiple folders with -Recurse.

Primary Endpoint: GET /odata/Buckets

OAuth required scopes: OR.Administration or OR.Administration.Read

Required permissions: Buckets.View

## EXAMPLES

### Example 1: Get all buckets in the current folder

```powershell
PS Orch1:\Shared> Get-OrchBucket
```

Gets all storage buckets from the current folder and returns their properties including Name, Description, StorageProvider, and StorageContainer.

### Example 2: Get a bucket by name

```powershell
PS Orch1:\Shared> Get-OrchBucket TestBucket2
```

Gets the storage bucket named "TestBucket2" from the current folder. The -Name parameter is positional (position 0) so the parameter name can be omitted.

### Example 3: Get buckets by name with wildcards

```powershell
PS Orch1:\Shared> Get-OrchBucket Test*
```

Gets all storage buckets whose name starts with "Test" from the current folder. The -Name parameter supports wildcards.

### Example 4: Get buckets from a specific folder

```powershell
PS C:\> Get-OrchBucket -Path Orch1:\Production TestBucket2
```

Gets the storage bucket named "TestBucket2" from the Production folder. When -Path uses an absolute path (Orch1:\...), the command can be run from any location.

### Example 5: Get buckets recursively from all folders

```powershell
PS Orch1:\> Get-OrchBucket -Recurse | Format-Table Name, Description, StorageProvider
```

Gets all storage buckets from all folders recursively and displays them in a table format with selected properties.

### Example 6: Export buckets to CSV

```powershell
PS Orch1:\Shared> Get-OrchBucket -ExportCsv c:buckets.csv
```

Exports all storage buckets in the current folder to a CSV file. The CSV includes columns: Path, Name, Description, StorageProvider, StorageContainer, StorageParameters, CredentialStore, Password, ExternalName, Options, and Tags. CredentialStoreId values are resolved to human-readable CredentialStore names. When the current location is an Orch: drive, prefix the filename with c: to write to the filesystem.

### Example 7: Export buckets recursively

```powershell
PS Orch1:\> Get-OrchBucket -Recurse -ExportCsv c:all-buckets.csv
```

Exports storage buckets from all folders recursively to a CSV file.

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

### -CsvEncoding

Specifies the encoding for CSV export. Default is UTF-8 with BOM for Excel compatibility. Tab completion suggests all available system encodings (e.g., utf-8, shift_jis, us-ascii).

```yaml
Type: System.Text.Encoding
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

### -ExportCsv

Exports storage bucket information to the specified CSV file path. The CSV includes comprehensive bucket information with headers: Path, Name, Description, StorageProvider, StorageContainer, StorageParameters, CredentialStore, Password, ExternalName, Options, and Tags. CredentialStoreId values are resolved to human-readable CredentialStore names. Requires a filesystem path (not an Orch: drive path).

```yaml
Type: System.String
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

### -Name

Specifies the names of storage buckets to retrieve. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests bucket names from the target folders.

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

You can pipe bucket names to this cmdlet via the Name property.

## OUTPUTS

### UiPath.PowerShell.Entities.Bucket

Returns Bucket objects with properties including Name, Description, StorageProvider, StorageContainer, StorageParameters, CredentialStoreId, ExternalName, Options, Tags, and FolderPath.

## NOTES

Buckets are folder-scoped entities. You must navigate to a folder on the Orch: drive or use -Path to specify target folders.

The -ExportCsv parameter resolves CredentialStoreId values to human-readable CredentialStore names in the exported CSV. If the CredentialStore cannot be retrieved, a warning is displayed. The default CSV filename is "ExportedBuckets.csv".

## RELATED LINKS

[New-OrchBucket](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/New-OrchBucket.md)

[Remove-OrchBucket](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchBucket.md)

[Copy-OrchBucket](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchBucket.md)

[Get-OrchBucketItem](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchBucketItem.md)
