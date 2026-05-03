---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-PmLicensedGroup.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Remove-PmLicensedGroup
---

# Remove-PmLicensedGroup

## SYNOPSIS

Removes a licensed group from a UiPath Automation Cloud organization.

## SYNTAX

### __AllParameterSets

```
Remove-PmLicensedGroup [[-GroupName] <string[]>] [-Path <string[]>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Removes one or more licensed groups from a UiPath Automation Cloud organization at the platform management level. This removes the license group configuration, deallocating all user bundle licenses and user allocations associated with the group.

The -GroupName parameter supports wildcards and tab completion, enabling batch deletion of licensed groups matching a pattern.

Primary Endpoint: DELETE /api/license/accountant/UserLicense/group (License Accountant API)

OAuth required scopes: (License Accountant API - no per-endpoint scopes)

Required permissions: (managed by Identity Server)

## EXAMPLES

### Example 1: Remove a specific licensed group

```powershell
PS Orch1:\> Remove-PmLicensedGroup Developers
```

Removes the "Developers" licensed group. Because -GroupName is a positional parameter (position 0), the parameter name can be omitted.

### Example 2: Remove all licensed groups matching a pattern

```powershell
PS Orch1:\> Remove-PmLicensedGroup Test*
```

Removes all licensed groups whose names start with "Test".

### Example 3: Preview licensed group removal

```powershell
PS Orch1:\> Remove-PmLicensedGroup * -WhatIf
```

Shows which licensed groups would be removed without actually deleting them.

## PARAMETERS

### -Path

Specifies the target Pm: drives (organizations). If not specified, the current drive is targeted. Tab completion suggests available drive names.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: false
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

### -GroupName

Specifies the name(s) of the licensed group(s) to remove. Supports wildcards for pattern matching. Tab completion dynamically suggests licensed group names. This parameter has the alias "Name".

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: true
Aliases:
- Name
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

### -WhatIf

Runs the command in a mode that only reports what would happen without performing the actions.

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe group names to this cmdlet via the GroupName property.

## OUTPUTS

### None

This cmdlet does not produce output. The deletion is performed as a side effect.

## NOTES

Removing a licensed group deallocates all user bundle licenses and user allocations associated with the group. The licensed group cache is cleared after deletion.

## RELATED LINKS

Get-PmLicensedGroup

Add-PmLicenseToPmLicensedGroup

Remove-PmLicenseFromPmLicensedGroup
