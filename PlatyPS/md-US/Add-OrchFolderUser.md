---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Add-OrchFolderUser

## SYNOPSIS
Assigns the users to folders.

## SYNTAX

```
Add-OrchFolderUser -Type <String> [-UserName] <String[]> [[-Roles] <String[]>] [-Path <String[]>] [-Recurse]
 [-Depth <UInt32>] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The Add-OrchFolderUser cmdlet assigns existing users to specific folders within UiPath Orchestrator tenants. This cmdlet operates on folder entities and requires proper folder navigation or explicit folder specification using the -Path, -Recurse, or -Depth parameters.

Users must already exist in the tenant before they can be assigned to folders. Use Get-OrchUser to verify user existence. The cmdlet supports assigning multiple users to multiple folders with specific roles and permissions at the folder level.

Folder user assignments control which users have access to specific folders and define their permissions within those folders. Use the -Type parameter to specify the type of directory entity (DirectoryUser, DirectoryRobot, DirectoryGroup, or DirectoryExternalApplication), and the -Roles parameter to define their folder-level roles.

This is a folder entity cmdlet. If you receive the error "Use Set-Location cmdlet (cd command) to navigate to the target folder first", navigate to the target folder or use the -Path, -Recurse, or -Depth parameters to specify target folders.

Primary Endpoint: POST /odata/Folders/UiPath.Server.Configuration.OData.AssignDomainUser

OAuth required scopes: OR.Folders or OR.Folders.Write

Required permissions: (Units.Edit or SubFolders.Edit - Assigns domain user to any folder or only if user has SubFolders.Edit permission on all folders provided)

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Development> Add-OrchFolderUser DirectoryUser john.doe@company.com "Folder Administrator"
```

Assigns a directory user to the current folder with Folder Administrator role.

### Example 2
```powershell
PS Orch1:\> Add-OrchFolderUser -Path .\Development DirectoryUser jane.smith@company.com "Automation Developer"
```

Assigns a directory user to the Development folder with Automation Developer role using explicit path.

### Example 3
```powershell
PS Orch1:\> Add-OrchFolderUser -Recurse DirectoryUser admin.user@company.com *Administrator*
```

Assigns an admin user to all folders recursively with all Administrator roles using wildcard.

### Example 4
```powershell
PS Orch1:\Production> Add-OrchFolderUser DirectoryUser developer1@company.com, developer2@company.com "Automation Developer", "Automation User"
```

Assigns multiple directory users to the current folder with multiple roles.

### Example 5
```powershell
PS Orch1:\> Get-OrchUser *developer* | Add-OrchFolderUser -Path .\Development DirectoryUser "Automation Developer"
```

Pipes users containing "developer" and assigns them to the Development folder with developer role.

### Example 6
```powershell
PS Orch1:\> Add-OrchFolderUser -Path .\QA, .\Staging -Depth 1 DirectoryGroup "QA Team" "Automation User" -WhatIf
```

Shows what would happen when assigning a directory group to QA and Staging folders with depth limit using -WhatIf for safety.

## PARAMETERS

### -Path
Specifies the target folder. If not specified, the current folder will be targeted.

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
{{ Fill ProgressAction Description }}

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

### -Roles
Specifies the roles to be added to the users.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases: FolderRoles

Required: False
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -UserName
Specifies the UserName of the users to be assigned.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
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

### -Depth
Specifies the depth for recursion into the target folders. A depth of 0 indicates the current location only, with no subfolders included.

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

### -Recurse
Specifies that the operation should include the target folder and all its subfolders.

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

### -Type
Specifies the type of user to assign. Valid values are:
- DirectoryUser: Individual users from Active Directory
- DirectoryRobot: Robot accounts from Active Directory
- DirectoryGroup: Groups from Active Directory
- DirectoryExternalApplication: External applications from Active Directory

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### System.Object
## NOTES
This is a folder entity cmdlet. You must navigate to a folder using Set-Location or specify target folders using -Path, -Recurse, or -Depth parameters.

Users must already exist in the tenant before they can be assigned to folders. Use Get-OrchUser to verify user existence and Get-OrchFolderUser to verify successful assignment.

## RELATED LINKS

[Get-OrchFolderUser](Get-OrchFolderUser.md)

[Remove-OrchFolderUser](Remove-OrchFolderUser.md)

[Get-OrchUser](Get-OrchUser.md)

[Get-OrchRole](Get-OrchRole.md)
