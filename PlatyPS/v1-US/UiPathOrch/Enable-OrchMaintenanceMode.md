---
document type: cmdlet
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Enable-OrchMaintenanceMode
---

# Enable-OrchMaintenanceMode

## SYNOPSIS

Enables maintenance mode on unattended sessions in UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
Enable-OrchMaintenanceMode [[-MachineName] <string[]>] [[-HostMachineName] <string[]>]
 [[-ServiceUserName] <string[]>] [[-SessionId] <long[]>] [-Force] [-Path <string[]>] [-WhatIf]
 [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Enables maintenance mode on specified unattended sessions in UiPath Orchestrator. When maintenance mode is enabled on a session, no new jobs will be dispatched to that session, allowing the machine to be taken offline for maintenance.

The cmdlet filters sessions that currently have a Default (non-maintenance) MaintenanceMode state and have active runtimes. You can narrow the scope by specifying one or more of the filter parameters: -MachineName, -HostMachineName, -ServiceUserName, or -SessionId. If no filter parameters are specified, all eligible sessions on the targeted drives are affected.

The -MachineName, -HostMachineName, -ServiceUserName, and -SessionId parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values. Tab completion shows only sessions that are currently not in maintenance mode (i.e., sessions that can be enabled). The completions respect already-specified filter parameter values to show contextually relevant choices.

Primary Endpoint: POST /odata/Sessions/UiPath.Server.Configuration.OData.SetMaintenanceMode

OAuth required scopes: OR.Robots

Required permissions: Machines.Edit

## EXAMPLES

### Example 1: Enable maintenance mode on all sessions

```powershell
PS Orch1:\> Enable-OrchMaintenanceMode
```

Enables maintenance mode on all eligible unattended sessions on the current drive. The cmdlet prompts for confirmation for each session.

### Example 2: Enable maintenance mode on a specific machine

```powershell
PS Orch1:\> Enable-OrchMaintenanceMode ROBOT-SERVER01
```

Enables maintenance mode on all sessions running on the machine named ROBOT-SERVER01.

### Example 3: Enable maintenance mode with Force

```powershell
PS Orch1:\> Enable-OrchMaintenanceMode ROBOT-SERVER01 -Force
```

Enables maintenance mode on the specified machine and immediately kills all running jobs instead of waiting for them to complete.

### Example 4: Enable maintenance mode on a specific session

```powershell
PS C:\> Enable-OrchMaintenanceMode -SessionId 12345 -Path Orch1:
```

Enables maintenance mode on the session with the specified session ID on the Orch1: drive.

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

### -Force

Specifies to kill all running jobs immediately instead of waiting for them to complete gracefully.

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

Specifies the host machine name of the unattended sessions to enable maintenance mode on.

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

Specifies the machine name of the unattended sessions to enable maintenance mode on.

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

Specifies the service user name of the unattended sessions to enable maintenance mode on.

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

Specifies the session ID of the unattended sessions to enable maintenance mode on.

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

The cmdlet filters sessions that have runtimes (Runtimes != 0) and are currently in Default (non-maintenance) state. Duplicate sessions (by SessionId) are automatically excluded. The session cache is cleared after each successful update.

## RELATED LINKS

Disable-OrchMaintenanceMode

Get-OrchUnattendedSession
