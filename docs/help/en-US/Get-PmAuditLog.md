---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmAuditLog.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-PmAuditLog
---

# Get-PmAuditLog

## SYNOPSIS

Gets audit log entries from UiPath Automation Cloud organizations.

## SYNTAX

### __AllParameterSets

```
Get-PmAuditLog [-Skip <ulong>] [-First <ulong>] [-OrderAscending] [-Path <string[]>]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets audit log entries from UiPath Automation Cloud at the organization (platform management) level. The audit log records administrative actions such as user management, group changes, and configuration modifications.

Results are returned in descending order by default (newest first). Use the -OrderAscending switch to reverse the sort order. Use -Skip and -First for pagination through large result sets.

When the cmdlet is called without any filter parameters (-Skip, -First, or -OrderAscending), the entire result set is cached locally and a warning is displayed. Subsequent calls with the same parameters use the cached data for faster access.

When multiple Pm: drives are connected, specifying -Path targets specific organizations. If -Path is omitted, the current drive is targeted.

Primary Endpoint: GET /api/auditLog/{partitionGlobalId} (Identity Server)

OAuth required scopes: (Identity Server API - no per-endpoint scopes)

Required permissions: (managed by Identity Server)

## EXAMPLES

### Example 1: Get all audit log entries

```powershell
PS Orch1:\> Get-PmAuditLog
```

Gets all audit log entries from the current organization. A warning is displayed indicating the results are cached.

### Example 2: Get the most recent audit log entries

```powershell
PS Orch1:\> Get-PmAuditLog -First 10
```

Gets the 10 most recent audit log entries from the current organization.

### Example 3: Paginate through audit log entries

```powershell
PS Orch1:\> Get-PmAuditLog -Skip 20 -First 10
```

Skips the first 20 entries and returns the next 10 audit log entries.

### Example 4: Get audit logs in ascending order

```powershell
PS Orch1:\> Get-PmAuditLog -OrderAscending -First 5
```

Gets the 5 oldest audit log entries, sorted in ascending chronological order.

### Example 5: Get audit logs from a specific organization

```powershell
PS C:\> Get-PmAuditLog -Path Orch1: -First 50
```

Gets the 50 most recent audit log entries from the Orch1 organization drive.

## PARAMETERS

### -Path

Specifies the target Pm: drives (organizations). If not specified, the current drive is targeted. Tab completion suggests available drive names.

```yaml
Type: System.String[]
DefaultValue: ''
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

Specifies the maximum number of audit log entries to return. Use this parameter together with -Skip for pagination.

```yaml
Type: System.Nullable`1[System.UInt64]
DefaultValue: ''
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

Sorts the audit log entries in ascending chronological order (oldest first). By default, entries are returned in descending order (newest first).

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
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

Specifies the number of audit log entries to skip from the beginning of the result set. Use this parameter together with -First for pagination.

```yaml
Type: System.Nullable`1[System.UInt64]
DefaultValue: ''
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

### System.UInt64

You can pipe Skip and First values to this cmdlet.

### System.Management.Automation.SwitchParameter

You can pipe the OrderAscending value to this cmdlet.

### System.String[]

You can pipe Path values to this cmdlet.

## OUTPUTS

### UiPath.PowerShell.Entities.PmAuditLog

Returns PmAuditLog objects representing individual audit log entries with details such as timestamp, action type, actor, and affected entity.

## NOTES

When called without filter parameters, the cmdlet caches the entire audit log locally and displays a warning. Use -First and -Skip to retrieve specific pages of results without caching the full data set.

Audit logs are read-only and reflect administrative actions performed at the organization level.

## RELATED LINKS

Get-PmUser

Get-PmAuthenticationSetting
