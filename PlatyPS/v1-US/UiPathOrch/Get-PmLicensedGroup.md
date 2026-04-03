---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-PmLicensedGroup
---

# Get-PmLicensedGroup

## SYNOPSIS

Gets licensed groups from a UiPath Automation Cloud organization.

## SYNTAX

### ExpandAllocation

```
Get-PmLicensedGroup [[-GroupName] <string[]>] [[-UserName] <string[]>] [-ExpandAllocation]
 [-Path <string[]>] [-ExportCsv <string>] [-CsvEncoding <Encoding>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets license group information from a UiPath Automation Cloud organization at the platform management level. Licensed groups are groups that have been assigned user bundle licenses and have user allocations.

By default, this cmdlet returns NuLicensedGroup objects representing the groups themselves, including their assigned license bundle codes. When -ExpandAllocation is specified or -ExportCsv is used, the cmdlet expands each group to show its individual user allocations as NuLicensedGroupMember objects.

The -GroupName parameter supports wildcards and tab completion. The -UserName parameter (used with -ExpandAllocation or -ExportCsv) filters the expanded user allocations and also supports tab completion.

When -ExportCsv is specified, the output is written to a CSV file with columns: Path, GroupName, UserName, DisplayName, Email, LastInUse. This CSV can be used with Remove-PmAllocationFromPmLicensedGroup for bulk allocation removal.

Primary Endpoint: GET /api/license/accountant/UserLicense/group/page (License Accountant API)

OAuth required scopes: (License Accountant API - no per-endpoint scopes)

Required permissions: (managed by Identity Server)

## EXAMPLES

### Example 1: Get all licensed groups

```powershell
PS Orch1:\> Get-PmLicensedGroup
```

Gets all licensed groups in the current organization, showing group-level information.

### Example 2: Get a specific licensed group

```powershell
PS Orch1:\> Get-PmLicensedGroup Everyone
```

Gets the "Everyone" licensed group. Because -GroupName is a positional parameter (position 0), the parameter name can be omitted.

### Example 3: Expand group allocations

```powershell
PS Orch1:\> Get-PmLicensedGroup -ExpandAllocation
```

Gets all licensed groups and expands them to show individual user allocations.

### Example 4: Get allocations for a specific group and user

```powershell
PS Orch1:\> Get-PmLicensedGroup Everyone -UserName *yoshifumi* -ExpandAllocation
```

Gets allocations for users whose names contain "yoshifumi" in the "Everyone" licensed group.

### Example 5: Export allocations to CSV

```powershell
PS Orch1:\> Get-PmLicensedGroup -ExportCsv C:\temp\allocations.csv
```

Exports all licensed group allocations to a CSV file with Path, GroupName, UserName, DisplayName, Email, and LastInUse columns.

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

Exports licensed group allocations to the specified CSV file path. The CSV includes Path, GroupName, UserName, DisplayName, Email, and LastInUse columns. When this parameter is specified, allocations are automatically expanded (similar to -ExpandAllocation) and no objects are written to the pipeline.

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

Returns licensed group member (allocation) objects when -ExpandAllocation is specified or -ExportCsv is used.

## NOTES

The -UserName filter only takes effect when -ExpandAllocation is specified or -ExportCsv is used. Without expansion, only group-level objects are returned.

The exported CSV can be used with Remove-PmAllocationFromPmLicensedGroup for bulk allocation removal.

## RELATED LINKS

Get-PmLicensedUser

Remove-PmLicensedGroup

Add-PmLicenseToPmLicensedGroup

Remove-PmAllocationFromPmLicensedGroup
