---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# New-PmGroup

## SYNOPSIS
Creates groups in Platform Management.

## SYNTAX

```
New-PmGroup [-GroupName] <String[]> [-Path <String[]>] [-ProgressAction <ActionPreference>] [-WhatIf]
 [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The New-PmGroup cmdlet creates groups in UiPath Platform Management. Groups are used to organize users and manage permissions at the organization level across multiple tenants.

**This is an organization entity cmdlet.** It operates at the Platform Management level and manages groups across the entire organization. Use the -Path parameter to specify target tenants if needed.

Groups created through Platform Management provide centralized user and permission management capabilities that span multiple tenants within the same organization, as mentioned in the UiPathOrch 0.9.13.0 release notes regarding shared caches across tenants.

Primary Endpoint: POST /api/Group
OAuth required scopes: OR.Users or OR.Users.Write
Required permissions: Users.Create

## EXAMPLES

### Example 1
```powershell
New-PmGroup Administrators
```

Creates a new group named \"Administrators\" using positional parameters.

### Example 2
```powershell
New-PmGroup Developers, Testers, DevOps -WhatIf
```

Shows what would happen when creating multiple groups using positional parameters.

### Example 3
```powershell
New-PmGroup -Path Orch1: \"Finance Team\" -Confirm
```

Creates a group with confirmation prompt in the specified tenant context.

## PARAMETERS

### -Confirm
Prompts you for confirmation before running the cmdlet.

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

### -GroupName
Specifies the name(s) of the group(s) to create. Group names must be unique within the organization.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases: Name

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
Specifies the path(s) to target tenant(s). Use drive names like Orch1:, Orch2: to specify tenant context.

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

### -WhatIf
Shows what would happen if the cmdlet runs. The cmdlet is not run.

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

## RELATED LINKS

[Get-PmGroup](Get-PmGroup.md)
[Remove-PmGroup](Remove-PmGroup.md)
[Copy-PmGroup](Copy-PmGroup.md)
