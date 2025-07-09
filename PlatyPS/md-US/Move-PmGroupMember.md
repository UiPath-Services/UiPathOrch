---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Move-PmGroupMember

## SYNOPSIS
Moves group members between Platform Management groups.

## SYNTAX

```
Move-PmGroupMember [-GroupName] <String> [-UserName] <String[]> [-Destination] <String[]>
 [-KeepSource <String>] [-Path <String[]>] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## DESCRIPTION
The Move-PmGroupMember cmdlet moves members from one Platform Management group to another. This cmdlet operates at the organization level and manages group memberships across the organization.

This is an organization entity cmdlet that calls the Platform Management API. It operates at the organization level, where multiple tenants can belong to the same organization. Group memberships are managed centrally and affect access across all tenants within the organization.

By default, moving a member removes them from the source group and adds them to the destination group. Use -KeepSource to maintain membership in the original group while adding to the destination group.

Primary Endpoint: POST /api/Directory/BulkResolveByName/{tenantId}, GET /api/Group/{tenantId}/{groupId}, PUT /api/Group/{groupId}

OAuth required scopes: PM.Group

Required permissions: Administration.Edit

## EXAMPLES

### Example 1
```powershell
Move-PmGroupMember "Administrators" "MyRobot4" "Everyone" -WhatIf
```

Shows what would happen when moving MyRobot4 from the Administrators group to the Everyone group.

### Example 2
```powershell
Move-PmGroupMember "Administrators" "MyRobot4" "Everyone"
```

Moves MyRobot4 from the Administrators group to the Everyone group.

### Example 3
```powershell
Move-PmGroupMember "Developers" "john.doe", "jane.smith" "TeamLead"
```

Moves multiple users (john.doe and jane.smith) from the Developers group to the TeamLead group.

### Example 4
```powershell
Move-PmGroupMember -GroupName "TestGroup" -UserName "*Robot*" -Destination "AutomationUsers"
```

Moves all members whose names contain "Robot" from TestGroup to AutomationUsers group.

### Example 5
```powershell
Move-PmGroupMember "Administrators" "service.account" "AutomationUsers" -KeepSource true
```

Moves service.account to AutomationUsers while keeping membership in the Administrators group.

### Example 6
```powershell
Move-PmGroupMember -Path Orch1:, Orch2: "OldGroup" "migration.user" "NewGroup" -Confirm
```

Moves migration.user from OldGroup to NewGroup across multiple tenants with confirmation.

### Example 7
```powershell
Get-PmGroupMember | Where-Object {$_.PathGroupName -like "*Temporary*"} | Move-PmGroupMember -Destination "PermanentGroup"
```

Moves all members from groups containing "Temporary" to the PermanentGroup. Group membership information is passed via pipeline using ByPropertyName binding.

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
Specifies the name of the destination groups where members will be moved.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 2
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -GroupName
Specifies the name of the source group from which members will be moved.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -KeepSource
Specifies whether to keep the member in the source group after moving to the destination. Set to "true" to maintain dual membership.

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
Specifies the name of the target tenant drives. The group membership changes are organization-wide regardless of which tenant drive is used.

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
Specifies the names of the users or robot accounts to be moved between groups.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
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

### System.String
### System.String[]
## OUTPUTS

### System.Object
## NOTES

## RELATED LINKS
