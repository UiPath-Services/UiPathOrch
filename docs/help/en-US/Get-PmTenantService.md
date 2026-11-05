---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmTenantService.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 11/05/2026
PlatyPS schema version: 2024-05-01
title: Get-PmTenantService
---

# Get-PmTenantService

## SYNOPSIS

Gets the services of each tenant (Orchestrator, Test Manager, Data Service, ...) with their status and URL.

## SYNTAX

### __AllParameterSets

```
Get-PmTenantService [-Path <string[]>] [-LiteralPath <string[]>] [[-Tenant] <string[]>]
 [[-ServiceType] <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets the services of the tenants of the organization that the drive belongs to, one object per tenant and service: the service type (orchestrator, testmanager, dataservice, actions and so on), its status, its region, and its URL by organization and tenant name (friendlyUrl).

The -Tenant parameter filters by tenant name, and -ServiceType by service type. Both support wildcards; -Tenant also supports tab completion. Without them, every service of every tenant is returned.

The services of the organization as a whole (portal, identity, docs and so on) are not tenant services; Get-PmOrganization lists them in its organizationServiceInstances property.

Results are cached per organization and tenant.

Primary Endpoint: GET /organization_/api/organization/{organizationId}/tenants/{tenantId}

Tenant List Endpoint: GET /organization_/api/organization/{organizationId}/tenants

Required permissions: Organization administrator.

This cmdlet works on UiPath Automation Cloud and on Automation Suite. A standalone Orchestrator has no Organization Management Service, and the cmdlet reports that instead.

## EXAMPLES

### Example 1: List the services of every tenant

```powershell
PS Orch1:\> Get-PmTenantService
```

### Example 2: Which tenants have Test Manager?

```powershell
PS Orch1:\> Get-PmTenantService -ServiceType testmanager | Format-Table Tenant, status, friendlyUrl
```

### Example 3: The services of one tenant

```powershell
PS Orch1:\> Get-PmTenantService DefaultTenant orch*, test*
```

Gets the Orchestrator and Test Manager services of the tenant DefaultTenant. -Tenant and -ServiceType are positional (positions 0 and 1).

## PARAMETERS

### -Tenant

Specifies the tenant names whose services to get. Wildcards are permitted. Tab completion suggests the tenant names of the target organization.

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

### -ServiceType

Specifies the service types to get, such as `orchestrator`, `testmanager` or `dataservice`. Wildcards are permitted.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: true
Aliases: []
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

You can pipe tenant names and service types to this cmdlet by property name.

## OUTPUTS

### UiPath.PowerShell.Entities.PmService

One object per tenant and service, with Path, Tenant, tenantId, serviceType, status, region, isCrossRegionService, friendlyUrl, url (by GUIDs), id and isVisible.

## NOTES

Get-PmLicenseAllocation also lists each tenant's service types (its services property), as part of the licensing data, without their status or URL.

## RELATED LINKS

[Get-PmTenant](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmTenant.md)

[Get-PmOrganization](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmOrganization.md)
