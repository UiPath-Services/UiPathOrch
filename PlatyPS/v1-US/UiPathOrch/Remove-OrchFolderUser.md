---
document type: cmdlet
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Remove-OrchFolderUser
---

# Remove-OrchFolderUser

## SYNOPSIS

Unassigns users from folders in UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
Remove-OrchFolderUser [[-UserName] <string[]>] [[-FullName] <string[]>] [-NoMatchWarning]
 [-Path <string[]>] [-Recurse] [-Depth <uint>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Removes user assignments from UiPath Orchestrator folders. The cmdlet identifies target users by -UserName or -FullName (at least one must be specified). Both parameters support wildcards for matching multiple users at once.

When both -UserName and -FullName are specified, the filters are combined so that only users matching both criteria are removed.

The -UserName and -FullName parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values. Tab completion dynamically suggests users assigned to the target folders.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: POST /odata/Folders({folderId})/UiPath.Server.Configuration.OData.RemoveUserFromFolder

OAuth required scopes: OR.Folders

Required permissions: (Units.Edit or SubFolders.Edit - Remove user from any folder or only if caller has SubFolders.Edit permission on provided folder)

## EXAMPLES

### Example 1: Remove a user from the current folder

```powershell
PS Orch1:\Shared> Remove-OrchFolderUser ytsuda@gmail.com
```

Removes the user "ytsuda@gmail.com" from the current folder (Shared). The -UserName parameter is positional (position 0), so the parameter name can be omitted.

### Example 2: Remove multiple users with wildcards

```powershell
PS Orch1:\Shared> Remove-OrchFolderUser ytsuda*
```

Removes all users whose username starts with "ytsuda" from the current folder.

### Example 3: Remove a user by full name

```powershell
PS Orch1:\Shared> Remove-OrchFolderUser -FullName "Yoshifumi Tsuda"
```

Removes the user whose display name is "Yoshifumi Tsuda" from the current folder.

### Example 4: Preview removal with -WhatIf

```powershell
PS Orch1:\Shared> Remove-OrchFolderUser ytsuda@gmail.com -WhatIf
```

Shows which user would be removed without actually unassigning them.

### Example 5: Remove users recursively from all folders

```powershell
PS Orch1:\> Remove-OrchFolderUser -Recurse -UserName ytsuda@gmail.com
```

Removes the user "ytsuda@gmail.com" from all folders where they are directly assigned.

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

Specifies the display names of the users to unassign. Supports wildcards. Tab completion dynamically suggests full names from the target folders. Either -UserName or -FullName must be specified.

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

Displays a warning message when no matching user is found for the specified -UserName and -FullName criteria. This is useful when importing from CSV to identify entries that do not match any existing folder user.

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

### -UserName

Specifies the usernames of the users to unassign. Supports wildcards. Tab completion dynamically suggests usernames from the target folders. Either -UserName or -FullName must be specified.

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

You can pipe usernames, full names, and paths to this cmdlet via the UserName, FullName, and Path properties.

## OUTPUTS

### System.Object

This cmdlet does not produce output.

## NOTES

Folder users are folder-scoped entities. You must navigate to a folder on the Orch: drive or use -Path to specify target folders. Personal workspace folders are excluded from the operation.

Either -UserName or -FullName must be specified. If neither is provided, the cmdlet writes an error.

Only directly assigned users are considered for removal. Users inherited from parent folders cannot be removed from child folders.

## RELATED LINKS

Get-OrchFolderUser

Add-OrchFolderUser

Copy-OrchFolderUser

Move-OrchFolderUser
