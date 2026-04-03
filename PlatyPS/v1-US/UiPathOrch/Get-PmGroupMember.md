---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-PmGroupMember
---

# Get-PmGroupMember

## SYNOPSIS

Gets the members of platform management groups in a UiPath Automation Cloud organization.

## SYNTAX

### __AllParameterSets

```
Get-PmGroupMember [[-GroupName] <string[]>] [-Path <string[]>] [-ExportCsv <string>]
 [-CsvEncoding <Encoding>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets the members of one or more groups at the organization (platform management) level. For each matching group, the cmdlet retrieves detailed group information including all members.

Members can be of different types: DirectoryUser, DirectoryGroup, DirectoryRobotUser, and DirectoryApplication. Each member includes properties such as name, email, objectType, source, groupName, and identifier.

The -GroupName parameter supports wildcards and tab completion. When -ExportCsv is specified, the output is written to a CSV file with columns: Path, GroupName, Type, UserName, Email, Source. Members are sorted by group name, then by object type, then by member name.

When not exporting to CSV, member objects are written to the pipeline sorted by name.

Primary Endpoint: GET /api/Group/{partitionGlobalId}/{groupId} (Identity Server)

OAuth required scopes: (Identity Server API - no per-endpoint scopes)

Required permissions: (managed by Identity Server)

## EXAMPLES

### Example 1: Get members of all groups

```powershell
PS Orch1:\> Get-PmGroupMember
```

Gets all members of all groups in the current organization.

### Example 2: Get members of a specific group

```powershell
PS Orch1:\> Get-PmGroupMember Administrators
```

Gets all members of the "Administrators" group. Because -GroupName is a positional parameter (position 0), the parameter name can be omitted.

### Example 3: Get members of groups matching a pattern

```powershell
PS Orch1:\> Get-PmGroupMember Auto*
```

Gets all members of groups whose names start with "Auto".

### Example 4: Export group members to CSV

```powershell
PS Orch1:\> Get-PmGroupMember -ExportCsv C:\temp\members.csv
```

Exports all group members to a CSV file with Path, GroupName, Type, UserName, Email, and Source columns. The exported CSV can be used with Add-PmGroupMember or Remove-PmGroupMember for bulk operations.

### Example 5: Filter members by type

```powershell
PS Orch1:\> Get-PmGroupMember Administrators | Where-Object objectType -eq DirectoryUser
```

Gets only the directory user members from the "Administrators" group.

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

### -CsvEncoding

Specifies the encoding for CSV export. Default is UTF-8 with BOM for Excel compatibility. Tab completion suggests all available system encodings (e.g., utf-8, shift_jis, us-ascii).

```yaml
Type: System.Text.Encoding
DefaultValue: ''
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

### -ExportCsv

Exports group members to the specified CSV file path. The CSV includes Path, GroupName, Type, UserName, Email, and Source columns. When this parameter is specified, no objects are written to the pipeline.

```yaml
Type: System.String
DefaultValue: ''
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

### -GroupName

Specifies the names of groups whose members to retrieve. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests group names from the target organizations. This parameter has the alias "Name".

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: true
Aliases:
- Name
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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe group names to this cmdlet via the GroupName property.

## OUTPUTS

### UiPath.PowerShell.Entities.DirectoryUser

Returns DirectoryUser member objects.

### UiPath.PowerShell.Entities.DirectoryGroup

Returns DirectoryGroup member objects.

### UiPath.PowerShell.Entities.DirectoryRobotUser

Returns DirectoryRobotUser member objects.

### UiPath.PowerShell.Entities.DirectoryApplication

Returns DirectoryApplication member objects.

## NOTES

This cmdlet makes an additional API call per group to retrieve detailed member information. For organizations with many groups, this may take some time.

The exported CSV can be used as input for Add-PmGroupMember or Remove-PmGroupMember for bulk member management.

## RELATED LINKS

Add-PmGroupMember

Remove-PmGroupMember

Move-PmGroupMember

Get-PmGroup
