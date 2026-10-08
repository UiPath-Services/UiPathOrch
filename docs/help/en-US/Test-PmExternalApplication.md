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

Checks external applications you registered for other tools: requests a token for a confidential one, or checks the registration, and explains what keeps it from working.

## SYNTAX

### __AllParameterSets

```
Test-PmExternalApplication [[-Name] <string[]>] [[-AppSecret] <string>] [[-Scope] <string[]>]
 [-AppId <string>] [-RedirectUri <string>] [-Path <string[]>] [-LiteralPath <string[]>]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

The `Test-PmExternalApplication` cmdlet checks external applications that you registered for another tool or script -- for example with `New-PmExternalApplication` -- before you hand them over. Name them with -Name (wildcards allowed) or -AppId; it returns one result per application.

With -AppSecret, it requests a token from the identity server of the target drive with the client credentials grant, as a script using a confidential application does, and returns whether a token was issued, the error when it was not, the scopes requested and granted, the token itself (**AccessToken**, valid for **ExpiresIn** seconds, to call the API with as the application would) and the likely causes of a refusal. A secret belongs to one application, so -Name must then match one.

Without -AppSecret, it signs nothing in and checks each application's registration instead, returning the result with **Succeeded** empty:

- a confidential application: it has a secret, not all of its secrets have expired or expire within 30 days, it has scopes, an application with user scopes only has a redirect URL, and the scopes given with -Scope are registered as application scopes;
- a non-confidential application, which signs in in the browser and returns to the other tool's redirect URL: it has user scopes and a redirect URL, the scopes given with -Scope are registered as user scopes, and the URL given with -RedirectUri is the registered one.

So `Test-PmExternalApplication *` reviews every application of the organization.

When a token is refused, the token endpoint says little: a wrong App Secret or an unknown App ID gets a bare `invalid_client`, a scope not registered on the application a bare `invalid_scope`, and a user scope `invalid_request` "Client=... is not allowed to access User scopes" without saying which scope. The cmdlet reads the application's registration to name the cause:

- no application with this App ID is registered in the organization;
- the application is non-confidential, so it cannot use client credentials;
- every secret of the application has expired, or the App Secret does not match;
- a requested scope is registered on the application as a user scope (a token requested with client credentials carries application scopes only), or is not registered at all;
- the scope value is longer than the server accepts.

The registrations are read through the target drive or another drive of the same organization that is already signed in, such as a drive using your own account. Drives not signed in are not used, so no sign-in starts. A name without wildcards that matches no application is an error; a pattern that matches none is not.

With neither -Name nor -AppId, the cmdlet tests the confidential application the target drive is configured with (its AppId, AppSecret and Scope in the configuration file). The token is not stored on the drive.

Tab completion of -Name offers the organization's applications. Tab completion of -Scope offers the scopes registered on the application -- application scopes, or user scopes for a non-confidential one -- leaving out those already given.

Primary Endpoint: POST /identity_/connect/token (Automation Cloud, Automation Suite) or /identity/connect/token (standalone)

OAuth required scopes: none for the token request. Reading the registration needs a signed-in drive of the organization on which `Get-PmExternalApplication` works (GET /api/ExternalClient/{partitionGlobalId}, Identity Server).

## EXAMPLES

### Example 1: Review every application of the organization

```powershell
PS C:\> Test-PmExternalApplication * -Path Orch1: | Where-Object Problems
```

Checks the registration of every external application, without signing in, and shows those with problems: no secret, secrets expired or expiring within 30 days, no scope, no redirect URL where one is needed.

### Example 2: Test an application before handing it over

```powershell
PS C:\> Test-PmExternalApplication NightlyBatch $secret OR.Jobs*, OR.Queues -Path Orch1:
```

Requests a token for the application named NightlyBatch with OR.Queues and every application scope of it that begins with OR.Jobs, and lists the causes when it is refused.

### Example 3: Request every application scope registered on an application

```powershell
PS C:\> Get-PmExternalApplication -Path Orch1: -Name OCM | Test-PmExternalApplication -AppSecret $secret -Path Orch1:
```

Pipes the application (its name binds to -Name and its id to -AppId) and, since -Scope is omitted, requests all the application scopes registered on it.

### Example 4: Call the API with the application's token

```powershell
PS C:\> $t = Test-PmExternalApplication -Path Orch1c:
PS C:\> Invoke-RestMethod 'https://cloud.uipath.com/myorg/mytenant/orchestrator_/odata/Folders' -Headers @{ Authorization = "Bearer $($t.AccessToken)" }
```

Requests a token as the Orch1c drive's confidential application and calls Orchestrator with it, as a script using that application would.

### Example 5: Check a non-confidential application without signing in

```powershell
PS C:\> Test-PmExternalApplication MyTool -Scope OR.Folders*, OR.Jobs -RedirectUri http://localhost:8085/callback -Path Orch1:
```

Checks that the scopes are registered on the application as user scopes and that the redirect URL is the registered one, and lists what is not.

## PARAMETERS

### -Name

Specifies the names of the applications to test, as `Get-PmExternalApplication` shows them. Wildcard characters are permitted; each application that matches is tested. With neither -Name nor -AppId, the drive's own confidential application is tested.

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

### -AppId

Specifies the App ID (client ID) of one application, instead of -Name; it wins when both are given, as when Get-PmExternalApplication is piped.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: false
Aliases:
- ClientId
- id
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

### -AppSecret

Specifies the App Secret (client secret) of a confidential application; a token is then requested. Omit it for a non-confidential application, whose registration is checked instead.

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

Specifies the scopes to request: several values separated by commas, or by spaces within one value, as in the configuration file's Scope. A wildcard pattern expands against the application scopes registered on the application (or, when the registration cannot be read, against the organization's scopes that can be application scopes); a pattern matching none is an error, and no token is requested. A plain name is sent as given, registered or not, so the server's answer to it can be seen. When omitted with -AppId, every application scope registered on the application is requested; when omitted without -AppId, the drive's configured Scope is used. For a non-confidential application checked without -AppSecret, the scopes are compared with its user scopes instead, and wildcards expand against those; omitted, only the registration itself is checked.

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

### -RedirectUri

For a non-confidential application, without -AppSecret: the redirect URL the other tool signs in with. It is compared with the URL registered on the application.

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

One object per drive: **Path**, **AppId**, **Name** and **IsConfidential** (from the registration), **Succeeded** (empty when no token was requested), **Error** and **ErrorDescription** (the OAuth error of a refusal), **RequestedScope**, **GrantedScope**, **ExpiresIn** (seconds), **AccessToken** (the issued token) and **Problems** (the causes found).

## NOTES

Answers measured on Automation Cloud (2026-10-08): a non-confidential application is refused with `unauthorized_client`; a wrong App Secret or an unknown App ID with `invalid_client`; a scope not registered on the application with `invalid_scope`; a user scope, even one among valid application scopes, with `invalid_request` "is not allowed to access User scopes". Application scopes of different services (Orchestrator, Test Manager, Platform Management, Conversational Agents) can be requested together.

## RELATED LINKS

[Get-PmExternalApplication](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmExternalApplication.md)

[Resolve-OrchAuthError](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Resolve-OrchAuthError.md)
