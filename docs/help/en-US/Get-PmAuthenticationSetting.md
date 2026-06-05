---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmAuthenticationSetting.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-PmAuthenticationSetting
---

# Get-PmAuthenticationSetting

## SYNOPSIS

Gets authentication settings from UiPath Automation Cloud organizations.

## SYNTAX

### __AllParameterSets

```
Get-PmAuthenticationSetting [[-Path] <string[]>] [-LiteralPath <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets the authentication configuration settings from UiPath Automation Cloud at the organization (platform management) level. This cmdlet retrieves the complete authentication settings tree including SAML, Azure AD, Google, and other identity provider configurations.

The returned PmAuthenticationRoot object contains nested properties for each authentication provider, including whether they are enabled, their configuration parameters, and restriction policies.

When multiple Pm: drives are connected, specifying -Path targets specific organizations. If -Path is omitted, the current drive is targeted.

Primary Endpoint: GET /api/AuthenticationSetting/getAll/{partitionGlobalId} (Identity Server)

OAuth required scopes: (Identity Server API - no per-endpoint scopes)

Required permissions: (managed by Identity Server)

## EXAMPLES

### Example 1: Get authentication settings

```powershell
PS Orch1:\> Get-PmAuthenticationSetting
```

Gets the authentication settings from the current organization.

### Example 2: Get authentication settings from a specific organization

```powershell
PS C:\> Get-PmAuthenticationSetting Orch1:
```

Gets the authentication settings from the Orch1 organization drive. Because -Path is a positional parameter (position 0), the parameter name can be omitted.

### Example 3: Compare authentication settings across organizations

```powershell
PS C:\> Get-PmAuthenticationSetting Orch1:,Orch2:
```

Gets the authentication settings from both the Orch1 and Orch2 organizations for comparison.

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
  Position: 0
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

You can pipe drive paths to this cmdlet via the Path property.

## OUTPUTS

### UiPath.PowerShell.Entities.PmAuthenticationRoot

Returns PmAuthenticationRoot objects containing the complete authentication configuration tree, including settings for SAML, Azure AD, Google, and other identity providers.

## NOTES

Authentication settings are read-only through this cmdlet. The returned object provides a snapshot of the current authentication configuration for the organization.

The PmAuthenticationRoot object contains nested properties for each supported identity provider, including enablement status, configuration URIs, and restriction policies.

## RELATED LINKS

[Get-PmUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmUser.md)

[Get-PmAuditLog](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmAuditLog.md)
