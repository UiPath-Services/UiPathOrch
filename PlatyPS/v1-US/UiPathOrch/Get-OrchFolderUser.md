---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchFolderUser
---

# Get-OrchFolderUser

## SYNOPSIS

Gets the users assigned to folders.

## SYNTAX

### __AllParameterSets

```
Get-OrchFolderUser [[-UserName] <string[]>] [[-FullName] <string[]>] [-Type <string[]>]
 [-IncludeInherited] [-Path <string[]>] [-Recurse] [-Depth <uint>] [-ExportCsv <string>]
 [-CsvEncoding <Encoding>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets user assignment information from UiPath Orchestrator folders. This cmdlet retrieves users that are directly assigned to the specified folders, showing each user's username, full name, directory type, and assigned folder roles.

By default, only directly assigned users are returned. When -IncludeInherited is specified, users inherited from parent folders are also included in the results.

The cmdlet supports filtering by -UserName, -FullName, and -Type parameters. All three support wildcards and multiple comma-separated values. The -Type parameter accepts the following values: DirectoryUser, DirectoryGroup, DirectoryRobot, and DirectoryExternalApplication. Tab completion dynamically suggests values from actual data in the target folders. Press [Ctrl+Space] or [Tab] to see available values.

Results can be exported to a CSV file using -ExportCsv. The CSV includes columns: Path, Type, UserName, FullName, and FolderRoles. The exported CSV can be used with Add-OrchFolderUser for import.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: GET /odata/Folders/UiPath.Server.Configuration.OData.GetUsersForFolder(key={folderId})

OAuth required scopes: OR.Folders or OR.Folders.Read

Required permissions: Units.View or SubFolders.View

## EXAMPLES

### Example 1: Get all users in the current folder

```powershell
PS Orch1:\Shared> Get-OrchFolderUser
```

Gets all users directly assigned to the current folder (Shared).

### Example 2: Get users by username with wildcards

```powershell
PS Orch1:\Shared> Get-OrchFolderUser ytsuda*
```

Gets all folder users whose username starts with "ytsuda" from the current folder. The -UserName parameter is positional (position 0), so the parameter name can be omitted.

### Example 3: Get users recursively across all folders

```powershell
PS Orch1:\> Get-OrchFolderUser -Recurse
```

Gets all user assignments from all folders in the Orchestrator instance.

### Example 4: Get users including inherited assignments

```powershell
PS Orch1:\Shared> Get-OrchFolderUser -IncludeInherited
```

Gets all users assigned to the current folder, including users inherited from parent folders.

### Example 5: Get users by type and export to CSV

```powershell
PS Orch1:\> Get-OrchFolderUser -Recurse -Type DirectoryUser -ExportCsv C:\temp\folder-users.csv
```

Gets all directory users from all folders and exports them to a CSV file. The CSV can be used with Add-OrchFolderUser for import to another Orchestrator instance.

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

### -CsvEncoding

Specifies the encoding for CSV export. Default is UTF-8 with BOM for Excel compatibility. Tab completion suggests all available system encodings (e.g., utf-8, shift_jis, us-ascii).

```yaml
Type: System.Text.Encoding
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

### -ExportCsv

Exports folder user assignments to the specified CSV file path. The CSV includes columns: Path, Type, UserName, FullName, and FolderRoles. When -ExportCsv is specified, no objects are written to the pipeline. The exported CSV can be used with Add-OrchFolderUser for import. Requires a filesystem path (not an Orch: drive path). If a directory path is specified, the default filename ExportedFolderUsers.csv is used.

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
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -FullName

Specifies the display names of the folder users to retrieve. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests full names from the target folders.

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

### -IncludeInherited

Includes users inherited from parent folders in the results. By default, only users directly assigned to the target folder are returned.

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

Filters folder users by directory type. Valid values are: DirectoryUser, DirectoryGroup, DirectoryRobot, and DirectoryExternalApplication. Supports wildcards. Tab completion dynamically suggests available types.

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

Specifies the usernames of the folder users to retrieve. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests usernames from the target folders.

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe usernames, full names, types, and paths to this cmdlet via the UserName, FullName, Type, and Path properties.

## OUTPUTS

### UiPath.PowerShell.Entities.UserRoles

Returns UserRoles objects with properties including UserName, FullName, Type (directory type), and Roles (list of folder roles assigned to the user). Each object represents a user's assignment to a specific folder.

## NOTES

Folder users are folder-scoped entities. You must navigate to a folder on the Orch: drive or use -Path to specify target folders.

Personal workspace folders are included in the enumeration. To exclude them, filter by folder type or use specific -Path values.

The -ExportCsv parameter exports data with columns: Path, Type, UserName, FullName, FolderRoles. For DirectoryGroup entries, the UserName column contains the FullName value to ensure correct resolution when importing with Add-OrchFolderUser.

## RELATED LINKS

Add-OrchFolderUser

Remove-OrchFolderUser

Copy-OrchFolderUser

Add-OrchRoleToFolderUser

Remove-OrchRoleFromFolderUser
