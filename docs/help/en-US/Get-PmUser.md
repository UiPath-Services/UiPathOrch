---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmUser.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-PmUser
---

# Get-PmUser

## SYNOPSIS

Gets platform management users from UiPath Automation Cloud organizations.

## SYNTAX

### __AllParameterSets

```
Get-PmUser [-Path <string[]>] [-LiteralPath <string[]>] [[-Email] <string[]>] [-CsvEncoding <Encoding>]
 [-ExportCsv <string>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets user information from UiPath Automation Cloud at the organization (platform management) level. Users at this level are organization members who can be assigned to groups, tenants, and services.

This cmdlet retrieves users from the identity service API and returns PmUser objects containing properties such as email, userName, name, surname, displayName, bypassBasicAuthRestriction, invitationAccepted, and groupIDs.

The -Email parameter supports wildcards and tab completion. Press [Ctrl+Space] or [Tab] to see available user email addresses. Multiple values can be specified, and wildcard patterns are supported for flexible filtering.

When -ExportCsv is specified, the output is written to a CSV file instead of the pipeline. The CSV includes columns: Path, Email, Name, SurName, DisplayName, Type, BypassBasicAuthRestriction, InvitationAccepted, GroupName. Group IDs are resolved to human-readable group names in the export.

When multiple Pm: drives are connected, specifying -Path targets specific organizations. If -Path is omitted, the current drive is targeted.

Primary Endpoint: GET /api/User/users/{partitionGlobalId} (Identity Server)

OAuth required scopes: (Identity Server API - no per-endpoint scopes)

Required permissions: (managed by Identity Server)

## EXAMPLES

### Example 1: Get all users in the current organization

```powershell
PS Orch1:\> Get-PmUser
```

Gets all platform management users from the current organization.

### Example 2: Get a specific user by email

```powershell
PS Orch1:\> Get-PmUser ytsuda@gmail.com
```

Gets the user with the specified email address. Because -Email is a positional parameter (position 0), the parameter name can be omitted.

### Example 3: Get users matching a wildcard pattern

```powershell
PS Orch1:\> Get-PmUser *@example.com
```

Gets all users whose email addresses end with "@example.com".

### Example 4: Export users to CSV

```powershell
PS Orch1:\> Get-PmUser -ExportCsv C:\temp\users.csv
```

Exports all users to a CSV file. Group IDs are automatically resolved to group names. The exported CSV can be used with New-PmUser for bulk import.

### Example 5: Get users from a specific organization

```powershell
PS C:\> Get-PmUser -Path Orch1: -Email ytsuda*
```

Gets users whose email starts with "ytsuda" from the Orch1 organization drive.

### Example 6: Export users with Shift_JIS encoding

```powershell
PS Orch1:\> Get-PmUser -ExportCsv C:\temp\users.csv -CsvEncoding shift_jis
```

Exports all users to a CSV file using Shift_JIS encoding for compatibility with Japanese editions of Excel.

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

### -Email

Specifies the email addresses of users to retrieve. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests user email addresses from the target organizations. This parameter has the alias "UserName".

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: true
Aliases:
- UserName
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

### -ExportCsv

Exports users to the specified CSV file path. The CSV includes Path, Email, Name, SurName, DisplayName, Type, BypassBasicAuthRestriction, InvitationAccepted, and GroupName columns. Group IDs are resolved to human-readable group names. When this parameter is specified, no objects are written to the pipeline.

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe email addresses to this cmdlet via the Email property.

## OUTPUTS

### UiPath.PowerShell.Entities.PmUser

Returns PmUser objects with properties including email, userName, name, surname, displayName, bypassBasicAuthRestriction, invitationAccepted, and groupIDs.

## NOTES

Platform management users are organization-level entities. They are distinct from Orchestrator-level users and are managed through the UiPath identity service API.

The -ExportCsv parameter generates a CSV file that can be used as input for New-PmUser to replicate users to another organization.

## RELATED LINKS

[New-PmUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/New-PmUser.md)

[Update-PmUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Update-PmUser.md)

[Copy-PmUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-PmUser.md)

[Remove-PmUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-PmUser.md)
