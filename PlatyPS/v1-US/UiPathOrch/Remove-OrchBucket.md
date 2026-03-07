---
document type: cmdlet
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Remove-OrchBucket
---

# Remove-OrchBucket

## SYNOPSIS

Removes storage buckets from UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
Remove-OrchBucket [-Name] <string[]> [-Path <string[]>] [-Recurse] [-Depth <uint>] [-WhatIf]
 [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Removes (deletes) storage buckets from UiPath Orchestrator folders. The specified buckets and all their contents are permanently deleted.

The -Name parameter is mandatory and supports tab completion. Press [Ctrl+Space] or [Tab] to see available bucket names dynamically populated from the target folders. Multiple values can be specified using comma-separated text that includes wildcards.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

This cmdlet supports ShouldProcess. Use -WhatIf to preview which buckets would be removed, or -Confirm to be prompted before each deletion.

Primary Endpoint: DELETE /odata/Buckets({bucketId})

OAuth required scopes: OR.Administration

Required permissions: Buckets.Delete

## EXAMPLES

### Example 1: Remove a bucket by name

```powershell
PS Orch1:\Shared> Remove-OrchBucket OldBucket
```

Removes the storage bucket named "OldBucket" from the current folder. The -Name parameter is positional (position 0) so the parameter name can be omitted.

### Example 2: Remove buckets using wildcards

```powershell
PS Orch1:\Shared> Remove-OrchBucket Temp*
```

Removes all storage buckets whose name starts with "Temp" from the current folder.

### Example 3: Remove a bucket from a specific folder

```powershell
PS C:\> Remove-OrchBucket -Path Orch1:\Shared -Name TestBucket
```

Removes the storage bucket named "TestBucket" from the Shared folder. When -Path uses an absolute path (Orch1:\...), the command can be run from any location.

### Example 4: Remove buckets recursively with confirmation

```powershell
PS Orch1:\> Remove-OrchBucket -Recurse OldData* -Confirm
```

Removes all storage buckets matching "OldData*" from all folders recursively, prompting for confirmation before each deletion.

### Example 5: Preview removal with -WhatIf

```powershell
PS Orch1:\Shared> Remove-OrchBucket TestBucket -WhatIf
```

```output
What if: Performing the operation "Remove Bucket" on target "Orch1:\Shared\TestBucket".
```

Shows what would happen without actually removing the bucket. Use this to verify which buckets would be affected before performing the operation.

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

### -Name

Specifies the names of the storage buckets to remove. This parameter is mandatory. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests bucket names from the target folders.

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

You can pipe bucket names to this cmdlet via the Name property.

## OUTPUTS

### System.Object

This cmdlet does not produce output. The bucket is deleted from the Orchestrator.

## NOTES

Buckets are folder-scoped entities. You must navigate to a folder on the Orch: drive or use -Path to specify target folders.

Removing a bucket permanently deletes the bucket and all files stored within it. This operation cannot be undone. The bucket cache and bucket link cache are cleared after each successful deletion.

## RELATED LINKS

Get-OrchBucket

New-OrchBucket

Copy-OrchBucket
