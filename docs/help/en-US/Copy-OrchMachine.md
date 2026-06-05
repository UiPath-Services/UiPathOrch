---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchMachine.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Copy-OrchMachine
---

# Copy-OrchMachine

## SYNOPSIS

Copies machines to another Orchestrator instance.

## SYNTAX

### __AllParameterSets

```
Copy-OrchMachine [-Path <string>] [-LiteralPath <string>] [-Name] <string[]> [-Destination] <string[]> [-Confirm]
 [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Copies machine definitions from one Orchestrator instance to another. This cmdlet is primarily used for cross-drive copy between different Orchestrator connections.

Machines with a Scope of 'PersonalWorkspace' are automatically excluded from the copy operation. Machines with a Scope of 'Cloud' cannot be copied and will generate a warning.

When machines have robot user assignments, the cmdlet maps robot users from the source to the destination by matching robot names (case-insensitive). If a matching robot is not found in the destination, an error is generated for that robot user.

If the source and destination are the same drive, the operation is silently skipped.

The -Name parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available machine names. Multiple values can be specified using comma-separated text that includes wildcards.

Note that -Path is a single string (not string[]), unlike most other cmdlets.

Machines are tenant-scoped entities. Use the -Path parameter to specify the source Orchestrator drive.

Primary Endpoint: GET /odata/Machines, POST /odata/Machines

OAuth required scopes: OR.Machines or OR.Machines.Read (source), OR.Machines (destination)

Required permissions: Machines.View (source), Machines.Create (destination)

## EXAMPLES

### Example 1: Copy all machines to another Orchestrator

```powershell
PS Orch1:\> Copy-OrchMachine * Orch2:\
```

Copies all machines from Orch1 to Orch2. PersonalWorkspace and Cloud machines are automatically excluded.

### Example 2: Copy specific machines

```powershell
PS Orch1:\> Copy-OrchMachine m* Orch2:\
```

Copies all machines matching 'm*' from Orch1 to Orch2.

### Example 3: Preview copy with -WhatIf

```powershell
PS Orch1:\> Copy-OrchMachine * Orch2:\ -WhatIf
```

Shows which machines would be copied without actually executing the copy.

### Example 4: Copy from a specific source drive

```powershell
PS C:\> Copy-OrchMachine -Path Orch1:\ pool Orch2:\
```

Copies the machine named 'pool' from Orch1 to Orch2. When -Path uses an absolute path, the command can be run from any location.

## PARAMETERS

### -Path

Specifies the source Orchestrator drive. If not specified, the current drive is used. Unlike most other cmdlets, this parameter accepts a single string (not an array).

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

### -LiteralPath

Specifies the target folder or drive by literal path -- wildcard metacharacters (`[`, `]`, `*`, `?`) are treated as literal characters rather than patterns. Accepts the same drive-qualified paths as -Path. Its `PSPath` alias also binds the path of items piped from Get-ChildItem / Get-Item, so you can pipe folders directly. Use -LiteralPath instead of -Path when a folder name contains a wildcard metacharacter.

```yaml
Type: System.String
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

### -Destination

Specifies the destination Orchestrator drives. Can be one or more drives for copying machines to multiple destinations.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: false
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

Specifies the names of the machines to copy. Supports wildcards. Tab completion dynamically suggests machine names from the source drive.

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

You can pipe machine names to this cmdlet via the Name property.

### System.String

You can pipe the source path via the Path property.

## OUTPUTS

### UiPath.PowerShell.Entities.CreatedMachine

Returns CreatedMachine objects for newly created machines at the destination.

## NOTES

Machines are tenant-scoped entities. The -Path parameter specifies the source Orchestrator drive (not a folder path).

The -Path parameter is a single string, not a string array. This differs from most other cmdlets in the module.

PersonalWorkspace machines are automatically excluded from the copy operation. Cloud machines generate a warning and are skipped.

Robot user assignments are mapped by matching robot names (case-insensitive) between source and destination. Ensure that corresponding robots exist in the destination before copying machines with robot user assignments.

## RELATED LINKS

[Get-OrchMachine](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchMachine.md)

[New-OrchMachine](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/New-OrchMachine.md)

[Update-OrchMachine](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Update-OrchMachine.md)

[Remove-OrchMachine](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchMachine.md)
