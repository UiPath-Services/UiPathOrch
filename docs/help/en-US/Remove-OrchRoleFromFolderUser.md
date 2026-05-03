---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchRoleFromFolderUser.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Remove-OrchRoleFromFolderUser
---

# Remove-OrchRoleFromFolderUser

## SYNOPSIS

Removes roles from folder users in UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
Remove-OrchRoleFromFolderUser [[-UserName] <string[]>] [-Roles] <string[]> [-FullName <string[]>]
 [-Type <string[]>] [-Path <string[]>] [-Recurse] [-Depth <uint>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Removes folder-level roles from users assigned to UiPath Orchestrator folders. The target users can be identified by -UserName or -FullName (at least one must be specified). Both parameters support filtering to narrow down which users are affected.

The -Roles parameter is mandatory and specifies which folder roles to remove. Roles that are not currently assigned to a user are silently ignored. Only folder-level roles can be removed; tenant-level roles are excluded from the operation.

The -Type parameter can filter which directory types to target (e.g., DirectoryUser, DirectoryGroup, DirectoryRobot, DirectoryExternalApplication).

The -UserName, -FullName, -Roles, and -Type parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values. Tab completion for -Roles is context-aware and suggests only roles currently assigned to the target users.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: POST /odata/Folders/UiPath.Server.Configuration.OData.AssignUsers

OAuth required scopes: OR.Folders

Required permissions: (Units.Edit or SubFolders.Edit - Assigns users to any folder or if the user has SubFolders.Edit permission on all folders provided)

## EXAMPLES

### Example 1: Remove a role from a specific user

```powershell
PS Orch1:\Shared> Remove-OrchRoleFromFolderUser ytsuda@gmail.com Executor
```

Removes the "Executor" role from the user "ytsuda@gmail.com" in the current folder. The -UserName and -Roles parameters are positional (positions 0 and 1).

### Example 2: Remove multiple roles from a user

```powershell
PS Orch1:\Shared> Remove-OrchRoleFromFolderUser ytsuda@gmail.com "Folder Administrator",Executor
```

Removes the "Folder Administrator" and "Executor" roles from the user "ytsuda@gmail.com" in the current folder.

### Example 3: Remove a role from users by full name

```powershell
PS Orch1:\Shared> Remove-OrchRoleFromFolderUser -FullName "Yoshifumi Tsuda" -Roles Executor
```

Removes the "Executor" role from the user whose display name is "Yoshifumi Tsuda" in the current folder.

### Example 4: Remove a role from all users recursively

```powershell
PS Orch1:\> Remove-OrchRoleFromFolderUser -Recurse * -Roles Executor -Type DirectoryUser
```

Removes the "Executor" role from all directory users in all folders. Users who do not have the role are skipped.

### Example 5: Preview role removal with -WhatIf

```powershell
PS Orch1:\Shared> Remove-OrchRoleFromFolderUser ytsuda@gmail.com Executor -WhatIf
```

Shows what would happen without actually removing the role. The output displays the target user and the roles that would be removed.

## PARAMETERS

### -Path

Specifies the target folders. If not specified, the current folder is targeted. Supports wildcards and comma-separated values for multiple folders.

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

### -Recurse

Includes the target folder and all its subfolders in the operation.

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

### -Depth

Specifies the depth for recursion into the target folders. A depth of 0 targets only the current folder with no subfolders included. When -Depth is specified, -Recurse is implied.

```yaml
Type: System.UInt32
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

### -FullName

Specifies the display names of the folder users from whom the roles will be removed. Supports wildcards. Tab completion dynamically suggests full names from the target folders. Either -UserName or -FullName must be specified.

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

Specifies the folder roles to remove from the users. This is a mandatory parameter. Tab completion dynamically suggests roles currently assigned to the target users. Only folder-level roles can be removed; tenant-level roles are excluded.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: false
Aliases:
- FolderRoles
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

Filters folder users by directory type. Valid values are: DirectoryUser, DirectoryGroup, DirectoryRobot, and DirectoryExternalApplication. Supports wildcards. Tab completion suggests available types.

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

Specifies the usernames of the folder users from whom the roles will be removed. Tab completion dynamically suggests usernames from the target folders. Either -UserName or -FullName must be specified.

```yaml
Type: System.String[]
DefaultValue: None
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

### None

This cmdlet does not produce output.

## NOTES

Folder users are folder-scoped entities. You must navigate to a folder on the Orch: drive or use -Path to specify target folders. Personal workspace folders are excluded from the operation.

Either -UserName or -FullName must be specified. If neither is provided, the cmdlet writes an error.

The cmdlet works by reassigning the user with the remaining roles (existing roles minus the removed roles). If all roles are removed, the user remains assigned to the folder with no roles.

## RELATED LINKS

[Get-OrchFolderUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchFolderUser.md)

[Add-OrchRoleToFolderUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Add-OrchRoleToFolderUser.md)

[Add-OrchFolderUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Add-OrchFolderUser.md)

[Remove-OrchFolderUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchFolderUser.md)
