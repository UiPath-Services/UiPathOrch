---
external help file: UiPath.PowerShell.OrchProvider.dll-help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Disable-OrchPersonalWorkspace

## SYNOPSIS
Disables personal workspace access for specified users.

## SYNTAX

```
Disable-OrchPersonalWorkspace [-UserName] <String[]> [-Path <String[]>] [-ProgressAction <ActionPreference>]
 [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The Disable-OrchPersonalWorkspace cmdlet disables personal workspace functionality for specified users within UiPath Orchestrator. Personal workspaces provide users with dedicated environments for developing, testing, and managing their automation projects independently from shared organizational folders.

Disabling personal workspace prevents users from accessing their individual workspace environments while preserving their existing workspace data. This is useful for enforcing organizational policies, managing resource allocation, temporarily restricting user access during maintenance, or when implementing new security policies.

Internally, this cmdlet calls Update-OrchUser with -MayHavePersonalWorkspace False parameter. Unlike the Enable counterpart, this cmdlet only affects the personal workspace permission and does not modify robot session access. This targeted approach allows for fine-grained control over user capabilities.

This cmdlet operates at the tenant level, managing user workspace access across the organization. Users with disabled personal workspaces will be restricted to shared organizational folders for their automation activities. The operation automatically clears the Orchestrator cache to ensure immediate restriction of workspace access.

Primary Endpoint: PATCH /odata/Users

OAuth required scopes: OR.Users or OR.Users.Write

Required permissions: Users.Edit

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Disable-OrchPersonalWorkspace john.doe
```

Disables personal workspace access for user john.doe in the current tenant.

### Example 2
```powershell
PS C:\> Disable-OrchPersonalWorkspace -Path Orch1: jane.smith -WhatIf
```

Shows what would happen when disabling personal workspace for jane.smith in the Orch1 tenant.

### Example 3
```powershell
PS Orch1:\> Disable-OrchPersonalWorkspace contractor1, contractor2, contractor3
```

Disables personal workspaces for multiple contractor users in the current tenant.

### Example 4
```powershell
PS C:\> Disable-OrchPersonalWorkspace -Path Orch1: *temp* -Confirm
```

Disables personal workspaces for all users with usernames containing "temp" with confirmation prompts.

### Example 5
```powershell
PS Orch1:\> Get-OrchUser -Type External | Disable-OrchPersonalWorkspace -WhatIf
```

Gets all external users and shows what would happen when disabling their personal workspaces using pipeline input.

### Example 6
```powershell
PS C:\> Disable-OrchPersonalWorkspace -Path Orch1:, Orch2: inactive.user1, inactive.user2
```

Disables personal workspaces for inactive users across multiple tenants.

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
Specifies the usernames of users whose personal workspaces should be disabled. Supports wildcard patterns for bulk operations.

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

## OUTPUTS

### System.Object
## NOTES
This cmdlet is a tenant-level entity operation that sets the MayHavePersonalWorkspace property to False for specified users. Unlike Enable-OrchPersonalWorkspace, this cmdlet only affects workspace access and does not modify robot session permissions. Personal workspaces provide dedicated development environments separate from shared organizational folders. Disabling workspace access restricts users to shared organizational folders while preserving existing workspace data. The operation automatically clears the Orchestrator cache to ensure immediate restriction of workspace access.

## RELATED LINKS

[Enable-OrchPersonalWorkspace](Enable-OrchPersonalWorkspace.md)

[Get-OrchPersonalWorkspace](Get-OrchPersonalWorkspace.md)

[Get-OrchUser](Get-OrchUser.md)

[Update-OrchUser](Update-OrchUser.md)
