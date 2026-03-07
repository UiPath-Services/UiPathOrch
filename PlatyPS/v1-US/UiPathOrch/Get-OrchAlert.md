---
document type: cmdlet
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchAlert
---

# Get-OrchAlert

## SYNOPSIS

Gets the alerts. (DEPRECATED since API version 18.0)

## SYNTAX

### __AllParameterSets

```
Get-OrchAlert [[-Last] <string>] [[-Severity] <string>] [[-Component] <string[]>]
 [-CreationTimeAfter <datetime>] [-CreationTimeBefore <datetime>] [-Skip <ulong>] [-First <ulong>]
 [-Path <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

DEPRECATED: The Alerts API has been deprecated since Orchestrator API version 18.0. This cmdlet is only available on Orchestrator instances running API version 17.0 or earlier. On API version 18.0 and later, the cmdlet throws an InvalidOperationException.

The `Get-OrchAlert` cmdlet gets alerts from UiPath Orchestrator. Alert is a tenant-level entity, so the `-Path` parameter specifies the target drive names rather than folder paths. This cmdlet always queries the Orchestrator for the most current status; the output is not cached.

You can filter alerts by time range, severity level, and component. When the `-Severity` parameter is not specified, the default severity is `Success`, which returns alerts at the Success level and above (Success, Warn, Error, Fatal).

The `-Last` parameter supports tab completion with predefined time range values. The `-Severity` parameter supports tab completion with severity levels. The `-Component` parameter supports both tab completion and wildcard characters.

Results are always sorted by CreationTime in descending order (newest first).

Primary Endpoint: GET /odata/Alerts

OAuth required scopes: OR.Monitoring or OR.Monitoring.Read

Required permissions: Alerts.View

## EXAMPLES

### Example 1: Get all alerts from the current drive

```powershell
PS Orch1:\> Get-OrchAlert
```

Gets all alerts on the Orch1: drive with the default severity of Success and above.

### Example 2: Get alerts from a specific drive

```powershell
PS C:\> Get-OrchAlert -Path Orch1:
```

Gets all alerts on the Orch1: drive. This command can be executed from any drive.

### Example 3: Get alerts from the last day

```powershell
PS Orch1:\> Get-OrchAlert Day
```

Gets alerts created within the last day. The `-Last` parameter is positional (position 0), so the parameter name can be omitted.

### Example 4: Get error alerts from the last week

```powershell
PS Orch1:\> Get-OrchAlert Week Error
```

Gets alerts at the Error level and above (Error and Fatal) from the last week. Both `-Last` and `-Severity` are positional parameters.

### Example 5: Get alerts filtered by component

```powershell
PS Orch1:\> Get-OrchAlert -Component Robots
```

Gets alerts from the Robots component with the default severity of Success and above.

### Example 6: Get alerts with paging

```powershell
PS Orch1:\> Get-OrchAlert -Skip 3 -First 5
```

Retrieves the first 5 alerts after skipping the initial 3 alerts. This is useful for paging through a large number of alerts.

### Example 7: Get alerts within a specific time range

```powershell
PS Orch1:\> Get-OrchAlert -CreationTimeAfter '2026-03-01' -CreationTimeBefore '2026-03-05'
```

Gets alerts created between March 1 and March 5, 2026.

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

### -Component

Specifies the component of the alerts to be retrieved. Wildcard characters are permitted. Tab completion suggests available component values such as Folders, Robots, Process, and others.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
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

### -CreationTimeAfter

Specifies that only alerts created at or after the provided datetime value will be returned. The value is converted to UTC before filtering.

```yaml
Type: System.Nullable`1[System.DateTime]
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

### -CreationTimeBefore

Specifies that only alerts created before the provided datetime value will be returned. The value is converted to UTC before filtering.

```yaml
Type: System.Nullable`1[System.DateTime]
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

### -Last

Filters alerts by a predefined time range relative to the current time based on their creation date. Valid values are: Hour, Day, Week, Month, 3Months, 6Months, Year, 3Years.

```yaml
Type: System.String
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

### -Severity

Specifies the minimum severity level of the alerts to be retrieved. The specified level acts as a threshold, returning alerts at that level and above. Valid values are: Info (all levels), Success (Success and above, the default), Warn (Warn and above), Error (Error and Fatal), Fatal (Fatal only).

```yaml
Type: System.String
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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String

You can pipe string values to the **Last**, **Severity**, and **Path** parameters.

### System.String[]

You can pipe string arrays to the **Component** and **Path** parameters.

### System.DateTime

You can pipe DateTime values to the **CreationTimeAfter** and **CreationTimeBefore** parameters.

### System.UInt64

You can pipe values to the **Skip** and **First** parameters.

## OUTPUTS

### UiPath.PowerShell.Entities.Alert

This cmdlet returns Alert objects representing UiPath Orchestrator alerts.

## NOTES

DEPRECATED: The Alerts API has been deprecated since Orchestrator API version 18.0. On API version 18.0 and later, this cmdlet throws an InvalidOperationException. It is only available on Orchestrator instances running API version 17.0 or earlier.

This cmdlet always queries the Orchestrator directly. Unlike some other cmdlets, the output is not cached locally.

When the `-Severity` parameter is omitted, it defaults to `Success`, returning alerts at the Success level and above.

## RELATED LINKS

Get-OrchLog

Get-OrchAuditLog
