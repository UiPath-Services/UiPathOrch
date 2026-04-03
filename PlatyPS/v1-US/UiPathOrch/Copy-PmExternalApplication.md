---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Copy-PmExternalApplication
---

# Copy-PmExternalApplication

## SYNOPSIS

Copies external applications from one UiPath Automation Cloud organization to another.

## SYNTAX

### __AllParameterSets

```
Copy-PmExternalApplication [[-Name] <string[]>] [-Destination] <string[]> [-Path <string>] [-WhatIf]
 [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Copies external applications (OAuth clients) at the organization (platform management) level from a source organization to one or more destination organizations. The cmdlet replicates external application properties including scopes and, for confidential applications, group memberships.

If an external application with the same name already exists in the destination organization, the copy is skipped as a duplicate. The cmdlet preserves the application type, configured scopes, and redirect URIs.

For confidential applications, group memberships are also copied to the destination. Groups are matched by name (case-insensitive) in the destination.

The -Name parameter supports wildcards and tab completion. The -Destination and -Path parameters support tab completion for available drives.

Primary Endpoint: POST /api/ExternalClient (Identity Server)

OAuth required scopes: (Identity Server API - no per-endpoint scopes)

Required permissions: (managed by Identity Server)

## EXAMPLES

### Example 1: Copy all external applications to another organization

```powershell
PS Orch1:\> Copy-PmExternalApplication * Orch2:
```

Copies all external applications from the Orch1 organization to the Orch2 organization, including scopes and group memberships for confidential apps.

### Example 2: Copy a specific external application across drives

```powershell
PS C:\> Copy-PmExternalApplication -Path Orch1: uipathorch Orch2:
```

Copies the external application named "uipathorch" from the Orch1 organization to the Orch2 organization.

### Example 3: Copy external applications to multiple destinations

```powershell
PS Orch1:\> Copy-PmExternalApplication * Orch2:,Orch3:
```

Copies all external applications from the Orch1 organization to both the Orch2 and Orch3 organizations.

### Example 4: Preview a cross-organization copy

```powershell
PS Orch1:\> Copy-PmExternalApplication * Orch2: -WhatIf
```

Shows which external applications would be copied without performing the operation.

## PARAMETERS

### -Path

Specifies the source Pm: drive (organization) to copy external applications from. If not specified, the current drive is used. Tab completion suggests available drive names.

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

### -Destination

Specifies the destination Pm: drive(s) (organizations) to copy external applications to. Multiple destinations can be specified. Tab completion suggests available drive names.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 1
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Name

Specifies the name(s) of the external application(s) to copy. Supports wildcards for pattern matching. Tab completion dynamically suggests external application names from the source organization. If not specified, all external applications are copied.

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

### -WhatIf

Runs the command in a mode that only reports what would happen without performing the actions.

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe external application names to this cmdlet via the Name property.

## OUTPUTS

### UiPath.PowerShell.Entities.ExternalClientCreated

Returns ExternalClientCreated objects for each successfully created external application in the destination organization. The object includes the generated client ID and, for confidential applications, the client secret.

## NOTES

External applications with the same name already existing in the destination are skipped as duplicates.

For confidential applications, the copy includes scopes and group memberships. A new client secret is generated in the destination; the original secret is not transferred.

Non-confidential applications are copied with their scopes and redirect URIs.

## RELATED LINKS

Get-PmExternalApplication

Remove-PmExternalApplication

Get-PmExternalApiResource
