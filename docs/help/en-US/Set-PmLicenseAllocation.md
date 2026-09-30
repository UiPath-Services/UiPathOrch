---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Set-PmLicenseAllocation.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 09/30/2026
PlatyPS schema version: 2024-05-01
title: Set-PmLicenseAllocation
---

# Set-PmLicenseAllocation

## SYNOPSIS

Sets the license allocation of a tenant in a UiPath Automation Cloud organization.

## SYNTAX

### __AllParameterSets

```
Set-PmLicenseAllocation [-Path <string[]>] [-LiteralPath <string[]>] [-Tenant] <string>
 [-AppTestRobot <int>] [-Confirm] [-DataServiceUnit <int>] [-NonProductionRobot <int>]
 [-PerformanceTesting <int>] [-PlatformUnits <double>] [-Products <hashtable>]
 [-ScreenPlayRuns <double>] [-ServiceType <string>] [-TestHeals <double>]
 [-TestingRobot <int>] [-UnattendedHostingRobot <int>] [-UnattendedRobot <int>] [-WhatIf]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Allocates robot runtimes and consumable units to one tenant, the operation the Admin / Licenses page performs through its Robots & Services and Consumables panels.

The underlying API replaces a service's whole allocation: a product code missing from a non-empty `products` array is set to 0. This cmdlet therefore reads the tenant's current service licenses first and re-sends them with the requested codes overlaid, so allocations you did not mention keep their quantity. Only the services whose codes you actually change are written.

Each named parameter maps to one product code: -UnattendedRobot to `UNATT`, -UnattendedHostingRobot to `UNATT-HOSTING`, -NonProductionRobot to `NONPR`, -TestingRobot to `TAUNATT`, -AppTestRobot to `APPTESTR`, -PerformanceTesting to `PERFTEST`, -DataServiceUnit to `DSU`, -PlatformUnits to `PLTU`, -ScreenPlayRuns to `SPR`, and -TestHeals to `HEALTEST`. Any other code goes through -Products, which wins over a named parameter for the same code.

A product code is routed to the service that grants it: the service it is allocated through today, or the module's grouping of the known codes (robot runtimes through `orchestrator`, `DSU` through `dataservice`, consumable units through the `tenant` pseudo-service). A code the cmdlet cannot place is reported as an error naming -ServiceType rather than being sent to an arbitrary service.

Setting a quantity higher than the organization holds, or lower than the tenant already consumes, is refused by the API and surfaced as an error.

Primary Endpoint: PUT /api/manageLicense/api/account/{accountId}/service-license/{tenantId}/{serviceType}

OAuth required scopes: (Portal Licensing API - no per-endpoint scopes)

Required permissions: Organization administrator.

This cmdlet works on UiPath Automation Cloud and on Automation Suite (verified on 24.10.11); a standalone Orchestrator does not expose the Portal Management API and answers with an HTML page, which the cmdlet reports as "This operation is not available on this Orchestrator".

## EXAMPLES

### Example 1: Allocate an App Test Robot runtime to a tenant

```powershell
PS Orch1:\> Set-PmLicenseAllocation DefaultTenant -AppTestRobot 1
```

Gives the tenant one App Test Robot runtime, which is what Autonomous Test Execution runs on. Because -Tenant is positional (position 0), the parameter name can be omitted.

### Example 2: Allocate consumable units

```powershell
PS Orch1:\> Set-PmLicenseAllocation DefaultTenant -PlatformUnits 3000 -ScreenPlayRuns 50000 -TestHeals 2000
```

Allocates Platform Units, ScreenPlay runs, and Test Heals in a single call. All three are granted by the `tenant` service, so this writes once.

### Example 3: Release an allocation

```powershell
PS Orch1:\> Set-PmLicenseAllocation DefaultTenant -UnattendedRobot 0
```

Returns the tenant's unattended robot runtime to the organization pool. Other allocations of the `orchestrator` service are preserved.

### Example 4: Set a product code that has no named parameter

```powershell
PS Orch1:\> Set-PmLicenseAllocation DefaultTenant -Products @{ TEU = 1000; ACR = 1 }
```

Sets Test Execution Units and Automation Cloud Robots by product code.

### Example 5: Preview the change

```powershell
PS Orch1:\> Set-PmLicenseAllocation DefaultTenant -AppTestRobot 1 -WhatIf
```

```output
What if: Performing the operation "Set PmLicenseAllocation" on target "Orch1:DefaultTenant [orchestrator]".
```

Shows which services would be written without sending anything.

### Example 6: Verify afterwards

```powershell
PS Orch1:\> Set-PmLicenseAllocation DefaultTenant -AppTestRobot 1 |
                Format-Table Tenant, code, allocated, total
