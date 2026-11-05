---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmTenant.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 11/05/2026
PlatyPS schema version: 2024-05-01
title: Get-PmTenant
---

# Get-PmTenant

## SYNOPSIS

Gets the tenants of the organization, with their status, region and environment.

## SYNTAX

### __AllParameterSets

```
Get-PmTenant [-Path <string[]>] [-LiteralPath <string[]>] [[-Name] <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets every tenant of the organization that the drive belongs to, including tenants that no drive is mounted on. Each tenant comes with its status (Enabled or Disabled), region, environment (Production and so on), creation and update dates, and its id (the GUID other organization APIs take).

The -Name parameter filters by tenant name. It supports wildcards and tab completion.

Results are cached per organization, so several drives of one organization share one API call.

Primary Endpoint: GET /organization_/api/organization/{organizationId}/tenants

Required permissions: Organization administrator.

This cmdlet works on UiPath Automation Cloud and on Automation Suite. A standalone Orchestrator has no Organization Management Service, and the cmdlet reports that instead.

## EXAMPLES

### Example 1: List the tenants of the current organization

```powershell
PS Orch1:\> Get-PmTenant
```

### Example 2: Find the disabled tenants

```powershell
PS Orch1:\> Get-PmTenant | Where-Object status -ne 'Enabled'
```

## PARAMETERS

### -Name

Specifies the tenant names to get. Wildcards are permitted. Tab completion suggests the tenant names of the target organization.

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

### -Path

Specifies the target drives. If not specified, the current drive is targeted. Tab completion suggests available drive names (for example `Orch1:`).

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe tenant names to this cmdlet by property name.

## OUTPUTS

### UiPath.PowerShell.Entities.PmTenant

One object per tenant, with name, id, status, region, environment, isCanaryTenant, createdOn, updatedOn and the ids of who created and last updated it.

## NOTES

The services of each tenant are listed by Get-PmTenantService.

## RELATED LINKS

[Get-PmOrganization](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmOrganization.md)

[Get-PmTenantService](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmTenantService.md)

[Get-PmLicenseAllocation](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmLicenseAllocation.md)
