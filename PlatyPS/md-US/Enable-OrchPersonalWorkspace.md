---
external help file: UiPath.PowerShell.OrchProvider.dll-help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Enable-OrchPersonalWorkspace

## SYNOPSIS
Enables personal workspace and robot session access for specified users.

## SYNTAX

```
Enable-OrchPersonalWorkspace [-UserName] <String[]> [-Path <String[]>] [-ProgressAction <ActionPreference>]
 [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The Enable-OrchPersonalWorkspace cmdlet enables both personal workspace functionality and robot session access for specified users within UiPath Orchestrator. This operation grants users comprehensive development and execution capabilities in their individual environments.

Personal workspaces provide users with dedicated environments for developing, testing, and managing their automation projects independently from shared organizational folders. The cmdlet also enables robot session access, allowing users to run attended automation processes within their workspace environment.

Internally, this cmdlet calls Update-OrchUser with both -MayHavePersonalWorkspace True and -MayHaveRobotSession True parameters. This dual configuration ensures users receive both workspace development capabilities and execution permissions, supporting the complete automation development lifecycle.

This cmdlet operates at the tenant level, managing user workspace access across the organization. Users with enabled personal workspaces can access both their dedicated workspace and shared organizational folders. The operation automatically clears the Orchestrator cache to ensure immediate access to the new capabilities.

Primary Endpoint: PATCH /odata/Users

OAuth required scopes: OR.Users or OR.Users.Write

Required permissions: Users.Edit

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Enable-OrchPersonalWorkspace john.doe
```

Enables personal workspace and robot session access for user john.doe in the current tenant.

### Example 2
```powershell
PS C:\> Enable-OrchPersonalWorkspace -Path Orch1: jane.smith -WhatIf
```

Shows what would happen when enabling personal workspace for jane.smith in the Orch1 tenant.

### Example 3
```powershell
PS Orch1:\> Enable-OrchPersonalWorkspace developer1, developer2, developer3
```

Enables personal workspaces for multiple developers in the current tenant.

### Example 4
```powershell
PS C:\> Enable-OrchPersonalWorkspace -Path Orch1: *citizen* -Confirm
```

Enables personal workspaces for all users with usernames containing "citizen" with confirmation prompts.

### Example 5
```powershell
PS Orch1:\> Get-OrchUser -Type Internal | Where-Object {$_.Department -eq "RPA"} | Enable-OrchPersonalWorkspace
```

Gets all internal users from RPA department and enables their personal workspaces using pipeline input.

### Example 6
```powershell
PS C:\> Enable-OrchPersonalWorkspace -Path Orch1:, Orch2: newuser1, newuser2
```

Enables personal workspaces for newuser1 and newuser2 across multiple tenants.

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

### -Path
Specifies the target tenant drives. If not specified, the current tenant will be targeted. For tenant-level operations targeting multiple tenants.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -UserName
Specifies the usernames of users whose personal workspaces should be enabled. Supports wildcard patterns for bulk operations.

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
Usernames can be piped to this cmdlet.

### UiPath.PowerShell.Entities.User
User objects from Get-OrchUser can be piped to this cmdlet. The UserName property will be automatically mapped to the -UserName parameter via ByPropertyName binding.

## OUTPUTS

### None
This cmdlet does not generate any output.

## NOTES
This cmdlet is a tenant-level entity operation that sets both MayHavePersonalWorkspace and MayHaveRobotSession properties to True for specified users. Personal workspaces provide dedicated development environments separate from shared organizational folders. The robot session capability allows users to execute attended automation processes within their workspace environment. The operation automatically clears the Orchestrator cache to ensure immediate availability of new permissions.

## RELATED LINKS

[Disable-OrchPersonalWorkspace](Disable-OrchPersonalWorkspace.md)

[Get-OrchPersonalWorkspace](Get-OrchPersonalWorkspace.md)

[Get-OrchUser](Get-OrchUser.md)

[Update-OrchUser](Update-OrchUser.md)
