---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Add-OrchRoleToFolderUser
---

# Add-OrchRoleToFolderUser

## SYNOPSIS

Assigns roles to folder users in UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
Add-OrchRoleToFolderUser [[-UserName] <string[]>] [-Roles] <string[]> [-FullName <string[]>]
 [-Type <string[]>] [-Path <string[]>] [-Recurse] [-Depth <uint>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Adds folder-level roles to users already assigned to UiPath Orchestrator folders. The target users can be identified by -UserName or -FullName (at least one must be specified). Both parameters support filtering to narrow down which users receive the new roles.

The -Roles parameter is mandatory and specifies which folder roles to add. Roles that are already assigned to a user are automatically skipped. Only folder-level roles are available; tenant-level roles are excluded.

The -Type parameter can filter which directory types to target (e.g., DirectoryUser, DirectoryGroup, DirectoryRobot, DirectoryExternalApplication).

The -UserName, -FullName, -Roles, and -Type parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values. Tab completion for -Roles is context-aware and suggests only roles not yet assigned to the target users.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: POST /odata/Folders/UiPath.Server.Configuration.OData.AssignUsers

OAuth required scopes: OR.Folders

Required permissions: (Units.Edit or SubFolders.Edit - Assigns users to any folder or if the user has SubFolders.Edit permission on all folders provided)

## EXAMPLES

### Example 1: Add a role to a specific user

```powershell
PS Orch1:\Shared> Add-OrchRoleToFolderUser ytsuda@gmail.com Executor
```

Adds the "Executor" role to the user "ytsuda@gmail.com" in the current folder. The -UserName and -Roles parameters are positional (positions 0 and 1).

### Example 2: Add multiple roles to a user

```powershell
PS Orch1:\Shared> Add-OrchRoleToFolderUser ytsuda@gmail.com "Folder Administrator",Executor
```

Adds the "Folder Administrator" and "Executor" roles to the user "ytsuda@gmail.com" in the current folder.

### Example 3: Add a role to users by full name

```powershell
PS Orch1:\Shared> Add-OrchRoleToFolderUser -FullName "Yoshifumi Tsuda" -Roles Executor
```

Adds the "Executor" role to the user whose display name is "Yoshifumi Tsuda" in the current folder.

### Example 4: Add a role to all directory users recursively

```powershell
PS Orch1:\> Add-OrchRoleToFolderUser -Recurse -UserName * -Roles Executor -Type DirectoryUser
```

Adds the "Executor" role to all directory users in all folders. Users who already have the role are skipped.

### Example 5: Preview role assignment with -WhatIf

```powershell
PS Orch1:\Shared> Add-OrchRoleToFolderUser ytsuda@gmail.com Executor -WhatIf
```

Shows what would happen without actually adding the role. The output displays the target user and the roles that would be added.

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

Specifies the display names of the folder users to whom the roles will be assigned. Supports wildcards. Tab completion dynamically suggests full names from the target folders. Either -UserName or -FullName must be specified.

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

Specifies the folder roles to add to the users. This is a mandatory parameter. Tab completion dynamically suggests roles not yet assigned to the target users. Only folder-level roles are available; tenant-level roles are excluded.

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

Specifies the usernames of the folder users to whom the roles will be assigned. Tab completion dynamically suggests usernames from the target folders. Either -UserName or -FullName must be specified.

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

Roles that are already assigned to a user are automatically skipped. Only roles that the user does not currently have are added.

## RELATED LINKS

Get-OrchFolderUser

Remove-OrchRoleFromFolderUser

Add-OrchFolderUser

Remove-OrchFolderUser
