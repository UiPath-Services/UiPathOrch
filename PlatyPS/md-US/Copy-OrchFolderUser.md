---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Copy-OrchFolderUser

## SYNOPSIS
Copies folder user assignments to destination folders.

## SYNTAX

```
Copy-OrchFolderUser [-UserName] <String[]> [-Destination] <String> [-Type <String[]>] [-Path <String>]
 [-Recurse] [-Depth <UInt32>] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The Copy-OrchFolderUser cmdlet copies user assignments from source folders to destination folders within UiPath Orchestrator tenants or across different tenants. This cmdlet replicates the user-to-folder relationships and their associated roles, ensuring that the same users have equivalent access in the destination folders.

The cmdlet supports both intra-tenant copying (within the same tenant) and inter-tenant copying (between different tenants). This is useful for maintaining consistent user access patterns across different environments or for setting up parallel folder structures with identical permissions.

Use the -UserName parameter to specify which users to copy assignments for and the -Destination parameter to specify the target folder. The -Type parameter allows filtering by user types. The cmdlet copies the user assignments and their roles rather than the users themselves.

This is a folder entity cmdlet. Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters. The -Recurse parameter enables copying user assignments from all subfolders, maintaining the folder structure in the destination.

Primary Endpoint: GET /odata/Folders/UiPath.Server.Configuration.OData.GetUsersForFolder(key={key},includeInherited={includeInherited}), GET /api/DirectoryService/SearchForUsersAndGroups, POST /odata/Folders/UiPath.Server.Configuration.OData.AssignDirectoryUser

OAuth required scopes: OR.Folders

Required permissions: Units.View, SubFolders.View, Units.Edit, SubFolders.Edit, Assets.Create, Assets.Edit

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Copy-OrchFolderUser john.doe \Production
```

Copies john.doe's user assignment from the current folder (Development) to the Production folder within the same tenant.

### Example 2
```powershell
PS C:\> Copy-OrchFolderUser -Path Orch1:\Development jane.smith Orch2:\Production
```

Copies jane.smith's user assignment from Orch1:\Development to Orch2:\Production, demonstrating inter-tenant user assignment copying.

### Example 3
```powershell
PS Orch1:\Development> Copy-OrchFolderUser admin.*, lead.* \Production -WhatIf
```

Shows what would happen when copying multiple user assignments with usernames matching admin.* or lead.* patterns from the current folder to the Production folder.

### Example 4
```powershell
PS C:\> Copy-OrchFolderUser -Path Orch1:\Development -Type DirectoryUser *manager* Orch2:\Production
```

Copies all user assignments containing manager in their username from Orch1:\Development to Orch2:\Production, filtering by User type.

### Example 5
```powershell
PS Orch1:\> Copy-OrchFolderUser -Recurse *admin* Orch2:\ -WhatIf
```

Shows what would happen when copying all user assignments containing admin from all subfolders recursively to Orch2:\.

### Example 6
```powershell
PS Orch1:\Development> Get-OrchFolderUser *developer* | Copy-OrchFolderUser -Destination Orch2:\Production
```

Gets all folder user assignments containing developer in their usernames and copies them to Orch2:\Production using pipeline input.

## PARAMETERS

### -Destination
Specifies the destination folder where user assignments should be copied.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
Specifies the source folder. If not specified, the current folder will be used as the source.

```yaml
Type: String
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

### -UserName
Specifies the UserName of the users whose assignments should be copied.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
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

### -Recurse
Specifies that user assignments should be copied from all subfolders recursively, maintaining the folder structure in the destination.

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
Specifies the user types to filter by when copying assignments.

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
This is a folder entity cmdlet. Use Set-Location cmdlet (cd command) to navigate to the target folder first, or specify the target folders using -Path, -Recurse, or -Depth parameters.

This cmdlet copies user assignments and their roles, not the users themselves. The users must already exist in the target tenant when copying across tenants. Use -Type parameter to filter by specific user types.

## RELATED LINKS

[Get-OrchFolderUser](Get-OrchFolderUser.md)

[Add-OrchRoleToFolderUser](Add-OrchRoleToFolderUser.md)

[Remove-OrchRoleFromFolderUser](Remove-OrchRoleFromFolderUser.md)
