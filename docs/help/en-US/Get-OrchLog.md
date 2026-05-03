---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchLog.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchLog
---

# Get-OrchLog

## SYNOPSIS

Gets the execution logs of the robots.

## SYNTAX

### __AllParameterSets

```
Get-OrchLog [-Last <string>] [-TimeStampAfter <datetime>] [-TimeStampBefore <datetime>]
 [-Level <string>] [-Machine <string>] [-ProcessName <string>] [-WindowsIdentity <string[]>]
 [-Skip <ulong>] [-First <ulong>] [-JobKey <string>] [-OrderBy <string>] [-OrderAscending]
 [-Path <string[]>] [-Recurse] [-Depth <uint>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

The `Get-OrchLog` cmdlet gets robot execution logs from UiPath Orchestrator. You can filter logs by time range, log level, machine, process name, Windows identity, and job key.

You must specify at least one filter parameter to query Orchestrator. If no filter parameters are specified, the cmdlet outputs the cached log contents and displays a warning.

When the `-Level` parameter is not specified, the default level is `Info`, which returns logs at the Info level and above (Info, Warn, Error, Fatal). By default, results are sorted by TimeStamp in descending order.

The `-Machine`, `-ProcessName`, `-Level`, `-Last`, `-JobKey`, and `-OrderBy` parameters support tab completion. The `-WindowsIdentity` parameter supports both tab completion and wildcard characters.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: GET /odata/RobotLogs

OAuth required scopes: OR.Monitoring or OR.Monitoring.Read

Required permissions: Logs.View

## EXAMPLES

### Example 1: Get logs from the last day

```powershell
PS Orch1:\Shared> Get-OrchLog -Last Day
```

Gets all robot execution logs created within the last day from the current folder with the default level of Info and above.

### Example 2: Get error and fatal logs from the last week

```powershell
PS Orch1:\Shared> Get-OrchLog -Last Week -Level Error
```

Gets logs at the Error level and above (Error and Fatal) from the last week.

### Example 3: Get logs for a specific process

```powershell
PS Orch1:\Shared> Get-OrchLog -Last Day -ProcessName BlankProcess19
```

Gets logs from the last day filtered by the process name `BlankProcess19`.

### Example 4: Get logs for a specific machine

```powershell
PS Orch1:\Shared> Get-OrchLog -Last Day -Machine ROBOT-PC01
```

Gets logs from the last day filtered by the machine name `ROBOT-PC01`.

### Example 5: Get logs filtered by Windows identity

```powershell
PS Orch1:\Shared> Get-OrchLog -Last Day -WindowsIdentity 'DOMAIN\user*'
```

Gets logs from the last day for Windows identities matching the wildcard pattern `DOMAIN\user*`.

### Example 6: Get logs using a time range

```powershell
PS Orch1:\Shared> Get-OrchLog -TimeStampAfter 2026-03-01 -TimeStampBefore 2026-03-05
```

Gets logs with timestamps between March 1 and March 5, 2026.

### Example 7: Get logs for a specific job key from a specific folder

```powershell
PS C:\> Get-OrchLog -Path Orch1:\Shared -JobKey a1b2c3d4-e5f6-7890-abcd-ef1234567890 -Last Month
```

Gets logs from the last month for the specified job key from the Shared folder.

### Example 8: Get logs recursively with paging

```powershell
PS C:\> Get-OrchLog -Path Orch1:\Shared -Recurse -Last Week -First 100 -Skip 50
```

Gets logs from the last week from the Shared folder and all its subfolders, skipping the first 50 results and returning the next 100.

### Example 9: Get logs sorted in ascending order

```powershell
PS Orch1:\Shared> Get-OrchLog -Last Day -OrderBy TimeStamp -OrderAscending
```

Gets logs from the last day sorted by timestamp in ascending order (oldest first).

## PARAMETERS

### -Path

Specifies the target folder. If not specified, the current folder is targeted.

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

### -Recurse

Includes the target folder and all its subfolders in the operation.

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

### -Depth

Specifies the depth for recursion into the target folders.
A depth of 0 indicates the current location only, with no subfolders included. When -Depth is specified, -Recurse is implied.

```yaml
Type: System.UInt32
DefaultValue: None
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

### -JobKey

Filters logs by the job key (GUID). Tab completion suggests job keys from the local cache.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases:
- Key
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

Filters logs by a predefined time range relative to the current time. Valid values are: Hour, Day, Week, Month, 3Months, 6Months, Year, 3Years.

```yaml
Type: System.String
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

### -Level

Specifies the minimum log level to retrieve. The specified level acts as a threshold, returning logs at that level and above. Valid values are: Trace (all levels), Info (Info and above, the default), Warn (Warn and above), Error (Error and Fatal), Fatal (Fatal only).

```yaml
Type: System.String
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

### -Machine

Filters logs by machine name. Tab completion suggests machine names assigned to the target folder.

```yaml
Type: System.String
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

### -OrderAscending

Specifies that the results should be sorted in ascending order. By default, results are sorted in descending order.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -OrderBy

Specifies the field to sort the results by. If not specified, the default sort field is `TimeStamp`. Tab completion suggests available sort fields.

```yaml
Type: System.String
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

### -ProcessName

Filters logs by the process (release) name. Tab completion suggests release names from Orchestrator.

```yaml
Type: System.String
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

### -TimeStampAfter

Filters logs with timestamps at or after the specified date and time. The value is converted to UTC before filtering.

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

### -TimeStampBefore

Filters logs with timestamps before the specified date and time. The value is converted to UTC before filtering.

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

### -WindowsIdentity

Filters logs by the Windows identity (user name) of the robot. Wildcard characters are permitted. Tab completion suggests Windows identities from the target folder's user robots.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
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

You can pipe a folder path to the **Path** parameter, or string values to the **Last**, **Level**, **Machine**, **ProcessName**, **JobKey**, and **OrderBy** parameters.

### System.DateTime

You can pipe DateTime values to the **TimeStampAfter** and **TimeStampBefore** parameters.

### System.String[]

You can pipe string arrays to the **WindowsIdentity** and **Path** parameters.

### System.UInt64

You can pipe values to the **Skip** and **First** parameters.

## OUTPUTS

### UiPath.PowerShell.Entities.Log

This cmdlet returns Log objects representing UiPath Orchestrator robot execution logs.

## NOTES

If no filter parameters are specified, the cmdlet outputs the contents of the local log cache and writes a warning. You must specify at least one filter parameter to query Orchestrator.

When the `-Level` parameter is omitted, it defaults to `Info`, returning logs at the Info level and above.

## RELATED LINKS

Get-OrchJob

Get-OrchAuditLog
