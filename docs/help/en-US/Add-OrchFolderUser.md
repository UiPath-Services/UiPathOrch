---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Add-OrchFolderUser.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Add-OrchFolderUser
---

# Add-OrchFolderUser

## SYNOPSIS

Assigns users to folders in UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
Add-OrchFolderUser [-Type] <string> [-UserName] <string[]> [[-Roles] <string[]>] [-Path <string[]>]
 [-Recurse] [-Depth <uint>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Assigns directory users, groups, robots, or external applications to UiPath Orchestrator folders with optional folder roles. The -Type parameter specifies the kind of directory entity to assign. Valid types are: DirectoryUser, DirectoryGroup, DirectoryRobot, and DirectoryExternalApplication.

When assigning a user, the cmdlet searches the directory service to resolve the specified username. The -UserName parameter requires at least one character to be entered for tab completion to search the directory. A 600ms delay is applied after each assignment to avoid API rate limiting.

The -Roles parameter specifies folder-level roles to assign to the user. If omitted, the user is assigned to the folder with no roles. Tab completion for -Roles dynamically suggests available folder roles (tenant-level roles are excluded). The -Roles parameter supports wildcards.

The -Type, -UserName, and -Roles parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: POST /odata/Folders/UiPath.Server.Configuration.OData.AssignDomainUser

OAuth required scopes: OR.Folders

Required permissions: (Units.Edit or SubFolders.Edit - Assigns domain user to any folder or only if user has SubFolders.Edit permission on all folders provided)

## EXAMPLES

### Example 1: Assign a directory user to the current folder

```powershell
PS Orch1:\Shared> Add-OrchFolderUser DirectoryUser ytsuda@gmail.com
```

Assigns the directory user "ytsuda@gmail.com" to the current folder (Shared) with no roles. The -Type and -UserName parameters are positional (positions 0 and 1), so their names can be omitted.

### Example 2: Assign a user with folder roles

```powershell
PS Orch1:\Shared> Add-OrchFolderUser DirectoryUser ytsuda+c@gmail.com "Folder Administrator",Executor
```

Assigns the directory user "ytsuda+c@gmail.com" to the Shared folder with the "Folder Administrator" and "Executor" roles. The -Roles parameter is positional (position 2).

### Example 3: Assign a directory group to multiple folders recursively

```powershell
PS Orch1:\> Add-OrchFolderUser -Recurse -Type DirectoryGroup -UserName Administrators -Roles Executor
```

Assigns the directory group "Administrators" to all folders with the Executor role.

### Example 4: Assign a user from a specific folder using -Path

```powershell
PS C:\> Add-OrchFolderUser -Path Orch1:\Shared -Type DirectoryUser -UserName ytsuda@gmail.com -Roles "Folder Administrator"
```

Assigns the user "ytsuda@gmail.com" to the Shared folder with the "Folder Administrator" role. When -Path uses an absolute path, the command can be run from any location.

### Example 5: Preview assignment with -WhatIf

```powershell
PS Orch1:\Shared> Add-OrchFolderUser DirectoryUser ytsuda@gmail.com Executor -WhatIf
```

Shows what would happen without actually assigning the user. The output displays the resolved username, display name, and target folder.

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

### -Roles

Specifies the folder roles to assign to the user. Supports wildcards. Tab completion dynamically suggests available folder roles (tenant-level roles are excluded). If omitted, the user is assigned to the folder with no roles.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
Aliases:
- FolderRoles
ParameterSets:
- Name: (All)
  Position: 2
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Type

Specifies the directory entity type to assign. Valid values are: DirectoryUser, DirectoryGroup, DirectoryRobot, and DirectoryExternalApplication. Tab completion suggests available types.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: false
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

### -UserName

Specifies the usernames of the directory entities to assign. For tab completion, enter at least one character to trigger a directory search. Multiple usernames can be specified.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: false
Aliases: []
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

### System.String

You can pipe the directory type to this cmdlet via the Type property.

### System.String[]

You can pipe usernames, roles, and paths to this cmdlet via the UserName, Roles, and Path properties.

## OUTPUTS

### None

This cmdlet does not produce output.

## NOTES

Folder users are folder-scoped entities. You must navigate to a folder on the Orch: drive or use -Path to specify target folders. Personal workspace folders are excluded from the operation.

The cmdlet performs a bulk directory search for all specified usernames grouped by drive and type. For DirectoryRobot types, each robot is resolved individually at assignment time rather than in bulk.

A 600ms delay is applied after each successful assignment to avoid API rate limiting when assigning multiple users.

If a specified role pattern does not match any existing folder role, a warning is displayed.

## RELATED LINKS

[Get-OrchFolderUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchFolderUser.md)

[Remove-OrchFolderUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchFolderUser.md)

[Copy-OrchFolderUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchFolderUser.md)

[Add-OrchRoleToFolderUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Add-OrchRoleToFolderUser.md)

[Remove-OrchRoleFromFolderUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchRoleFromFolderUser.md)
