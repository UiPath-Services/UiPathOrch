---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Remove-PmGroupMember
---

# Remove-PmGroupMember

## SYNOPSIS

Removes a member from a platform management group in a UiPath Automation Cloud organization.

## SYNTAX

### __AllParameterSets

```
Remove-PmGroupMember [-GroupName] <string[]> [-Type] <string[]> [-UserName] <string[]>
 [-NoMatchWarning] [-Path <string[]>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Removes one or more members from one or more groups at the organization (platform management) level. Members are identified by group name, member type, and member name. All three parameters support wildcards, enabling flexible batch removals.

This cmdlet accepts pipeline input, allowing bulk member removals from a CSV file. All pipeline input is collected during ProcessRecord and the API calls are made during EndProcessing. Duplicate entries (same member in the same group) are automatically deduplicated.

The -NoMatchWarning switch enables warnings when the specified GroupName or UserName pattern does not match any existing entries. This is useful during CSV import to identify rows that do not correspond to actual members.

The cmdlet supports Ctrl+C cancellation during batch operations.

The -GroupName, -Type, -UserName, and -Path parameters support tab completion. The -Type completer suggests types that exist within the selected group, and the -UserName completer suggests members of the selected type within the selected group.

Primary Endpoint: PUT /api/Group/{groupId} (Identity Server)

OAuth required scopes: (Identity Server API - no per-endpoint scopes)

Required permissions: (managed by Identity Server)

## EXAMPLES

### Example 1: Remove a user from a group

```powershell
PS Orch1:\> Remove-PmGroupMember "Automation Developers" DirectoryUser ytsuda@gmail.com
```

Removes the directory user "ytsuda@gmail.com" from the "Automation Developers" group. Because -GroupName, -Type, and -UserName are positional parameters (positions 0, 1, 2), parameter names can be omitted.

### Example 2: Remove all users of a type from a group

```powershell
PS Orch1:\> Remove-PmGroupMember "Automation Developers" DirectoryUser *
```

Removes all directory users from the "Automation Developers" group.

### Example 3: Bulk remove members from CSV

```powershell
PS Orch1:\> Import-Csv C:\temp\members.csv | Remove-PmGroupMember
```

Removes group members listed in the CSV file. The CSV should contain columns: GroupName, Type, UserName, and optionally Path.

### Example 4: Remove with no-match warning

```powershell
PS Orch1:\> Import-Csv C:\temp\members.csv | Remove-PmGroupMember -NoMatchWarning
```

Removes members and displays warnings for any CSV rows where the GroupName or UserName does not match.

### Example 5: Preview member removal

```powershell
PS Orch1:\> Remove-PmGroupMember Automation* DirectoryUser *@gmail.com -WhatIf
```

Shows which members would be removed without actually removing them.

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

Specifies the name(s) of the group(s) to remove members from. Supports wildcards for pattern matching. Tab completion dynamically suggests group names. This parameter has the alias "Name".

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

### -NoMatchWarning

When specified, displays a warning message if the GroupName or UserName pattern does not match any existing entries. Without this switch, unmatched patterns are silently ignored.

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
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Type

Specifies the type of the member to remove. Supports wildcards. Tab completion suggests member types that exist within the selected group(s). Common values include DirectoryUser, DirectoryGroup, DirectoryApplication, and DirectoryRobotUser.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: true
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

### -UserName

Specifies the name(s) of the member(s) to remove. Supports wildcards for pattern matching. Tab completion suggests member names of the selected type within the selected group(s).

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: true
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

Returns the updated PmGroup object after members have been removed.

## NOTES

All pipeline input is collected first, then API calls are made during EndProcessing. Members are removed in bulk per group for efficiency.

Duplicate entries (same member in the same group from multiple pipeline inputs) are automatically deduplicated.

The cmdlet supports Ctrl+C cancellation during the EndProcessing phase.

## RELATED LINKS

Add-PmGroupMember

Move-PmGroupMember

Get-PmGroupMember

Get-PmGroup
