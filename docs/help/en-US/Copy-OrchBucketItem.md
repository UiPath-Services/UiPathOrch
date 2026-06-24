---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchBucketItem.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 06/24/2026
PlatyPS schema version: 2024-05-01
title: Copy-OrchBucketItem
---

# Copy-OrchBucketItem

## SYNOPSIS

Copies the files inside storage buckets directly from one folder, drive, or tenant to another.

## SYNTAX

### __AllParameterSets

```
Copy-OrchBucketItem [-Name] <string[]> [-FullPath] <string[]> [-Destination] <string>
 [[-DestinationBucket] <string>] [-Path <string>] [-LiteralPath <string>] [-Recurse] [-Depth <uint>]
 [-Confirm] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Copies the files contained in UiPath Orchestrator storage buckets straight from a source to a destination — the bucket-file counterpart of `Copy-OrchQueueItem`. A folder copy (`Copy-Item -Recurse`) and `Copy-OrchBucket` reproduce only the bucket *definition*; this cmdlet carries the *contents*.

The transfer streams each file from the source's pre-signed read URI directly into the destination's pre-signed write URI, with **no local staging** — nothing is written to local disk. The read runs through the source drive's connection and the write through the destination drive's connection, so each side's proxy / SSL configuration applies. (For staging to local disk — for backup, inspection, or editing before upload — use `Export-OrchBucketItem` and `Import-OrchBucketItem` instead.)

The **destination bucket must already exist**; create it first with `Copy-OrchBucket` (or `New-OrchBucket`). A missing destination bucket is reported as a warning and skipped — it is never created implicitly. Each file is copied into the same-named bucket in the structurally-corresponding destination folder; use -DestinationBucket to redirect a single source bucket's files into a differently-named bucket. The full in-bucket path of each file is preserved (the file is written to its `FullPath`, including any sub-directory structure).

-Name (which source buckets), -FullPath (which files), and -Destination (the destination folder) are all **mandatory** and **positional** (positions 0, 1, 2), matching the `Export-OrchBucketItem` argument order. As a cmdlet that writes data, it follows the module convention of an explicit selector — there is no silent "copy everything" default. To copy all buckets or all files, pass `*` explicitly (e.g. `Copy-OrchBucketItem * * Destination:\ -Recurse`). -Name, -FullPath, and -DestinationBucket support tab completion; -DestinationBucket completes against the buckets that already exist in the -Destination folder. When specifying -Path, -Recurse, and -Depth, place them immediately after the cmdlet name so autocomplete for subsequent parameters works correctly.

This cmdlet supports ShouldProcess. Use -WhatIf to preview which files would be copied, or -Confirm to be prompted before each file. The successfully-copied source files are emitted to the pipeline, so a copy-then-delete "move" can be performed by piping the output into `Remove-OrchBucketItem`.

Primary Endpoints: GET /odata/Buckets({bucketId})/UiPath.Server.Configuration.OData.GetReadUri (source) and GET /odata/Buckets({bucketId})/UiPath.Server.Configuration.OData.GetWriteUri (destination). Both return a pre-signed blob URI; the content is then streamed directly from the source's pre-signed GET into the destination's pre-signed PUT, with no local staging.

OAuth required scopes: source OR.Administration or OR.Administration.Read; destination OR.Administration or OR.Administration.Write

Required permissions: source Buckets.View and BlobFiles.View; destination Buckets.View and BlobFiles.Create

## EXAMPLES

### Example 1: Migrate every bucket's files across tenants

```powershell
PS C:\> Copy-OrchBucket     -Path Orch1:\ -Destination Orch2:\ -Recurse
PS C:\> Copy-OrchBucketItem -Path Orch1:\ * * -Destination Orch2:\ -Recurse
```

First `Copy-OrchBucket` recreates the bucket definitions and folder structure on the destination tenant; then `Copy-OrchBucketItem` copies the files into them. The `* *` selects all buckets and all files explicitly. Together they migrate buckets in full, with no local staging.

### Example 2: Copy one bucket's files between folders

```powershell
PS Orch1:\Shared> Copy-OrchBucketItem MyBucket * \Production
```

Copies all files from the `MyBucket` bucket in the current folder (Shared) into the same-named bucket in the Production folder. -Name, -FullPath, and -Destination are positional; the `*` selects all files in the bucket.

### Example 3: Copy into a differently-named bucket

```powershell
PS C:\> Copy-OrchBucketItem -Path Orch1:\Shared MyBucket * -Destination Orch2:\Shared -DestinationBucket ArchiveBucket
```

Copies the files from `MyBucket` on Orch1 into `ArchiveBucket` on Orch2. -DestinationBucket applies to a single source bucket; it cannot be combined with -Recurse.

### Example 4: Copy only selected files using wildcards

```powershell
PS Orch1:\Shared> Copy-OrchBucketItem MyBucket *.csv Orch2:\Shared
```

Copies only the CSV files from `MyBucket` into the same-named bucket on Orch2. -FullPath is positional (position 1).

### Example 5: Move items (copy, then delete the copied ones from the source)

```powershell
PS Orch1:\Shared> Copy-OrchBucketItem MyBucket * Orch2:\Shared | Remove-OrchBucketItem
```

The successfully-copied source files are emitted, so piping into `Remove-OrchBucketItem` deletes them from the source for a transaction-safe move.

### Example 6: Preview with -WhatIf

```powershell
PS Orch1:\Shared> Copy-OrchBucketItem MyBucket * Orch2:\Shared -WhatIf
```

```output
What if: Performing the operation "Copy BucketItem" on target "Item: 'Orch1:\Shared\MyBucket/report.csv' Destination: 'Orch2:\Shared\MyBucket/report.csv'".
```

Shows which files would be copied without transferring anything.

## PARAMETERS

### -Name

Specifies the names of the source storage buckets to copy files from. Mandatory — pass `*` to select all buckets in the source folder. Supports wildcards and multiple comma-separated values. Tab completion suggests bucket names from the source folders. Aliased as `-Bucket`, so files piped from `Get-OrchBucketItem` bind it by property name (e.g. `Get-OrchBucketItem reports *.csv | Copy-OrchBucketItem -Destination Cloud:\Reports`).

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: true
Aliases:
- Bucket
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

### -Destination

Specifies the destination folder (drive-qualified, e.g. `Orch2:\Shared`). Files are copied into the same-named bucket in this folder, or into the bucket named by -DestinationBucket. Supports wildcards. Mandatory; positional (position 2).

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: (All)
  Position: 2
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -DestinationBucket

Redirects the copy into a destination bucket whose name differs from the source bucket. Only valid when the source resolves to a single bucket; it cannot be combined with -Recurse. If omitted, files are copied into the same-named bucket in the destination folder. Positional (position 3, right after -Destination); tab completion suggests the buckets that already exist in the -Destination folder.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 3
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -FullPath

Selects which files to copy by their FullPath within the source buckets. Mandatory — pass `*` to copy all files in the bucket. Supports wildcards and multiple comma-separated values. Tab completion suggests file paths from the source buckets.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: (All)
  Position: 1
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

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

### -LiteralPath

Specifies the source folder or drive by literal path -- wildcard metacharacters (`[`, `]`, `*`, `?`) are treated as literal characters rather than patterns. Accepts the same drive-qualified paths as -Path. Its `PSPath` alias also binds the path of items piped from Get-ChildItem / Get-Item, so you can pipe folders directly. Use -LiteralPath instead of -Path when a folder name contains a wildcard metacharacter.

```yaml
Type: System.String
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

