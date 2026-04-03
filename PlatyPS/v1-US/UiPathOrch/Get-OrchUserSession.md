---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchUserSession
---

# Get-OrchUserSession

## SYNOPSIS

Gets user sessions from UiPath Orchestrator tenants.

## SYNTAX

### __AllParameterSets

```
Get-OrchUserSession [-State <string[]>] [-Type <string[]>] [-OrderBy <string[]>] [-Skip <ulong>]
 [-First <ulong>] [-Path <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets global user session information from UiPath Orchestrator tenants. Sessions represent connected robot instances on machines, showing their current state, type, and associated user and machine details.

The -State parameter filters sessions by their connection status. Tab completion suggests valid values: Available, Busy, and Disconnected.

The -Type parameter filters sessions by robot type. Tab completion suggests valid values: "Attended (Attended User)", "Attended (Citizen Developer)", "Attended (RPA Developer)", "Attended (Automation Developer)", "Production (Unattended)", and "Cloud - VM".

The -OrderBy parameter specifies the sort order for the returned sessions. Tab completion suggests available orderable fields.

The -Skip and -First parameters support server-side paging for large result sets.

The -State, -Type, -OrderBy, -First, and -Path parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values.

Primary Endpoint: GET /odata/Sessions/UiPath.Server.Configuration.OData.GetGlobalSessions

OAuth required scopes: OR.Robots or OR.Robots.Read

Required permissions: (Robots.View and Users.View - Classic and modern robot sessions are returned.) and (Users.View or Machines.Create or Machines.Edit - Modern robot sessions are returned.
Users.View is required only when the robot is expanded) and (Robots.View - Classic robot sessions are returned.
Users.View is required only when the robot is expanded)

## EXAMPLES

### Example 1: Get all sessions

```powershell
PS Orch1:\> Get-OrchUserSession
```

Gets all user sessions from the current tenant.

### Example 2: Get available sessions

```powershell
PS Orch1:\> Get-OrchUserSession -State Available
```

Gets only sessions in the Available state. Other valid values are Busy and Disconnected.

### Example 3: Get unattended sessions

```powershell
PS Orch1:\> Get-OrchUserSession -Type 'Production (Unattended)'
```

Gets only unattended production robot sessions.

### Example 4: Get sessions with paging

```powershell
PS Orch1:\> Get-OrchUserSession -First 10 -Skip 20
```

Gets 10 sessions starting from the 21st record. Useful for paging through large numbers of sessions.

### Example 5: Get sessions sorted by machine name

```powershell
PS Orch1:\> Get-OrchUserSession -OrderBy Hostname
```

Gets all sessions sorted by the host machine name in ascending order. Tab completion suggests the available orderable field names: User, Domain, Hostname, Type, and Version.

### Example 6: Get sessions from a specific drive

```powershell
PS C:\> Get-OrchUserSession -Path Orch1:\
```

Gets all sessions from the Orch1 tenant. When -Path uses an absolute path, the command can be run from any location.

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
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -First

Gets only the specified number of objects. Enter the number of objects to get. Used for server-side paging in combination with -Skip. Tab completion suggests the value 10.

```yaml
Type: System.Nullable`1[System.UInt64]
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

### -OrderBy

Specifies the field to sort the returned sessions by. Tab completion suggests available orderable fields. Results are sorted in ascending order.

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

### -Skip

Ignores the specified number of objects and then gets the remaining objects. Enter the number of objects to skip. Used for server-side paging in combination with -First.

```yaml
Type: System.Nullable`1[System.UInt64]
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

### -State

Filters sessions by their connection state. Tab completion suggests valid values: Available, Busy, and Disconnected. Multiple values can be specified.

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

### -Type

Filters sessions by robot type. Tab completion suggests valid values: "Attended (Attended User)", "Attended (Citizen Developer)", "Attended (RPA Developer)", "Attended (Automation Developer)", "Production (Unattended)", and "Cloud - VM". Multiple values can be specified.

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe State, Type, OrderBy, and Path values to this cmdlet via their property names.

### System.UInt64

You can pipe Skip and First values to this cmdlet via their property names.

## OUTPUTS

### UiPath.PowerShell.Entities.Session

Returns Session objects with properties including HostMachineName, State, Robot (with expanded License and User), UpdateInfo, and Path. The Robot expansion includes the associated user and license information.

## NOTES

User sessions are tenant-scoped entities. Navigate to the root of an Orch: drive or use -Path to specify target drives.

The API returns sessions with expanded Robot (including License and User) and UpdateInfo properties, providing comprehensive session details in a single call.

Server-side filtering is performed for -State and -Type parameters via OData $filter. The -OrderBy parameter maps to OData $orderby.

## RELATED LINKS

Get-OrchUser

Get-OrchMachine