```

The cmdlet re-reads the tenant's allocation after the write and emits the rows for the codes it changed.

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

Specifies the tenant to allocate to, by display name (for example `DefaultTenant`). This is a mandatory parameter and takes exactly one tenant: wildcards are not accepted, so a write cannot fan out across tenants by accident. Tab completion suggests tenant names in the target organization and shows the tenant's App Test robot, unattended robot, and Platform Unit counts as tooltips.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 0
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -AppTestRobot

Specifies the number of App Test Robot runtimes (`APPTESTR`) to allocate to the tenant. This is the runtime Autonomous Test Execution and other Test Cloud application testing runs on.

```yaml
Type: System.Int32
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

### -Confirm

Prompts you for confirmation before running the cmdlet.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases:
- cf
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -DataServiceUnit

Specifies the number of Data Service units (`DSU`) to allocate to the tenant. Granted by the `dataservice` service.

```yaml
Type: System.Int32
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

### -NonProductionRobot

Specifies the number of NonProduction robot runtimes (`NONPR`) to allocate to the tenant.

```yaml
Type: System.Int32
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

### -PerformanceTesting

Specifies the number of Performance Testing licenses (`PERFTEST`) to allocate to the tenant.

```yaml
Type: System.Int32
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

### -PlatformUnits

Specifies the number of Platform Units (`PLTU`) to allocate to the tenant. Platform Units are the single consumable of the Unified Pricing plan, so agentic and Autonomous Testing consumption draws on this allocation.

```yaml
Type: System.Double
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

### -Products

Specifies product codes the named parameters do not cover, as a hashtable of code to quantity (for example `@{ TEU = 1000 }`). An entry here overrides the named parameter for the same code.

```yaml
Type: System.Collections.Hashtable
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

### -ScreenPlayRuns

Specifies the number of ScreenPlay runs (`SPR`) to allocate to the tenant. Autonomous Test Execution consumes ScreenPlay when it drives an application.

```yaml
Type: System.Double
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

### -ServiceType

Forces every requested product code through this service, instead of letting the cmdlet route each code itself. Tab completion suggests `orchestrator`, `tenant`, `dataservice`, and `testmanager`; the list is not closed. Use it only for a code the cmdlet reports it cannot place.

```yaml
Type: System.String
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

### -TestHeals

Specifies the number of Test Heals (`HEALTEST`) to allocate to the tenant, the consumable of the healing agent for testing.

```yaml
Type: System.Double
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

### -TestingRobot

Specifies the number of Testing robot runtimes (`TAUNATT`) to allocate to the tenant. This is the Automation Cloud testing runtime, not the Test Cloud App Test Robot.

```yaml
Type: System.Int32
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

### -UnattendedHostingRobot

Specifies the number of Unattended Hosting robot runtimes (`UNATT-HOSTING`) to allocate to the tenant.

```yaml
Type: System.Int32
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

### -UnattendedRobot

Specifies the number of Unattended robot runtimes (`UNATT`) to allocate to the tenant.

```yaml
Type: System.Int32
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

### -WhatIf

Shows what would happen if the cmdlet runs. The cmdlet is not run.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases:
- wi
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
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

### System.String

You can pipe a tenant name to this cmdlet via the -Tenant parameter by property name.

## OUTPUTS

### UiPath.PowerShell.Entities.TenantProductAllocation

Returns the tenant's refreshed allocation rows for the product codes that were changed, read back after the write.

## NOTES

Allocating a runtime to a tenant does not yet make it usable: the runtime also has to be assigned to a machine (`Update-OrchMachine -AppTestSlots 1` for App Testing) and that machine has to be assigned to a folder.

The write invalidates the cached per-tenant allocations, the organization inventory, and the per-tenant product allocations, so a following `Get-PmLicenseAllocation` / `Get-PmLicenseProductAllocation` reports the new state. Cached Orchestrator-side license data (`Get-OrchLicense`) is a separate cache and may still show the previous allocation until it is cleared.

## RELATED LINKS

[Get-PmLicenseProductAllocation](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmLicenseProductAllocation.md)

[Get-PmLicenseAllocation](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmLicenseAllocation.md)

[Get-PmLicenseInventory](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmLicenseInventory.md)

[Update-OrchMachine](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Update-OrchMachine.md)
