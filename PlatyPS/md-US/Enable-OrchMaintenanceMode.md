---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Enable-OrchMaintenanceMode

## SYNOPSIS
Enables maintenance mode of unattended sessions.

## SYNTAX

```
Enable-OrchMaintenanceMode [[-MachineName] <String[]>] [[-HostMachineName] <String[]>]
 [[-ServiceUserName] <String[]>] [[-SessionId] <Int64[]>] [-Force] [-Path <String[]>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The Enable-OrchMaintenanceMode cmdlet enables maintenance mode for unattended robot sessions within UiPath Orchestrator. This cmdlet prevents robot sessions from accepting new jobs while allowing currently running jobs to complete gracefully, making it ideal for maintenance activities or system updates.

Maintenance mode is essential for performing maintenance tasks on robot machines without interrupting running automation processes. When enabled, the robot sessions will finish their current jobs but will not accept new jobs from the queue until maintenance mode is disabled.

Use the various filtering parameters to target specific robot sessions for maintenance mode activation. You can filter by MachineName, HostMachineName, ServiceUserName, or specific SessionId values. The -Path parameter allows targeting specific folders.

The -Force parameter can be used to bypass confirmation prompts when enabling maintenance mode for multiple sessions simultaneously.

Primary Endpoint: GET /odata/Sessions/UiPath.Server.Configuration.OData.GetMachineSessionRuntimes

OAuth required scopes: OR.Robots or OR.Robots.Write

Required permissions: Machines.Edit

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Development> Enable-OrchMaintenanceMode -MachineName Robot01
```

Enables maintenance mode for all sessions on machine Robot01 in the current folder.

### Example 2
```powershell
PS C:\> Enable-OrchMaintenanceMode -Path Orch1:\Production -HostMachineName Server01
```

Enables maintenance mode for all sessions on host machine Server01 in the Production folder.

### Example 3
```powershell
PS Orch1:\Development> Enable-OrchMaintenanceMode -ServiceUserName ServiceAccount01, ServiceAccount02 -WhatIf
```

Shows what would happen when enabling maintenance mode for sessions using ServiceAccount01 and ServiceAccount02.

### Example 4
```powershell
PS C:\> Enable-OrchMaintenanceMode -Path Orch1:\Development -SessionId 12345, 67890 -Force
```

Enables maintenance mode for specific sessions with IDs 12345 and 67890 in the Development folder without confirmation prompts.

### Example 5
```powershell
PS Orch1:\Production> Enable-OrchMaintenanceMode -MachineName *Robot* -Confirm
```

Enables maintenance mode for all sessions on machines with names containing Robot with confirmation prompts.

### Example 6
```powershell
PS C:\> Get-OrchUnattendedSession -MaintenanceMode $false | Enable-OrchMaintenanceMode -WhatIf
```

Gets all sessions not currently in maintenance mode and shows what would happen when enabling maintenance mode for them using pipeline input.

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

### -Force
Forces the operation without confirmation prompts.

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

### -HostMachineName
Specifies the host machine names to target for maintenance mode activation.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -MachineName
Specifies the machine names to target for maintenance mode activation.

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

### -Path
Specifies the target folders. If not specified, the current folder will be targeted.

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

### -ServiceUserName
Specifies the service user names to target for maintenance mode activation.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 2
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -SessionId
Specifies the session IDs to target for maintenance mode activation.

```yaml
Type: Int64[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 3
Default value: None
Accept pipeline input: True (ByPropertyName)
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

### None
## OUTPUTS

### System.Object
## NOTES
Maintenance mode prevents robot sessions from accepting new jobs while allowing current jobs to complete. This is essential for performing maintenance without interrupting running processes. Use filtering parameters to target specific sessions. The -Force parameter bypasses confirmation prompts for bulk operations.

## RELATED LINKS

[Disable-OrchMaintenanceMode](Disable-OrchMaintenanceMode.md)

[Get-OrchUnattendedSession](Get-OrchUnattendedSession.md)

[Get-OrchMachine](Get-OrchMachine.md)

[Get-OrchRobot](Get-OrchRobot.md)
