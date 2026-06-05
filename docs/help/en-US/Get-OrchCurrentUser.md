---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchCurrentUser.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchCurrentUser
---

# Get-OrchCurrentUser

## SYNOPSIS

Gets the currently authenticated user from UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
Get-OrchCurrentUser [[-Path] <string[]>] [-LiteralPath <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets the user information for the currently authenticated user on the target Orchestrator tenant. This returns the user account that was used to create the Orch: drive connection.

This cmdlet can only be used for drives connected with non-confidential application settings. It cannot be used with confidential application drives, as confidential applications do not have an associated user identity.

The retrieval is performed in parallel when multiple drives are specified, with results returned as they complete.

The -Path parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available Orch: drives.

Primary Endpoint: /odata/Users/UiPath.Server.Configuration.OData.GetCurrentUser

OAuth required scopes: OR.Users or OR.Users.Read

Required permissions: None (the user can always retrieve their own information)

## EXAMPLES

### Example 1: Get the current user

```powershell
PS Orch1:\> Get-OrchCurrentUser
```

Gets the user information for the currently authenticated user on the current Orch: drive.

### Example 2: Get the current user from a specific drive

```powershell
PS C:\> Get-OrchCurrentUser Orch1:\
```

Gets the current user from the Orch1 drive. The -Path parameter is positional (position 0) so the parameter name can be omitted. When -Path uses an absolute path, the command can be run from any location.

### Example 3: Get the current user from multiple drives

```powershell
PS C:\> Get-OrchCurrentUser Orch1:\,Orch2:\
```

Gets the current user from both the Orch1 and Orch2 drives. The retrieval is performed in parallel for improved performance.

## PARAMETERS

### -Path

Specifies the target Orch: drives. If not specified, the current drive is targeted. Tab completion suggests available Orch: drives.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: false
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

### -LiteralPath

Specifies the target folder or drive by literal path -- wildcard metacharacters (`[`, `]`, `*`, `?`) are treated as literal characters rather than patterns. Accepts the same drive-qualified paths as -Path. Its `PSPath` alias also binds the path of items piped from Get-ChildItem / Get-Item, so you can pipe folders directly. Use -LiteralPath instead of -Path when a folder name contains a wildcard metacharacter.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: false
Aliases:
- PSPath
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

### System.String[]

You can pipe drive paths to this cmdlet via the Path property.

## OUTPUTS

### UiPath.PowerShell.Entities.User

Returns a User object for the currently authenticated user with properties including UserName, FullName, Type, EmailAddress, MayHaveUserSession, MayHaveRobotSession, MayHaveUnattendedSession, and RolesList.

## NOTES

This cmdlet requires a non-confidential application connection. Confidential application drives do not have an associated user identity and will return an error.

The retrieval is performed using a thread pool for parallel execution when multiple drives are specified.

## RELATED LINKS

[Get-OrchUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchUser.md)

[Update-OrchCurrentUserURPassword](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Update-OrchCurrentUserURPassword.md)
