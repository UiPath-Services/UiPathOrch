---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Update-OrchCurrentUserURPassword.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Update-OrchCurrentUserURPassword
---

# Update-OrchCurrentUserURPassword

## SYNOPSIS

Updates the unattended robot password for the currently authenticated user.

## SYNTAX

### __AllParameterSets

```
Update-OrchCurrentUserURPassword [[-Path] <string[]>] -Password <securestring>
 -Confirmation <securestring> [-WhatIf] [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Updates the unattended robot (UR) password for the currently authenticated user in UiPath Orchestrator. Both -Password and -Confirmation are mandatory and must match; if they do not match, an error is returned.

The passwords are SecureString parameters with DontShow set, so they will not appear in command history or logging. PowerShell will prompt interactively for these values if they are not provided on the command line.

This cmdlet first retrieves the current user via the GetCurrentUser API to determine the user ID, then calls the UpdateCurrentUserURPassword endpoint.

The -Path parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available Orch: drives.

Primary Endpoint: POST /odata/Users({userId})/UiPath.Server.Configuration.OData.UpdateCurrentUserURPassword

OAuth required scopes: OR.Users

Required permissions: None (the user can update their own unattended robot password)

## EXAMPLES

### Example 1: Update the unattended robot password interactively

```powershell
PS Orch1:\> Update-OrchCurrentUserURPassword
```

Prompts interactively for both -Password and -Confirmation SecureString values, then updates the unattended robot password for the currently authenticated user.

### Example 2: Update the password with pre-created SecureStrings

```powershell
PS Orch1:\> $pwd = Read-Host -AsSecureString -Prompt 'New Password'
PS Orch1:\> Update-OrchCurrentUserURPassword -Password $pwd -Confirmation $pwd
```

Creates a SecureString from user input and passes it to both -Password and -Confirmation parameters.

### Example 3: Update the password from a specific drive

```powershell
PS C:\> Update-OrchCurrentUserURPassword Orch1:\
```

Updates the unattended robot password on the Orch1 drive. The -Path parameter is positional (position 0) so the parameter name can be omitted. When -Path uses an absolute path, the command can be run from any location.

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
  Position: 0
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Confirmation

Specifies the confirmation of the new unattended robot password. Must match the -Password value. This is a mandatory SecureString parameter. PowerShell will prompt interactively if not provided.

```yaml
Type: System.Security.SecureString
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Password

Specifies the new unattended robot password. This is a mandatory SecureString parameter. PowerShell will prompt interactively if not provided.

```yaml
Type: System.Security.SecureString
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
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

### System.String[]

You can pipe drive paths to this cmdlet via the Path property.

## OUTPUTS

### None

This cmdlet does not produce output. The password is updated on the server.

## NOTES

Both -Password and -Confirmation must contain identical values. If they do not match, an error is returned with the message "Password does not match."

The SecureString values are converted to plain text internally for the API call. The DontShow attribute prevents the values from appearing in command-line help or tab completion suggestions.

## RELATED LINKS

[Get-OrchCurrentUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchCurrentUser.md)

[Update-OrchUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Update-OrchUser.md)
