---
document type: cmdlet
external help file: UiPathOrch-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Find-OrchFolderNoUserAssigned.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Find-OrchFolderNoUserAssigned
---

# Find-OrchFolderNoUserAssigned

## SYNOPSIS

Finds folders that have no user assignments.

## SYNTAX

### __AllParameterSets

```
Find-OrchFolderNoUserAssigned [[-Path] <string>] [-IncludeInherited]
```

## ALIASES

## DESCRIPTION

Recursively checks all folders under a specified path and returns folders where no users are assigned. By default, only direct user assignments are considered. Users inherited from parent folders are not counted unless the -IncludeInherited switch is specified.

This cmdlet is useful for identifying folders that may need user assignments or that can be cleaned up.

This is a script function (external help file: UiPathOrch-Help.xml) that uses Get-ChildItem (dir) with -Recurse to enumerate folders, and Get-OrchFolderUser to check user assignments for each folder.

Primary Endpoint: GET /odata/Folders/UiPath.Server.Configuration.OData.GetUsersForFolder(key={folderId},includeInherited={bool})

OAuth required scopes: OR.Folders or OR.Folders.Read

Required permissions: SubFolders.View, Users.View

## EXAMPLES

### Example 1: Find folders without direct user assignments

```powershell
PS C:\> Find-OrchFolderNoUserAssigned Orch1:\
```

Recursively checks all folders under the Orch1:\ root and lists those without direct user assignments.

### Example 2: Find folders without any user assignments including inherited

```powershell
PS C:\> Find-OrchFolderNoUserAssigned Orch1:\ -IncludeInherited
```

Recursively checks all folders under the Orch1:\ root and lists those without any user assignments, including users inherited from parent folders.

## PARAMETERS

### -Path

Specifies the path to the folder where the search should start.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 0
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -IncludeInherited

When specified, the cmdlet considers users inherited from parent folders as part of the user assignments. Without this switch, only direct user assignments are checked. A folder with only inherited users (no direct assignments) is returned when this switch is not used, but excluded when this switch is used.

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

## INPUTS

## OUTPUTS

### UiPath.PowerShell.Entities.Folder

Returns Folder objects for each folder that has no user assignments.

## NOTES

By default, users inherited from parent folders are ignored. If the -IncludeInherited switch is provided, inherited users will be included in the check.

## RELATED LINKS

Get-OrchFolderUser
