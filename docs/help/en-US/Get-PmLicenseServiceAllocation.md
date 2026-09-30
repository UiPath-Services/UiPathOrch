---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmLicenseServiceAllocation.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 09/30/2026
PlatyPS schema version: 2024-05-01
title: Get-PmLicenseServiceAllocation
---

# Get-PmLicenseServiceAllocation

## SYNOPSIS

Gets a tenant's license allocation grouped by the service that grants each product.

## SYNTAX

### __AllParameterSets

```
Get-PmLicenseServiceAllocation [-Path <string[]>] [-LiteralPath <string[]>] [[-Tenant] <string[]>]
 [[-Code] <string[]>] [-ServiceType <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets one row per product code a tenant holds, naming the service that grants it (`orchestrator`, `dataservice`, `testmanager`, or the `tenant` pseudo-service that grants the consumable units) together with the quantity allocated, how much of it the tenant has used, what is still available in the organization, the organization total, and the entitlement type.

The service is the segment `Set-PmLicenseAllocation` writes to, and the write replaces that service's whole allocation, so this is the view to read before and after a change: it shows exactly which codes travel together in one PUT.

Product codes allocated 0 are omitted by the API, so this cmdlet reports only what the tenant actually holds. For the full code list, including the codes at 0, use `Get-PmLicenseProductAllocation`.

The -Tenant, -Code and -ServiceType parameters all support wildcards; omitting -Tenant reports every tenant in the organization.

Primary Endpoint: GET /api/manageLicense/api/account/{accountId}/service-licenses/{tenantId}?services={services}

OAuth required scopes: (Portal Licensing API - no per-endpoint scopes)

Required permissions: Organization administrator.

This cmdlet works on UiPath Automation Cloud and on Automation Suite (verified on 24.10.11, where the services group the same way — robot runtimes through `orchestrator`, consumables through `tenant`); a standalone Orchestrator does not expose the Portal Management API and answers with an HTML page, which the cmdlet reports as "This operation is not available on this Orchestrator".

## EXAMPLES

### Example 1: What does this tenant hold, and through which service?

```powershell
PS Orch1:\> Get-PmLicenseServiceAllocation DefaultTenant
```

```output
Tenant        Service      Code     Quantity Used Available TotalUnits Type
------        -------      ----     -------- ---- --------- ---------- ----
DefaultTenant dataservice  DSU          2.00 0.00      0.00       2.00 STANDARD
DefaultTenant orchestrator APPTESTR     1.00 0.00      0.00       1.00 STANDARD
DefaultTenant tenant       PLTU      3000.00 0.00   6934.00    9934.00 CONSUMPTION_INTERVAL
```

### Example 2: See which codes a write would travel with

```powershell
PS Orch1:\> Get-PmLicenseServiceAllocation DefaultTenant -ServiceType tenant
```

Lists the consumable units granted by the `tenant` service — the codes one `Set-PmLicenseAllocation -PlatformUnits ...` call re-sends together.

### Example 3: Follow one product across tenants

```powershell
PS Orch1:\> Get-PmLicenseServiceAllocation -Code PLTU
```

Reports each tenant's Platform Unit allocation, what it has used, and what is left in the organization pool.

### Example 4: All fields

```powershell
PS Orch1:\> Get-PmLicenseServiceAllocation DefaultTenant -Code APPTESTR | Format-List *
```

Adds the fields the table leaves out: `reserved`, `allocated`, `allocatedAcrossOtherTenants`, `consumedByDeletedTenants`, `consumedAtOrganizationLevel`, `unlimited`, and the Unix-epoch `startDate` / `endDate`.

## PARAMETERS

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

### -Tenant

Specifies the tenant display names to report on (for example `DefaultTenant`). Supports wildcards and multiple comma-separated values; omitting it returns every tenant in the organization. Tab completion suggests tenant names and shows the tenant's App Test robot, unattended robot, and Platform Unit counts as tooltips.

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

### -Code

Specifies the product codes to report on (for example `APPTESTR`, `PLTU`). Supports wildcards and multiple comma-separated values.

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

### -ServiceType

Specifies the services to report on (for example `orchestrator`, `tenant`). Supports wildcards and multiple comma-separated values; omitting it returns every service that grants the tenant something.

```yaml
Type: System.String[]
DefaultValue: ''
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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe tenant names to this cmdlet via the -Tenant parameter by property name.

## OUTPUTS

### UiPath.PowerShell.Entities.ServiceLicenseProduct

Returns one object per product code with `code`, `type`, `unlimited`, `quantity`, `allocated`, `reserved`, `available`, `used`, `allocatedAcrossOtherTenants`, `consumedByDeletedTenants`, `totalUnits`, `consumedAtOrganizationLevel`, `startDate`, `endDate`, plus the drive-local `Path`, `Tenant` and `serviceType` properties added by UiPathOrch.

## NOTES

Results are cached per organization and tenant, like the other license read cmdlets. `Set-PmLicenseAllocation` reads the same cache to build its merge and invalidates it after a write, so the next call reports the new state.

## RELATED LINKS

[Set-PmLicenseAllocation](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Set-PmLicenseAllocation.md)

[Get-PmLicenseProductAllocation](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmLicenseProductAllocation.md)

[Get-PmLicenseAllocation](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmLicenseAllocation.md)

[Get-PmLicenseInventory](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmLicenseInventory.md)
