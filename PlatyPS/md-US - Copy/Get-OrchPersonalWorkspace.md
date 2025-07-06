---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchPersonalWorkspace

## SYNOPSIS
Gets the personal workspaces.

## SYNTAX

```
Get-OrchPersonalWorkspace [[-Name] <String[]>] [-Path <String[]>] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION
The `Get-OrchPersonalWorkspace` cmdlet retrieves personal workspace information from UiPath Orchestrator. Personal workspaces are individual automation environments assigned to specific users, providing isolated spaces for development, testing, and personal automation activities.

The cmdlet returns information about personal workspaces including workspace identifiers, names, ownership details, activity status, and last login information. Personal workspaces enable users to have dedicated environments for automation development and testing without affecting shared organizational resources.

Personal workspaces support individual user productivity by providing isolated environments where users can develop, test, and manage their automation projects independently. The workspace information includes ownership tracking and activity monitoring to help administrators manage workspace usage and lifecycle.

The cmdlet provides insights into workspace allocation, user activity, and workspace management across the Orchestrator environment, which is essential for resource planning and user management.

Primary Endpoint: GET /odata/PersonalWorkspaces

OAuth required scopes: [PLACEHOLDER - Personal workspace scopes]

Required permissions: [PLACEHOLDER - Personal workspace view permissions]

Primary Endpoint: GET /odata/PersonalWorkspaces

OAuth required scopes: OR.Folders or OR.Folders.Read

Required permissions: Units.View

## EXAMPLES

### Example 1
```powershell
PS C:\> {{ Add example code here }}
```

{{ Add example description here }}

## PARAMETERS

### -Name
Specifies the Name of the personal workspaces to be retrieved.

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

### UiPath.PowerShell.Entities.PersonalWorkspace
## NOTES

## RELATED LINKS
