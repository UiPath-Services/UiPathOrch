---
document type: cmdlet
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchUnattendedSession
---

# Get-OrchUnattendedSession

## SYNOPSIS

Gets unattended sessions from UiPath Orchestrator.

## SYNTAX

### __AllParameterSets

```
Get-OrchUnattendedSession [[-Status] <string[]>] [-Path <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets the machine session runtime information for unattended sessions from UiPath Orchestrator. The cmdlet returns MachineSessionRuntime objects that include details about each session such as machine name, host machine name, service user name, session status, runtime type, and maintenance mode state.

Results can be filtered by session status using the -Status parameter, which accepts wildcard patterns and supports the values: Available, Busy, Disconnected, and Unknown.

The -Status and -Path parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values. The -Status completions show the available status values, excluding any already specified.

Primary Endpoint: GET /odata/Sessions/UiPath.Server.Configuration.OData.GetMachineSessionRuntimes

OAuth required scopes: OR.Robots or OR.Robots.Read

Required permissions: Machines.View

## EXAMPLES

### Example 1: Get all unattended sessions

```powershell
PS Orch1:\> Get-OrchUnattendedSession
```

Gets all unattended sessions from the current Orchestrator drive.

### Example 2: Get sessions filtered by status

```powershell
PS Orch1:\> Get-OrchUnattendedSession Available
```

Gets only unattended sessions with the Available status. The status value is specified as a positional parameter.

### Example 3: Get sessions with multiple statuses

```powershell
PS Orch1:\> Get-OrchUnattendedSession Available,Busy
```

Gets unattended sessions that have either the Available or Busy status.

### Example 4: Get sessions from a specific drive

```powershell
PS C:\> Get-OrchUnattendedSession -Path Orch1: -Status Disconnected
```

Gets all disconnected unattended sessions from the Orch1: drive.

## PARAMETERS

### -Path

Specifies the name of the target drives.
If not specified, the current drive will be targeted.

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

### -Status

Specifies the status of the unattended sessions to retrieve. Accepts wildcard patterns. Valid values are: Available, Busy, Disconnected, and Unknown.

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe drive names to this cmdlet via the Path property, or status values via the Status property.

## OUTPUTS

### UiPath.PowerShell.Entities.MachineSessionRuntime

Returns MachineSessionRuntime objects containing details about each unattended session, including MachineName, HostMachineName, ServiceUserName, Status, SessionId, Runtimes, RuntimeType, and MaintenanceMode.

## NOTES

When multiple drives are specified, queries are executed in parallel using the thread pool. The -Status parameter filters the results client-side after retrieval.

## RELATED LINKS

Enable-OrchMaintenanceMode

Disable-OrchMaintenanceMode
