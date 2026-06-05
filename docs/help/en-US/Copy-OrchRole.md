---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchRole.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Copy-OrchRole
---

# Copy-OrchRole

## SYNOPSIS

Copies roles to another Orchestrator tenant.

## SYNTAX

### __AllParameterSets

```
Copy-OrchRole [-Path <string>] [-LiteralPath <string>] [-Name] <string[]> [-Destination] <string[]> [-Confirm]
 [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Copies roles from one Orchestrator tenant to another. The cmdlet reads roles from the source drive and creates them on the destination drives with the same name, type, and permissions.

The -Name parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available role names. Multiple values can be specified using comma-separated text that includes wildcards.

If a static (built-in) role with the same name already exists on the destination drive, it is skipped. If the source and destination drives are the same, the operation is skipped.

Roles are tenant-scoped entities. Use the -Path parameter to specify the source Orchestrator drive.

Primary Endpoint: POST /odata/Roles

OAuth required scopes: OR.Users or OR.Users.Write

Required permissions: Roles.View (source) and Roles.Create (destination)

## EXAMPLES

### Example 1: Copy all roles to another tenant

```powershell
PS Orch1:\> Copy-OrchRole * Orch2:\
```

Copies all roles from Orch1 to Orch2.

### Example 2: Copy specific roles

```powershell
PS Orch1:\> Copy-OrchRole Custom* Orch2:\
```

Copies roles matching 'Custom*' from Orch1 to Orch2.

### Example 3: Copy roles from a specific source

```powershell
PS C:\> Copy-OrchRole -Path Orch1:\ * Orch2:\,Orch3:\
```

Copies all roles from Orch1 to both Orch2 and Orch3.

### Example 4: Preview copy with WhatIf

```powershell
PS Orch1:\> Copy-OrchRole * Orch2:\ -WhatIf
```

Shows what roles would be copied without actually performing the operation.

## PARAMETERS

### -Path

Specifies the source Orchestrator drive. If not specified, the current drive is used as the source.

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

Specifies the destination Orchestrator drives to copy roles to. Multiple drives can be specified.

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

Specifies the names of the roles to copy. Supports wildcards. Tab completion dynamically suggests role names from the source drive.

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

You can pipe role names to this cmdlet via the Name property.

### System.String

You can pipe the source Path to this cmdlet.

## OUTPUTS

### None

This cmdlet does not produce output.

## NOTES

Roles are tenant-scoped entities. The -Path and -Destination parameters specify Orchestrator drives (not folder paths).

If a static (built-in) role with the same name already exists on the destination, it is silently skipped.

If the source and destination are the same drive, the operation is silently skipped.

## RELATED LINKS

[Get-OrchRole](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchRole.md)

[Set-OrchRole](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Set-OrchRole.md)

[Remove-OrchRole](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchRole.md)
