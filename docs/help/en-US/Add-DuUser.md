---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Add-DuUser.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Add-DuUser
---

# Add-DuUser

## SYNOPSIS

Adds a user with role assignments to a Document Understanding project.

## SYNTAX

### __AllParameterSets

```
Add-DuUser [-Type] <string[]> [-Name] <string[]> [-Roles] <string[]> [-Path <string[]>] [-Recurse]
 [-WhatIf] [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Adds a user, group, robot account, or external application to a Document Understanding project with the specified roles.
The `-Type` parameter specifies the directory entity type, and the `-Name` parameter specifies the display name of the entity to add.
The `-Roles` parameter specifies which Document Understanding roles to assign, and supports wildcard matching against the available roles.

The cmdlet resolves the specified names against the platform directory service (Identity Management) in bulk for efficiency.
If a specified name cannot be resolved, a warning is displayed and the entry is skipped.

This cmdlet supports pipeline input from CSV files, where multiple values can be comma-separated in the first element.

This cmdlet operates on the PSDrive of the UiPathOrchDu provider.
If the scope in the configuration file includes "Du.", the PSDrive of the UiPathOrchDu provider will be automatically added.
You can confirm this with the Get-PSDrive cmdlet.
The configuration file can be opened with the Edit-OrchConfig cmdlet.

Primary Endpoint: PATCH /{partitionGlobalId}/pap_/api/userroleassignments (Document Understanding PAP API)

OAuth required scopes: (Document Understanding PAP API)

Required permissions: (managed by Document Understanding)

## EXAMPLES

### Example 1: Add a directory user with a role

```powershell
PS Orch1Du:\MyProject> Add-DuUser DirectoryUser ytsuda@gmail.com "DU Administrator"
```

Adds the directory user ytsuda@gmail.com to the current Document Understanding project with the "DU Administrator" role. The role name contains a space so it must be quoted.

### Example 2: Add a directory group to a specific project

```powershell
PS C:\> Add-DuUser -Path Orch1Du:\MyProject DirectoryGroup Administrators DU*
```

Adds the directory group "Administrators" to the specified project with all roles whose name starts with "DU".

### Example 3: Preview adding a user to all projects

```powershell
PS Orch1Du:\> Add-DuUser -Type DirectoryUser -Name ytsuda+c@gmail.com -Roles "DU Validator" -Recurse -WhatIf
```

Shows what would happen when adding the user ytsuda+c@gmail.com with the "DU Validator" role to all projects, without actually performing the operation.

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

Specifies the display name of the user, group, robot account, or external application to add.
This parameter is mandatory.

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

### -Roles

Specifies the names of the Document Understanding roles to assign to the user.
Wildcard characters are permitted.
This parameter is mandatory.

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

### -Type

Specifies the type of directory entity to add.
Acceptable values are DirectoryUser, DirectoryGroup, DirectoryRobotUser, and DirectoryApplication.
This parameter is mandatory.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: false
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

You can pipe Type, Name, Roles, and Path values to this cmdlet by property name.

## OUTPUTS

### None

This cmdlet does not produce pipeline output.

## NOTES

This cmdlet operates on the UiPathOrchDu provider PSDrive. It resolves user, group, robot account, and external application names against the platform directory service in bulk, and supports pipeline input from CSV files.


## RELATED LINKS

Get-DuUser

Get-DuRole

Remove-DuRoleFromDuUser
