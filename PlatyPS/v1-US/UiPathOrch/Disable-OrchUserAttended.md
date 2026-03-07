---
document type: cmdlet
external help file: UiPathOrch-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Disable-OrchUserAttended
---

# Disable-OrchUserAttended

## SYNOPSIS

Disables the attended robot session for users.

## SYNTAX

### __AllParameterSets

```
Disable-OrchUserAttended [-UserName] <string[]> [-Path <string[]>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Disables the attended robot session (MayHaveRobotSession) for specified users in UiPath Orchestrator. This is a convenience wrapper around Update-OrchUser that sets -MayHaveRobotSession to False for the specified users.

When a user has MayHaveRobotSession disabled, they are not allowed to run attended automations using UiPath Assistant on their machine.

The -UserName parameter supports wildcards to disable the attended session for multiple users at once. Tab completion dynamically suggests usernames from the target tenant.

The -UserName and -Path parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values.

Primary Endpoint: GET /odata/Users, PUT /odata/Users({userId})

OAuth required scopes: OR.Users

Required permissions: Users.View, Users.Edit

## EXAMPLES

### Example 1: Disable attended robot for a specific user

```powershell
PS Orch1:\> Disable-OrchUserAttended ytsuda@gmail.com
```

Disables the attended robot session for the user "ytsuda@gmail.com". The -UserName parameter is positional (position 0) so the parameter name can be omitted.

### Example 2: Disable attended robot for multiple users with a wildcard

```powershell
PS Orch1:\> Disable-OrchUserAttended ytsuda*
```

Disables the attended robot session for all users whose username starts with "ytsuda".

### Example 3: Disable attended robot from a specific drive

```powershell
PS C:\> Disable-OrchUserAttended -Path Orch1:\ -UserName ytsuda+c@gmail.com
```

Disables the attended robot session for "ytsuda+c@gmail.com" on the Orch1 tenant. When -Path uses an absolute path, the command can be run from any location.

### Example 4: Preview changes with -WhatIf

```powershell
PS Orch1:\> Disable-OrchUserAttended ytsuda* -WhatIf
```

Shows which users would have their attended robot session disabled without actually making changes.

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

Specifies the usernames of users to disable the attended robot session for. This is a mandatory parameter. Supports wildcards. Tab completion dynamically suggests usernames from the target tenant.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: (All)
  Position: 0
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
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

### None

This cmdlet does not produce output. The user is updated via Update-OrchUser internally.

## NOTES

This is a PowerShell script function that wraps Update-OrchUser with -MayHaveRobotSession False. Each specified user is updated individually in a loop. The function also passes all bound parameters via splatting (@PSBoundParameters).

Users are tenant-scoped entities. Navigate to the root of an Orch: drive or use -Path to specify target drives.

## RELATED LINKS

Enable-OrchUserAttended

Update-OrchUser

Get-OrchUser
