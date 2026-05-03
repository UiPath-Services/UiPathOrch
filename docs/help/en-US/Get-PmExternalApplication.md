---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmExternalApplication.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-PmExternalApplication
---

# Get-PmExternalApplication

## SYNOPSIS

Gets external applications from UiPath Automation Cloud organizations.

## SYNTAX

### __AllParameterSets

```
Get-PmExternalApplication [[-Name] <string[]>] [-Path <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets external application (OAuth client) information from UiPath Automation Cloud at the organization (platform management) level. External applications are OAuth2 clients registered in the organization for third-party integrations and API access.

This cmdlet retrieves external applications from the identity service API and returns ExternalClient objects containing properties such as name, application type (confidential or non-confidential), scopes, and redirect URIs.

The -Name parameter supports wildcards and tab completion. Press [Ctrl+Space] or [Tab] to see available external application names.

When multiple Pm: drives are connected, specifying -Path targets specific organizations. If -Path is omitted, the current drive is targeted.

Primary Endpoint: GET /api/ExternalClient/{partitionGlobalId} (Identity Server)

OAuth required scopes: (Identity Server API - no per-endpoint scopes)

Required permissions: (managed by Identity Server)

## EXAMPLES

### Example 1: Get all external applications

```powershell
PS Orch1:\> Get-PmExternalApplication
```

Gets all external applications from the current organization.

### Example 2: Get a specific external application by name

```powershell
PS Orch1:\> Get-PmExternalApplication uipathorch
```

Gets the external application with the specified name. Because -Name is a positional parameter (position 0), the parameter name can be omitted.

### Example 3: Get external applications matching a wildcard pattern

```powershell
PS Orch1:\> Get-PmExternalApplication ほえほえ*
```

Gets all external applications whose names start with "ほえほえ".

### Example 4: Get external applications from a specific organization

```powershell
PS C:\> Get-PmExternalApplication -Path Orch1: uipathorch
```

Gets the specified external application from the Orch1 organization drive.

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

Specifies the names of external applications to retrieve. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests external application names from the target organizations.

```yaml
Type: System.String[]
DefaultValue: ''
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

You can pipe external application names to this cmdlet via the Name property.

## OUTPUTS

### UiPath.PowerShell.Entities.ExternalClient

Returns ExternalClient objects with properties including name, application type, scopes, and redirect URIs.

## NOTES

External applications are organization-level OAuth2 clients. They can be either confidential (server-side) or non-confidential (public) application types.

## RELATED LINKS

Copy-PmExternalApplication

Remove-PmExternalApplication

Get-PmExternalApiResource
