---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmUserLicense.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-PmUserLicense
---

# Get-PmUserLicense

## SYNOPSIS

Gets licensed users from a UiPath Automation Cloud organization.

## SYNTAX

### __AllParameterSets

```
Get-PmUserLicense [-Path <string[]>] [[-Name] <string[]>] [[-Email] <string[]>]
 [-CsvEncoding <Encoding>] [-ExportCsv <string>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets user license information from a UiPath Automation Cloud organization at the platform management level. This cmdlet retrieves the list of users who have been allocated licenses (named user licenses).

The -Name and -Email parameters can be used independently or together to filter results. Both support wildcards. Tab completion for -Name suggests user names, filtering by the currently specified -Email. Tab completion for -Email suggests user email addresses, filtering by the currently specified -Name.

When -ExportCsv is specified, the output is written to a CSV file with Path, UserName, and License columns — one row per (user, license) — instead of to the pipeline. That CSV round-trips back into Add-PmUserLicense to reassign the licenses. Orphaned license rows (licenses not tied to a directory user) are omitted because they cannot be reassigned on import.

Primary Endpoint: GET /portal_/api/license/accountant/UserLicense/user/page (License Accountant API)

OAuth required scopes: (License Accountant API - no per-endpoint scopes)

Required permissions: (managed by Identity Server)

## EXAMPLES

### Example 1: Get all licensed users

```powershell
PS Orch1:\> Get-PmUserLicense
```

Gets all licensed users in the current organization.

### Example 2: Get a licensed user by name

```powershell
PS Orch1:\> Get-PmUserLicense ytsuda@gmail.com
```

Gets the licensed user with the specified name. Because -Name is a positional parameter (position 0), the parameter name can be omitted.

### Example 3: Filter by email

```powershell
PS Orch1:\> Get-PmUserLicense -Email *@gmail.com
```

Gets all licensed users whose email addresses end with "@gmail.com".

### Example 4: Filter by both name and email

```powershell
PS Orch1:\> Get-PmUserLicense -Name ytsuda* -Email *@gmail.com
```

Gets licensed users whose names start with "ytsuda" and whose email addresses end with "@gmail.com".

### Example 5: Export licenses to CSV and re-import

```powershell
PS Orch1:\> Get-PmUserLicense -ExportCsv C:\temp\license-users.csv
PS Orch1:\> Import-Csv C:\temp\license-users.csv | Add-PmUserLicense
```

Exports one row per (user, license) with Path, UserName, and License columns, then re-imports them. The UserName column binds to Add-PmUserLicense's -Email parameter via its -UserName alias. The round trip is additive: editing the CSV and re-importing adds licenses but never revokes the ones left out.

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

### -Email

Specifies the email addresses to filter by. Supports wildcards. Tab completion dynamically suggests user email addresses, filtered by the current -Name selection.

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

### -ExportCsv

Exports the licensed users to the specified CSV file path. The CSV has Path, UserName, and License columns — one row per (user, license) — and round-trips into Add-PmUserLicense (the UserName column binds to that cmdlet's -Email parameter through its -UserName alias). Orphaned license rows (not tied to a directory user) are omitted. When this parameter is specified, no objects are written to the pipeline.

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

### -Name

Specifies the names of licensed users to retrieve. Supports wildcards. Tab completion dynamically suggests user names, filtered by the current -Email selection.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: false
Aliases: []
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

You can pipe names or email addresses to this cmdlet via the Name or Email properties.

## OUTPUTS

### UiPath.PowerShell.Entities.NuLicensedUser

Returns NuLicensedUser objects with properties including name and email.

## NOTES

This cmdlet retrieves the named user license list from the organization's license management API.

The License Accountant API returns an empty email field; the user's login is carried in the name field, which is why the CSV identifier column is UserName (it re-matches the user on import). Orphaned license rows are excluded from -ExportCsv because they have no user to reassign to.

## RELATED LINKS

[Get-PmGroupLicense](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmGroupLicense.md)

[Add-PmUserLicense](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Add-PmUserLicense.md)

[Remove-PmUserLicense](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-PmUserLicense.md)

[CSV Export & Import Guide](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/03-CsvExportImport.md)
