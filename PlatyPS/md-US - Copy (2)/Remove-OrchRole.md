---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Remove-OrchRole

## SYNOPSIS
Removes roles from the Orchestrator.

## SYNTAX

```
Remove-OrchRole -Name <String[]> [-Path <String[]>] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## DESCRIPTION
Permanently removes roles from UiPath Orchestrator tenants. Roles define permissions and access levels for users and groups within the organization.

Only custom roles that are not currently assigned to users or groups can be removed. Static (built-in) roles cannot be deleted as they are essential for Orchestrator functionality.

The cmdlet supports safety features like -WhatIf to preview operations and -Confirm to request confirmation before deletion.

Primary Endpoint: DELETE /odata/Roles({roleId})

OAuth required scopes: OR.Users

Required permissions: Roles.Delete

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Remove-OrchRole CustomRole1 -WhatIf
```

Shows what would happen when removing the role without actually performing the deletion.

### Example 2
```powershell
PS Orch1:\> Remove-OrchRole Test* -Confirm
```

Removes roles whose names start with "Test", prompting for confirmation before each removal.

### Example 3
```powershell
PS Orch1:\> Remove-OrchRole TemporaryRole -Path Orch1:, Orch2:
```

Removes the role from multiple tenants.

### Example 4
```powershell
PS Orch1:\> Get-OrchRole | Where-Object {$_.IsStatic -eq $false} | Remove-OrchRole -WhatIf
```

Shows which custom (non-static) roles would be removed using pipeline input.

## PARAMETERS

### -Path
Specifies target tenants by drive name. Use comma-separated values for multiple tenants. If not specified, targets the current tenant.

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

### -ProgressAction
Controls how progress information is displayed during cmdlet execution.

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

### -Confirm
Prompts for confirmation before running the cmdlet. Recommended for destructive operations.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: cf

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -WhatIf
Shows what would happen if the cmdlet runs without actually performing the operation. Recommended for safety verification.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: wi

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -Name
Specifies the names of roles to remove. Supports wildcards and multiple values.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]
Role names can be piped to this cmdlet.

### UiPath.PowerShell.Entities.Role
Role objects can be piped to this cmdlet. The Name property will be automatically mapped to the -Name parameter via ByPropertyName binding.

## OUTPUTS

### None
This cmdlet does not generate output.

## NOTES
Role entities are tenant-scoped and operate across the entire tenant.

Before removing roles, ensure they are not assigned to any users or groups. Use Get-OrchRole to check assignments and Remove-OrchRoleFromUser or Remove-OrchRoleFromFolderUser to remove assignments.

Static roles (IsStatic = $true) cannot be removed as they are essential for Orchestrator functionality.

The removal operation is irreversible. Use -WhatIf to preview operations before execution.

## RELATED LINKS

[Get-OrchRole](Get-OrchRole.md)

[Set-OrchRole](Set-OrchRole.md)

[Copy-OrchRole](Copy-OrchRole.md)

[Remove-OrchRoleFromUser](Remove-OrchRoleFromUser.md)
