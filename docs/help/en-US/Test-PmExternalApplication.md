---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Test-PmExternalApplication.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 10/08/2026
PlatyPS schema version: 2024-05-01
title: Test-PmExternalApplication
---

# Test-PmExternalApplication

## SYNOPSIS

Requests a token for an external application with client credentials and explains why it is refused.

## SYNTAX

### __AllParameterSets

```
Test-PmExternalApplication [[-AppId] <string>] [[-AppSecret] <string>] [[-Scope] <string[]>]
 [-Path <string[]>] [-LiteralPath <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

The `Test-PmExternalApplication` cmdlet requests a token from the identity server of the target drive with the client credentials grant, as a script or tool using a confidential external application does, and returns one result per drive: whether a token was issued, the error when it was not, the scopes requested and granted, and the likely causes of a refusal.

The token endpoint says little about a refusal: a wrong App Secret or an unknown App ID gets a bare `invalid_client`, a scope not registered on the application a bare `invalid_scope`, and a user scope `invalid_request` "Client=... is not allowed to access User scopes" without saying which scope. The cmdlet reads the application's registration in the organization to name the cause:

- no application with this App ID is registered in the organization;
- the application is non-confidential, so it cannot use client credentials;
- every secret of the application has expired, or the App Secret does not match;
- a requested scope is registered on the application as a user scope (a token requested with client credentials carries application scopes only), or is not registered at all;
- the scope value is longer than the server accepts.

The registration is read through the target drive or another drive of the same organization that is already signed in, such as a drive using your own account, since the drive under test often lacks the Platform Management scopes needed to read it. Drives not signed in are not used, so no sign-in starts. When the registration cannot be read, the result says so and only the error is reported.

Without -AppId, the cmdlet tests the confidential application the target drive is configured with (its AppId, AppSecret and Scope in the configuration file). The token is used only to read the scopes granted and is then dropped: it is not stored on the drive and not returned.

Tab completion of -AppId offers the organization's applications, with each name as the tip; what you type is matched against the name as well, so typing part of a name finds the App ID. Tab completion of -Scope offers the application scopes registered on the application, leaving out those already given.

Primary Endpoint: POST /identity_/connect/token (Automation Cloud, Automation Suite) or /identity/connect/token (standalone)

OAuth required scopes: none for the token request. Reading the registration needs a signed-in drive of the organization on which `Get-PmExternalApplication` works (GET /api/ExternalClient/{partitionGlobalId}, Identity Server).

## EXAMPLES

### Example 1: Test the confidential application of a drive

```powershell
PS C:\> Test-PmExternalApplication -Path Orch1c:
```

Requests a token with the AppId, AppSecret and Scope that the Orch1c drive is configured with.

### Example 2: Test an application before configuring it

```powershell
PS C:\> Test-PmExternalApplication 32ae9da7-db02-4f16-bd32-bd75a19d70bc $secret OR.Jobs*, OR.Assets -Path Orch1:
```

Requests a token for the application with OR.Assets and every application scope of the application that begins with OR.Jobs, on the identity server of Orch1, and lists the causes when it is refused.

### Example 3: Request every application scope registered on an application

```powershell
PS C:\> Get-PmExternalApplication -Path Orch1: -Name OCM | Test-PmExternalApplication -AppSecret $secret -Path Orch1:
```

Pipes the application (its id binds to -AppId) and, since -Scope is omitted, requests all the application scopes registered on it.

## PARAMETERS

### -AppId

Specifies the App ID (client ID) of the external application. Without it, the cmdlet tests the confidential application the target drive is configured with.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: false
Aliases:
- ClientId
- id
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

### -AppSecret

Specifies the App Secret (client secret) of the application. Required with -AppId.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: false
Aliases:
- ClientSecret
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

### -Scope

Specifies the scopes to request: several values separated by commas, or by spaces within one value, as in the configuration file's Scope. A wildcard pattern expands against the application scopes registered on the application (or, when the registration cannot be read, against the organization's scopes that can be application scopes); a pattern matching none is an error, and no token is requested. A plain name is sent as given, registered or not, so the server's answer to it can be seen. When omitted with -AppId, every application scope registered on the application is requested; when omitted without -AppId, the drive's configured Scope is used.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
Aliases: []
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

### -Path

Specifies the drive whose identity server is asked for the token. If not specified, the current drive is used.

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

Specifies the target drive by literal path -- wildcard metacharacters (`[`, `]`, `*`, `?`) are treated as literal characters rather than patterns. Its `PSPath` alias also binds the path of items piped from Get-ChildItem / Get-Item.

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String

You can pipe objects with **AppId** (or **id**, **ClientId**), **AppSecret** and **Scope** properties, such as the output of Get-PmExternalApplication.

## OUTPUTS

### UiPath.PowerShell.Commands.ExternalApplicationTestResult

One object per drive: **Path**, **AppId**, **Name** and **IsConfidential** (from the registration), **Succeeded**, **Error** and **ErrorDescription** (the OAuth error of a refusal), **RequestedScope**, **GrantedScope**, **ExpiresIn** (seconds) and **Problems** (the causes found).

## NOTES

Answers measured on Automation Cloud (2026-10-08): a non-confidential application is refused with `unauthorized_client`; a wrong App Secret or an unknown App ID with `invalid_client`; a scope not registered on the application with `invalid_scope`; a user scope, even one among valid application scopes, with `invalid_request` "is not allowed to access User scopes". Application scopes of different services (Orchestrator, Test Manager, Platform Management, Conversational Agents) can be requested together.

## RELATED LINKS

[Get-PmExternalApplication](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmExternalApplication.md)

[Resolve-OrchAuthError](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Resolve-OrchAuthError.md)
