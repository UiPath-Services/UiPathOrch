---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Disable-OrchMaintenanceMode.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Disable-OrchMaintenanceMode
---

# Disable-OrchMaintenanceMode

## SYNOPSIS

Disables maintenance mode on unattended sessions in UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
Disable-OrchMaintenanceMode [-Path <string[]>] [-LiteralPath <string[]>] [[-MachineName] <string[]>]
 [[-HostMachineName] <string[]>] [[-ServiceUserName] <string[]>] [[-SessionId] <long[]>]
 [-Confirm] [-Force] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Disables maintenance mode on specified unattended sessions in UiPath Orchestrator. When maintenance mode is disabled on a session, the session returns to normal operation and jobs can once again be dispatched to it.

The cmdlet filters sessions that currently have an Enabled MaintenanceMode state and have active runtimes. You can narrow the scope by specifying one or more of the filter parameters: -MachineName, -HostMachineName, -ServiceUserName, or -SessionId. If no filter parameters are specified, all sessions currently in maintenance mode on the targeted drives are affected.

The -MachineName, -HostMachineName, -ServiceUserName, and -SessionId parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values. Tab completion shows only sessions that are currently in maintenance mode (i.e., sessions that can be disabled). The completions respect already-specified filter parameter values to show contextually relevant choices.

Primary Endpoint: POST /odata/Sessions/UiPath.Server.Configuration.OData.SetMaintenanceMode

OAuth required scopes: OR.Robots or OR.Robots.Write

Required permissions: Machines.Edit

## EXAMPLES

### Example 1: Disable maintenance mode on all sessions

```powershell
PS Orch1:\> Disable-OrchMaintenanceMode
```

Disables maintenance mode on all sessions currently in maintenance mode on the current drive.

### Example 2: Disable maintenance mode on a specific machine

```powershell
PS Orch1:\> Disable-OrchMaintenanceMode ROBOT-SERVER01
```

Disables maintenance mode on all sessions running on the machine named ROBOT-SERVER01.

### Example 3: Disable maintenance mode on a specific drive

```powershell
PS C:\> Disable-OrchMaintenanceMode -Path Orch1:
```

Disables maintenance mode on all maintenance-mode sessions on the Orch1: drive.

### Example 4: Disable maintenance mode for a specific session ID

```powershell
PS C:\> Disable-OrchMaintenanceMode -Path Orch1: -SessionId 12345
```

Disables maintenance mode on the session with the specified session ID on the Orch1: drive.

## PARAMETERS

### -Path

Specifies the name of the target drives.
If not specified, the current drive is targeted.

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
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Force

Specifies to force the operation. When used with Disable-OrchMaintenanceMode, this forces the session back to normal state immediately.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: False
SupportsWildcards: false
Aliases: []
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

### -HostMachineName

Specifies the host machine name of the unattended sessions to disable maintenance mode on.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: false
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

### -MachineName

Specifies the machine name of the unattended sessions to disable maintenance mode on.

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

### -ServiceUserName

Specifies the service user name of the unattended sessions to disable maintenance mode on.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 2
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -SessionId

Specifies the session ID of the unattended sessions to disable maintenance mode on.

```yaml
Type: System.Int64[]
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 3
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -WhatIf

Shows what would happen if the cmdlet runs.
The cmdlet is not run.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: False
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
DefaultValue: False
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

### System.String[]

You can pipe machine names via MachineName, host machine names via HostMachineName, service user names via ServiceUserName, or drive names via Path.

### System.Int64[]

You can pipe session IDs via SessionId.

## OUTPUTS

### None

This cmdlet does not produce pipeline output. The maintenance mode state is updated on the Orchestrator.

## NOTES

The cmdlet filters sessions that have runtimes (Runtimes != 0) and are currently in Enabled (maintenance) state. Duplicate sessions (by SessionId) are automatically excluded. The session cache is cleared after each successful update.

## RELATED LINKS

[Enable-OrchMaintenanceMode](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Enable-OrchMaintenanceMode.md)

[Get-OrchUnattendedSession](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchUnattendedSession.md)
