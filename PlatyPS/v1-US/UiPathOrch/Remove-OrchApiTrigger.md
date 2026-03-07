---
document type: cmdlet
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Remove-OrchApiTrigger
---

# Remove-OrchApiTrigger

## SYNOPSIS

Removes the API triggers.

## SYNTAX

### __AllParameterSets

```
Remove-OrchApiTrigger [-Name] <string[]> [-Path <string[]>] [-Recurse] [-Depth <uint>] [-WhatIf]
 [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Removes API triggers with specified names from the target folders.
The target folders can be specified using the -Path, -Recurse, and -Depth parameters.
If these are not specified, the current location is used as the target folder.

Multiple values for the -Path and -Name parameters can be specified using comma-separated text that includes wildcards.
Additionally, you can use autocomplete for these values by pressing [Ctrl+Space] or [Tab].

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name.
This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: DELETE /odata/HttpTriggers({triggerId})

OAuth required scopes: OR.Execution

Required permissions: HttpTriggers.Delete

## EXAMPLES

### Example 1

```powershell
PS Orch1:\Shared> Remove-OrchApiTrigger MyApiTrigger
```

Removes the API trigger named 'MyApiTrigger' from the 'Shared' folder, which is the current location.

### Example 2

```powershell
PS Orch1:\Shared> Remove-OrchApiTrigger Test* -WhatIf
```

Shows what would happen if API triggers matching the wildcard pattern 'Test*' were removed from the 'Shared' folder, without actually removing them.

### Example 3

```powershell
PS Orch1:\> Remove-OrchApiTrigger -Recurse OldApiTrigger
```

Removes the API trigger named 'OldApiTrigger' from the current folder and all its subfolders.

### Example 4

```powershell
PS C:\> Remove-OrchApiTrigger -Path Orch1:\Shared MyApiTrigger
```

Removes the API trigger named 'MyApiTrigger' from the 'Shared' folder on Orch1.

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

### -Name

Specifies the Name of the API triggers to be removed.

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

### System.String[]

You can pipe API trigger names to this cmdlet.

## OUTPUTS

### None

This cmdlet does not produce output.

## NOTES

Main endpoint called: DELETE /odata/HttpTriggers({triggerId})

Required Scope: OR.Execution

Required permissions: HttpTriggers.Delete

## RELATED LINKS

Get-OrchApiTrigger

Enable-OrchApiTrigger

Disable-OrchApiTrigger

Copy-OrchApiTrigger

Get-OrchTrigger

Get-OrchEventTrigger
