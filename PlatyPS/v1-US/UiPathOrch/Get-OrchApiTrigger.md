---
document type: cmdlet
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchApiTrigger
---

# Get-OrchApiTrigger

## SYNOPSIS

Gets the API triggers.

## SYNTAX

### __AllParameterSets

```
Get-OrchApiTrigger [[-Name] <string[]>] [-Path <string[]>] [-Recurse] [-Depth <uint>]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Outputs information about API triggers with specified names in the target folders.
The target folders can be specified using the -Path, -Recurse, and -Depth parameters.
If these are not specified, the current location is used as the target folder.
If no API trigger names are specified, it outputs all API triggers in the target folders.

Multiple values for the -Path and -Name parameters can be specified using comma-separated text that includes wildcards.
Additionally, you can use autocomplete for these values by pressing [Ctrl+Space] or [Tab].

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name.
This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: GET /odata/HttpTriggers

OAuth required scopes: OR.Execution or OR.Execution.Read

Required permissions: HttpTriggers.View

## EXAMPLES

### Example 1: Get all API triggers in the current folder

```powershell
PS Orch1:\Dept#2> Get-OrchApiTrigger
```

Gets all API triggers in the current folder.

### Example 2: Get API triggers by name with wildcards

```powershell
PS Orch1:\Dept#2> Get-OrchApiTrigger my*, Report*
```

Gets API triggers matching the wildcard patterns `my*` and `Report*` in the current folder. Multiple names can be specified as comma-separated values. API trigger names can be auto-completed with [Ctrl+Space] or [Tab].

### Example 3: Get API triggers from specific folders

```powershell
PS C:\> Get-OrchApiTrigger -Path Orch1:\Dept#2,Orch1:\Production *trigger*
```

Gets API triggers matching the wildcard pattern `*trigger*` from the specified folders.

### Example 4: Get API triggers recursively from all folders

```powershell
PS Orch1:\> Get-OrchApiTrigger -Recurse
```

Gets all API triggers from all folders recursively. When run from the root folder, this shows all API triggers across all folders in the tenant.

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

### -Name

Specifies the Name of the API triggers to be retrieved.

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe API trigger names to this cmdlet.

## OUTPUTS

### UiPath.PowerShell.Entities.HttpTrigger

This cmdlet returns HttpTrigger objects representing the API triggers.

## NOTES

Main endpoint called: GET /odata/HttpTriggers

Required Scope: OR.Execution or OR.Execution.Read

Required permissions: HttpTriggers.View

## RELATED LINKS

Remove-OrchApiTrigger

Enable-OrchApiTrigger

Disable-OrchApiTrigger

Copy-OrchApiTrigger

Get-OrchTrigger

Get-OrchEventTrigger
