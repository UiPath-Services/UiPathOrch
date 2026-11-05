---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmOrganization.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 11/05/2026
PlatyPS schema version: 2024-05-01
title: Get-PmOrganization
---

# Get-PmOrganization

## SYNOPSIS

Gets the organization record: name, id, region, status, and its organization-level services.

## SYNTAX

### __AllParameterSets

```
Get-PmOrganization [[-Path] <string[]>] [-LiteralPath <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets the organization that the drive belongs to: its display name, the name used in URLs (logicalName), its id (the GUID other organization APIs take), status, region, country, language and creation date.

The organizationServiceInstances property lists the services enabled for the organization as a whole, such as portal, identity, docs and llmops. The default view shows the enabled ones in one line. The services of each tenant (orchestrator, testmanager and so on) are listed by Get-PmTenantService.

Results are cached per organization, so several drives of one organization share one API call.

Primary Endpoint: GET /organization_/api/organization/{organizationId}

Required permissions: Organization administrator.

This cmdlet works on UiPath Automation Cloud and on Automation Suite. A standalone Orchestrator has no Organization Management Service, and the cmdlet reports that instead.

## EXAMPLES

### Example 1: Get the current organization

```powershell
PS Orch1:\> Get-PmOrganization
```

### Example 2: List the organization-level services

```powershell
PS Orch1:\> (Get-PmOrganization).organizationServiceInstances | Format-Table serviceType, status, friendlyUrl
```

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe drive paths to this cmdlet by property name.

## OUTPUTS

### UiPath.PowerShell.Entities.PmOrganization

One object per organization, with name, logicalName, id, country, region, status, createdOn, language and organizationServiceInstances (PmService objects).

## NOTES

The tenants of the organization are listed by Get-PmTenant, and their services by Get-PmTenantService.

## RELATED LINKS

[Get-PmTenant](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmTenant.md)

[Get-PmTenantService](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmTenantService.md)
