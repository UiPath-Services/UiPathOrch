---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmLicenseProductAllocation.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 09/30/2026
PlatyPS schema version: 2024-05-01
title: Get-PmLicenseProductAllocation
---

# Get-PmLicenseProductAllocation

## SYNOPSIS

Gets the per-product license allocation of a tenant in a UiPath Automation Cloud organization.

## SYNTAX

### __AllParameterSets

```
Get-PmLicenseProductAllocation [-Path <string[]>] [-LiteralPath <string[]>] [[-Tenant] <string[]>]
 [[-Code] <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets one row per product code allocated to a tenant, reporting the organization total, the quantity allocated to that tenant, whether the product is a consumable, whether it is unlimited, and the validity interval of consumable entitlements.

This cmdlet is the product-level counterpart of `Get-PmLicenseAllocation`: where that cmdlet returns one wide row per tenant with fixed numeric fields, this one returns the tenant's allocation broken down by product code (`APPTESTR`, `PLTU`, `SPR`, `DSU`, ...), including the codes currently allocated 0. It covers both the Robots & Services and the Consumables panels of the Admin / Licenses page.

The -Tenant parameter filters by tenant display name and supports wildcards and tab completion; omitting it returns every tenant in the organization. The -Code parameter filters by product code and supports wildcards.

The default table view shows the tenant, product code, allocated and total quantities, what is still available, whether the product is a consumable, and a usage bar; rows are grouped by drive.

Results are cached per organization and tenant. `Set-PmLicenseAllocation` invalidates that cache after a write.

Primary Endpoint: GET /api/licensing/tenantProductAllocation?accountGlobalId={org}&tenantGlobalId={tenant}

OAuth required scopes: (Portal Licensing API - no per-endpoint scopes)

Required permissions: Organization administrator.

This cmdlet works on UiPath Automation Cloud and on Automation Suite (verified on 24.10.11, where `unlimited` comes back empty); a standalone Orchestrator does not expose the Portal Management API and answers with an HTML page, which the cmdlet reports as "This operation is not available on this Orchestrator".

## EXAMPLES

### Example 1: Get the product allocation of one tenant

```powershell
PS Orch1:\> Get-PmLicenseProductAllocation DefaultTenant
```

Gets every product code allocated to the tenant named `DefaultTenant`. Because -Tenant is positional (position 0), the parameter name can be omitted.

### Example 2: Show only the products that are actually allocated

```powershell
PS Orch1:\> Get-PmLicenseProductAllocation DefaultTenant | Where-Object allocated -gt 0
```

Filters out the product codes the tenant holds none of.

### Example 3: Check one product across every tenant

```powershell
PS Orch1:\> Get-PmLicenseProductAllocation -Code APPTESTR | Format-Table Tenant, code, allocated, total
```

Reports how many App Test Robot runtimes each tenant of the organization holds.

### Example 4: Consumable units only

```powershell
PS Orch1:\> Get-PmLicenseProductAllocation DefaultTenant -Code 'P*' | Format-List *
```

Expands every field, including `unlimited` and the Unix-epoch `startDate` / `endDate` of consumable entitlements.

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

Specifies the product codes to report on (for example `APPTESTR`, `PLTU`). Supports wildcards and multiple comma-separated values; omitting it returns every code the organization's license knows.

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe tenant names to this cmdlet via the -Tenant parameter by property name.

## OUTPUTS

### UiPath.PowerShell.Entities.TenantProductAllocation

Returns one object per product code with `code`, `total`, `allocated`, `isConsumable`, `unlimited`, `startDate`, `endDate`, plus the drive-local `Path` and `Tenant` properties added by UiPathOrch. The type derives from `ProductAllocation` (the row type of `Get-PmLicenseInventory`) and adds the tenant scope, so it has a table view of its own.

## NOTES

`unlimited` is returned only by this endpoint. Rows coming from `Get-PmLicenseInventory` (the organization-level inventory) leave it null.

To change an allocation, use `Set-PmLicenseAllocation`. To see which service grants each code — the service a write goes to — use `Get-PmLicenseServiceAllocation`.

## RELATED LINKS

[Set-PmLicenseAllocation](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Set-PmLicenseAllocation.md)

[Get-PmLicenseAllocation](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmLicenseAllocation.md)

[Get-PmLicenseInventory](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmLicenseInventory.md)

[Get-PmLicenseContract](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmLicenseContract.md)
