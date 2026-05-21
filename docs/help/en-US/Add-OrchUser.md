---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Add-OrchUser.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Add-OrchUser
---

# Add-OrchUser

## SYNOPSIS

Adds users to UiPath Orchestrator tenants.

## SYNTAX

### __AllParameterSets

```
Add-OrchUser [-Path <string[]>] [[-Type] <string[]>] [-UserName] <string[]>
 [[-Roles] <string[]>] [-Confirm] [-ES_AutoDownloadProcess <string>]
 [-ES_FontSmoothing <string>] [-ES_LoginToConsole <string>] [-ES_ResolutionDepth <int>]
 [-ES_ResolutionHeight <int>] [-ES_ResolutionWidth <int>]
 [-ES_StudioNotifyServer <string>] [-ES_TracingLevel <string>]
 [-IsExternalLicensed <string>] [-MayHavePersonalWorkspace <string>]
 [-MayHaveRobotSession <string>] [-MayHaveUnattendedSession <string>]
 [-MayHaveUserSession <string>] [-RestrictToPersonalWorkspace <string>]
 [-UpdatePolicyType <string>] [-UpdatePolicyVersion <string>]
 [-UR_CredentialExternalName <string>] [-UR_CredentialStore <string>]
 [-UR_CredentialType <string>] [-UR_LimitConcurrentExecution <string>]
 [-UR_Password <string>] [-UR_UserName <string>] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Adds users to UiPath Orchestrator tenants by searching the connected directory service and registering matching accounts. The cmdlet supports adding directory users, directory groups, robot accounts, and external applications.

The -Type parameter specifies the kind of directory entry to add. Valid values are DirectoryUser, DirectoryGroup, DirectoryRobot, and DirectoryExternalApplication. If -Type is not specified, all matching types are considered. The -UserName parameter is mandatory and triggers a directory search; the tab completer requires at least one character to initiate the search and excludes users already registered in the tenant.

Users can be assigned tenant roles via the -Roles parameter. Folder roles are automatically excluded with a warning. License settings such as -MayHaveUserSession, -MayHaveRobotSession, -MayHaveUnattendedSession, and -MayHavePersonalWorkspace control which session types the user is allowed. For robot accounts (-Type DirectoryRobot), MayHaveUnattendedSession is forced to true and MayHaveUserSession is forced to false.

Unattended robot credentials can be configured using the UR_ parameters. If -UR_Password is specified without -UR_CredentialStore, the "Orchestrator Database" credential store is used by default. If no password is specified, -UR_CredentialType defaults to "NoCredential". Execution settings for both attended (RobotProvision) and unattended robots can be configured using the ES_ parameters.

This cmdlet supports pipeline input from CSV files exported by Get-OrchUser -ExportCsv. When multiple pipeline records specify the same user, settings are merged with later values taking precedence, and roles are accumulated.

The -Type, -Roles, -UserName, and -Path parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values.

Primary Endpoint: GET /odata/Users?$expand=OrganizationUnits,UserRoles, GET /odata/Roles?$expand=Permissions, GET /api/DirectoryService/SearchForUsersAndGroups?domain=autogen&prefix={prefix}&searchContext=All, POST /odata/Users

OAuth required scopes: OR.Users

Required permissions: (Users.View or Units.Edit or SubFolders.Edit), (Roles.View or Units.Edit or SubFolders.Edit)

## EXAMPLES

### Example 1: Add a directory user with default settings

```powershell
PS Orch1:\> Add-OrchUser DirectoryUser ytsuda@gmail.com
```

Adds the directory user "ytsuda@gmail.com" to the current tenant. The -Type (position 0) and -UserName (position 1) parameters are positional.

### Example 2: Add a user with tenant roles

```powershell
PS Orch1:\> Add-OrchUser DirectoryUser ytsuda+c@gmail.com Administrator,Automation_Developer
```

Adds the user "ytsuda+c@gmail.com" with the Administrator and Automation_Developer tenant roles. The -Roles parameter (position 2) accepts comma-separated role names.

### Example 3: Add a user with attended robot session enabled

```powershell
PS Orch1:\> Add-OrchUser DirectoryUser ytsuda@gmail.com -MayHaveRobotSession True
```

Adds the user and enables the attended robot session (MayHaveRobotSession), allowing the user to run attended automations.

### Example 4: Add a robot account with unattended credentials

```powershell
PS Orch1:\> Add-OrchUser DirectoryRobot testrobot01 -MayHaveUnattendedSession True -UR_UserName 'DOMAIN\testrobot01' -UR_Password 'P@ssw0rd'
```

Adds a robot account with unattended robot credentials. When -UR_Password is specified, the "Orchestrator Database" credential store is used by default.

### Example 5: Add a user from a specific drive

```powershell
PS C:\> Add-OrchUser -Path Orch1:\ DirectoryUser ytsuda@gmail.com
```

Adds the user "ytsuda@gmail.com" to the Orch1 tenant. When -Path uses an absolute path, the command can be run from any location.

### Example 6: Preview adding users with -WhatIf

```powershell
PS Orch1:\> Add-OrchUser DirectoryUser ytsuda@gmail.com,ytsuda+c@gmail.com -WhatIf
```

Shows which users would be added without executing the command.

### Example 7: Import users from CSV

```powershell
PS Orch1:\> Import-Csv C:\temp\users.csv | Add-OrchUser
```

Imports users from a CSV file exported by Get-OrchUser -ExportCsv. Pipeline input maps CSV columns to parameters via ValueFromPipelineByPropertyName. Duplicate usernames are merged with roles accumulated across rows.

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

### -ES_AutoDownloadProcess

Specifies whether processes are automatically downloaded to the robot machine. Valid values are True and False. Tab completion suggests available values.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -ES_FontSmoothing

Specifies whether font smoothing is enabled for the remote desktop session. Valid values are True and False. Tab completion suggests available values.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -ES_LoginToConsole

Specifies whether the robot logs in to the console session. Valid values are True and False. Tab completion suggests available values.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -ES_ResolutionDepth

Specifies the color depth (in bits) for the remote desktop session resolution.

```yaml
Type: System.Nullable`1[System.Int32]
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

### -ES_ResolutionHeight

Specifies the height (in pixels) for the remote desktop session resolution.

```yaml
Type: System.Nullable`1[System.Int32]
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

### -ES_ResolutionWidth

Specifies the width (in pixels) for the remote desktop session resolution.

```yaml
Type: System.Nullable`1[System.Int32]
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

### -ES_StudioNotifyServer

Specifies whether UiPath Studio notifies the server of activity. Valid values are True and False. Tab completion suggests available values.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -ES_TracingLevel

Specifies the tracing level for execution logging. Tab completion suggests valid values: Verbose, Trace, Information, Warning, Error, Critical, and Off.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -IsExternalLicensed

Specifies whether the user consumes an external license. Valid values are True and False. Tab completion suggests available values.

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

### -MayHavePersonalWorkspace

Specifies whether the user may have a personal workspace folder. Valid values are True and False. Tab completion suggests available values.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -MayHaveRobotSession

Specifies whether the user may have an attended robot session. Valid values are True and False. Tab completion suggests available values.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -MayHaveUnattendedSession

Specifies whether the user may have an unattended robot session. Valid values are True and False. Tab completion suggests available values.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -MayHaveUserSession

Specifies whether the user may have a standard user interface (attended) session. Valid values are True and False. Tab completion suggests available values.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -RestrictToPersonalWorkspace

Specifies whether the user is restricted to their personal workspace only. Valid values are True and False. Tab completion suggests available values.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Roles

Specifies the tenant roles to assign to the user. Supports multiple comma-separated values. Folder roles are automatically excluded with a warning. Tab completion dynamically suggests available tenant roles from the target drives. Alias: TenantRoles.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: false
Aliases:
- TenantRoles
ParameterSets:
- Name: (All)
  Position: 2
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Type

Specifies the directory type of the user to add. Valid values are DirectoryUser, DirectoryGroup, DirectoryRobot, and DirectoryExternalApplication. Supports wildcards. Tab completion suggests available type values.

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

### -UpdatePolicyType

Specifies the robot update policy type. Tab completion suggests valid values: None, LatestPatch, LatestVersion, and SpecificVersion.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -UpdatePolicyVersion

Specifies the target version for the robot update policy. Required when -UpdatePolicyType is LatestPatch or SpecificVersion. Tab completion suggests available versions.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -UR_CredentialExternalName

Specifies the external name for the unattended robot credential when using an external credential store.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -UR_CredentialStore

Specifies the credential store name for the unattended robot. If -UR_Password is specified without this parameter, "Orchestrator Database" is used by default. Tab completion suggests available credential store names.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -UR_CredentialType

Specifies the credential type for the unattended robot. Tab completion suggests valid values: Default, SmartCard, NCipher, SafeNet, and NoCredential. Defaults to "NoCredential" when no password is specified, or "Default" when a password is provided.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -UR_LimitConcurrentExecution

Specifies whether to limit concurrent execution for the unattended robot. Valid values are True and False. Tab completion suggests available values.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -UR_Password

Specifies the password for the unattended robot Windows credential. When specified, -UR_CredentialType defaults to "Default" and the "Orchestrator Database" credential store is used unless -UR_CredentialStore is specified.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -UR_UserName

Specifies the Windows username (e.g., DOMAIN\username) for the unattended robot credential.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -UserName

Specifies the usernames to add. This is a mandatory parameter. Tab completion searches the connected directory service for matching accounts; at least one character must be typed to initiate the search. Users already registered in the tenant are excluded from suggestions.

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

### -WhatIf

Shows what would happen if the cmdlet runs.
The cmdlet is not run.

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

### System.String

You can pipe individual property values (such as IsExternalLicensed or UR_UserName) to this cmdlet via their property names.

### System.String[]

You can pipe Type, UserName, Roles, and Path values to this cmdlet via their property names. CSV pipeline input is supported for bulk import.

### System.Int32

You can pipe ES_ResolutionWidth, ES_ResolutionHeight, and ES_ResolutionDepth values to this cmdlet via their property names.

## OUTPUTS

### UiPath.PowerShell.Entities.User

Returns User objects for newly created users with properties including UserName, FullName, Type, MayHaveUserSession, MayHaveRobotSession, MayHaveUnattendedSession, MayHavePersonalWorkspace, RolesList, UnattendedRobot, and Path.

## NOTES

Users are tenant-scoped entities. Navigate to the root of an Orch: drive or use -Path to specify target drives.

The cmdlet searches the connected directory service for the specified -UserName. If the user already exists in the tenant, a warning is displayed and the user is skipped.

For robot accounts (-Type DirectoryRobot), MayHaveUnattendedSession is forced to true and MayHaveUserSession is forced to false. An UnattendedRobot configuration is always created for robot accounts.

When importing from CSV via pipeline, duplicate entries for the same user on the same drive are merged. Boolean and string settings take the latest non-null value, and roles are accumulated across all rows for the same user.

## RELATED LINKS

[Get-OrchUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchUser.md)

[Update-OrchUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Update-OrchUser.md)

[Remove-OrchUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchUser.md)

[Copy-OrchUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchUser.md)
