---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmGroup.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-PmGroup
---

# Get-PmGroup

## SYNOPSIS

Gets platform management groups from UiPath Automation Cloud organizations.

## SYNTAX

### __AllParameterSets

```
Get-PmGroup [-Path <string[]>] [-LiteralPath <string[]>] [[-GroupName] <string[]>] [-CsvEncoding <Encoding>]
 [-ExportCsv <string>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets group information from UiPath Automation Cloud at the organization (platform management) level. Groups are used to organize users and assign shared permissions across the organization.

This cmdlet retrieves groups from the identity service API and returns PmGroup objects containing properties such as name and id.

The -GroupName parameter supports wildcards and tab completion. Press [Ctrl+Space] or [Tab] to see available group names. Multiple values can be specified, and wildcard patterns are supported for flexible filtering.

When -ExportCsv is specified, the output is written to a CSV file instead of the pipeline. The CSV includes columns: Path and GroupName, sorted by group name.

The -Path parameter specifies the target Pm: drives (organizations). If -Path is omitted, the current drive is targeted.

Primary Endpoint: GET /api/Group/{partitionGlobalId} (Identity Server)

OAuth required scopes: (Identity Server API - no per-endpoint scopes)

Required permissions: (managed by Identity Server)

## EXAMPLES

### Example 1: Get all groups

```powershell
PS Orch1:\> Get-PmGroup
```

Gets all platform management groups from the current organization.

### Example 2: Get a specific group by name

```powershell
PS Orch1:\> Get-PmGroup Administrators
```

Gets the group named "Administrators". Because -GroupName is a positional parameter (position 0), the parameter name can be omitted.

### Example 3: Get groups matching a wildcard pattern

```powershell
PS Orch1:\> Get-PmGroup Auto*
```

Gets all groups whose names start with "Auto".

### Example 4: Export groups to CSV

```powershell
PS Orch1:\> Get-PmGroup -ExportCsv C:\temp\groups.csv
```

Exports all groups to a CSV file with Path and GroupName columns.

### Example 5: Get groups from a specific organization

```powershell
PS C:\> Get-PmGroup -Path Orch1: -GroupName *Admin*
```

Gets groups containing "Admin" in their name from the Orch1 organization drive.

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

### -ExportCsv

Exports groups to the specified CSV file path. The CSV includes Path and GroupName columns. When this parameter is specified, no objects are written to the pipeline.

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

Specifies the names of groups to retrieve. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests group names from the target organizations. This parameter has the alias "Name".

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe group names to this cmdlet via the GroupName property.

## OUTPUTS

### UiPath.PowerShell.Entities.PmGroup

Returns PmGroup objects with properties including name and id.

## NOTES

Platform management groups are organization-level entities. They are used to organize users and assign shared permissions.

The -ExportCsv parameter generates a CSV file that can be used as input for New-PmGroup to replicate groups to another organization.

## RELATED LINKS

[New-PmGroup](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/New-PmGroup.md)

[Copy-PmGroup](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-PmGroup.md)

[Remove-PmGroup](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-PmGroup.md)

[Get-PmGroupMember](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmGroupMember.md)
