---
document type: cmdlet
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Remove-OrchRoleFromUser
---

# Remove-OrchRoleFromUser

## SYNOPSIS

Removes tenant-level roles from users in UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
Remove-OrchRoleFromUser [[-UserName] <string[]>] [-Roles] <string[]> [-FullName <string[]>]
 [-Type <string[]>] [-Path <string[]>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Removes tenant-level roles from users in UiPath Orchestrator. The target users can be identified by -UserName or -FullName (at least one must be specified). Both parameters support wildcards for matching multiple users at once.

The -Roles parameter is mandatory and specifies which tenant roles to remove. Roles that are not currently assigned to a user are silently ignored.

The -Type parameter can filter which directory types to target (e.g., DirectoryUser, DirectoryGroup, DirectoryRobot, DirectoryExternalApplication).

This is a tenant-scoped cmdlet. The -Path parameter specifies Orchestrator drives (not folder paths).

The -UserName, -FullName, -Roles, and -Type parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values. Tab completion for -Roles is context-aware and suggests only roles currently assigned to the target users.

Primary Endpoint: PUT /odata/Users({user.Id!.Value})

OAuth required scopes: OR.Users

Required permissions: Users.Edit or Robots.Create or Robots.Edit or Robots.Delete

## EXAMPLES

### Example 1: Remove a role from a specific user

```powershell
PS Orch1:\> Remove-OrchRoleFromUser ytsuda@gmail.com Administrator
```

Removes the "Administrator" role from the user "ytsuda@gmail.com" in the current tenant. The -UserName and -Roles parameters are positional (positions 0 and 1).

### Example 2: Remove multiple roles from a user

```powershell
PS Orch1:\> Remove-OrchRoleFromUser ytsuda@gmail.com Administrator,"Allow to be Automation User"
```

Removes the "Administrator" and "Allow to be Automation User" roles from the user "ytsuda@gmail.com".

### Example 3: Remove a role from users by full name

```powershell
PS Orch1:\> Remove-OrchRoleFromUser -FullName "Yoshifumi Tsuda" -Roles Administrator
```

Removes the "Administrator" role from the user whose display name is "Yoshifumi Tsuda".

### Example 4: Remove a role from a specific drive

```powershell
PS C:\> Remove-OrchRoleFromUser -Path Orch1:\ -UserName ytsuda@gmail.com -Roles Administrator
```

Removes the "Administrator" role from the user "ytsuda@gmail.com" on the Orch1 tenant. When -Path uses an absolute path, the command can be run from any location.

### Example 5: Preview role removal with -WhatIf

```powershell
PS Orch1:\> Remove-OrchRoleFromUser ytsuda@gmail.com Administrator -WhatIf
```

Shows what would happen without actually removing the role. The output displays the roles that would be removed and the target user.

## PARAMETERS

### -Path

Specifies the target Orchestrator drives. If not specified, the current drive is targeted.

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

### -FullName

Specifies the display names of the users from whom the roles will be removed. Supports wildcards. Tab completion dynamically suggests full names from the target tenant. Either -UserName or -FullName must be specified.

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

### -Roles

Specifies the tenant roles to remove from the users. This is a mandatory parameter. Tab completion dynamically suggests roles currently assigned to the target users.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: false
Aliases:
- TenantRoles
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

### -Type

Filters users by directory type. Valid values are: DirectoryUser, DirectoryGroup, DirectoryRobot, and DirectoryExternalApplication. Supports wildcards. Tab completion suggests available types.

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

Specifies the usernames of the users from whom the roles will be removed. Supports wildcards. Tab completion dynamically suggests usernames from the target tenant. Either -UserName or -FullName must be specified.

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

Shows what would happen if the cmdlet runs. The cmdlet is not run.

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

You can pipe usernames, roles, full names, types, and paths to this cmdlet via the UserName, Roles, FullName, Type, and Path properties.

## OUTPUTS

### UiPath.PowerShell.Entities.User

Returns updated User objects after the roles have been removed.

## NOTES

This is a tenant-scoped cmdlet. Users and their tenant roles are managed at the tenant level, not at the folder level. The -Path parameter specifies Orchestrator drives (e.g., Orch1:\), not folder paths.

Either -UserName or -FullName must be specified. If neither is provided, the cmdlet writes an error.

The cmdlet updates the user by replacing the entire role list. It retrieves the full user object, removes the specified roles from the roles list, and writes the updated user back via the PUT endpoint.

## RELATED LINKS

Get-OrchUser

Add-OrchRoleToFolderUser

Remove-OrchRoleFromFolderUser
