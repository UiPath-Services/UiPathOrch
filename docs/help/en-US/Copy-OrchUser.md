---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchUser.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Copy-OrchUser
---

# Copy-OrchUser

## SYNOPSIS

Copies users to another Orchestrator instance.

## SYNTAX

### __AllParameterSets

```
Copy-OrchUser [-Path <string>] [-LiteralPath <string>] [-UserName] <string[]> [-Destination] <string[]> [-Confirm]
 [-FullName <string[]>] [-Type <string[]>] [-UserMappingCsv <string>] [-WhatIf]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Copies user registrations from a source Orchestrator tenant to one or more destination tenants. The cmdlet retrieves detailed user information from the source, resolves the user in the destination directory service, and creates the user at the destination with matching roles, license settings, unattended robot configuration, and execution settings.

The -UserName parameter supports wildcards to copy multiple users at once. The -FullName and -Type parameters provide additional filtering. If the source and destination are the same drive, the copy is skipped silently.

For cross-instance migration, the cmdlet searches the destination organization's directory for a matching user by username (and email address as a fallback). If found, the directory identifier is used; if not found, a warning is displayed and the user is skipped.

Roles are matched by name between source and destination. Roles that do not exist at the destination are removed with an error message. Folder roles are automatically excluded with a warning. Credential store assignments for unattended robots are matched by name at the destination. Unattended robot passwords cannot be copied (the API returns masked values); a warning is displayed advising to update the password with Update-OrchUser.

When copying between different Orchestrator instances where usernames differ, use -UserMappingCsv to specify a CSV that maps source usernames to destination usernames. The mapping CSV can be generated using New-OrchUserMappingCsv. Note that -UserMappingCsv is only supported when the destination is a single drive.

The -UserName, -FullName, -Type, -Path, and -Destination parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values.

Note that -Path is a single string (not string[]), unlike most other cmdlets.

Primary Endpoint: GET /odata/Users, GET /odata/Users({userId}), POST /odata/Users, GET /api/DirectoryService/SearchForUsersAndGroups

OAuth required scopes: OR.Users or OR.Users.Read (source), OR.Users (destination)

Required permissions: Users.View (source), Users.Create (destination)

## EXAMPLES

### Example 1: Copy a specific user to another instance

```powershell
PS Orch1:\> Copy-OrchUser ytsuda@gmail.com Orch2:\
```

Copies the user "ytsuda@gmail.com" from the current tenant (Orch1) to the Orch2 tenant. The -UserName (position 0) and -Destination (position 1) parameters are positional.

### Example 2: Copy all users to another instance

```powershell
PS Orch1:\> Copy-OrchUser * Orch2:\
```

Copies all users from the Orch1 tenant to the Orch2 tenant.

### Example 3: Preview copy with -WhatIf

```powershell
PS Orch1:\> Copy-OrchUser * Orch2:\ -WhatIf
```

Shows which users would be copied without executing the command.

### Example 4: Copy users of a specific type

```powershell
PS Orch1:\> Copy-OrchUser * Orch2:\ -Type DirectoryUser
```

Copies only directory users (excluding groups, robots, and applications) from Orch1 to Orch2.

### Example 5: Copy with user mapping for cross-instance migration

```powershell
PS C:\> Copy-OrchUser -Path Orch1:\ * Orch2:\ -UserMappingCsv C:\temp\user-mapping.csv
```

Copies users with a user mapping CSV file that maps source usernames to destination usernames. Use New-OrchUserMappingCsv to generate the mapping file. When -Path uses an absolute path, the command can be run from any location.

### Example 6: Copy users filtered by full name

```powershell
PS Orch1:\> Copy-OrchUser * Orch2:\ -FullName Yoshifumi*
```

Copies users whose display name starts with "Yoshifumi" from Orch1 to Orch2. Both -UserName and -FullName filters are applied together.

## PARAMETERS

### -Path

Specifies the source drive. If not specified, the current drive is used. Unlike most other cmdlets, this parameter accepts a single string (not an array).

```yaml
Type: System.String
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

### -LiteralPath

Specifies the target folder or drive by literal path -- wildcard metacharacters (`[`, `]`, `*`, `?`) are treated as literal characters rather than patterns. Accepts the same drive-qualified paths as -Path. Its `PSPath` alias also binds the path of items piped from Get-ChildItem / Get-Item, so you can pipe folders directly. Use -LiteralPath instead of -Path when a folder name contains a wildcard metacharacter.

```yaml
Type: System.String
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

### -Destination

Specifies the destination drive names. This is a mandatory parameter. Can be one or more Orchestrator drives for cross-instance migration (e.g., Orch2:\).

```yaml
Type: System.String[]
DefaultValue: None
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

### -FullName

Filters users by display name. Supports wildcards. Tab completion dynamically suggests user display names from the source tenant.

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

### -Type

Filters users by directory type. Valid values are DirectoryUser, DirectoryGroup, DirectoryRobot, and DirectoryExternalApplication. Supports wildcards. Tab completion suggests available type values.

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

### -UserMappingCsv

Specifies the path to a user mapping CSV file for cross-instance migration. The CSV maps source usernames to destination usernames, which is required when copying users across Orchestrator instances where usernames differ. Use New-OrchUserMappingCsv to generate the mapping file. Requires a filesystem path (not an Orch: drive path). Only supported when the destination is a single drive.

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

### -UserName

Specifies the usernames of users to copy. This is a mandatory parameter. Supports wildcards to copy multiple users. Tab completion dynamically suggests usernames from the source tenant.

```yaml
Type: System.String[]
DefaultValue: None
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

### -WhatIf

Shows what would happen if the cmdlet runs. The cmdlet is not run. The output shows the source user details and the destination drive.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: False
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
DefaultValue: False
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

You can pipe UserName, FullName, Type, and Destination values to this cmdlet via their property names.

### System.String

You can pipe the source Path value to this cmdlet via the Path property.

## OUTPUTS

### None

This cmdlet does not produce pipeline output. Created users are managed at the destination drive internally. A warning is displayed if the unattended robot password needs to be updated at the destination.

## NOTES

The -Path parameter is a single string, not a string array. This differs from most other cmdlets in the module that accept string arrays for -Path.

When the source and destination are the same drive, the copy is silently skipped.

Unattended robot passwords cannot be read from the source; only the credential structure is copied. If the source user has an unattended robot with a password, a warning is displayed advising to update the password using Update-OrchUser at the destination.

Classic folder (OrganizationUnit) assignments are migrated by matching folder names between source and destination. Only top-level classic folders with ProvisionType "Manual" are matched.

The -UserMappingCsv parameter is only effective when copying to a single destination drive.

## RELATED LINKS

[Get-OrchUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchUser.md)

[Add-OrchUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Add-OrchUser.md)

[Update-OrchUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Update-OrchUser.md)

[Remove-OrchUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchUser.md)

[New-OrchUserMappingCsv](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/New-OrchUserMappingCsv.md)
