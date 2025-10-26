---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchFolderUser

## SYNOPSIS
Retrieves users and groups assigned to folders in UiPath Orchestrator.

## SYNTAX

```
Get-OrchFolderUser [[-UserName] <String[]>] [[-FullName] <String[]>] [-Type <String[]>] [-IncludeInherited]
 [-Path <String[]>] [-Recurse] [-Depth <UInt32>] [-ExportCsv <String>] [-CsvEncoding <Encoding>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
The Get-OrchFolderUser cmdlet retrieves users and groups assigned to folders within UiPath Orchestrator. This cmdlet provides visibility into folder-level access control, showing which users and groups have access to specific folders and their assigned roles and permissions.

Each folder user entry contains information about the user entity (including Type: DirectoryUser or DirectoryGroup), UserName, HasAlertsEnabled status, RobotType capabilities (Attended, Unattended), and assigned Roles with their permissions. The UserEntity includes details such as FullName, AuthenticationSource, and robot capabilities.

This cmdlet operates as a folder entity operation, requiring navigation to the appropriate folder context or specification of target folders using the -Path parameter. Use the -Recurse parameter to include users from subfolders, and -Depth to control recursion levels.

Primary Endpoint: GET /odata/FolderUsers

OAuth required scopes: OR.Users or OR.Users.Read

Required permissions: Users.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Get-OrchFolderUser
```

Retrieves all users and groups assigned to the current Shared folder, displaying Id, UserEntity.Type, UserEntity.UserName, HasAlertsEnabled, RobotType, and Roles.

### Example 2
```powershell
PS Orch1:\Shared> Get-OrchFolderUser | ConvertTo-Json -Depth 3
```

Displays detailed folder user properties in JSON format, including complete UserEntity details and Role information with Origin and InheritedFromFolder properties.

### Example 3
```powershell
PS C:\> Get-OrchFolderUser -Path Orch1:\Shared -UserName *admin*
```

Gets all folder users with usernames containing "admin" in the Shared folder.

### Example 4
```powershell
PS Orch1:\> Get-OrchFolderUser -Recurse -Type DirectoryUser
```

Retrieves all individual users (not groups) assigned across all folders using the -Type parameter for efficient filtering.

### Example 5
```powershell
PS Orch1:\Shared> Get-OrchFolderUser | ConvertTo-Json -Depth 3
```

Displays detailed folder user properties in JSON format, including complete UserEntity details and Role information with Origin and InheritedFromFolder properties.

### Example 6
```powershell
PS Orch1:\> Get-OrchFolderUser -Recurse | Group-Object {$_.UserEntity.Type} | Select-Object Name, Count, @{Name="UserNames";Expression={($_.Group.UserEntity.UserName | Sort-Object -Unique) -join ", "}}
```

Groups folder users by entity type and displays user counts with actual usernames for each type, providing a clear overview of user distribution.

## PARAMETERS

### -Depth
Specifies the depth for recursion into target folders. A depth of 0 indicates the current location only. Higher values include more subfolder levels.

```yaml
Type: UInt32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -FullName
Specifies the FullName of the folder users to be retrieved.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -IncludeInherited
Specifies to also retrieve users inherited from the parent folders.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -Path
Specifies the target folders to search. If not specified, the current folder context will be used. For folder entity operations requiring path specification.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Recurse
Includes the target folder and all its subfolders in the search operation. Essential for comprehensive folder user discovery.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -UserName
Specifies the usernames to filter folder users. Supports wildcard patterns for flexible user selection.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -ProgressAction
Controls how progress information is displayed during command execution. Use 'SilentlyContinue' to suppress progress display.

```yaml
Type: ActionPreference
Parameter Sets: (All)
Aliases: proga

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -CsvEncoding
Specifies the character encoding for CSV export when using -ExportCsv. Common values include 'UTF8', 'ASCII', 'UTF32'. Default is UTF8.

```yaml
Type: Encoding
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ExportCsv
Exports the folder user data to a CSV file at the specified path. Use this for reporting, backup, or data analysis purposes.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Type
Filters users by type. Specify 'DirectoryUser' for Active Directory users, 'DirectoryGroup' for AD groups, or other user types as needed.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.UserRoles
## NOTES
This cmdlet is a folder entity operation requiring navigation to a folder context or path specification using -Path parameter. The cmdlet reveals folder-level access control showing both individual users (DirectoryUser) and groups (DirectoryGroup) with their role assignments. UserEntity.Type distinguishes between users and groups. Roles include Origin information showing whether permissions are directly "Assigned" or inherited. This operation requires Users.View permissions in the target folders.



Primary Endpoint: GET /odata/Folders/UiPath.Server.Configuration.OData.GetUsersForFolder
OAuth required scopes: OR.Users or OR.Users.Read
Required permissions: Users.View

## RELATED LINKS

[Add-OrchFolderUser](Add-OrchFolderUser.md)

[Remove-OrchFolderUser](Remove-OrchFolderUser.md)

[Set-OrchFolderUser](Set-OrchFolderUser.md)

[Get-OrchUser](Get-OrchUser.md)
