---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Search-OrchDirectory

## SYNOPSIS
Searches Directory.

## SYNTAX

```
Search-OrchDirectory [-Name] <String> [-Path <String[]>] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION
The `Search-OrchDirectory` cmdlet searches for directory users and groups in UiPath Orchestrator's directory service. This cmdlet provides a way to find users and groups by searching for names that start with the specified search term within the Orchestrator's directory context.

The search results include directory objects such as users and robot accounts, along with their identifiers, display names, domains, and type information. This cmdlet specifically searches within the Orchestrator's directory service scope, which may include different results compared to Platform Management directory searches.

The search is performed using a "starts with" pattern and includes domain context (typically "autogen" for local users), making it suitable for finding directory objects within the Orchestrator's authentication and user management context.

This cmdlet is useful for user discovery, administrative tasks, and integration scenarios where you need to find and identify directory objects within the Orchestrator environment.

Primary Endpoint: GET /api/DirectoryService/SearchForUsersAndGroups?domain=autogen&prefix={prefix}&searchContext=All

OAuth required scopes: [PLACEHOLDER - Directory search scopes]

Required permissions: [PLACEHOLDER - Directory search permissions]

Primary Endpoint: GET /api/DirectoryService/SearchForUsersAndGroups?domain=autogen&prefix={prefix}&searchContext=All

OAuth required scopes: OR.Users.Read

Required permissions: (Users.View or Units.Edit or SubFolders.Edit)

## EXAMPLES

### Example 1
```powershell
PS C:\> {{ Add example code here }}
```

{{ Add example description here }}

## PARAMETERS

### -Name
{{ Fill Name Description }}

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
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

### UiPath.PowerShell.Entities.DirectoryObject
## NOTES

## RELATED LINKS
