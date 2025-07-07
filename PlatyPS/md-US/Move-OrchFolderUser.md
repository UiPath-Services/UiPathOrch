---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Move-OrchFolderUser

## SYNOPSIS
Moves folder users between folders.

## SYNTAX

```
Move-OrchFolderUser [-UserName] <String[]> [[-Destination] <String[]>] [-KeepSource <String>]
 [-Path <String[]>] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The Move-OrchFolderUser cmdlet moves users from one folder to another, transferring their access rights and role assignments between folder contexts.

This is a folder entity cmdlet. To use this cmdlet, you must first navigate to the target folder using Set-Location (cd), or specify the target folders using -Path, -Recurse, or -Depth parameters.

By default, moving a user removes their access from the source folder and grants access to the destination folder. Use -KeepSource to maintain access to the original folder while adding access to the destination folder.

Primary Endpoint: POST /odata/FolderUsers/Move

OAuth required scopes: OR.Folders or OR.Folders.Write

Required permissions: Folders.Edit

## EXAMPLES

### Example 1
```powershell
Move-OrchFolderUser "automation developers" -WhatIf
```

Shows what would happen when moving the "automation developers" user from the current folder.

### Example 2
```powershell
Move-OrchFolderUser "john.doe" -Destination "Orch1:\Development"
```

Moves john.doe from the current folder to the Development folder.

### Example 3
```powershell
Move-OrchFolderUser "jane.smith", "bob.jones" -Destination "Orch1:\Production"
```

Moves multiple users (jane.smith and bob.jones) to the Production folder.

### Example 4
```powershell
Move-OrchFolderUser "*developer*" -Destination "Orch1:\Development"
```

Moves all users whose names contain "developer" to the Development folder.

### Example 5
```powershell
Move-OrchFolderUser "service.account" -Destination "Orch1:\Testing" -KeepSource true
```

Moves service.account to the Testing folder while keeping access to the current folder.

### Example 6
```powershell
Move-OrchFolderUser -Path "Orch1:\Legacy" "migration.user" -Destination "Orch1:\Modern" -Confirm
```

Moves migration.user from the Legacy folder to the Modern folder with confirmation.

### Example 7
```powershell
Get-OrchFolderUser | Where-Object {$_.UserEntity.Type -eq "DirectoryGroup"} | Move-OrchFolderUser -Destination "Orch1:\GroupManagement"
```

Moves all directory group users to the GroupManagement folder. User information is passed via pipeline using ByPropertyName binding.

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

### -Destination
Specifies the destination folder path where users will be moved.

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

### -KeepSource
Specifies whether to keep the user in the source folder after moving to the destination. Set to "true" to maintain dual folder access.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

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

### -UserName
Specifies the names of the users to be moved between folders.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]
User names can be piped to this cmdlet.

### UiPath.PowerShell.Entities.UserRoles
Folder user objects can be piped to this cmdlet. The UserName property will be automatically mapped to the -UserName parameter via ByPropertyName binding.

## OUTPUTS

### System.Object

## NOTES

## RELATED LINKS
