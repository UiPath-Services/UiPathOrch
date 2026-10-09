---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Measure-OrchLog.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 10/08/2026
PlatyPS schema version: 2024-05-01
title: Measure-OrchLog
---

# Measure-OrchLog

## SYNOPSIS

Counts the robot execution logs that match a filter, per folder, without fetching them.

## SYNTAX

### __AllParameterSets

```
Measure-OrchLog [-Path <string[]>] [-LiteralPath <string[]>] [-Recurse] [-Depth <uint>]
 [-JobKey <string>] [-Last <string>] [-Level <string>] [-Machine <string>] [-ProcessName <string>]
 [-TimeStampAfter <datetime>] [-TimeStampBefore <datetime>] [-WindowsIdentity <string[]>]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

The `Measure-OrchLog` cmdlet counts the robot execution logs in each target folder and returns one object per folder with its path and the count. The logs themselves are not downloaded, so the cmdlet answers quickly even for millions of logs, for example to size the log volume per day.

The filter parameters are those of `Get-OrchLog` and mean the same: the count is the number of logs `Get-OrchLog` returns for the same arguments. As in `Get-OrchLog`, `-Level` defaults to `Info`, so logs at the Info level and above are counted unless you specify another level. Unlike `Get-OrchLog`, no filter parameter is required: without one, the cmdlet counts all Info-and-above logs the folder holds.

The count comes from the GetTotalCount endpoint, not from `@odata.count` on `/odata/RobotLogs`. When Orchestrator keeps the logs in Elasticsearch, as Automation Cloud does, `@odata.count` stops at 10,000 (the index's max_result_window), while GetTotalCount gives the full number.

A folder where nothing can match (it does not hold the process named by `-ProcessName`, or the `-Machine` or `-WindowsIdentity` value names nothing there) is written with a count of 0. A folder whose processes or machines cannot be read is reported as an error for that folder, and the other folders are still counted.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: GET /odata/RobotLogs/UiPath.Server.Configuration.OData.GetTotalCount

OAuth required scopes: OR.Monitoring or OR.Monitoring.Read

Required permissions: Logs.View

## EXAMPLES

### Example 1: Count the logs of the last day in every folder

```powershell
PS C:\> Measure-OrchLog -Path Orch1:\ -Recurse -Last Day -Level Trace
```

Counts the logs of every level written in the last day, in each folder of the tenant.

### Example 2: Total for the tenant

```powershell
PS C:\> (Measure-OrchLog -Path Orch1:\ -Recurse -Last Day -Level Trace | Measure-Object Count -Sum).Sum
```

Adds the per-folder counts up to the number of logs the tenant received in the last day.

### Example 3: Errors of one process in the last week

```powershell
PS Orch1:\Shared> Measure-OrchLog -Last Week -Level Error -ProcessName BlankProcess19
```

Counts the Error and Fatal logs that the process `BlankProcess19` wrote in the last week.

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

### -LiteralPath

Specifies the target folder or drive by literal path -- wildcard metacharacters (`[`, `]`, `*`, `?`) are treated as literal characters rather than patterns. Accepts the same drive-qualified paths as -Path. Its `PSPath` alias also binds the path of items piped from Get-ChildItem / Get-Item, so you can pipe folders directly. Use -LiteralPath instead of -Path when a folder name contains a wildcard metacharacter.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: false
Aliases:
- PSPath
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

### -JobKey

Counts only the logs of the job with this key (GUID). Tab completion suggests job keys from the local cache.

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

Counts only the logs within a predefined time range up to now. Valid values are: Hour, Day, Week, Month, 3Months, 6Months, Year, 3Years.

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

Specifies the minimum log level to count. Valid values are: Trace (all levels), Info (Info and above, the default), Warn (Warn and above), Error (Error and Fatal), Fatal (Fatal only).

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

Counts only the logs of this machine. The name is looked up among all machines of the tenant, as in `Get-OrchLog`; a name no machine of the tenant has counts no logs. Where the drive cannot read the tenant's machines, the name is looked up among the machines assigned to the folder. Tab completion suggests the machines assigned to the target folder first, then the tenant's other machines.

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

Counts only the logs of this process (release). Tab completion suggests release names from Orchestrator.

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

### -TimeStampAfter

Counts only the logs with timestamps at or after the specified date and time. The value is converted to UTC before filtering.

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

Counts only the logs with timestamps before the specified date and time. The value is converted to UTC before filtering.

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

Counts only the logs of jobs that ran under these Windows identities (domain\user). One value without wildcard characters is compared with the identity each log records, ignoring case; a wildcard pattern or several values are compared with the user names configured on the target folder's robots, as in `Get-OrchLog`. A value that matches nothing counts no logs. Tab completion suggests the user names configured on the target folder's robots.

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

You can pipe a folder path to the **Path** parameter, or string values to the **Last**, **Level**, **Machine**, **ProcessName** and **JobKey** parameters.

### System.DateTime

You can pipe DateTime values to the **TimeStampAfter** and **TimeStampBefore** parameters.

### System.String[]

You can pipe string arrays to the **WindowsIdentity** and **Path** parameters.

## OUTPUTS

### UiPath.PowerShell.Commands.LogCount

One object per folder, with the folder's **Path** and the **Count** of matching logs.

## NOTES

When the `-Level` parameter is omitted, it defaults to `Info`, counting logs at the Info level and above.

## RELATED LINKS

[Get-OrchLog](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchLog.md)
