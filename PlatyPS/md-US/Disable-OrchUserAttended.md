---
external help file: UiPath.PowerShell.OrchProvider.dll-help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Disable-OrchUserAttended

## SYNOPSIS
Disables attended robot session capabilities for specified users.

## SYNTAX

```
Disable-OrchUserAttended [-UserName] <String[]> [-Path <String[]>] [-ProgressAction <ActionPreference>]
 [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The Disable-OrchUserAttended cmdlet disables attended robot session capabilities for specified users within UiPath Orchestrator. Attended robots run on user desktops and are triggered manually or through user interaction, as opposed to unattended robots that run automatically on server machines.

Disabling attended robot capabilities prevents users from establishing robot sessions and running attended automation processes on their local machines. This effectively blocks attended automation execution while maintaining user access to other Orchestrator features such as viewing jobs, accessing shared folders, and managing assets based on their role permissions.

Internally, this cmdlet calls Update-OrchUser with -MayHaveRobotSession False parameter. This targeted approach specifically controls robot session capabilities without affecting other user permissions like personal workspace access or general Orchestrator functionality.

This cmdlet operates at the tenant level, managing user robot session capabilities across the organization. Unlike its Personal Workspace counterpart, this cmdlet does not automatically clear the cache, making it a lighter operation focused solely on robot session control.

Primary Endpoint: PATCH /odata/Users

OAuth required scopes: OR.Users or OR.Users.Write

Required permissions: Users.Edit

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Disable-OrchUserAttended john.doe
```

Disables attended robot session capabilities for user john.doe in the current tenant.

### Example 2
```powershell
PS C:\> Disable-OrchUserAttended -Path Orch1: jane.smith -WhatIf
```

Shows what would happen when disabling attended capabilities for jane.smith in the Orch1 tenant.

### Example 3
```powershell
PS Orch1:\> Disable-OrchUserAttended contractor1, contractor2, contractor3
```

Disables attended robot capabilities for multiple contractor users in the current tenant.

### Example 4
```powershell
PS C:\> Disable-OrchUserAttended -Path Orch1: *external* -Confirm
```

Disables attended capabilities for all users with usernames containing "external" with confirmation prompts.

### Example 5
```powershell
PS Orch1:\> Get-OrchUser -Type External | Disable-OrchUserAttended -WhatIf
```

Gets all external users and shows what would happen when disabling their attended capabilities using pipeline input.

### Example 6
```powershell
PS C:\> Disable-OrchUserAttended -Path Orch1:, Orch2: viewer.user1, viewer.user2
```

Disables attended capabilities for viewer users across multiple tenants.

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
Specifies the usernames of users whose attended robot capabilities should be disabled. Supports wildcard patterns for bulk operations.

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
This cmdlet is a tenant-level entity operation that sets the MayHaveRobotSession property to False for specified users. Attended robot capabilities allow users to run automation processes on their desktop machines with user interaction. Disabling these capabilities prevents robot session establishment and attended automation execution while preserving other user permissions. This cmdlet operates independently of personal workspace permissions and does not automatically clear the Orchestrator cache.

## RELATED LINKS

[Enable-OrchUserAttended](Enable-OrchUserAttended.md)

[Get-OrchUser](Get-OrchUser.md)

[Update-OrchUser](Update-OrchUser.md)

[Get-OrchMachineSession](Get-OrchMachineSession.md)
