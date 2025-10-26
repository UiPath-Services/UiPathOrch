---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Remove-OrchPersonalWorkspace

## SYNOPSIS
Removes personal workspace folders from UiPath Orchestrator.

## SYNTAX

```
Remove-OrchPersonalWorkspace [[-Name] <String[]>] [[-OwnerName] <String[]>] [-Path <String[]>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The Remove-OrchPersonalWorkspace cmdlet permanently removes personal workspace folders from UiPath Orchestrator. Personal workspaces are dedicated folders that provide users with individual environments for developing, testing, and managing their automation projects.

This cmdlet removes the actual folder structure and all contained content, including processes, assets, and other automation artifacts stored within the personal workspace. This operation is irreversible and permanently deletes the workspace data.

The cmdlet operates at the folder level within Orchestrator, targeting specific personal workspace folders by name or owner. Use the -Name parameter to specify workspace folder names directly, or the -OwnerName parameter to target workspaces by their owning user. Both parameters support wildcard patterns for bulk operations.

This is a folder-level entity operation that requires navigation to the appropriate folder path or specification of the target path using the -Path parameter. The operation requires appropriate permissions to delete folders within the Orchestrator environment.

Primary Endpoint: GET /odata/PersonalWorkspaces, DELETE /odata/Folders({folderId})

OAuth required scopes: OR.Folders

Required permissions: Units.View, (Units.Delete or SubFolders.Delete)

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Remove-OrchPersonalWorkspace -OwnerName john.doe -WhatIf
```

Shows what would happen when removing the personal workspace owned by john.doe.

### Example 2
```powershell
PS Orch1:\> Remove-OrchPersonalWorkspace "john.doe Personal Workspace" -Confirm
```

Removes the specific personal workspace folder with confirmation prompt.

### Example 3
```powershell
PS C:\> Remove-OrchPersonalWorkspace Orch1: -OwnerName temp.user1, temp.user2 -WhatIf
```

Shows what would happen when removing personal workspaces for multiple temporary users in the Orch1 tenant.

### Example 4
```powershell
PS Orch1:\> Remove-OrchPersonalWorkspace -OwnerName *contractor* -Confirm
```

Removes personal workspaces for all users with usernames containing contractor with confirmation prompts.

### Example 5
```powershell
PS Orch1:\> Get-OrchPersonalWorkspace | Where-Object {$_.LastModified -lt (Get-Date).AddDays(-90)} | Remove-OrchPersonalWorkspace -WhatIf
```

Shows what would happen when removing personal workspaces that haven''t been modified in the last 90 days.

### Example 6
```powershell
PS C:\> Remove-OrchPersonalWorkspace -Path Orch1:, Orch2: -OwnerName inactive.user1 -Confirm
```

Removes personal workspace for inactive.user1 across multiple tenants with confirmation.

## PARAMETERS

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

### -Name
Specifies the name of the personal workspace folders to be removed. Supports wildcard patterns for bulk operations.

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

### -OwnerName
Specifies the owner name (username) of the personal workspace folders to be removed. Supports wildcard patterns for bulk operations.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases: UserName

Required: False
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
Specifies the target tenant drives. If not specified, the current drive will be targeted. For folder-level operations requiring path specification.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]
## OUTPUTS

### UiPath.PowerShell.Entities.PersonalWorkspace
## NOTES
This cmdlet performs a folder-level entity operation that permanently removes personal workspace folders and all contained content. The operation is irreversible and requires appropriate folder deletion permissions. Personal workspaces provide dedicated development environments for users. Always use -WhatIf to preview the operation before execution, especially when using wildcard patterns. The operation requires Units.Delete or SubFolders.Delete permissions depending on the folder hierarchy.

## RELATED LINKS

[Get-OrchPersonalWorkspace](Get-OrchPersonalWorkspace.md)

[Enable-OrchPersonalWorkspace](Enable-OrchPersonalWorkspace.md)

[Disable-OrchPersonalWorkspace](Disable-OrchPersonalWorkspace.md)

[Get-OrchFolder](Get-OrchFolder.md)
