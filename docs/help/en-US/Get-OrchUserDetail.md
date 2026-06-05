---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchUserDetail.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 05/11/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchUserDetail
---

# Get-OrchUserDetail

## SYNOPSIS

Gets per-user detailed information from UiPath Orchestrator tenants.

## SYNTAX

### __AllParameterSets

```
Get-OrchUserDetail [-Path <string[]>] [-LiteralPath <string[]>] [-UserName] <string[]> [[-FullName] <string[]>]
 [-CsvEncoding <Encoding>] [-ExportCsv <string>] [-Type <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets the per-user detail payload (unattended robot settings, execution settings, update policy, role assignments) from UiPath Orchestrator tenants. The list endpoint backing `Get-OrchUser` returns shallow user records; this cmdlet calls the per-id detail endpoint for each matched user and emits the detailed `User`.

`-UserName` is mandatory by design — the detail path makes one API call per matched user, so a default "all users" would fan out unexpectedly on large tenants. Wildcards (including `*`) are accepted; the user just has to type the selector explicitly.

`-FullName` and `-Type` are optional additional filters that mirror `Get-OrchUser`'s filter set so users can filter detail-fetched users the same way they filter the shallow list.

The CSV format produced by `-ExportCsv` matches `Add-OrchUser`'s import shape, so an exported file can be re-imported via `Import-Csv | Add-OrchUser`.

The -UserName, -FullName, -Type, and -Path parameters support tab completion. -UserName / -FullName completion is dynamically populated from actual tenant users.

Primary Endpoint: GET /odata/Users({userId})

OAuth required scopes: OR.Users or OR.Users.Read

Required permissions: Users.View

## EXAMPLES

### Example 1: Detail for all users on the current drive

```powershell
PS Orch1:\> Get-OrchUserDetail -UserName '*'
```

Returns the detailed payload for every user on `Orch1:\`. The explicit `*` reminds the user that the operation fans out across every user; omitting `-UserName` would prompt instead of silently fetching everything.

### Example 2: Detail for a specific user

```powershell
PS Orch1:\> Get-OrchUserDetail yoshifumi.tsuda@uipath.com
```

Returns the detailed payload for the named user. `-UserName` is positional (Position 0), so the parameter name can be omitted.

### Example 3: Detail for robots only

```powershell
PS Orch1:\> Get-OrchUserDetail -UserName '*' -Type DirectoryRobot
```

Combines the mandatory selector with a `-Type` filter to fetch detail only for robot accounts.

### Example 4: Export to CSV (round-trips with Add-OrchUser)

```powershell
PS C:\> Get-OrchUserDetail -Path Orch1: -UserName '*' -ExportCsv C:\temp
```

Exports the detailed payload of every user on `Orch1:` to `C:\temp\ExportedUsers.csv`. The CSV columns match `Add-OrchUser`'s import shape — the file can be piped via `Import-Csv | Add-OrchUser`.

## PARAMETERS

### -UserName

Specifies the user names to retrieve detail for. Mandatory by design — see DESCRIPTION. Wildcards (including `*`) are accepted.

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

### -FullName

Optional additional filter on user full name. Wildcards accepted.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
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

### -Type

Optional additional filter on user type. Wildcards accepted. Valid values: DirectoryUser, DirectoryGroup, DirectoryRobot, DirectoryExternalApplication.

```yaml
Type: System.String[]
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

### -Path

Specifies the target Orch: drives. If not specified, the current drive is targeted.

```yaml
Type: System.String[]
DefaultValue: None
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

### -ExportCsv

Specifies the directory or file path to export user details as a CSV file. If a directory path is specified, the default file name `ExportedUsers.csv` is used. The CSV format is round-trip compatible with `Add-OrchUser`.

```yaml
Type: System.String
DefaultValue: None
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

### -CsvEncoding

Specifies the encoding for CSV export. Default is UTF-8 with BOM for Excel compatibility. Tab completion suggests all available system encodings (e.g., utf-8, shift_jis, us-ascii).

```yaml
Type: System.Text.Encoding
DefaultValue: None
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

You can pipe user names to this cmdlet via the UserName property, or drive names via the Path property.

## OUTPUTS

### UiPath.PowerShell.Entities.User

Returns the detailed User entity (one per matched user).

## NOTES

Users are tenant-scoped entities. The -Path parameter targets drives (Orchestrator instances), not folders.

Each matched user triggers one detail-endpoint call. For large tenants, prefer narrow `-UserName` wildcards over `*` to limit fan-out.

## RELATED LINKS

[Get-OrchUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchUser.md)

[Add-OrchUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Add-OrchUser.md)

[Update-OrchUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Update-OrchUser.md)

[Remove-OrchUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchUser.md)
