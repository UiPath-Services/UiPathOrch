---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmLicenseAllocation.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 04/22/2026
PlatyPS schema version: 2024-05-01
title: Get-PmLicenseAllocation
---

# Get-PmLicenseAllocation

## SYNOPSIS

Gets per-tenant license allocations for a UiPath Automation Cloud organization (Robots & Services tab).

## SYNTAX

### __AllParameterSets

```
Get-PmLicenseAllocation [[-Tenant] <string[]>] [-Path <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets the per-tenant robot and runtime license allocations for a UiPath Automation Cloud organization. Each returned row describes one tenant within the organization and reports how many unattended robots, non-production robots, testing robots, App Test robots, AI robots, GPU slots, and consumable units (Data Service, Robot Units, AI Units, Agent Units, Platform Units, Performance Testing, Heals, Test Heals, Medical Record Summarization Units, Lending Units) are allocated to that tenant.

This cmdlet corresponds to the "Robots & Services" tab of the Admin / Licenses page in Automation Cloud. The default view is a compact table (Tenant, Unatt, NonPr, Test, AppTest, DSU, Region, Status); the full tenant metadata, service catalog, and less common allocation counters are exposed on each object for deeper inspection.

The -Tenant parameter filters by the tenant display name (for example `svc1`). It supports wildcards and tab completion.

Results are cached per organization. Multiple drives that belong to the same organization share a single API call.

Primary Endpoint: GET /api/licensing/tenantAllocations?accountGlobalId={org}

OAuth required scopes: (Portal Licensing API - no per-endpoint scopes)

Required permissions: Organization administrator.

This cmdlet works only on UiPath Automation Cloud deployments; on-premises Orchestrator does not expose the Portal Management API.

## EXAMPLES

### Example 1: Get allocations for every tenant in the current organization

```powershell
PS Orch1:\> Get-PmLicenseAllocation
```

Gets per-tenant license allocations for the organization behind the current drive.

### Example 2: Filter by tenant name

```powershell
PS Orch1:\> Get-PmLicenseAllocation svc1
```

Gets the allocation row for the tenant named `svc1`. Because -Tenant is a positional parameter (position 0), the parameter name can be omitted.

### Example 3: Wildcard match

```powershell
PS Orch1:\> Get-PmLicenseAllocation 'svc*'
```

Gets the allocation rows for every tenant whose name starts with `svc`.

### Example 4: Inspect all details for a tenant

```powershell
PS Orch1:\> Get-PmLicenseAllocation svc1 | Format-List *
```

Expands every field on the tenant allocation, including the nested `tenant` metadata and the full `services` list.

### Example 5: Query multiple organizations

```powershell
PS C:\> Get-OrchPSDrive |
            Where-Object Root -like 'https://cloud*' |
            Group-Object { ([Uri]$_.Root).Segments[1].TrimEnd('/') } |
            ForEach-Object { Get-PmLicenseAllocation -Path "$($_.Group[0].Name):" }
```

Because this cmdlet is organization-scoped, the snippet groups drives by their organization slug and issues exactly one API call per organization.

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

### -Tenant

Specifies the tenant display names to filter by (for example `svc1`). Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests tenant names in the target organization and shows per-tenant Unatt/NonPr/Test counts as tooltips.

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

You can pipe tenant names to this cmdlet via the -Tenant parameter by property name.

## OUTPUTS

### UiPath.PowerShell.Entities.TenantAllocation

Returns one object per tenant with a nested `tenant` metadata object, a `services` string array, and 17 numeric allocation fields covering robots, GPUs, and consumable units.

## NOTES

This cmdlet reports allocated quantities only. For the organization-level totals (Total vs Allocated across all tenants), use `Get-PmLicenseInventory`.

Because the Portal Licensing API returns the same data for every tenant that shares an organization, querying multiple drives within the same organization is redundant. Group by the organization slug in the drive URL to avoid duplicate API calls.

## RELATED LINKS

[Get-PmLicenseInventory](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmLicenseInventory.md)

[Get-PmLicenseContract](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmLicenseContract.md)

[Get-PmLicense](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmLicense.md)

[Get-OrchLicense](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchLicense.md)
