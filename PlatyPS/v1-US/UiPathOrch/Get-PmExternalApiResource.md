---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-PmExternalApiResource
---

# Get-PmExternalApiResource

## SYNOPSIS

Gets external API resources from UiPath Automation Cloud organizations.

## SYNTAX

### __AllParameterSets

```
Get-PmExternalApiResource [[-Name] <string[]>] [-Path <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets external API resource information from UiPath Automation Cloud at the organization (platform management) level. External API resources define the available scopes and permissions that can be assigned to external applications (OAuth clients).

This cmdlet retrieves the registered API resources from the identity service and returns ExternalResource objects containing properties such as name, display name, and available scopes.

The -Name parameter supports wildcards and tab completion. Press [Ctrl+Space] or [Tab] to see available API resource names.

When multiple Pm: drives are connected, specifying -Path targets specific organizations. If -Path is omitted, the current drive is targeted.

Primary Endpoint: GET /api/ExternalApiResource (Identity Server)

OAuth required scopes: (Identity Server API - no per-endpoint scopes)

Required permissions: (managed by Identity Server)

## EXAMPLES

### Example 1: Get all external API resources

```powershell
PS Orch1:\> Get-PmExternalApiResource
```

Gets all external API resources from the current organization.

### Example 2: Get a specific API resource by name

```powershell
PS Orch1:\> Get-PmExternalApiResource UiPath.Orchestrator
```

Gets the API resource with the specified name. Because -Name is a positional parameter (position 0), the parameter name can be omitted.

### Example 3: Get API resources matching a wildcard pattern

```powershell
PS Orch1:\> Get-PmExternalApiResource *Api*
```

Gets all API resources whose names contain "Api".

### Example 4: Get API resources from a specific organization

```powershell
PS C:\> Get-PmExternalApiResource -Path Orch1:
```

Gets all external API resources from the Orch1 organization drive.

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

Specifies the names of external API resources to retrieve. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests API resource names from the target organizations.

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

You can pipe API resource names to this cmdlet via the Name property.

## OUTPUTS

### UiPath.PowerShell.Entities.ExternalResource

Returns ExternalResource objects with properties including name, display name, and available scopes.

## NOTES

External API resources define the permission scopes available to external applications. Use Get-PmExternalApplication to see which scopes are assigned to specific applications.

## RELATED LINKS

Get-PmExternalApplication

Copy-PmExternalApplication
