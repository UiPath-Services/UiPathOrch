---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Update-OrchUser.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Update-OrchUser
---

# Update-OrchUser

## SYNOPSIS

Updates users in UiPath Orchestrator tenants.

## SYNTAX

### __AllParameterSets

```
Update-OrchUser [-Path <string[]>] [-LiteralPath <string[]>] [-UserName] <string[]> [-Confirm]
 [-ES_AutoDownloadProcess <string>] [-ES_FontSmoothing <string>]
 [-ES_LoginToConsole <string>] [-ES_ResolutionDepth <string>]
 [-ES_ResolutionHeight <string>] [-ES_ResolutionWidth <string>]
 [-ES_StudioNotifyServer <string>] [-ES_TracingLevel <string>]
 [-IsExternalLicensed <string>] [-MayHavePersonalWorkspace <string>]
 [-MayHaveRobotSession <string>] [-MayHaveUnattendedSession <string>]
 [-MayHaveUserSession <string>] [-Name <string>] [-RestrictToPersonalWorkspace <string>]
 [-Roles <string[]>] [-Surname <string>] [-UpdatePolicyType <string>]
 [-UpdatePolicyVersion <string>] [-UR_CredentialExternalName <string>]
 [-UR_CredentialStore <string>] [-UR_CredentialType <string>]
 [-UR_LimitConcurrentExecution <string>] [-UR_Password <string>] [-UR_UserName <string>]
 [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Updates existing user configurations in UiPath Orchestrator tenants. The cmdlet retrieves the current user details, creates a deep copy, applies the specified modifications, and submits the update only if changes are detected.

The -UserName parameter supports wildcards to update multiple users at once. Only parameters that are explicitly specified are modified; unspecified properties retain their current values on the server.

The -Name (alias: FirstName) and -Surname (alias: LastName) parameters update the user's display name components. The -Roles parameter replaces the entire role assignment; specify an empty string to remove all tenant roles. Folder roles are automatically excluded with a warning.

Unattended robot settings can be modified using the UR_ parameters. The -UR_Password parameter only sets a new password; it does not echo the current password (the API returns masked values). Execution settings for both attended and unattended robots can be updated using the ES_ parameters.

The -UserName, -Roles, and -Path parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values.

Primary Endpoint: GET /odata/Users, GET /odata/Users({userId}), PUT /odata/Users({userId})

OAuth required scopes: OR.Users or OR.Users.Read

Required permissions: Users.View, Users.Edit or Robots.Create or Robots.Edit or Robots.Delete.

## EXAMPLES

### Example 1: Update a user's roles

```powershell
PS Orch1:\> Update-OrchUser ytsuda@gmail.com -Roles "Orchestrator Administrator"
```

Updates the user "ytsuda@gmail.com" to have only the Orchestrator Administrator role. The -Roles parameter replaces all existing tenant roles.

### Example 2: Update multiple users with a wildcard

```powershell
PS Orch1:\> Update-OrchUser ytsuda* -MayHaveRobotSession True
```

Enables the attended robot session for all users whose username starts with "ytsuda".

### Example 3: Update a user's display name

```powershell
PS Orch1:\> Update-OrchUser ytsuda+c@gmail.com -Name Taro -Surname Yamada
```

Updates the first name and last name for the user "ytsuda+c@gmail.com". The -Name parameter has alias FirstName, and -Surname has alias LastName.

### Example 4: Update unattended robot credentials

```powershell
PS Orch1:\> Update-OrchUser UR-1 -UR_UserName 'UIPATH\svc-robot01' -UR_Password 'NewP@ssw0rd' -UR_CredentialStore 'Orchestrator Database'
```

Updates the unattended robot credentials for the user "UR-1", setting a new Windows username, password, and credential store.

### Example 5: Remove all roles from a user

```powershell
PS Orch1:\> Update-OrchUser ytsuda+c@gmail.com -Roles ''
```

Removes all tenant roles from the user "ytsuda+c@gmail.com" by specifying an empty string for -Roles.

### Example 6: Update a user from a specific drive

```powershell
PS C:\> Update-OrchUser -Path Orch1:\ -UserName ytsuda+c@gmail.com -MayHaveUnattendedSession True
```

Updates the user "ytsuda+c@gmail.com" on the Orch1 tenant. When -Path uses an absolute path, the command can be run from any location.

### Example 7: Import updates from CSV

```powershell
PS Orch1:\> Import-Csv C:\temp\user-updates.csv | Update-OrchUser
```

Imports user updates from a CSV file. Pipeline input maps CSV columns to parameters via ValueFromPipelineByPropertyName.

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

### -LiteralPath

Specifies the target folder or drive by literal path -- wildcard metacharacters (`[`, `]`, `*`, `?`) are treated as literal characters rather than patterns. Accepts the same drive-qualified paths as -Path. Its `PSPath` alias also binds the path of items piped from Get-ChildItem / Get-Item, so you can pipe folders directly. Use -LiteralPath instead of -Path when a folder name contains a wildcard metacharacter.

```yaml
Type: System.String[]
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

### -ES_ResolutionHeight

Specifies the height (in pixels) for the remote desktop session resolution.

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

### -ES_ResolutionWidth

Specifies the width (in pixels) for the remote desktop session resolution.

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

### -Name

Specifies the first name (display name) of the user. Alias: FirstName.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases:
- FirstName
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

Specifies the tenant roles to assign to the user. This replaces all existing tenant roles. Specify an empty string to remove all roles. Folder roles are automatically excluded with a warning. Tab completion dynamically suggests available tenant roles. Alias: TenantRoles.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: false
Aliases:
- TenantRoles
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

### -Surname

Specifies the last name (surname) of the user. Alias: LastName.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases:
- LastName
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

Specifies the credential store name for the unattended robot. Tab completion suggests available credential store names. If the specified credential store does not exist, a warning is displayed and the value is ignored.

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

Specifies the credential type for the unattended robot. Tab completion suggests valid values: Default, SmartCard, NCipher, SafeNet, and NoCredential.

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

Specifies the password for the unattended robot Windows credential. The API does not return the current password (it returns a masked value), so the cmdlet only sets new passwords. Unchanged if not specified.

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

Specifies the usernames of users to update. This is a mandatory parameter. Supports wildcards to update multiple users at once. Tab completion dynamically suggests usernames from the target tenant.

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

You can pipe individual property values (such as Name, Surname, or UR_UserName) to this cmdlet via their property names.

### System.String[]

You can pipe UserName, Roles, and Path values to this cmdlet via their property names. CSV pipeline input is supported for bulk updates.

## OUTPUTS

### None

This cmdlet does not produce output. The user is updated in place on the server. If the deep-copied user object is equal to the original after modifications, the update is skipped silently.

## NOTES

Users are tenant-scoped entities. Navigate to the root of an Orch: drive or use -Path to specify target drives.

The cmdlet creates a deep copy of the user before modifications to detect actual changes. If no properties differ between the copy and the original, the API call is skipped.

The masked password value returned by the API is cleared to null before comparison. Only explicitly specified -UR_Password values are sent to the server.

Execution settings (ES_ parameters) are applied to both RobotProvision (attended) and UnattendedRobot execution settings when they exist on the user, except for DirectoryExternalApplication users.

## RELATED LINKS

[Get-OrchUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchUser.md)

[Add-OrchUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Add-OrchUser.md)

[Remove-OrchUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchUser.md)

[Copy-OrchUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchUser.md)
