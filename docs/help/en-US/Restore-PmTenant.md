---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Restore-PmTenant.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 11/05/2026
PlatyPS schema version: 2024-05-01
title: Restore-PmTenant
---

# Restore-PmTenant

## SYNOPSIS

Restores tenants that Remove-PmTenant removed (Automation Suite).

## SYNTAX

### __AllParameterSets

```
Restore-PmTenant [-Path <string[]>] [-LiteralPath <string[]>] [-Id] <string[]> [-Confirm]
 [-NoWait] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Restores removed tenants by id: a removed tenant is no longer listed, so its name cannot be looked up. Remove-PmTenant shows the id in its -WhatIf / -Confirm line and with -Verbose. The tenants are output once restored.

Automation Suite allows this. Automation Cloud refuses it through the API (403), and the cmdlet says so.

A tenant write is asynchronous: the cmdlet waits until the operation it started has finished and the tenant is no longer Updating (a few seconds; a few minutes for a new tenant), so the next write to the tenant is not refused. Use -NoWait to return at once.

This cmdlet operates on the PSDrive of the UiPathOrch provider. It calls the Organization Management Service ({host}/{organization}/organization_), the API of `uip admin tenants`, which Automation Cloud and Automation Suite have and a standalone Orchestrator does not.

Primary Endpoint: PATCH /organization_/api/organization/{organizationId}/tenants/{tenantId}/restore

Required permissions: Organization administrator.

## EXAMPLES

### Example 1: Restore a removed tenant

```powershell
PS AS:\> Restore-PmTenant -Id cc9bfc4b-3405-4d20-b04c-6b4e9359f6ea
```

## PARAMETERS

### -Id

Specifies the ids (GUIDs) of the removed tenants.

```yaml
Type: System.String[]
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

The tenants once restored.

## NOTES

The organization-level cmdlets act on the whole organization that the drive belongs to, not only on the drive's tenant.

## RELATED LINKS

[Get-PmTenant](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmTenant.md)

[Get-PmTenantService](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmTenantService.md)

[Get-PmOrganization](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmOrganization.md)
