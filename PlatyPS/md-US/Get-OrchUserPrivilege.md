---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchUserPrivilege

## SYNOPSIS
Retrieves user privileges based on the union of privileges model.

## SYNTAX

```
Get-OrchUserPrivilege [[-UserName] <String[]>] [-Path <String[]>] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION
The `Get-OrchUserPrivilege` cmdlet retrieves comprehensive user privilege information from UiPath Orchestrator. This cmdlet provides detailed insights into user permissions, role assignments, access levels, and session privileges across the Orchestrator environment.

The cmdlet returns extensive privilege information including explicit and inherited roles, access permissions, attended session privileges, personal workspace permissions, and update policies. Each privilege category shows explicit assignments, inherited permissions from group memberships, and the effective (final) permissions that apply to the user.

This information is essential for understanding the complete permission landscape for users, troubleshooting access issues, auditing user privileges, and ensuring proper security configuration. The privilege data includes the inheritance chain showing which groups contribute to inherited permissions.

The privilege information covers multiple dimensions including roles (with explicit, inherited, and effective assignments), access levels (None, Standard, PersonalWorkspace), session permissions (attendedSession, personalWorkspace), and update policies.

Primary Endpoint: GET /api/Users/GetPrivileges?userId={userId}

OAuth required scopes: [PLACEHOLDER - User privileges scopes]

Required permissions: [PLACEHOLDER - User privileges view permissions]

Primary Endpoint: GET /api/Users/GetPrivileges?userId={userId}

OAuth required scopes:

Required permissions:

## EXAMPLES

### Example 1
```powershell
PS C:\> {{ Add example code here }}
```

{{ Add example description here }}

## PARAMETERS

### -Path
Specifies the name of the target drives. If not specified, the current drive will be targeted.

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
{{ Fill UserName Description }}

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
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

### UiPath.PowerShell.Entities.UserPrivilege
## NOTES

## RELATED LINKS
