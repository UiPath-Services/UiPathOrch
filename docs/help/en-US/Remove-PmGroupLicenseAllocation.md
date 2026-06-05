---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-PmGroupLicenseAllocation.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Remove-PmGroupLicenseAllocation
---

# Remove-PmGroupLicenseAllocation

## SYNOPSIS

Removes a user allocation from a licensed group in a UiPath Automation Cloud organization.

## SYNTAX

### __AllParameterSets

```
Remove-PmGroupLicenseAllocation [-Path <string[]>] [-LiteralPath <string[]>] [-GroupName] <string[]>
 [-UserName] <string[]> [-Confirm] [-NoMatchWarning] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Removes one or more user allocations from a licensed group at the organization (platform management) level. This deallocates the named user license from the specified user(s) within the group.

The -GroupName and -UserName parameters both support wildcards, enabling flexible batch operations. The cmdlet supports Ctrl+C cancellation during batch operations.

The -NoMatchWarning switch enables warnings when the specified -GroupName or -UserName pattern does not match any existing entries. This is useful during CSV import to identify rows that do not correspond to actual allocations.

This cmdlet accepts pipeline input, allowing bulk allocation removal from a CSV file exported by Get-PmGroupLicense -ExportCsv.

Primary Endpoint: DELETE /api/license/accountant/UserLicense/group/{groupId}/user/{userId} (License Accountant API)

OAuth required scopes: (License Accountant API - no per-endpoint scopes)

Required permissions: (managed by Identity Server)

## EXAMPLES

### Example 1: Remove a user allocation from a group

```powershell
PS Orch1:\> Remove-PmGroupLicenseAllocation "Automation Developers" ytsuda@gmail.com
```

Removes the allocation for ytsuda@gmail.com from the "Automation Developers" licensed group. Because -GroupName and -UserName are positional parameters (positions 0 and 1), parameter names can be omitted.

### Example 2: Remove all allocations from a group

```powershell
PS Orch1:\> Remove-PmGroupLicenseAllocation "Automation Developers" *
```

Removes all user allocations from the "Automation Developers" licensed group.

### Example 3: Bulk remove allocations from CSV

```powershell
PS Orch1:\> Import-Csv C:\temp\allocations.csv | Remove-PmGroupLicenseAllocation
```

Removes allocations listed in the CSV file. The CSV should contain columns: GroupName, UserName, and optionally Path.

### Example 4: Remove with no-match warning

```powershell
PS Orch1:\> Import-Csv C:\temp\allocations.csv | Remove-PmGroupLicenseAllocation -NoMatchWarning
```

Removes allocations and displays warnings for any CSV rows where the GroupName or UserName does not match.

### Example 5: Preview allocation removal

```powershell
PS Orch1:\> Remove-PmGroupLicenseAllocation "Automation Developers" * -WhatIf
```

Shows which allocations would be removed without actually removing them.

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

Specifies the name(s) of the licensed group(s) to remove allocations from. Supports wildcards for pattern matching. Tab completion dynamically suggests licensed group names. This parameter has the alias "Name".

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

### -NoMatchWarning

When specified, displays a warning message if the GroupName or UserName pattern does not match any existing entries. Without this switch, unmatched patterns are silently ignored.

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

### -UserName

Specifies the name(s) of the user(s) whose allocations to remove. Supports wildcards for pattern matching. Tab completion dynamically suggests allocated user names within the selected licensed group(s).

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

You can pipe objects with GroupName, UserName, and Path properties to this cmdlet.

## OUTPUTS

### None

This cmdlet does not produce pipeline output. The allocation removal is performed as a side effect.

## NOTES

The cmdlet supports Ctrl+C cancellation during batch operations.

Each allocation is removed individually via the API. The allocation cache is cleared after each removal.

The CSV exported by Get-PmGroupLicense -ExportCsv can be directly piped to this cmdlet for bulk allocation removal.

## RELATED LINKS

[Get-PmGroupLicense](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmGroupLicense.md)

[Add-PmGroupLicense](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Add-PmGroupLicense.md)

[Get-PmUserLicense](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmUserLicense.md)
