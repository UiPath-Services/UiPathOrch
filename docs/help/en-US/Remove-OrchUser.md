---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchUser.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Remove-OrchUser
---

# Remove-OrchUser

## SYNOPSIS

Removes users from UiPath Orchestrator tenants.

## SYNTAX

### __AllParameterSets

```
Remove-OrchUser [-Path <string[]>] [-LiteralPath <string[]>] [[-UserName] <string[]>] [[-FullName] <string[]>]
 [-Confirm] [-NoMatchWarning] [-Type <string[]>] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Removes user registrations from UiPath Orchestrator tenants. Either -UserName or -FullName must be specified. Both parameters support wildcards for matching multiple users. The -Type parameter can be used to filter users by directory type before removal.

The cmdlet retrieves the list of users from the tenant, filters by the specified -UserName, -FullName, and -Type criteria, and deletes matching users. If -NoMatchWarning is specified, a warning is displayed when no users match the given criteria instead of silently completing.

The -UserName, -FullName, -Type, and -Path parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values.

Primary Endpoint: DELETE /odata/Users({userId})

OAuth required scopes: OR.Users

Required permissions: Users.Delete

## EXAMPLES

### Example 1: Remove a specific user

```powershell
PS Orch1:\> Remove-OrchUser ytsuda@gmail.com
```

Removes the user "ytsuda@gmail.com" from the current tenant. The -UserName parameter is positional (position 0) so the parameter name can be omitted.

### Example 2: Remove users with a wildcard

```powershell
PS Orch1:\> Remove-OrchUser ytsuda*
```

Removes all users whose username starts with "ytsuda" from the current tenant.

### Example 3: Remove a user by full name

```powershell
PS Orch1:\> Remove-OrchUser -FullName 'Yoshifumi Tsuda'
```

Removes the user whose display name matches "Yoshifumi Tsuda". The -FullName parameter is positional (position 1).

### Example 4: Preview removal with -WhatIf

```powershell
PS Orch1:\> Remove-OrchUser dev* -WhatIf
```

Shows which users would be removed without actually deleting them.

### Example 5: Remove users of a specific type

```powershell
PS Orch1:\> Remove-OrchUser * -Type DirectoryRobot
```

Removes all robot accounts from the current tenant. Valid values for -Type are DirectoryUser, DirectoryGroup, DirectoryRobot, and DirectoryExternalApplication.

### Example 6: Remove a user from a specific drive with no-match warning

```powershell
PS C:\> Remove-OrchUser -Path Orch1:\ -UserName old-user -NoMatchWarning
```

Attempts to remove the user "old-user" from the Orch1 tenant. If no matching user is found, a warning is displayed instead of completing silently.

## PARAMETERS

### -Path

Specifies the target Orch: drives. If not specified, the current drive is targeted. Tab completion suggests available Orch: drives.

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

### -FullName

Specifies the display names of users to remove. Supports wildcards. Tab completion dynamically suggests user display names from the target tenant.

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

### -NoMatchWarning

Displays a warning when no users match the specified -UserName and -FullName criteria. Without this switch, the cmdlet completes silently when no matches are found.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: False
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

### -Type

Filters users by directory type before removal. Valid values are DirectoryUser, DirectoryGroup, DirectoryRobot, and DirectoryExternalApplication. Supports wildcards.

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

### -UserName

Specifies the usernames of users to remove. Supports wildcards. Tab completion dynamically suggests usernames from the target tenant.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
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

### -WhatIf

Shows what would happen if the cmdlet runs.
The cmdlet is not run.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: False
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
DefaultValue: False
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

You can pipe UserName, FullName, Type, and Path values to this cmdlet via their property names.

## OUTPUTS

### None

This cmdlet does not produce output.

## NOTES

Users are tenant-scoped entities. Navigate to the root of an Orch: drive or use -Path to specify target drives.

Either -UserName or -FullName must be specified; if both are empty or null, an error is returned. When both are specified, both filters are applied (AND logic) to narrow the set of users to remove.

## RELATED LINKS

[Get-OrchUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchUser.md)

[Add-OrchUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Add-OrchUser.md)

[Update-OrchUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Update-OrchUser.md)

[Copy-OrchUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchUser.md)
