---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchBucketLink.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 05/21/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchBucketLink
---

# Get-OrchBucketLink

## SYNOPSIS

Gets the folder links of buckets.

## SYNTAX

### __AllParameterSets

```
Get-OrchBucketLink [-Path <string[]>] [-Recurse] [-Depth <uint>] [[-Name] <string[]>]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets the folder links of buckets in UiPath Orchestrator. Bucket linking allows a bucket defined in one folder to be shared with other folders without duplicating the bucket. This cmdlet shows which folders each bucket is accessible from.

Only buckets that are linked to multiple folders (more than one accessible folder) are included in the output. Buckets accessible from only their owning folder are not displayed.

The output is grouped by bucket name and shows all folders that have access to each linked bucket.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

The -Name and -Path parameters support tab completion. The -Name completion dynamically lists buckets from the target folders.

Primary Endpoint: GET /odata/Buckets/UiPath.Server.Configuration.OData.GetFoldersForBucket(id={bucketId})

OAuth required scopes: OR.Administration or OR.Administration.Read

Required permissions: Buckets.View

## EXAMPLES

### Example 1: Get all bucket links in the current folder

```powershell
PS Orch1:\Shared> Get-OrchBucketLink
```

Gets all buckets in the Shared folder that are linked to multiple folders. The output is grouped by bucket name, showing which folders each bucket is accessible from.

### Example 2: Get links for a specific bucket

```powershell
PS Orch1:\Shared> Get-OrchBucketLink SharedDocuments
```

Gets the folder links for the "SharedDocuments" bucket. Shows all folders that can access this bucket.

### Example 3: Get bucket links from a specific folder

```powershell
PS C:\> Get-OrchBucketLink -Path Orch1:\Shared Test*
```

Gets bucket links for buckets matching "Test*" in the Shared folder. When -Path uses an absolute path, the command can be run from any location.

### Example 4: Get bucket links recursively

```powershell
PS Orch1:\> Get-OrchBucketLink -Recurse
```

Gets all bucket links from all folders. This shows the complete bucket sharing topology across the Orchestrator instance.

## PARAMETERS

### -Name

Specifies the names of buckets whose links are to be retrieved. Supports wildcards and multiple comma-separated values. Tab completion dynamically lists buckets from the target folders.

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe bucket names to this cmdlet via the Name property.

## OUTPUTS

### UiPath.PowerShell.Entities.BucketLink

Returns BucketLink objects describing which folders have access to each linked bucket. Use Get-Member on the output for the full property surface.

## NOTES

Only buckets linked to multiple folders appear in the output. A bucket accessible from only its owning folder is not considered "linked" and is omitted.

When the same bucket is discovered from multiple source folders (e.g., with -Recurse), duplicate link groups are suppressed — each unique set of linked folders is shown only once.

## RELATED LINKS

[Add-OrchBucketLink](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Add-OrchBucketLink.md)

[Remove-OrchBucketLink](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchBucketLink.md)

[Get-OrchBucket](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchBucket.md)

[Get-OrchAssetLink](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchAssetLink.md)
