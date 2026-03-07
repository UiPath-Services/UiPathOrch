---
document type: cmdlet
external help file: UiPath.PowerShell.OrchProvider.dll-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Search-PmDirectory
---

# Search-PmDirectory

## SYNOPSIS

Searches the connected directory for users, groups, or applications by name.

## SYNTAX

### __AllParameterSets

```
Search-PmDirectory [-Name] <string> [-Path <string[]>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Searches the connected directory service (such as Azure AD or SAML directory) at the organization (platform management) level for entities matching the specified name. The search is performed against the external directory configured for the organization and returns matching users, groups, and applications.

This cmdlet is useful for looking up directory entities before adding them to the organization. The search query is sent to the directory service API and results include entity type, display name, and directory-specific identifiers.

Tab completion on the -Name parameter triggers a directory search, allowing interactive discovery of directory entities.

When multiple Pm: drives are connected, specifying -Path targets specific organizations. If -Path is omitted, the current drive is targeted.

Primary Endpoint: GET /api/Directory/Search/{partitionGlobalId} (Identity Server)

OAuth required scopes: (Identity Server API - no per-endpoint scopes)

Required permissions: (managed by Identity Server)

## EXAMPLES

### Example 1: Search for a directory entity by name

```powershell
PS Orch1:\> Search-PmDirectory ytsuda
```

Searches the connected directory for entities matching "ytsuda". Because -Name is a positional parameter (position 0), the parameter name can be omitted.

### Example 2: Search for a directory group

```powershell
PS Orch1:\> Search-PmDirectory admin
```

Searches the connected directory for entities matching "admin".

### Example 3: Search a specific organization's directory

```powershell
PS C:\> Search-PmDirectory -Name admin -Path Orch1:
```

Searches the directory connected to the Orch1 organization for entities matching "admin".

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

Specifies the search query string to match against directory entities. The search is performed against the external directory service configured for the organization. Tab completion triggers a live directory search for interactive discovery.

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String

You can pipe a search name string to this cmdlet via the Name property.

## OUTPUTS

### UiPath.PowerShell.Entities.PmDirectoryEntityInfo

Returns PmDirectoryEntityInfo objects representing matching directory entities, including entity type (user, group, or application), display name, and directory-specific identifiers.

## NOTES

This cmdlet searches the external directory service connected to the organization (e.g., Azure AD, SAML). It does not search local UiPath users or groups.

The -Name parameter accepts a single search string (not an array). The search behavior depends on the directory provider configuration.

## RELATED LINKS

Resolve-PmDirectoryNameBulk

Get-PmUser
