---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmAvailableService.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 11/05/2026
PlatyPS schema version: 2024-05-01
title: Get-PmAvailableService
---

# Get-PmAvailableService

## SYNOPSIS

Gets the services a tenant can have.

## SYNTAX

### __AllParameterSets

```
Get-PmAvailableService [-Path <string[]>] [-LiteralPath <string[]>]
 [[-ServiceType] <string[]>] [-Region <string>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets the catalog of services a tenant of the organization can have, in a region: which ones Add-PmTenantService can provision (provisioningMode Explicit), and which ones every tenant always keeps (isAlwaysProvision), which Remove- and Disable-PmTenantService refuse.

This cmdlet operates on the PSDrive of the UiPathOrch provider. It calls the Organization Management Service ({host}/{organization}/organization_), the API of `uip admin tenants`, which Automation Cloud and Automation Suite have and a standalone Orchestrator does not.

Primary Endpoint: GET /organization_/api/organization/{organizationId}/availableservices?scope=Tenant

Required permissions: Organization administrator.

## EXAMPLES

### Example 1: The services that can be added or removed

```powershell
PS Orch1:\> Get-PmAvailableService | Where-Object { -not $_.isAlwaysProvision }
```

## PARAMETERS

### -ServiceType

Specifies the service types to get. Wildcards are permitted.

```yaml
Type: System.String[]
DefaultValue: None
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

### -Region

Specifies the region, such as `Europe` or `Japan`. By default the organization's.

```yaml
Type: System.String
DefaultValue: None
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

### -Path

Specifies the target drives. If not specified, the current drive is targeted. Tab completion suggests available drive names (for example `Orch1:`).

```yaml
Type: System.String[]
DefaultValue: None
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

Specifies the target drive by literal path -- wildcard metacharacters (`[`, `]`, `*`, `?`) are treated as literal characters rather than patterns. Accepts the same drive-qualified paths as -Path.

```yaml
Type: System.String[]
DefaultValue: None
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

You can pipe objects with the parameters' names as properties, such as the output of Get-PmTenant.

## OUTPUTS

### UiPath.PowerShell.Entities.PmAvailableService

One object per service: id (the service type), name, provisioningMode, isAlwaysProvision, entitlement, supportedRegions.

## NOTES

The organization-level cmdlets act on the whole organization that the drive belongs to, not only on the drive's tenant.

## RELATED LINKS

[Get-PmTenant](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmTenant.md)

[Get-PmTenantService](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmTenantService.md)

[Get-PmOrganization](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmOrganization.md)
