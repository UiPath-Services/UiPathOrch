---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Move-OrchFolderUser.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Move-OrchFolderUser
---

# Move-OrchFolderUser

## SYNOPSIS

Moves folder user assignments from one folder to another.

## SYNTAX

### __AllParameterSets

```
Move-OrchFolderUser [-Path <string[]>] [-LiteralPath <string[]>] [-UserName] <string[]> [[-Destination] <string[]>]
 [-Confirm] [-KeepSource <string>] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Moves folder user assignments from source folders to destination folders in UiPath Orchestrator. By default, users are unassigned from the source folder and assigned to the destination folder with the same roles (move operation). When -KeepSource is set to "true", users are assigned to the destination without being removed from the source (copy operation).

If the source and destination resolve to the same folder, the operation is silently skipped. Moving folder users between different Orchestrator tenants (cross-drive) is not supported and produces a warning.

The -UserName parameter supports wildcards to move multiple users at once. Tab completion dynamically suggests usernames from the source folders.

The -UserName parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available values.

Primary Endpoint: GET /odata/Folders/UiPath.Server.Configuration.OData.GetUsersForFolder(key={folderId}), POST /odata/Folders/UiPath.Server.Configuration.OData.AssignUsers, POST /odata/Folders({folderId})/UiPath.Server.Configuration.OData.RemoveUserFromFolder

OAuth required scopes: OR.Folders

Required permissions: Units.Edit or SubFolders.Edit

## EXAMPLES

### Example 1: Move a user to another folder

```powershell
PS Orch1:\Shared> Move-OrchFolderUser ytsuda@gmail.com Orch1:\Dept#2
```

Moves the user "ytsuda@gmail.com" from the Shared folder to the Dept#2 folder, preserving their folder roles. The user is removed from Shared after being assigned to Dept#2.

### Example 2: Copy a user to another folder (keep source)

```powershell
PS Orch1:\Shared> Move-OrchFolderUser ytsuda@gmail.com Orch1:\Dept#2 -KeepSource true
```

Assigns the user "ytsuda@gmail.com" to the Dept#2 folder with the same roles, while keeping the user's assignment in the Shared folder.

### Example 3: Move all users to another folder

```powershell
PS Orch1:\Shared> Move-OrchFolderUser * Orch1:\Dept#2
```

Moves all users from the Shared folder to the Dept#2 folder.

### Example 4: Move a user from a specific folder using -Path

```powershell
PS C:\> Move-OrchFolderUser -Path Orch1:\Shared -UserName ytsuda+c@gmail.com -Destination Orch1:\Dept#2
```

Moves the user "ytsuda+c@gmail.com" from the Shared folder to the Dept#2 folder. When -Path uses an absolute path, the command can be run from any location.

### Example 5: Preview move with -WhatIf

```powershell
PS Orch1:\Shared> Move-OrchFolderUser ytsuda@gmail.com Orch1:\Dept#2 -WhatIf
```

Shows what would happen without actually moving the user. The output displays the source user path and destination folder.

## PARAMETERS

### -Path

Specifies the source folders. If not specified, the current folder is targeted. Supports wildcards and comma-separated values for multiple folders.

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

### -Destination

Specifies the destination folders. Can be one or more folder paths on the same Orchestrator instance. Cross-drive (cross-tenant) moves are not supported.

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

### -KeepSource

Controls whether the user assignment is kept in the source folder. Set to "true" to copy (keep the source assignment) or "false" to move (remove the source assignment). Defaults to "false" (move). Tab completion suggests "true" and "false".

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -UserName

Specifies the usernames of the folder users to move. This is a mandatory parameter. Supports wildcards to move multiple users. Tab completion dynamically suggests usernames from the source folders.

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

You can pipe the KeepSource value to this cmdlet via the KeepSource property.

### System.String[]

You can pipe usernames, destination paths, and source paths to this cmdlet via the UserName, Destination, and Path properties.

## OUTPUTS

### None

This cmdlet does not produce output.

## NOTES

Folder users are folder-scoped entities. You must navigate to a folder on the Orch: drive or use -Path to specify source folders. Personal workspace folders are excluded from the operation.

Moving folder users between different Orchestrator tenants (cross-drive) is not supported. If a cross-drive move is attempted, a warning is displayed and the operation is skipped.

This cmdlet does not support -Recurse or -Depth parameters. To move users from multiple folders, specify multiple paths using the -Path parameter.

## RELATED LINKS

[Get-OrchFolderUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchFolderUser.md)

[Add-OrchFolderUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Add-OrchFolderUser.md)

[Remove-OrchFolderUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchFolderUser.md)

[Copy-OrchFolderUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchFolderUser.md)
