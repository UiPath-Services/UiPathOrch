---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Remove-PmLicenseFromPmLicensedGroup
---

# Remove-PmLicenseFromPmLicensedGroup

## SYNOPSIS

Removes a user bundle license from a licensed group in a UiPath Automation Cloud organization.

## SYNTAX

### __AllParameterSets

```
Remove-PmLicenseFromPmLicensedGroup [-GroupName] <string[]> [-License] <string[]> [-Path <string[]>]
 [-WhatIf] [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Removes one or more user bundle licenses from a licensed group at the organization (platform management) level. This cmdlet accepts pipeline input, collecting all removals during ProcessRecord and applying them in bulk during EndProcessing.

The -GroupName parameter supports wildcards and tab completion for existing licensed groups. The -License parameter supports wildcards and tab completion that dynamically shows licenses currently assigned to the selected group(s), including availability information as tooltips.

If a license is not currently assigned to the group, the removal is skipped silently.

Primary Endpoint: PUT /api/license/accountant/UserLicense/group (License Accountant API)

OAuth required scopes: (License Accountant API - no per-endpoint scopes)

Required permissions: (managed by Identity Server)

## EXAMPLES

### Example 1: Remove a license from a group

```powershell
PS Orch1:\> Remove-PmLicenseFromPmLicensedGroup Developers Unattended
```

Removes the "Unattended" license bundle from the "Developers" licensed group. Because -GroupName and -License are positional parameters (positions 0 and 1), parameter names can be omitted.

### Example 2: Remove all licenses from a group

```powershell
PS Orch1:\> Remove-PmLicenseFromPmLicensedGroup Developers *
```

Removes all license bundles from the "Developers" licensed group.

### Example 3: Preview license removal

```powershell
PS Orch1:\> Remove-PmLicenseFromPmLicensedGroup Developers "Citizen Developer" -WhatIf
```

Shows what would happen without actually removing the license.

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

Specifies the name(s) of the licensed group(s) to remove licenses from. Supports wildcards. Tab completion dynamically suggests licensed group names. This parameter has the alias "Name".

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: true
Aliases:
- Name
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

### -License

Specifies the license bundle name(s) to remove. Supports wildcards. Tab completion dynamically suggests licenses currently assigned to the selected group(s), with availability information as tooltips.

```yaml
Type: System.String[]
DefaultValue: ''
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

You can pipe objects with GroupName, License, and Path properties to this cmdlet.

## OUTPUTS

### UiPath.PowerShell.Entities.UpdateLicensedGroupResponse

Returns the updated licensed group response, including the remaining list of assigned user bundle license codes and their human-readable names.

## NOTES

All pipeline input is collected first, then API calls are made during EndProcessing. If a license is not currently assigned to the group, the removal is skipped.

License names are resolved from internal bundle codes to human-readable names.

## RELATED LINKS

Add-PmLicenseToPmLicensedGroup

Get-PmLicensedGroup

Remove-PmLicensedGroup
