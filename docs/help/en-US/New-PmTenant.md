---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/New-PmTenant.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 11/05/2026
PlatyPS schema version: 2024-05-01
title: New-PmTenant
---

# New-PmTenant

## SYNOPSIS

Creates a tenant in the organization.

## SYNTAX

### __AllParameterSets

```
New-PmTenant [-Path <string[]>] [-LiteralPath <string[]>] [-Name] <string>
 [-Color <string>] [-Confirm] [-Environment <string>] [-NoWait] [-Region <string>]
 [-Service <string[]>] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Creates a tenant named -Name in the organization that the drive belongs to, and waits until it is provisioned (a few minutes on Automation Cloud), unless -NoWait. The new tenant is output.

The name must start with a letter and contain only letters and digits (no spaces, hyphens or other symbols), 2 to 32 characters.

-Service lists the services to provision. The organization's always-provisioned services (Orchestrator and others) are provisioned anyway; when -Service is omitted, orchestrator is requested, which Automation Suite requires the list to contain. Add more later with Add-PmTenantService.

A tenant write is asynchronous: the cmdlet waits until the operation it started has finished and the tenant is no longer Updating (a few seconds; a few minutes for a new tenant), so the next write to the tenant is not refused. Use -NoWait to return at once.

This cmdlet operates on the PSDrive of the UiPathOrch provider. It calls the Organization Management Service ({host}/{organization}/organization_), the API of `uip admin tenants`, which Automation Cloud and Automation Suite have and a standalone Orchestrator does not.

Primary Endpoint: POST /organization_/api/organization/{organizationId}/tenants

Required permissions: Organization administrator.

## EXAMPLES

### Example 1: Create a tenant

```powershell
PS Orch1:\> New-PmTenant Test2 -Environment Development
```

Creates the tenant Test2 and waits until it is ready.

### Example 2: Create a tenant with Test Manager

```powershell
PS Orch1:\> New-PmTenant QA -Region Japan -Service orchestrator, testmanager
```

## PARAMETERS

### -Name

Specifies the name of the new tenant: a letter first, then letters and digits only, 2 to 32 characters.

```yaml
Type: System.String
DefaultValue: None
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

### -Service

Specifies the services to provision, such as `testmanager`. Defaults to `orchestrator`. The always-provisioned services come anyway.

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

### -Environment

Specifies the environment tag: Production, NonProduction or Development.

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

### -Color

Specifies the tenant's color in the portal, such as `#3366FF`.

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

### -NoWait

Returns as soon as the request is accepted, without waiting for the tenant to settle. A tenant takes one write at a time: another write to it before this one is done is refused ("Another operation is in-progress").

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: False
SupportsWildcards: false
Aliases: []
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

### -WhatIf

Shows what would happen if the cmdlet runs.
The cmdlet is not run.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: False
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

### -Confirm

Prompts you for confirmation before running the cmdlet.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: False
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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe objects with the parameters' names as properties, such as the output of Get-PmTenant.

## OUTPUTS

### UiPath.PowerShell.Entities.PmTenant

The new tenant, as it is once provisioned (or as it was created, with -NoWait).

## NOTES

The organization-level cmdlets act on the whole organization that the drive belongs to, not only on the drive's tenant.

## RELATED LINKS

[Get-PmTenant](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmTenant.md)

[Get-PmTenantService](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmTenantService.md)

[Get-PmOrganization](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmOrganization.md)
