---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchLog

## SYNOPSIS
Retrieves execution logs from UiPath Orchestrator with filtering capabilities.

## SYNTAX

```
Get-OrchLog [-Last <String>] [-TimeStampAfter <DateTime>] [-TimeStampBefore <DateTime>] [[-Level] <String>]
 [-Machine <String>] [-ProcessName <String>] [-WindowsIdentity <String[]>] [-Skip <UInt64>] [-First <UInt64>]
 [-JobKey <String>] [-OrderBy <String>] [-OrderAscending] [-Path <String[]>] [-Recurse] [-Depth <UInt32>]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
The Get-OrchLog cmdlet retrieves execution logs from UiPath Orchestrator with comprehensive filtering capabilities. Logs provide detailed information about automation execution, errors, and system activities. This cmdlet requires at least one filter parameter to prevent excessive data retrieval.

Logs contain information such as Level (severity), TimeStamp, Machine, WindowsIdentity, ProcessName, JobKey, and detailed message content. The cmdlet supports various filtering options including time ranges, log levels, machines, processes, and job keys.

This cmdlet operates as a folder entity operation, requiring navigation to the appropriate folder context or specification of target folders using the -Path parameter. Use pagination parameters (Skip, First) to manage large result sets efficiently.

**Important**: This cmdlet requires at least one filter parameter (such as TimeStampAfter, Last, Level, Machine, ProcessName, or JobKey) to execute. Without filters, it will output cached contents with a warning.

Primary Endpoint: GET /odata/Logs

OAuth required scopes: OR.Monitoring or OR.Monitoring.Read

Required permissions: Logs.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Get-OrchLog -Last Day -First 10
```

Retrieves the first 10 logs from the last day.

### Example 2
```powershell
PS Orch1:\Shared> Get-OrchLog -TimeStampAfter (Get-Date).AddHours(-2) -Level Error, Fatal
```

Gets all error and fatal logs from the last 2 hours.

### Example 3
```powershell
PS C:\> Get-OrchLog -Path Orch1:\Production -ProcessName InvoiceProcess -First 20
```

Retrieves the first 20 logs for the InvoiceProcess in the Production folder.

### Example 4
```powershell
PS Orch1:\> Get-OrchLog -Last Week -Machine Robot01 -Level Warn, Error
```

Gets warning and error logs from Robot01 for the last week across all folders in that tenant.

### Example 5
```powershell
PS Orch1:\Shared> Get-OrchLog -JobKey 12345678-1234-1234-1234-123456789012 -OrderBy TimeStamp -OrderAscending
```

Retrieves logs for a specific job key ordered by timestamp in ascending order.

### Example 6
```powershell
PS C:\> Get-OrchLog -Path Orch1:\Shared -Last Month -WindowsIdentity DOMAIN\robotuser -Skip 100 -First 50
```

Gets logs for a specific Windows identity from the last month, skipping the first 100 results and taking the next 50.

## PARAMETERS

### -Depth
Specifies the depth for recursion into target folders. A depth of 0 indicates the current location only. Higher values include more subfolder levels.

```yaml
Type: UInt32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Level
Specifies the log levels to filter. Common values include Trace, Debug, Info, Warn, Error, Fatal.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Path
Specifies the target folders to search. If not specified, the current folder context will be used. For folder entity operations requiring path specification.

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

### -Recurse
Includes the target folder and all its subfolders in the search operation. Essential for comprehensive log discovery.

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

### -TimeStampAfter
Specifies the earliest timestamp for filtering logs. Only logs after this time will be returned.

```yaml
Type: DateTime
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -TimeStampBefore
Specifies the latest timestamp for filtering logs. Only logs before this time will be returned.

```yaml
Type: DateTime
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Skip
Specifies the number of log entries to skip from the beginning of the result set. Useful for pagination.

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
Specifies the maximum number of log entries to return from the beginning of the result set.

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

### -Last
Specifies a time period for recent logs. Valid values: Hour, Day, Week, Month, 3Months, 6Months, Year, 3Years.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Machine
Specifies the machine names to filter logs. Use to retrieve logs from specific robots or machines.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -WindowsIdentity
Specifies the Windows identities to filter logs. Use to retrieve logs for specific user accounts.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -ProcessName
Specifies the process names to filter logs. Use to retrieve logs for specific automation processes.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -OrderAscending
Specifies whether to order results in ascending (true) or descending (false) order.

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

### -OrderBy
Specifies the field to order results by (e.g., TimeStamp, Level, Machine).

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -JobKey
Specifies the job keys to filter logs. Use to retrieve logs for specific automation executions.

```yaml
Type: String
Parameter Sets: (All)
Aliases: Key

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.Log
## NOTES
This cmdlet is a folder entity operation requiring at least one filter parameter to prevent excessive data retrieval. The cmdlet will output cached contents with a warning if no filter parameters are specified. Use pagination parameters (Skip, First) to manage large result sets. Common filter patterns include time ranges (Last, TimeStampAfter), severity levels (Level), and specific execution contexts (Machine, ProcessName, JobKey). This operation requires Logs.View permissions in the target folders.



Primary Endpoint: GET /odata/RobotLogs
OAuth required scopes: OR.Monitoring or OR.Monitoring.Read
Required permissions: Logs.View

## RELATED LINKS

[Get-OrchJob](Get-OrchJob.md)

[Get-OrchAuditLog](Get-OrchAuditLog.md)

[Clear-OrchLog](Clear-OrchLog.md)

[Export-OrchLog](Export-OrchLog.md)
