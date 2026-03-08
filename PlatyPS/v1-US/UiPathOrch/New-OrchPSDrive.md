---
document type: cmdlet
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/08/2026
PlatyPS schema version: 2024-05-01
title: New-OrchPSDrive
---

# New-OrchPSDrive

## SYNOPSIS

Creates a new UiPathOrch PSDrive with the specified parameters.

## SYNTAX

### AppAuth

```
New-OrchPSDrive [-Name] <string> [-Root] <string> [-Description <string>] [-IdentityUrl <string>]
 [-AppId <string>] [-AppSecret <string>] [-RedirectUrl <string>] [-HttpListener <string>]
 [-OAuthScope <string>] [-IgnoreSslErrors <bool>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### TokenAuth

```
New-OrchPSDrive [-Name] <string> [-Root] <string> [-Description <string>] [-OAuthScope <string>]
 [-AccessToken <string>] [-IgnoreSslErrors <bool>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### UserAuth

```
New-OrchPSDrive [-Name] <string> [-Root] <string> [-Description <string>] [-Username <string>]
 [-Password <string>] [-IgnoreSslErrors <bool>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

The `New-OrchPSDrive` cmdlet creates a new UiPathOrch PSDrive without using the configuration file. You specify the drive name, root URL, and authentication parameters directly. This is useful for creating temporary drives, testing connections, or scripting drive creation.

Three authentication methods are supported:

- **AppAuth**: Uses AppId and AppSecret (Confidential App) or AppId and RedirectUrl (Public App) for OAuth authentication.
- **TokenAuth**: Uses a pre-obtained access token.
- **UserAuth**: Uses username and password for on-premises Orchestrator.

The created drive is registered in the Global scope and is available for the duration of the session. It is not saved to the configuration file.

Primary Endpoint: (none)

OAuth required scopes: (none)

Required permissions: (none)

## EXAMPLES

### Example 1: Create a drive with Confidential App authentication

```powershell
PS C:\> New-OrchPSDrive Orch1 https://cloud.uipath.com/myorg/mytenant -AppId '12345678-abcd-1234-abcd-123456789012' -AppSecret 'mySecret' -OAuthScope 'OR.Folders.Read OR.Assets OR.Jobs'
```

Creates a new drive named Orch1 using Confidential App (AppId + AppSecret) authentication.

### Example 2: Create a drive with an access token

```powershell
PS C:\> New-OrchPSDrive Orch1 https://cloud.uipath.com/myorg/mytenant -AccessToken $token -OAuthScope 'OR.Folders.Read OR.Assets'
```

Creates a new drive named Orch1 using a pre-obtained access token.

### Example 3: Create a drive for on-premises Orchestrator

```powershell
PS C:\> New-OrchPSDrive OnPrem https://orchestrator.local -Username admin -Password 'P@ssw0rd'
```

Creates a new drive named OnPrem using username and password authentication for an on-premises Orchestrator instance.

### Example 4: Preview with -WhatIf

```powershell
PS C:\> New-OrchPSDrive Orch1 https://cloud.uipath.com/myorg/mytenant -AppId '12345678-abcd-1234-abcd-123456789012' -AppSecret 'mySecret' -WhatIf
```

Shows what drive would be created without actually creating it.

## PARAMETERS

### -Name

Specifies the name of the new PSDrive. This name is used to reference the drive (e.g., `Orch1:`).

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 0
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Root

Specifies the root URL of the Orchestrator tenant (e.g., `https://cloud.uipath.com/myorg/mytenant`).

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 1
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Description

Specifies a description for the new PSDrive.

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

### -IdentityUrl

Specifies the Identity Server URL for OAuth authentication. If not specified, the URL is derived from the Root URL.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: AppAuth
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -AppId

Specifies the Application ID for OAuth authentication. This is the external application ID registered in the Orchestrator admin panel.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: AppAuth
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -AppSecret

Specifies the Application Secret for Confidential App OAuth authentication.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: AppAuth
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -RedirectUrl

Specifies the redirect URL for Public App OAuth authentication.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: AppAuth
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -HttpListener

Specifies the HTTP listener URL for receiving the OAuth callback during Public App authentication.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: AppAuth
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -OAuthScope

Specifies the OAuth scopes to request (e.g., `OR.Folders.Read OR.Assets OR.Jobs`).

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: AppAuth
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
- Name: TokenAuth
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -AccessToken

Specifies a pre-obtained access token for authentication.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: TokenAuth
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Username

Specifies the username for on-premises Orchestrator authentication.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: UserAuth
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Password

Specifies the password for on-premises Orchestrator authentication.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: UserAuth
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -IgnoreSslErrors

Specifies whether to ignore SSL certificate errors when connecting to the Orchestrator.

```yaml
Type: System.Nullable`1[System.Boolean]
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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None

This cmdlet does not accept pipeline input.

## OUTPUTS

### UiPath.PowerShell.Core.OrchDriveInfo

Returns the created OrchDriveInfo object representing the new PSDrive.

## NOTES

The drive created by this cmdlet is not saved to the configuration file. It exists only for the duration of the current session. To persist drive configurations, use `Edit-OrchConfig` to add the drive to UiPathOrchConfig.json.

## RELATED LINKS

Mount-OrchPSDrive

Get-OrchPSDrive

Edit-OrchConfig

Get-OrchConfigPath
