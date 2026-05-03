---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchRobot.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchRobot
---

# Get-OrchRobot

## SYNOPSIS

Gets robots auto-provisioned from users.

## SYNTAX

### __AllParameterSets

```
Get-OrchRobot [[-FullName] <string[]>] [[-Username] <string[]>] [-Path <string[]>]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets robot information from UiPath Orchestrator. Robots are auto-provisioned from users and represent the runtime identity that executes automation processes on machines. This cmdlet retrieves robots from the Modern Folders model.

The -FullName and -Username parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values. Multiple values can be specified using comma-separated text that includes wildcards.

The cmdlet uses multi-threaded processing to retrieve robots from multiple drives in parallel.

Robots are tenant-scoped entities. Use the -Path parameter to specify different Orchestrator drives.

Primary Endpoint: GET /odata/Robots/UiPath.Server.Configuration.OData.GetConfiguredRobots?$expand=User

OAuth required scopes: OR.Robots or OR.Robots.Read

Required permissions: Users.View and Robots.View

## EXAMPLES

### Example 1: Get all robots

```powershell
PS Orch1:\> Get-OrchRobot
```

Gets all robots from the current Orchestrator tenant.

### Example 2: Get robots by username

```powershell
PS Orch1:\> Get-OrchRobot -Username CORP\*
```

Gets robots whose username matches `CORP\*`.

### Example 3: Get robots by username pattern

```powershell
PS Orch1:\> Get-OrchRobot -Username autogen\*
```

Gets robots whose username matches `autogen\*`.

### Example 4: Get robots from a specific drive

```powershell
PS C:\> Get-OrchRobot -Path Orch1:\ -Username CORP\Robot01
```

Gets the robot with the specified username from the Orch1 tenant.

## PARAMETERS

### -Path

Specifies the target Orchestrator drives. If not specified, the current drive is targeted.

```yaml
Type: System.String[]
DefaultValue: None
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

### -FullName

Specifies the full names of the robots to retrieve. Supports wildcards. Tab completion dynamically suggests robot full names from the target drives.

```yaml
Type: System.String[]
DefaultValue: None
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

### -Username

Specifies the usernames of the robots to retrieve (typically in DOMAIN\username format). Supports wildcards. Tab completion dynamically suggests robot usernames from the target drives.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: (All)
  Position: 1
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

You can pipe robot full names to this cmdlet via the FullName property.

## OUTPUTS

### UiPath.PowerShell.Entities.Robot

Returns Robot objects representing auto-provisioned robots with properties including FullName, Username, Type, and associated User information.

## NOTES

Robots are tenant-scoped entities. The -Path parameter specifies Orchestrator drives (not folder paths).

This cmdlet retrieves robots from the Modern Folders model. For classic folder robots, use Get-OrchClassicRobot.

Results are ordered by User.FullName and then by Username.

## RELATED LINKS

Get-OrchClassicRobot

Get-OrchClassicEnvironment

Get-OrchMachine
