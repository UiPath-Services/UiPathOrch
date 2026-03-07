---
document type: cmdlet
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Enable-OrchApiTrigger
---

# Enable-OrchApiTrigger

## SYNOPSIS

Enables the API triggers.

## SYNTAX

### __AllParameterSets

```
Enable-OrchApiTrigger [-Name] <string[]> [-Path <string[]>] [-Recurse] [-Depth <uint>] [-WhatIf]
 [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Enables API triggers that are currently disabled, with specified names in the target folders.
The target folders can be specified using the -Path, -Recurse, and -Depth parameters.
If these are not specified, the current location is used as the target folder.
Only API triggers that are currently disabled will be processed by this cmdlet.

Multiple values for the -Path and -Name parameters can be specified using comma-separated text that includes wildcards.
Additionally, you can use autocomplete for these values by pressing [Ctrl+Space] or [Tab].
The -Name parameter tab-completes only disabled API triggers.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name.
This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: POST /odata/HttpTriggers/UiPath.Server.Configuration.OData.SetEnabled

OAuth required scopes: OR.Execution

Required permissions: HttpTriggers.Edit

## EXAMPLES

### Example 1: Enable a specific API trigger

```powershell
PS Orch1:\Dept#2> Enable-OrchApiTrigger mytrigger
```

Enables the API trigger named 'mytrigger' in the 'Dept#2' folder, which is the current location.
The API trigger must be currently disabled for this command to take effect.

### Example 2: Enable API triggers using wildcards

```powershell
PS Orch1:\Dept#2> Enable-OrchApiTrigger my*
```

Enables all disabled API triggers matching the wildcard pattern 'my*' in the 'Dept#2' folder.

### Example 3: Enable all API triggers recursively

```powershell
PS Orch1:\> Enable-OrchApiTrigger -Recurse *
```

Enables all disabled API triggers in the current folder and all its subfolders.

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

Specifies the Name of the API triggers to be enabled.
Tab completion lists only disabled API triggers.

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

Main endpoint called: POST /odata/HttpTriggers/UiPath.Server.Configuration.OData.SetEnabled

Required Scope: OR.Execution

Required permissions: HttpTriggers.Edit

## RELATED LINKS

Get-OrchApiTrigger

Disable-OrchApiTrigger

Remove-OrchApiTrigger

Copy-OrchApiTrigger

Get-OrchTrigger

Get-OrchEventTrigger
