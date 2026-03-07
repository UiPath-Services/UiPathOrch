---
document type: cmdlet
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Copy-PmGroup
---

# Copy-PmGroup

## SYNOPSIS

Copies platform management groups from one UiPath Automation Cloud organization to another.

## SYNTAX

### __AllParameterSets

```
Copy-PmGroup [[-GroupName] <string[]>] [-Destination] <string[]> [-Path <string>] [-WhatIf]
 [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Copies groups at the organization (platform management) level from a source organization to one or more destination organizations. The cmdlet replicates both the group structure and its members.

Group members are resolved in the destination organization's directory. Members of type DirectoryUser, DirectoryGroup, DirectoryApplication, and DirectoryRobotUser are each resolved using the appropriate directory lookup. Users are first resolved by name, then by email if the name lookup fails. Members that cannot be found in the destination directory are skipped with a warning.

If a group with the same name already exists in the destination organization (matched case-insensitively), the cmdlet adds any missing members to the existing group rather than creating a duplicate. If the group does not exist, it is created with all resolved members.

The source and destination must belong to different organizations (different partitionGlobalId); same-organization copies are silently skipped.

The -GroupName, -Destination, and -Path parameters support tab completion.

Primary Endpoint: GET /api/Group/{partitionGlobalId}/{groupId}, PUT /api/Group/{groupId} (Identity Server)

OAuth required scopes: (Identity Server API - no per-endpoint scopes)

Required permissions: (managed by Identity Server)

## EXAMPLES

### Example 1: Copy all groups to another organization

```powershell
PS Orch1:\> Copy-PmGroup * -Destination Orch2:
```

Copies all groups and their members from the Orch1 organization to the Orch2 organization.

### Example 2: Copy a specific group across drives

```powershell
PS C:\> Copy-PmGroup -GroupName "Automation Developers" -Path Orch1: -Destination Orch2:
```

Copies the "Automation Developers" group and its members from the Orch1 organization to the Orch2 organization.

### Example 3: Copy groups matching a pattern

```powershell
PS Orch1:\> Copy-PmGroup Automation* -Destination Orch2:
```

Copies all groups whose names start with "Automation" to the Orch2 organization.

### Example 4: Preview a cross-organization group copy

```powershell
PS Orch1:\> Copy-PmGroup * -Destination Orch2: -WhatIf
```

Shows which groups would be copied without performing the operation.

## PARAMETERS

### -Path

Specifies the source Pm: drive (organization) to copy groups from. If not specified, the current drive is used. Tab completion suggests available drive names.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Destination

Specifies the destination Pm: drive(s) (organizations) to copy groups to. Multiple destinations can be specified. Tab completion suggests available drive names.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 1
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -GroupName

Specifies the names of groups to copy. Supports wildcards for pattern matching. Tab completion dynamically suggests group names from the source organization. This parameter has the alias "Name".

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

You can pipe group names to this cmdlet via the GroupName property.

## OUTPUTS

### UiPath.PowerShell.Entities.PmGroup

Returns the created or updated PmGroup objects in the destination organization.

## NOTES

The source and destination drives must belong to different organizations (different partitionGlobalId). Same-organization copies are silently skipped.

Member resolution uses bulk directory lookups. Users are resolved first by name, then by email if the name lookup fails. Members from local sources or app sources that cannot be found are silently ignored.

DirectoryRobotUser members are resolved by matching the robot account name (case-insensitive) in the destination organization's robot account list.

## RELATED LINKS

Get-PmGroup

New-PmGroup

Remove-PmGroup

Copy-PmUser
