---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchAuditLog

## SYNOPSIS
Retrieves audit logs from UiPath Orchestrator for monitoring and compliance purposes.

## SYNTAX

### Filter (Default)
```
Get-OrchAuditLog [[-Last] <String>] [[-Component] <String[]>] [[-UserName] <String[]>] [[-Action] <String[]>]
 [-ExecutionTimeAfter <DateTime>] [-ExecutionTimeBefore <DateTime>] [-ExpandEntity] [-ExpandDetails]
 [-Skip <UInt64>] [-First <UInt64>] [-Path <String[]>] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

### Id
```
Get-OrchAuditLog [-Id <String[]>] [-ExpandEntity] [-ExpandDetails] [-Skip <UInt64>] [-First <UInt64>]
 [-Path <String[]>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
The Get-OrchAuditLog cmdlet retrieves audit logs from UiPath Orchestrator, providing detailed information about user activities, system changes, and administrative actions. Audit logs are essential for security monitoring, compliance reporting, and troubleshooting system activities.

Audit logs capture comprehensive information including user actions, component modifications, timestamps, and contextual details about operations performed within Orchestrator. This cmdlet enables filtering by various criteria such as user names, components, actions, and time ranges to focus on specific audit events.

Audit logs are tenant entities that operate across the entire tenant scope. Use the -Path parameter to specify target tenants by drive name (e.g., Orch1:, Orch2:).

This cmdlet operates at the tenant level, providing access to audit events across the organization. Use filtering parameters to narrow down results and improve query performance. The -ExpandEntity and -ExpandDetails parameters provide additional contextual information about logged events.

The cmdlet supports both filter-based queries for broad searches and ID-based queries for retrieving specific audit log entries. Use pagination parameters (-Skip, -First) to manage large result sets efficiently.

Primary Endpoint: GET /odata/AuditLogs

OAuth required scopes: OR.Audit or OR.Audit.Read

Required permissions: Audit.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Get-OrchAuditLog -Last Day
```

Retrieves all audit logs from the last 24 hours in the current tenant.

### Example 2
```powershell
PS Orch1:\> Get-OrchAuditLog -Component Queues,Jobs -Action Create,Delete
```

Gets audit logs for create and delete actions on Queues and Jobs components.

### Example 3
```powershell
PS C:\> Get-OrchAuditLog -Path Orch1:, Orch2: -UserName admin*
```

Retrieves audit logs for users with names starting with "admin" from multiple tenants.

### Example 4
```powershell
PS Orch1:\> Get-OrchAuditLog -Last Day | Select-Object -First 1 | ConvertTo-Json -Depth 3
```

Gets recent audit logs and displays the complete structure including nested Entities and CustomDataExpanded.

## PARAMETERS

### -Action
Specifies the action types to filter audit logs. Common actions include Create, Update, Delete, Login, Logout, Start, Stop, etc.

```yaml
Type: String[]
Parameter Sets: Filter
Aliases:

Required: False
Position: 3
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Component
Specifies the component types to filter audit logs. Examples include Users, Roles, Jobs, Processes, Assets, Machines, etc.

```yaml
Type: String[]
Parameter Sets: Filter
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -ExecutionTimeAfter
Specifies the earliest execution time for filtering audit logs. Only logs after this timestamp will be returned.

```yaml
Type: DateTime
Parameter Sets: Filter
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -ExecutionTimeBefore
Specifies the latest execution time for filtering audit logs. Only logs before this timestamp will be returned.

```yaml
Type: DateTime
Parameter Sets: Filter
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -ExpandEntity
Expands entity information in the audit log entries, providing additional details about the affected objects.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Id
Specifies the IDs of specific audit log entries to retrieve. Use this for targeted queries of known log entries.

```yaml
Type: String[]
Parameter Sets: Id
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Last
Specifies a time period for recent logs. Valid values: Hour, Day, Week, Month, 3Months, 6Months, Year, 3Years.

```yaml
Type: String
Parameter Sets: Filter
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Path
Specifies the target tenant drives. If not specified, the current tenant will be targeted. For tenant-level audit log access.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -ProgressAction
{{ Fill ProgressAction Description }}

```yaml
Type: ActionPreference
Parameter Sets: (All)
Aliases: proga

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -UserName
Specifies the usernames to filter audit logs. Supports wildcard patterns for flexible user filtering.

```yaml
Type: String[]
Parameter Sets: Filter
Aliases:

Required: False
Position: 2
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Skip
Specifies the number of audit log entries to skip from the beginning of the result set. Useful for pagination.

```yaml
Type: UInt64
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -First
Specifies the maximum number of audit log entries to return from the beginning of the result set.

```yaml
Type: UInt64
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -ExpandDetails
Expands detailed information in the audit log entries, providing comprehensive context about the logged actions.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.AuditLog
## NOTES
This cmdlet is a tenant-level entity operation for accessing audit logs across the organization. Audit logs are essential for security monitoring, compliance reporting, and troubleshooting. Use filtering parameters to improve query performance and focus on relevant events. The -ExpandEntity and -ExpandDetails parameters provide additional context but may impact performance. Consider using pagination for large result sets to manage memory usage and response times.



Primary Endpoint: GET /odata/AuditLogs
OAuth required scopes: OR.Audit or OR.Audit.Read
Required permissions: Audit.View

## RELATED LINKS

[Get-OrchCurrentUser](Get-OrchCurrentUser.md)

[Get-OrchUser](Get-OrchUser.md)

[Get-OrchRole](Get-OrchRole.md)

[Get-OrchJob](Get-OrchJob.md)
