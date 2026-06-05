---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Add-OrchBucketLink.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 05/21/2026
PlatyPS schema version: 2024-05-01
title: Add-OrchBucketLink
---

# Add-OrchBucketLink

## SYNOPSIS

Links buckets to additional folders.

## SYNTAX

### __AllParameterSets

```
Add-OrchBucketLink [-Path <string[]>] [-LiteralPath <string[]>] [-Recurse] [-Depth <uint>] [-Name] <string[]>
 [-Link] <string[]> [-Confirm] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Links buckets to additional folders in UiPath Orchestrator. Bucket linking allows a bucket defined in one folder to be shared with other folders without duplicating the bucket. Linked folders can read from and write to the shared bucket, and changes are reflected in all linked folders.

The -Name parameter specifies which buckets to link, and the -Link parameter specifies the destination folders to share the buckets with. Both the source and destination folders must be on the same Orchestrator instance (same drive).

The -Name, -Path, and -Link parameters support tab completion. The -Name completion dynamically lists buckets from the source folder(s). The -Link parameter accepts folder paths on the same Orch: drive.

Use -Recurse (optionally with -Depth) to walk subfolders of -Path looking for matching buckets — useful when the same bucket name appears under multiple department folders and you want to link them all to a common destination in one call.

Primary Endpoint: POST /odata/Buckets/UiPath.Server.Configuration.OData.ShareToFolders

OAuth required scopes: OR.Administration or OR.Administration.Write

Required permissions: Buckets.Edit

## EXAMPLES

### Example 1: Link a bucket to another folder

```powershell
PS Orch1:\Shared> Add-OrchBucketLink -Name TestBucket1 -Link Orch1:\Dept#2
```

Links the "TestBucket1" bucket from the Shared folder to the Dept#2 folder. The Dept#2 folder can now access this bucket.

### Example 2: Preview link operation with -WhatIf

```powershell
PS Orch1:\Shared> Add-OrchBucketLink -Name TestBucket1 -Link Orch1:\Dept#2 -WhatIf
```

```output
What if: Performing the operation "Add BucketLink to Orch1:\Dept#2" on target "Orch1:\Shared\TestBucket1".
```

Shows what would happen without executing the command.

### Example 3: Link multiple buckets to multiple folders

```powershell
PS Orch1:\Shared> Add-OrchBucketLink -Name Test* -Link Orch1:\Dept#2, Orch1:\Dept#3
```

Links all buckets matching "Test*" to both the Dept#2 and Dept#3 folders. Both -Name and -Link accept wildcards and comma-separated values.

### Example 4: Recursively process subfolders

```powershell
PS C:\> Add-OrchBucketLink -Path Orch1:\Depts\* -Recurse -Depth 2 -Name SharedBucket -Link Orch1:\Common
```

Walks the subfolders of Orch1:\Depts up to 2 levels deep, finds buckets named "SharedBucket", and links each to the Common folder.

## PARAMETERS

### -Name

Specifies the names of buckets to link. This is a mandatory parameter. Supports wildcards and multiple comma-separated values. Tab completion dynamically lists buckets from the source folder(s).

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

Specifies the destination folder paths to link the buckets to. This is a mandatory parameter. The destination folders must be on the same Orchestrator instance (same drive) as the source. Supports wildcards and multiple comma-separated values.

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

Specifies the source folder(s) containing the buckets to link. If not specified, the current folder is used. Supports wildcards and comma-separated values.

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

The source and destination must be on the same Orchestrator drive. Cross-instance linking is not supported. To share buckets across instances, use Copy-OrchBucket instead.

If the bucket is already linked to the specified folder, the operation succeeds without error.

## RELATED LINKS

[Get-OrchBucketLink](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchBucketLink.md)

[Remove-OrchBucketLink](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchBucketLink.md)

[Get-OrchBucket](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchBucket.md)

[Copy-OrchBucket](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchBucket.md)

[Add-OrchAssetLink](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Add-OrchAssetLink.md)
