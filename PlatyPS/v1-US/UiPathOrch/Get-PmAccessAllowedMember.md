---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-PmAccessAllowedMember
---

# Get-PmAccessAllowedMember

## SYNOPSIS

Gets access-allowed members from a UiPath Automation Cloud organization.

## SYNTAX

### __AllParameterSets

```
Get-PmAccessAllowedMember [[-Name] <string[]>] [-Path <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets the list of members that are allowed access in a UiPath Automation Cloud organization at the platform management level. These are entities (users, groups, robot accounts, external applications) that have been granted access to the organization.

The -Name parameter supports wildcards and tab completion. Tab completion dynamically suggests member names along with their display names as tooltips.

Primary Endpoint: GET /api/identity/PartitionAccessPolicy/{partitionGlobalId} (Identity Server)

OAuth required scopes: (Identity Server API - no per-endpoint scopes)

Required permissions: (managed by Identity Server)

## EXAMPLES

### Example 1: Get all access-allowed members

```powershell
PS Orch1:\> Get-PmAccessAllowedMember
```

Gets all access-allowed members in the current organization.

### Example 2: Get a specific member by name

```powershell
PS Orch1:\> Get-PmAccessAllowedMember Administrators
```

Gets the access-allowed member with the specified name. Because -Name is a positional parameter (position 0), the parameter name can be omitted.

### Example 3: Get members matching a wildcard

```powershell
PS Orch1:\> Get-PmAccessAllowedMember *Admin*
```

Gets all access-allowed members whose names contain "Admin".

### Example 4: Get members from a specific organization

```powershell
PS C:\> Get-PmAccessAllowedMember -Path Orch1:
```

Gets all access-allowed members from the Orch1 organization.

## PARAMETERS

### -Path

Specifies the target Pm: drives (organizations). If not specified, the current drive is targeted. Tab completion suggests available drive names.

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

### -Name

Specifies the names of access-allowed members to retrieve. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests member names from the target organizations, showing display names as tooltips.

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe member names to this cmdlet via the Name property.

## OUTPUTS

### UiPath.PowerShell.Entities.AccessAllowedMember

Returns AccessAllowedMember objects with properties including name and displayName.

## NOTES

This cmdlet retrieves the access-allowed member list from the organization's security API. The returned list includes all entity types that have been granted access.

## RELATED LINKS

Get-PmUser

Get-PmGroup

Get-PmRobotAccount
