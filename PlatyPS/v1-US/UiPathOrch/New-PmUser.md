---
document type: cmdlet
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: New-PmUser
---

# New-PmUser

## SYNOPSIS

Creates a new platform management user in a UiPath Automation Cloud organization.

## SYNTAX

### __AllParameterSets

```
New-PmUser [-Email] <string> [-Name <string>] [-SurName <string>] [-DisplayName <string>]
 [-Type <string>] [-BypassBasicAuthRestriction <string>] [-InvitationAccepted <string>]
 [-GroupName <string[]>] [-Path <string[]>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Creates a new user at the organization (platform management) level in UiPath Automation Cloud. The user is created via the identity service bulk creation API.

This cmdlet accepts pipeline input, allowing multiple users to be created from a CSV file or other pipeline sources. When multiple users are piped in, they are batched by target drive and group membership and created in bulk for efficiency. The bulk API call is executed during EndProcessing, so all pipeline input is collected before any API calls are made.

If a -GroupName is specified and the group does not exist, the cmdlet automatically creates the group before adding the user. Wildcard patterns in -GroupName are expanded to match existing group names. When importing from a CSV exported by Get-PmUser, the GroupName column may contain comma-separated group names, which are automatically parsed.

Duplicate email entries within the same batch are detected and skipped with a warning.

The -Email, -Type, -BypassBasicAuthRestriction, -InvitationAccepted, -GroupName, and -Path parameters support tab completion.

Primary Endpoint: POST /api/User/BulkCreate (Identity Server)

OAuth required scopes: (Identity Server API - no per-endpoint scopes)

Required permissions: (managed by Identity Server)

## EXAMPLES

### Example 1: Create a single user

```powershell
PS Orch1:\> New-PmUser newuser@example.com -Name Taro -SurName Yamada -DisplayName "Taro Yamada"
```

Creates a new user with the specified email, name, surname, and display name. Because -Email is a positional parameter (position 0), the parameter name can be omitted.

### Example 2: Create a user and assign to a group

```powershell
PS Orch1:\> New-PmUser newuser@example.com -GroupName "Automation Developers"
```

Creates a new user and assigns them to the "Automation Developers" group. If the group does not exist, it is automatically created.

### Example 3: Bulk import users from CSV

```powershell
PS Orch1:\> Import-Csv C:\temp\users.csv | New-PmUser
```

Imports users from a CSV file. The CSV should contain columns matching the parameter names: Email, Name, SurName, DisplayName, Type, BypassBasicAuthRestriction, InvitationAccepted, GroupName, and Path. Users are batched by group membership and created via the bulk API.

### Example 4: Create a user with group wildcard

```powershell
PS Orch1:\> New-PmUser newuser@example.com -GroupName "Automation*"
```

Creates a new user and assigns them to all existing groups matching the "Automation*" wildcard pattern.

### Example 5: Preview user creation without executing

```powershell
PS Orch1:\> New-PmUser newuser@example.com -Name Test -WhatIf
```

Shows what would happen if the command were to run without actually creating the user.

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

### -BypassBasicAuthRestriction

Specifies whether the user can bypass basic authentication restrictions. Tab completion suggests "true" and "false".

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

### -DisplayName

Specifies the display name for the user.

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

### -Email

Specifies the email address for the new user. This value is also used as the userName. This parameter has the aliases "UserName" and "DestinationUserName".

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases:
- UserName
- DestinationUserName
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

### -GroupName

Specifies the groups to assign the user to. Supports wildcards to match existing group names. If a specified group name does not match any existing group and does not contain wildcard characters, the group is automatically created. When importing from CSV, comma-separated group names in a single value are automatically parsed. Tab completion suggests existing group names.

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

### -InvitationAccepted

Specifies whether the user's invitation has been accepted. Tab completion suggests "true" and "false".

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

### -Name

Specifies the first name (given name) for the user.

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

### -SurName

Specifies the surname (last name) for the user.

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

### -Type

Specifies the user type. Tab completion suggests available user type values.

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

### System.String

You can pipe email addresses to this cmdlet via the Email property.

### System.String[]

You can pipe objects with Email, Name, SurName, DisplayName, Type, BypassBasicAuthRestriction, InvitationAccepted, GroupName, and Path properties.

## OUTPUTS

### UiPath.PowerShell.Entities.PmUser

Returns the created PmUser objects ordered by name.

## NOTES

This cmdlet uses the bulk user creation API. All pipeline input is collected during ProcessRecord and the API call is made during EndProcessing. This means output objects are only emitted after all input has been processed.

If a group specified in -GroupName does not exist and does not contain wildcard characters, the cmdlet automatically creates the group. Groups are matched case-insensitively.

The CSV exported by Get-PmUser can be directly piped to New-PmUser for bulk user replication.

## RELATED LINKS

Get-PmUser

Update-PmUser

Copy-PmUser

Remove-PmUser
