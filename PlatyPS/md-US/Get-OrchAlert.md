---
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
Module Name: UiPathOrch
online version:
schema: 2.0.0
---

# Get-OrchAlert

## SYNOPSIS
Gets the alerts.

## SYNTAX

```
Get-OrchAlert [[-Last] <String>] [[-Severity] <String>] [[-Component] <String[]>]
 [-CreationTimeAfter <DateTime>] [-CreationTimeBefore <DateTime>] [-Skip <UInt64>] [-First <UInt64>]
 [-Path <String[]>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
Alert is a tenant entity. Therefore, specify the drive names as the targets on this cmdlet. This cmdlet always queries the Orchestrator for the most current status. Therefore, note that the output of this cmdlet is not cached.

Primary Endpoint: GET /odata/Alerts

OAuth required scopes: OR.Monitoring or OR.Monitoring.Read

Required permissions: Alerts.View

## EXAMPLES

### Example 1
```powershell
PS Orch1:\> Get-OrchAlert
```

Displays all alerts on Orch1: drive, which is the current location.

### Example 2
```powershell
PS C:\> Get-OrchAlert -Path Orch1:
```

Displays all alerts on Orch1: drive. This command can be executed on any drive.

### Example 3
```powershell
PS Orch1:\> Get-OrchAlert -Last Day
```

Displays alerts from the past 24 hours. Additionally, you can specify multiple parameters simultaneously to narrow down the alerts you retrieve. For example, parameters such as `-CreationTimeAfter`, `-Component`, and `-Severity` are available.

### Example 4
```powershell
PS Orch1:\> Get-OrchAlert -Skip 3 -First 5
```

This command retrieves the first 5 alerts after skipping the initial 3 alerts. It is useful for paging through results when you have a large number of alerts and only need to see a specific subset.

### Example 5
```powershell
PS Orch1:\> Get-OrchAlert -Severity Fatal | Select-Object Path, CreationTime, Severity, Component
```

Gets fatal severity alerts and displays key properties with Path shown first.

## PARAMETERS

### -Component
Specify the Component of the alerts to be retrieved. Component you can specify includes Folders, Robots, Process, and others.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 2
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -CreationTimeAfter
Specifies that only alerts created after the provided datetime value will be returned.

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

### -CreationTimeBefore
Specifies that only alerts created before the provided datetime value will be returned.

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

### -Last
Specifies a time period for recent alerts. Valid values: Hour, Day, Week, Month, 3Months, 6Months, Year, 3Years.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Path
Specifies the name of the target drives. If not specified, the current drive will be targeted.

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

### -Severity
Specifies the severity level of the alerts to be retrieved. Acceptable values are Info, Success, Warn, Error, and Fatal.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### UiPath.PowerShell.Entities.Alert
## NOTES
Required Scope: OR.Monitoring.Read



Primary Endpoint: GET /odata/Alerts
OAuth required scopes: OR.Monitoring or OR.Monitoring.Read
Required permissions: Alerts.View

## RELATED LINKS
