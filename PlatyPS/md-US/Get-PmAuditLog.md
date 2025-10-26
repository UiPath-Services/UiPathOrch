---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-PmAuditLog

## SYNOPSIS
Gets audit log entries from Platform Management.

## SYNTAX

```
Get-PmAuditLog [-Skip <UInt64>] [-First <UInt64>] [-OrderAscending] [-Path <String[]>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
The Get-PmAuditLog cmdlet retrieves audit log entries from UiPath Platform Management. This cmdlet operates at the organization level and tracks administrative activities, user actions, and system events across the organization.

This is an organization entity cmdlet that calls the Platform Management API. It operates at the organization level, where multiple tenants can belong to the same organization. The -Path parameter specifies target tenants using drive names (e.g., Orch1:, Orch2:), but the audit data reflects organization-wide activities.

Platform Management audit logs provide comprehensive tracking of user management activities, role assignments, license changes, and administrative actions performed across all tenants within the organization. This is essential for compliance, security auditing, and administrative oversight.

Primary Endpoint: GET /api/auditLog/{partitionGlobalId}

OAuth required scopes: OR.Administration or OR.Administration.Read

Required permissions: Administration.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Get-PmAuditLog
```

Gets audit log entries from the current organization.

### Example 2
```powershell
PS Orch1:\> Get-PmAuditLog -First 100
```

Gets the first 100 audit log entries (most recent by default).

### Example 3
```powershell
PS Orch1:\> Get-PmAuditLog -OrderAscending -First 50
```

Gets the first 50 audit log entries in ascending order (oldest first).

### Example 4
```powershell
PS Orch1:\> Get-PmAuditLog -Path Orch1:, Orch2: -First 200
```

Gets audit log entries from the organization, accessed through multiple tenant drives.

### Example 5
```powershell
PS Orch1:\> Get-PmAuditLog -Skip 100 -First 50
```

Skips the first 100 entries and gets the next 50 audit log entries (pagination).

### Example 6
```powershell
PS Orch1:\> Get-PmAuditLog | Where-Object {$_.EventType UserCreated}
```

Gets all audit log entries for user creation events.

### Example 7
```powershell
PS Orch1:\> Get-PmAuditLog -First 1000 | Select-Object Timestamp, UserName, EventType, Details | Export-Csv "AuditLog.csv"
```

Gets the first 1000 audit log entries and exports key information to a CSV file for analysis.

## PARAMETERS

### -First
Gets only the specified number of objects.
Enter the number of objects to get.

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

### -OrderAscending
Specifies the sorting order for createdOn. By default, entries are returned in descending order (newest first).

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Path
Specifies the name of the target tenant drives. The audit log data is organization-wide regardless of which tenant drive is used.

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

### -Skip
Ignores the specified number of objects and then gets the remaining objects.
Enter the number of objects to skip.

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

### -ProgressAction
Controls how progress information is displayed during command execution. Use 'SilentlyContinue' to suppress progress display.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.Nullable`1[[System.UInt64, System.Private.CoreLib, Version=9.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
### System.Management.Automation.SwitchParameter
### System.String[]
## OUTPUTS

### UiPath.PowerShell.Entities.PmAuditLog
## NOTES

## RELATED LINKS
