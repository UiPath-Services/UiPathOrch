---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Add-OrchRoleToFolderUser

## SYNOPSIS
Assigns roles to folder users.

## SYNTAX

```
Add-OrchRoleToFolderUser [[-UserName] <String[]>] [-FullName <String[]>] [-Roles] <String[]> [-Type <String[]>]
 [-Path <String[]>] [-Recurse] [-Depth <UInt32>] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## DESCRIPTION
The Add-OrchRoleToFolderUser cmdlet assigns roles to users within specific folders in UiPath Orchestrator tenants. This cmdlet manages folder-level permissions by granting users specific roles that determine their access rights and capabilities within those folders.

The cmdlet supports assigning roles to users using either UserName or FullName for identification. You can assign multiple roles to multiple users simultaneously and target specific folders or apply changes recursively to folder hierarchies.

Use the -Roles parameter to specify which roles to assign and either -UserName or -FullName to identify the target users. The -Path parameter specifies target folders, and the -Recurse parameter applies role assignments to all subfolders.

This is a folder entity cmdlet. Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters. The -Recurse parameter enables assigning roles to users across all subfolders, maintaining consistent permissions throughout the folder structure.

Primary Endpoint: POST /odata/Folders/UiPath.Server.Configuration.OData.AssignUsers

OAuth required scopes: OR.Folders

Required permissions: (Units.Edit or SubFolders.Edit - Assigns users to any folder or if the user has SubFolders.Edit permission on all folders provided)

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Add-OrchRoleToFolderUser -UserName john.doe -Roles Developer
```

Assigns the Developer role to user john.doe in the current folder (Development).

### Example 2
```powershell
PS C:\> Add-OrchRoleToFolderUser -Path Orch1:\Finance jane.smith "Business Analyst"
```

Assigns Business Analyst and Viewer roles to user jane.smith in the Finance folder.

### Example 3
```powershell
PS Orch1:\Production> Add-OrchRoleToFolderUser -FullName "Alex Johnson" -Roles Administrator -WhatIf
```

Shows what would happen when assigning the Administrator role to user Alex Johnson in the Production folder.

### Example 4
```powershell
PS C:\> Add-OrchRoleToFolderUser -Path Orch1:\Development team.lead "Team Lead"
```

Assigns the Team Lead role to user team.lead in both Development and Testing folders.

### Example 5
```powershell
PS Orch1:\> Add-OrchRoleToFolderUser -Recurse support.user -Roles Viewer
```

Assigns the Viewer role to support.user in all folders recursively from the current location.

### Example 6
```powershell
PS Orch1:\Finance> Get-OrchUser *analyst* | Add-OrchRoleToFolderUser -Roles "Business Analyst"
```

Gets all users with names containing analyst and assigns them the Business Analyst role in the current Finance folder.

### Example 7
```powershell
PS C:\> Add-OrchRoleToFolderUser -Path Orch1:\Development -Type DirectoryUser contractor.* -Roles Developer
```

Assigns the Developer role to all users matching contractor.* pattern in the Development folder, filtering by User type.

## PARAMETERS

### -Depth
Specifies the maximum number of subfolder levels to include when using -Recurse parameter.

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
Specifies the full names of the users to assign roles to. Alternative to UserName parameter.

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

### -Path
Specifies the target folders where role assignments should be made.

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

### -Recurse
Specifies that role assignments should be applied to all subfolders recursively from the specified path.

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

### -Roles
Specifies the roles to assign to the users.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases: FolderRoles

Required: True
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -UserName
Specifies the usernames of the users to assign roles to.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Confirm
Prompts you for confirmation before running the cmdlet.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: cf

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -WhatIf
Shows what would happen if the cmdlet runs.
The cmdlet is not run.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: wi

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Type
Specifies the user types to filter by when assigning roles.

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

### System.Object
## NOTES
This is a folder entity cmdlet. Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters.

Use either -UserName or -FullName to identify users. Multiple roles can be assigned simultaneously. Use -WhatIf to preview changes before executing role assignments.

## RELATED LINKS

[Get-OrchUser](Get-OrchUser.md)

[Get-OrchRole](Get-OrchRole.md)

[Remove-OrchRoleFromFolderUser](Remove-OrchRoleFromFolderUser.md)

[Get-OrchFolderUser](Get-OrchFolderUser.md)
