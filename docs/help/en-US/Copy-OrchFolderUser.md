---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchFolderUser.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Copy-OrchFolderUser
---

# Copy-OrchFolderUser

## SYNOPSIS

Copies folder user assignments to another folder or Orchestrator instance.

## SYNTAX

### __AllParameterSets

```
Copy-OrchFolderUser [-UserName] <string[]> [-Destination] <string> [-Type <string[]>]
 [-Path <string>] [-Recurse] [-Depth <uint>] [-UserMappingCsv <string>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Copies folder user assignments from a source folder to a destination folder in UiPath Orchestrator. The destination can be a different folder on the same Orchestrator instance or a folder on a different Orchestrator instance (cross-drive copy). Each user's folder roles are preserved during the copy.

If the source and destination resolve to the same folder, the operation is silently skipped. The -UserName parameter supports wildcards to copy multiple user assignments at once. The -Type parameter can filter which directory types to copy (e.g., DirectoryUser, DirectoryGroup).

With -Recurse, the cmdlet preserves the folder hierarchy relative to the source root. Subfolders are matched by relative path on the destination.

When copying across different Orchestrator instances, use -UserMappingCsv to specify how source usernames map to destination usernames. The user mapping CSV can be generated using New-OrchUserMappingCsv.

Note that -Path is a single string (not string[]), unlike most other cmdlets.

The -UserName and -Type parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values. Tab completion dynamically suggests users from the source folders.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: GET /odata/Folders/UiPath.Server.Configuration.OData.GetUsersForFolder(key={folderId}), POST /odata/Folders/UiPath.Server.Configuration.OData.AssignUsers

OAuth required scopes: OR.Folders or OR.Folders.Read (source), OR.Folders (destination)

Required permissions: Units.View or SubFolders.View (source), Units.Edit or SubFolders.Edit (destination)

## EXAMPLES

### Example 1: Copy all user assignments to another folder

```powershell
PS Orch1:\Shared> Copy-OrchFolderUser * Orch1:\Dept#2
```

Copies all folder user assignments from the current folder (Shared) to the Dept#2 folder on the same Orchestrator instance. The -UserName and -Destination parameters are positional (positions 0 and 1).

### Example 2: Copy a specific user to another folder

```powershell
PS Orch1:\Shared> Copy-OrchFolderUser ytsuda@gmail.com Orch1:\Dept#2
```

Copies the folder assignment for user "ytsuda@gmail.com" (including their folder roles) from Shared to the Dept#2 folder.

### Example 3: Copy only directory users with -Type filter

```powershell
PS Orch1:\Shared> Copy-OrchFolderUser * Orch1:\Dept#2 -Type DirectoryUser
```

Copies only directory user assignments (excluding groups, robots, and external applications) from the current folder to the Dept#2 folder.

### Example 4: Copy recursively to another Orchestrator instance

```powershell
PS C:\> Copy-OrchFolderUser -Path Orch1:\Shared -Recurse * Orch2:\Shared -UserMappingCsv C:\temp\user-mapping.csv
```

Copies all folder user assignments from Shared and all its subfolders on Orch1, preserving the folder hierarchy at the destination on Orch2. The user mapping CSV maps source usernames to destination usernames for cross-instance migration.

### Example 5: Preview copy with -WhatIf

```powershell
PS Orch1:\Shared> Copy-OrchFolderUser ytsuda* Orch1:\Dept#2 -WhatIf
```

Shows which user assignments would be copied without executing the command.

## PARAMETERS

### -Path

Specifies the source folder. If not specified, the current folder is used. Unlike most other cmdlets, this parameter accepts a single string (not an array).

```yaml
Type: System.String
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

Includes the source folder and all its subfolders in the copy operation. The folder hierarchy relative to the source root is preserved at the destination.

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

Specifies the depth for recursion into the source folders. A depth of 0 targets only the source folder with no subfolders included. When -Depth is specified, -Recurse is implied.

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

### -Destination

Specifies the destination folder path. This is a mandatory parameter. Can be a folder on the same Orchestrator instance (e.g., Orch1:\Production) or on a different instance (e.g., Orch2:\Shared) for cross-instance migration.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: true
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

### -UserMappingCsv

Specifies the path to a user mapping CSV file for cross-instance migration. The CSV maps source usernames to destination usernames, which is required when copying folder users across Orchestrator instances where user accounts have different names. Use New-OrchUserMappingCsv to generate the mapping file. Requires a filesystem path (not an Orch: drive path).

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
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -UserName

Specifies the usernames of the folder users to copy. This is a mandatory parameter. Supports wildcards to copy multiple users. Tab completion dynamically suggests usernames from the source folders.

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

### System.String[]

You can pipe usernames to this cmdlet via the UserName property.

### System.String

You can pipe source path and destination via the Path and Destination properties.

## OUTPUTS

### UiPath.PowerShell.Entities.UserRoles

Returns UserRoles objects for newly created folder user assignments at the destination.

## NOTES

The -Path parameter is a single string, not a string array. This differs from most other cmdlets in the module that accept string arrays for -Path.

When the source and destination resolve to the same folder, the operation is silently skipped without error.

For cross-instance migration, a user mapping CSV is required because user identifiers differ between instances. Without -UserMappingCsv, users may not be matched correctly if usernames do not correspond. Use New-OrchUserMappingCsv to generate the mapping file.

## RELATED LINKS

Get-OrchFolderUser

Add-OrchFolderUser

Move-OrchFolderUser

Remove-OrchFolderUser

New-OrchUserMappingCsv
