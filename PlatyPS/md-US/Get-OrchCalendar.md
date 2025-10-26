---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchCalendar

## SYNOPSIS
Retrieves business calendars configured in UiPath Orchestrator.

## SYNTAX

```
Get-OrchCalendar [[-Name] <String[]>] [-ExpandExcludedDate] [-IncludePastDate] [-Path <String[]>]
 [-ExportCsv <String>] [-CsvEncoding <Encoding>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
The Get-OrchCalendar cmdlet retrieves business calendars configured within UiPath Orchestrator. Business calendars define working days, holidays, and excluded dates for automation scheduling, enabling precise control over when automation processes should execute.

Calendars contain properties such as Name, TimeZoneId, ExcludedDates array, and a unique Key (GUID identifier). These calendars are used by triggers and schedules to determine execution timing based on business rules and holiday schedules.

Business calendars are tenant entities that operate across the entire tenant scope. Use the -Path parameter to specify target tenants by drive name (e.g., Orch1:, Orch2:).

This cmdlet operates as a tenant-level entity operation, retrieving calendars from the specified Orchestrator environment. The -ExpandExcludedDate parameter provides detailed information about excluded dates, while -IncludePastDate controls whether historical date information is included.

Primary Endpoint: GET /odata/Calendars

OAuth required scopes: OR.Jobs or OR.Jobs.Read

Required permissions: Schedules.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\Shared> Get-OrchCalendar
```

Retrieves all business calendars, displaying Id, Name, TimeZoneId, and Key properties.

### Example 2
```powershell
PS Orch1:\> Get-OrchCalendar *Holiday*
```

Gets calendars whose names contain "Holiday" using wildcard pattern matching.

### Example 3
```powershell
PS C:\> Get-OrchCalendar -Path Orch1:, Orch2: -ExpandExcludedDate
```

Gets calendars from multiple tenants with expanded excluded date information.

### Example 4
```powershell
PS Orch1:\> Get-OrchCalendar | ConvertTo-Json -Depth 3
```

Displays detailed calendar properties in JSON format, including ExcludedDates array and all configuration details.

### Example 5
```powershell
PS Orch1:\> Get-OrchCalendar -ExportCsv C:\\Reports\\Calendars.csv
```

Exports all calendars to CSV with UTF-8 BOM encoding. The exported CSV can be imported using Import-Csv | New-OrchCalendar.

## PARAMETERS

### -ExpandExcludedDate
Expands the ExcludedDates property to show detailed information about excluded dates in the calendar.

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

### -Name
Specifies the names of calendars to be retrieved. Supports wildcard patterns for flexible calendar selection.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -Path
Specifies the target tenant drives. If not specified, the current tenant will be targeted. For tenant-level operations.

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

### -CsvEncoding
Specifies the character encoding for CSV export. Supports various encoding formats for international compatibility.

```yaml
Type: Encoding
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: UTF8 with BOM
Accept pipeline input: False
Accept wildcard characters: False
```

### -ExportCsv
Specifies the file path for exporting calendar information to CSV format. Includes configuration details for documentation and analysis.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -IncludePastDate
Includes past dates in the excluded dates information when used with -ExpandExcludedDate.

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

### UiPath.PowerShell.Entities.ExtendedCalendar
### UiPath.PowerShell.Entities.ExcludedDateNamed
## NOTES
This cmdlet is a tenant-level entity operation for accessing business calendar configurations. Calendars define working days and holidays for automation scheduling. Use -ExpandExcludedDate to get detailed excluded date information and -IncludePastDate to include historical dates. The -ExportCsv parameter facilitates documentation and configuration backup. This operation requires Schedules.View permissions.



Primary Endpoint: GET /odata/Calendars
OAuth required scopes: OR.Settings or OR.Settings.Read
Required permissions: Settings.View

## RELATED LINKS



[Remove-OrchCalendar](Remove-OrchCalendar.md)

[Get-OrchTrigger](Get-OrchTrigger.md)
