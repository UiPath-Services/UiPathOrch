---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Copy-OrchRole

## SYNOPSIS
Copies roles between tenants.

## SYNTAX

```
Copy-OrchRole -Name <String[]> [-Destination] <String[]> [-Path <String>] [-ProgressAction <ActionPreference>]
 [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Copies roles from the current tenant to one or more destination tenants. This cmdlet enables role configuration replication across multiple UiPath Orchestrator environments.

Only custom (non-static) roles can be copied. Static roles are built-in and cannot be replicated as they already exist in all tenants.

The cmdlet preserves role permissions, descriptions, and other configuration details during the copy operation.

Primary Endpoint: GET /odata/Roles, POST /odata/Roles

OAuth required scopes: OR.Users

Required permissions: Roles.View, Roles.Create

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Copy-OrchRole CustomRole1 Orch2: -WhatIf
```

Shows what would happen when copying a role without actually copying it.

### Example 2
```powershell
PS Orch1:\> Copy-OrchRole CustomRole1 Orch2:
```

Copies CustomRole1 from the current tenant to Orch2.

### Example 3
```powershell
PS Orch1:\> Copy-OrchRole TestRole Orch2:, Orch3: -Confirm
```

Copies TestRole to multiple tenants with confirmation prompts.

### Example 4
```powershell
PS Orch1:\> Copy-OrchRole Custom* Orch2:
```

Copies all roles starting with "Custom" to Orch2.

### Example 5
```powershell
PS Orch1:\> Get-OrchRole | Where-Object {$_.IsStatic -eq $false} | Copy-OrchRole -Destination Orch2: -WhatIf
```

Shows which custom (non-static) roles would be copied using pipeline input.

## PARAMETERS

### -Destination
Specifies the destination tenants by drive name. Use comma-separated values for multiple destinations.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Path
Specifies the source tenant by drive name. If not specified, uses the current tenant.

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
Prompts for confirmation before copying roles. Recommended when copying multiple roles.

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
Shows what would happen if the cmdlet runs without actually copying roles. Recommended for safety verification.

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
Specifies the names of roles to copy. Supports wildcards and multiple values.

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

### None
## OUTPUTS

### UiPath.PowerShell.Entities.Role
## NOTES
Role entities are tenant-scoped and this cmdlet enables cross-tenant replication.

Only custom roles (IsStatic = $false) can be copied. Static roles are built-in and already exist in all tenants.

The copy operation preserves all role configuration including permissions, descriptions, and metadata.

Use -WhatIf to preview copy operations before execution, especially when using wildcards that might match multiple roles.

Ensure you have appropriate permissions in both source and destination tenants.

## RELATED LINKS

[Get-OrchRole](Get-OrchRole.md)

[Set-OrchRole](Set-OrchRole.md)

[Remove-OrchRole](Remove-OrchRole.md)

[New-OrchRole](New-OrchRole.md)
