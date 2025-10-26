---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# New-PmUser

## SYNOPSIS
Creates users to the organization.

## SYNTAX

```
New-PmUser [-Email] <String> [-Name <String>] [-SurName <String>] [-DisplayName <String>] [-Type <String>]
 [-BypassBasicAuthRestriction <String>] [-InvitationAccepted <String>] [-GroupName <String[]>]
 [-Path <String[]>] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
このコマンドの詳細な説明。

Primary Endpoint: POST /api/User/BulkCreate

OAuth required scopes: PM.Group

Required permissions:

## EXAMPLES

### Example 1

```powershell
PS Orch1:\Shared> New-PmUser user@uipath.com
```

Creates a new user with the specified email address using positional parameters.

### Example 2

```powershell
PS Orch1:\> New-PmUser admin@uipath.com -Name admin -DisplayName 'System Administrator' -SurName Administrator -Type User
```

Creates a user with complete profile information including names and user type.

### Example 3

```powershell
PS Orch1:\> New-PmUser developer@uipath.com -GroupName Developers,'Automation Express' -InvitationAccepted True
```

Creates a user and assigns them to multiple groups with invitation pre-accepted.

### Example 4

```powershell
PS C:\> New-PmUser -Path Orch1: finance@uipath.com -DisplayName 'Finance Manager' -BypassBasicAuthRestriction True -WhatIf
```

Shows what would happen when creating a user in a specific tenant with authentication bypass enabled.

## PARAMETERS

### -BypassBasicAuthRestriction
BypassBasicAuthRestrictionを指定します。

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

### -DisplayName
DisplayNameを指定します。

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

### -Email
Emailを指定します。

```yaml
Type: String
Parameter Sets: (All)
Aliases: UserName

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -GroupName
GroupNameを指定します。

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -InvitationAccepted
InvitationAcceptedを指定します。

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

### -Name
Nameを指定します。

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
Specifies the name of the target drives.
If not specified, the current drive will be targeted.

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

### -SurName
SurNameを指定します。

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

### -Type
Typeを指定します。

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

### -WhatIf
Shows what would happen if the cmdlet runs.
The cmdlet is not run.

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
ProgressActionを指定します。

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

### UiPath.PowerShell.Entities.PmUser
## NOTES

## RELATED LINKS
