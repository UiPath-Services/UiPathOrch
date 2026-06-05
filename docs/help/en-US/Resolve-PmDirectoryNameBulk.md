---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Resolve-PmDirectoryNameBulk.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Resolve-PmDirectoryNameBulk
---

# Resolve-PmDirectoryNameBulk

## SYNOPSIS

Resolves directory entity names in bulk from UiPath Automation Cloud organizations.

## SYNTAX

### __AllParameterSets

```
Resolve-PmDirectoryNameBulk [-Path <string[]>] [-LiteralPath <string[]>] [-EntityType] <string> [-Name] <string[]>
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Resolves one or more directory entity names in bulk at the organization (platform management) level. This cmdlet performs a batch lookup against the external directory service to resolve user, group, or application names to their full directory representations.

The -EntityType parameter specifies the type of directory entity to resolve: User, Group, or Application. The -Name parameter accepts multiple names for bulk resolution.

The returned object type depends on the -EntityType value: DirectoryUser for User, DirectoryGroup for Group, or DirectoryApplication for Application.

When multiple Pm: drives are connected, specifying -Path targets specific organizations. If -Path is omitted, the current drive is targeted.

Primary Endpoint: POST /api/Directory/BulkResolveByName/{partitionGlobalId} (Identity Server)

OAuth required scopes: (Identity Server API - no per-endpoint scopes)

Required permissions: (managed by Identity Server)

## EXAMPLES

### Example 1: Resolve directory user names

```powershell
PS Orch1:\> Resolve-PmDirectoryNameBulk User ytsuda@gmail.com,ytsuda+c@gmail.com
```

Resolves the specified user names against the connected directory service. Because -EntityType and -Name are positional parameters (positions 0 and 1), the parameter names can be omitted.

### Example 2: Resolve directory group names

```powershell
PS Orch1:\> Resolve-PmDirectoryNameBulk Group Administrators,Everyone
```

Resolves the specified group names against the connected directory service.

### Example 3: Resolve directory application names

```powershell
PS Orch1:\> Resolve-PmDirectoryNameBulk Application uipathorch
```

Resolves the specified application name against the connected directory service.

### Example 4: Resolve names from a specific organization

```powershell
PS C:\> Resolve-PmDirectoryNameBulk -Path Orch1: -EntityType User -Name ytsuda@gmail.com
```

Resolves the specified user name against the directory connected to the Orch1 organization.

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

### -EntityType

Specifies the type of directory entity to resolve. Valid values are User, Group, and Application. This determines the type of object returned and the directory API endpoint used for resolution.

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

### -Name

Specifies the name(s) of directory entities to resolve. Multiple names can be specified for bulk resolution. The names are looked up against the external directory service configured for the organization.

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String

You can pipe EntityType values to this cmdlet.

### System.String[]

You can pipe Name values to this cmdlet.

## OUTPUTS

### UiPath.PowerShell.Entities.DirectoryUser

Returned when -EntityType is User. Contains directory user properties such as email, display name, and directory identifiers.

### UiPath.PowerShell.Entities.DirectoryGroup

Returned when -EntityType is Group. Contains directory group properties such as group name and directory identifiers.

### UiPath.PowerShell.Entities.DirectoryApplication

Returned when -EntityType is Application. Contains directory application properties such as application name and directory identifiers.

## NOTES

This cmdlet performs a bulk resolution against the external directory service connected to the organization. It is more efficient than multiple individual Search-PmDirectory calls when resolving many names at once.

The -EntityType parameter accepts exactly one of: User, Group, or Application.

## RELATED LINKS

[Search-PmDirectory](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Search-PmDirectory.md)

[Get-PmUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmUser.md)
