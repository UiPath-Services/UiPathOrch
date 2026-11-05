---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-PmTenant.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 11/05/2026
PlatyPS schema version: 2024-05-01
title: Remove-PmTenant
---

# Remove-PmTenant

## SYNOPSIS

Removes (soft-deletes) tenants.

## SYNTAX

### __AllParameterSets

```
Remove-PmTenant [-Path <string[]>] [-LiteralPath <string[]>] [-Name] <string[]> [-Confirm]
 [-NoWait] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Removes the tenants -Name selects. This is a soft delete: the tenant leaves the list and can no longer be read, and Automation Suite can bring it back with Restore-PmTenant -Id (Automation Cloud refuses that through the API). The tenant's id, which Restore-PmTenant needs, is shown in the -WhatIf / -Confirm line and, with -Verbose, after the removal. The cmdlet asks for confirmation (ConfirmImpact High).

A tenant write is asynchronous: the cmdlet waits until the operation it started has finished and the tenant is no longer Updating (a few seconds; a few minutes for a new tenant), so the next write to the tenant is not refused. Use -NoWait to return at once.

This cmdlet operates on the PSDrive of the UiPathOrch provider. It calls the Organization Management Service ({host}/{organization}/organization_), the API of `uip admin tenants`, which Automation Cloud and Automation Suite have and a standalone Orchestrator does not.

Primary Endpoint: DELETE /organization_/api/organization/{organizationId}/tenants/{tenantId}

Required permissions: Organization administrator.

## EXAMPLES

### Example 1: Remove a tenant, keeping its id

```powershell
PS Orch1:\> Remove-PmTenant Test2 -Verbose
```

The verbose line names the id that Restore-PmTenant takes.

## PARAMETERS

### -Name

Specifies the tenant names. A name without wildcards must match exactly one tenant; a name with wildcards selects every tenant that matches. Tab completion suggests the tenant names of the target organization.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
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

### None

This cmdlet does not output anything.

## NOTES

The organization-level cmdlets act on the whole organization that the drive belongs to, not only on the drive's tenant.

## RELATED LINKS

[Get-PmTenant](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmTenant.md)

[Get-PmTenantService](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmTenantService.md)

[Get-PmOrganization](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmOrganization.md)
