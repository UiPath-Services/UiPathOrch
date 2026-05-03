---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-DuRoleFromDuUser.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Remove-DuRoleFromDuUser
---

# Remove-DuRoleFromDuUser

## SYNOPSIS

Removes role assignments from users in a Document Understanding project.

## SYNTAX

### __AllParameterSets

```
Remove-DuRoleFromDuUser [-Name] <string[]> [[-Roles] <string[]>] [-Path <string[]>] [-Recurse]
 [-WhatIf] [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Removes the specified role assignments from users in a Document Understanding project.
The `-Name` parameter identifies the target users by display name (wildcard matching supported).
The `-Roles` parameter specifies which roles to remove (wildcard matching supported).
Only directly assigned (non-inherited) roles can be removed; inherited roles are skipped.

This cmdlet operates on the PSDrive of the UiPathOrchDu provider.
If the scope in the configuration file includes "Du.", the PSDrive of the UiPathOrchDu provider will be automatically added.
You can confirm this with the Get-PSDrive cmdlet.
The configuration file can be opened with the Edit-OrchConfig cmdlet.

Primary Endpoint: PATCH /{partitionGlobalId}/pap_/api/userroleassignments (Document Understanding PAP API)

OAuth required scopes: (Document Understanding PAP API)

Required permissions: (managed by Document Understanding)

## EXAMPLES

### Example 1: Remove a role from a user

```powershell
PS Orch1Du:\MyProject> Remove-DuRoleFromDuUser john.doe@example.com "DU Validator"
```

Removes the "DU Validator" role from the user john.doe@example.com in the current project. The role name contains a space so it must be quoted.

### Example 2: Remove all roles from matching users in a specific project

```powershell
PS C:\> Remove-DuRoleFromDuUser -Path Orch1Du:\MyProject john* *
```

Removes all directly assigned roles from all users whose display name starts with "john" in the specified project.

### Example 3: Preview removing roles from all users across all projects

```powershell
PS Orch1Du:\> Remove-DuRoleFromDuUser -Name * -Roles "DU Admin*" -Recurse -WhatIf
```

Shows what would happen when removing all roles starting with "DU Admin" from all users across all projects, without actually performing the operation.

## PARAMETERS

### -Path

Specifies the target folder. If not specified, the current folder is targeted.

```yaml
Type: System.String[]
DefaultValue: ''
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

### -Recurse

Includes the target folder and all its subfolders in the operation.

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

### -Name

Specifies the display name of the users from which roles should be removed.
Wildcard characters are permitted.
This parameter is mandatory.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: true
Aliases: []
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

### -Roles

Specifies the names of the roles to remove from the users.
Wildcard characters are permitted.
Only directly assigned (non-inherited) roles are removed.

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe Name, Roles, and Path values to this cmdlet by property name.

## OUTPUTS

### None

This cmdlet does not produce pipeline output.

## NOTES

This cmdlet operates on the UiPathOrchDu provider PSDrive. Only directly assigned (non-inherited) roles can be removed; inherited roles are skipped with no error.


## RELATED LINKS

[Get-DuUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-DuUser.md)

[Add-DuUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Add-DuUser.md)

[Get-DuRole](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-DuRole.md)
