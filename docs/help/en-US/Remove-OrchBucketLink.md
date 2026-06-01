---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchBucketLink.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 05/21/2026
PlatyPS schema version: 2024-05-01
title: Remove-OrchBucketLink
---

# Remove-OrchBucketLink

## SYNOPSIS

Removes folder links from buckets.

## SYNTAX

### __AllParameterSets

```
Remove-OrchBucketLink [-Path <string[]>] [-Recurse] [-Depth <uint>] [-Name] <string[]>
 [-Link] <string[]> [-Confirm] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Removes folder links from buckets in UiPath Orchestrator. The opposite of Add-OrchBucketLink: detaches the specified destination folders from the bucket so they can no longer access it. The owning folder always retains access; this cmdlet only removes shared-access entries.

The -Name parameter specifies which buckets to unlink, and the -Link parameter specifies the folders to remove from each bucket's link list. Both the source and destination folders must be on the same Orchestrator instance (same drive).

The -Name, -Path, and -Link parameters support tab completion. The -Name completion dynamically lists buckets from the source folder(s). The -Link completion lists the folders currently linked to each bucket.

Use -Recurse (optionally with -Depth) to walk subfolders of -Path — useful when removing a link across many folders that share the same bucket name.

Primary Endpoint: POST /odata/Buckets/UiPath.Server.Configuration.OData.ShareToFolders (same endpoint as Add-OrchBucketLink; the two are distinguished by the request body)

OAuth required scopes: OR.Administration or OR.Administration.Write

Required permissions: Buckets.Edit

## EXAMPLES

### Example 1: Remove a bucket link

```powershell
PS Orch1:\Shared> Remove-OrchBucketLink -Name TestBucket1 -Link Orch1:\Dept#2
```

Removes the Dept#2 folder from "TestBucket1"'s link list. Dept#2 no longer has access to the bucket; Shared (the owning folder) is unaffected.

### Example 2: Preview removal with -WhatIf

```powershell
PS Orch1:\Shared> Remove-OrchBucketLink -Name TestBucket1 -Link Orch1:\Dept#2 -WhatIf
```

```output
What if: Performing the operation "Remove BucketLink from Orch1:\Dept#2" on target "Orch1:\Shared\TestBucket1".
```

Shows what would happen without executing.

### Example 3: Remove multiple links across multiple buckets

```powershell
PS Orch1:\Shared> Remove-OrchBucketLink -Name Test* -Link Orch1:\Dept#2, Orch1:\Dept#3
```

Removes both the Dept#2 and Dept#3 folder links from every bucket matching "Test*".

### Example 4: Recursively unlink across subfolders

```powershell
PS C:\> Remove-OrchBucketLink -Path Orch1:\Depts\* -Depth 2 -Name SharedBucket -Link Orch1:\Common
```

Walks the subfolders of Orch1:\Depts up to 2 levels deep, finds buckets named "SharedBucket", and removes the Common folder from each one's link list.

## PARAMETERS

### -Name

Specifies the names of buckets to unlink. This is a mandatory parameter. Supports wildcards and multiple comma-separated values. Tab completion dynamically lists buckets from the source folder(s).

```yaml
Type: System.String[]
DefaultValue: None
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

### -Link

Specifies the destination folder paths to remove from the buckets' link lists. This is a mandatory parameter. Supports wildcards and multiple comma-separated values. Tab completion lists the folders currently linked to each target bucket.

```yaml
Type: System.String[]
DefaultValue: None
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

Specifies the source folder(s) containing the buckets. If not specified, the current folder is used. Supports wildcards and comma-separated values.

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

Recursively searches subfolders of -Path for matching buckets. Combine with -Depth to limit how deep the recursion goes.

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

Limits how many levels of subfolder are searched when -Recurse is specified. 0 means only the immediate -Path folder is searched. Has no effect without -Recurse.

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

### -WhatIf

Shows what would happen if the cmdlet runs. The cmdlet is not run.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: False
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
DefaultValue: False
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

### System.String[]

You can pipe bucket names and link folder paths to this cmdlet via the Name, Link, and Path properties.

## OUTPUTS

### None

This cmdlet does not produce any output.

## NOTES

The owning folder of a bucket always retains access; this cmdlet only removes shared-access entries from linked folders. To delete a bucket entirely use Remove-OrchBucket.

If a folder in -Link is not currently linked to the bucket, the operation succeeds without error for that target.

## RELATED LINKS

[Add-OrchBucketLink](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Add-OrchBucketLink.md)

[Get-OrchBucketLink](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchBucketLink.md)

[Remove-OrchBucket](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchBucket.md)

[Remove-OrchAssetLink](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchAssetLink.md)
