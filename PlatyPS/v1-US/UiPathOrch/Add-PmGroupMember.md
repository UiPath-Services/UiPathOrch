---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Add-PmGroupMember
---

# Add-PmGroupMember

## SYNOPSIS

Adds a member to a platform management group in a UiPath Automation Cloud organization.

## SYNTAX

### __AllParameterSets

```
Add-PmGroupMember [-GroupName] <string[]> [[-Type] <string[]>] [-UserName] <string[]>
 [-Path <string[]>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Adds one or more members to one or more groups at the organization (platform management) level. Members can be directory users, directory groups, directory applications, or directory robot users.

This cmdlet accepts pipeline input, allowing bulk member additions from a CSV file. All pipeline input is collected during ProcessRecord and the API calls are made during EndProcessing. Members are resolved via the organization's directory service using bulk lookups for efficiency.

The -UserName parameter requires entering at least one character to trigger directory search-based tab completion. The tab completer searches the organization's directory and excludes members already present in the target group(s). Local groups cannot be added as members.

If a member is already present in the target group, a warning is displayed and the duplicate is skipped. Members that cannot be resolved in the directory are also skipped with a warning.

The -GroupName, -Type, -UserName, and -Path parameters support tab completion.

Primary Endpoint: PUT /api/Group/{groupId} (Identity Server)

OAuth required scopes: (Identity Server API - no per-endpoint scopes)

Required permissions: (managed by Identity Server)

## EXAMPLES

### Example 1: Add a user to a group

```powershell
PS Orch1:\> Add-PmGroupMember "Automation Developers" DirectoryUser ytsuda@gmail.com
```

Adds the directory user "ytsuda@gmail.com" to the "Automation Developers" group. Because -GroupName, -Type, and -UserName are positional parameters (positions 0, 1, 2), parameter names can be omitted.

### Example 2: Add a user to multiple groups

```powershell
PS Orch1:\> Add-PmGroupMember Automation* DirectoryUser ytsuda@gmail.com
```

Adds the user to all groups matching "Automation*".

### Example 3: Bulk import members from CSV

```powershell
PS Orch1:\> Import-Csv C:\temp\members.csv | Add-PmGroupMember
```

Imports group members from a CSV file. The CSV should contain columns matching the parameter names: GroupName, Type, UserName, and optionally Path.

### Example 4: Add a robot account to a group

```powershell
PS Orch1:\> Add-PmGroupMember "Automation Users" DirectoryRobotUser MyRobot1
```

Adds the robot account "MyRobot1" to the "Automation Users" group.

### Example 5: Preview member addition

```powershell
PS Orch1:\> Add-PmGroupMember "Automation Developers" DirectoryUser ytsuda+c@gmail.com -WhatIf
```

Shows what would happen without actually adding the member.

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

### -GroupName

Specifies the name(s) of the group(s) to add members to. Supports wildcards for pattern matching. Tab completion dynamically suggests group names. This parameter has the alias "Name".

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: true
Aliases:
- Name
ParameterSets:
- Name: (All)
  Position: 0
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Type

Specifies the type of the member to add. Valid values are DirectoryUser, DirectoryGroup, DirectoryApplication, and DirectoryRobotUser. Tab completion suggests available types. Supports wildcards.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: true
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

### -UserName

Specifies the name(s) of the member(s) to add. For DirectoryUser types, this is typically the user's email address or display name. Tab completion searches the organization's directory when at least one character is entered, and excludes members already in the target group(s).

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 2
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -WhatIf

Runs the command in a mode that only reports what would happen without performing the actions.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases:
- wi
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

### -Confirm

Prompts you for confirmation before running the cmdlet.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases:
- cf
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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe objects with GroupName, Type, UserName, and Path properties to this cmdlet.

## OUTPUTS

### UiPath.PowerShell.Entities.PmGroup

Returns the updated PmGroup object after members have been added.

## NOTES

All pipeline input is collected first, then API calls are made during EndProcessing for efficiency. Members are resolved via bulk directory lookups grouped by member type (user, group, application).

Local groups (source = "local") cannot be added as members and are skipped with a warning.

If a member already exists in the target group, a warning is displayed and the duplicate addition is skipped.

DirectoryRobotUser members are resolved via directory search rather than bulk resolution.

## RELATED LINKS

Remove-PmGroupMember

Move-PmGroupMember

Get-PmGroupMember

Get-PmGroup
