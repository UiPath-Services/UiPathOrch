---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmGroupLicense.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-PmGroupLicense
---

# Get-PmGroupLicense

## SYNOPSIS

Gets licensed groups from a UiPath Automation Cloud organization.

## SYNTAX

### ExpandAllocation

```
Get-PmGroupLicense [-Path <string[]>] [-LiteralPath <string[]>] [[-GroupName] <string[]>] [[-UserName] <string[]>]
 [-CsvEncoding <Encoding>] [-ExpandAllocation] [-ExportCsv <string>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets license group information from a UiPath Automation Cloud organization at the platform management level. Licensed groups are groups that have been assigned user bundle licenses and have user allocations.

By default, this cmdlet returns NuLicensedGroup objects representing the groups themselves, including their assigned license bundle codes. When -ExpandAllocation is specified, the cmdlet expands each group to show its individual user allocations as NuLicensedGroupMember objects.

The -GroupName parameter supports wildcards and tab completion. The -UserName parameter (used with -ExpandAllocation) filters the expanded user allocations and also supports tab completion.

When -ExportCsv is specified, the output is written to a CSV file with Path, GroupName, and License columns — one row per (group, license) — instead of to the pipeline. That CSV round-trips back into Add-PmGroupLicense to reassign the bundles (the License column is the friendly bundle name that -License accepts). This is the group-level bundle assignment, not the per-user allocation list that -ExpandAllocation returns.

Primary Endpoint: GET /api/license/accountant/UserLicense/group/page (License Accountant API)

OAuth required scopes: (License Accountant API - no per-endpoint scopes)

Required permissions: (managed by Identity Server)

## EXAMPLES

### Example 1: Get all licensed groups

```powershell
PS Orch1:\> Get-PmGroupLicense
```

Gets all licensed groups in the current organization, showing group-level information.

### Example 2: Get a specific licensed group

```powershell
PS Orch1:\> Get-PmGroupLicense Everyone
```

Gets the "Everyone" licensed group. Because -GroupName is a positional parameter (position 0), the parameter name can be omitted.

### Example 3: Expand group allocations

```powershell
PS Orch1:\> Get-PmGroupLicense -ExpandAllocation
```

Gets all licensed groups and expands them to show individual user allocations.

### Example 4: Get allocations for a specific group and user

```powershell
PS Orch1:\> Get-PmGroupLicense Everyone -UserName *yoshifumi* -ExpandAllocation
```

Gets allocations for users whose names contain "yoshifumi" in the "Everyone" licensed group.

### Example 5: Export group bundles to CSV and re-import

```powershell
PS Orch1:\> Get-PmGroupLicense -ExportCsv C:\temp\license-groups.csv
PS Orch1:\> Import-Csv C:\temp\license-groups.csv | Add-PmGroupLicense
```

Exports one row per (group, license) with Path, GroupName, and License columns, then re-imports them. The round trip is additive: editing the CSV and re-importing adds bundles but never revokes the ones left out.

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

### -CsvEncoding

Specifies the encoding for CSV export. Default is UTF-8 with BOM for Excel compatibility. Tab completion suggests all available system encodings (e.g., utf-8, shift_jis, us-ascii).

```yaml
Type: System.Text.Encoding
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

### -ExpandAllocation

Expands licensed groups to show individual user allocations. When specified, NuLicensedGroupMember objects are returned instead of NuLicensedGroup objects.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: ExpandAllocation
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -ExportCsv

Exports the licensed groups' bundle assignments to the specified CSV file path. The CSV has Path, GroupName, and License columns — one row per (group, license) — and round-trips into Add-PmGroupLicense (the License column is the friendly bundle name that cmdlet's -License accepts). This is the group-level bundle list, not the per-user allocations returned by -ExpandAllocation. When this parameter is specified, no objects are written to the pipeline.

```yaml
Type: System.String
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

### -GroupName

Specifies the names of licensed groups to retrieve. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests licensed group names. This parameter has the alias "Name".

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

### -UserName

Specifies the names of users to filter when expanding allocations. Supports wildcards. Tab completion dynamically suggests user names within the selected licensed group(s).

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 1
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

You can pipe group names to this cmdlet via the GroupName property.

## OUTPUTS

### UiPath.PowerShell.Entities.NuLicensedGroup

Returns licensed group objects when -ExpandAllocation is not specified.

### UiPath.PowerShell.Entities.NuLicensedGroupMember

Returns licensed group member (allocation) objects when -ExpandAllocation is specified.

## NOTES

The -UserName filter only takes effect when -ExpandAllocation is specified. Without expansion, only group-level objects are returned.

-ExportCsv writes the group-level bundle list (Path, GroupName, License), which round-trips into Add-PmGroupLicense. To remove bundles from a CSV instead, pipe the same shape into Remove-PmGroupLicense.

## RELATED LINKS

[Get-PmUserLicense](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmUserLicense.md)

[Remove-PmLicensedGroup](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-PmLicensedGroup.md)

[Add-PmGroupLicense](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Add-PmGroupLicense.md)

[Remove-PmGroupLicense](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-PmGroupLicense.md)

[Remove-PmGroupLicenseAllocation](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-PmGroupLicenseAllocation.md)

[CSV Export & Import Guide](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/05-CsvExportImport.md)
