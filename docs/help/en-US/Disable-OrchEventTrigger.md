---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Disable-OrchEventTrigger.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Disable-OrchEventTrigger
---

# Disable-OrchEventTrigger

## SYNOPSIS

Disables the event triggers.

## SYNTAX

### __AllParameterSets

```
Disable-OrchEventTrigger [-Path <string[]>] [-Recurse] [-Depth <uint>] [-Name] <string[]>
 [-Confirm] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Disables event triggers that are currently enabled, with specified names in the target folders.
The target folders can be specified using the -Path, -Recurse, and -Depth parameters.
If these are not specified, the current location is used as the target folder.
Only event triggers that are currently enabled will be processed by this cmdlet.

Multiple values for the -Path and -Name parameters can be specified using comma-separated text that includes wildcards.
Additionally, you can use autocomplete for these values by pressing [Ctrl+Space] or [Tab].
The -Name parameter tab-completes only enabled event triggers.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name.
This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: POST /odata/ApiTriggers/UiPath.Server.Configuration.OData.SetEnabled

OAuth required scopes: OR.Execution

Required permissions: EventTriggers.Edit

## EXAMPLES

### Example 1: Disable a specific event trigger

```powershell
PS Orch1:\Shared> Disable-OrchEventTrigger MyEventTrigger
```

Disables the event trigger named 'MyEventTrigger' in the 'Shared' folder, which is the current location.
The event trigger must be currently enabled for this command to take effect.

### Example 2: Disable event triggers using wildcards

```powershell
PS Orch1:\Shared> Disable-OrchEventTrigger My*
```

Disables all enabled event triggers matching the wildcard pattern 'My*' in the 'Shared' folder.

### Example 3: Disable all event triggers recursively

```powershell
PS Orch1:\> Disable-OrchEventTrigger -Recurse *
```

Disables all enabled event triggers in the current folder and all its subfolders.

## PARAMETERS

### -Path

Specifies the target folder. If not specified, the current folder is targeted.

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

Includes the target folder and all its subfolders in the operation.

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

Specifies the depth for recursion into the target folders.
A depth of 0 indicates the current location only, with no subfolders included. When -Depth is specified, -Recurse is implied.

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

### -Name

Specifies the Name of the event triggers to be disabled.
Tab completion lists only enabled event triggers.

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

Shows what would happen if the cmdlet runs.
The cmdlet is not run.

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe event trigger names to this cmdlet.

## OUTPUTS

### None

This cmdlet does not produce output.

## NOTES

Main endpoint called: POST /odata/ApiTriggers/UiPath.Server.Configuration.OData.SetEnabled

Required Scope: OR.Execution

Required permissions: EventTriggers.Edit

## RELATED LINKS

[Get-OrchEventTrigger](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchEventTrigger.md)

[Enable-OrchEventTrigger](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Enable-OrchEventTrigger.md)

[Remove-OrchEventTrigger](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchEventTrigger.md)

[Get-OrchTrigger](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchTrigger.md)

[Get-OrchApiTrigger](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchApiTrigger.md)