Includes the source folder and all its subfolders, copying files into the structurally-corresponding destination folders. Cannot be combined with -DestinationBucket.

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

Specifies the depth for recursion into the source folders. A depth of 0 targets only the current folder with no subfolders included. When -Depth is specified, -Recurse is implied.

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

You can pipe a destination folder path to this cmdlet via the Destination property.

### System.String[]

You can pipe source bucket names or file paths to this cmdlet via the Name and FullPath properties.

## OUTPUTS

### UiPath.PowerShell.Entities.BlobFile

The source files that were successfully copied. Pipe them into `Remove-OrchBucketItem` to perform a copy-then-delete move.

## NOTES

The destination bucket must exist before running this cmdlet — copy the bucket definitions first with `Copy-OrchBucket` (or `Copy-Item -Recurse`, which copies definitions but not contents). A missing destination bucket is a warning, not an error.

For Orchestrator-hosted buckets (StorageProvider = Orchestrator), this transfers the file contents. For external-storage buckets (Amazon S3, Azure Blob, MinIO, etc.) the files reside in the customer's own storage; in that case copy the bucket definition and reset its credential with `Update-OrchBucket` rather than re-uploading content.

Unlike `Export-OrchBucketItem` / `Import-OrchBucketItem`, which stage files on local disk, this cmdlet streams source to destination directly. Use the Export/Import pair when you need the files on disk (backup, inspection, or editing before upload).

## RELATED LINKS

[Copy-OrchBucket](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchBucket.md)

[Export-OrchBucketItem](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Export-OrchBucketItem.md)

[Import-OrchBucketItem](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Import-OrchBucketItem.md)

[Get-OrchBucketItem](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchBucketItem.md)

[Remove-OrchBucketItem](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchBucketItem.md)
