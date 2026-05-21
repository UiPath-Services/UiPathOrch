---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchAuditLog.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchAuditLog
---

# Get-OrchAuditLog

## SYNOPSIS

Gets the audit logs.

## SYNTAX

### Filter (Default)

```
Get-OrchAuditLog [-Path <string[]>] [[-Last] <string>] [[-Component] <string[]>]
 [[-UserName] <string[]>] [[-Action] <string[]>] [-ExecutionTimeAfter <datetime>]
 [-ExecutionTimeBefore <datetime>] [-ExpandDetails] [-ExpandEntity] [-First <ulong>]
 [-Skip <ulong>] [<CommonParameters>]
```

### Id

```
Get-OrchAuditLog [-Path <string[]>] [-ExpandDetails] [-ExpandEntity] [-First <ulong>]
 [-Id <string[]>] [-Skip <ulong>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

The `Get-OrchAuditLog` cmdlet gets audit logs from UiPath Orchestrator. Audit log is a tenant-level entity, so the `-Path` parameter specifies the target drive names rather than folder paths.

You can retrieve audit logs using the **Filter** parameter set (the default), which supports filtering by time range, component, user name, and action. Alternatively, use the **Id** parameter set to retrieve specific audit log entries by their IDs from the local cache.

You must specify at least one filter parameter to query Orchestrator. If no filter parameters are specified, the cmdlet outputs the cached audit log contents and displays a warning.

The `-Last`, `-Component`, and `-Action` parameters support tab completion with predefined values. The `-UserName` parameter supports tab completion with user names from Orchestrator. The `-Id` parameter supports both tab completion from the local cache and wildcard characters.

Results are sorted by ExecutionTime in descending order (newest first).

When `-ExpandEntity` is specified, the cmdlet outputs the entity objects embedded in each audit log entry instead of the audit log entries themselves. When `-ExpandDetails` is specified, the cmdlet makes additional API calls to retrieve detailed information for each audit log entry and outputs the detail objects.

Primary Endpoint: GET /odata/AuditLogs

OAuth required scopes: OR.Audit or OR.Audit.Read

Required permissions: Audit.View

## EXAMPLES

### Example 1: Get audit logs from the last day

```powershell
PS Orch1:\> Get-OrchAuditLog Day
```

Gets all audit logs from the last day. The `-Last` parameter is positional (position 0), so the parameter name can be omitted.

### Example 2: Get audit logs from a specific drive

```powershell
PS C:\> Get-OrchAuditLog -Path Orch1: -Last Week
```

Gets audit logs from the last week on the Orch1: drive. This command can be executed from any drive.

### Example 3: Get audit logs by component and user

```powershell
PS Orch1:\> Get-OrchAuditLog Month Robots ytsuda@gmail.com
```

Gets audit logs from the last month filtered by the Robots component and the specified user. The `-Last`, `-Component`, and `-UserName` parameters are all positional.

### Example 4: Get audit logs by action

```powershell
PS Orch1:\> Get-OrchAuditLog -Last Week -Action Create,Delete
```

Gets audit logs from the last week where the action is Create or Delete.

### Example 5: Get audit logs within a time range

```powershell
PS Orch1:\> Get-OrchAuditLog -ExecutionTimeAfter 2026-03-01 -ExecutionTimeBefore 2026-03-05
```

Gets audit logs with execution times between March 1 and March 5, 2026.

### Example 6: Expand entity details

```powershell
PS Orch1:\> Get-OrchAuditLog -Last Day -ExpandEntity
```

Gets audit logs from the last day and outputs the entity objects embedded in each audit log entry instead of the audit log entries themselves.

### Example 7: Expand detailed information

```powershell
PS Orch1:\> Get-OrchAuditLog -Last Day -ExpandDetails
```

Gets audit logs from the last day and makes additional API calls to retrieve and output the detailed information for each entry.

### Example 8: Get specific audit log entries by ID

```powershell
PS Orch1:\> Get-OrchAuditLog -Id 123,456
```

Gets audit log entries with IDs 123 and 456 from the local cache.

### Example 9: Get audit logs with paging

```powershell
PS Orch1:\> Get-OrchAuditLog -Last Month -Skip 50 -First 20
```

Gets audit logs from the last month, skipping the first 50 results and returning the next 20.

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

### -Action

Specifies the action type of the audit logs to be retrieved. Tab completion suggests available action values such as Create, Update, Delete, and others.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Filter
  Position: 3
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Component

Specifies the component of the audit logs to be retrieved. Tab completion suggests available component values.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Filter
  Position: 1
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -ExecutionTimeAfter

Filters audit logs with execution times at or after the specified date and time. The value is converted to UTC before filtering.

```yaml
Type: System.Nullable`1[System.DateTime]
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Filter
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -ExecutionTimeBefore

Filters audit logs with execution times before the specified date and time. The value is converted to UTC before filtering.

```yaml
Type: System.Nullable`1[System.DateTime]
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Filter
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -ExpandDetails

When specified, the cmdlet makes additional API calls to retrieve detailed information for each audit log entry and outputs the detail objects instead of the audit log entries.

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

### -ExpandEntity

When specified, the cmdlet outputs the entity objects embedded in each audit log entry instead of the audit log entries themselves.

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

### -First

Gets only the specified number of objects.
Enter the number of objects to get.

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

### -Id

Specifies the audit log ID or IDs to retrieve from the local cache. Wildcard characters are permitted. Tab completion suggests audit log IDs from the local cache.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: Id
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Last

Filters audit logs by a predefined time range relative to the current time based on their execution date. Valid values are: Hour, Day, Week, Month, 3Months, 6Months, Year, 3Years.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Filter
  Position: 0
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Skip

Ignores the specified number of objects and then gets the remaining objects.
Enter the number of objects to skip.

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

### -UserName

Specifies the user name to filter audit logs. The cmdlet resolves user names to user IDs by querying Orchestrator before applying the filter. Tab completion suggests user names from Orchestrator.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: Filter
  Position: 2
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

You can pipe string values to the **Last** and **Path** parameters.

### System.String[]

You can pipe string arrays to the **Component**, **UserName**, **Action**, **Id**, and **Path** parameters.

### System.DateTime

You can pipe DateTime values to the **ExecutionTimeAfter** and **ExecutionTimeBefore** parameters.

### System.UInt64

You can pipe values to the **Skip** and **First** parameters.

## OUTPUTS

### UiPath.PowerShell.Entities.AuditLog

This cmdlet returns AuditLog objects representing UiPath Orchestrator audit log entries. This is the default output type.

### UiPath.PowerShell.Entities.AuditLogEntity

When `-ExpandEntity` is specified, this cmdlet returns AuditLogEntity objects embedded in each audit log entry.

## NOTES

If no filter parameters are specified, the cmdlet outputs the contents of the local audit log cache and writes a warning. You must specify at least one filter parameter to query Orchestrator.

The `-Id` parameter retrieves entries from the local cache only. To populate the cache, first run `Get-OrchAuditLog` with filter parameters.

When `-ExpandDetails` is specified, the cmdlet makes additional API calls for each audit log entry, which may take additional time for large result sets.

## RELATED LINKS

[Get-OrchLog](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchLog.md)

[Get-OrchAlert](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchAlert.md)
