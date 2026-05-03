---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmLicenseInventory.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 04/22/2026
PlatyPS schema version: 2024-05-01
title: Get-PmLicenseInventory
---

# Get-PmLicenseInventory

## SYNOPSIS

Gets the organization-level license inventory dashboard (Robots & Services tab summary).

## SYNTAX

### __AllParameterSets

```
Get-PmLicenseInventory [[-Path] <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets the complete organization-level license inventory for a UiPath Automation Cloud organization. The returned object is the dashboard shown at the top of the Admin / Licenses page (Robots & Services tab) and bundles five collections into one response:

- `productAllocations` — one row per SKU (for example UNATT, NONPR, ATTUNU) with Total and Allocated counts. Equivalent to "how many seats were purchased versus how many have been distributed to tenants so far."
- `userLicensingBundles` — user bundle usage rows (same shape as `Get-PmLicense`).
- `entitlementUsages` — consumption pool status (for example API calls) with current consumed and total quantities per refresh interval.
- `availableServices` — the catalog of services exposed by the organization, with provisioning mode and supported regions.
- `mlKeys` — ML service keys bundled with the subscription.

The default view shows item counts for each section. Access individual sections via the object properties (for example `(Get-PmLicenseInventory).productAllocations`).

Results are cached per organization. Multiple drives that belong to the same organization share a single API call.

Primary Endpoint: GET /api/license/management/account/available?accountGlobalId={org}&accountUserId={sub}

OAuth required scopes: (Portal License Management API - no per-endpoint scopes)

Required permissions: Organization administrator.

This cmdlet works only on UiPath Automation Cloud deployments; standalone on-premises Orchestrator does not expose the Portal Management API.

## EXAMPLES

### Example 1: Get the inventory dashboard

```powershell
PS Orch1:\> Get-PmLicenseInventory
```

Gets the full license inventory for the organization behind the current drive. The default view summarises the five collections as item counts.

### Example 2: Expand product allocations

```powershell
PS Orch1:\> (Get-PmLicenseInventory).productAllocations | Where-Object total -gt 0
```

Lists only the SKUs with a non-zero total, using the `ProductAllocation` default view (Code, Allocated, Total, Available, Usage bar).

### Example 3: Inspect entitlement pool usage

```powershell
PS Orch1:\> (Get-PmLicenseInventory).entitlementUsages |
                ForEach-Object { $_.poolSummary } |
                Select-Object poolName, consumedQuantity, totalQuantity, refreshType, hasNextAvailable
```

Lists each consumption pool, its current consumed versus total quantities, and the next refresh time.

### Example 4: Inspect the service catalog

```powershell
PS Orch1:\> (Get-PmLicenseInventory).availableServices |
                Select-Object id, serviceLicenseStatus, provisioningMode, defaultRegion
```

Summarises every service available to the organization.

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

You can pipe drive names to the -Path parameter by property name.

## OUTPUTS

### UiPath.PowerShell.Entities.LicenseInventory

Returns one dashboard object per organization. Sub-properties: `productAllocations`, `userLicensingBundles`, `entitlementUsages`, `availableServices`, `mlKeys`.

## NOTES

For the per-tenant breakdown of how these totals are distributed across tenants, use `Get-PmLicenseAllocation`. For the full contract (purchase details, entitlements definitions, templates), use `Get-PmLicenseContract`.

Because the Portal License Management API returns the same data for every tenant that shares an organization, querying multiple drives within the same organization is redundant. Group by the organization slug in the drive URL to avoid duplicate API calls.

## RELATED LINKS

[Get-PmLicenseAllocation](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmLicenseAllocation.md)

[Get-PmLicenseContract](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmLicenseContract.md)

[Get-PmLicense](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmLicense.md)

[Get-OrchLicense](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchLicense.md)
