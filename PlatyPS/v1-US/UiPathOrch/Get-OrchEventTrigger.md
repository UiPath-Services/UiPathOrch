---
document type: cmdlet
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchEventTrigger
---

# Get-OrchEventTrigger

## SYNOPSIS

Gets the event triggers.

## SYNTAX

### __AllParameterSets

```
Get-OrchEventTrigger [[-Name] <string[]>] [-Path <string[]>] [-Recurse] [-Depth <uint>]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Outputs information about event triggers with specified names in the target folders.
The target folders can be specified using the -Path, -Recurse, and -Depth parameters.
If these are not specified, the current location is used as the target folder.
If no event trigger names are specified, it outputs all event triggers in the target folders.

Multiple values for the -Path and -Name parameters can be specified using comma-separated text that includes wildcards.
Additionally, you can use autocomplete for these values by pressing [Ctrl+Space] or [Tab].

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name.
This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: GET /odata/ApiTriggers

OAuth required scopes: OR.Execution or OR.Execution.Read

Required permissions: EventTriggers.View

## EXAMPLES

### Example 1: Get all event triggers in the current folder

```powershell
PS Orch1:\Shared> Get-OrchEventTrigger
```

Gets all event triggers in the current folder.

### Example 2: Get event triggers by name with wildcards

```powershell
PS Orch1:\Shared> Get-OrchEventTrigger aaaa, 2*
```

Gets event triggers matching the specified names and wildcard patterns in the current folder. Multiple names can be specified as comma-separated values. Event trigger names can be auto-completed with [Ctrl+Space] or [Tab].

### Example 3: Get event triggers from specific folders

```powershell
PS C:\> Get-OrchEventTrigger -Path Orch1:\Shared,Orch1:\Dept#2 a*
```

Gets event triggers matching the wildcard pattern `a*` from the Shared folder and the Dept#2 folder.

### Example 4: Get event triggers recursively from all folders

```powershell
PS Orch1:\> Get-OrchEventTrigger -Recurse
```

Gets all event triggers from all folders recursively. When run from the root folder, this shows all event triggers across all folders in the tenant.

## PARAMETERS

### -Path

Specifies the target folder.
If not specified, the current folder will be targeted.

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

Specifies that the operation should include the target folder and all its subfolders.

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

### -Name

Specifies the Name of the event triggers to be retrieved.

```yaml
Type: System.String[]
DefaultValue: ''
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

You can pipe event trigger names to this cmdlet.

## OUTPUTS

### UiPath.PowerShell.Entities.ApiTrigger

This cmdlet returns ApiTrigger objects representing the event triggers.

## NOTES

Main endpoint called: GET /odata/ApiTriggers

Required Scope: OR.Execution or OR.Execution.Read

Required permissions: EventTriggers.View

## RELATED LINKS

Remove-OrchEventTrigger

Enable-OrchEventTrigger

Disable-OrchEventTrigger

Get-OrchTrigger

Get-OrchApiTrigger
