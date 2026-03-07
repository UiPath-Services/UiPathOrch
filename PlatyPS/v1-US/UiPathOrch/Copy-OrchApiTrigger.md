---
document type: cmdlet
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Copy-OrchApiTrigger
---

# Copy-OrchApiTrigger

## SYNOPSIS

Copies the API triggers to a destination folder.

## SYNTAX

### __AllParameterSets

```
Copy-OrchApiTrigger [-Name] <string[]> [-Destination] <string> [-Path <string>] [-Recurse]
 [-Depth <uint>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Copies API triggers with specified names from the source folder to the destination folder.
The source folder can be specified using the -Path, -Recurse, and -Depth parameters.
If -Path is not specified, the current location is used as the source folder.
Cross-drive copy is supported, allowing you to copy API triggers between different Orchestrator connections.

Multiple values for the -Name parameter can be specified using comma-separated text that includes wildcards.
Additionally, you can use autocomplete for these values by pressing [Ctrl+Space] or [Tab].

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name.
This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: GET /odata/HttpTriggers, POST /odata/HttpTriggers

OAuth required scopes: OR.Execution

Required permissions: HttpTriggers.View (source), HttpTriggers.Create (destination)

## EXAMPLES

### Example 1

```powershell
PS Orch1:\Shared> Copy-OrchApiTrigger * Orch1:\Dept#2
```

Copies all API triggers from the 'Shared' folder to the 'Dept#2' folder on the same Orchestrator.

### Example 2

```powershell
PS Orch1:\Shared> Copy-OrchApiTrigger api* Orch1:\Dept#2
```

Copies all API triggers matching the wildcard pattern 'api*' from the 'Shared' folder to the 'Dept#2' folder.

### Example 3

```powershell
PS C:\> Copy-OrchApiTrigger -Path Orch1:\Shared * Orch2:\Shared
```

Copies all API triggers from the 'Shared' folder on Orch1 to the 'Shared' folder on Orch2.
This demonstrates cross-drive copy between different Orchestrator connections.

### Example 4

```powershell
PS Orch1:\> Copy-OrchApiTrigger -Recurse * Orch2:\
```

Copies all API triggers from the current folder and all its subfolders on Orch1 to the root folder on Orch2.

## PARAMETERS

### -Path

Specifies the source folder.
If not specified, the current folder will be used as the source.

```yaml
Type: System.String
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

Specifies that the operation should include the source folder and all its subfolders.

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

Specifies the depth for recursion into the source folders.
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

### -Destination

Specifies the destination folder where the API triggers will be copied to.

```yaml
Type: System.String
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

### -Name

Specifies the Name of the API triggers to be copied.

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

### System.String

You can pipe a destination path to this cmdlet.

## OUTPUTS

### None

This cmdlet does not produce output.

## NOTES

Main endpoint called: GET /odata/HttpTriggers, POST /odata/HttpTriggers

Required Scope: OR.Execution

Required permissions: HttpTriggers.View (source), HttpTriggers.Create (destination)

## RELATED LINKS

Get-OrchApiTrigger

Remove-OrchApiTrigger

Enable-OrchApiTrigger

Disable-OrchApiTrigger

Get-OrchTrigger

Get-OrchEventTrigger
