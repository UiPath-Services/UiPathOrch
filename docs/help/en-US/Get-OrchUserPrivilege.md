---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchUserPrivilege.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchUserPrivilege
---

# Get-OrchUserPrivilege

## SYNOPSIS

Gets user privileges from UiPath Orchestrator tenants.

## SYNTAX

### __AllParameterSets

```
Get-OrchUserPrivilege [[-UserName] <string[]>] [-Path <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets the privilege information for users in UiPath Orchestrator tenants. Privileges represent the effective permissions that a user has based on their assigned roles and folder access.

This cmdlet only returns privileges for users of type DirectoryUser and DirectoryGroup. Robot accounts (DirectoryRobot) and external applications (DirectoryExternalApplication) are automatically filtered out.

The -UserName parameter supports wildcards to retrieve privileges for multiple users. Tab completion dynamically suggests usernames from the target tenant. Privilege retrieval is performed in parallel for improved performance when querying multiple users.

The -UserName and -Path parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values.

Primary Endpoint: GET /odata/Users({userId})/UiPath.Server.Configuration.OData.GetUserPrivileges

OAuth required scopes: OR.Users or OR.Users.Read

Required permissions: Users.View

## EXAMPLES

### Example 1: Get privileges for all users

```powershell
PS Orch1:\> Get-OrchUserPrivilege
```

Gets the privilege information for all directory users and directory groups in the current tenant.

### Example 2: Get privileges for a specific user

```powershell
PS Orch1:\> Get-OrchUserPrivilege yoshifumi.tsuda@uipath.com
```

Gets the privileges for the specified user. The -UserName parameter is positional (position 0) so the parameter name can be omitted.

### Example 3: Get privileges for users matching a wildcard

```powershell
PS Orch1:\> Get-OrchUserPrivilege ytsuda*
```

Gets the privileges for all users whose username starts with "ytsuda".

### Example 4: Get privileges from a specific drive

```powershell
PS C:\> Get-OrchUserPrivilege -Path Orch1:\ -UserName yoshifumi.tsuda@uipath.com
```

Gets the privileges for the specified user from the Orch1 tenant. When -Path uses an absolute path, the command can be run from any location.

## PARAMETERS

### -Path

Specifies the target Orch: drives. If not specified, the current drive is targeted. Tab completion suggests available Orch: drives.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -UserName

Specifies the usernames of users whose privileges to retrieve. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests usernames from the target tenant.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: (All)
  Position: 0
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String

You can pipe a single username to this cmdlet via the UserName property.

### System.String[]

You can pipe usernames and path values to this cmdlet via the UserName and Path properties.

## OUTPUTS

### UiPath.PowerShell.Entities.UserPrivilege

Returns UserPrivilege objects representing the effective permissions for each user based on their role assignments and folder access.

## NOTES

Users are tenant-scoped entities. Navigate to the root of an Orch: drive or use -Path to specify target drives.

Only DirectoryUser and DirectoryGroup users are included in the results. DirectoryRobot and DirectoryExternalApplication users are automatically excluded.

Privilege retrieval is performed in parallel using a thread pool for improved performance when querying multiple users.

## RELATED LINKS

Get-OrchUser

Get-OrchRole
