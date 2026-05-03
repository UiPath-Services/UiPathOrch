---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchJobMedia.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Remove-OrchJobMedia
---

# Remove-OrchJobMedia

## SYNOPSIS

Removes execution media recordings from Orchestrator.

## SYNTAX

### __AllParameterSets

```
Remove-OrchJobMedia [-JobId] <long[]> [-Path <string[]>] [-Recurse] [-Depth <uint>] [-WhatIf]
 [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

The `Remove-OrchJobMedia` cmdlet deletes execution media recordings from UiPath Orchestrator for the specified job IDs. The `-JobId` parameter is mandatory and does not support wildcards.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: DELETE execution media by job ID

OAuth required scopes: OR.Execution

Required permissions: ExecutionMedia.Delete

## EXAMPLES

### Example 1: Remove media for a specific job

```powershell
PS Orch1:\Shared> Remove-OrchJobMedia -JobId 12345
```

Removes all execution media recordings associated with job ID 12345 from the current folder.

### Example 2: Remove media for multiple jobs

```powershell
PS Orch1:\Shared> Remove-OrchJobMedia -JobId 12345, 67890, 11111
```

Removes execution media recordings associated with the specified job IDs from the current folder.

### Example 3: Preview removal with -WhatIf

```powershell
PS Orch1:\Shared> Remove-OrchJobMedia -JobId 12345 -WhatIf
```

Shows what would happen if the cmdlet runs without actually removing any media. Use this to verify which media would be deleted.

### Example 4: Remove media from a specific folder

```powershell
PS C:\> Remove-OrchJobMedia -Path Orch1:\Shared -JobId 12345
```

Removes execution media for job ID 12345 from the specified Orchestrator folder.

## PARAMETERS

### -Path

Specifies the target folder. If not specified, the current folder is targeted.

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

Specifies the depth for recursion into the target folders.
A depth of 0 indicates the current location only, with no subfolders included. When -Depth is specified, -Recurse is implied.

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

### -JobId

Specifies the job IDs for which to remove execution media. This parameter is mandatory and does not support wildcards. Tab completion shows job IDs that have media available.

```yaml
Type: System.Int64[]
DefaultValue: None
SupportsWildcards: false
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

Shows what would happen if the cmdlet runs.
The cmdlet is not run.

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.Int64[]

You can pipe the **JobId** values to this cmdlet.

### System.String[]

You can pipe the **Path** values to this cmdlet.

## OUTPUTS

### None

This cmdlet does not generate output objects.

## NOTES

This cmdlet processes each matching item individually, so if one removal fails, remaining items continue to be processed.

## RELATED LINKS

[Get-OrchJobMedia](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchJobMedia.md)

[Export-OrchJobMedia](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Export-OrchJobMedia.md)
