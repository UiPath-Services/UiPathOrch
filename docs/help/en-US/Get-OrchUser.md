---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchUser.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchUser
---

# Get-OrchUser

## SYNOPSIS

Gets users from UiPath Orchestrator tenants.

## SYNTAX

### __AllParameterSets

```
Get-OrchUser [[-UserName] <string[]>] [[-FullName] <string[]>] [-Type <string[]>] [-ExpandDetails]
 [-Path <string[]>] [-ExportCsv <string>] [-CsvEncoding <Encoding>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets user information from UiPath Orchestrator tenants. Users are tenant-scoped entities that represent directory users, directory groups, robot accounts, and external applications registered in the Orchestrator tenant.

Users have a Type property that indicates their directory type: DirectoryUser, DirectoryGroup, DirectoryRobot, or DirectoryExternalApplication. Each user can have assigned tenant roles, license settings (MayHaveUserSession, MayHaveRobotSession, MayHaveUnattendedSession), personal workspace configuration, and unattended robot settings.

This cmdlet supports filtering by UserName, FullName, and Type. By default it returns shallow user records from the list endpoint. To fetch the per-user detail payload (unattended robot settings, execution settings, role assignments), use `Get-OrchUserDetail` — which makes one API call per matched user and requires an explicit `-UserName` selector.

> **Deprecated:** `-ExpandDetails` is deprecated and will be removed in a future major release. Use `Get-OrchUserDetail` instead. `-ExportCsv` continues to be supported and transparently uses the detail cache to enrich the exported rows (the CSV row shape is the User entity, matching this cmdlet's output type).

The -UserName, -FullName, -Type, and -Path parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values. The -UserName and -FullName completions are dynamically populated from actual tenant users. The -Type completion suggests DirectoryUser, DirectoryGroup, DirectoryRobot, and DirectoryExternalApplication.

The -ExportCsv parameter exports user data to a CSV file including unattended robot settings, execution settings, roles, and license configurations. The exported CSV can be used with Add-OrchUser for import.

Primary Endpoint: GET /odata/Users?$expand=OrganizationUnits,UserRoles

OAuth required scopes: OR.Users or OR.Users.Read

Required permissions: Users.View

## EXAMPLES

### Example 1: Get all users in the current tenant

```powershell
PS Orch1:\> Get-OrchUser
```

Gets all users from the current tenant. Users are tenant-scoped, so the command works from the root of the Orch: drive.

### Example 2: Get a specific user by username

```powershell
PS Orch1:\> Get-OrchUser yoshifumi.tsuda@uipath.com
```

Gets the user with the specified username from the current tenant. The -UserName parameter is positional (position 0) so the parameter name can be omitted.

### Example 3: Get users matching a wildcard pattern

```powershell
PS Orch1:\> Get-OrchUser ytsuda*
```

Gets all users whose username starts with "ytsuda". Wildcard patterns are supported for both -UserName and -FullName.

### Example 4: Get users by type

```powershell
PS Orch1:\> Get-OrchUser -Type DirectoryRobot
```

Gets all robot accounts registered in the tenant. Valid values for -Type are DirectoryUser, DirectoryGroup, DirectoryRobot, and DirectoryExternalApplication.

### Example 5 (deprecated): Get users with detailed information

```powershell
PS Orch1:\> Get-OrchUser -ExpandDetails
```

Emits a deprecation warning and routes through `Get-OrchUserDetail`. Use `Get-OrchUserDetail -UserName '*'` directly instead.

### Example 6: Get a user from a specific drive

```powershell
PS C:\> Get-OrchUser -Path Orch1:\ -UserName yoshifumi.tsuda@uipath.com
```

Gets the specified user from the Orch1 drive. When -Path uses an absolute path (Orch1:\), the command can be run from any location.

### Example 7: Export users to CSV

```powershell
PS Orch1:\> Get-OrchUser -ExportCsv C:\temp\users.csv
```

Exports all users in the current tenant to a CSV file. The CSV includes columns for Path, UserName, FullName, Type, license settings, unattended robot configuration, execution settings, and Roles. The exported CSV can be used with Add-OrchUser for import.

## PARAMETERS

### -Path

Specifies the target Orch: drives. If not specified, the current drive is targeted. Tab completion suggests available Orch: drives.

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

### -CsvEncoding

Specifies the encoding for CSV export. Default is UTF-8 with BOM for Excel compatibility. Tab completion suggests all available system encodings (e.g., utf-8, shift_jis, us-ascii).

```yaml
Type: System.Text.Encoding
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

### -ExpandDetails

**Deprecated.** Routes to `Get-OrchUserDetail` and emits a deprecation warning. Use `Get-OrchUserDetail` directly. When `-ExportCsv` is specified the same detail enrichment runs automatically (and `-ExportCsv` is NOT deprecated, since the CSV row shape matches this cmdlet's output type).

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

### -ExportCsv

Exports users to the specified CSV file path. The CSV includes columns for Path, UserName, FullName, Type, license settings, unattended robot configuration, execution settings, and Roles. When this parameter is specified, -ExpandDetails is implied automatically. Requires a filesystem path (not an Orch: drive path).

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
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -FullName

Specifies the display names of users to retrieve. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests user display names from the target tenant.

```yaml
Type: System.String[]
DefaultValue: None
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

### -UserName

Specifies the usernames of users to retrieve. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests usernames from the target tenant.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
Aliases: []
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

You can pipe usernames and full names to this cmdlet via the UserName and FullName properties.

## OUTPUTS

### UiPath.PowerShell.Entities.User

Returns User objects with properties including UserName, FullName, Type, MayHaveUserSession, MayHaveRobotSession, MayHaveUnattendedSession, MayHavePersonalWorkspace, RestrictToPersonalWorkspace, IsExternalLicensed, RolesList, UnattendedRobot, RobotProvision, UpdatePolicy, and Path.

## NOTES

Users are tenant-scoped entities. Navigate to the root of an Orch: drive or use -Path to specify target drives. Unlike folder-scoped cmdlets, you do not need to navigate to a specific folder.

When -ExpandDetails is specified, the cmdlet makes a separate API call for each user to retrieve detailed information. For tenants with many users, this may take some time. A progress bar is displayed during retrieval.

The -ExportCsv parameter produces a CSV with columns: Path, UserName, FullName, Type, IsExternalLicensed, MayHaveUserSession, MayHaveRobotSession, MayHaveUnattendedSession, MayHavePersonalWorkspace, RestrictToPersonalWorkspace, UpdatePolicyType, UpdatePolicyVersion, UR_UserName, UR_Password, UR_CredentialStore, UR_CredentialExternalName, UR_CredentialType, UR_LimitConcurrentExecution, ES_TracingLevel, ES_StudioNotifyServer, ES_LoginToConsole, ES_ResolutionWidth, ES_ResolutionHeight, ES_ResolutionDepth, ES_FontSmoothing, ES_AutoDownloadProcess, Roles.

## RELATED LINKS

[Get-OrchUserDetail](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchUserDetail.md)

[Add-OrchUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Add-OrchUser.md)

[Update-OrchUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Update-OrchUser.md)

[Remove-OrchUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchUser.md)

[Copy-OrchUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchUser.md)

[Get-OrchUserPrivilege](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchUserPrivilege.md)
