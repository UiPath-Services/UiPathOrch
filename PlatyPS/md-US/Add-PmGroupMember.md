---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Add-PmGroupMember

## SYNOPSIS
Adds members to groups.

## SYNTAX

```
Add-PmGroupMember [-GroupName] <String[]> [[-Type] <String[]>] [-UserName] <String[]> [-Path <String[]>]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
The Add-PmGroupMember cmdlet adds users to groups within UiPath Platform Management. This cmdlet manages group membership at the organization level, allowing you to assign users to groups that control access to various platform features and resources.

Groups in Platform Management define access permissions and roles that can be inherited by members. Adding users to groups grants them the permissions and capabilities associated with those groups across the organization.

Use the -GroupName parameter to specify which groups to add members to, and the -UserName parameter to specify which users to add. The -Type parameter allows filtering by user types. The -Path parameter enables working with multiple platform instances.

This is a tenant entity cmdlet. The -Path parameter specifies drive names (e.g., Orch1:, Orch2:) for targeting specific platform instances when working with multiple environments.

Primary Endpoint: POST /api/Directory/BulkResolveByName/{tenantId}, GET /api/Group/{tenantId}/{groupId}, PUT /api/Group/{groupId}

OAuth required scopes: PM.Group

Required permissions: Group management permissions at the organization level

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Add-PmGroupMember Administrators john.doe
```

Adds user john.doe to the Administrators group in the current platform instance.

### Example 2
```powershell
PS C:\> Add-PmGroupMember -Path Orch1:, Orch2: "Power Users" jane.smith
```

Adds user jane.smith to the "Power Users" group in both Orch1 and Orch2 platform instances.

### Example 3
```powershell
PS Orch1:\> Add-PmGroupMember Developers admin.user, lead.user -WhatIf
```

Shows what would happen when adding admin.user and lead.user to the Developers group.

### Example 4
```powershell
PS C:\> Add-PmGroupMember -GroupName "Business Users" -UserName *analyst* -Type User
```

Adds all users with usernames containing "analyst" to the "Business Users" group, filtering by User type.

### Example 5
```powershell
PS Orch1:\> Get-PmUser | Where-Object {$_.Email -like "*@uipath.com"} | Add-PmGroupMember -GroupName Employees
```

Gets all users with contoso.com email domain and adds them to the Employees group using pipeline input.

### Example 6
```powershell
PS C:\> Add-PmGroupMember -Path Orch1: Support, "Help Desk" support.user -Confirm
```

Adds support.user to both Support and "Help Desk" groups with confirmation prompts.

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

### -GroupName
Specifies the name of the groups to add members to.

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
Specifies the platform instances to target.

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

### -Type
Specifies the user types to filter by when adding members.

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

### -UserName
Specifies the usernames of the users to add to the groups.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 2
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

### System.String[]
## OUTPUTS

### System.Object
## NOTES
This is a tenant entity cmdlet. The -Path parameter specifies drive names (e.g., Orch1:, Orch2:) for targeting specific platform instances.

This cmdlet operates at the organization level through Platform Management. Groups define permissions and access rights that are inherited by members. Use wildcards for efficient bulk operations and -WhatIf for testing before actual execution.

## RELATED LINKS

[Get-PmGroup](Get-PmGroup.md)

[Remove-PmGroupMember](Remove-PmGroupMember.md)

[Get-PmUser](Get-PmUser.md)

[Get-PmGroupMember](Get-PmGroupMember.md)
