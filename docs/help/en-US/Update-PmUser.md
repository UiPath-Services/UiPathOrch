---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Update-PmUser.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Update-PmUser
---

# Update-PmUser

## SYNOPSIS

Updates properties of a platform management user in a UiPath Automation Cloud organization.

## SYNTAX

### __AllParameterSets

```
Update-PmUser [-Path <string[]>] [-LiteralPath <string[]>] [[-Email] <string[]>]
 [-BypassBasicAuthRestriction <string>] [-Confirm] [-DisplayName <string>]
 [-Name <string>] [-Password <string>] [-Surname <string>] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Updates the properties of an existing user at the organization (platform management) level. The cmdlet retrieves the current user properties, applies the specified changes, and calls the update API only if at least one property has actually changed.

The -Email parameter identifies the user(s) to update and supports wildcards, allowing batch updates. The email address itself cannot be changed through this cmdlet.

Updatable properties include Name, DisplayName, Surname, Password, and BypassBasicAuthRestriction. Group membership is not modified by this cmdlet; use Add-PmGroupMember or Remove-PmGroupMember for group changes.

The -Email, -BypassBasicAuthRestriction, and -Path parameters support tab completion.

Primary Endpoint: PUT /api/User/{userId} (Identity Server)

OAuth required scopes: (Identity Server API - no per-endpoint scopes)

Required permissions: (managed by Identity Server)

## EXAMPLES

### Example 1: Update a user's display name

```powershell
PS Orch1:\> Update-PmUser ytsuda@gmail.com -DisplayName "Yoshifumi Tsuda"
```

Updates the display name for the specified user. Because -Email is a positional parameter (position 0), the parameter name can be omitted.

### Example 2: Update multiple users with a wildcard

```powershell
PS Orch1:\> Update-PmUser *@gmail.com -BypassBasicAuthRestriction true
```

Sets BypassBasicAuthRestriction to true for all users whose email ends with "@gmail.com".

### Example 3: Update a user's password

```powershell
PS Orch1:\> Update-PmUser ytsuda@gmail.com -Password "NewSecurePassword123!"
```

Updates the password for the specified user.

### Example 4: Preview changes without applying

```powershell
PS Orch1:\> Update-PmUser *@gmail.com -Name Updated -WhatIf
```

Shows which users would be updated without actually making any changes.

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

### -BypassBasicAuthRestriction

Specifies whether the user can bypass basic authentication restrictions. Tab completion suggests "true" and "false".

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -DisplayName

Specifies the new display name for the user.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Email

Specifies the email address(es) of the user(s) to update. Supports wildcards for batch updates. Tab completion dynamically suggests user email addresses. This parameter has the alias "UserName", and a pattern matches a user by either its userName or its email -- so a userName that differs from the email (or a userName-only account) still resolves. As with the former behavior, no -Email/-UserName means no user is matched (a no-op), not all users.

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

### -Name

Specifies the new first name (given name) for the user.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Password

Specifies the new password for the user.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Surname

Specifies the new surname (last name) for the user.

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

You can pipe email addresses to this cmdlet via the Email property.

## OUTPUTS

### None

This cmdlet does not produce output. The update is performed as a side effect.

## NOTES

The update API is only called when at least one property value differs from the current value. If all specified values match the existing values, no API call is made.

The user's email address cannot be changed through this cmdlet.

Group membership changes are not supported by this cmdlet. Use Add-PmGroupMember and Remove-PmGroupMember instead.

## RELATED LINKS

[Get-PmUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmUser.md)

[New-PmUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/New-PmUser.md)

[Copy-PmUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-PmUser.md)

[Remove-PmUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-PmUser.md)
