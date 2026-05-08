---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmLicenseContract.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 04/22/2026
PlatyPS schema version: 2024-05-01
title: Get-PmLicenseContract
---

# Get-PmLicenseContract

## SYNOPSIS

Gets the full account-level license contract for a UiPath Automation Cloud organization (subscription, products, templates, entitlements, ML keys).

## SYNTAX

### __AllParameterSets

```
Get-PmLicenseContract [[-Path] <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets the complete license contract document for a UiPath Automation Cloud organization. The returned object is the raw subscription record that underpins what is shown in the Admin / Licenses page and includes:

- Contract identity: `accountId`, `bundleCode`, `licenseCode`, `subscriptionCode`, `subscriptionPlan`, `accountType`.
- Status: `licenseStatus`, `gracePeriod`, `needsLicenseReactivation`, `enableMigration`, `migrationStartedAt`, `migrationDeadlineDays`.
- Validity window: `startDate`, `endDate` (both Unix epoch seconds).
- Purchased products: `products[]` — one entry per SKU with `code`, `quantity`, `type`, feature flags, and consumption distribution intervals.
- Bundle templates: `templates[]` — which child product codes are entailed by each top-level user bundle (for example ATTUNU → EE, DU, APPS, IXP, ATTR).
- Entitlements: `entitlements[]` — rule / ubl / flag definitions that govern which licenses can be consumed and how.
- `mlKeys[]` — ML service keys bundled with the subscription.
- `payload` — the original embedded JSON license payload, preserved verbatim.

The default view shows the contract identity and item counts for the nested collections. Access individual collections via the object properties (for example `(Get-PmLicenseContract).entitlements`).

Results are cached per organization. Multiple drives that belong to the same organization share a single API call.

Primary Endpoint: GET /api/license/management/account?accountGlobalId={org}&accountUserId={sub}

OAuth required scopes: (Portal License Management API - no per-endpoint scopes)

Required permissions: Organization administrator.

This cmdlet works only on UiPath Automation Cloud deployments; on-premises Orchestrator does not expose the Portal Management API.

## EXAMPLES

### Example 1: Get the contract

```powershell
PS Orch1:\> Get-PmLicenseContract
```

Gets the license contract for the organization behind the current drive. The default view summarises key identifiers and the sizes of the nested collections.

### Example 2: Inspect every purchased product

```powershell
PS Orch1:\> (Get-PmLicenseContract).products | Where-Object quantity -gt 0
```

Lists every SKU with a non-zero purchased quantity.

### Example 3: Show bundle templates

```powershell
PS Orch1:\> (Get-PmLicenseContract).templates |
                ForEach-Object { "$($_.productCode) -> $(($_.products | ForEach-Object code) -join ', ')" }
```

Prints each bundle template and the child product codes it entails (for example `ATTUNU -> EE, DU, APPS, IXP, ATTR`).

### Example 4: Enumerate entitlements

```powershell
PS Orch1:\> (Get-PmLicenseContract).entitlements |
                Select-Object name, type, startsOn, endsOn
```

Lists every entitlement along with its type and validity window.

### Example 5: Export the embedded payload

```powershell
PS Orch1:\> (Get-PmLicenseContract).payload | Set-Content license-payload.json
```

Saves the full embedded license payload to a file for offline inspection.

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

### UiPath.PowerShell.Entities.AccountLicense

Returns one contract object per organization. Sub-properties: `products`, `templates`, `entitlements`, `entitlementOverrides`, `mlKeys`, `payload`, plus scalar identity fields (`bundleCode`, `licenseCode`, `subscriptionPlan`, etc.).

## NOTES

For an operational summary (how many seats are allocated to tenants right now), use `Get-PmLicenseInventory`. For the per-tenant breakdown, use `Get-PmLicenseAllocation`.

The `payload` property contains an embedded JSON string that mirrors much of the top-level structure. It is preserved verbatim so that callers can sign, archive, or forward the raw document without re-serialising.

Because the Portal License Management API returns the same data for every tenant that shares an organization, querying multiple drives within the same organization is redundant. Group by the organization slug in the drive URL to avoid duplicate API calls.

## RELATED LINKS

[Get-PmLicenseInventory](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmLicenseInventory.md)

[Get-PmLicenseAllocation](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmLicenseAllocation.md)

[Get-PmLicense](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmLicense.md)

[Get-OrchLicense](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchLicense.md)
