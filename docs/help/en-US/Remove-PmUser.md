---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-PmUser.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Remove-PmUser
---

# Remove-PmUser

## SYNOPSIS

Removes a platform management user from a UiPath Automation Cloud organization.

## SYNTAX

### __AllParameterSets

```
Remove-PmUser [-Path <string[]>] [-LiteralPath <string[]>] [-Email] <string[]> [-Confirm] [-NoMatchWarning]
 [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Removes one or more users at the organization (platform management) level from a UiPath Automation Cloud organization. The cmdlet calls the identity service delete API to permanently remove the user.

The -Email parameter supports wildcards, allowing batch deletion of users matching a pattern. The currently logged-in user is automatically excluded from deletion for safety.

The cmdlet supports cancellation via Ctrl+C during batch operations.

The API endpoint used depends on the server version. The cmdlet automatically detects whether the newer partition-based API or the deprecated user-based API should be used, and falls back gracefully between them.

The -Email and -Path parameters support tab completion.

Primary Endpoint: DELETE /api/User/{userId} (Identity Server)

OAuth required scopes: (Identity Server API - no per-endpoint scopes)

Required permissions: (managed by Identity Server)

## EXAMPLES

### Example 1: Remove a specific user

```powershell
PS Orch1:\> Remove-PmUser ytsuda+c@gmail.com
```

Removes the specified user from the organization. Because -Email is a positional parameter (position 0), the parameter name can be omitted.

### Example 2: Remove multiple users with a wildcard

```powershell
PS Orch1:\> Remove-PmUser *+c@gmail.com
```

Removes all users whose email ends with "+c@gmail.com". The currently logged-in user is automatically excluded.

### Example 3: Remove users with confirmation prompt

```powershell
PS Orch1:\> Remove-PmUser ytsuda* -Confirm
```

Prompts for confirmation before removing each user matching the pattern.

### Example 4: Remove a user with no-match warning

```powershell
PS Orch1:\> Remove-PmUser nonexistent@example.com -NoMatchWarning
```

Attempts to remove the specified user and displays a warning if no matching user is found instead of silently completing.

### Example 5: Preview user removal without executing

```powershell
PS Orch1:\> Remove-PmUser ytsuda* -WhatIf
```

Shows which users would be removed without actually deleting them.

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

### -Email

Specifies the email address(es) of the user(s) to remove. Supports wildcards for pattern matching. Tab completion dynamically suggests user email addresses. This parameter has the alias "UserName", and a pattern matches a user by either its userName or its email -- so a userName that differs from the email, or a userName-only account without an email, still resolves (mirrors Get-PmUser / Update-PmUser / Copy-PmUser).

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: true
Aliases:
- UserName
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

When specified, displays a warning message if no users match the specified -Email pattern. Without this switch, the cmdlet completes silently when no match is found.

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

You can pipe email addresses to this cmdlet via the Email property.

## OUTPUTS

### None

This cmdlet does not produce output. The deletion is performed as a side effect.

## NOTES

The currently logged-in user is automatically excluded from deletion to prevent accidental self-removal.

The cmdlet supports Ctrl+C cancellation during batch operations.

The cmdlet automatically handles API version differences, falling back from the newer partition-based API to the deprecated user-based API as needed.

## RELATED LINKS

[Get-PmUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmUser.md)

[New-PmUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/New-PmUser.md)

[Copy-PmUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-PmUser.md)
