---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/New-PmExternalApplication.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 10/08/2026
PlatyPS schema version: 2024-05-01
title: New-PmExternalApplication
---

# New-PmExternalApplication

## SYNOPSIS

Registers an external application in the organization.

## SYNTAX

### __AllParameterSets

```
New-PmExternalApplication [-Path <string[]>] [-LiteralPath <string[]>] [-Name] <string>
 [-ApplicationScope <string[]>] [-Confirm] [-IsConfidential <string>]
 [-RedirectUri <string>] [-UserScope <string[]>] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

The `New-PmExternalApplication` cmdlet registers an external application in the organization of the target drive, as the Add Application page of the admin portal does: a name, the application type, its scopes and a redirect URL. It returns the new application with its App ID and, for a confidential application, its App Secret. The secret is shown only now; keep it.

A confidential application can hold both kinds of scope:

- **Application scopes** (-ApplicationScope) are what it receives when it requests a token with client credentials, with no user signed in -- as a script, or a UiPathOrch drive with an AppSecret, does.
- **User scopes** (-UserScope) are what it receives on behalf of a user who signs in.

A non-confidential application has no secret, so it never uses client credentials and takes user scopes only; -ApplicationScope with `-IsConfidential false` is refused before anything is sent.

Scope parameters take several values separated by commas, or by spaces within one value, as an OAuth scope string. A wildcard pattern expands against the organization's scopes that can serve as that kind of scope; a pattern matching none is an error, and nothing is created. Tab completion offers the same scopes, leaving out those already given. The organization's scopes come from GET /api/ExternalApiResource, the list `Get-PmExternalApiResource` shows.

An application with the same name as an existing one is refused: the API would accept it, and the portal lists applications by name.

Primary Endpoint: POST /api/ExternalClient (Identity Server)

OAuth required scopes: (Identity Server API - no per-endpoint scopes)

## EXAMPLES

### Example 1: Register a confidential application for a script

```powershell
PS C:\> New-PmExternalApplication -Path Orch1: NightlyBatch -ApplicationScope OR.Jobs*, OR.Queues
```

Registers a confidential application whose client-credentials token carries OR.Jobs, OR.Jobs.Read, OR.Jobs.Write and OR.Queues, and returns its App ID and App Secret.

### Example 2: Register a non-confidential application for interactive sign-in

```powershell
PS C:\> New-PmExternalApplication -Path Orch1: MyTool -IsConfidential false -UserScope 'OR.Folders.Read OR.Users.Read' -RedirectUri http://localhost:8765/
```

Registers a non-confidential application that a user signs in to in the browser, with user scopes only.

### Example 3: Check the new application

```powershell
PS C:\> $app = New-PmExternalApplication -Path Orch1: NightlyBatch -ApplicationScope OR.Jobs
PS C:\> Test-PmExternalApplication $app.id $app.secret -Path Orch1:
```

Registers an application and requests a token with it right away.

## PARAMETERS

### -Name

Specifies the name of the application.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -IsConfidential

Specifies whether the application is confidential (true) or non-confidential (false). When omitted, the application is confidential, the type that takes application scopes.

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

### -ApplicationScope

Specifies the scopes the application receives with client credentials. Several values separated by commas, or by spaces within one value; wildcards expand against the organization's scopes that can be application scopes. Not allowed with `-IsConfidential false`.

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

### -UserScope

Specifies the scopes the application receives on behalf of a signed-in user. Several values separated by commas, or by spaces within one value; wildcards expand against the organization's scopes that can be user scopes.

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

### -RedirectUri

Specifies the redirect URL a user's sign-in returns to, for an application used with user scopes.

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

Specifies the drive whose organization the application is registered in. If not specified, the current drive is used.

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

### System.String

You can pipe objects with **Name**, **IsConfidential**, **ApplicationScope**, **UserScope** and **RedirectUri** properties.

## OUTPUTS

### UiPath.PowerShell.Entities.ExternalClientCreated

The new application: its name, App ID (**id**) and, for a confidential application, App Secret (**secret**).

## NOTES

The cmdlet clears the cached application list of the organization, so `Get-PmExternalApplication` and `Test-PmExternalApplication` see the new application at once.

## RELATED LINKS

[Get-PmExternalApplication](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmExternalApplication.md)

[Test-PmExternalApplication](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Test-PmExternalApplication.md)

[Remove-PmExternalApplication](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-PmExternalApplication.md)

[Get-PmExternalApiResource](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmExternalApiResource.md)
