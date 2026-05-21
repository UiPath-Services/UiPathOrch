---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Switch-OrchCurrentUser.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/08/2026
PlatyPS schema version: 2024-05-01
title: Switch-OrchCurrentUser
---

# Switch-OrchCurrentUser

## SYNOPSIS

Switches the authenticated user on an Orch: drive by re-authenticating via an InPrivate browser window.

## SYNTAX

### __AllParameterSets

```
Switch-OrchCurrentUser [[-Path] <string[]>] [-Confirm] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Switches the authenticated user on the target Orch: drive(s). This cmdlet clears the existing authentication and all cached data, then opens an InPrivate Microsoft Edge window for re-authentication. Because an InPrivate window is used with a unique temporary profile, existing SSO cookies do not interfere, allowing you to select a different identity provider (e.g., Microsoft/Entra ID, Google) and account.

After successful re-authentication, the cmdlet outputs the new current user (same as `Get-OrchCurrentUser`).

This cmdlet is useful when:

- SSO auto-login prevents you from choosing a different account
- You need to switch from a local account to an Entra ID account (or vice versa)
- You want to re-authenticate after the Entra ID warning appears

The -Path parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available Orch: drives.

Primary Endpoint: PKCE authentication flow (browser-based)

OAuth required scopes: None (authentication only)

Required permissions: None

## EXAMPLES

### Example 1: Switch user on the current drive

```powershell
PS Orch1:\> Switch-OrchCurrentUser
```

Clears authentication and cache on the current drive, opens an InPrivate browser for re-authentication, and outputs the new current user.

### Example 2: Switch user on a specific drive

```powershell
PS C:\> Switch-OrchCurrentUser Orch1:
```

Switches the user on the Orch1 drive. The -Path parameter is positional (position 0) so the parameter name can be omitted.

### Example 3: Switch user with confirmation

```powershell
PS C:\> Switch-OrchCurrentUser Orch1: -WhatIf
```

Shows what would happen without actually performing the switch. Use this to verify the target drive before proceeding.

### Example 4: Switch user on multiple drives

```powershell
PS C:\> Switch-OrchCurrentUser Orch1:,Orch2:
```

Switches the user on both the Orch1 and Orch2 drives sequentially. Each drive opens a separate InPrivate browser window for authentication.

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

### -Confirm

Prompts you for confirmation before running the cmdlet.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases:
- cf
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -WhatIf

Runs the command in a mode that only reports what would happen without performing the actions.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases:
- wi
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
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

Returns a User object for the newly authenticated user with properties including UserName, FullName, Type, EmailAddress, and RolesList.

## NOTES

This cmdlet requires Microsoft Edge to be installed. It launches Edge with `--inprivate --user-data-dir` using a unique temporary profile directory to ensure complete cookie isolation.

After switching, all cached data (users, folders, assets, etc.) is cleared because the new user may have different permissions.

If the Entra ID warning appeared after the initial connection, switching to an Entra ID account resolves it.

## RELATED LINKS

[Get-OrchCurrentUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchCurrentUser.md)

[Get-OrchPSDrive](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchPSDrive.md)
