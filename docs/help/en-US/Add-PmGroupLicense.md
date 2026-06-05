---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Add-PmGroupLicense.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Add-PmGroupLicense
---

# Add-PmGroupLicense

## SYNOPSIS

Adds a user bundle license to a licensed group in a UiPath Automation Cloud organization.

## SYNTAX

### __AllParameterSets

```
Add-PmGroupLicense [-Path <string[]>] [-LiteralPath <string[]>] [-GroupName] <string[]>
 [-License] <string[]> [-Confirm] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Adds one or more user bundle licenses to a licensed group at the organization (platform management) level. The group is resolved via directory search, so the -GroupName parameter requires entering at least one character to trigger the search-based tab completer.

The -License parameter supports tab completion that dynamically shows available license bundles for the selected group, including the number of remaining available licenses. Licenses already assigned to the group are excluded from the completion suggestions. Supports wildcards for matching multiple license types.

This cmdlet accepts pipeline input, collecting all additions during ProcessRecord and applying them in bulk during EndProcessing. If a license is already assigned to the group, it is not added again (duplicates are skipped).

Primary Endpoint: PUT /api/license/accountant/UserLicense/group (License Accountant API)

OAuth required scopes: (License Accountant API - no per-endpoint scopes)

Required permissions: (managed by Identity Server)

## EXAMPLES

### Example 1: Add a license to a group

```powershell
PS Orch1:\> Add-PmGroupLicense "Automation Developers" Unattended
```

Adds the "Unattended" license bundle to the "Automation Developers" licensed group. Because -GroupName and -License are positional parameters (positions 0 and 1), parameter names can be omitted.

### Example 2: Add multiple licenses using wildcards

```powershell
PS Orch1:\> Add-PmGroupLicense "Automation Developers" *Attended*
```

Adds all available license bundles matching "*Attended*" to the "Automation Developers" group.

### Example 3: Preview license addition

```powershell
PS Orch1:\> Add-PmGroupLicense "Automation Developers" "Citizen Developer" -WhatIf
```

Shows what would happen without actually adding the license.

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

### -LiteralPath

Specifies the target folder or drive by literal path -- wildcard metacharacters (`[`, `]`, `*`, `?`) are treated as literal characters rather than patterns. Accepts the same drive-qualified paths as -Path. Its `PSPath` alias also binds the path of items piped from Get-ChildItem / Get-Item, so you can pipe folders directly. Use -LiteralPath instead of -Path when a folder name contains a wildcard metacharacter.

```yaml
Type: System.String[]
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

### -GroupName

Specifies the name(s) of the group(s) to add licenses to. The group is resolved via directory search. Tab completion requires entering at least one character to search. Supports wildcards. This parameter has the alias "Name".

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

Specifies the license bundle name(s) to add. Supports wildcards. Tab completion dynamically suggests available license bundles for the selected group, showing the number of remaining available licenses as tooltips.

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

Returns the updated licensed group response, including the list of assigned user bundle license codes and their human-readable names.

## NOTES

All pipeline input is collected first, then API calls are made during EndProcessing. If a license is already assigned to the group, the operation is skipped for that license.

The group is resolved via directory search, accepting both DirectoryGroup and LocalGroup types.

License codes are internally mapped to human-readable names for both tab completion display and output.

## RELATED LINKS

[Remove-PmGroupLicense](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-PmGroupLicense.md)

[Get-PmGroupLicense](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmGroupLicense.md)

[Remove-PmLicensedGroup](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-PmLicensedGroup.md)
